using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

[assembly: AssemblyTitle("BCN Wallet")]
[assembly: AssemblyProduct("BCN Wallet")]
[assembly: AssemblyDescription("Desktop wallet for Bytecoin (BCN): light mode via a remote node or a full node with the blockchain")]
[assembly: AssemblyCompany("Bytecoin Community")]
[assembly: AssemblyCopyright("Bytecoin Community, LGPL-3.0")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

namespace BcnWallet
{
    public static class App
    {
        public const string Version = "1.0.0";
        public const string SiteUrl = "https://bytecoin.community";
        public const string RepoUrl = "https://github.com/bytecoinBCN-community/bcn-wallet";
        public const string ExplorerUrl = "https://explorer.bytecoin.community";
        public const string WebWalletUrl = "https://web.bytecoin.community";
        public const string PoolUrl = "https://pool.bytecoin.community";
        public const string MinerUrl = "https://github.com/bytecoinBCN-community/bcn-miner/releases/latest";
        public const string TelegramUrl = "https://t.me/bytecoin_chat";
        public const string GithubUrl = "https://github.com/bytecoinBCN-community";
        public const string BcndevUrl = "https://github.com/bcndev/bytecoin";

        public static string SiteLang
        {
            get { return L.Code == "en" ? SiteUrl + "/" : SiteUrl + "/" + L.Code + "/"; }
        }

        public static string TxUrl(string hash) { return ExplorerUrl + "/#/tx/" + hash; }

        [STAThread]
        public static int Main(string[] args)
        {
            bool created;
            ForceUtf8Stdin();
            var settings = WalletSettings.Load();
            L.Set(settings.Language);
            using (var mutex = new Mutex(true, "Local\\BcnWalletGui-v1", out created))
            {
                if (!created)
                {
                    MessageBox.Show(L.T("app.running"), "BCN Wallet", MessageBoxButton.OK, MessageBoxImage.Information);
                    return 0;
                }
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.DispatcherUnhandledException += (s, e) =>
                {
                    WriteCrash(e.Exception);
                    MessageBox.Show(L.F("app.crash", e.Exception.Message, CrashPath), "BCN Wallet", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => WriteCrash(e.ExceptionObject as Exception);

                using (var st = Assembly.GetExecutingAssembly().GetManifestResourceStream("BcnWallet.Styles.xaml"))
                    app.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(st));

                Theme.Apply(settings.Theme);
                bool minimized = Array.IndexOf(args, "--minimized") >= 0;
                var w = new MainWindow(settings);
                if (!minimized || !settings.MinimizeToTray) w.Show();
                return app.Run();
            }
        }

        // Passwords and the mnemonic go to walletd through stdin. Process.StandardInput uses Console.InputEncoding:
        // in a windowed app that is the ANSI code page (a Cyrillic password would not match the one typed into
        // walletd's own console, which reads UTF-8), and on systems with UTF-8 as the ANSI code page it starts the
        // stream with a BOM that walletd would take as part of the first line. The setter needs a console, so the
        // cached value is replaced directly: UTF-8 without BOM.
        static void ForceUtf8Stdin()
        {
            var utf8 = new UTF8Encoding(false);
            try { Console.InputEncoding = utf8; return; } catch { }
            foreach (var name in new[] { "_inputEncoding", "inputEncoding" })
            {
                try
                {
                    var f = typeof(Console).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
                    if (f != null) { f.SetValue(null, utf8); return; }
                }
                catch { }
            }
        }

        static string CrashPath { get { return Path.Combine(WalletSettings.Dir, "crash.log"); } }

        static void WriteCrash(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(WalletSettings.Dir);
                File.AppendAllText(CrashPath, DateTime.Now.ToString("u") + " " + ex + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }
}
