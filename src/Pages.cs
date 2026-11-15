using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace BcnWallet
{
    // Progress bar from the design: 6 px rounded track with a coloured fill.
    public class Bar : Grid
    {
        readonly Border fill;
        double frac;
        public Bar(string fillBrush)
        {
            Height = 6;
            var track = new Border { CornerRadius = new CornerRadius(3) };
            track.SetResourceReference(Border.BackgroundProperty, "Track");
            fill = new Border { CornerRadius = new CornerRadius(3), HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
            fill.SetResourceReference(Border.BackgroundProperty, fillBrush);
            Children.Add(track); Children.Add(fill);
            SizeChanged += (a, b) => Apply();
        }
        public void Set(double f, string brush = null)
        {
            frac = Math.Max(0, Math.Min(1, f));
            if (brush != null) fill.SetResourceReference(Border.BackgroundProperty, brush);
            Apply();
        }
        void Apply() { fill.Width = ActualWidth * frac; }
    }

    public partial class MainWindow
    {
        // ---------------------------------------------------------------- Overview
        TextBlock balVal, balLocked, ovAddr, syncPct, syncText, nodeSyncText, ovType;
        Bar syncBar, nodeBar;
        StackPanel nodeSyncBox, recentList;
        TextBlock recentEmpty;

        FrameworkElement BuildOverview()
        {
            var sp = Page();
            var recv = U.Btn(L.T("nav.receive"), "Secondary", (a, b) => Go(2), U.IcReceive);
            var send = U.Btn(L.T("nav.send"), "Primary", (a, b) => Go(1), U.IcSend);
            recv.Margin = new Thickness(12, 0, 0, 0); send.Margin = new Thickness(12, 0, 0, 0);
            if (W.Info != null && W.Info.ViewOnly) send.IsEnabled = false;
            sp.Children.Add(U.Columns("1*", U.H1(L.T("nav.overview")), "auto", recv, "auto", send));

            // balance hero card (navy in both themes)
            var hero = new StackPanel();
            var hl = U.T(L.T("ov.available"), 13, "HeroMuted");
            var eye = new Button { Style = U.St("Cap"), Width = 32, Height = 26, Content = U.Icon(U.IcEye, 16), ToolTip = L.T("ov.hide") };
            eye.SetResourceReference(Control.ForegroundProperty, "HeroMuted");
            AutomationProperties.SetName(eye, L.T("ov.hide"));
            eye.Click += (a, b) => { S.HideBalance = !S.HideBalance; S.Save(); if (snap != null) UpdateOverview(snap); };
            hero.Children.Add(U.Columns("1*", hl, "auto", eye));
            balVal = new TextBlock { FontSize = 44, FontWeight = FontWeights.Bold, Text = "—" };
            balVal.SetResourceReference(TextBlock.ForegroundProperty, "HeroText");
            Typography.SetNumeralAlignment(balVal, FontNumeralAlignment.Tabular);
            var unit = U.T("BCN", 18, "HeroMuted", true);
            unit.VerticalAlignment = VerticalAlignment.Bottom; unit.Margin = new Thickness(0, 0, 0, 6);
            var bal = U.H(10, balVal, unit);
            bal.Margin = new Thickness(0, 10, 0, 0);
            hero.Children.Add(bal);
            balLocked = U.T("", 13, "HeroMuted");
            balLocked.Margin = new Thickness(0, 12, 0, 0);
            hero.Children.Add(balLocked);
            ovAddr = U.T(W.Info != null ? W.Info.FirstAddress : "", 13, "HeroMuted");
            ovAddr.FontFamily = U.Mono; ovAddr.TextWrapping = TextWrapping.NoWrap; ovAddr.TextTrimming = TextTrimming.CharacterEllipsis;
            ovAddr.VerticalAlignment = VerticalAlignment.Center;
            var cp = new Button { Style = U.St("Small"), Width = 32, Height = 32, Padding = new Thickness(0), Content = U.Icon(U.IcCopy, 16), ToolTip = L.T("b.copy_addr") };
            cp.SetResourceReference(Control.BackgroundProperty, "HeroBtn");
            cp.SetResourceReference(Control.ForegroundProperty, "HeroText");
            cp.SetResourceReference(Control.BorderBrushProperty, "HeroLine");
            AutomationProperties.SetName(cp, L.T("b.copy_addr"));
            cp.Click += (a, b) => { Copy(W.Info.FirstAddress); Toast(L.T("toast.copied")); };
            var addrBox = new Border { CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), Padding = new Thickness(12, 4, 4, 4), Margin = new Thickness(0, 16, 0, 0),
                Child = U.Columns("1*", ovAddr, "auto", cp) };
            addrBox.SetResourceReference(Border.BackgroundProperty, "HeroInner");
            addrBox.SetResourceReference(Border.BorderBrushProperty, "HeroLine");
            hero.Children.Add(addrBox);
            var heroCard = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(26, 22, 26, 24), Child = hero };
            heroCard.SetResourceReference(Border.BackgroundProperty, "Hero");
            heroCard.SetResourceReference(Border.BorderBrushProperty, "HeroLine");

            // sync card
            var sync = new StackPanel();
            syncPct = U.T("", 13, "Good", true);
            sync.Children.Add(U.Columns("1*", U.T(L.T("ov.sync"), 13, "Muted"), "auto", syncPct));
            syncBar = new Bar("GoodDot") { Margin = new Thickness(0, 10, 0, 0) };
            sync.Children.Add(syncBar);
            syncText = U.T("", 12, "Muted"); syncText.FontFamily = U.Mono; syncText.Margin = new Thickness(0, 10, 0, 0);
            sync.Children.Add(syncText);
            nodeSyncBox = new StackPanel { Margin = new Thickness(0, 14, 0, 0), Visibility = S.NodeMode == "local" ? Visibility.Visible : Visibility.Collapsed };
            nodeSyncBox.Children.Add(U.T(L.T("ov.node_sync"), 13, "Muted"));
            nodeBar = new Bar("Warn") { Margin = new Thickness(0, 10, 0, 0) };
            nodeSyncBox.Children.Add(nodeBar);
            nodeSyncText = U.T("", 12, "Muted"); nodeSyncText.FontFamily = U.Mono; nodeSyncText.Margin = new Thickness(0, 10, 0, 0);
            nodeSyncBox.Children.Add(nodeSyncText);
            sync.Children.Add(nodeSyncBox);
            var syncCard = U.Card(sync);

            var sec = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(6) };
            sec.SetResourceReference(Border.BackgroundProperty, "GoodBg");
            var secIc = new ContentControl { Content = U.Icon(U.IcShield, 20), Focusable = false, IsTabStop = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            secIc.SetResourceReference(Control.ForegroundProperty, "Good");
            sec.Child = secIc;
            ovType = U.T(WalletTypeText(), 12, "Muted");
            var secText = U.V(2, U.T(L.T("ov.encrypted"), 14, "Text", true), ovType);
            secText.VerticalAlignment = VerticalAlignment.Center;
            var secCard = U.Card(U.Columns("auto", Wrap(sec, 14), "1*", secText));
            secCard.Margin = new Thickness(0, 16, 0, 0);

            var right = U.V(0, syncCard, secCard);
            var row = U.Columns("1.6*", heroCard, 16, null, "1*", right);
            row.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(row);

            // recent transactions
            var rc = new StackPanel();
            var all = U.ActionLink(L.T("ov.all"), () => Go(3), 13);
            var rh = U.Columns("1*", U.H2(L.T("ov.recent")), "auto", all);
            rh.Margin = new Thickness(20, 16, 20, 14);
            rc.Children.Add(rh);
            rc.Children.Add(HeaderRow());
            recentList = new StackPanel();
            rc.Children.Add(recentList);
            recentEmpty = U.T(L.T("ov.empty"), 13, "Muted");
            recentEmpty.Margin = new Thickness(20, 16, 20, 18);
            rc.Children.Add(recentEmpty);
            var recentCard = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Child = rc, Margin = new Thickness(0, 20, 0, 0) };
            recentCard.SetResourceReference(Border.BackgroundProperty, "Card");
            recentCard.SetResourceReference(Border.BorderBrushProperty, "Line");
            sp.Children.Add(recentCard);
            if (snap != null) UpdateOverview(snap);
            return U.Scroll(sp);
        }

        string WalletTypeText()
        {
            if (W.Info == null) return "";
            string t = W.Info.Type == "legacy" ? L.T("ov.type_legacy") : W.Info.Type == "hardware" ? L.T("ov.type_hw") : L.T("ov.type_hd");
            if (W.Info.ViewOnly) t = L.T("ov.view_only") + " · " + t;
            return L.T("ov.keys_local") + " · " + t;
        }

        void UpdateOverview(Snapshot s)
        {
            if (balVal == null) return;
            balVal.Text = S.HideBalance ? "••••••" : Amount.Format(s.Spendable);
            balLocked.Text = S.HideBalance ? "" : L.F("ov.locked", Amount.Format(s.Locked));
            double f = s.KnownHeight > 0 ? (double)s.WalletHeight / s.KnownHeight : 0;
            bool synced = s.Synced;
            syncPct.Text = synced ? "100%" : (f * 100).ToString(f > 0.999 ? "0.00" : "0.0", CultureInfo.InvariantCulture) + "%";
            syncPct.SetResourceReference(TextBlock.ForegroundProperty, synced ? "Good" : "Warn");
            syncBar.Set(synced ? 1 : f, synced ? "GoodDot" : "Warn");
            syncText.Text = !string.IsNullOrEmpty(s.LowerError) ? s.LowerError : Num(s.WalletHeight) + " / " + Num(s.KnownHeight);
            if (S.NodeMode == "local")
            {
                nodeSyncBox.Visibility = Visibility.Visible;
                double nf = s.NodeKnownHeight > 0 ? (double)s.NodeHeight / s.NodeKnownHeight : 0;
                bool ns = s.NodeKnownHeight > 0 && s.NodeHeight + 2 >= s.NodeKnownHeight;
                nodeBar.Set(ns ? 1 : nf, ns ? "GoodDot" : "Warn");
                nodeSyncText.Text = s.NodeReady ? Num(s.NodeHeight) + " / " + Num(s.NodeKnownHeight) + " · " + L.F("ov.peers", s.Peers) : L.T("ov.node_wait");
            }
            recentList.Children.Clear();
            foreach (var t in s.Recent.Take(6)) recentList.Children.Add(TxRow(t, s));
            recentEmpty.Visibility = s.Recent.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (recentEmpty.Visibility == Visibility.Visible)
                recentEmpty.Text = synced ? L.T("ov.empty") : L.T("ov.empty_sync");
        }

        // ---------------------------------------------------------------- transaction rows

        Grid TxGrid()
        {
            var g = new Grid();
            foreach (var w in new[] { new GridLength(46), new GridLength(1.5, GridUnitType.Star), new GridLength(1, GridUnitType.Star), new GridLength(1, GridUnitType.Star), new GridLength(150) })
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = w });
            return g;
        }

        UIElement HeaderRow()
        {
            var g = TxGrid();
            string[] h = { "", L.T("tx.desc"), L.T("tx.date"), L.T("tx.amount"), L.T("tx.status") };
            for (int i = 1; i < h.Length; i++)
            {
                var t = U.T(h[i], 12, "Muted");
                if (i == 3) { t.TextAlignment = TextAlignment.Right; t.Margin = new Thickness(0, 0, 14, 0); }
                Grid.SetColumn(t, i);
                g.Children.Add(t);
            }
            var b = new Border { BorderThickness = new Thickness(0, 1, 0, 1), Padding = new Thickness(20, 8, 20, 8), Child = g };
            b.SetResourceReference(Border.BorderBrushProperty, "Line");
            return b;
        }

        string TxTitle(TxItem t)
        {
            if (t.Coinbase) return L.T("tx.mined");
            if (t.Net >= 0)
            {
                var n = t.PaymentId.Length > 0 ? " · ID " + t.PaymentId.Substring(0, 8) + "…" : "";
                return L.T("tx.received") + n;
            }
            var who = ContactName(t.Counterparty) ?? (t.Counterparty != null ? Short(t.Counterparty) : L.T("tx.self"));
            return L.T("tx.sent") + " · " + who;
        }

        void TxStatus(TxItem t, Snapshot s, out string text, out string fg, out string dot, out string bg)
        {
            long top = s != null ? Math.Max(s.WalletHeight, 0) : 0;
            int need = t.Coinbase ? 10 : 6;
            long conf = t.Confirmed && top >= t.Height ? top - t.Height + 1 : 0;
            if (!t.Confirmed) { text = L.T("tx.pool"); fg = "Muted"; dot = "Muted"; bg = "Badge"; }
            else if (conf < need) { text = L.F("tx.pending", conf, need); fg = "Muted"; dot = "Muted"; bg = "Badge"; }
            else { text = L.T("tx.confirmed"); fg = "Good"; dot = "GoodDot"; bg = "GoodBg"; }
        }

        UIElement TxRow(TxItem t, Snapshot s)
        {
            var g = TxGrid();
            bool inc = t.Net >= 0;
            var circle = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(16), HorizontalAlignment = HorizontalAlignment.Left };
            circle.SetResourceReference(Border.BackgroundProperty, inc ? "GoodBg" : "OutBg");
            var ic = new ContentControl { Content = U.Icon(inc ? U.IcReceive : U.IcSend, 16), Focusable = false, IsTabStop = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            ic.SetResourceReference(Control.ForegroundProperty, inc ? "Good" : "Out");
            circle.Child = ic;
            g.Children.Add(circle);
            var title = U.T(TxTitle(t), 14, "Text");
            title.TextTrimming = TextTrimming.CharacterEllipsis; title.TextWrapping = TextWrapping.NoWrap; title.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(title, 1); g.Children.Add(title);
            var date = U.T(Date(t.Timestamp), 13, "Muted");
            date.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(date, 2); g.Children.Add(date);
            var amt = U.T(S.HideBalance ? "•••" : Amount.Format(t.Net, true), 14, inc ? "Good" : "Text", true);
            amt.TextAlignment = TextAlignment.Right; amt.Margin = new Thickness(0, 0, 14, 0); amt.VerticalAlignment = VerticalAlignment.Center;
            Typography.SetNumeralAlignment(amt, FontNumeralAlignment.Tabular);
            Grid.SetColumn(amt, 3); g.Children.Add(amt);
            string st, fg, dot, bg;
            TxStatus(t, s, out st, out fg, out dot, out bg);
            var pill = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
            pill.SetResourceReference(Border.BackgroundProperty, bg);
            var pt = U.T(st, 12, fg, true);
            pill.Child = U.H(6, U.Dot(dot, 7), pt);
            Grid.SetColumn(pill, 4); g.Children.Add(pill);
            var b = new Button { Style = U.St("Row"), Content = g, Padding = new Thickness(20, 9, 20, 9) };
            AutomationProperties.SetName(b, TxTitle(t) + " " + Amount.Format(t.Net, true) + " BCN");
            b.Click += (a, e) => TxDetails(t);
            var wrap = new Border { BorderThickness = new Thickness(0, 0, 0, 1), Child = b };
            wrap.SetResourceReference(Border.BorderBrushProperty, "Grid");
            return wrap;
        }

        void TxDetails(TxItem t)
        {
            WrapPanel bp;
            var sp = Dialog(TxTitle(t), null, out bp);
            var amt = U.T(Amount.Format(t.Net, true) + " BCN", 26, t.Net >= 0 ? "Good" : "Text", true);
            amt.Margin = new Thickness(0, 10, 0, 6);
            sp.Children.Add(amt);
            string st, fg, dot, bg;
            TxStatus(t, snap, out st, out fg, out dot, out bg);
            var rows = new List<Tuple<string, string, bool>> {
                Tuple.Create(L.T("tx.status"), st, false),
                Tuple.Create(L.T("tx.date"), Date(t.Timestamp), false),
                Tuple.Create(L.T("tx.height"), t.Confirmed ? Num(t.Height) : "—", false),
                Tuple.Create(L.T("tx.fee"), Amount.Format(t.Fee) + " BCN", false),
                Tuple.Create(L.T("tx.hash"), t.Hash, true) };
            if (t.Net < 0 && t.Counterparty != null) rows.Add(Tuple.Create(L.T("tx.to"), t.Counterparty, true));
            if (t.Net >= 0 && t.ToOurAddress != null && W.Info != null && t.ToOurAddress != W.Info.FirstAddress) rows.Add(Tuple.Create(L.T("tx.to_ours"), t.ToOurAddress, true));
            if (t.PaymentId.Length > 0) rows.Add(Tuple.Create(L.T("tx.pid"), t.PaymentId, true));
            foreach (var r in rows)
            {
                var k = U.T(r.Item1, 12, "Muted");
                var v = new TextBox { Text = r.Item2, IsReadOnly = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
                v.SetResourceReference(Control.ForegroundProperty, "Text");
                if (r.Item3) v.FontFamily = U.Mono;
                var line = U.V(2, k, v);
                line.Margin = new Thickness(0, 10, 0, 0);
                sp.Children.Add(line);
            }
            var ex = U.Btn(L.T("tx.explorer"), "Secondary", (a, e) => U.OpenUrl(App.TxUrl(t.Hash)), U.IcExternal);
            var cp = U.Btn(L.T("tx.copy_hash"), "Secondary", (a, e) => { Copy(t.Hash); Toast(L.T("toast.copied")); }, U.IcCopy);
            var ok = U.Btn(L.T("b.close"), "Primary", (a, e) => CloseModal());
            ex.Margin = new Thickness(0, 0, 10, 0); cp.Margin = new Thickness(0, 0, 10, 0);
            if (t.Net < 0 && t.Counterparty != null && W.Info != null && !W.Info.ViewOnly)
            {
                var pr = U.Btn(L.T("proof.create"), "Secondary", (a, e) => ProofCreateFlow(t), U.IcShield);
                pr.Margin = new Thickness(0, 0, 10, 0);
                pr.ToolTip = L.T("proof.create_hint");
                bp.Children.Add(pr);
            }
            bp.Children.Add(ex); bp.Children.Add(cp); bp.Children.Add(ok);
            sp.Children.Add(bp);
            ShowModal(sp, true, 640);
        }

        // ---------------------------------------------------------------- sendproof
        void ProofCreateFlow(TxItem t)
        {
            WrapPanel bp;
            var sp = Dialog(L.T("proof.create"), L.T("proof.create_text"), out bp);
            var msg = U.Field("", L.T("proof.message"));
            msg.FontFamily = FontFamily;
            var ml = U.Labeled(L.T("proof.message"), msg, L.T("proof.message_hint"));
            ml.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(ml);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var go = U.Btn(L.T("b.create"), "Primary", (a, e) =>
            {
                var m = msg.Text.Trim();
                CloseModal();
                string proof = null;
                Run(() => proof = W.CreateSendproof(t.Hash, t.Counterparty, m), () =>
                {
                    WrapPanel bp2;
                    var sp2 = Dialog(L.T("proof.ready"), L.T("proof.ready_text"), out bp2);
                    var box = new TextBox { Style = U.St("Area"), Text = proof, Height = 150, Margin = new Thickness(0, 14, 0, 0), FontSize = 11 };
                    sp2.Children.Add(box);
                    var c = U.Btn(L.T("b.copy"), "Secondary", (a2, e2) => { Copy(proof); Toast(L.T("toast.copied")); }, U.IcCopy);
                    c.Margin = new Thickness(0, 0, 10, 0);
                    bp2.Children.Add(c);
                    bp2.Children.Add(U.Btn(L.T("b.close"), "Primary", (a2, e2) => CloseModal()));
                    sp2.Children.Add(bp2);
                    ShowModal(sp2, true, 640);
                }, ex => Error(Msg(ex)));
            }, U.IcShield);
            go.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        void ProofCheckFlow()
        {
            WrapPanel bp;
            var sp = Dialog(L.T("proof.check"), L.T("proof.check_text"), out bp);
            var box = new TextBox { Style = U.St("Field"), Height = 120, TextWrapping = TextWrapping.Wrap, VerticalContentAlignment = VerticalAlignment.Top, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 14, 0, 0), FontSize = 11 };
            AutomationProperties.SetName(box, L.T("proof.check"));
            sp.Children.Add(box);
            var result = U.T("", 13, "Text");
            result.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(result);
            var no = U.Btn(L.T("b.close"), "Secondary", (a, e) => CloseModal());
            Button go = null;
            go = U.Btn(L.T("proof.check_btn"), "Primary", (a, e) =>
            {
                var proof = box.Text.Trim();
                if (proof.Length == 0) return;
                go.IsEnabled = false;
                result.Text = L.T("proof.checking");
                result.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
                Dictionary<string, object> res = null;
                Run(() => res = W.CheckSendproof(proof), () =>
                {
                    go.IsEnabled = true;
                    result.Text = L.F("proof.valid", Amount.Format(J.Long(res, "amount")), J.Str(res, "address"), J.Str(res, "transaction_hash"),
                        J.Str(res, "message").Length > 0 ? J.Str(res, "message") : "—");
                    result.SetResourceReference(TextBlock.ForegroundProperty, "Good");
                }, ex =>
                {
                    go.IsEnabled = true;
                    result.Text = L.T("proof.invalid") + "\n" + ex.Message;
                    result.SetResourceReference(TextBlock.ForegroundProperty, "Bad");
                });
            }, U.IcShield);
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp, true, 640);
        }

        // ---------------------------------------------------------------- toast (bottom-right note)
        void Toast(string text)
        {
            if (tray != null && !IsVisible) return;
            var t = U.T(text, 13, "Text", true);
            var b = new Border { CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Padding = new Thickness(14, 9, 14, 9), Child = t,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 24, 24), IsHitTestVisible = false };
            b.SetResourceReference(Border.BackgroundProperty, "NavActive");
            b.SetResourceReference(Border.BorderBrushProperty, "Line");
            bodyHost.Children.Add(b);
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.8) };
            timer.Tick += (a, e) => { timer.Stop(); bodyHost.Children.Remove(b); };
            timer.Start();
        }

        // ---------------------------------------------------------------- Send
        TextBox sendAddr, sendAmount, sendPid;
        RadioButton[] feeBtns;
        CheckBox sendSubtract;
        TextBlock sendAvail, sendErr, sendFeeHint;

        static readonly Regex RxAddress = new Regex("^(bcn[1-9A-HJ-NP-Za-km-z]{90,110}|2[1-9A-HJ-NP-Za-km-z]{94})$");
        static readonly Regex RxPid = new Regex("^[0-9a-fA-F]{64}$");

        FrameworkElement BuildSend()
        {
            var sp = Page();
            sp.Children.Add(U.H1(L.T("nav.send")));
            var form = new StackPanel();
            sendAddr = U.Field("", L.T("s.to"));
            var book = U.Btn(L.T("s.from_book"), "Small", (a, b) => PickContact(), U.IcBook);
            book.Margin = new Thickness(10, 0, 0, 0);
            form.Children.Add(U.Labeled(L.T("s.to"), U.Columns("1*", sendAddr, "auto", book), L.T("s.to_hint")));
            sendAmount = U.Field("", L.T("s.amount"));
            sendAmount.FontSize = 15;
            var max = U.Btn(L.T("s.all"), "Small", (a, b) =>
            {
                if (snap == null) return;
                sendAmount.Text = Amount.Plain(snap.Spendable);
                sendSubtract.IsChecked = true;
            });
            max.Margin = new Thickness(10, 0, 0, 0);
            var al = U.Labeled(L.T("s.amount"), U.Columns("1*", sendAmount, "auto", max));
            al.Margin = new Thickness(0, 16, 0, 0);
            form.Children.Add(al);
            sendPid = U.Field("", L.T("s.pid"));
            var pl = U.Labeled(L.T("s.pid"), sendPid, L.T("s.pid_hint"));
            pl.Margin = new Thickness(0, 16, 0, 0);
            form.Children.Add(pl);
            var fl = U.T(L.T("s.fee"), 12, "Muted");
            fl.Margin = new Thickness(0, 16, 0, 5);
            form.Children.Add(fl);
            form.Children.Add(U.Segmented("fee", new[] { L.T("s.fee_eco"), L.T("s.fee_normal"), L.T("s.fee_fast") }, new[] { "0", "1", "2" },
                S.FeeLevel.ToString(CultureInfo.InvariantCulture), v => { S.FeeLevel = int.Parse(v, CultureInfo.InvariantCulture); S.Save(); UpdateSendBalance(snap); }, out feeBtns));
            sendFeeHint = U.T("", 11, "Muted");
            sendFeeHint.Margin = new Thickness(0, 6, 0, 0);
            form.Children.Add(sendFeeHint);
            anonLabel = U.T("", 12, "Muted");
            anonLabel.Margin = new Thickness(0, 16, 0, 5);
            form.Children.Add(anonLabel);
            anonSlider = U.Range(0, 50, S.Anonymity);
            AutomationProperties.SetName(anonSlider, L.T("s.anon"));
            anonSlider.ValueChanged += (a, b) => { S.Anonymity = (int)anonSlider.Value; S.Save(); UpdateAnon(); };
            form.Children.Add(anonSlider);
            anonHint = U.T("", 11, "Muted");
            anonHint.Margin = new Thickness(0, 6, 0, 0);
            form.Children.Add(anonHint);
            UpdateAnon();
            sendSubtract = new CheckBox { Style = U.St("Switch"), Margin = new Thickness(0, 16, 0, 0),
                Content = U.V(3, U.T(L.T("s.subtract"), 14, "Text"), U.T(L.T("s.subtract_hint"), 12, "Muted")) };
            form.Children.Add(sendSubtract);
            sendErr = U.T("", 13, "Bad");
            sendErr.Margin = new Thickness(0, 14, 0, 0);
            form.Children.Add(sendErr);
            var go = U.Btn(L.T("s.review"), "Primary", (a, b) => PrepareSend(), U.IcSend);
            go.HorizontalAlignment = HorizontalAlignment.Left;
            go.Margin = new Thickness(0, 14, 0, 0);
            if (W.Info != null && W.Info.ViewOnly) { go.IsEnabled = false; sendErr.Text = L.T("s.view_only"); }
            form.Children.Add(go);

            var side = new StackPanel();
            side.Children.Add(U.T(L.T("ov.available"), 13, "Muted"));
            sendAvail = U.T("—", 26, "Text", true);
            sendAvail.Margin = new Thickness(0, 6, 0, 0);
            side.Children.Add(sendAvail);
            var tip = U.T(L.T("s.tip"), 12, "Muted");
            tip.Margin = new Thickness(0, 16, 0, 0);
            side.Children.Add(tip);

            var row = U.Columns("1.6*", U.Card(form), 16, null, "1*", U.Card(side));
            ((FrameworkElement)row.Children[1]).VerticalAlignment = VerticalAlignment.Top;
            row.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(row);
            if (snap != null) UpdateSendBalance(snap);
            return U.Scroll(sp);
        }

        TextBlock anonLabel, anonHint;
        Slider anonSlider;

        // Anonymity = how many other outputs (mixins) hide each coin you spend in a ring signature.
        void UpdateAnon()
        {
            int a = S.Anonymity;
            anonLabel.Text = L.F("s.anon_n", a);
            string key = a == 0 ? "s.anon_0" : a < 6 ? "s.anon_low" : a == 6 ? "s.anon_def" : "s.anon_high";
            anonHint.Text = L.T(key);
            anonHint.SetResourceReference(TextBlock.ForegroundProperty, a == 0 ? "Bad" : a < 6 ? "Warn" : "Muted");
        }

        long FeePerByte(Snapshot s)
        {
            long rec = s != null && s.FeePerByte > 0 ? s.FeePerByte : 100;
            return S.FeeLevel == 0 ? Math.Max(1, rec / 2) : S.FeeLevel == 2 ? rec * 2 : rec;
        }

        void UpdateSendBalance(Snapshot s)
        {
            if (sendAvail == null || s == null) return;
            sendAvail.Text = Amount.Format(s.Spendable) + " BCN";
            sendFeeHint.Text = L.F("s.fee_hint", FeePerByte(s));
        }

        void PickContact()
        {
            if (S.Contacts.Count == 0) { Info(L.T("nav.contacts"), L.T("c.book_empty")); return; }
            WrapPanel bp;
            var sp = Dialog(L.T("s.from_book"), null, out bp);
            var list = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            foreach (var c in S.Contacts.OrderBy(x => x.Name))
            {
                var cc = c;
                var addr = U.T(Short(c.Address), 12, "Muted"); addr.FontFamily = U.Mono;
                var b = new Button { Style = U.St("Row"), Padding = new Thickness(10, 8, 10, 8), Content = U.V(2, U.T(c.Name, 14, "Text", true), addr) };
                b.Click += (a, e) => { sendAddr.Text = cc.Address; sendPid.Text = cc.PaymentId ?? ""; CloseModal(); };
                list.Children.Add(b);
            }
            sp.Children.Add(new ScrollViewer { Content = list, MaxHeight = 380, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            bp.Children.Add(U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal()));
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        void PrepareSend()
        {
            sendErr.Text = "";
            var addr = sendAddr.Text.Trim();
            var pid = sendPid.Text.Trim();
            long amount;
            if (!RxAddress.IsMatch(addr)) { sendErr.Text = L.T("s.bad_addr"); sendAddr.Focus(); return; }
            if (W.Info != null && addr == W.Info.FirstAddress) { sendErr.Text = L.T("s.self"); return; }
            if (!Amount.TryParse(sendAmount.Text, out amount) || amount <= 0) { sendErr.Text = L.T("s.bad_amount"); sendAmount.Focus(); return; }
            if (pid.Length > 0 && !RxPid.IsMatch(pid)) { sendErr.Text = L.T("s.bad_pid"); sendPid.Focus(); return; }
            if (snap != null && !snap.Synced) { sendErr.Text = L.T("s.not_synced"); return; }
            bool subtract = sendSubtract.IsChecked == true;
            long fpb = FeePerByte(snap);
            int anon = S.Anonymity;
            ShowProgress(L.T("s.preparing"), L.T("s.preparing_text"));
            Dictionary<string, object> res = null;
            Run(() => res = W.CreateTx(addr, amount, pid, fpb, subtract, anon), () =>
            {
                CloseModal();
                var tx = J.Obj(res, "transaction");
                long fee = J.Long(tx, "fee");
                long sent = subtract ? amount - fee : amount;
                ConfirmSend(addr, sent, fee, pid, J.Str(res, "binary_transaction"), J.Str(tx, "hash"));
            }, ex =>
            {
                CloseModal();
                var rx = ex as RpcException;
                if (rx != null && rx.Code == -301) sendErr.Text = L.T("s.no_funds");
                else if (rx != null && rx.Code == -302) sendErr.Text = L.T("s.too_big");
                else if (rx != null && rx.Code == -4) sendErr.Text = L.T("s.bad_addr");
                else if (rx != null && rx.Code == -303) sendErr.Text = L.T("s.no_anon");
                else if (rx != null && rx.Code == -305) sendErr.Text = L.T("s.too_anon");
                else sendErr.Text = Msg(ex);
            });
        }

        void ConfirmSend(string addr, long amount, long fee, string pid, string binary, string hash)
        {
            WrapPanel bp;
            var sp = Dialog(L.T("s.confirm"), L.T("s.confirm_text"), out bp);
            var who = ContactName(addr);
            var rows = new List<Tuple<string, string, bool>> {
                Tuple.Create(L.T("tx.to"), (who != null ? who + "\n" : "") + addr, true),
                Tuple.Create(L.T("s.amount"), Amount.Format(amount) + " BCN", false),
                Tuple.Create(L.T("tx.fee"), Amount.Format(fee) + " BCN", false),
                Tuple.Create(L.T("s.total"), Amount.Format(amount + fee) + " BCN", false),
                Tuple.Create(L.T("s.anon"), S.Anonymity.ToString(CultureInfo.InvariantCulture), false) };
            if (pid.Length > 0) rows.Add(Tuple.Create(L.T("tx.pid"), pid, true));
            foreach (var r in rows)
            {
                var v = U.T(r.Item2, r.Item1 == L.T("s.total") ? 18 : 13, "Text", r.Item1 == L.T("s.total"));
                if (r.Item3) v.FontFamily = U.Mono;
                var line = U.V(2, U.T(r.Item1, 12, "Muted"), v);
                line.Margin = new Thickness(0, 12, 0, 0);
                sp.Children.Add(line);
            }
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var go = U.Btn(L.T("s.send_now"), "Primary", (a, e) =>
            {
                CloseModal();
                ShowProgress(L.T("s.sending"), L.T("s.sending_text"));
                Run(() => W.SendTx(binary), () =>
                {
                    CloseModal();
                    sendAddr.Text = ""; sendAmount.Text = ""; sendPid.Text = ""; sendSubtract.IsChecked = false;
                    SentDialog(hash, addr, pid);
                }, ex => { CloseModal(); Error(L.T("s.send_failed") + "\n" + Msg(ex)); });
            }, U.IcSend);
            go.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp, true, 600);
        }

        void SentDialog(string hash, string addr, string pid)
        {
            WrapPanel bp;
            var sp = Dialog(L.T("s.sent"), L.T("s.sent_text"), out bp);
            var h = new TextBox { Text = hash, IsReadOnly = true, BorderThickness = new Thickness(0), Background = Brushes.Transparent, TextWrapping = TextWrapping.Wrap, FontFamily = U.Mono, Margin = new Thickness(0, 12, 0, 0) };
            h.SetResourceReference(Control.ForegroundProperty, "Text");
            sp.Children.Add(h);
            if (ContactName(addr) == null)
            {
                var save = U.Btn(L.T("s.save_contact"), "Secondary", (a, e) => { CloseModal(); EditContact(new Contact { Address = addr, PaymentId = pid, Name = "" }, true); }, U.IcBook);
                save.Margin = new Thickness(0, 0, 10, 0);
                bp.Children.Add(save);
            }
            var ex = U.Btn(L.T("tx.explorer"), "Secondary", (a, e) => U.OpenUrl(App.TxUrl(hash)), U.IcExternal);
            ex.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(ex);
            var ok = U.Btn(L.T("b.ok"), "Primary", (a, e) => { CloseModal(); Go(0); });
            ok.IsDefault = true;
            bp.Children.Add(ok);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        // ---------------------------------------------------------------- Receive
        Image qrImage;
        TextBlock rcvAddr, rcvLabel;
        TextBox rcvAmount;
        StackPanel rcvList;
        string rcvCurrent;

        FrameworkElement BuildReceive()
        {
            var sp = Page();
            var add = U.Btn(L.T("r.new"), "Secondary", (a, b) => NewAddressFlow(), U.IcPlus);
            add.ToolTip = L.T("r.new_hint");
            if (W.Info == null || W.Info.Type != "amethyst" || W.Info.ViewOnly) add.Visibility = Visibility.Collapsed;
            sp.Children.Add(U.Columns("1*", U.H1(L.T("nav.receive")), "auto", add));

            var left = new StackPanel();
            rcvLabel = U.T(L.T("r.main"), 13, "Muted");
            left.Children.Add(rcvLabel);
            rcvAddr = U.T("", 15, "Text", true);
            rcvAddr.FontFamily = U.Mono;
            rcvAddr.Margin = new Thickness(0, 8, 0, 0);
            left.Children.Add(rcvAddr);
            var copy = U.Btn(L.T("b.copy_addr"), "Primary", (a, b) => { Copy(rcvCurrent); Toast(L.T("toast.copied")); }, U.IcCopy);
            copy.HorizontalAlignment = HorizontalAlignment.Left;
            copy.Margin = new Thickness(0, 16, 0, 0);
            left.Children.Add(copy);
            rcvAmount = U.Field("", L.T("r.amount"));
            rcvAmount.TextChanged += (a, b) => UpdateQr();
            var al = U.Labeled(L.T("r.amount"), rcvAmount, L.T("r.amount_hint"));
            al.Margin = new Thickness(0, 22, 0, 0);
            left.Children.Add(al);
            var note = U.T(L.T("r.note"), 12, "Muted");
            note.Margin = new Thickness(0, 18, 0, 0);
            left.Children.Add(note);

            qrImage = new Image { Width = 220, Height = 220, Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(qrImage, BitmapScalingMode.NearestNeighbor);
            var qrBox = new Border { CornerRadius = new CornerRadius(8), Background = Brushes.White, Padding = new Thickness(8), Child = qrImage, HorizontalAlignment = HorizontalAlignment.Center };
            var row = U.Columns("1.5*", U.Card(left), 16, null, "auto", U.Card(qrBox));
            row.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(row);

            var lc = new StackPanel();
            lc.Children.Add(U.H2(L.T("r.all")));
            rcvList = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            lc.Children.Add(rcvList);
            var listCard = U.Card(lc);
            listCard.Margin = new Thickness(0, 16, 0, 0);
            sp.Children.Add(listCard);
            rcvCurrent = W.Info != null ? W.Info.FirstAddress : "";
            rcvAddr.Text = rcvCurrent;
            UpdateQr();
            return U.Scroll(sp);
        }

        void UpdateQr()
        {
            if (qrImage == null || string.IsNullOrEmpty(rcvCurrent)) return;
            long a;
            string uri = "bytecoin:" + rcvCurrent;
            if (Amount.TryParse(rcvAmount.Text, out a) && a > 0) uri += "?amount=" + Amount.Plain(a);
            try { qrImage.Source = Qr.Image(uri); qrImage.ToolTip = uri; } catch { qrImage.Source = null; }
        }

        void RefreshAddresses()
        {
            if (rcvList == null) return;
            List<Tuple<string, string>> list = null;
            Run(() => list = W.Addresses(), () =>
            {
                rcvList.Children.Clear();
                int i = 0;
                foreach (var t in list)
                {
                    var addr = t.Item1;
                    var label = t.Item2.Length > 0 ? t.Item2 : (i == 0 ? L.T("r.main") : L.F("r.n", i + 1));
                    var at = U.T(addr, 12, "Muted"); at.FontFamily = U.Mono; at.TextTrimming = TextTrimming.CharacterEllipsis; at.TextWrapping = TextWrapping.NoWrap;
                    var b = new Button { Style = U.St("Row"), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(-10, 2, -10, 0), Content = U.V(2, U.T(label, 14, "Text", true), at) };
                    var lab = label;
                    b.Click += (a, e) => { rcvCurrent = addr; rcvAddr.Text = addr; rcvLabel.Text = lab; UpdateQr(); };
                    rcvList.Children.Add(b);
                    i++;
                }
            }, ex => { });
        }

        void NewAddressFlow()
        {
            WrapPanel bp;
            var sp = Dialog(L.T("r.new"), L.T("r.new_text"), out bp);
            var name = U.Field("", L.T("r.label"));
            name.FontFamily = FontFamily;
            var nl = U.Labeled(L.T("r.label"), name);
            nl.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(nl);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var go = U.Btn(L.T("b.create"), "Primary", (a, e) =>
            {
                var label = name.Text.Trim();
                CloseModal();
                string created = null;
                Run(() => created = W.NewAddress(label), () =>
                {
                    if (created != null) { rcvCurrent = created; rcvAddr.Text = created; rcvLabel.Text = label.Length > 0 ? label : L.T("r.new"); UpdateQr(); }
                    RefreshAddresses();
                }, ex => Error(Msg(ex)));
            });
            go.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(go);
            sp.Children.Add(bp);
            ShowModal(sp);
        }

        // ---------------------------------------------------------------- History
        StackPanel histList;
        TextBlock histEmpty;
        Button histMore;
        TextBox histSearch;
        RadioButton[] histBtns;
        string histFilter = "all";
        readonly List<TxItem> histItems = new List<TxItem>();
        long histNext;

        FrameworkElement BuildHistory()
        {
            var sp = Page();
            var chk = U.Btn(L.T("proof.check"), "Secondary", (a, b) => ProofCheckFlow(), U.IcShield);
            chk.ToolTip = L.T("proof.check_text");
            sp.Children.Add(U.Columns("1*", U.H1(L.T("nav.history")), "auto", chk));
            histSearch = U.Field("", L.T("h.search"));
            histSearch.TextChanged += (a, b) => RenderHistory();
            var seg = U.Segmented("hist", new[] { L.T("h.all"), L.T("h.in"), L.T("h.out") }, new[] { "all", "in", "out" }, histFilter,
                v => { histFilter = v; RenderHistory(); }, out histBtns);
            var bar = U.Columns("1*", U.Labeled(L.T("h.search"), histSearch), 16, null, "auto", U.Labeled(L.T("h.filter"), seg));
            bar.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(bar);

            var c = new StackPanel();
            c.Children.Add(HeaderRow());
            histList = new StackPanel();
            c.Children.Add(histList);
            histEmpty = U.T(L.T("ov.empty"), 13, "Muted");
            histEmpty.Margin = new Thickness(20, 16, 20, 18);
            c.Children.Add(histEmpty);
            histMore = U.Btn(L.T("h.more"), "Small", (a, b) => LoadMoreHistory());
            histMore.Margin = new Thickness(20, 12, 20, 16);
            histMore.HorizontalAlignment = HorizontalAlignment.Left;
            c.Children.Add(histMore);
            var card = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Child = c, Margin = new Thickness(0, 16, 0, 0), Padding = new Thickness(0, 6, 0, 0) };
            card.SetResourceReference(Border.BackgroundProperty, "Card");
            card.SetResourceReference(Border.BorderBrushProperty, "Line");
            sp.Children.Add(card);
            return U.Scroll(sp);
        }

        void ReloadHistory()
        {
            if (histList == null || !W.IsOpen) return;
            Tuple<List<TxItem>, long> res = null;
            Run(() => res = W.LoadTransfers(null, 0, uint.MaxValue, 200), () =>
            {
                histItems.Clear(); histItems.AddRange(res.Item1); histNext = res.Item2;
                RenderHistory();
            }, ex => { histEmpty.Text = Msg(ex); histEmpty.Visibility = Visibility.Visible; });
        }

        void LoadMoreHistory()
        {
            if (histNext <= 0) return;
            histMore.IsEnabled = false;
            Tuple<List<TxItem>, long> res = null;
            Run(() => res = W.LoadOlder(histNext, 200), () =>
            {
                histMore.IsEnabled = true;
                var have = new HashSet<string>(histItems.Select(x => x.Hash));
                histItems.AddRange(res.Item1.Where(x => have.Add(x.Hash)));
                histNext = res.Item2;
                RenderHistory();
            }, ex => { histMore.IsEnabled = true; Error(Msg(ex)); });
        }

        void RenderHistory()
        {
            if (histList == null) return;
            var q = histSearch.Text.Trim();
            histList.Children.Clear();
            int n = 0;
            foreach (var t in histItems)
            {
                if (histFilter == "in" && t.Net < 0) continue;
                if (histFilter == "out" && t.Net >= 0) continue;
                if (q.Length > 0)
                {
                    var hay = t.Hash + " " + (t.Counterparty ?? "") + " " + t.PaymentId + " " + (ContactName(t.Counterparty) ?? "") + " " + Amount.Plain(Math.Abs(t.Net));
                    if (hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                histList.Children.Add(TxRow(t, snap));
                if (++n >= 500) break;
            }
            histEmpty.Visibility = n == 0 ? Visibility.Visible : Visibility.Collapsed;
            histEmpty.Text = histItems.Count == 0 ? (snap != null && !snap.Synced ? L.T("ov.empty_sync") : L.T("ov.empty")) : L.T("h.nothing");
            histMore.Visibility = histNext > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
