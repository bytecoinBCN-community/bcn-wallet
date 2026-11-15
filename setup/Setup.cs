// BCN Wallet installer: a single exe with the program zip embedded as a resource.
// Installs per user (no administrator rights) to %LOCALAPPDATA%\Programs\BCN Wallet, adds Start menu / desktop
// shortcuts and an entry in "Installed apps". Wallets and settings live in %APPDATA%\BCN Wallet and are never
// touched by install, update or uninstall.
//   BCNWallet-x.y.z-setup.exe            interactive install / update
//   BCNWallet-x.y.z-setup.exe /S         silent install
//   uninstall.exe /uninstall [/S]        remove the program (kept in the install folder)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("BCN Wallet Setup")]
[assembly: AssemblyProduct("BCN Wallet")]
[assembly: AssemblyCompany("Bytecoin Community")]
[assembly: AssemblyCopyright("Bytecoin Community, LGPL-3.0")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]

static class Setup
{
    const string Version = "1.0.0";
    const string AppName = "BCN Wallet";
    const string Exe = "BCNWallet.exe";
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\BCNWallet";
    static readonly Color Navy = Color.FromArgb(0x14, 0x22, 0x32), Card = Color.FromArgb(0x10, 0x1B, 0x28),
        Line = Color.FromArgb(0x2B, 0x3F, 0x55), Ink = Color.FromArgb(0xEE, 0xF1, 0xF4), Muted = Color.FromArgb(0xA9, 0xB6, 0xC3),
        Coral = Color.FromArgb(0xFF, 0x6A, 0x55);

    static string DefaultDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName); } }

    // ------------------------------------------------------------------ strings (the 8 languages of bytecoin.community)
    static Dictionary<string, string> S;
    static void Lang()
    {
        var t = new Dictionary<string, Dictionary<string, string>>
        {
            { "en", D("Install BCN Wallet {0}", "The Bytecoin (BCN) wallet from the community. It is installed for your user only, no administrator rights needed.", "Folder", "Create a desktop shortcut", "Start BCN Wallet when done", "Install", "Cancel", "Installing…", "BCN Wallet is installed.", "Close BCN Wallet first, then press OK.", "Remove BCN Wallet from this computer?\n\nWallet files and settings in %APPDATA%\\BCN Wallet are kept. Make sure you have your 24 words anyway.", "BCN Wallet was removed. Your wallets are still in %APPDATA%\\BCN Wallet.", "Installation failed: {0}", "Change…") },
            { "uk", D("Встановлення BCN Wallet {0}", "Гаманець Bytecoin (BCN) від спільноти. Встановлюється лише для вашого користувача, права адміністратора не потрібні.", "Папка", "Створити ярлик на робочому столі", "Запустити BCN Wallet після встановлення", "Встановити", "Скасувати", "Встановлення…", "BCN Wallet встановлено.", "Спершу закрийте BCN Wallet, потім натисніть OK.", "Видалити BCN Wallet з цього комп'ютера?\n\nФайли гаманців і налаштування в %APPDATA%\\BCN Wallet буде збережено. Але про всяк випадок переконайтеся, що маєте свої 24 слова.", "BCN Wallet видалено. Ваші гаманці лишилися в %APPDATA%\\BCN Wallet.", "Не вдалося встановити: {0}", "Змінити…") },
            { "es", D("Instalar BCN Wallet {0}", "El monedero de Bytecoin (BCN) de la comunidad. Se instala solo para su usuario, sin permisos de administrador.", "Carpeta", "Crear un acceso directo en el escritorio", "Abrir BCN Wallet al terminar", "Instalar", "Cancelar", "Instalando…", "BCN Wallet está instalado.", "Cierre BCN Wallet primero y pulse Aceptar.", "¿Quitar BCN Wallet de este ordenador?\n\nLos monederos y ajustes en %APPDATA%\\BCN Wallet se conservan. Aun así, asegúrese de tener sus 24 palabras.", "BCN Wallet se ha quitado. Sus monederos siguen en %APPDATA%\\BCN Wallet.", "La instalación falló: {0}", "Cambiar…") },
            { "pt", D("Instalar o BCN Wallet {0}", "A carteira Bytecoin (BCN) da comunidade. É instalada só para o seu usuário, sem direitos de administrador.", "Pasta", "Criar atalho na área de trabalho", "Abrir o BCN Wallet ao terminar", "Instalar", "Cancelar", "Instalando…", "O BCN Wallet foi instalado.", "Feche o BCN Wallet primeiro e clique em OK.", "Remover o BCN Wallet deste computador?\n\nAs carteiras e configurações em %APPDATA%\\BCN Wallet são mantidas. Mesmo assim, confira se tem as suas 24 palavras.", "O BCN Wallet foi removido. Suas carteiras continuam em %APPDATA%\\BCN Wallet.", "Falha na instalação: {0}", "Alterar…") },
            { "tr", D("BCN Wallet {0} kurulumu", "Topluluğun Bytecoin (BCN) cüzdanı. Yalnızca sizin kullanıcınız için kurulur, yönetici hakları gerekmez.", "Klasör", "Masaüstü kısayolu oluştur", "Bitince BCN Wallet'ı başlat", "Kur", "İptal", "Kuruluyor…", "BCN Wallet kuruldu.", "Önce BCN Wallet'ı kapatın, sonra Tamam'a basın.", "BCN Wallet bu bilgisayardan kaldırılsın mı?\n\n%APPDATA%\\BCN Wallet içindeki cüzdanlar ve ayarlar korunur. Yine de 24 kelimenizin elinizde olduğundan emin olun.", "BCN Wallet kaldırıldı. Cüzdanlarınız %APPDATA%\\BCN Wallet içinde duruyor.", "Kurulum başarısız: {0}", "Değiştir…") },
            { "id", D("Pasang BCN Wallet {0}", "Dompet Bytecoin (BCN) dari komunitas. Dipasang hanya untuk pengguna Anda, tanpa hak administrator.", "Folder", "Buat pintasan di desktop", "Jalankan BCN Wallet setelah selesai", "Pasang", "Batal", "Memasang…", "BCN Wallet telah terpasang.", "Tutup BCN Wallet terlebih dulu, lalu tekan OK.", "Hapus BCN Wallet dari komputer ini?\n\nFile dompet dan pengaturan di %APPDATA%\\BCN Wallet tetap disimpan. Tetap pastikan Anda memiliki 24 kata Anda.", "BCN Wallet telah dihapus. Dompet Anda masih ada di %APPDATA%\\BCN Wallet.", "Pemasangan gagal: {0}", "Ubah…") },
            { "vi", D("Cài đặt BCN Wallet {0}", "Ví Bytecoin (BCN) của cộng đồng. Chỉ cài cho người dùng của bạn, không cần quyền quản trị.", "Thư mục", "Tạo lối tắt trên màn hình", "Chạy BCN Wallet khi xong", "Cài đặt", "Hủy", "Đang cài đặt…", "Đã cài đặt BCN Wallet.", "Hãy đóng BCN Wallet trước rồi nhấn OK.", "Gỡ BCN Wallet khỏi máy này?\n\nCác tệp ví và cài đặt trong %APPDATA%\\BCN Wallet được giữ lại. Dù vậy, hãy chắc chắn bạn có 24 từ của mình.", "Đã gỡ BCN Wallet. Các ví của bạn vẫn ở %APPDATA%\\BCN Wallet.", "Cài đặt thất bại: {0}", "Đổi…") },
            { "zh", D("安装 BCN Wallet {0}", "社区出品的 Bytecoin（BCN）钱包。仅为当前用户安装，不需要管理员权限。", "文件夹", "创建桌面快捷方式", "完成后启动 BCN Wallet", "安装", "取消", "正在安装…", "BCN Wallet 已安装。", "请先关闭 BCN Wallet，然后点击“确定”。", "要从这台电脑上删除 BCN Wallet 吗？\n\n%APPDATA%\\BCN Wallet 中的钱包文件和设置会保留。但请仍确认您保存了 24 个单词。", "BCN Wallet 已删除。您的钱包仍在 %APPDATA%\\BCN Wallet 中。", "安装失败：{0}", "更改…") }
        };
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (code == "ru" || code == "be") code = "uk";
        S = t.ContainsKey(code) ? t[code] : t["en"];
    }

    static Dictionary<string, string> D(params string[] v)
    {
        string[] k = { "title", "lead", "folder", "desktop", "launch", "install", "cancel", "installing", "done", "close_app", "un_ask", "un_done", "failed", "change" };
        var d = new Dictionary<string, string>();
        for (int i = 0; i < k.Length; i++) d[k[i]] = v[i];
        return d;
    }

    [STAThread]
    static int Main(string[] args)
    {
        Lang();
        bool silent = Array.Exists(args, a => a.Equals("/S", StringComparison.OrdinalIgnoreCase));
        if (Array.Exists(args, a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase))) return Uninstall(silent);
        if (silent)
        {
            try { Install(DefaultDir, true, null); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
        return 0;
    }

    // ------------------------------------------------------------------ install / uninstall

    static void WaitForApp(string dir, bool silent)
    {
        while (true)
        {
            bool running = false;
            foreach (var p in Process.GetProcessesByName("BCNWallet"))
            {
                try { if (p.MainModule.FileName.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) running = true; } catch { }
            }
            if (!running) return;
            if (silent) throw new Exception("BCN Wallet is running");
            if (MessageBox.Show(S["close_app"], AppName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                throw new OperationCanceledException();
        }
    }

    public static void Install(string dir, bool desktop, Action<int> progress)
    {
        dir = Path.GetFullPath(dir);
        WaitForApp(dir, progress == null);
        Directory.CreateDirectory(dir);
        using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
        using (var zip = new ZipArchive(res, ZipArchiveMode.Read))
        {
            int n = 0;
            foreach (var e in zip.Entries)
            {
                n++;
                if (progress != null) progress(n * 90 / zip.Entries.Count);
                var target = Path.GetFullPath(Path.Combine(dir, e.FullName));
                if (!target.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) continue; // no path traversal
                if (e.FullName.EndsWith("/")) { Directory.CreateDirectory(target); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                e.ExtractToFile(target, true);
            }
        }
        var self = Assembly.GetExecutingAssembly().Location;
        var un = Path.Combine(dir, "uninstall.exe");
        if (!string.Equals(self, un, StringComparison.OrdinalIgnoreCase)) File.Copy(self, un, true);
        var exe = Path.Combine(dir, Exe);
        var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk");
        Shortcut(startMenu, exe, dir);
        var desk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");
        if (desktop) Shortcut(desk, exe, dir);
        long size = 0;
        foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) try { size += new FileInfo(f).Length; } catch { }
        using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
        {
            k.SetValue("DisplayName", AppName);
            k.SetValue("DisplayVersion", Version);
            k.SetValue("Publisher", "Bytecoin Community");
            k.SetValue("DisplayIcon", exe + ",0");
            k.SetValue("InstallLocation", dir);
            k.SetValue("UninstallString", "\"" + un + "\" /uninstall");
            k.SetValue("QuietUninstallString", "\"" + un + "\" /uninstall /S");
            k.SetValue("URLInfoAbout", "https://bytecoin.community");
            k.SetValue("HelpLink", "https://github.com/bytecoinBCN-community/bcn-wallet");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            k.SetValue("EstimatedSize", (int)(size / 1024), RegistryValueKind.DWord);
        }
        if (progress != null) progress(100);
    }

    static void Shortcut(string lnk, string target, string workDir)
    {
        var t = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(t);
        object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnk });
        var st = sc.GetType();
        st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
        st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workDir });
        st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { target + ",0" });
        st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { "Bytecoin (BCN) wallet" });
        st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
    }

    static int Uninstall(bool silent)
    {
        string dir = null;
        using (var k = Registry.CurrentUser.OpenSubKey(UninstallKey)) if (k != null) dir = k.GetValue("InstallLocation") as string;
        if (string.IsNullOrEmpty(dir)) dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!silent && MessageBox.Show(S["un_ask"], AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return 1;
        try { WaitForApp(dir, silent); } catch { return 1; }
        // program files only; a "data" folder (portable use of the install dir) is left alone
        foreach (var name in new[] { Exe, "get-node.ps1", "LICENSE", "COPYING" }) TryDelete(Path.Combine(dir, name));
        foreach (var f in Directory.GetFiles(dir, "README*.md")) TryDelete(f);
        try { if (Directory.Exists(Path.Combine(dir, "node"))) Directory.Delete(Path.Combine(dir, "node"), true); } catch { }
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk"));
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"));
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false); } catch { }
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                if (k != null) k.DeleteValue(AppName, false);
        }
        catch { }
        // this exe cannot delete itself while running: a hidden cmd removes it (and the folder if now empty)
        var me = Assembly.GetExecutingAssembly().Location;
        var cmd = "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + me + "\" & rmdir \"" + dir + "\"";
        try { Process.Start(new ProcessStartInfo("cmd.exe", cmd) { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden }); } catch { }
        if (!silent) MessageBox.Show(S["un_done"], AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return 0;
    }

    static void TryDelete(string f) { try { if (File.Exists(f)) File.Delete(f); } catch { } }

    // ------------------------------------------------------------------ window

    class Form1 : Form
    {
        readonly TextBox dirBox;
        readonly CheckBox cbDesk, cbRun;
        readonly Button go, cancel, change;
        readonly ProgressBar bar;
        readonly Label status;
        readonly float k = 1;
        Point P(int x, int y) { return new Point((int)(x * k), (int)(y * k)); }
        Size Z(int w, int h) { return new Size((int)(w * k), (int)(h * k)); }

        public Form1()
        {
            Text = AppName + " " + Version;
            try { Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); } catch { }
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            // layout below is in 96-dpi pixels, scaled here to the monitor's DPI (fonts are in points already)
            AutoScaleMode = AutoScaleMode.None;
            using (var g = CreateGraphics()) k = g.DpiX / 96f;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Navy;
            ForeColor = Ink;
            ClientSize = Z(560, 330);

            var logo = new PictureBox { Location = P(28, 26), Size = Z(48, 48), SizeMode = PictureBoxSizeMode.Zoom };
            try { logo.Image = new Icon(Icon, 48, 48).ToBitmap(); } catch { }
            Controls.Add(logo);
            Controls.Add(new Label { Text = string.Format(S["title"], Version), Location = P(90, 26), AutoSize = true, Font = new Font("Segoe UI Semibold", 15f), ForeColor = Ink });
            Controls.Add(new Label { Text = S["lead"], Location = P(92, 62), Size = Z(440, 44), ForeColor = Muted });
            Controls.Add(new Label { Text = S["folder"], Location = P(28, 120), AutoSize = true, ForeColor = Muted });
            dirBox = new TextBox { Text = DefaultDir, Location = P(28, 142), Width = (int)(400 * k), BackColor = Card, ForeColor = Ink, BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(dirBox);
            change = Btn(S["change"], P(438, 140), 94, false);
            change.Click += (a, b) =>
            {
                using (var d = new FolderBrowserDialog { SelectedPath = dirBox.Text })
                    if (d.ShowDialog(this) == DialogResult.OK) dirBox.Text = Path.Combine(d.SelectedPath, AppName);
            };
            cbDesk = new CheckBox { Text = S["desktop"], Checked = true, Location = P(28, 184), AutoSize = true, ForeColor = Ink };
            cbRun = new CheckBox { Text = S["launch"], Checked = true, Location = P(28, 212), AutoSize = true, ForeColor = Ink };
            Controls.Add(cbDesk); Controls.Add(cbRun);
            bar = new ProgressBar { Location = P(28, 250), Size = Z(504, 6), Visible = false };
            Controls.Add(bar);
            status = new Label { Location = P(28, 262), Size = Z(504, 20), ForeColor = Muted };
            Controls.Add(status);
            cancel = Btn(S["cancel"], P(318, 284), 100, false);
            cancel.Click += (a, b) => Close();
            go = Btn(S["install"], P(428, 284), 104, true);
            go.Click += (a, b) => DoInstall();
            AcceptButton = go;
            CancelButton = cancel;
        }

        Button Btn(string text, Point p, int w, bool primary)
        {
            var b = new Button { Text = text, Location = p, Size = Z(w, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = primary ? Coral : Line;
            b.BackColor = primary ? Coral : Card;
            b.ForeColor = primary ? Card : Ink;
            if (primary) b.Font = new Font("Segoe UI Semibold", 9.5f);
            Controls.Add(b);
            return b;
        }

        void DoInstall()
        {
            go.Enabled = cancel.Enabled = change.Enabled = dirBox.Enabled = cbDesk.Enabled = cbRun.Enabled = false;
            bar.Visible = true;
            status.Text = S["installing"];
            var dir = dirBox.Text.Trim();
            bool desk = cbDesk.Checked, run = cbRun.Checked;
            var th = new Thread(() =>
            {
                Exception err = null;
                try { Install(dir, desk, p => BeginInvoke(new Action(() => bar.Value = p))); }
                catch (Exception ex) { err = ex; }
                BeginInvoke(new Action(() =>
                {
                    if (err is OperationCanceledException) { Close(); return; }
                    if (err != null)
                    {
                        MessageBox.Show(this, string.Format(S["failed"], err.Message), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        go.Enabled = cancel.Enabled = change.Enabled = dirBox.Enabled = cbDesk.Enabled = cbRun.Enabled = true;
                        bar.Visible = false; status.Text = "";
                        return;
                    }
                    status.Text = S["done"];
                    if (run) try { Process.Start(new ProcessStartInfo(Path.Combine(Path.GetFullPath(dir), Exe)) { UseShellExecute = true, WorkingDirectory = dir }); } catch { }
                    Close();
                }));
            }) { IsBackground = true };
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }
    }
}
