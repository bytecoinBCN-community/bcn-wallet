using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace BcnWallet
{
    // UI strings. Each language is an embedded "lang/<code>.txt" with "key = value" lines
    // (same idea as the site's i18n/*.txt). Missing keys fall back to English, then to the key itself.
    // A translation can be overridden or added without rebuilding: put <code>.txt into a "lang" folder next to the exe.
    public static class L
    {
        public static readonly string[] Codes = { "uk", "en", "es", "pt", "tr", "id", "vi", "zh" };
        public static readonly string[] Names = { "Українська", "English", "Español", "Português", "Türkçe", "Bahasa Indonesia", "Tiếng Việt", "中文" };

        static Dictionary<string, string> cur = new Dictionary<string, string>();
        static Dictionary<string, string> en = new Dictionary<string, string>();
        public static string Code { get; private set; }

        public static string FromSystem()
        {
            string two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return Array.IndexOf(Codes, two) >= 0 ? two : "en";
        }

        public static void Set(string setting)
        {
            string code = setting == null || setting == "system" || Array.IndexOf(Codes, setting) < 0 ? FromSystem() : setting;
            en = Load("en");
            cur = code == "en" ? en : Load(code);
            Code = code;
            try
            {
                var ci = CultureInfo.GetCultureInfo(code == "zh" ? "zh-CN" : code);
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
            catch { }
        }

        static Dictionary<string, string> Load(string code)
        {
            var d = new Dictionary<string, string>();
            string text = null;
            try
            {
                using (var st = Assembly.GetExecutingAssembly().GetManifestResourceStream("BcnWallet.lang." + code + ".txt"))
                    if (st != null) using (var r = new StreamReader(st, Encoding.UTF8)) text = r.ReadToEnd();
            }
            catch { }
            Parse(text, d);
            try
            {
                var local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang", code + ".txt");
                if (File.Exists(local)) Parse(File.ReadAllText(local, Encoding.UTF8), d);
            }
            catch { }
            return d;
        }

        static void Parse(string text, Dictionary<string, string> d)
        {
            if (text == null) return;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                d[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
            }
        }

        public static string T(string key)
        {
            string v;
            if (cur.TryGetValue(key, out v) && v.Length > 0) return v;
            if (en.TryGetValue(key, out v) && v.Length > 0) return v;
            return key;
        }

        public static string F(string key, params object[] args)
        {
            try { return string.Format(T(key), args); } catch { return T(key); }
        }
    }
}
