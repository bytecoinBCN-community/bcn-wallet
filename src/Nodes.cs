using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace BcnWallet
{
    public class KnownNode
    {
        public string Url;
        public string Name;
        public string Operator;     // L key
        public KnownNode(string url, string name, string op) { Url = url; Name = name; Operator = op; }
    }

    public class NodeStatus
    {
        public string Url;
        public bool Ok;
        public long Ms;
        public long Height, Known;
        public int Peers;
        public string RefHash = ""; // hash of the block at the common reference height
        public string Error = "";
        public string Verdict = ""; // "ok" | "behind" | "fork" | "down"
    }

    // Public bytecoind nodes the wallet can use without the blockchain, their health and agreement,
    // and the check of a local blockchain against them.
    public static class Nodes
    {
        // Nodes with an open JSON-RPC (checked 2026-11-15). Other nodes found by the network scan have RPC closed.
        public static readonly KnownNode[] Known = {
            new KnownNode(WalletSettings.CommunityNode, "node.bytecoin.community", "nodes.op_community"),
            new KnownNode("https://node01.bytecoin.org:8091", "node01.bytecoin.org", "nodes.op_bcndev"),
            new KnownNode("https://node02.bytecoin.org:8091", "node02.bytecoin.org", "nodes.op_bcndev"),
        };

        public const int MaxLag = 5;            // blocks behind the best node before it counts as lagging

        // walletd accepts "host:port", "http://host:port" and "https://host:port"
        public static string Normalize(string s)
        {
            s = (s ?? "").Trim().TrimEnd('/');
            if (s.EndsWith("/json_rpc", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 9);
            if (s.Length == 0) return null;
            if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) s = "http://" + s;
            Uri u;
            if (!Uri.TryCreate(s, UriKind.Absolute, out u) || u.Host.Length == 0) return null;
            int port = u.IsDefaultPort ? (u.Scheme == "https" ? 443 : 8081) : u.Port;
            return u.Scheme + "://" + u.Host + ":" + port.ToString(CultureInfo.InvariantCulture);
        }

        static Dictionary<string, object> Call(string url, string method, object prms, int timeoutMs)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var ser = new JavaScriptSerializer();
            var body = Encoding.UTF8.GetBytes(ser.Serialize(new Dictionary<string, object> {
                { "jsonrpc", "2.0" }, { "id", "1" }, { "method", method }, { "params", prms ?? new Dictionary<string, object>() } }));
            var req = (HttpWebRequest)WebRequest.Create(url + "/json_rpc");
            req.Method = "POST"; req.ContentType = "application/json"; req.Timeout = timeoutMs; req.ReadWriteTimeout = timeoutMs;
            req.UserAgent = "bcn-wallet/" + App.Version; req.KeepAlive = false;
            using (var s = req.GetRequestStream()) s.Write(body, 0, body.Length);
            using (var resp = req.GetResponse())
            using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                var obj = ser.DeserializeObject(r.ReadToEnd()) as Dictionary<string, object>;
                var err = J.Obj(obj, "error");
                if (err != null) throw new RpcException((int)J.Long(err, "code"), J.Str(err, "message"));
                return J.Obj(obj, "result") ?? new Dictionary<string, object>();
            }
        }

        public static string BlockHash(string url, long height, int timeoutMs = 8000)
        {
            var r = Call(url, "get_block_header", new Dictionary<string, object> { { "height_or_depth", height } }, timeoutMs);
            return J.Str(J.Obj(r, "block_header"), "hash");
        }

        // Probes all nodes in parallel, then compares the hash of one block a few blocks below the lowest top:
        // a node whose hash differs from the majority is on another chain (fork / island) and must not be used.
        public static List<NodeStatus> Survey(IEnumerable<string> urls)
        {
            var list = urls.Select(Normalize).Where(u => u != null).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(u => new NodeStatus { Url = u }).ToList();
            Parallel(list, n =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var st = Call(n.Url, "get_status", null, 6000);
                    n.Ms = sw.ElapsedMilliseconds;
                    n.Height = J.Long(st, "top_block_height");
                    n.Known = J.Long(st, "top_known_block_height");
                    n.Peers = (int)(J.Long(st, "outgoing_peer_count") + J.Long(st, "incoming_peer_count"));
                    n.Ok = n.Height > 0;
                    if (!n.Ok) n.Error = "height 0";
                }
                catch (Exception ex) { n.Error = ex.Message; }
            });
            var up = list.Where(n => n.Ok).ToList();
            if (up.Count > 0)
            {
                long best = up.Max(n => n.Height);
                long refH = up.Where(n => n.Height + MaxLag >= best).Min(n => n.Height) - 6;
                Parallel(up, n => { try { n.RefHash = BlockHash(n.Url, Math.Min(refH, n.Height - 1), 6000); } catch (Exception ex) { n.Error = ex.Message; } });
                var majority = up.Where(n => n.RefHash.Length > 0 && n.Height + MaxLag >= best)
                    .GroupBy(n => n.RefHash).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "";
                foreach (var n in up)
                    n.Verdict = n.Height + MaxLag < best ? "behind" : (n.RefHash.Length > 0 && n.RefHash != majority && up.Count > 1) ? "fork" : "ok";
            }
            foreach (var n in list) if (!n.Ok) n.Verdict = "down";
            return list;
        }

        // Auto choice: a random healthy node among those not much slower than the fastest. Spreading wallets
        // over several nodes keeps any single public node from becoming a bottleneck or a single point of failure.
        public static string PickAuto(List<NodeStatus> survey, ICollection<string> exclude)
        {
            var good = survey.Where(n => n.Verdict == "ok" && (exclude == null || !exclude.Contains(n.Url))).ToList();
            if (good.Count == 0) good = survey.Where(n => n.Verdict == "ok").ToList();
            if (good.Count == 0) return null;
            long fastest = good.Min(n => n.Ms);
            var band = good.Where(n => n.Ms <= Math.Max(fastest * 2, fastest + 400)).ToList();
            return band[new Random().Next(band.Count)].Url;
        }

        static void Parallel<T>(List<T> items, Action<T> work)
        {
            var threads = items.Select(it => new Thread(() => { try { work(it); } catch { } }) { IsBackground = true }).ToList();
            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join(20000);
        }

        // ------------------------------------------------------------------ local blockchain check

        public class ChainReport
        {
            public long LocalHeight;
            public int Checked, Matched, Compared;
            public List<string> Mismatch = new List<string>();
            public List<string> Sources = new List<string>();
        }

        // Compares the local node's block hashes at heights spread over the whole chain (genesis, every ~500 000 blocks,
        // and near the top) with the public nodes. Matching hashes at many heights mean the local database is on the same
        // chain as the rest of the network; bytecoind itself validates every block it adds from now on.
        public static ChainReport CheckLocalChain(string localUrl, IEnumerable<string> refs)
        {
            var rep = new ChainReport();
            var st = Call(localUrl, "get_status", null, 8000);
            rep.LocalHeight = J.Long(st, "top_block_height");
            var survey = Survey(refs).Where(n => n.Verdict == "ok").ToList();
            rep.Sources = survey.Select(n => n.Url).ToList();
            if (survey.Count == 0 || rep.LocalHeight < 1) return rep;
            var heights = new List<long> { 0, 1, 1000 };
            for (long h = 500000; h < rep.LocalHeight - 10; h += 500000) heights.Add(h);
            heights.Add(Math.Max(0, rep.LocalHeight - 10));
            foreach (var h in heights.Distinct())
            {
                rep.Checked++;
                string local;
                try { local = BlockHash(localUrl, h); } catch { rep.Mismatch.Add(h.ToString("N0", CultureInfo.InvariantCulture) + ": local error"); continue; }
                var remote = new List<string>();
                foreach (var n in survey) { try { remote.Add(BlockHash(n.Url, h)); } catch { } }
                if (remote.Count == 0) continue;
                rep.Compared++;
                if (remote.All(x => x == local)) rep.Matched++;
                else rep.Mismatch.Add(h.ToString("N0", CultureInfo.InvariantCulture));
            }
            return rep;
        }
    }
}
