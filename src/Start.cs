using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Path = System.IO.Path;

namespace BcnWallet
{
    // Start screen: connection mode, recent wallets, create / restore / open / import.
    public partial class MainWindow
    {
        RadioButton[] modeBtns;
        TextBlock modeDesc;

        void ShowStart()
        {
            pages = null; navs = null; navState = null;
            var sp = new StackPanel { MaxWidth = 820, Margin = new Thickness(32, 30, 32, 30) };

            var names = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var h = U.T("BCN Wallet", 28, "Text"); h.FontWeight = FontWeights.Bold;
            names.Children.Add(h);
            names.Children.Add(U.T(L.T("start.sub"), 13, "Muted"));
            var head = U.H(16, U.Logo(56), names);
            sp.Children.Add(head);

            if (!NodeFiles.Installed) sp.Children.Add(NodeMissingBanner());

            // connection mode
            var mode = new StackPanel();
            mode.Children.Add(U.H2(L.T("start.mode")));
            var seg = U.Segmented("mode", new[] { L.T("mode.remote"), L.T("mode.local") }, new[] { "remote", "local" }, S.NodeMode, v =>
            {
                if (v == S.NodeMode) return;
                if (v == "remote") { S.NodeMode = v; S.Save(); UpdateModeDesc(); return; }
                var folder = SuggestedChainDir();
                ConfirmLocalMode(folder, () =>
                {
                    S.BlockchainDir = string.Equals(folder, System.IO.Path.Combine(WalletSettings.Dir, "blockchain"), StringComparison.OrdinalIgnoreCase) ? "" : folder;
                    S.NodeMode = "local"; S.Save(); UpdateModeDesc();
                }, () => U.SelectSeg(modeBtns, "remote"));
            }, out modeBtns);
            seg.Margin = new Thickness(0, 12, 0, 0);
            mode.Children.Add(seg);
            modeDesc = U.T("", 12, "Muted");
            modeDesc.Margin = new Thickness(0, 10, 0, 0);
            mode.Children.Add(modeDesc);
            UpdateModeDesc();
            var modeCard = U.Card(mode);
            modeCard.Margin = new Thickness(0, 22, 0, 0);
            sp.Children.Add(modeCard);

            // actions
            var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 14, 0, 0) };
            grid.Children.Add(Tile(U.IcPlus, L.T("start.create"), L.T("start.create_d"), true, () => CreateFlow()));
            grid.Children.Add(Tile(U.IcKey, L.T("start.restore"), L.T("start.restore_d"), false, () => RestoreFlow()));
            grid.Children.Add(Tile(U.IcFolder, L.T("start.open"), L.T("start.open_d"), false, () => OpenFileFlow()));
            grid.Children.Add(Tile(U.IcLock, L.T("start.import"), L.T("start.import_d"), false, () => ImportFlow()));
            sp.Children.Add(grid);

            // less common wallet types, as in the official bytecoin-gui
            var more = new WrapPanel();
            var ml = U.T(L.T("start.more"), 12, "Muted");
            ml.VerticalAlignment = VerticalAlignment.Center;
            ml.Margin = new Thickness(0, 0, 12, 8);
            more.Children.Add(ml);
            var hw = U.Btn(L.T("start.hw"), "Small", (a, e) => { if (!NodeFiles.Installed) DownloadNodeFlow(HardwareFlow); else HardwareFlow(); }, U.IcShield);
            var lg = U.Btn(L.T("start.legacy"), "Small", (a, e) => { if (!NodeFiles.Installed) DownloadNodeFlow(LegacyFlow); else LegacyFlow(); }, U.IcKey);
            hw.ToolTip = L.T("start.hw_d"); lg.ToolTip = L.T("start.legacy_d");
            hw.Margin = new Thickness(0, 0, 8, 8); lg.Margin = new Thickness(0, 0, 8, 8);
            more.Children.Add(hw); more.Children.Add(lg);
            sp.Children.Add(more);

            // recent wallets
            var recent = S.RecentWallets.Where(File.Exists).ToList();
            if (recent.Count > 0)
            {
                var rs = new StackPanel();
                rs.Children.Add(U.H2(L.T("start.recent")));
                foreach (var f in recent)
                {
                    var file = f;
                    var name = U.T(Path.GetFileNameWithoutExtension(f), 14, "Text", true);
                    var path = U.T(f, 11, "Muted"); path.FontFamily = U.Mono; path.TextTrimming = TextTrimming.CharacterEllipsis; path.TextWrapping = TextWrapping.NoWrap;
                    var col = U.V(2, name, path);
                    var arrow = U.T(L.T("start.open_btn"), 13, "Link", true);
                    arrow.VerticalAlignment = VerticalAlignment.Center;
                    var row = new Button { Style = U.St("Row"), Padding = new Thickness(10, 8, 10, 8), Content = U.Columns("auto", Wrap(U.Icon(U.IcLock, 18), 12), "1*", col, "auto", arrow) };
                    AutomationProperties.SetName(row, Path.GetFileName(f));
                    row.Margin = new Thickness(-10, 8, -10, 0);
                    row.Click += (a, b) => OpenWithPassword(file);
                    rs.Children.Add(row);
                }
                var rc = U.Card(rs);
                rc.Margin = new Thickness(0, 14, 0, 0);
                sp.Children.Add(rc);
            }

            // languages + community
            var foot = new WrapPanel { Margin = new Thickness(0, 22, 0, 0) };
            for (int i = 0; i < L.Codes.Length; i++)
            {
                var code = L.Codes[i];
                var b = U.Btn(L.Names[i], "Small", (a, e) => { if (code != L.Code) ApplyLanguage(code); });
                b.Margin = new Thickness(0, 0, 8, 8);
                if (code == L.Code) b.SetResourceReference(Control.BorderBrushProperty, "Accent");
                foot.Children.Add(b);
            }
            sp.Children.Add(foot);
            var links = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            foreach (var l in new[] {
                Tuple.Create("bytecoin.community", App.SiteLang), Tuple.Create(L.T("c.explorer"), App.ExplorerUrl),
                Tuple.Create(L.T("c.miner"), App.MinerUrl), Tuple.Create("Telegram", App.TelegramUrl), Tuple.Create("GitHub", App.RepoUrl) })
            {
                var t = U.Link(l.Item1, l.Item2, 12);
                t.Margin = new Thickness(0, 0, 18, 6);
                links.Children.Add(t);
            }
            sp.Children.Add(links);
            var ver = U.T(L.F("start.version", App.Version, NodeFiles.Version) + (WalletSettings.Portable ? " · " + L.T("start.portable") : ""), 11, "Muted");
            ver.Margin = new Thickness(0, 8, 0, 0);
            sp.Children.Add(ver);

            bodyHost.Children.Clear();
            bodyHost.Children.Add(U.Scroll(new Border { Child = sp, HorizontalAlignment = HorizontalAlignment.Center }));
        }

        static Border Wrap(UIElement e, double right)
        {
            return new Border { Child = e, Margin = new Thickness(0, 0, right, 0), VerticalAlignment = VerticalAlignment.Center };
        }

        void UpdateModeDesc()
        {
            if (modeDesc == null) return;
            if (S.NodeMode != "local") { modeDesc.Text = L.F("mode.remote_d", S.NodeChoice == "auto" ? L.T("nodes.auto") : S.NodeChoice); return; }
            var t = L.F("mode.local_d", S.EffectiveBlockchainDir);
            long have = Disk.ChainSize(S.EffectiveBlockchainDir);
            if (have > (1L << 30)) t += "\n" + L.F("disk.have", Disk.Gb(have));
            modeDesc.Text = t;
        }

        // The blockchain folder to offer: the one already chosen, otherwise the official wallet's
        // %APPDATA%\bytecoin when it holds a blockchain (no second 40+ GB download), otherwise data\blockchain.
        string SuggestedChainDir()
        {
            if (!string.IsNullOrWhiteSpace(S.BlockchainDir)) return S.BlockchainDir;
            return Disk.FindExistingChain() ?? S.EffectiveBlockchainDir;
        }

        // Disk size, SSD/HDD and time warning before switching to full-node mode.
        void ConfirmLocalMode(string folder, Action yes, Action no)
        {
            ShowProgress(L.T("set.local_title"), L.T("disk.checking"));
            bool ok = true;
            string report = null;
            Run(() => report = Disk.FullNodeReport(folder, out ok), () =>
            {
                CloseModal();
                WrapPanel bp;
                var sp = Dialog(L.T("set.local_title"), null, out bp);
                var box = WarnBox(report);
                if (!ok) { box.SetResourceReference(Border.BackgroundProperty, "BadBg"); box.SetResourceReference(Border.BorderBrushProperty, "Bad"); }
                sp.Children.Add(box);
                var cancel = U.Btn(L.T("b.cancel"), ok ? "Secondary" : "Primary", (a, e) => { CloseModal(); no(); });
                bp.Children.Add(cancel);
                if (ok)
                {
                    var go = U.Btn(L.T("disk.go"), "Primary", (a, e) => { CloseModal(); yes(); });
                    go.Margin = new Thickness(10, 0, 0, 0);
                    bp.Children.Add(go);
                }
                sp.Children.Add(bp);
                ShowModal(sp, false, 600);
            }, ex => { CloseModal(); no(); Error(Msg(ex)); });
        }

        Button Tile(string icon, string title, string desc, bool primary, Action click)
        {
            var holder = new ContentControl { Content = U.Icon(icon, 20), Focusable = false, IsTabStop = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            if (primary) holder.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0x1B, 0x28));
            else holder.SetResourceReference(Control.ForegroundProperty, "Text");
            var ic = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(8), Child = holder };
            ic.SetResourceReference(Border.BackgroundProperty, primary ? "Accent" : "NavActive");
            var t = U.T(title, 15, "Text", true);
            var d = U.T(desc, 12, "Muted");
            d.Margin = new Thickness(0, 4, 0, 0);
            var content = U.Columns("auto", Wrap(ic, 14), "1*", U.V(0, t, d));
            var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(16, 16, 16, 16), Child = content };
            card.SetResourceReference(Border.BackgroundProperty, "Card");
            card.SetResourceReference(Border.BorderBrushProperty, "Line");
            var b = new Button { Style = U.St("Row"), Content = card, Margin = new Thickness(0, 0, 7, 14), Padding = new Thickness(0), VerticalContentAlignment = VerticalAlignment.Stretch };
            AutomationProperties.SetName(b, title);
            b.Click += (a, e) => { if (!NodeFiles.Installed) { DownloadNodeFlow(click); return; } click(); };
            return b;
        }

        Border NodeMissingBanner()
        {
            var t = U.T(L.F("node.missing", NodeFiles.Version, Math.Round(NodeFiles.ZipSize / 1048576.0, 1)), 13, "Text");
            t.VerticalAlignment = VerticalAlignment.Center;
            var b = U.Btn(L.T("node.download"), "Small", (a, e) => DownloadNodeFlow(null), U.IcDownload);
            b.Margin = new Thickness(16, 0, 0, 0);
            var banner = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 22, 0, 0), Child = U.Columns("1*", t, "auto", b) };
            banner.SetResourceReference(Border.BackgroundProperty, "WarnBg");
            banner.SetResourceReference(Border.BorderBrushProperty, "Warn");
            return banner;
        }

        void DownloadNodeFlow(Action then)
        {
            WrapPanel bp;
            var sp = Dialog(L.T("node.title"), L.F("node.text", NodeFiles.Version, Math.Round(NodeFiles.ZipSize / 1048576.0, 1), NodeFiles.ZipUrl, NodeFiles.ZipSha256), out bp);
            var bar = new ProgressBar { Height = 6, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 16, 0, 0), Visibility = Visibility.Collapsed, BorderThickness = new Thickness(0) };
            bar.SetResourceReference(Control.ForegroundProperty, "Accent");
            bar.SetResourceReference(Control.BackgroundProperty, "Track");
            sp.Children.Add(bar);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            Button go = null;
            go = U.Btn(L.T("node.download"), "Primary", (a, e) =>
            {
                go.IsEnabled = false; no.IsEnabled = false; modalCanCancel = false;
                bar.Visibility = Visibility.Visible;
                Run(() => NodeFiles.Download(p => Dispatcher.BeginInvoke(new Action(() => bar.Value = p))),
                    () => { CloseModal(); if (pages == null) ShowStart(); if (then != null) then(); },
                    ex => { CloseModal(); Error(L.T("node.failed") + "\n" + ex.Message); });
            }, U.IcDownload);
            go.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        // ------------------------------------------------------------------ open

        void OpenFileFlow()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = L.T("start.open"),
                Filter = L.T("f.wallet_filter") + " (*.wallet)|*.wallet|" + L.T("f.all_files") + " (*.*)|*.*",
                InitialDirectory = Directory.Exists(WalletSettings.WalletsDir) ? WalletSettings.WalletsDir : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog(this) == true) OpenWithPassword(dlg.FileName);
        }

        void OpenWithPassword(string file)
        {
            AskPassword(Path.GetFileName(file), L.T("open.password_text"), pw =>
                DoOpen(new OpenRequest { Kind = OpenKind.Open, File = file, Password = pw }));
        }

        void DoOpen(OpenRequest req)
        {
            ShowProgress(req.Kind == OpenKind.Open ? L.T("p.opening") : L.T("p.creating"),
                S.NodeMode == "local" ? L.T("open.node_start") : L.F("open.remote", NodeLabel()));
            W.Progress += OnProgress;
            Run(() => W.Open(req), () =>
            {
                W.Progress -= OnProgress;
                S.Remember(W.File);
                knownTx.Clear(); knownTxLoaded = false;
                CloseModal();
                ShowShell();
                if (req.Kind == OpenKind.Restore)
                    Info(L.T("restore.done"), L.T("restore.done_text"));
            }, ex =>
            {
                W.Progress -= OnProgress;
                CloseModal();
                Error(Msg(ex));
            });
        }

        // ------------------------------------------------------------------ create

        void CreateFlow()
        {
            ShowProgress(L.T("create.title"), L.T("create.generating"));
            string words = null;
            Run(() => words = WalletService.CreateMnemonic(), () => ShowMnemonicStep(words), ex => { CloseModal(); Error(Msg(ex)); });
        }

        void ShowMnemonicStep(string words)
        {
            WrapPanel bp;
            var sp = Dialog(L.T("create.title"), L.T("create.words_text"), out bp);
            sp.Children.Add(WordsGrid(words));
            var copy = U.Btn(L.T("b.copy_all"), "Small", (a, e) => CopySecret(words), U.IcCopy);
            copy.HorizontalAlignment = HorizontalAlignment.Left;
            copy.Margin = new Thickness(0, 12, 0, 0);
            copy.ToolTip = L.T("create.copy_warn");
            sp.Children.Add(copy);
            var warn = WarnBox(L.T("create.warn"));
            sp.Children.Add(warn);
            var cb = new CheckBox { Style = U.St("Switch"), Content = U.T(L.T("create.saved"), 13, "Text"), Margin = new Thickness(0, 14, 0, 0) };
            sp.Children.Add(cb);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var go = U.Btn(L.T("b.continue"), "Primary", (a, e) => ShowVerifyStep(words));
            go.IsEnabled = false;
            cb.Checked += (a, e) => go.IsEnabled = true;
            cb.Unchecked += (a, e) => go.IsEnabled = false;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp, true, 640);
        }

        UIElement WordsGrid(string words)
        {
            var w = words.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var g = new UniformGrid { Columns = 4, Margin = new Thickness(0, 16, 0, 0) };
            for (int i = 0; i < w.Length; i++)
            {
                var n = U.T((i + 1).ToString(CultureInfo.InvariantCulture), 11, "Muted");
                n.Width = 22; n.VerticalAlignment = VerticalAlignment.Center;
                var t = U.T(w[i], 13, "Text", true); t.FontFamily = U.Mono;
                var cell = new Border { CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 6, 6),
                    Child = U.H(4, n, t) };
                cell.SetResourceReference(Border.BackgroundProperty, "Input");
                cell.SetResourceReference(Border.BorderBrushProperty, "Line");
                g.Children.Add(cell);
            }
            return g;
        }

        Border WarnBox(string text)
        {
            var t = U.T(text, 12, "Text");
            var b = new Border { CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 14, 0, 0), Child = t };
            b.SetResourceReference(Border.BackgroundProperty, "WarnBg");
            b.SetResourceReference(Border.BorderBrushProperty, "Warn");
            return b;
        }

        // Ask for three random words: catches a mnemonic that was not really written down.
        void ShowVerifyStep(string words)
        {
            var w = words.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var rnd = new Random();
            var idx = Enumerable.Range(0, w.Length).OrderBy(x => rnd.Next()).Take(3).OrderBy(x => x).ToArray();
            WrapPanel bp;
            var sp = Dialog(L.T("create.verify"), L.T("create.verify_text"), out bp);
            var boxes = new List<TextBox>();
            var row = new UniformGrid { Columns = 3, Margin = new Thickness(0, 14, 0, 0) };
            foreach (var i in idx)
            {
                var tb = U.Field("", L.F("create.word_n", i + 1));
                boxes.Add(tb);
                var l = U.Labeled(L.F("create.word_n", i + 1), tb);
                l.Margin = new Thickness(0, 0, 10, 0);
                row.Children.Add(l);
            }
            sp.Children.Add(row);
            var err = U.T("", 12, "Bad"); err.Margin = new Thickness(0, 10, 0, 0);
            sp.Children.Add(err);
            var back = U.Btn(L.T("b.back"), "Secondary", (a, e) => ShowMnemonicStep(words));
            var go = U.Btn(L.T("b.continue"), "Primary", (a, e) =>
            {
                for (int k = 0; k < 3; k++)
                    if (!string.Equals(boxes[k].Text.Trim(), w[idx[k]], StringComparison.OrdinalIgnoreCase)) { err.Text = L.T("create.verify_bad"); return; }
                ShowPasswordStep(L.T("create.title"), null, (name, pw, ts) =>
                    DoOpen(new OpenRequest { Kind = OpenKind.Create, File = name, Password = pw, Mnemonic = words }));
            });
            go.IsDefault = true;
            back.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(back); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp, true, 640);
        }

        // Wallet name + password twice (+ optional extra UI above). Calls done(fullPath, password, unused).
        void ShowPasswordStep(string title, UIElement extra, Action<string, string, long> done, Func<string> validateExtra = null, Func<long> creationTs = null)
        {
            WrapPanel bp;
            var sp = Dialog(title, L.T("pw.text"), out bp);
            if (extra != null) sp.Children.Add(extra);
            var name = U.Field(NextWalletName(), L.T("f.name"));
            name.FontFamily = FontFamily;
            var nl = U.Labeled(L.T("f.name"), name, L.F("f.name_hint", WalletSettings.WalletsDir));
            nl.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(nl);
            var p1 = U.Pass(L.T("f.password"));
            var p2 = U.Pass(L.T("f.password2"));
            var pl = U.Columns("1*", U.Labeled(L.T("f.password"), p1), 12, null, "1*", U.Labeled(L.T("f.password2"), p2));
            pl.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(pl);
            sp.Children.Add(WarnBox(L.T("pw.warn")));
            var err = U.T("", 12, "Bad"); err.Margin = new Thickness(0, 10, 0, 0);
            sp.Children.Add(err);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var go = U.Btn(L.T("b.create"), "Primary", (a, e) =>
            {
                if (validateExtra != null) { var ve = validateExtra(); if (ve != null) { err.Text = ve; return; } }
                var n = name.Text.Trim();
                if (n.Length == 0 || n.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { err.Text = L.T("pw.bad_name"); return; }
                var file = Path.Combine(WalletSettings.WalletsDir, n.EndsWith(".wallet", StringComparison.OrdinalIgnoreCase) ? n : n + ".wallet");
                if (File.Exists(file)) { err.Text = L.T("err.209"); return; }
                if (p1.Password.Length < 8) { err.Text = L.T("pw.short"); return; }
                if (p1.Password != p2.Password) { err.Text = L.T("pw.mismatch"); return; }
                var pw = p1.Password;
                p1.Clear(); p2.Clear();
                done(file, pw, creationTs != null ? creationTs() : 0);
            });
            go.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp, true, 600);
        }

        static string NextWalletName()
        {
            for (int i = 1; i < 1000; i++)
            {
                var n = i == 1 ? "BCN" : "BCN-" + i;
                if (!File.Exists(Path.Combine(WalletSettings.WalletsDir, n + ".wallet"))) return n;
            }
            return "BCN-" + DateTime.Now.ToString("yyyyMMddHHmm");
        }

        // ------------------------------------------------------------------ restore / import

        void RestoreFlow()
        {
            var extra = new StackPanel();
            var words = new TextBox { Style = U.St("Field"), Height = 92, TextWrapping = TextWrapping.Wrap, AcceptsReturn = false, VerticalContentAlignment = VerticalAlignment.Top, Padding = new Thickness(10, 8, 10, 8) };
            AutomationProperties.SetName(words, L.T("restore.words"));
            var wl = U.Labeled(L.T("restore.words"), words, L.T("restore.words_hint"));
            wl.Margin = new Thickness(0, 14, 0, 0);
            extra.Children.Add(wl);
            var date = U.Field("", L.T("restore.date"));
            var dl = U.Labeled(L.T("restore.date"), date, L.T("restore.date_hint"));
            dl.Margin = new Thickness(0, 12, 0, 0);
            extra.Children.Add(dl);
            Func<string> norm = () => Regex.Replace(words.Text.Trim().ToLowerInvariant(), @"\s+", " ");
            Func<long> ts = () =>
            {
                DateTime d;
                if (DateTime.TryParseExact(date.Text.Trim(), new[] { "yyyy-MM-dd", "dd.MM.yyyy", "yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out d))
                    return Math.Max(0, new DateTimeOffset(d.AddDays(-2)).ToUnixTimeSeconds());
                return 0;
            };
            ShowPasswordStep(L.T("start.restore"), extra, (file, pw, t) =>
            {
                var m = norm();
                ShowProgress(L.T("p.creating"), L.T("restore.checking"));
                bool ok = false;
                Run(() => ok = WalletService.CheckMnemonic(m), () =>
                {
                    if (!ok) { CloseModal(); Error(L.T("err.213")); return; }
                    DoOpen(new OpenRequest { Kind = OpenKind.Restore, File = file, Password = pw, Mnemonic = m, CreationTs = t });
                }, ex => { CloseModal(); Error(Msg(ex)); });
            }, () =>
            {
                int n = norm().Split(' ').Length;
                if (n != 12 && n != 15 && n != 18 && n != 21 && n != 24) return L.T("restore.bad_count");
                if (date.Text.Trim().Length > 0 && ts() == 0) return L.T("restore.bad_date");
                return null;
            }, ts);
        }

        void HardwareFlow()
        {
            var extra = WarnBox(L.T("hw.text"));
            ShowPasswordStep(L.T("start.hw"), extra, (file, pw, t) =>
                DoOpen(new OpenRequest { Kind = OpenKind.Hardware, File = file, Password = pw }));
        }

        void LegacyFlow()
        {
            var extra = WarnBox(L.T("legacy.text"));
            ShowPasswordStep(L.T("start.legacy"), extra, (file, pw, t) =>
                DoOpen(new OpenRequest { Kind = OpenKind.Legacy, File = file, Password = pw }));
        }

        void ImportFlow()
        {
            var extra = new StackPanel();
            var keys = new TextBox { Style = U.St("Field"), Height = 92, TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Top, Padding = new Thickness(10, 8, 10, 8) };
            AutomationProperties.SetName(keys, L.T("import.keys"));
            var kl = U.Labeled(L.T("import.keys"), keys, L.T("import.keys_hint"));
            kl.Margin = new Thickness(0, 14, 0, 0);
            extra.Children.Add(kl);
            Func<string> norm = () => Regex.Replace(keys.Text, @"\s+", "");
            ShowPasswordStep(L.T("start.import"), extra, (file, pw, t) =>
                DoOpen(new OpenRequest { Kind = OpenKind.ImportKeys, File = file, Password = pw, Keys = norm() }),
                () => Regex.IsMatch(norm(), "^[0-9a-fA-F]{256}$") ? null : L.T("import.bad"));
        }
    }
}
