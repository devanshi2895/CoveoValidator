using System.Collections.Generic;
using Newtonsoft.Json;

namespace CoveoValidator.Models.Coveo
{
    // ── Coveo REST API response shapes ─────────────────────────────────────────

    /// <summary>Top-level response envelope returned by the Coveo Search API.</summary>
    public class CoveoApiResponse
    {
        [JsonProperty("results")]
        public List<CoveoResult> Results { get; set; } = new List<CoveoResult>();

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }
    }

    /// <summary>Single search result item.</summary>
    public class CoveoResult
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("raw")]
        public CoveoRawFields Raw { get; set; }
    }

    /// <summary>
    /// Raw field bag returned per result.
    /// Only the fields we care about are mapped; the rest are ignored.
    /// </summary>
    public class CoveoRawFields
    {
        // ── Common ──────────────────────────────────────────────────────────────
        [JsonProperty("fname50416")]
        public string Name { get; set; }

        [JsonProperty("ftemplateid50416")]
        public string TemplateId { get; set; }

        [JsonProperty("fitemid50416")]
        public string ItemId { get; set; }

        // ── Hotel ───────────────────────────────────────────────────────────────
        [JsonProperty("fhotelinfo50416")]
        public string HotelInfo { get; set; }

        [JsonProperty("fgahotelinfo50416")]
        public string GaHotelInfo { get; set; }

        // ── Excursion ───────────────────────────────────────────────────────────
        [JsonProperty("fez120xcursioninfo50416")]
        public string ExcursionInfo { get; set; }

        [JsonProperty("fgaez120xcursioninfo50416")]
        public string GaExcursionInfo { get; set; }

        // ── Language / path (used in query filter, returned for reference) ──────
        [JsonProperty("flanguage50416")]
        public string Language { get; set; }

        [JsonProperty("ffullpath50416")]
        public string FullPath { get; set; }
    }
}
