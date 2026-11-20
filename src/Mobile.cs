using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path = System.IO.Path;

namespace BcnWallet
{
    // "iOS · Android · Web": how to move this wallet to the Amethyst web wallet (browser or installed phone app)
    // with an encrypted .amwbackup file, what the file holds, and the way back.
    public partial class MainWindow
    {
        FrameworkElement BuildMobile()
        {
            var sp = Page();
            sp.Children.Add(U.H1(L.T("m.title")));
            var lead = U.T(L.T("m.lead"), 13, "Muted");
            lead.Margin = new Thickness(0, 8, 0, 0);
            sp.Children.Add(lead);

            // 1-2-3 + export button | QR with the web wallet address
            var steps = new StackPanel();
            steps.Children.Add(U.H2(L.T("m.steps_title")));
            for (int i = 1; i <= 4; i++)
            {
                var n = U.T(i.ToString(CultureInfo.InvariantCulture), 13, "Accent", true);
                n.Width = 22;
                var t = U.T(L.T("m.step" + i), 13, "Text");
                var row = U.Columns("auto", n, "1*", t);
                row.Margin = new Thickness(0, 10, 0, 0);
                steps.Children.Add(row);
            }
            bool can = W.Info != null && W.Info.Type == "amethyst" && !W.Info.ViewOnly;
            var export = U.Btn(L.T("m.export"), "Primary", (a, b) => ExportBackupFlow(), U.IcDownload);
            export.HorizontalAlignment = HorizontalAlignment.Left;
            export.Margin = new Thickness(0, 18, 0, 0);
            export.IsEnabled = can;
            steps.Children.Add(export);
            if (!can)
            {
                var why = U.T(L.T("bk.only_amethyst"), 12, "Muted");
                why.Margin = new Thickness(0, 8, 0, 0);
                steps.Children.Add(why);
            }

            var qr = new Image { Width = 150, Height = 150, Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(qr, BitmapScalingMode.NearestNeighbor);
            try { qr.Source = Qr.Image(App.WebWalletUrl); } catch { }
            var qrBox = new Border { CornerRadius = new CornerRadius(8), Background = Brushes.White, Padding = new Thickness(6), Child = qr, HorizontalAlignment = HorizontalAlignment.Center };
            var side = new StackPanel { Width = 190 };
            side.Children.Add(qrBox);
            var qt = U.T(L.T("m.qr"), 12, "Muted");
            qt.TextAlignment = TextAlignment.Center;
            qt.Margin = new Thickness(0, 10, 0, 0);
            side.Children.Add(qt);
            var link = U.Link(App.WebWalletUrl.Replace("https://", ""), App.WebWalletUrl, 12);
            link.HorizontalAlignment = HorizontalAlignment.Center;
            link.Margin = new Thickness(0, 4, 0, 0);
            side.Children.Add(link);
            var row1 = U.Columns("1*", U.Card(steps), 14, null, "auto", U.Card(side));
            row1.Margin = new Thickness(0, 20, 0, 0);
            sp.Children.Add(row1);

            // install on each platform
            var inst = new StackPanel();
            inst.Children.Add(U.H2(L.T("m.install")));
            var grid = new UniformGrid3();
            foreach (var p in new[] { "ios", "android", "web" })
            {
                var col = new StackPanel { Margin = new Thickness(0, 12, 16, 0) };
                col.Children.Add(U.T(L.T("m." + p), 14, "Text", true));
                var d = U.T(L.T("m." + p + "_d"), 12, "Muted");
                d.Margin = new Thickness(0, 6, 0, 0);
                col.Children.Add(d);
                grid.Children.Add(col);
            }
            inst.Children.Add(grid);
            var instCard = U.Card(inst);
            instCard.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(instCard);

            // what the file is and how it works
            var how = new StackPanel();
            how.Children.Add(U.H2(L.T("m.how")));
            foreach (var k in new[] { "m.how1", "m.how2", "m.how3", "m.how4" })
            {
                var t = U.T("•  " + L.T(k), 13, "Text");
                t.Margin = new Thickness(0, 10, 0, 0);
                how.Children.Add(t);
            }
            var tech = U.T(L.T("m.tech"), 11, "Muted");
            tech.Margin = new Thickness(0, 14, 0, 0);
            how.Children.Add(tech);
            how.Children.Add(WarnBox(L.T("m.warn")));
            var howCard = U.Card(how);
            howCard.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(howCard);

            // the way back: phone / web -> this PC
            var back = new StackPanel();
            back.Children.Add(U.H2(L.T("m.back")));
            var bt = U.T(L.T("m.back_d"), 13, "Muted");
            bt.Margin = new Thickness(0, 8, 0, 0);
            back.Children.Add(bt);
            var imp = U.Btn(L.T("bk.import"), "Secondary", (a, b) => Confirm(L.T("bk.import"), L.T("m.back_lock"), L.T("nav.lock"), () => LockWallet(ImportBackupFlow)), U.IcKey);
            imp.HorizontalAlignment = HorizontalAlignment.Left;
            imp.Margin = new Thickness(0, 14, 0, 0);
            back.Children.Add(imp);
            var backCard = U.Card(back);
            backCard.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(backCard);
            return U.Scroll(sp);
        }

        // three equal columns without UniformGrid's fixed row height surprises
        class UniformGrid3 : System.Windows.Controls.Primitives.UniformGrid { public UniformGrid3() { Columns = 3; } }

        // ---------------------------------------------------------------- export

        void ExportBackupFlow()
        {
            if (W.Info == null || W.Info.Type != "amethyst" || W.Info.ViewOnly) { Error(L.T("bk.only_amethyst")); return; }
            WrapPanel bp;
            var sp = Dialog(L.T("bk.export_title"), L.T("bk.export_text"), out bp);
            var p0 = U.Pass(L.T("bk.wallet_pw"));
            var p1 = U.Pass(L.T("bk.pw"));
            var p2 = U.Pass(L.T("bk.pw2"));
            var l0 = U.Labeled(L.T("bk.wallet_pw"), p0); l0.Margin = new Thickness(0, 14, 0, 0);
            var l12 = U.Columns("1*", U.Labeled(L.T("bk.pw"), p1), 12, null, "1*", U.Labeled(L.T("bk.pw2"), p2));
            l12.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(l0); sp.Children.Add(l12);
            var hint = U.T(L.F("bk.pw_hint", Backup.MinPassword), 12, "Muted"); hint.Margin = new Thickness(0, 8, 0, 0);
            sp.Children.Add(hint);
            var err = U.T("", 12, "Bad"); err.Margin = new Thickness(0, 10, 0, 0);
            sp.Children.Add(err);
            var no = U.Btn(L.T("b.cancel"), "Secondary", (a, e) => CloseModal());
            var ok = U.Btn(L.T("b.continue"), "Primary", (a, e) =>
            {
                if (p0.Password != W.Password) { err.Text = L.T("err.207"); return; }
                if (p1.Password.Length < Backup.MinPassword) { err.Text = L.F("bk.pw_short", Backup.MinPassword); return; }
                if (p1.Password != p2.Password) { err.Text = L.T("pw.mismatch"); return; }
                if (p1.Password == p0.Password) { err.Text = L.T("bk.pw_same"); return; }
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = L.T("bk.export_title"),
                    Filter = L.T("bk.filter") + " (*" + Backup.Extension + ")|*" + Backup.Extension,
                    FileName = Backup.FileName(W.Info.FirstAddress),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                if (dlg.ShowDialog(this) != true) return;
                var target = dlg.FileName;
                var walletPw = p0.Password; var pw = p1.Password;
                p0.Clear(); p1.Clear(); p2.Clear();
                CloseModal();
                ShowProgress(L.T("bk.export_title"), L.T("bk.step_phrase"));
                string reason = ""; long height = 0;
                Action<string> step = s => Dispatcher.BeginInvoke(new Action(() => { if (progressText != null) progressText.Text = s; }));
                Run(() =>
                {
                    var payload = W.BackupPayload(walletPw, step, out reason, out height);
                    step(L.T("bk.step_encrypt"));
                    var json = Backup.ToJson(payload);
                    payload.Clear();
                    var bytes = Backup.Encrypt(json, pw);
                    // check the file reads back before telling the user it is saved
                    if (Backup.Parse(Backup.Decrypt(bytes, pw)).FirstAddress != W.Info.FirstAddress) throw new Exception(L.T("err.export"));
                    var tmp = target + ".tmp";
                    File.WriteAllBytes(tmp, bytes);
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(tmp, target);
                }, () =>
                {
                    CloseModal();
                    Info(L.T("bk.saved_title"), (height > 0 ? L.F("bk.saved_state", height.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " "))
                        : L.F("bk.saved_no_state", reason)) + "\n\n" + target + "\n\n" + L.T("bk.saved_next"));
                }, ex => { CloseModal(); Error(Msg(ex)); });
            });
            ok.IsDefault = true;
            no.Margin = new Thickness(0, 0, 10, 0);
            bp.Children.Add(no); bp.Children.Add(ok);
            sp.Children.Add(bp);
            ShowModal(sp, true, 600);
        }

        // ---------------------------------------------------------------- import (start screen)

        void ImportBackupFlow()
        {
            if (!NodeFiles.Installed) { DownloadNodeFlow(ImportBackupFlow); return; }
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = L.T("bk.import"),
                Filter = L.T("bk.filter") + " (*" + Backup.Extension + ")|*" + Backup.Extension + "|" + L.T("f.all_files") + " (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dlg.ShowDialog(this) != true) return;
            byte[] file;
            try
            {
                if (new FileInfo(dlg.FileName).Length > 32L * 1024 * 1024) throw new Exception(L.T("bk.not_backup"));
                file = File.ReadAllBytes(dlg.FileName);
            }
            catch (Exception ex) { Error(ex.Message); return; }
            if (!Backup.LooksLikeBackup(file)) { Error(L.T("bk.not_backup")); return; }
            AskPassword(Path.GetFileName(dlg.FileName), L.T("bk.import_pw"), pw =>
            {
                ShowProgress(L.T("bk.import"), L.T("bk.decrypting"));
                Backup.Contents c = null;
                bool phraseOk = false;
                Run(() => { c = Backup.Parse(Backup.Decrypt(file, pw)); phraseOk = WalletService.CheckMnemonic(c.Mnemonic); }, () =>
                {
                    CloseModal();
                    if (!phraseOk) { Error(L.T("err.213")); return; }
                    ShowPasswordStep(L.T("bk.import"), BackupSummary(c), (path, walletPw, t) =>
                        DoOpen(new OpenRequest { Kind = OpenKind.Restore, File = path, Password = walletPw, Mnemonic = c.Mnemonic,
                            MnemonicPassword = c.MnemonicPassword ?? "", CreationTs = c.ScanFrom, AddressCount = c.AddressCount, ExpectFirstAddress = c.FirstAddress }));
                }, ex => { CloseModal(); Error(Msg(ex)); });
            });
        }

        UIElement BackupSummary(Backup.Contents c)
        {
            var lines = new List<string>();
            if (c.FirstAddress.Length > 0) lines.Add(L.T("bk.sum_address") + ": " + Short(c.FirstAddress));
            if (c.SavedAt > 0) lines.Add(L.T("bk.sum_saved") + ": " + Date(c.SavedAt) + (c.CreatedBy.Length > 0 ? " · " + c.CreatedBy : ""));
            lines.Add(L.T("bk.sum_addresses") + ": " + c.AddressCount.ToString(CultureInfo.InvariantCulture));
            lines.Add(c.ScanFrom > 0 ? L.F("bk.sum_scan", Date(c.ScanFrom)) : L.T("bk.sum_scan_all"));
            var t = U.T(string.Join("\n", lines), 12, "Text");
            var b = new Border { CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 14, 0, 0), Child = t };
            b.SetResourceReference(Border.BackgroundProperty, "Input");
            b.SetResourceReference(Border.BorderBrushProperty, "Line");
            return b;
        }
    }
}
