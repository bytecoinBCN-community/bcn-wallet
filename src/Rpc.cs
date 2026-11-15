using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace BcnWallet
{
    public class RpcException : Exception
    {
        public int Code;
        public RpcException(int code, string message) : base(message) { Code = code; }
    }

    // Minimal JSON-RPC 2.0 client for walletd / bytecoind (POST /json_rpc, HTTP Basic auth).
    public class Rpc
    {
        readonly string url;
        readonly string auth;
        int id;

        public Rpc(string hostPort, string userPass)
        {
            url = "http://" + hostPort + "/json_rpc";
            auth = string.IsNullOrEmpty(userPass) ? null : Convert.ToBase64String(Encoding.ASCII.GetBytes(userPass));
        }

        public Dictionary<string, object> Call(string method, object prms, int timeoutMs = 30000)
        {
            var body = new Dictionary<string, object> {
                { "jsonrpc", "2.0" }, { "id", (++id).ToString(CultureInfo.InvariantCulture) }, { "method", method },
                { "params", prms ?? new Dictionary<string, object>() } };
            var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var bytes = Encoding.UTF8.GetBytes(ser.Serialize(body));
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.Proxy = null;
            req.KeepAlive = false;
            if (auth != null) req.Headers["Authorization"] = "Basic " + auth;
            using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
            string text;
            try
            {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) text = r.ReadToEnd();
            }
            catch (WebException ex)
            {
                // walletd answers JSON-RPC errors with HTTP 200, but be tolerant of other servers
                if (ex.Response == null) throw;
                using (var r = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8)) text = r.ReadToEnd();
                if (string.IsNullOrWhiteSpace(text) || text.TrimStart()[0] != '{') throw;
            }
            var obj = ser.DeserializeObject(text) as Dictionary<string, object>;
            if (obj == null) throw new RpcException(-32700, "invalid JSON");
            object err;
            if (obj.TryGetValue("error", out err) && err is Dictionary<string, object>)
            {
                var e = (Dictionary<string, object>)err;
                throw new RpcException((int)J.Long(e, "code"), J.Str(e, "message"));
            }
            object res;
            if (obj.TryGetValue("result", out res) && res is Dictionary<string, object>) return (Dictionary<string, object>)res;
            return new Dictionary<string, object>();
        }
    }

    // Accessors for JavaScriptSerializer output.
    public static class J
    {
        public static long Long(Dictionary<string, object> d, string k)
        {
            object v;
            if (d == null || !d.TryGetValue(k, out v) || v == null) return 0;
            try { return Convert.ToInt64(v, CultureInfo.InvariantCulture); } catch { return 0; }
        }

        public static string Str(Dictionary<string, object> d, string k)
        {
            object v;
            if (d == null || !d.TryGetValue(k, out v) || v == null) return "";
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static bool Bool(Dictionary<string, object> d, string k)
        {
            object v;
            return d != null && d.TryGetValue(k, out v) && v is bool && (bool)v;
        }

        public static Dictionary<string, object> Obj(Dictionary<string, object> d, string k)
        {
            object v;
            if (d != null && d.TryGetValue(k, out v)) return v as Dictionary<string, object>;
            return null;
        }

        public static List<Dictionary<string, object>> Arr(Dictionary<string, object> d, string k)
        {
            var r = new List<Dictionary<string, object>>();
            object v;
            if (d != null && d.TryGetValue(k, out v) && v is IEnumerable && !(v is string))
                foreach (var x in (IEnumerable)v) { var o = x as Dictionary<string, object>; if (o != null) r.Add(o); }
            return r;
        }

        public static List<string> StrArr(Dictionary<string, object> d, string k)
        {
            var r = new List<string>();
            object v;
            if (d != null && d.TryGetValue(k, out v) && v is IEnumerable && !(v is string))
                foreach (var x in (IEnumerable)v) if (x != null) r.Add(x.ToString());
            return r;
        }
    }

    // BCN amounts: 1 BCN = 10^8 atomic units.
    public static class Amount
    {
        public const long Coin = 100000000;

        static NumberFormatInfo Nfi()
        {
            var n = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            n.NumberGroupSeparator = " "; // narrow no-break space, like the design
            return n;
        }

        // 1234500000 -> "12 345.00" (at least 2 decimals, at most 8, trailing zeros trimmed)
        public static string Format(long atomic, bool sign = false)
        {
            bool neg = atomic < 0;
            ulong a = neg ? (ulong)(-(atomic + 1)) + 1 : (ulong)atomic;
            ulong whole = a / Coin, frac = a % Coin;
            string f = frac.ToString("D8").TrimEnd('0');
            if (f.Length < 2) f = f.PadRight(2, '0');
            string s = whole.ToString("#,0", Nfi()) + "." + f;
            if (neg) return "−" + s;
            return sign && atomic > 0 ? "+" + s : s;
        }

        // Accepts "12345.67", "12 345,67", "0.00000001". Returns false on bad input or more than 8 decimals.
        public static bool TryParse(string text, out long atomic)
        {
            atomic = 0;
            if (text == null) return false;
            var t = text.Replace(" ", "").Replace(" ", "").Replace(" ", "").Replace("_", "").Trim();
            if (t.IndexOf(',') >= 0 && t.IndexOf('.') < 0) t = t.Replace(',', '.');
            else t = t.Replace(",", "");
            if (t.Length == 0) return false;
            var parts = t.Split('.');
            if (parts.Length > 2) return false;
            foreach (var p in parts) foreach (char c in p) if (c < '0' || c > '9') return false;
            if (parts[0].Length == 0 && (parts.Length < 2 || parts[1].Length == 0)) return false;
            string frac = parts.Length > 1 ? parts[1] : "";
            if (frac.Length > 8) return false;
            ulong whole;
            if (!ulong.TryParse(parts[0].Length == 0 ? "0" : parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out whole)) return false;
            if (whole > 184467440737UL) return false;
            ulong f = frac.Length == 0 ? 0 : ulong.Parse(frac.PadRight(8, '0'), CultureInfo.InvariantCulture);
            ulong total = whole * (ulong)Coin + f;
            if (total > long.MaxValue) return false;
            atomic = (long)total;
            return true;
        }

        public static string Plain(long atomic)
        {
            // for text fields: "12345.67" without group separators
            return Format(atomic).Replace(" ", "").Replace("−", "-");
        }
    }
}
