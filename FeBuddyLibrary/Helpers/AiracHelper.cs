using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FeBuddyLibrary.Helpers
{
    /// <summary>
    /// AIRAC cycle math + authoritative lookup via FAA APRA.
    ///
    /// Cycle dates are deterministic (28-day interval from ICAO epoch), so
    /// offline math is the primary source. APRA (external-api.faa.gov) is
    /// consulted as the authoritative cross-check and to confirm FAA has
    /// actually published a given product edition.
    /// </summary>
    public static class AiracHelper
    {
        // ICAO AIRAC epoch: cycle 2001 effective 2020-01-02.
        private static readonly DateTime Epoch =
            new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        private const int CycleDays = 28;

        private static readonly XNamespace ApraNs = "http://arpa.ait.faa.gov/arpa_response";

        private static HttpClient Http => SharedHttp.Client;

        public readonly struct AiracCycle
        {
            public AiracCycle(DateTime effective)
            {
                Effective = DateTime.SpecifyKind(effective.Date, DateTimeKind.Utc);
            }

            public DateTime Effective { get; }

            /// <summary>ICAO 4-digit cycle identifier, e.g. "2604" for the 4th cycle of 2026.</summary>
            public string Id
            {
                get
                {
                    var jan1 = new DateTime(Effective.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    int firstCycleOfYear = (int)Math.Ceiling((jan1 - Epoch).TotalDays / CycleDays);
                    int thisCycle = (int)((Effective - Epoch).TotalDays / CycleDays);
                    int ordinal = thisCycle - firstCycleOfYear + 1;
                    return $"{Effective.Year % 100:D2}{ordinal:D2}";
                }
            }

            /// <summary>FAA d-tpp directory name — the 4-digit cycle id.</summary>
            public string FaaDirectory => Id;

            public AiracCycle Next => new AiracCycle(Effective.AddDays(CycleDays));
            public AiracCycle Previous => new AiracCycle(Effective.AddDays(-CycleDays));

            public override string ToString() => $"{Id} ({Effective:yyyy-MM-dd})";
        }

        public static AiracCycle Current(DateTime? utcNow = null)
        {
            var now = (utcNow ?? DateTime.UtcNow).ToUniversalTime();
            int cycles = (int)((now - Epoch).TotalDays / CycleDays);
            return new AiracCycle(Epoch.AddDays(cycles * CycleDays));
        }

        public static AiracCycle Next(DateTime? utcNow = null) => Current(utcNow).Next;

        // ---------------------------------------------------------------
        // FAA APRA client
        // https://external-api.faa.gov/apra/{product}/info?edition={current|next}
        // ---------------------------------------------------------------

        public enum ApraEdition { Current, Next }

        /// <summary>
        /// A single product edition record returned by APRA.
        /// </summary>
        public readonly struct ApraEditionInfo
        {
            public ApraEditionInfo(string editionName, DateTime editionDate, int editionNumber,
                                   string geoname, string format)
            {
                EditionName = editionName;
                EditionDate = editionDate;
                EditionNumber = editionNumber;
                Geoname = geoname;
                Format = format;
            }

            public string EditionName { get; }
            public DateTime EditionDate { get; }
            public int EditionNumber { get; }
            public string Geoname { get; }
            public string Format { get; }

            /// <summary>4-digit AIRAC cycle id built from the edition date + number.</summary>
            public string CycleId =>
                $"{EditionDate.Year % 100:D2}{EditionNumber:D2}";

            public AiracCycle AsCycle() => new AiracCycle(EditionDate);
        }

        /// <summary>
        /// Queries APRA for an edition of a product (e.g. "dtpp", "cifp", "supp").
        /// Returns null if the API responded with a non-200 status attribute or the
        /// network call failed — callers should fall back to <see cref="Current"/>.
        /// </summary>
        public static async Task<ApraEditionInfo?> GetApraEditionAsync(
            string product, ApraEdition edition, CancellationToken ct = default)
        {
            var url = $"https://external-api.faa.gov/apra/{product}/info" +
                      $"?edition={(edition == ApraEdition.Current ? "current" : "next")}";
            try
            {
                var xml = await Http.GetStringAsync(url, ct).ConfigureAwait(false);
                var doc = XDocument.Parse(xml);
                var status = doc.Root?.Element(ApraNs + "status");
                if (status?.Attribute("code")?.Value != "200") return null;

                var ed = doc.Root?.Element(ApraNs + "edition");
                if (ed is null) return null;

                var date = DateTime.ParseExact(
                    ed.Element(ApraNs + "editionDate")!.Value,
                    "MM/dd/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                return new ApraEditionInfo(
                    editionName:   ed.Attribute("editionName")?.Value ?? "",
                    editionDate:   date,
                    editionNumber: int.Parse(ed.Element(ApraNs + "editionNumber")!.Value,
                                             CultureInfo.InvariantCulture),
                    geoname:       ed.Attribute("geoname")?.Value ?? "",
                    format:        ed.Attribute("format")?.Value ?? "");
            }
            catch (HttpRequestException) { return null; }
            catch (TaskCanceledException) { return null; }
            catch (System.Xml.XmlException) { return null; }
        }

        /// <summary>
        /// Authoritative current cycle from APRA's d-tpp feed, falling back to
        /// deterministic math if APRA is unreachable.
        /// </summary>
        public static async Task<AiracCycle> ResolveCurrentAsync(CancellationToken ct = default)
        {
            var apra = await GetApraEditionAsync("dtpp", ApraEdition.Current, ct).ConfigureAwait(false);
            return apra?.AsCycle() ?? Current();
        }

        public static async Task<AiracCycle> ResolveNextAsync(CancellationToken ct = default)
        {
            var apra = await GetApraEditionAsync("dtpp", ApraEdition.Next, ct).ConfigureAwait(false);
            return apra?.AsCycle() ?? Next();
        }

        /// <summary>
        /// Returns true only when the d-tpp Metafile for this cycle is
        /// actually hosted on aeronav.faa.gov. APRA announces the next
        /// edition ahead of publication, so checking APRA alone falsely
        /// greenlights downloads before files exist.
        /// </summary>
        public static async Task<bool> IsPublishedAsync(AiracCycle cycle, CancellationToken ct = default)
        {
            var url = $"https://aeronav.faa.gov/d-tpp/{cycle.FaaDirectory}/xml_data/d-tpp_Metafile.xml";
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                return resp.StatusCode == HttpStatusCode.OK;
            }
            catch (HttpRequestException) { return false; }
            catch (TaskCanceledException) { return false; }
        }
    }
}
