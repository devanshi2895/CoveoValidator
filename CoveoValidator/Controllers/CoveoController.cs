using System;
using System.Collections.Generic;
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
                Request = new CoveoRequestModel { ContentType = "Hotel", Language = "en" }
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
                    bearerToken.Trim(),
                    string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim());
            }
            catch (Exception ex)
            {
                vm.GlobalError = $"Unexpected error: {ex.Message}";
            }

            return View(vm);
        }

        // ── POST /Coveo/ExportExcel ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExportExcel(CoveoRequestModel request)
        {
            string bearerToken = ConfigurationManager.AppSettings["Coveo:DefaultBearerToken"];

            if (string.IsNullOrWhiteSpace(bearerToken) || string.IsNullOrWhiteSpace(request.SriggleIds))
                return RedirectToAction("Index");

            var ids = request.SriggleIds
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (!ids.Any())
                return RedirectToAction("Index");

            string language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim();

            List<ComparisonResultModel> results;
            try
            {
                results = await _coveoService.SearchAndValidateAsync(
                    request.ContentType,
                    ids,
                    bearerToken.Trim(),
                    language);
            }
            catch (Exception ex)
            {
                TempData["ExportError"] = $"Export failed: {ex.Message}";
                return RedirectToAction("Index");
            }

            byte[] fileBytes = ExcelExportService.Export(results, language);
            string fileName = string.Format("CoveoValidation_{0}_{1}_{2:yyyyMMdd_HHmmss}.xlsx",
                request.ContentType, language, DateTime.Now);

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
