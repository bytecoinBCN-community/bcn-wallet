using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BcnWallet
{
    public enum OpenKind { Open, Create, Restore, ImportKeys, Legacy, Hardware }

    public class OpenRequest
    {
        public OpenKind Kind;
        public string File;
        public string Password;
        public string Mnemonic;     // Create / Restore
        public string Keys;         // ImportKeys: 256 hex chars from an old (legacy) wallet
        public long CreationTs;     // Restore: unix time to start scanning from (0 = from the beginning)
        public string MnemonicPassword = "";    // Restore: BIP39 passphrase (from an .amwbackup file; normally empty)
        public int AddressCount = 1;            // Restore: addresses to recreate after opening
        public string ExpectFirstAddress;       // Restore from a backup: the first address the file names
    }

    public class TxItem
    {
        public string Hash;
        public long Height;
        public long Timestamp;
        public long Net;            // signed change of our balance, atomic units (fee included for outgoing)
        public long Fee;
        public string Counterparty; // recipient for outgoing transactions; incoming ones do not reveal the sender
        public string ToOurAddress; // which of our addresses received it
        public string PaymentId;
        public bool Confirmed;
        public bool Coinbase;
        public bool Locked;
    }

    public class Snapshot
    {
        public long WalletHeight, KnownHeight, NodeHeight, NodeKnownHeight;
        public int Peers;
        public string LowerError = "";
        public long Spendable, Locked;
        public long FeePerByte;
        public long TopTimestamp;
        public bool NodeReady;      // local mode: bytecoind answers
        public List<TxItem> Recent = new List<TxItem>();
        public bool Synced { get { return KnownHeight > 0 && WalletHeight + 2 >= KnownHeight && string.IsNullOrEmpty(LowerError); } }
    }

    public class WalletInfo
    {
        public string FirstAddress = "";
        public string Type = "";    // amethyst | legacy | hardware
        public bool ViewOnly;
        public long Created;
        public int AddressCount;
        public string Net = "";
    }

    // Starts the daemons, talks to walletd and keeps a fresh Snapshot. All heavy calls run off the UI thread;
    // events are raised on worker threads.
    public class WalletService
    {
        readonly WalletSettings S;
        readonly Daemon walletd = new Daemon("walletd");
        readonly Daemon bytecoind = new Daemon("bytecoind");
        Rpc rpc, nodeRpc;
        Thread poller;
        volatile bool stopping;
        int generation;
        string pollKey = "";
        long lastBalanceCheck;

        public event Action<string> Progress;       // human readable step while opening
        public event Action<Snapshot> Updated;
        public event Action<string> Failed;         // walletd stopped unexpectedly
        public event Action<string, string> LogLine;

        public WalletInfo Info { get; private set; }
        public Snapshot Last { get; private set; }
        public string File { get; private set; }
        public string Password { get; private set; }
        public bool IsOpen { get { return rpc != null; } }
        public bool OwnNode { get; private set; }   // we started bytecoind ourselves
        public string NodeAddress { get; private set; }

        public WalletService(WalletSettings s)
        {
            S = s;
            walletd.Line += l => Log("walletd", l);
            bytecoind.Line += l => Log("bytecoind", l);
        }

        void Log(string who, string l) { var h = LogLine; if (h != null) h(who, l); }
        void Step(string text) { var h = Progress; if (h != null) h(text); }

        public List<string> WalletdTail { get { return walletd.Tail; } }
        public List<string> NodeTail { get { return bytecoind.Tail; } }

        // ------------------------------------------------------------------ one-shot helpers

        static string Q(string path) { return "\"" + path.TrimEnd('\\') + "\""; }

        public static string CreateMnemonic()
        {
            List<string> o;
            int code = Daemon.RunOnce(NodeFiles.Walletd, "--create-mnemonic", null, out o, 30000);
            var words = o.LastOrDefault(l => l.Trim().Split(' ').Length >= 12);
            if (code != 0 || words == null) throw new Exception(L.T("err.mnemonic_gen"));
            return words.Trim();
        }

        public static bool CheckMnemonic(string mnemonic)
        {
            List<string> o;
            return Daemon.RunOnce(NodeFiles.Walletd, "--check-mnemonic", new[] { mnemonic }, out o, 30000) == 0;
        }

        // Shows the secret that restores the wallet: the BIP39 mnemonic (amethyst) or the keys (legacy).
        public string ExportSecret(string password)
        {
            List<string> o;
            bool hd = Info != null && Info.Type != "legacy";
            int code = Daemon.RunOnce(NodeFiles.Walletd, "--wallet-file=" + Q(File) + " --data-folder=" + Q(WalletSettings.WalletdDataDir) +
                (hd ? " --export-mnemonic" : " --export-keys"), new[] { password }, out o, 60000);
            if (code != 0) throw new Exception(ErrorText(code, o));
            for (int i = o.Count - 1; i >= 0; i--)
            {
                var l = o[i].Trim();
                if (l.Length == 0 || l.StartsWith("Enter", StringComparison.Ordinal) || l.StartsWith("Opened", StringComparison.Ordinal)) continue;
                if (l.IndexOf(':') >= 0 && l.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int c = l.LastIndexOf(':');
                    l = l.Substring(c + 1).Trim();
                    if (l.Length == 0) continue;
                }
                return l;
            }
            throw new Exception(L.T("err.export"));
        }

        public void ExportViewOnly(string password, string target)
        {
            List<string> o;
            int code = Daemon.RunOnce(NodeFiles.Walletd, "--wallet-file=" + Q(File) + " --data-folder=" + Q(WalletSettings.WalletdDataDir) +
                " --export-view-only=" + Q(target) + (Info != null && Info.Type == "amethyst" ? " --view-outgoing-addresses" : ""),
                new[] { password }, out o, 120000);
            if (code != 0) throw new Exception(ErrorText(code, o));
        }

        // walletd must not hold the file while it is re-encrypted: stop it, change the password, start again.
        public void ChangePassword(string oldPw, string newPw)
        {
            if (oldPw != Password) throw new Exception(L.T("err.207"));
            StopWalletd();
            List<string> o;
            int code = Daemon.RunOnce(NodeFiles.Walletd, "--wallet-file=" + Q(File) + " --data-folder=" + Q(WalletSettings.WalletdDataDir) + " --set-password",
                new[] { oldPw, newPw, newPw }, out o, 120000);
            string err = code == 0 ? null : ErrorText(code, o);
            StartWalletd(new OpenRequest { Kind = OpenKind.Open, File = File, Password = code == 0 ? newPw : oldPw });
            if (err != null) throw new Exception(err);
        }

        // ------------------------------------------------------------------ open / close

        // Blocking; call from a worker thread. Throws with a translated message.
        public void Open(OpenRequest req)
        {
            stopping = false;
            if (!NodeFiles.Installed) throw new Exception(L.T("err.no_node"));
            Directory.CreateDirectory(WalletSettings.WalletdDataDir);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(req.File)));
            if (S.NodeMode == "local") EnsureLocalNode();
            else { OwnNode = false; nodeRpc = null; NodeAddress = ChooseRemote(); }
            StartWalletd(req);
        }

        // Nodes that failed in this session; the automatic choice skips them (failover).
        public readonly HashSet<string> Avoid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<NodeStatus> LastSurvey { get; private set; }

        string ChooseRemote()
        {
            if (S.NodeChoice != "auto") return S.NodeChoice;
            Step(L.T("open.choosing"));
            LastSurvey = Nodes.Survey(S.AllNodes());
            var pick = Nodes.PickAuto(LastSurvey, Avoid);
            if (pick == null) throw new Exception(L.T("err.no_nodes"));
            Step(L.F("open.remote", pick));
            return pick;
        }

        void EnsureLocalNode()
        {
            // An already running bytecoind on the default port is reused (for example the user's own node).
            if (bytecoind.Running && nodeRpc != null) return;
            var probe = new Rpc("127.0.0.1:8081", null);
            try
            {
                probe.Call("get_status", null, 3000);
                nodeRpc = probe; OwnNode = false; NodeAddress = "127.0.0.1:8081";
                Step(L.T("open.node_found"));
                return;
            }
            catch { }
            var dataDir = S.EffectiveBlockchainDir;
            Directory.CreateDirectory(dataDir);
            int rpcPort = NodeFiles.PortFree(8081) ? 8081 : NodeFiles.FreePort();
            var args = "--data-folder=" + Q(dataDir) + " --bytecoind-bind-address=127.0.0.1:" + rpcPort;
            if (!NodeFiles.PortFree(8080)) args += " --p2p-bind-address=0.0.0.0:" + NodeFiles.FreePort();
            int exitCode = int.MinValue;
            Action<int> onExit = c => exitCode = c;
            bytecoind.Exited += onExit;
            Step(L.T("open.node_start"));
            bytecoind.Start(NodeFiles.Bytecoind, args, null);
            nodeRpc = new Rpc("127.0.0.1:" + rpcPort, null);
            OwnNode = true;
            NodeAddress = "127.0.0.1:" + rpcPort;
            var until = DateTime.UtcNow.AddMinutes(30);
            try
            {
                while (DateTime.UtcNow < until)
                {
                    if (stopping) throw new Exception(L.T("err.cancelled"));
                    if (exitCode != int.MinValue) throw new Exception(L.T("err.node_exit") + " " + ErrorText(exitCode, bytecoind.Tail));
                    try { nodeRpc.Call("get_status", null, 3000); return; } catch { }
                    Thread.Sleep(1000);
                }
                throw new Exception(L.T("err.node_timeout"));
            }
            finally { bytecoind.Exited -= onExit; }
        }

        void StartWalletd(OpenRequest req)
        {
            int gen = Interlocked.Increment(ref generation);
            int port = NodeFiles.FreePort();
            var pass = RandomToken(24);
            var auth = "gui:" + pass;
            var args = new StringBuilder();
            args.Append("--wallet-file=").Append(Q(Path.GetFullPath(req.File)));
            args.Append(" --data-folder=").Append(Q(WalletSettings.WalletdDataDir));
            args.Append(" --walletd-bind-address=127.0.0.1:").Append(port);
            args.Append(" --bytecoind-remote-address=").Append(NodeAddress);
            var stdin = new List<string>();
            switch (req.Kind)
            {
                case OpenKind.Create:
                case OpenKind.Restore:
                    args.Append(" --create-wallet --launch-after-command --creation-timestamp=")
                        .Append(req.Kind == OpenKind.Create ? "now" : Math.Max(0, req.CreationTs).ToString(CultureInfo.InvariantCulture));
                    stdin.Add(req.Mnemonic.Trim());
                    stdin.Add(req.MnemonicPassword ?? "");
                    stdin.Add(req.Password); stdin.Add(req.Password);
                    break;
                case OpenKind.ImportKeys:
                    args.Append(" --create-wallet --wallet-type=legacy --import-keys --launch-after-command");
                    stdin.Add(req.Keys.Trim());
                    stdin.Add(req.Password); stdin.Add(req.Password);
                    break;
                case OpenKind.Legacy:
                    // old-style (non-deterministic) wallet: random keys, backed up by the file or --export-keys
                    args.Append(" --create-wallet --wallet-type=legacy --launch-after-command --creation-timestamp=now");
                    stdin.Add(req.Password); stdin.Add(req.Password);
                    break;
                case OpenKind.Hardware:
                    // keys stay on the device (Ledger/Trezor with the Bytecoin app); the view key is imported so
                    // the wallet can scan without asking the device for every block
                    args.Append(" --create-wallet --wallet-type=hardware --launch-after-command --import-view-key");
                    stdin.Add(req.Password); stdin.Add(req.Password);
                    break;
                default:
                    stdin.Add(req.Password);
                    break;
            }
            stdin.Add(auth);

            int exitCode = int.MinValue;
            Action<int> onExit = null;
            onExit = c =>
            {
                exitCode = c;
                if (gen == generation && rpc != null && !stopping)
                {
                    rpc = null;
                    var f = Failed;
                    if (f != null) f(ErrorText(c, walletd.Tail));
                }
            };
            walletd.Exited += onExit;
            Step(req.Kind == OpenKind.Open ? L.T("open.wallet") : L.T("open.creating"));
            walletd.Start(NodeFiles.Walletd, args.ToString(), stdin);
            stdin.Clear();
            var r = new Rpc("127.0.0.1:" + port, auth);
            var until = DateTime.UtcNow.AddMinutes(5);
            while (true)
            {
                if (exitCode != int.MinValue) { walletd.Exited -= onExit; throw new Exception(ErrorText(exitCode, walletd.Tail)); }
                if (stopping) { walletd.Exited -= onExit; throw new Exception(L.T("err.cancelled")); }
                if (DateTime.UtcNow > until) { walletd.Exited -= onExit; walletd.Stop(5000); throw new Exception(L.T("err.wallet_timeout")); }
                try
                {
                    var wi = r.Call("get_wallet_info", new Dictionary<string, object> { { "need_secrets", false } }, 3000);
                    Info = new WalletInfo
                    {
                        FirstAddress = J.Str(wi, "first_address"), Type = J.Str(wi, "wallet_type"), ViewOnly = J.Bool(wi, "view_only"),
                        Created = J.Long(wi, "wallet_creation_timestamp"), AddressCount = (int)J.Long(wi, "total_address_count"), Net = J.Str(wi, "net")
                    };
                    break;
                }
                catch { Thread.Sleep(400); }
            }
            File = Path.GetFullPath(req.File);
            Password = req.Password;
            rpc = r;
            pollKey = "";
            // an amethyst wallet restored from its phrase starts with one address; recreate the others by index
            if (req.Kind == OpenKind.Restore && req.AddressCount > Info.AddressCount && Info.Type == "amethyst")
            {
                try
                {
                    r.Call("get_wallet_records", new Dictionary<string, object> { { "create", true }, { "index", 0 }, { "count", req.AddressCount } }, 60000);
                    Info.AddressCount = req.AddressCount;
                }
                catch (Exception ex) { Log("gui", "create addresses: " + ex.Message); }
            }
            StartPolling();
        }

        // ------------------------------------------------------------------ .amwbackup

        // First address of an amethyst wallet made from this phrase + passphrase: walletd creates a throw-away
        // wallet file (with a random password) in a temporary folder and prints the address.
        public static string AddressOfMnemonic(string mnemonic, string mnemonicPassword)
        {
            var dir = Path.Combine(Path.GetTempPath(), "bcnw-" + RandomToken(12));
            Directory.CreateDirectory(dir);
            try
            {
                var pw = RandomToken(24);
                List<string> o;
                int code = Daemon.RunOnce(NodeFiles.Walletd, "--wallet-file=" + Q(Path.Combine(dir, "probe.wallet")) + " --data-folder=" + Q(dir) +
                    " --create-wallet --creation-timestamp=now", new[] { mnemonic, mnemonicPassword ?? "", pw, pw }, out o, 60000);
                if (code != 0) throw new Exception(ErrorText(code, o));
                const string marker = "first address ";
                foreach (var l in o)
                {
                    int i = l.IndexOf(marker, StringComparison.Ordinal);
                    if (i >= 0) return l.Substring(i + marker.Length).Trim();
                }
                throw new Exception(L.T("err.export"));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        // The payload of an .amwbackup file (format version 2). snapshotReason is empty when the wallet state was
        // included, otherwise it says why not (the file then restores from the creation date).
        public Dictionary<string, object> BackupPayload(string walletPassword, Action<string> step, out string snapshotReason, out long snapshotHeight)
        {
            var r = rpc;
            if (r == null || Info == null) throw new Exception(L.T("err.closed"));
            if (Info.Type != "amethyst" || Info.ViewOnly) throw new Exception(L.T("bk.only_amethyst"));
            if (walletPassword != Password) throw new Exception(L.T("err.207"));
            step(L.T("bk.step_phrase"));
            var mnemonic = ExportSecret(walletPassword);
            if (mnemonic.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length < 12) throw new Exception(L.T("bk.only_amethyst"));
            // walletd cannot tell whether the phrase has a BIP39 passphrase; the file would restore a different
            // (empty) wallet if it had one. Rebuild the address from the phrase alone and compare.
            step(L.T("bk.step_check"));
            if (AddressOfMnemonic(mnemonic, "") != Info.FirstAddress) throw new Exception(L.T("bk.passphrase"));
            var wi = r.Call("get_wallet_info", new Dictionary<string, object> { { "need_secrets", false } }, 15000);
            var payload = new Dictionary<string, object> {
                { "format", Backup.Format }, { "version", 2 }, { "created_by", "BCN Wallet " + App.Version + " (Windows)" },
                { "saved_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }, { "net", J.Str(wi, "net").Length > 0 ? J.Str(wi, "net") : "main" },
                { "wallet", new Dictionary<string, object> {
                    { "type", "amethyst" }, { "mnemonic", mnemonic }, { "mnemonic_password", "" },
                    { "address_count", Math.Max(1, J.Long(wi, "total_address_count")) }, { "first_address", J.Str(wi, "first_address") },
                    { "creation_timestamp", J.Long(wi, "wallet_creation_timestamp") } } } };
            step(L.T("bk.step_state"));
            snapshotHeight = 0;
            Dictionary<string, object> snap = null;
            try { snap = MakeSnapshot(r, out snapshotReason); }
            catch (Exception ex) { snapshotReason = ex.Message; }
            if (snap != null) { payload["snapshot"] = snap; snapshotHeight = J.Long(snap, "height"); }
            return payload;
        }

        // Wallet state at top - 10 (format section 2): balance, unspent outputs with key images, history.
        Dictionary<string, object> MakeSnapshot(Rpc r, out string reason)
        {
            reason = "";
            var st = r.Call("get_status", null, 15000);
            long top = J.Long(st, "top_block_height"), known = J.Long(st, "top_known_block_height");
            if (!(known > 0 && top >= known)) { reason = L.T("bk.not_synced"); return null; }
            long height = top - Backup.SnapshotDepth;
            var node = Nodes.Normalize(NodeAddress);
            Dictionary<string, object> hdr = null;
            try { hdr = J.Obj(Nodes.Call(node, "get_block_header", new Dictionary<string, object> { { "height_or_depth", height } }, 20000), "block_header"); } catch { }
            if (hdr == null || J.Long(hdr, "height") != height || J.Str(hdr, "hash").Length != 64) { reason = L.F("bk.no_block", height); return null; }
            var at = new Dictionary<string, object> { { "height_or_depth", height } };
            var balance = r.Call("get_balance", at, 60000);
            var u = r.Call("get_unspents", at, 60000);
            var unspents = new List<object>();
            foreach (var k in new[] { "spendable", "locked_or_unconfirmed" }) unspents.AddRange(J.Arr(u, k).Cast<object>());
            decimal sum = 0;
            foreach (Dictionary<string, object> o in unspents)
            {
                if (J.Str(o, "key_image").Trim('0').Length == 0) { reason = L.T("bk.no_key_image"); return null; }
                sum += Convert.ToDecimal(o["amount"], CultureInfo.InvariantCulture);
            }
            decimal total = 0;
            foreach (var k in new[] { "spendable", "spendable_dust", "locked_or_unconfirmed" })
                if (balance.ContainsKey(k)) total += Convert.ToDecimal(balance[k], CultureInfo.InvariantCulture);
            long count = J.Long(balance, "spendable_outputs") + J.Long(balance, "spendable_dust_outputs") + J.Long(balance, "locked_or_unconfirmed_outputs");
            if (sum != total || unspents.Count != count) { reason = L.T("bk.mismatch"); return null; }
            var first = r.Call("get_transfers", new Dictionary<string, object> {
                { "from_height", 0 }, { "to_height", height }, { "forward", true }, { "desired_transaction_count", 1 } }, 60000);
            var fb = J.Obj(J.Arr(first, "blocks").FirstOrDefault(), "header");
            var blocks = new List<Dictionary<string, object>>();
            for (long from = 0; from < height; )
            {
                var page = r.Call("get_transfers", new Dictionary<string, object> {
                    { "from_height", from }, { "to_height", height }, { "forward", true }, { "desired_transaction_count", 1000 }, { "need_outputs", true } }, 120000);
                var pb = J.Arr(page, "blocks");
                if (pb.Count == 0) break;
                blocks.AddRange(pb);
                long next = J.Long(page, "next_from_height");
                if (next <= from) break;
                from = next;
            }
            var snap = new Dictionary<string, object> {
                { "height", height }, { "block_hash", J.Str(hdr, "hash") }, { "block_timestamp", J.Long(hdr, "timestamp") },
                { "first_activity_height", fb != null ? J.Long(fb, "height") : 0 }, { "first_activity_timestamp", fb != null ? J.Long(fb, "timestamp") : 0 },
                { "balance", balance }, { "unspents", unspents }, { "history_from_height", 0L }, { "blocks", blocks } };
            // too long a history: drop the oldest tenth until it fits (balance and spending use the unspents)
            while (blocks.Count > 0 && Encoding.UTF8.GetByteCount(Backup.ToJson(snap)) > Backup.MaxPayload)
            {
                blocks = blocks.Skip((blocks.Count + 9) / 10).ToList();
                snap["blocks"] = blocks;
                snap["history_from_height"] = blocks.Count > 0 ? J.Long(J.Obj(blocks[0], "header"), "height") - 1 : height;
            }
            return snap;
        }

        void StopWalletd()
        {
            Interlocked.Increment(ref generation);
            rpc = null;
            walletd.Stop(15000);
        }

        // Stops everything. With an own bytecoind, it is given time to close the blockchain database cleanly.
        public void Close(bool stopNode)
        {
            stopping = true;
            StopWalletd();
            if (stopNode && OwnNode) { bytecoind.Stop(90000); nodeRpc = null; OwnNode = false; }
            Info = null; Last = null; File = null; Password = null;
        }

        public void StopNode()
        {
            if (OwnNode) bytecoind.Stop(90000);
            nodeRpc = null; OwnNode = false;
        }

        static string RandomToken(int n)
        {
            const string abc = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var b = new byte[n];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
            var sb = new StringBuilder();
            foreach (var x in b) sb.Append(abc[x % abc.Length]);
            return sb.ToString();
        }

        public static string ErrorText(int code, List<string> tail)
        {
            string key = "err." + code;
            string t = L.T(key);
            if (t == key)
            {
                // unknown code: show the daemon's last meaningful line
                string last = tail == null ? null : tail.LastOrDefault(l => l.Trim().Length > 0 && !l.StartsWith("Enter", StringComparison.Ordinal));
                t = L.F("err.code", code) + (last != null ? "\n" + last.Trim() : "");
            }
            return t;
        }

        // ------------------------------------------------------------------ polling

        void StartPolling()
        {
            if (poller != null && poller.IsAlive) return;
            poller = new Thread(PollLoop) { IsBackground = true, Name = "walletd-poll" };
            poller.Start();
        }

        void PollLoop()
        {
            while (!stopping)
            {
                var r = rpc;
                if (r != null)
                {
                    try { Poll(r); }
                    catch (Exception ex) { Log("gui", "poll: " + ex.Message); }
                }
                for (int i = 0; i < 20 && !stopping; i++) Thread.Sleep(100);
            }
        }

        void Poll(Rpc r)
        {
            var snap = new Snapshot();
            var st = r.Call("get_status", null, 15000);
            snap.WalletHeight = J.Long(st, "top_block_height");
            snap.KnownHeight = J.Long(st, "top_known_block_height");
            snap.Peers = (int)(J.Long(st, "outgoing_peer_count") + J.Long(st, "incoming_peer_count"));
            snap.LowerError = J.Str(st, "lower_level_error");
            snap.FeePerByte = J.Long(st, "recommended_fee_per_byte");
            snap.TopTimestamp = J.Long(st, "top_block_timestamp");
            var n = nodeRpc;
            if (n != null)
            {
                try
                {
                    var ns = n.Call("get_status", null, 5000);
                    snap.NodeHeight = J.Long(ns, "top_block_height");
                    snap.NodeKnownHeight = J.Long(ns, "top_known_block_height");
                    snap.Peers = (int)(J.Long(ns, "outgoing_peer_count") + J.Long(ns, "incoming_peer_count"));
                    snap.NodeReady = true;
                }
                catch { }
            }
            string key = J.Str(st, "top_block_hash") + "/" + J.Long(st, "transaction_pool_version");
            var prev = Last;
            bool changed = key != pollKey || prev == null || Environment.TickCount - lastBalanceCheck > 30000;
            if (changed)
            {
                var bal = r.Call("get_balance", null, 30000);
                snap.Spendable = J.Long(bal, "spendable") + J.Long(bal, "spendable_dust");
                snap.Locked = J.Long(bal, "locked_or_unconfirmed");
                snap.Recent = LoadTransfers(r, 0, uint.MaxValue, 60).Item1;
                pollKey = key;
                lastBalanceCheck = Environment.TickCount;
            }
            else
            {
                snap.Spendable = prev.Spendable; snap.Locked = prev.Locked; snap.Recent = prev.Recent;
            }
            Last = snap;
            var h = Updated;
            if (h != null) h(snap);
        }

        // Newest first. Returns (items, next_to_height for the following page, or 0 when there is nothing older).
        public Tuple<List<TxItem>, long> LoadTransfers(Rpc r, long fromHeight, long toHeight, int count)
        {
            var res = (r ?? rpc).Call("get_transfers", new Dictionary<string, object> {
                { "from_height", fromHeight }, { "to_height", toHeight }, { "forward", false }, { "desired_transaction_count", count } }, 60000);
            var list = new List<TxItem>();
            var seen = new HashSet<string>();
            foreach (var b in J.Arr(res, "blocks"))
            {
                var hdr = J.Obj(b, "header");
                foreach (var tx in J.Arr(b, "transactions"))
                {
                    var it = ParseTx(tx, hdr);
                    if (it != null && seen.Add(it.Hash)) list.Add(it);
                }
            }
            list.Sort((a, c) =>
            {
                if (a.Confirmed != c.Confirmed) return a.Confirmed ? 1 : -1;
                int k = c.Height.CompareTo(a.Height);
                return k != 0 ? k : c.Timestamp.CompareTo(a.Timestamp);
            });
            long next = J.Long(res, "next_to_height");
            if (next >= toHeight || next >= uint.MaxValue || list.Count == 0) next = 0;
            return Tuple.Create(list, next);
        }

        public Tuple<List<TxItem>, long> LoadOlder(long toHeight, int count)
        {
            var r = rpc;
            if (r == null) return Tuple.Create(new List<TxItem>(), 0L);
            return LoadTransfers(r, 0, toHeight, count);
        }

        static TxItem ParseTx(Dictionary<string, object> tx, Dictionary<string, object> hdr)
        {
            var it = new TxItem
            {
                Hash = J.Str(tx, "hash"),
                Height = J.Long(tx, "block_height"),
                Timestamp = J.Long(tx, "timestamp"),
                Fee = J.Long(tx, "fee"),
                Coinbase = J.Bool(tx, "coinbase"),
                PaymentId = J.Str(tx, "payment_id")
            };
            if (it.PaymentId.Trim('0').Length == 0) it.PaymentId = "";
            var bh = J.Str(tx, "block_hash");
            it.Confirmed = bh.Trim('0').Length > 0;
            if (it.Timestamp == 0 && hdr != null) it.Timestamp = J.Long(hdr, "timestamp");
            long net = 0;
            foreach (var t in J.Arr(tx, "transfers"))
            {
                long a = J.Long(t, "amount");
                bool ours = J.Bool(t, "ours");
                if (ours)
                {
                    net += a;
                    if (a > 0 && it.ToOurAddress == null) it.ToOurAddress = J.Str(t, "address");
                    if (J.Bool(t, "locked")) it.Locked = true;
                }
                else if (a > 0 && it.Counterparty == null) it.Counterparty = J.Str(t, "address");
            }
            it.Net = net;
            if (it.Hash.Length == 0) return null;
            return it;
        }

        // ------------------------------------------------------------------ sending, addresses

        public Dictionary<string, object> CreateTx(string address, long amount, string paymentId, long feePerByte, bool subtractFee, int anonymity)
        {
            var r = rpc;
            if (r == null) throw new Exception(L.T("err.closed"));
            var tr = new Dictionary<string, object> { { "address", address }, { "amount", amount } };
            var tx = new Dictionary<string, object> { { "transfers", new object[] { tr } }, { "anonymity", anonymity } };
            if (!string.IsNullOrEmpty(paymentId)) tx["payment_id"] = paymentId;
            var p = new Dictionary<string, object> {
                { "transaction", tx }, { "any_spend_address", true }, { "change_address", Info.FirstAddress },
                { "confirmed_height_or_depth", -6 }, { "subtract_fee_from_amount", subtractFee } };
            if (feePerByte > 0) p["fee_per_byte"] = feePerByte;
            return r.Call("create_transaction", p, 120000);
        }

        public void SendTx(string binaryTx)
        {
            var r = rpc;
            if (r == null) throw new Exception(L.T("err.closed"));
            r.Call("send_transaction", new Dictionary<string, object> { { "binary_transaction", binaryTx } }, 120000);
            pollKey = "";
        }

        // Proof that this wallet sent a transaction to an address (for P2P deals, exchanges, disputes).
        public string CreateSendproof(string txHash, string address, string message)
        {
            var r = rpc;
            if (r == null) throw new Exception(L.T("err.closed"));
            var res = r.Call("create_sendproof", new Dictionary<string, object> {
                { "transaction_hash", txHash }, { "message", message ?? "" }, { "addresses", new object[] { address } } }, 60000);
            var p = J.StrArr(res, "sendproofs");
            if (p.Count == 0) throw new Exception(L.T("proof.none"));
            return p[0];
        }

        public Dictionary<string, object> CheckSendproof(string proof)
        {
            var r = rpc;
            if (r == null) throw new Exception(L.T("err.closed"));
            return r.Call("check_sendproof", new Dictionary<string, object> { { "sendproof", proof } }, 60000);
        }

        public List<Tuple<string, string>> Addresses()
        {
            var r = rpc;
            var list = new List<Tuple<string, string>>();
            if (r == null) return list;
            var res = r.Call("get_wallet_records", new Dictionary<string, object> { { "need_secrets", false }, { "index", 0 }, { "count", 1000 } }, 30000);
            foreach (var rec in J.Arr(res, "records")) list.Add(Tuple.Create(J.Str(rec, "address"), J.Str(rec, "label")));
            if (list.Count == 0 && Info != null) list.Add(Tuple.Create(Info.FirstAddress, ""));
            return list;
        }

        public string NewAddress(string label)
        {
            var r = rpc;
            if (r == null) throw new Exception(L.T("err.closed"));
            var res = r.Call("create_addresses", new Dictionary<string, object> { { "secret_spend_keys", new object[] { "" } } }, 30000);
            var a = J.StrArr(res, "addresses").FirstOrDefault();
            if (a != null && !string.IsNullOrEmpty(label)) SetLabel(a, label);
            return a;
        }

        public void SetLabel(string address, string label)
        {
            var r = rpc;
            if (r == null) return;
            r.Call("set_address_label", new Dictionary<string, object> { { "address", address }, { "label", label ?? "" } }, 30000);
        }
    }
}
