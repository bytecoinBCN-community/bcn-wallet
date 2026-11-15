using System;
using System.IO;
using System.Linq;
using System.Management;

namespace BcnWallet
{
    // Checks for full-node mode: free space, SSD or HDD, and a blockchain that is already on this PC
    // (the official Bytecoin wallet keeps it in %APPDATA%\bytecoin).
    public static class Disk
    {
        public const long ChainBytes = 45L * 1024 * 1024 * 1024;     // ~41.7 GB in November 2026, plus growth
        public const long ReserveBytes = 5L * 1024 * 1024 * 1024;

        public static string OfficialDataFolder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bytecoin"); }
        }

        // Size of the blockchain database in a bytecoind --data-folder (0 if there is none).
        public static long ChainSize(string dataFolder)
        {
            try
            {
                var bc = Path.Combine(dataFolder, "blockchain");
                if (!Directory.Exists(bc)) return 0;
                return new DirectoryInfo(bc).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            catch { return 0; }
        }

        // A blockchain of at least 1 GB downloaded by the official wallet, or null.
        public static string FindExistingChain()
        {
            var d = OfficialDataFolder;
            return ChainSize(d) > (1L << 30) ? d : null;
        }

        public static long FreeBytes(string path)
        {
            try { return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).AvailableFreeSpace; } catch { return -1; }
        }

        // "SSD", "HDD" or null when Windows does not say (network drives, some VMs and RAID).
        public static string MediaType(string path)
        {
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(path));
                if (root == null || root.Length < 2 || root[1] != ':') return null;
                char letter = char.ToUpperInvariant(root[0]);
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                uint disk = uint.MaxValue;
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DiskNumber, DriveLetter FROM MSFT_Partition")))
                    foreach (ManagementObject p in s.Get())
                    {
                        var dl = p["DriveLetter"];
                        if (dl != null && char.ToUpperInvariant(Convert.ToChar(dl)) == letter) { disk = Convert.ToUInt32(p["DiskNumber"]); break; }
                    }
                if (disk == uint.MaxValue) return null;
                using (var s = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT DeviceId, MediaType FROM MSFT_PhysicalDisk")))
                    foreach (ManagementObject d in s.Get())
                    {
                        if (Convert.ToString(d["DeviceId"]) != disk.ToString()) continue;
                        int mt = Convert.ToInt32(d["MediaType"]);
                        return mt == 4 ? "SSD" : mt == 3 ? "HDD" : null;
                    }
            }
            catch { }
            return null;
        }

        public static string Gb(long bytes) { return (bytes / 1073741824.0).ToString("0.0"); }

        // Full warning for switching to full-node mode. ok = false when the disk is too small.
        public static string FullNodeReport(string dataFolder, out bool ok)
        {
            long have = ChainSize(dataFolder);
            long need = Math.Max(0, ChainBytes - have) + ReserveBytes;
            long free = FreeBytes(dataFolder);
            string media = MediaType(dataFolder);
            ok = free < 0 || free >= need;
            var lines = new System.Collections.Generic.List<string>();
            lines.Add(L.F("disk.folder", dataFolder));
            if (have > (1L << 30)) lines.Add(L.F("disk.have", Gb(have)));
            lines.Add(free < 0 ? L.F("disk.need", Gb(need)) : L.F("disk.need_free", Gb(need), Gb(free)));
            if (!ok) lines.Add(L.T("disk.too_small"));
            lines.Add(media == "SSD" ? L.T("disk.ssd") : media == "HDD" ? L.T("disk.hdd") : L.T("disk.unknown"));
            lines.Add(have > (1L << 30) ? L.T("disk.time_catchup") : media == "HDD" ? L.T("disk.time_hdd") : L.T("disk.time_ssd"));
            lines.Add(L.T("disk.switch_back"));
            return string.Join("\n", lines);
        }
    }
}
