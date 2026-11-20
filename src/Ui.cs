using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BcnWallet
{
    // Small factory helpers so the code-built UI stays readable. Colours are always resource references,
    // so switching the theme repaints everything.
    public static class U
    {
        public static readonly FontFamily Mono = new FontFamily("Cascadia Mono, Consolas");

        public const string IcMining = "M3 3v18h18M7 15l4-4 3 3 6-6";
        public const string IcPool = "M4.5 4h15a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 9.5v-4A1.5 1.5 0 0 1 4.5 4zM4.5 13h15a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 18.5v-4A1.5 1.5 0 0 1 4.5 13zM7 7.5h.01M7 16.5h.01";
        public const string IcChip = "M8 6h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2zM9 2v4M15 2v4M9 18v4M15 18v4M2 9h4M2 15h4M18 9h4M18 15h4";
        public const string IcLog = "M4 6h16M4 10h16M4 14h10M4 18h7";
        public const string IcSettings = "M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12M16 4a2 2 0 1 1 0 4a2 2 0 1 1 0-4zM10 10a2 2 0 1 1 0 4a2 2 0 1 1 0-4zM18 16a2 2 0 1 1 0 4a2 2 0 1 1 0-4z";
        public const string IcPause = "M9 5v14M15 5v14";
        public const string IcPlay = "M7 4.5l12 7.5-12 7.5z";
        public const string IcStop = "M7 7h10v10H7z";
        public const string IcFolder = "M3 6.5A1.5 1.5 0 0 1 4.5 5H9l2 2h8.5A1.5 1.5 0 0 1 21 8.5v9a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 17.5z";
        public const string IcCopy = "M9 9h10v10H9zM5 15V5h10";
        public const string IcRefresh = "M20 11a8 8 0 1 0-2.3 5.7M20 4v7h-7";
        public const string IcGlobe = "M12 3a9 9 0 1 1 0 18a9 9 0 1 1 0-18zM3 12h18M12 3c2.5 2.6 3.8 5.6 3.8 9s-1.3 6.4-3.8 9M12 3c-2.5 2.6-3.8 5.6-3.8 9s1.3 6.4 3.8 9";
        public const string IcCode = "M8 7l-5 5 5 5M16 7l5 5-5 5M14 4l-4 16";

        // wallet icons from the design canvas (24x24 stroke paths)
        public const string IcHome = "M3 11l9-7 9 7v9a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z";
        public const string IcSend = "M7 17L17 7M8 7h9v9";
        public const string IcReceive = "M17 7L7 17M16 17H7V8";
        public const string IcHistory = "M12 3a9 9 0 1 1 0 18a9 9 0 1 1 0-18zM12 7v5l3 2";
        public const string IcBook = "M5 4h12a2 2 0 0 1 2 2v14H7a2 2 0 0 1-2-2zM5 18a2 2 0 0 1 2-2h12";
        public const string IcPeople = "M9 11a4 4 0 1 0 0-8a4 4 0 0 0 0 8zM2 21v-1a6 6 0 0 1 6-6h2a6 6 0 0 1 6 6v1M16 3.5a4 4 0 0 1 0 7.5M22 21v-1a6 6 0 0 0-4-5.6";
        public const string IcShield = "M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6zM9 12l2 2 4-4";
        public const string IcLock = "M6 11h12v10H6zM8 11V7a4 4 0 0 1 8 0v4";
        public const string IcKey = "M14 10a4 4 0 1 0-1.2 2.9L21 21M17 17l2-2M19 19l2-2";
        public const string IcPlus = "M12 5v14M5 12h14";
        public const string IcEye = "M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12zM12 9a3 3 0 1 1 0 6a3 3 0 1 1 0-6z";
        public const string IcDownload = "M12 4v11M7 10l5 5 5-5M5 20h14";
        public const string IcExternal = "M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5";
        public const string IcTrash = "M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3";
        public const string IcPhone = "M8 2.5h8a1.5 1.5 0 0 1 1.5 1.5v16a1.5 1.5 0 0 1-1.5 1.5H8A1.5 1.5 0 0 1 6.5 20V4A1.5 1.5 0 0 1 8 2.5zM11 18h2";
        public const string IcChat ="M4 5h16v11H9l-5 4z";
        public const string IcServer = "M4.5 4h15a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 9.5v-4A1.5 1.5 0 0 1 4.5 4zM4.5 13h15a1.5 1.5 0 0 1 1.5 1.5v4a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 18.5v-4A1.5 1.5 0 0 1 4.5 13zM7 7.5h.01M7 16.5h.01";

        public static PasswordBox Pass(string accessibleName)
        {
            var p = new PasswordBox { Style = St("Pass") };
            AutomationProperties.SetName(p, accessibleName);
            return p;
        }

        public static void OpenUrl(string url)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        // A text link in the accent colour that opens a web page.
        public static TextBlock Link(string text, string url, double size)
        {
            var h = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(text)) { NavigateUri = new Uri(url), TextDecorations = null, Cursor = System.Windows.Input.Cursors.Hand };
            h.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Link");
            h.RequestNavigate += (a, b) => { OpenUrl(url); b.Handled = true; };
            h.MouseEnter += (a, b) => h.TextDecorations = TextDecorations.Underline;
            h.MouseLeave += (a, b) => h.TextDecorations = null;
            var t = new TextBlock { FontSize = size, ToolTip = url };
            t.Inlines.Add(h);
            AutomationProperties.SetName(t, text);
            return t;
        }

        // Same look as Link, but runs an action inside the app.
        public static TextBlock ActionLink(string text, Action click, double size)
        {
            var h = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(text)) { TextDecorations = null, Cursor = System.Windows.Input.Cursors.Hand };
            h.SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "Link");
            h.Click += (a, b) => click();
            h.MouseEnter += (a, b) => h.TextDecorations = TextDecorations.Underline;
            h.MouseLeave += (a, b) => h.TextDecorations = null;
            var t = new TextBlock { FontSize = size, VerticalAlignment = VerticalAlignment.Center };
            t.Inlines.Add(h);
            AutomationProperties.SetName(t, text);
            return t;
        }

        public static Style St(string key) { return (Style)Application.Current.FindResource(key); }

        public static TextBlock T(string text, double size, string fg, bool bold = false)
        {
            var t = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap };
            if (bold) t.FontWeight = FontWeights.SemiBold;
            t.SetResourceReference(TextBlock.ForegroundProperty, fg);
            return t;
        }

        public static TextBlock H1(string text)
        {
            var t = T(text, 24, "Text");
            t.FontWeight = FontWeights.Bold;
            t.VerticalAlignment = VerticalAlignment.Center;
            return t;
        }

        public static TextBlock H2(string text)
        {
            var t = T(text, 15, "Text", true);
            return t;
        }

        public static Border Card(UIElement child, string bg = "Card")
        {
            var b = new Border { CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(18, 16, 18, 16), Child = child };
            b.SetResourceReference(Border.BackgroundProperty, bg);
            b.SetResourceReference(Border.BorderBrushProperty, "Line");
            return b;
        }

        // Vertical stack with a fixed gap between children.
        public static StackPanel V(double gap, params UIElement[] items)
        {
            var sp = new StackPanel();
            foreach (var i in items) Add(sp, i, gap);
            return sp;
        }

        public static void Add(StackPanel sp, UIElement e, double gap)
        {
            var fe = e as FrameworkElement;
            if (fe != null && sp.Children.Count > 0)
            {
                var m = fe.Margin;
                if (sp.Orientation == Orientation.Vertical) fe.Margin = new Thickness(m.Left, m.Top + gap, m.Right, m.Bottom);
                else fe.Margin = new Thickness(m.Left + gap, m.Top, m.Right, m.Bottom);
            }
            sp.Children.Add(e);
        }

        public static StackPanel H(double gap, params UIElement[] items)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var i in items) Add(sp, i, gap);
            return sp;
        }

        public static FrameworkElement Icon(string data, double size = 18)
        {
            var p = new Path
            {
                Data = Geometry.Parse(data),
                StrokeThickness = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            p.SetBinding(Shape.StrokeProperty, new Binding("Foreground") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Control), 1) });
            var c = new Canvas { Width = 24, Height = 24 };
            c.Children.Add(p);
            return new Viewbox { Width = size, Height = size, Child = c, VerticalAlignment = VerticalAlignment.Center };
        }

        public static Button Btn(string text, string style, RoutedEventHandler click, string icon = null)
        {
            var b = new Button { Style = St(style) };
            if (icon == null) b.Content = text;
            else
            {
                var sp = H(8, Icon(icon, 16), new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
                b.Content = sp;
                AutomationProperties.SetName(b, text);
            }
            if (click != null) b.Click += click;
            return b;
        }

        public static void SetBtnText(Button b, string text, string icon)
        {
            if (icon == null) { b.Content = text; return; }
            b.Content = H(8, Icon(icon, 16), new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            AutomationProperties.SetName(b, text);
        }

        public static TextBox Field(string value, string accessibleName)
        {
            var t = new TextBox { Style = St("Field"), Text = value ?? "" };
            AutomationProperties.SetName(t, accessibleName);
            return t;
        }

        public static StackPanel Labeled(string label, FrameworkElement input, string hint = null)
        {
            var sp = V(5, T(label, 12, "Muted"), input);
            if (hint != null)
            {
                var h = T(hint, 11, "Muted");
                h.Margin = new Thickness(0, 5, 0, 0);
                sp.Children.Add(h);
            }
            return sp;
        }

        public static CheckBox Switch(string title, string desc, bool value, Action<bool> changed)
        {
            var content = new StackPanel();
            content.Children.Add(T(title, 14, "Text"));
            if (desc != null)
            {
                var d = T(desc, 12, "Muted");
                d.Margin = new Thickness(0, 3, 0, 0);
                content.Children.Add(d);
            }
            var cb = new CheckBox { Style = St("Switch"), Content = content, IsChecked = value };
            AutomationProperties.SetName(cb, title);
            cb.Checked += (a, b) => changed(true);
            cb.Unchecked += (a, b) => changed(false);
            return cb;
        }

        // Segmented control (radio buttons in a rounded box).
        public static Border Segmented(string group, string[] labels, string[] values, string current, Action<string> changed, out RadioButton[] buttons, int columns = 0)
        {
            int cols = columns > 0 ? columns : labels.Length;
            var grid = new UniformGrid { Columns = cols };
            buttons = new RadioButton[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var v = values[i];
                var r = new RadioButton { Style = St("Seg"), Content = labels[i], GroupName = group, IsChecked = v == current, Tag = v };
                r.Margin = new Thickness(i % cols > 0 ? 3 : 0, i >= cols ? 3 : 0, 0, 0);
                r.Checked += (a, b) => changed(v);
                buttons[i] = r;
                grid.Children.Add(r);
            }
            var box = new Border { CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), Padding = new Thickness(3), Child = grid };
            box.SetResourceReference(Border.BackgroundProperty, "SegBg");
            box.SetResourceReference(Border.BorderBrushProperty, "Line");
            return box;
        }

        public static void SelectSeg(RadioButton[] buttons, string value)
        {
            foreach (var b in buttons) b.IsChecked = (string)b.Tag == value;
        }

        public static Slider Range(double min, double max, double value)
        {
            return new Slider { Style = St("Range"), Minimum = min, Maximum = max, Value = value, SmallChange = 1, LargeChange = 1 };
        }

        public static Ellipse Dot(string brush, double size = 8)
        {
            var e = new Ellipse { Width = size, Height = size, VerticalAlignment = VerticalAlignment.Center };
            e.SetResourceReference(Shape.FillProperty, brush);
            return e;
        }

        // Status pill: coloured dot + label on a tinted background.
        public class Pill
        {
            public Border Root;
            public Ellipse Dot;
            public TextBlock Text;
            public void Set(string text, string fg, string dot, string bg)
            {
                Text.Text = text;
                Text.SetResourceReference(TextBlock.ForegroundProperty, fg);
                Dot.SetResourceReference(Shape.FillProperty, dot);
                Root.SetResourceReference(Border.BackgroundProperty, bg);
            }
        }

        public static Pill MakePill()
        {
            var p = new Pill();
            p.Dot = Dot("GoodDot");
            p.Text = T("", 13, "Good", true);
            p.Text.VerticalAlignment = VerticalAlignment.Center;
            p.Root = new Border { CornerRadius = new CornerRadius(999), Padding = new Thickness(12, 5, 12, 5), VerticalAlignment = VerticalAlignment.Center, Child = H(6, p.Dot, p.Text) };
            return p;
        }

        public static Border Badge(string text)
        {
            var t = T(text, 11, "Muted");
            var b = new Border { CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1), Padding = new Thickness(7, 2, 7, 2), Child = t, VerticalAlignment = VerticalAlignment.Center };
            b.SetResourceReference(Border.BorderBrushProperty, "Line");
            b.SetResourceReference(Border.BackgroundProperty, "Badge");
            return b;
        }

        // The Bytecoin coin logo from the design canvas (vector, 100x100 view box).
        public static FrameworkElement Logo(double size, bool fixedCoral = false)
        {
            var c = new Canvas { Width = 100, Height = 100 };
            var coin = new Ellipse { Width = 96, Height = 96 };
            Canvas.SetLeft(coin, 2); Canvas.SetTop(coin, 2);
            var ring = new Ellipse { Width = 84.5, Height = 84.5, StrokeThickness = 3.5 };
            Canvas.SetLeft(ring, 7.75); Canvas.SetTop(ring, 7.75);
            var b = new Path { Data = Geometry.Parse("F0 M34 25H59C69 25 74 31 74 38C74 43 71.5 46.5 67.5 48.5C73.5 50.5 77 55.5 77 62C77 71 70.5 76 60.5 76H34ZM44.5 34V44H57C60.5 44 62.5 42 62.5 39C62.5 36 60.5 34 57 34ZM44.5 54.5V67H59C63 67 65.5 64.5 65.5 60.75C65.5 57 63 54.5 59 54.5Z") };
            var bar = new Rectangle { Width = 20, Height = 7.5 };
            Canvas.SetLeft(bar, 20); Canvas.SetTop(bar, 46.5);
            if (fixedCoral)
            {
                var coral = new SolidColorBrush(Color.FromRgb(0xFF, 0x6A, 0x55));
                var navy = new SolidColorBrush(Color.FromRgb(0x10, 0x1B, 0x28));
                coin.Fill = coral; ring.Stroke = navy; b.Fill = navy; bar.Fill = navy;
            }
            else
            {
                coin.SetResourceReference(Shape.FillProperty, "CoinFill");
                ring.SetResourceReference(Shape.StrokeProperty, "CoinInk");
                b.SetResourceReference(Shape.FillProperty, "CoinInk");
                bar.SetResourceReference(Shape.FillProperty, "CoinInk");
            }
            c.Children.Add(coin); c.Children.Add(ring); c.Children.Add(b); c.Children.Add(bar);
            return new Viewbox { Width = size, Height = size, Child = c, VerticalAlignment = VerticalAlignment.Center };
        }

        public static ScrollViewer Scroll(UIElement content)
        {
            return new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false };
        }

        public static Grid Columns(params object[] widthsAndChildren)
        {
            // Columns("1.55*", a, 14, null, "1*", b): string = star width, number = fixed gap column
            var g = new Grid();
            int col = 0;
            for (int i = 0; i < widthsAndChildren.Length; i += 2)
            {
                var w = widthsAndChildren[i];
                GridLength gl;
                if (w is string)
                {
                    var s = (string)w;
                    if (s == "auto") gl = GridLength.Auto;
                    else gl = new GridLength(double.Parse(s.TrimEnd('*'), CultureInfo.InvariantCulture), GridUnitType.Star);
                }
                else gl = new GridLength(Convert.ToDouble(w, CultureInfo.InvariantCulture));
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = gl });
                var child = i + 1 < widthsAndChildren.Length ? widthsAndChildren[i + 1] as UIElement : null;
                if (child != null) { Grid.SetColumn(child, col); g.Children.Add(child); }
                col++;
            }
            return g;
        }

        public static void Colorize(TextBlock tb, string line)
        {
            // Engine log line: highlight accepted / rejected like the design's journal.
            tb.Inlines.Clear();
            string[] marks = { "accepted", "rejected", "new job", "error", "failed" };
            int pos = -1; string mark = null;
            foreach (var m in marks)
            {
                int p = line.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                if (p >= 0 && (pos < 0 || p < pos)) { pos = p; mark = m; }
            }
            if (pos < 0) { tb.Inlines.Add(new Run(line)); return; }
            tb.Inlines.Add(new Run(line.Substring(0, pos)));
            var r = new Run(line.Substring(pos, mark.Length));
            r.SetResourceReference(TextElement.ForegroundProperty, mark == "accepted" ? "Good" : (mark == "new job" ? "Text" : "Bad"));
            tb.Inlines.Add(r);
            tb.Inlines.Add(new Run(line.Substring(pos + mark.Length)));
        }
    }
}
