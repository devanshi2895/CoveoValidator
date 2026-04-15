using System.Collections.Generic;
using System.Threading.Tasks;
using CoveoValidator.Models.Coveo;

namespace CoveoValidator.Services
{
    /// <summary>
    /// Contract for searching and validating Coveo content.
    /// </summary>
    public interface ICoveoService
    {
        /// <summary>
        /// Searches Coveo for all supplied <paramref name="sriggleIds"/> in parallel,
        /// validates each result's GA content against the appropriate domain model,
        /// and returns a merged list of comparison results.
        /// </summary>
        /// <param name="contentType">"Hotel" or "Excursion"</param>
        /// <param name="sriggleIds">One or more Sriggle IDs to look up.</param>
        /// <param name="bearerToken">Coveo platform API bearer token.</param>
        Task<List<ComparisonResultModel>> SearchAndValidateAsync(
            string contentType,
            IEnumerable<string> sriggleIds,
            string bearerToken);
    }
}
