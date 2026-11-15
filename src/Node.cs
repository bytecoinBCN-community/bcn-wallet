using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BcnWallet
{
    // walletd.exe and bytecoind.exe from the official bcndev release (LGPL-3.0, github.com/bcndev/bytecoin).
    // They live in "node\" next to BCNWallet.exe. If they are missing, the wallet downloads the release archive
    // and installs it only when its SHA-256 matches the value pinned below.
    public static class NodeFiles
    {
        public const string Version = "3.5.1";
        public const string ZipUrl = "https://github.com/bcndev/bytecoin/releases/download/v3.5.1/bytecoin-daemons-3.5.1-win64.zip";
        public const string ZipSha256 = "daa6a8a3b5c3aec7abc05a1f001abe1c0afd2d540ae7cb8fe9dc6dfdb72aec96";
        public const long ZipSize = 25601161;
        public const string SourceUrl = "https://github.com/bcndev/bytecoin/releases/tag/v3.5.1";

        static string dir;
        public static string Dir
        {
            get
            {
                if (dir == null)
                {
                    var app = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "node");
                    if (File.Exists(Path.Combine(app, "walletd.exe")) || CanWrite(AppDomain.CurrentDomain.BaseDirectory)) dir = app;
                    else dir = Path.Combine(WalletSettings.Dir, "node");
                }
                return dir;
            }
        }

        public static string Walletd { get { return Path.Combine(Dir, "walletd.exe"); } }
        public static string Bytecoind { get { return Path.Combine(Dir, "bytecoind.exe"); } }
        public static bool Installed { get { return File.Exists(Walletd) && File.Exists(Bytecoind); } }

        static bool CanWrite(string folder)
        {
            try
            {
                var p = Path.Combine(folder, ".write-test");
                File.WriteAllText(p, "");
                File.Delete(p);
                return true;
            }
            catch { return false; }
        }

        // Runs on a worker thread. progress(0..100); throws on any error, never installs an unverified file.
        public static void Download(Action<int> progress)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var tmp = Path.Combine(Path.GetTempPath(), "bcn-wallet-node-" + Guid.NewGuid().ToString("N") + ".zip");
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(ZipUrl);
                req.UserAgent = "bcn-wallet/" + App.Version;
                req.Timeout = 30000;
                req.ReadWriteTimeout = 60000;
                using (var resp = req.GetResponse())
                using (var src = resp.GetResponseStream())
                using (var dst = File.Create(tmp))
                {
                    long total = resp.ContentLength > 0 ? resp.ContentLength : ZipSize, done = 0;
                    var buf = new byte[81920];
                    int n, last = -1;
                    while ((n = src.Read(buf, 0, buf.Length)) > 0)
                    {
                        dst.Write(buf, 0, n);
                        done += n;
                        int pct = (int)Math.Min(99, done * 100 / total);
                        if (pct != last) { last = pct; progress(pct); }
                    }
                }
                string hash;
                using (var f = File.OpenRead(tmp))
                using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").ToLowerInvariant();
                if (hash != ZipSha256) throw new Exception(L.F("node.bad_hash", ZipSha256, hash));
                Directory.CreateDirectory(Dir);
                int found = 0;
                using (var zip = ZipFile.OpenRead(tmp))
                    foreach (var e in zip.Entries)
                    {
                        if (e.Name != "walletd.exe" && e.Name != "bytecoind.exe") continue;
                        var target = Path.Combine(Dir, e.Name);
                        var part = target + ".part";
                        e.ExtractToFile(part, true);
                        if (File.Exists(target)) File.Delete(target);
                        File.Move(part, target);
                        found++;
                    }
                if (found < 2) throw new Exception("walletd.exe / bytecoind.exe not found in the archive");
                File.WriteAllText(Path.Combine(Dir, "README.txt"),
                    "walletd.exe and bytecoind.exe " + Version + " from the official Bytecoin release (bcndev), unchanged.\r\n" +
                    "Source and licence (LGPL-3.0): " + SourceUrl + "\r\n" +
                    "Archive SHA-256: " + ZipSha256 + "\r\n", Encoding.UTF8);
                progress(100);
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        public static int FreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }

        public static bool PortFree(int port)
        {
            try
            {
                var l = new TcpListener(IPAddress.Loopback, port);
                l.Start();
                l.Stop();
                return true;
            }
            catch { return false; }
        }
    }

    // One console daemon run without a window. Lines written to stdin are secrets (passwords, mnemonic);
    // they are never logged. The process is placed in a job object, so it dies together with the wallet.
    public class Daemon
    {
        Process p;
        readonly object gate = new object();
        readonly List<string> tail = new List<string>();
        public event Action<string> Line;
        public event Action<int> Exited;
        public string Name;

        public Daemon(string name) { Name = name; }

        public bool Running { get { lock (gate) return p != null && !p.HasExited; } }

        public List<string> Tail { get { lock (tail) return new List<string>(tail); } }

        public void Start(string exe, string args, IList<string> stdin)
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(exe),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.OutputDataReceived += (s, e) => OnLine(e.Data);
            proc.ErrorDataReceived += (s, e) => OnLine(e.Data);
            proc.Exited += (s, e) =>
            {
                int code = -1;
                try { code = proc.ExitCode; } catch { }
                var h = Exited;
                if (h != null) h(code);
            };
            lock (tail) tail.Clear();
            proc.Start();
            Job.Add(proc);
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            lock (gate) p = proc;
            if (stdin != null)
            {
                try
                {
                    foreach (var l in stdin) proc.StandardInput.WriteLine(l);
                    proc.StandardInput.Flush();
                }
                catch { }
            }
        }

        void OnLine(string l)
        {
            if (l == null) return;
            lock (tail)
            {
                tail.Add(l);
                if (tail.Count > 400) tail.RemoveRange(0, tail.Count - 400);
            }
            var h = Line;
            if (h != null) h(l);
        }

        // Ctrl+C first, then kill after the timeout. walletd/bytecoind 3.5.1 install no console handler, so Ctrl+C
        // ends them at once (as the official bytecoin-gui does); their LMDB databases survive that. The Ctrl+C path stays
        // so that builds which handle it get the chance to close cleanly.
        public void Stop(int timeoutMs)
        {
            Process proc;
            lock (gate) { proc = p; p = null; }
            if (proc == null) return;
            try
            {
                if (proc.HasExited) return;
                if (SendCtrlC(proc.Id) && proc.WaitForExit(timeoutMs)) return;
                proc.Kill();
                proc.WaitForExit(5000);
            }
            catch { }
        }

        [DllImport("kernel32.dll", SetLastError = true)] static extern bool AttachConsole(uint pid);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool FreeConsole();
        [DllImport("kernel32.dll")] static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);
        [DllImport("kernel32.dll")] static extern bool GenerateConsoleCtrlEvent(uint evt, uint group);
        static readonly object consoleGate = new object();

        static bool SendCtrlC(int pid)
        {
            lock (consoleGate)
            {
                try
                {
                    FreeConsole();
                    if (!AttachConsole((uint)pid)) return false;
                    SetConsoleCtrlHandler(IntPtr.Zero, true);
                    bool ok = GenerateConsoleCtrlEvent(0, 0);
                    Thread.Sleep(100);
                    FreeConsole();
                    // keep ignoring Ctrl+C for a moment: the event can still be in flight to this process
                    ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(2000); lock (consoleGate) SetConsoleCtrlHandler(IntPtr.Zero, false); });
                    return ok;
                }
                catch { return false; }
            }
        }

        // Runs a one-shot command (create mnemonic, export, set password) and returns exit code + output.
        public static int RunOnce(string exe, string args, IList<string> stdin, out List<string> output, int timeoutMs = 120000)
        {
            var lines = new List<string>();
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(exe),
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            using (var proc = new Process { StartInfo = psi })
            {
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) lock (lines) lines.Add(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (lines) lines.Add(e.Data); };
                proc.Start();
                Job.Add(proc);
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                try
                {
                    if (stdin != null) foreach (var l in stdin) proc.StandardInput.WriteLine(l);
                    proc.StandardInput.Close();
                }
                catch { }
                if (!proc.WaitForExit(timeoutMs)) { try { proc.Kill(); } catch { } output = lines; return -1; }
                proc.WaitForExit();
                lock (lines) output = new List<string>(lines);
                return proc.ExitCode;
            }
        }
    }

    static class Job
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr a, string name);
        [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job, int cls, ref ExtInfo info, int len);
        [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr proc);

        [StructLayout(LayoutKind.Sequential)]
        struct BasicInfo { public long a, b; public uint LimitFlags; public UIntPtr c, d; public uint e; public UIntPtr f; public uint g, h; }
        [StructLayout(LayoutKind.Sequential)]
        struct IoCounters { public ulong a, b, c, d, e, f; }
        [StructLayout(LayoutKind.Sequential)]
        struct ExtInfo { public BasicInfo Basic; public IoCounters Io; public UIntPtr m1, m2, m3, m4; }

        static IntPtr handle;

        public static void Add(Process p)
        {
            try
            {
                if (handle == IntPtr.Zero)
                {
                    handle = CreateJobObject(IntPtr.Zero, null);
                    var info = new ExtInfo();
                    info.Basic.LimitFlags = 0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                    SetInformationJobObject(handle, 9, ref info, Marshal.SizeOf(typeof(ExtInfo)));
                }
                AssignProcessToJobObject(handle, p.Handle);
            }
            catch { }
        }
    }
}
