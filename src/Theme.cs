using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace BcnWallet
{
    // Colours from the "Bytecoin Desktop Suite" design canvas (dark + light themes). The balance card ("Hero") stays navy in both.
    public static class Theme
    {
        static readonly Dictionary<string, string> Dark = new Dictionary<string, string> {
            {"Bg", "#142232"}, {"Nav", "#101B28"}, {"Card", "#101B28"}, {"NavActive", "#1D2F43"}, {"NavHover", "#172839"},
            {"Line", "#2B3F55"}, {"Text", "#EEF1F4"}, {"Muted", "#A9B6C3"}, {"Accent", "#FF6A55"}, {"Link", "#FF6A55"},
            {"Good", "#3DDC97"}, {"GoodDot", "#3DDC97"}, {"GoodBg", "#1F3DDC97"},
            {"Warn", "#F5B84B"}, {"WarnBg", "#1FF5B84B"}, {"Bad", "#FF7A66"}, {"BadBg", "#1FFF6A55"},
            {"Title", "#0B131D"}, {"LogBg", "#0B131D"}, {"Grid", "#1D2F43"}, {"Input", "#142232"}, {"InputLine", "#2B3F55"},
            {"SegBg", "#142232"}, {"SegActive", "#1D2F43"}, {"BtnBg", "#00000000"}, {"BtnHover", "#1D2F43"},
            {"Track", "#1D2F43"}, {"Knob", "#EEF1F4"}, {"SwitchOff", "#2B3F55"}, {"CapHover", "#1D2F43"},
            {"ChartLine", "#FF6A55"}, {"CoinFill", "#FF6A55"}, {"CoinInk", "#101B28"}, {"Scroll", "#2B3F55"},
            {"Hero", "#101B28"}, {"HeroText", "#EEF1F4"}, {"HeroMuted", "#A9B6C3"}, {"HeroInner", "#142232"}, {"HeroLine", "#2B3F55"}, {"HeroBtn", "#1D2F43"}, {"Overlay", "#B3060B11"},
            {"Badge", "#1D2F43"}, {"OutBg", "#24FF6A55"}, {"Out", "#FF8A78"}
        };

        static readonly Dictionary<string, string> Light = new Dictionary<string, string> {
            {"Bg", "#FAF9F7"}, {"Nav", "#F5F3ED"}, {"Card", "#FFFFFF"}, {"NavActive", "#FFFFFF"}, {"NavHover", "#ECE9E1"},
            {"Line", "#E0DBCF"}, {"Text", "#17222E"}, {"Muted", "#566473"}, {"Accent", "#FF6A55"}, {"Link", "#B93A29"},
            {"Good", "#17804F"}, {"GoodDot", "#2FBF7F"}, {"GoodBg", "#242FBF7F"},
            {"Warn", "#8A5A00"}, {"WarnBg", "#2EF5B84B"}, {"Bad", "#B93A29"}, {"BadBg", "#1FFF6A55"},
            {"Title", "#ECE9E1"}, {"LogBg", "#F5F3ED"}, {"Grid", "#EEEAE0"}, {"Input", "#FFFFFF"}, {"InputLine", "#D3CDBF"},
            {"SegBg", "#F5F3ED"}, {"SegActive", "#FFFFFF"}, {"BtnBg", "#FFFFFF"}, {"BtnHover", "#F5F3ED"},
            {"Track", "#EBE7DD"}, {"Knob", "#FFFFFF"}, {"SwitchOff", "#D3CDBF"}, {"CapHover", "#E0DBCF"},
            {"ChartLine", "#E5533E"}, {"CoinFill", "#101B28"}, {"CoinInk", "#F5F3ED"}, {"Scroll", "#D3CDBF"},
            {"Hero", "#101B28"}, {"HeroText", "#EEF1F4"}, {"HeroMuted", "#A9B6C3"}, {"HeroInner", "#142232"}, {"HeroLine", "#2B3F55"}, {"HeroBtn", "#1D2F43"}, {"Overlay", "#B3060B11"},
            {"Badge", "#F5F3ED"}, {"OutBg", "#24FF6A55"}, {"Out", "#B93A29"}
        };

        public static bool IsLight { get; private set; }

        public static bool SystemIsLight()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (k != null)
                    {
                        object v = k.GetValue("AppsUseLightTheme");
                        if (v is int) return (int)v != 0;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void Apply(string mode)
        {
            IsLight = mode == "light" || (mode != "dark" && SystemIsLight());
            var pal = IsLight ? Light : Dark;
            var d = new ResourceDictionary();
            foreach (var kv in pal)
            {
                var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(kv.Value));
                b.Freeze();
                d[kv.Key] = b;
            }
            var md = Application.Current.Resources.MergedDictionaries;
            if (md.Count > 1) md[1] = d; else md.Add(d);
        }

        public static Brush Get(string key)
        {
            var b = Application.Current.TryFindResource(key) as Brush;
            return b ?? Brushes.Gray;
        }
    }
}
