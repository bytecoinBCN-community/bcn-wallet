using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace BcnWallet
{
    public class Contact
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string PaymentId { get; set; }
    }

    public class WalletSettings
    {
        public string NodeMode { get; set; }        // "remote" (no blockchain download) | "local" (own bytecoind, full blockchain)
        public string RemoteNode { get; set; }      // (1.0 setting, migrated into NodeChoice)
        public string NodeChoice { get; set; }      // "auto" or a node URL for walletd --bytecoind-remote-address
        public List<string> CustomNodes { get; set; } // nodes the user added by hand
        public string BlockchainDir { get; set; }   // bytecoind --data-folder in local mode; empty = default
        public string LastWallet { get; set; }
        public List<string> RecentWallets { get; set; }
        public string Theme { get; set; }           // "system" | "dark" | "light"
        public string Language { get; set; }        // "system" | one of L.Codes
        public bool MinimizeToTray { get; set; }
        public bool StartWithWindows { get; set; }
        public int FeeLevel { get; set; }           // 0 economy, 1 normal, 2 fast
        public int Anonymity { get; set; }          // mixins per input; walletd default 6
        public bool HideBalance { get; set; }
        public List<Contact> Contacts { get; set; }

        // The public node of the Bytecoin community (see bytecoin.community): HTTPS, CORS, no logs of wallet data.
        public const string CommunityNode = "https://node.bytecoin.community:443";

        public WalletSettings()
        {
            NodeMode = "remote";
            RemoteNode = CommunityNode;
            NodeChoice = "auto";
            CustomNodes = new List<string>();
            BlockchainDir = "";
            LastWallet = "";
            RecentWallets = new List<string>();
            Theme = "system";
            Language = "system";
            MinimizeToTray = false;
            FeeLevel = 1;
            Anonymity = 6;
            Contacts = new List<Contact>();
        }

        // Portable mode: a "data" folder next to BCNWallet.exe holds settings, wallet files, the walletd cache
        // and (in local-node mode) the blockchain. Without it, %APPDATA%\BCN Wallet is used.
        static string dir;
        public static string Dir
        {
            get
            {
                if (dir == null)
                {
                    var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                    dir = Directory.Exists(local) && Writable(local) ? local
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BCN Wallet");
                }
                return dir;
            }
        }

        public static bool Portable
        {
            get { return Dir.StartsWith(AppDomain.CurrentDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase); }
        }

        public static string WalletsDir { get { return Path.Combine(Dir, "wallets"); } }
        public static string WalletdDataDir { get { return Path.Combine(Dir, "walletd"); } }
        public static string LogsDir { get { return Path.Combine(Dir, "logs"); } }

        public string EffectiveBlockchainDir
        {
            get { return string.IsNullOrWhiteSpace(BlockchainDir) ? Path.Combine(Dir, "blockchain") : BlockchainDir; }
        }

        static bool Writable(string folder)
        {
            try
            {
                var probe = Path.Combine(folder, ".write-test");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        static string FilePath { get { return Path.Combine(Dir, "settings.json"); } }

        public static WalletSettings Load()
        {
            WalletSettings s = null;
            try
            {
                if (File.Exists(FilePath))
                    s = new JavaScriptSerializer().Deserialize<WalletSettings>(File.ReadAllText(FilePath, Encoding.UTF8));
            }
            catch { s = null; }
            if (s == null) s = new WalletSettings();
            s.Fix();
            return s;
        }

        void Fix()
        {
            if (NodeMode != "local") NodeMode = "remote";
            if (string.IsNullOrWhiteSpace(RemoteNode)) RemoteNode = CommunityNode;
            if (CustomNodes == null) CustomNodes = new List<string>();
            CustomNodes = CustomNodes.Select(Nodes.Normalize).Where(u => u != null).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (string.IsNullOrWhiteSpace(NodeChoice))
            {
                // 1.0 kept a single RemoteNode: keep a hand-entered one as the choice, otherwise go automatic
                var old = Nodes.Normalize(RemoteNode);
                bool known = old != null && Array.Exists(Nodes.Known, k => string.Equals(k.Url, old, StringComparison.OrdinalIgnoreCase));
                NodeChoice = old == null || old == Nodes.Normalize(CommunityNode) ? "auto" : old;
                if (old != null && !known && !CustomNodes.Contains(old)) CustomNodes.Add(old);
            }
            if (NodeChoice != "auto") NodeChoice = Nodes.Normalize(NodeChoice) ?? "auto";
            if (BlockchainDir == null) BlockchainDir = "";
            if (LastWallet == null) LastWallet = "";
            if (RecentWallets == null) RecentWallets = new List<string>();
            RecentWallets.RemoveAll(p => string.IsNullOrEmpty(p));
            if (Theme != "dark" && Theme != "light") Theme = "system";
            if (Language == null || Array.IndexOf(L.Codes, Language) < 0) Language = "system";
            if (FeeLevel < 0 || FeeLevel > 2) FeeLevel = 1;
            if (Anonymity < 0 || Anonymity > 50) Anonymity = 6;
            if (Contacts == null) Contacts = new List<Contact>();
            Contacts.RemoveAll(c => c == null || string.IsNullOrWhiteSpace(c.Address));
        }

        public List<string> AllNodes()
        {
            var l = Nodes.Known.Select(k => k.Url).ToList();
            foreach (var c in CustomNodes) if (!l.Contains(c, StringComparer.OrdinalIgnoreCase)) l.Add(c);
            return l;
        }

        public void Remember(string walletFile)
        {
            LastWallet = walletFile;
            RecentWallets.RemoveAll(p => string.Equals(p, walletFile, StringComparison.OrdinalIgnoreCase));
            RecentWallets.Insert(0, walletFile);
            while (RecentWallets.Count > 8) RecentWallets.RemoveAt(RecentWallets.Count - 1);
            Save();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(this), new UTF8Encoding(false));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch { }
        }
    }
}
