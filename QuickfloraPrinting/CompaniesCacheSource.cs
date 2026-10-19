using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace QuickfloraPrinting
{
    /// <summary>
    /// AB#3958: the first real cache source for EdgeSync. Pulls this terminal's Companies row from the
    /// hub's cache.ashx and stores it in cache_rows (table "Companies", key CompanyID|DivisionID|DepartmentID).
    /// Incremental by change fingerprint: the hub returns no rows when nothing changed since the
    /// version EdgeSync passes in. Shop-facing columns only; the hub never serves keys or card data.
    /// </summary>
    internal sealed class CompaniesCacheSource : ICacheSource
    {
        private readonly string hubUrl;
        private readonly string key;

        public CompaniesCacheSource(string hubUrl, string hubKey) { this.hubUrl = hubUrl; key = hubKey ?? ""; }

        public string Table { get { return "Companies"; } }

        public int Pull(long sinceRowversion, out long newRowversion)
        {
            string url = hubUrl + "/cache.ashx?table=Companies&c=" + Uri.EscapeDataString(Program.CompanyID ?? "")
                + "&d=" + Uri.EscapeDataString(Program.DivisionID ?? "") + "&p=" + Uri.EscapeDataString(Program.DepartmentID ?? "")
                + "&since=" + sinceRowversion + (key.Length > 0 ? "&k=" + Uri.EscapeDataString(key) : "");
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 20000;
            req.UserAgent = "QuickFloraPrintApp/" + Program.AppVersion;
            string json;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                json = r.ReadToEnd();
            JObject o = JObject.Parse(json);
            newRowversion = o.Value<long>("version");
            int n = 0;
            JArray rows = o["rows"] as JArray;
            if (rows != null)
            {
                foreach (JToken row in rows)
                {
                    string k = row.Value<string>("key");
                    long v = row.Value<long>("version");
                    JToken data = row["data"];
                    EdgeStore.UpsertCache(Table, k, data == null ? "{}" : data.ToString(Newtonsoft.Json.Formatting.None), v);
                    n++;
                }
            }
            return n;
        }
    }
}
