using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using CoveoValidator.Models.Coveo;
using CoveoValidator.Services;

namespace CoveoValidator.Controllers
{
    /// <summary>
    /// Handles the Coveo search + validation UI.
    /// Bearer token is read from Web.config key "Coveo:DefaultBearerToken".
    /// </summary>
    public class CoveoController : Controller
    {
        private readonly ICoveoService _coveoService;

        public CoveoController()
        {
            _coveoService = new CoveoService();
        }

        public CoveoController(ICoveoService coveoService)
        {
            _coveoService = coveoService;
        }

        // ── GET /Coveo ────────────────────────────────────────────────────────────

        [HttpGet]
        public ActionResult Index()
        {
            var vm = new CoveoSearchViewModel
            {
                Request = new CoveoRequestModel { ContentType = "Hotel" }
            };
            return View(vm);
        }

        // ── POST /Coveo ───────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(CoveoRequestModel request)
        {
            var vm = new CoveoSearchViewModel
            {
                Request  = request,
                Searched = true
            };

            // Read bearer token from config — never from user input.
            string bearerToken = ConfigurationManager.AppSettings["Coveo:DefaultBearerToken"];

            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                vm.GlobalError = "Coveo bearer token is not configured. " +
                                 "Please set 'Coveo:DefaultBearerToken' in Web.config.";
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(request.SriggleIds))
            {
                vm.GlobalError = "Please enter at least one Sriggle ID.";
                return View(vm);
            }

            var ids = request.SriggleIds
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (!ids.Any())
            {
                vm.GlobalError = "No valid Sriggle IDs found in the input.";
                return View(vm);
            }

            try
            {
                vm.Results = await _coveoService.SearchAndValidateAsync(
                    request.ContentType,
                    ids,
                    bearerToken.Trim());
            }
            catch (Exception ex)
            {
                vm.GlobalError = $"Unexpected error: {ex.Message}";
            }

            return View(vm);
        }
    }
}
