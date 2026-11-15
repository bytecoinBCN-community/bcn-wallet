using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Path = System.IO.Path;

namespace BcnWallet
{
    public partial class MainWindow
    {
        // ---------------------------------------------------------------- Address book
        StackPanel contactList;
        TextBlock contactEmpty;

        FrameworkElement BuildContacts()
        {
            var sp = Page();
            var add = U.Btn(L.T("c.add"), "Primary", (a, b) => EditContact(new Contact { Name = "", Address = "", PaymentId = "" }, true), U.IcPlus);
            sp.Children.Add(U.Columns("1*", U.H1(L.T("nav.contacts")), "auto", add));
            var c = new StackPanel();
            contactList = new StackPanel();
            c.Children.Add(contactList);
            contactEmpty = U.T(L.T("c.book_empty"), 13, "Muted");
            c.Children.Add(contactEmpty);
            var card = U.Card(c);
            card.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(card);
            var note = U.T(L.F("c.book_note", WalletSettings.Dir), 12, "Muted");
            note.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(note);
            RebuildContacts();
            return U.Scroll(sp);
        }

        void RebuildContacts()
        {
            if (contactList == null) return;
            contactList.Children.Clear();
            foreach (var c in S.Contacts.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var cc = c;
                var addr = U.T(c.Address, 12, "Muted"); addr.FontFamily = U.Mono; addr.TextTrimming = TextTrimming.CharacterEllipsis; addr.TextWrapping = TextWrapping.NoWrap;
                var info = U.V(3, U.T(c.Name, 14, "Text", true), addr);
                if (!string.IsNullOrEmpty(c.PaymentId))
                {
                    var p = U.T("Payment ID " + c.PaymentId, 11, "Muted"); p.FontFamily = U.Mono; p.TextTrimming = TextTrimming.CharacterEllipsis; p.TextWrapping = TextWrapping.NoWrap;
                    info.Children.Add(p);
                }
                var send = U.Btn(L.T("nav.send"), "Small", (a, b) => { Go(1); sendAddr.Text = cc.Address; sendPid.Text = cc.PaymentId ?? ""; sendAmount.Focus(); }, U.IcSend);
                var edit = U.Btn(L.T("c.edit"), "Small", (a, b) => EditContact(cc, false));
                var copy = new Button { Style = U.St("Small"), Width = 32, Padding = new Thickness(0), Content = U.Icon(U.IcCopy, 16), ToolTip = L.T("b.copy_addr") };
                AutomationProperties.SetName(copy, L.T("b.copy_addr"));
                copy.Click += (a, b) => { Copy(cc.Address); Toast(L.T("toast.copied")); };
                send.Margin = new Thickness(10, 0, 0, 0); edit.Margin = new Thickness(8, 0, 0, 0); copy.Margin = new Thickness(8, 0, 0, 0);
                var row = U.Columns("1*", info, "auto", copy, "auto", edit, "auto", send);
                var b2 = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 10, 0, 10), Child = row };
                b2.SetResourceReference(Border.BorderBrushProperty, "Grid");
                contactList.Children.Add(b2);
            }
            contactEmpty.Visibility = S.Contacts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        void EditContact(Contact c, bool isNew)
        {
            WrapPanel bp;
            var sp = Dialog(isNew ? L.T("c.add") : L.T("c.edit"), null, out bp);
            var name = U.Field(c.Name, L.T("c.name")); name.FontFamily = FontFamily;
            var addr = U.Field(c.Address, L.T("s.to"));
            var pid = U.Field(c.PaymentId ?? "", L.T("s.pid"));
            var l1 = U.Labeled(L.T("c.name"), name); l1.Margin = new Thickness(0, 14, 0, 0);
            var l2 = U.Labeled(L.T("s.to"), addr); l2.Margin = new Thickness(0, 12, 0, 0);
            var l3 = U.Labeled(L.T("s.pid"), pid, L.T("s.pid_hint")); l3.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(l1); sp.Children.Add(l2); sp.Children.Add(l3);
            var err = U.T("", 12, "Bad"); err.Margin = new Thickness(0, 10, 0, 0);
            sp.Children.Add(err);
            if (!isNew)
            {
                var del = U.Btn(L.T("c.delete"), "Secondary", (a, e) => { S.Contacts.Remove(c); S.Save(); CloseModal(); RebuildContacts(); }, U.IcTrash);
                del.Margin = new Thickness(0, 0, 10, 0);
                bp.Children.Add(del);
            }
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var ok = U.Btn(L.T("b.save"), "Primary", (a, e) =>
            {
                var n = name.Text.Trim(); var ad = addr.Text.Trim(); var p = pid.Text.Trim();
                if (n.Length == 0) { err.Text = L.T("c.need_name"); return; }
                if (!RxAddress.IsMatch(ad)) { err.Text = L.T("s.bad_addr"); return; }
                if (p.Length > 0 && !RxPid.IsMatch(p)) { err.Text = L.T("s.bad_pid"); return; }
                c.Name = n; c.Address = ad; c.PaymentId = p;
                if (isNew) S.Contacts.Add(c);
                S.Save();
                CloseModal();
                RebuildContacts();
                Toast(L.T("toast.saved"));
            });
            ok.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(ok);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        // ---------------------------------------------------------------- Community
        FrameworkElement BuildCommunity()
        {
            var sp = Page();
            sp.Children.Add(U.H1(L.T("nav.community")));
            var lead = U.T(L.T("c.lead"), 13, "Muted");
            lead.Margin = new Thickness(0, 8, 0, 0);
            sp.Children.Add(lead);

            // miner card: mine straight into this wallet
            var miner = new StackPanel();
            miner.Children.Add(U.H2(L.T("c.miner_title")));
            var mt = U.T(L.T("c.miner_text"), 13, "Muted");
            mt.Margin = new Thickness(0, 8, 0, 0);
            miner.Children.Add(mt);
            var mb = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
            var exe = FindMiner();
            if (exe != null)
            {
                var run = U.Btn(L.T("c.miner_run"), "Primary", (a, b) => { Copy(W.Info.FirstAddress); try { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); } catch { } Toast(L.T("c.miner_copied")); }, U.IcPlay);
                run.Margin = new Thickness(0, 0, 10, 8);
                mb.Children.Add(run);
            }
            else
            {
                var dl = U.Btn(L.T("c.miner_get"), "Primary", (a, b) => U.OpenUrl(App.MinerUrl), U.IcDownload);
                dl.Margin = new Thickness(0, 0, 10, 8);
                mb.Children.Add(dl);
            }
            var ca = U.Btn(L.T("c.miner_copy"), "Secondary", (a, b) => { Copy(W.Info.FirstAddress); Toast(L.T("c.miner_copied")); }, U.IcCopy);
            ca.Margin = new Thickness(0, 0, 10, 8);
            mb.Children.Add(ca);
            var pool = U.Btn("pool.bytecoin.community", "Secondary", (a, b) => U.OpenUrl(App.PoolUrl), U.IcServer);
            pool.Margin = new Thickness(0, 0, 10, 8);
            mb.Children.Add(pool);
            miner.Children.Add(mb);
            var minerCard = U.Card(miner);
            minerCard.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(minerCard);

            var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 14, 0, 0) };
            var items = new[] {
                new[] { U.IcGlobe, L.T("c.site"), L.T("c.site_d"), App.SiteLang },
                new[] { U.IcHistory, L.T("c.explorer"), L.T("c.explorer_d"), App.ExplorerUrl },
                new[] { U.IcLock, L.T("c.web"), L.T("c.web_d"), App.WebWalletUrl },
                new[] { U.IcServer, L.T("c.node"), L.T("c.node_d"), "https://node.bytecoin.community/" },
                new[] { U.IcChat, "Telegram", L.T("c.tg_d"), App.TelegramUrl },
                new[] { U.IcCode, "GitHub", L.T("c.gh_d"), App.GithubUrl },
                new[] { U.IcCode, L.T("c.this"), L.T("c.this_d"), App.RepoUrl },
                new[] { U.IcShield, L.T("c.bcndev"), L.T("c.bcndev_d"), App.BcndevUrl } };
            foreach (var it in items)
            {
                var url = it[3];
                grid.Children.Add(Tile(it[0], it[1], it[2] + "\n" + url.Replace("https://", "").TrimEnd('/'), false, () => U.OpenUrl(url)));
            }
            sp.Children.Add(grid);
            var disc = U.T(L.T("c.disclaimer"), 11, "Muted");
            disc.Margin = new Thickness(0, 4, 0, 0);
            sp.Children.Add(disc);
            return U.Scroll(sp);
        }

        static string FindMiner()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var cands = new List<string> {
                Path.Combine(baseDir, "BCNMiner.exe"),
                Path.Combine(baseDir, "..", "BCNMiner", "BCNMiner.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "BCN Miner", "BCNMiner.exe") };
            foreach (var c in cands) { try { var f = Path.GetFullPath(c); if (File.Exists(f)) return f; } catch { } }
            return null;
        }

        // ---------------------------------------------------------------- Settings
        RadioButton[] themeBtns, langBtns, nodeBtns;
        TextBox remoteBox, chainBox, logBox;
        TextBlock nodeApplyNote;

        FrameworkElement BuildSettings()
        {
            var sp = Page();
            sp.Children.Add(U.H1(L.T("nav.settings")));

            // look
            var look = new StackPanel();
            look.Children.Add(U.H2(L.T("set.look")));
            var ll = U.T(L.T("set.language"), 12, "Muted"); ll.Margin = new Thickness(0, 12, 0, 5);
            look.Children.Add(ll);
            var langLabels = new List<string> { L.T("set.system") };
            var langValues = new List<string> { "system" };
            for (int i = 0; i < L.Codes.Length; i++) { langLabels.Add(L.Names[i]); langValues.Add(L.Codes[i]); }
            look.Children.Add(U.Segmented("lang", langLabels.ToArray(), langValues.ToArray(), S.Language,
                v => { if (v != S.Language) Dispatcher.BeginInvoke(new Action(() => ApplyLanguage(v))); }, out langBtns, 3));
            var tl = U.T(L.T("set.theme"), 12, "Muted"); tl.Margin = new Thickness(0, 18, 0, 5);
            look.Children.Add(tl);
            look.Children.Add(U.Segmented("theme", new[] { L.T("set.system"), L.T("set.dark"), L.T("set.light") }, new[] { "system", "dark", "light" }, S.Theme,
                v => { S.Theme = v; Theme.Apply(v); S.Save(); }, out themeBtns));
            var run = new StackPanel();
            run.Children.Add(U.H2(L.T("set.behaviour")));
            var c1 = U.Switch(L.T("set.tray"), L.T("set.tray_hint"), S.MinimizeToTray, v => { S.MinimizeToTray = v; S.Save(); });
            var c2 = U.Switch(L.T("set.autorun"), L.T("set.autorun_hint"), S.StartWithWindows, v => { S.StartWithWindows = v; SetAutorun(v); S.Save(); });
            var c3 = U.Switch(L.T("set.hide"), null, S.HideBalance, v => { S.HideBalance = v; S.Save(); if (snap != null) UpdateOverview(snap); });
            foreach (var c in new[] { c1, c2, c3 }) { c.Margin = new Thickness(0, 14, 0, 0); run.Children.Add(c); }
            var row1 = U.Columns("1*", U.Card(look), 14, null, "1*", U.Card(run));
            row1.Margin = new Thickness(0, 18, 0, 0);
            sp.Children.Add(row1);

            // node
            var node = new StackPanel();
            node.Children.Add(U.H2(L.T("set.node")));
            var seg = U.Segmented("nodemode", new[] { L.T("mode.remote_s"), L.T("mode.local_s") }, new[] { "remote", "local" }, S.NodeMode, v => { }, out nodeBtns);
            seg.Margin = new Thickness(0, 12, 0, 0);
            node.Children.Add(seg);
            var nd = U.T(L.T("set.node_d"), 12, "Muted"); nd.Margin = new Thickness(0, 8, 0, 0);
            node.Children.Add(nd);
            // remote nodes: automatic, ours + official, or added by hand
            var nh = U.Columns("1*", U.T(L.T("set.remote"), 12, "Muted"), "auto", U.Btn(L.T("nodes.check"), "Small", (a, b) => SurveyNodes(), U.IcRefresh));
            nh.Margin = new Thickness(0, 14, 0, 4);
            node.Children.Add(nh);
            nodePick = S.NodeChoice;
            nodeList = new StackPanel();
            node.Children.Add(nodeList);
            remoteBox = U.Field("", L.T("nodes.add"));
            var addBtn = U.Btn(L.T("nodes.add_btn"), "Small", (a, b) => AddCustomNode(), U.IcPlus);
            addBtn.Margin = new Thickness(10, 0, 0, 0);
            var rl = U.Labeled(L.T("nodes.add"), U.Columns("1*", remoteBox, "auto", addBtn), L.T("set.remote_hint"));
            rl.Margin = new Thickness(0, 10, 0, 0);
            node.Children.Add(rl);
            RebuildNodeList();
            if (W.LastSurvey != null) foreach (var s in W.LastSurvey) nodeStatus[s.Url] = s;
            RebuildNodeList();
            chainBox = U.Field(S.NodeMode == "local" ? S.EffectiveBlockchainDir : SuggestedChainDir(), L.T("set.chain"));
            var browse = U.Btn(L.T("set.browse"), "Small", (a, b) =>
            {
                using (var d = new System.Windows.Forms.FolderBrowserDialog { Description = L.T("set.chain"), SelectedPath = chainBox.Text })
                    if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) chainBox.Text = d.SelectedPath;
            }, U.IcFolder);
            browse.Margin = new Thickness(10, 0, 0, 0);
            var cl = U.Labeled(L.T("set.chain"), U.Columns("1*", chainBox, "auto", browse), L.T("set.chain_hint"));
            cl.Margin = new Thickness(0, 14, 0, 0);
            node.Children.Add(cl);
            var verify = U.Btn(L.T("chain.verify"), "Small", (a, b) => VerifyChainFlow(), U.IcShield);
            verify.HorizontalAlignment = HorizontalAlignment.Left;
            verify.Margin = new Thickness(0, 10, 0, 0);
            verify.ToolTip = L.T("chain.verify_hint");
            verify.IsEnabled = S.NodeMode == "local" && W.IsOpen;
            node.Children.Add(verify);
            nodeApplyNote = U.T("", 12, "Muted"); nodeApplyNote.Margin = new Thickness(0, 12, 0, 0);
            node.Children.Add(nodeApplyNote);
            var apply = U.Btn(L.T("set.apply"), "Primary", (a, b) => ApplyNode());
            apply.HorizontalAlignment = HorizontalAlignment.Left;
            apply.Margin = new Thickness(0, 12, 0, 0);
            node.Children.Add(apply);
            var bin = U.T(L.F("set.binaries", NodeFiles.Version, NodeFiles.Dir), 11, "Muted");
            bin.Margin = new Thickness(0, 12, 0, 0);
            node.Children.Add(bin);
            var nodeCard = U.Card(node);

            // wallet
            var wal = new StackPanel();
            wal.Children.Add(U.H2(L.T("set.wallet")));
            var wf = U.T(W.File ?? "", 12, "Muted"); wf.FontFamily = U.Mono; wf.Margin = new Thickness(0, 8, 0, 0);
            wal.Children.Add(wf);
            var wb = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            var b1 = U.Btn(W.Info != null && W.Info.Type == "legacy" ? L.T("set.show_keys") : L.T("set.show_words"), "Secondary", (a, b) => ShowSecretFlow(), U.IcKey);
            var b2 = U.Btn(L.T("set.change_pw"), "Secondary", (a, b) => ChangePasswordFlow(), U.IcLock);
            var b3 = U.Btn(L.T("set.view_only"), "Secondary", (a, b) => ExportViewOnlyFlow(), U.IcEye);
            var b4 = U.Btn(L.T("set.open_folder"), "Secondary", (a, b) => OpenFolder(Path.GetDirectoryName(W.File)), U.IcFolder);
            var b5 = U.Btn(L.T("nav.lock"), "Secondary", (a, b) => LockWallet(), U.IcLock);
            if (W.Info != null && W.Info.ViewOnly) { b1.IsEnabled = false; b3.IsEnabled = false; }
            foreach (var x in new[] { b1, b2, b3, b4, b5 }) { x.Margin = new Thickness(0, 8, 0, 0); x.HorizontalContentAlignment = HorizontalAlignment.Left; wb.Children.Add(x); }
            wal.Children.Add(wb);
            var walCard = U.Card(wal);
            var row2 = U.Columns("1.4*", nodeCard, 14, null, "1*", walCard);
            row2.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(row2);

            // about + log
            var about = new StackPanel();
            about.Children.Add(U.H2(L.T("set.about")));
            var at = U.T(L.F("set.about_text", App.Version, NodeFiles.Version, WalletSettings.Dir) + "\n" + (WalletSettings.Portable ? L.T("set.portable_on") : L.T("set.portable_off")), 12, "Muted");
            at.Margin = new Thickness(0, 8, 0, 0);
            about.Children.Add(at);
            var ab = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            var o1 = U.Btn(L.T("set.open_data"), "Small", (a, b) => OpenFolder(WalletSettings.Dir), U.IcFolder);
            var w1 = U.Btn("bytecoin.community", "Small", (a, b) => U.OpenUrl(App.SiteLang), U.IcGlobe);
            var w3 = U.Btn("GitHub", "Small", (a, b) => U.OpenUrl(App.RepoUrl), U.IcCode);
            foreach (var x in new[] { w1, w3, o1 }) { x.Margin = new Thickness(0, 6, 10, 0); ab.Children.Add(x); }
            about.Children.Add(ab);
            var lh = U.Columns("1*", U.H2(L.T("set.log")), "auto", U.Btn(L.T("set.refresh"), "Small", (a, b) => RefreshSettings(), U.IcRefresh));
            lh.Margin = new Thickness(0, 20, 0, 0);
            about.Children.Add(lh);
            logBox = new TextBox { Style = U.St("Area"), Height = 220, Margin = new Thickness(0, 10, 0, 0), FontSize = 11 };
            about.Children.Add(logBox);
            var aboutCard = U.Card(about);
            aboutCard.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(aboutCard);
            return U.Scroll(sp);
        }

        void RefreshSettings()
        {
            if (logBox == null) return;
            var lines = new List<string>();
            var nt = W.NodeTail;
            if (nt.Count > 0) { lines.Add("── bytecoind ──"); lines.AddRange(nt.Skip(Math.Max(0, nt.Count - 80))); lines.Add(""); }
            var wt = W.WalletdTail;
            lines.Add("── walletd ──");
            lines.AddRange(wt.Skip(Math.Max(0, wt.Count - 120)));
            logBox.Text = string.Join("\n", lines);
            logBox.ScrollToEnd();
        }

        string nodePick = "auto";
        StackPanel nodeList;
        readonly Dictionary<string, NodeStatus> nodeStatus = new Dictionary<string, NodeStatus>(StringComparer.OrdinalIgnoreCase);

        void RebuildNodeList()
        {
            if (nodeList == null) return;
            nodeList.Children.Clear();
            nodeList.Children.Add(NodeRow("auto", L.T("nodes.auto"), L.T("nodes.auto_d"), null, false));
            foreach (var k in Nodes.Known) nodeList.Children.Add(NodeRow(k.Url, k.Name, L.T(k.Operator) + " · " + k.Url, Status(k.Url), false));
            foreach (var c in S.CustomNodes) nodeList.Children.Add(NodeRow(c, c.Replace("https://", "").Replace("http://", ""), L.T("nodes.op_custom") + " · " + c, Status(c), true));
        }

        NodeStatus Status(string url) { NodeStatus s; return nodeStatus.TryGetValue(url, out s) ? s : null; }

        UIElement NodeRow(string value, string title, string sub, NodeStatus st, bool removable)
        {
            bool sel = string.Equals(nodePick, value, StringComparison.OrdinalIgnoreCase);
            string dot = st == null ? "Muted" : st.Verdict == "ok" ? "GoodDot" : st.Verdict == "behind" ? "Warn" : "Bad";
            string text = st == null ? (value == "auto" ? "" : L.T("nodes.unchecked"))
                : st.Verdict == "ok" ? L.F("nodes.ok", st.Ms, Num(st.Height))
                : st.Verdict == "behind" ? L.F("nodes.behind", Num(st.Height))
                : st.Verdict == "fork" ? L.T("nodes.fork") : L.T("nodes.down");
            var t = U.T(title, 14, "Text", true);
            var s2 = U.T(sub, 11, "Muted"); s2.TextTrimming = TextTrimming.CharacterEllipsis; s2.TextWrapping = TextWrapping.NoWrap;
            var stt = U.T(text, 12, st == null ? "Muted" : st.Verdict == "ok" ? "Good" : st.Verdict == "behind" ? "Warn" : "Bad");
            stt.VerticalAlignment = VerticalAlignment.Center; stt.Margin = new Thickness(10, 0, 0, 0);
            if (st != null && st.Error.Length > 0 && st.Verdict == "down") stt.ToolTip = st.Error;
            var cols = U.Columns("auto", Wrap(U.Dot(dot, 9), 10), "1*", U.V(2, t, s2), "auto", stt);
            if (removable)
            {
                var del = new Button { Style = U.St("Cap"), Width = 30, Height = 28, Content = U.Icon(U.IcTrash, 14), ToolTip = L.T("c.delete") };
                AutomationProperties.SetName(del, L.T("c.delete") + " " + title);
                del.Click += (a, e) =>
                {
                    S.CustomNodes.RemoveAll(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
                    if (string.Equals(nodePick, value, StringComparison.OrdinalIgnoreCase)) nodePick = "auto";
                    S.Save(); RebuildNodeList();
                    e.Handled = true;
                };
                cols.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetColumn(del, 3); cols.Children.Add(del);
            }
            var b = new Button { Style = U.St("Row"), Content = cols, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 4, 0, 0), BorderThickness = new Thickness(1) };
            var frame = new Border { CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Child = b, Margin = new Thickness(0, 4, 0, 0) };
            frame.SetResourceReference(Border.BorderBrushProperty, sel ? "Accent" : "Line");
            if (sel) frame.SetResourceReference(Border.BackgroundProperty, "NavActive");
            b.Margin = new Thickness(0);
            AutomationProperties.SetName(b, title);
            b.Click += (a, e) => { nodePick = value; nodeBtns[0].IsChecked = true; RebuildNodeList(); };
            return frame;
        }

        void SurveyNodes()
        {
            nodeApplyNote.Text = L.T("nodes.checking");
            List<NodeStatus> res = null;
            Run(() => res = Nodes.Survey(S.AllNodes()), () =>
            {
                foreach (var s in res) nodeStatus[s.Url] = s;
                nodeApplyNote.Text = L.F("nodes.checked", res.Count(x => x.Verdict == "ok"), res.Count);
                RebuildNodeList();
            }, ex => nodeApplyNote.Text = Msg(ex));
        }

        void AddCustomNode()
        {
            var u = Nodes.Normalize(remoteBox.Text);
            if (u == null) { nodeApplyNote.Text = L.T("nodes.bad"); return; }
            if (!S.AllNodes().Contains(u, StringComparer.OrdinalIgnoreCase)) S.CustomNodes.Add(u);
            S.Save();
            remoteBox.Text = "";
            nodePick = u;
            nodeBtns[0].IsChecked = true;
            RebuildNodeList();
            nodeApplyNote.Text = L.T("nodes.checking");
            List<NodeStatus> res = null;
            Run(() => res = Nodes.Survey(S.AllNodes()), () =>
            {
                foreach (var s in res) nodeStatus[s.Url] = s;
                var me = res.FirstOrDefault(x => x.Url == u);
                nodeApplyNote.Text = me != null && me.Verdict == "ok" ? L.T("nodes.added_ok") : L.T("nodes.added_bad");
                RebuildNodeList();
            }, ex => nodeApplyNote.Text = Msg(ex));
        }

        // Compare the local blockchain with the public nodes at heights spread over the whole chain.
        void VerifyChainFlow()
        {
            if (S.NodeMode != "local" || W.NodeAddress == null) return;
            ShowProgress(L.T("chain.verify"), L.T("chain.verifying"));
            Nodes.ChainReport rep = null;
            var local = Nodes.Normalize(W.NodeAddress);
            Run(() => rep = Nodes.CheckLocalChain(local, S.AllNodes()), () =>
            {
                CloseModal();
                string text;
                if (rep.Sources.Count == 0 || rep.Compared == 0) text = L.T("chain.no_refs");
                else if (rep.Mismatch.Count == 0) text = L.F("chain.ok", rep.Matched, Num(rep.LocalHeight), string.Join(", ", rep.Sources.Select(x => x.Replace("https://", "").Replace("http://", ""))));
                else text = L.F("chain.bad", string.Join(", ", rep.Mismatch), rep.Matched, rep.Compared);
                Info(L.T("chain.verify"), text);
            }, ex => { CloseModal(); Error(Msg(ex)); });
        }

        void ApplyNode()
        {
            string mode = nodeBtns[1].IsChecked == true ? "local" : "remote";
            var remote = nodePick;
            var chain = chainBox.Text.Trim();
            var defChain = Path.Combine(WalletSettings.Dir, "blockchain");
            bool changed = mode != S.NodeMode || (mode == "remote" && remote != S.NodeChoice) || (mode == "local" && !string.Equals(chain, S.EffectiveBlockchainDir, StringComparison.OrdinalIgnoreCase));
            if (!changed) { nodeApplyNote.Text = L.T("set.no_change"); return; }
            Action apply = () =>
            {
                if (S.NodeMode == "local" && (mode != "local" || !string.Equals(chain, S.EffectiveBlockchainDir, StringComparison.OrdinalIgnoreCase)))
                    W.StopNode();
                S.NodeMode = mode; S.NodeChoice = remote;
                W.Avoid.Clear();
                S.BlockchainDir = string.Equals(chain, defChain, StringComparison.OrdinalIgnoreCase) ? "" : chain;
                S.Save();
                Reconnect();
            };
            if (mode == "local") ConfirmLocalMode(chain, apply, () => { });
            else apply();
        }

        void ShowSecretFlow()
        {
            AskPassword(L.T("set.show_words"), L.T("set.secret_pw"), pw =>
            {
                if (pw != W.Password) { Error(L.T("err.207")); return; }
                ShowProgress(L.T("set.show_words"), L.T("set.secret_loading"));
                string secret = null;
                Run(() => secret = W.ExportSecret(pw), () =>
                {
                    CloseModal();
                    WrapPanel bp;
                    bool words = secret.Trim().Split(' ').Length >= 12;
                    var sp = Dialog(words ? L.T("set.show_words") : L.T("set.show_keys"), L.T("set.secret_text"), out bp);
                    if (words) sp.Children.Add(WordsGrid(secret));
                    else
                    {
                        var t = new TextBox { Text = secret, IsReadOnly = true, Style = U.St("Area"), Height = 110, Margin = new Thickness(0, 14, 0, 0) };
                        sp.Children.Add(t);
                    }
                    var copy = U.Btn(words ? L.T("b.copy_all") : L.T("b.copy"), "Small", (a, e) => CopySecret(secret.Trim()), U.IcCopy);
                    copy.HorizontalAlignment = HorizontalAlignment.Left;
                    copy.Margin = new Thickness(0, 12, 0, 0);
                    copy.ToolTip = L.T("create.copy_warn");
                    sp.Children.Add(copy);
                    sp.Children.Add(WarnBox(L.T("create.warn") + "\n" + L.T("create.copy_warn")));
                    var ok = U.Btn(L.T("b.close"), "Primary", (a, e) => CloseModal());
                    ok.IsDefault = true;
                    bp.Children.Add(ok);
                    sp.Children.Add(bp);
                    ShowModal(sp, true, 640);
                }, ex => { CloseModal(); Error(Msg(ex)); });
            });
        }

        void ChangePasswordFlow()
        {
            WrapPanel bp;
            var sp = Dialog(L.T("set.change_pw"), L.T("set.change_pw_text"), out bp);
            var p0 = U.Pass(L.T("f.password_old"));
            var p1 = U.Pass(L.T("f.password_new"));
            var p2 = U.Pass(L.T("f.password2"));
            var l0 = U.Labeled(L.T("f.password_old"), p0); l0.Margin = new Thickness(0, 14, 0, 0);
            var l12 = U.Columns("1*", U.Labeled(L.T("f.password_new"), p1), 12, null, "1*", U.Labeled(L.T("f.password2"), p2));
            l12.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(l0); sp.Children.Add(l12);
            var err = U.T("", 12, "Bad"); err.Margin = new Thickness(0, 10, 0, 0);
            sp.Children.Add(err);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var ok = U.Btn(L.T("b.save"), "Primary", (a, e) =>
            {
                if (p0.Password != W.Password) { err.Text = L.T("err.207"); return; }
                if (p1.Password.Length < 8) { err.Text = L.T("pw.short"); return; }
                if (p1.Password != p2.Password) { err.Text = L.T("pw.mismatch"); return; }
                var o = p0.Password; var n = p1.Password;
                p0.Clear(); p1.Clear(); p2.Clear();
                CloseModal();
                ShowProgress(L.T("set.change_pw"), L.T("set.change_pw_wait"));
                Run(() => W.ChangePassword(o, n), () => { CloseModal(); Info(L.T("set.change_pw"), L.T("set.change_pw_done")); }, ex => { CloseModal(); Error(Msg(ex)); });
            });
            ok.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(ok);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        void ExportViewOnlyFlow()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = L.T("set.view_only"),
                Filter = L.T("f.wallet_filter") + " (*.wallet)|*.wallet",
                FileName = Path.GetFileNameWithoutExtension(W.File) + "-view-only.wallet",
                InitialDirectory = Path.GetDirectoryName(W.File)
            };
            if (dlg.ShowDialog(this) != true) return;
            var target = dlg.FileName;
            if (File.Exists(target)) { Error(L.T("err.209")); return; }
            AskPassword(L.T("set.view_only"), L.T("set.view_only_text"), pw =>
            {
                if (pw != W.Password) { Error(L.T("err.207")); return; }
                ShowProgress(L.T("set.view_only"), L.T("set.secret_loading"));
                Run(() => W.ExportViewOnly(pw, target), () => { CloseModal(); Info(L.T("set.view_only"), L.F("set.view_only_done", target)); }, ex => { CloseModal(); Error(Msg(ex)); });
            });
        }
    }
}
