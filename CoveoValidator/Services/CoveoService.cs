using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CoveoValidator.Helpers;
using CoveoValidator.Models.Coveo;
using CoveoValidator.Models.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoveoValidator.Services
{
    /// <summary>
    /// Calls the Coveo Search API, parses raw field JSON, and runs field validation.
    /// </summary>
    public class CoveoService : ICoveoService
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const string CoveoSearchUrl = "https://platform.cloud.coveo.com/rest/search/v2";

        private const string HotelTemplateId     = "d801ccec-bb25-4621-a1db-ea4eaf32120e";
        private const string ExcursionTemplateId = "44002bda-145b-49a9-a8f4-36b568362c76";

        private static string LanguageFilter(string language) =>
            $"@flanguage50416==\"{language}\"";

        private const string CommonPathFilter     = "@ffullpath50416=*\"/sitecore/content/YasConnect/GlobalContent/SharedContent/Platform/*\"";

        // ── Shared HttpClient (thread-safe, reuse across calls) ──────────────────
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // ── ICoveoService ────────────────────────────────────────────────────────

        public async Task<List<ComparisonResultModel>> SearchAndValidateAsync(
            string contentType,
            IEnumerable<string> sriggleIds,
            string bearerToken,
            string language = "en")
        {
            // Fan-out: one API call per ID, all running concurrently.
            var tasks = sriggleIds
                .Select(id => SearchSingleIdAsync(contentType, id.Trim(), bearerToken, language));

            var nestedResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Flatten the per-ID result lists into one ordered list.
            return nestedResults.SelectMany(r => r).ToList();
        }

        // ── Private: per-ID search ───────────────────────────────────────────────

        private async Task<List<ComparisonResultModel>> SearchSingleIdAsync(
            string contentType,
            string sriggleId,
            string bearerToken,
            string language)
        {
            try
            {
                var queryBody = BuildQueryBody(contentType, sriggleId, language);
                var json      = JsonConvert.SerializeObject(queryBody);

                using (var request = new HttpRequestMessage(HttpMethod.Post, CoveoSearchUrl))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                    request.Content =
                        new StringContent(json, Encoding.UTF8, "application/json");

                    using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                    {
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            return ErrorResult(sriggleId, contentType,
                                $"Coveo API returned {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(responseBody, 300)}");
                        }

                        return ParseAndValidate(responseBody, sriggleId, contentType);
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(sriggleId, contentType, $"Exception: {ex.Message}");
            }
        }

        // ── Private: build CQL query body ────────────────────────────────────────

        private object BuildQueryBody(string contentType, string sriggleId, string language)
        {
            // Determine template + field list based on content type.
            bool isHotel = string.Equals(contentType, "Hotel", StringComparison.OrdinalIgnoreCase);

            string templateId  = isHotel ? HotelTemplateId  : ExcursionTemplateId;
            string infoField   = isHotel ? "fhotelinfo50416"      : "fez120xcursioninfo50416";
            string gaInfoField = isHotel ? "fgahotelinfo50416"    : "fgaez120xcursioninfo50416";

            // CQL expression: language + path + template + sriggle ID filter.
            // We match the sriggle ID inside the backend info JSON field (loose match).
            string cql = $"{LanguageFilter(language)} AND {CommonPathFilter} " +
                         $"AND @ftemplateid50416==\"{templateId}\" " +
                         $"AND @fsriggleid50416==\"{sriggleId}\"";

            return new
            {
                q           = "",
                aq          = cql,
                numberOfResults = 10,
                fieldsToInclude = new[]
                {
                    "fname50416",
                    "ftemplateid50416",
                    "fitemid50416",
                    "flanguage50416",
                    "ffullpath50416",
                    infoField,
                    gaInfoField
                }
            };
        }

        // ── Private: parse API response + validate ───────────────────────────────

        private List<ComparisonResultModel> ParseAndValidate(
            string responseBody,
            string sriggleId,
            string contentType)
        {
            CoveoApiResponse apiResponse;
            try
            {
                apiResponse = JsonConvert.DeserializeObject<CoveoApiResponse>(responseBody);
            }
            catch (Exception ex)
            {
                return ErrorResult(sriggleId, contentType, $"Failed to deserialize Coveo response: {ex.Message}");
            }

            if (apiResponse?.Results == null || !apiResponse.Results.Any())
            {
                // Return a placeholder so the UI can show "no data found".
                return new List<ComparisonResultModel>
                {
                    new ComparisonResultModel
                    {
                        SriggleId   = sriggleId,
                        ContentType = contentType,
                        Title       = $"[No results found for SriggleId: {sriggleId}]",
                        HasError    = false
                    }
                };
            }

            bool isHotel = string.Equals(contentType, "Hotel", StringComparison.OrdinalIgnoreCase);

            var results = new List<ComparisonResultModel>();

            foreach (var item in apiResponse.Results)
            {
                var raw = item.Raw ?? new CoveoRawFields();

                // Pick correct raw field strings.
                string backendJson = isHotel ? raw.HotelInfo      : raw.ExcursionInfo;
                string gaJson      = isHotel ? raw.GaHotelInfo    : raw.GaExcursionInfo;

                // Deserialize FieldItem lists.
                var backendFields = DeserializeFieldItems(backendJson);
                var gaFields      = DeserializeFieldItems(gaJson);

                // Extract sriggle ID from backend fields (field named "SriggleId").
                string resolvedSriggleId = sriggleId;
                var sriggleField = backendFields.FirstOrDefault(f =>
                    string.Equals(f.Name, "SriggleId", StringComparison.OrdinalIgnoreCase));
                if (sriggleField != null && !string.IsNullOrWhiteSpace(sriggleField.Value))
                    resolvedSriggleId = sriggleField.Value;

                // Run reflection-based validation against the correct model.
                ValidationResult validation = isHotel
                    ? FieldValidationHelper.Validate<HotelInfoModel>(gaFields)
                    : FieldValidationHelper.Validate<ExcursionInfoModel>(gaFields);

                results.Add(new ComparisonResultModel
                {
                    Title       = item.Title ?? raw.Name ?? resolvedSriggleId,
                    ItemId      = ExtractItemIdFromUri(item.Uri) ?? raw.ItemId,
                    SriggleId   = resolvedSriggleId,
                    TemplateId  = raw.TemplateId,
                    ContentType = contentType,
                    BackendFields = backendFields,
                    GaFields      = gaFields,
                    Validation    = validation
                });
            }

            return results;
        }

        // ── Private: JSON field helpers ──────────────────────────────────────────

        /// <summary>
        /// Safely deserialises a raw Coveo field that may contain:
        ///   - a JSON array of {name, value} objects → manual parse (value may be string/number/bool/array/object/null)
        ///   - a JSON string wrapping an array        → unwrap then recurse
        ///   - null / empty                           → empty list
        /// Each "value" token is normalised to a string regardless of its JSON type.
        /// </summary>
        private static List<FieldItem> DeserializeFieldItems(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<FieldItem>();

            try
            {
                var token = JToken.Parse(raw);

                if (token.Type == JTokenType.String)
                {
                    // Double-encoded: the field value is itself a JSON string.
                    return DeserializeFieldItems(token.Value<string>());
                }

                if (token.Type == JTokenType.Array)
                {
                    var list = new List<FieldItem>();
                    foreach (var element in (JArray)token)
                    {
                        if (element.Type != JTokenType.Object)
                            continue;

                        var obj = (JObject)element;
                        var name  = obj["name"]?.Value<string>();
                        var valueToken = obj["value"];

                        list.Add(new FieldItem
                        {
                            Name  = name,
                            Value = JTokenToString(valueToken)
                        });
                    }
                    return list;
                }
            }
            catch
            {
                // If all else fails, return an empty list; the validation will
                // flag all expected fields as missing.
            }

            return new List<FieldItem>();
        }

        /// <summary>
        /// Converts any JToken to a display string:
        ///   null / JTokenType.Null → null
        ///   string                 → raw string value
        ///   number / bool          → ToString()
        ///   array / object         → compact JSON (empty array/object becomes null so IsEmpty catches it)
        /// </summary>
        private static string JTokenToString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                    return token.ToString();

                case JTokenType.Array:
                case JTokenType.Object:
                    // Compact JSON; e.g. "[]" or "[{...}]" — IsEmpty handles the empty cases.
                    return token.ToString(Formatting.None);

                default:
                    return token.ToString();
            }
        }

        // ── Private: URI helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Extracts the GUID segment from a Sitecore URI such as:
        ///   sitecore://database/web/ItemId/929F6D86-BB4D-409C-8B5C-C98F0FF92692/Language/en/Version/14
        /// Returns null if the URI is null/empty or does not contain "/ItemId/".
        /// </summary>
        private static string ExtractItemIdFromUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            const string marker = "/ItemId/";
            int start = uri.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return null;

            start += marker.Length;
            int end = uri.IndexOf('/', start);
            return end < 0
                ? uri.Substring(start)
                : uri.Substring(start, end - start);
        }

        // ── Private: error helpers ───────────────────────────────────────────────

        private static List<ComparisonResultModel> ErrorResult(
            string sriggleId, string contentType, string message)
        {
            return new List<ComparisonResultModel>
            {
                new ComparisonResultModel
                {
                    SriggleId    = sriggleId,
                    ContentType  = contentType,
                    Title        = $"[Error – SriggleId: {sriggleId}]",
                    HasError     = true,
                    ErrorMessage = message
                }
            };
        }

        private static string Truncate(string s, int max) =>
            s?.Length > max ? s.Substring(0, max) + "…" : s;
    }
}
