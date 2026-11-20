using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using Ellipse = System.Windows.Shapes.Ellipse;
using Path = System.IO.Path;

namespace BcnWallet
{
    public partial class MainWindow : Window
    {
        readonly WalletSettings S;
        readonly WalletService W;
        bool exiting, trayHintShown, busy;
        int currentPage;
        Snapshot snap;
        readonly HashSet<string> knownTx = new HashSet<string>();
        bool knownTxLoaded;

        Border rootBorder;
        Button maxBtn;
        Grid bodyHost;          // start screen or the wallet shell
        Grid overlay;           // modal layer
        Border modalCard;
        RadioButton[] navs;
        FrameworkElement[] pages;
        Ellipse navDot;
        TextBlock navState, navHeight, navNode;

        System.Windows.Forms.NotifyIcon tray;
        System.Windows.Forms.ToolStripMenuItem trayShow, trayExit;

        public MainWindow(WalletSettings settings)
        {
            S = settings;
            W = new WalletService(S);
            Title = "BCN Wallet";
            var wa = SystemParameters.WorkArea;
            MinWidth = Math.Min(980, wa.Width); MinHeight = Math.Min(640, wa.Height);
            Width = Math.Max(MinWidth, Math.Min(1280, wa.Width * 0.94));
            Height = Math.Max(MinHeight, Math.Min(800, wa.Height * 0.94));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI");
            FontSize = 13;
            SetResourceReference(BackgroundProperty, "Bg");
            SetResourceReference(ForegroundProperty, "Text");
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 38,
                ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = false
            });
            SetLangAttr();
            Content = BuildRoot();
            ShowStart();

            W.Updated += s => Dispatcher.BeginInvoke(new Action(() => OnSnapshot(s)));
            W.Failed += msg => Dispatcher.BeginInvoke(new Action(() => OnWalletFailed(msg)));
            W.LogLine += (who, l) => { };

            StateChanged += (a, b) => SyncMaximized();
            SourceInitialized += (a, b) =>
            {
                try
                {
                    var hwnd = new WindowInteropHelper(this).Handle;
                    int round = 2; // DWMWCP_ROUND, Windows 11
                    DwmSetWindowAttribute(hwnd, 33, ref round, 4);
                }
                catch { }
            };
            Closing += OnClosing;
            SystemEvents.UserPreferenceChanged += OnSystemTheme;
            PreviewKeyDown += (a, e) => { if (e.Key == Key.Escape && overlay.Visibility == Visibility.Visible && modalCanCancel) CloseModal(); };
            SetupTray();
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        void SetLangAttr()
        {
            try { Language = System.Windows.Markup.XmlLanguage.GetLanguage(L.Code == "zh" ? "zh-CN" : L.Code); } catch { }
        }

        void SyncMaximized()
        {
            rootBorder.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
            maxBtn.Content = WindowState == WindowState.Maximized ? "" : "";
        }

        void ApplyLanguage(string setting)
        {
            S.Language = setting;
            S.Save();
            L.Set(setting);
            SetLangAttr();
            int page = currentPage;
            Content = BuildRoot();
            if (W.IsOpen) { ShowShell(); navs[page].IsChecked = true; if (snap != null) OnSnapshot(snap); }
            else ShowStart();
            SyncMaximized();
            UpdateTrayTexts();
        }

        // =============================================================================================
        // Layout
        // =============================================================================================

        UIElement BuildRoot()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new Grid();
            title.SetResourceReference(Panel.BackgroundProperty, "Title");
            var brand = U.H(8, U.Logo(16, true), U.T("BCN Wallet", 12, "Muted", true));
            ((TextBlock)brand.Children[1]).VerticalAlignment = VerticalAlignment.Center;
            brand.Margin = new Thickness(14, 0, 0, 0);
            brand.VerticalAlignment = VerticalAlignment.Center;
            title.Children.Add(brand);
            var caps = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var minBtn = new Button { Style = U.St("Cap"), Content = "", ToolTip = L.T("cap.min") };
            maxBtn = new Button { Style = U.St("Cap"), Content = "", ToolTip = L.T("cap.max") };
            var closeBtn = new Button { Style = U.St("CapClose"), Content = "", ToolTip = L.T("cap.close") };
            AutomationProperties.SetName(minBtn, L.T("cap.min")); AutomationProperties.SetName(maxBtn, L.T("cap.max")); AutomationProperties.SetName(closeBtn, L.T("cap.close"));
            minBtn.Click += (a, b) => WindowState = WindowState.Minimized;
            maxBtn.Click += (a, b) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            closeBtn.Click += (a, b) => Close();
            foreach (var c in new[] { minBtn, maxBtn, closeBtn })
            {
                WindowChrome.SetIsHitTestVisibleInChrome(c, true);
                caps.Children.Add(c);
            }
            title.Children.Add(caps);
            root.Children.Add(title);

            bodyHost = new Grid();
            Grid.SetRow(bodyHost, 1);
            root.Children.Add(bodyHost);

            overlay = new Grid { Visibility = Visibility.Collapsed };
            overlay.SetResourceReference(Panel.BackgroundProperty, "Overlay");
            Grid.SetRow(overlay, 1);
            modalCard = new Border { CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1), Padding = new Thickness(26, 24, 26, 22),
                MaxWidth = 560, MinWidth = 420, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24) };
            modalCard.SetResourceReference(Border.BackgroundProperty, "Card");
            modalCard.SetResourceReference(Border.BorderBrushProperty, "Line");
            modalCard.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 30, ShadowDepth = 6, Opacity = 0.35 };
            overlay.Children.Add(modalCard);
            root.Children.Add(overlay);

            rootBorder = new Border { BorderThickness = new Thickness(1), Child = root };
            rootBorder.SetResourceReference(Border.BorderBrushProperty, "Line");
            return rootBorder;
        }

        // The wallet shell: navigation + pages, built when a wallet is open.
        void ShowShell()
        {
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(232) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.Children.Add(BuildNav());
            var host = new Grid();
            Grid.SetColumn(host, 1);
            pages = new FrameworkElement[] { BuildOverview(), BuildSend(), BuildReceive(), BuildHistory(), BuildContacts(), BuildCommunity(), BuildMobile(), BuildSettings() };
            foreach (var p in pages) { p.Visibility = Visibility.Collapsed; host.Children.Add(p); }
            pages[0].Visibility = Visibility.Visible;
            currentPage = 0;
            body.Children.Add(host);
            bodyHost.Children.Clear();
            bodyHost.Children.Add(body);
        }

        UIElement BuildNav()
        {
            var nav = new DockPanel { LastChildFill = false };
            var border = new Border { BorderThickness = new Thickness(0, 0, 1, 0), Padding = new Thickness(12, 20, 12, 16), Child = nav };
            border.SetResourceReference(Border.BackgroundProperty, "Nav");
            border.SetResourceReference(Border.BorderBrushProperty, "Line");

            var top = new StackPanel();
            DockPanel.SetDock(top, Dock.Top);
            var names = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            names.Children.Add(U.T("BCN Wallet", 15, "Text", true));
            names.Children.Add(U.Link("Bytecoin Community", App.SiteLang, 11));
            var brand = U.H(10, U.Logo(32), names);
            brand.Margin = new Thickness(10, 0, 10, 20);
            top.Children.Add(brand);

            string[] labels = { L.T("nav.overview"), L.T("nav.send"), L.T("nav.receive"), L.T("nav.history"), L.T("nav.contacts"), L.T("nav.community"), L.T("nav.mobile"), L.T("nav.settings") };
            string[] icons = { U.IcHome, U.IcSend, U.IcReceive, U.IcHistory, U.IcBook, U.IcPeople, U.IcPhone, U.IcSettings };
            navs = new RadioButton[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var tb = new TextBlock { Text = labels[i], VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                var r = new RadioButton { Style = U.St("NavItem"), GroupName = "nav", Content = U.H(12, U.Icon(icons[i]), tb), IsChecked = i == 0 };
                AutomationProperties.SetName(r, labels[i]);
                r.Checked += (a, b) => ShowPage(idx);
                navs[i] = r;
                top.Children.Add(r);
            }
            nav.Children.Add(top);

            var status = new StackPanel();
            var line = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(10, 14, 10, 0), Child = status };
            line.SetResourceReference(Border.BorderBrushProperty, "Line");
            DockPanel.SetDock(line, Dock.Bottom);
            navDot = U.Dot("Muted");
            navState = U.T(L.T("st.connecting"), 13, "Muted", true);
            status.Children.Add(U.H(8, navDot, navState));
            navHeight = U.T("", 12, "Muted"); navHeight.FontFamily = U.Mono; navHeight.Margin = new Thickness(0, 6, 0, 0);
            navNode = U.T(NodeLabel(), 12, "Muted"); navNode.FontFamily = U.Mono; navNode.Margin = new Thickness(0, 6, 0, 0);
            navNode.TextTrimming = TextTrimming.CharacterEllipsis; navNode.TextWrapping = TextWrapping.NoWrap;
            status.Children.Add(navHeight);
            status.Children.Add(navNode);
            var lockBtn = U.Btn(L.T("nav.lock"), "Small", (a, b) => LockWallet(), U.IcLock);
            lockBtn.Margin = new Thickness(0, 12, 0, 0);
            lockBtn.HorizontalAlignment = HorizontalAlignment.Left;
            lockBtn.ToolTip = L.T("nav.lock_hint");
            status.Children.Add(lockBtn);
            nav.Children.Add(line);
            return border;
        }

        string NodeLabel()
        {
            if (S.NodeMode == "local") return L.T("nav.own_node");
            var n = (W.NodeAddress ?? (S.NodeChoice == "auto" ? "" : S.NodeChoice)).Replace("https://", "").Replace("http://", "");
            if (n.EndsWith(":443")) n = n.Substring(0, n.Length - 4);
            if (n.Length == 0) return L.T("nodes.auto");
            return S.NodeChoice == "auto" ? n + " · " + L.T("nodes.auto_short") : n;
        }

        void ShowPage(int idx)
        {
            if (pages == null) return;
            currentPage = idx;
            for (int i = 0; i < pages.Length; i++) pages[i].Visibility = i == idx ? Visibility.Visible : Visibility.Collapsed;
            if (idx == 2) RefreshAddresses();
            if (idx == 3) ReloadHistory();
            if (idx == 4) RebuildContacts();
            if (idx == 7) RefreshSettings();
        }

        void Go(int idx) { if (navs != null) navs[idx].IsChecked = true; }

        static StackPanel Page() { return new StackPanel { Margin = new Thickness(32, 28, 32, 28) }; }

        // =============================================================================================
        // Modal layer
        // =============================================================================================

        bool modalCanCancel = true;

        void ShowModal(UIElement content, bool canCancel = true, double maxWidth = 560)
        {
            modalCanCancel = canCancel;
            modalCard.MaxWidth = maxWidth;
            modalCard.Child = content;
            overlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var first = FindFocusable(content);
                if (first != null) first.Focus();
            }), DispatcherPriority.Input);
        }

        static UIElement FindFocusable(DependencyObject d)
        {
            int n = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < n; i++)
            {
                var c = VisualTreeHelper.GetChild(d, i);
                if (c is TextBox || c is PasswordBox) return (UIElement)c;
                var r = FindFocusable(c);
                if (r != null) return r;
            }
            return null;
        }

        void CloseModal()
        {
            overlay.Visibility = Visibility.Collapsed;
            modalCard.Child = null;
        }

        // Title, text, buttons (first = primary). Returns the panel so callers can add inputs.
        StackPanel Dialog(string title, string text, out WrapPanel buttons)
        {
            var sp = new StackPanel();
            var h = U.T(title, 18, "Text", true);
            sp.Children.Add(h);
            if (!string.IsNullOrEmpty(text))
            {
                var t = U.T(text, 13, "Muted");
                t.Margin = new Thickness(0, 10, 0, 0);
                sp.Children.Add(t);
            }
            buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 22, 0, 0) };
            return sp;
        }

        void Info(string title, string text)
        {
            WrapPanel bp;
            var sp = Dialog(title, text, out bp);
            var ok = U.Btn(L.T("b.ok"), "Primary", (a, b) => CloseModal());
            ok.IsDefault = true;
            bp.Children.Add(ok);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        void Error(string text) { Info(L.T("d.error"), text); }

        void Confirm(string title, string text, string yes, Action onYes)
        {
            WrapPanel bp;
            var sp = Dialog(title, text, out bp);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, b) => CloseModal());
            var y = U.Btn(yes, "Primary", (a, b) => { CloseModal(); onYes(); });
            y.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(y);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        void AskPassword(string title, string text, Action<string> ok)
        {
            WrapPanel bp;
            var sp = Dialog(title, text, out bp);
            var pb = U.Pass(L.T("f.password"));
            var lab = U.Labeled(L.T("f.password"), pb);
            lab.Margin = new Thickness(0, 16, 0, 0);
            sp.Children.Add(lab);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, b) => CloseModal());
            var y = U.Btn(L.T("b.continue"), "Primary", (a, b) => { var p = pb.Password; pb.Clear(); CloseModal(); ok(p); });
            y.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(y);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        // Spinner-less progress dialog (text updates); no cancel.
        TextBlock progressText;
        void ShowProgress(string title, string text)
        {
            var sp = new StackPanel();
            var row = U.H(12, U.Logo(28), U.T(title, 17, "Text", true));
            ((TextBlock)row.Children[1]).VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(row);
            progressText = U.T(text, 13, "Muted");
            progressText.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(progressText);
            var bar = new ProgressBar { IsIndeterminate = true, Height = 4, Margin = new Thickness(0, 18, 0, 0), BorderThickness = new Thickness(0) };
            bar.SetResourceReference(Control.ForegroundProperty, "Accent");
            bar.SetResourceReference(Control.BackgroundProperty, "Track");
            sp.Children.Add(bar);
            ShowModal(sp, false, 480);
        }

        // Runs work on a thread pool thread, then done/fail on the UI thread.
        void Run(Action work, Action done, Action<Exception> fail)
        {
            busy = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Exception err = null;
                try { work(); } catch (Exception ex) { err = ex; }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    busy = false;
                    if (err == null) { if (done != null) done(); }
                    else if (fail != null) fail(err);
                }));
            });
        }

        static string Msg(Exception ex)
        {
            if (ex is System.Net.WebException) return L.T("err.rpc") + "\n" + ex.Message;
            return ex.Message;
        }

        // =============================================================================================
        // Formatting helpers
        // =============================================================================================

        static string Short(string a)
        {
            if (string.IsNullOrEmpty(a) || a.Length < 24) return a ?? "";
            return a.Substring(0, 10) + "…" + a.Substring(a.Length - 6);
        }

        static CultureInfo Culture()
        {
            try { return CultureInfo.GetCultureInfo(L.Code == "zh" ? "zh-CN" : L.Code); } catch { return CultureInfo.InvariantCulture; }
        }

        static string Date(long unix)
        {
            if (unix <= 0) return "—";
            var dt = DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime;
            return dt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        static string Num(long n) { return n.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "); }

        string ContactName(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            var c = S.Contacts.FirstOrDefault(x => x.Address == address);
            return c != null ? c.Name : null;
        }

        static void Copy(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try { Clipboard.SetText(text); return; } catch { Thread.Sleep(60); }
            }
        }

        // Secrets may be copied whole, but the clipboard is wiped after 60 s if it still holds them.
        void CopySecret(string secret)
        {
            Copy(secret);
            Toast(L.T("toast.secret_copied"));
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            t.Tick += (a, b) =>
            {
                t.Stop();
                try { if (Clipboard.ContainsText() && Clipboard.GetText() == secret) Clipboard.Clear(); } catch { }
            };
            t.Start();
        }

        static void OpenFolder(string path)
        {
            try { Directory.CreateDirectory(path); Process.Start("explorer.exe", "\"" + path + "\""); } catch { }
        }

        // =============================================================================================
        // Wallet events
        // =============================================================================================

        void OnSnapshot(Snapshot s)
        {
            snap = s;
            if (pages == null) return;
            UpdateStatus(s);
            UpdateOverview(s);
            UpdateSendBalance(s);
            NotifyIncoming(s);
            CheckFailover(s);
        }

        // Automatic node choice: when the node is unreachable for 90 s, switch to another healthy one.
        DateTime badSince = DateTime.MaxValue;
        void CheckFailover(Snapshot s)
        {
            bool bad = S.NodeMode == "remote" && S.NodeChoice == "auto" && (s.KnownHeight == 0 || !string.IsNullOrEmpty(s.LowerError));
            if (!bad) { badSince = DateTime.MaxValue; return; }
            if (badSince == DateTime.MaxValue) { badSince = DateTime.UtcNow; return; }
            if ((DateTime.UtcNow - badSince).TotalSeconds < 90 || busy || overlay.Visibility == Visibility.Visible) return;
            badSince = DateTime.MaxValue;
            if (W.NodeAddress != null) W.Avoid.Add(Nodes.Normalize(W.NodeAddress));
            Reconnect(false);
        }

        void UpdateStatus(Snapshot s)
        {
            if (navState == null) return;
            string state, color;
            if (!string.IsNullOrEmpty(s.LowerError) && s.KnownHeight == 0) { state = L.T("st.no_node"); color = "Bad"; }
            else if (S.NodeMode == "local" && s.NodeKnownHeight > 0 && s.NodeHeight + 2 < s.NodeKnownHeight) { state = L.T("st.node_sync"); color = "Warn"; }
            else if (!s.Synced) { state = L.T("st.syncing"); color = "Warn"; }
            else { state = L.T("st.synced"); color = "Good"; }
            navState.Text = state;
            navState.SetResourceReference(TextBlock.ForegroundProperty, color);
            navDot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, color == "Good" ? "GoodDot" : color);
            long h = S.NodeMode == "local" && s.NodeHeight > 0 ? s.NodeHeight : s.KnownHeight;
            navHeight.Text = L.F("nav.block", Num(h), s.Peers);
            navNode.Text = NodeLabel();
            navNode.ToolTip = W.NodeAddress;
            if (tray != null) tray.Text = "BCN Wallet · " + (S.HideBalance ? "•••" : Amount.Format(s.Spendable) + " BCN");
        }

        void NotifyIncoming(Snapshot s)
        {
            var fresh = s.Recent.Where(t => !knownTx.Contains(t.Hash)).ToList();
            foreach (var t in fresh) knownTx.Add(t.Hash);
            if (!knownTxLoaded) { knownTxLoaded = s.Synced; return; }
            foreach (var t in fresh)
                if (t.Net > 0 && tray != null)
                    tray.ShowBalloonTip(5000, "BCN Wallet", L.F("t.received", Amount.Format(t.Net)), System.Windows.Forms.ToolTipIcon.Info);
        }

        void OnWalletFailed(string msg)
        {
            if (exiting) return;
            var file = W.File; var pw = W.Password;
            W.Close(false);
            snap = null;
            ShowStart();
            Error(L.T("err.walletd_stopped") + "\n\n" + msg);
        }

        void LockWallet() { LockWallet(null); }

        void LockWallet(Action then)
        {
            if (busy) return;
            ShowProgress(L.T("p.locking"), L.T("p.locking_text"));
            Run(() => W.Close(false), () => { snap = null; knownTx.Clear(); knownTxLoaded = false; CloseModal(); ShowStart(); if (then != null) then(); },
                ex => { CloseModal(); ShowStart(); if (then != null) then(); });
        }

        // Re-open the same wallet after a node setting changed (password is kept in memory while the wallet is open).
        void Reconnect(bool toSettings = true)
        {
            if (!W.IsOpen) return;
            var file = W.File; var pw = W.Password;
            bool stopNode = S.NodeMode != "local";
            ShowProgress(L.T("p.reconnect"), L.T("p.reconnect_text"));
            W.Progress += OnProgress;
            Run(() =>
            {
                W.Close(stopNode);
                W.Open(new OpenRequest { Kind = OpenKind.Open, File = file, Password = pw });
            }, () =>
            {
                W.Progress -= OnProgress;
                CloseModal();
                int page = currentPage;
                ShowShell();
                Go(toSettings ? 6 : page);
                if (!toSettings) Toast(L.F("nodes.switched", NodeLabel()));
            }, ex =>
            {
                W.Progress -= OnProgress;
                CloseModal();
                ShowStart();
                Error(Msg(ex));
            });
        }

        void OnProgress(string t) { Dispatcher.BeginInvoke(new Action(() => { if (progressText != null) progressText.Text = t; })); }

        // =============================================================================================
        // Tray, autorun, closing
        // =============================================================================================

        void SetupTray()
        {
            tray = new System.Windows.Forms.NotifyIcon();
            try { tray.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().Location); } catch { }
            tray.Text = "BCN Wallet";
            var menu = new System.Windows.Forms.ContextMenuStrip();
            trayShow = new System.Windows.Forms.ToolStripMenuItem("", null, (a, b) => ShowFromTray());
            trayExit = new System.Windows.Forms.ToolStripMenuItem("", null, (a, b) => { exiting = true; ShowFromTray(); Close(); });
            menu.Items.Add(trayShow);
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add(trayExit);
            tray.ContextMenuStrip = menu;
            tray.MouseClick += (a, b) => { if (b.Button == System.Windows.Forms.MouseButtons.Left) ShowFromTray(); };
            tray.Visible = true;
            UpdateTrayTexts();
        }

        void UpdateTrayTexts()
        {
            if (tray == null) return;
            trayShow.Text = L.T("t.show");
            trayExit.Text = L.T("t.exit");
        }

        void ShowFromTray()
        {
            Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        }

        void SetAutorun(bool on)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k == null) return;
                    if (on) k.SetValue("BCN Wallet", "\"" + System.Reflection.Assembly.GetEntryAssembly().Location + "\" --minimized");
                    else k.DeleteValue("BCN Wallet", false);
                }
            }
            catch { }
        }

        bool shuttingDown;

        void OnClosing(object sender, CancelEventArgs e)
        {
            if (shuttingDown) return;
            if (!exiting && S.MinimizeToTray && W.IsOpen)
            {
                e.Cancel = true;
                Hide();
                if (!trayHintShown)
                {
                    trayHintShown = true;
                    tray.ShowBalloonTip(4000, "BCN Wallet", L.T("t.hidden"), System.Windows.Forms.ToolTipIcon.Info);
                }
                return;
            }
            e.Cancel = true;
            exiting = true;
            if (busy && overlay.Visibility == Visibility.Visible && !modalCanCancel) return; // an operation is running; let it finish
            bool node = W.OwnNode;
            ShowProgress(L.T("p.exit"), node ? L.T("p.exit_node") : L.T("p.exit_text"));
            S.Save();
            Run(() => { W.Close(true); W.StopNode(); }, Shutdown, ex => Shutdown());
        }

        void Shutdown()
        {
            shuttingDown = true;
            SystemEvents.UserPreferenceChanged -= OnSystemTheme;
            tray.Visible = false;
            tray.Dispose();
            Application.Current.Shutdown();
        }

        void OnSystemTheme(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General || S.Theme != "system") return;
            Dispatcher.BeginInvoke(new Action(() => Theme.Apply("system")));
        }
    }
}
