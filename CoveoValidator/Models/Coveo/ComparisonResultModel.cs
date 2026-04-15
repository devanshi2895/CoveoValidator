using System.Collections.Generic;

namespace CoveoValidator.Models.Coveo
{
    /// <summary>
    /// The full comparison result for one Coveo document,
    /// combining raw metadata with GA validation outcome.
    /// </summary>
    public class ComparisonResultModel
    {
        // ── Identity ────────────────────────────────────────────────────────────
        public string Title { get; set; }
        public string ItemId { get; set; }
        public string SriggleId { get; set; }
        public string TemplateId { get; set; }
        public string ContentType { get; set; }   // "Hotel" | "Excursion"

        // ── Raw field snapshots ──────────────────────────────────────────────────
        /// <summary>Parsed backend info fields (fhotelinfo / fez120xcursioninfo).</summary>
        public List<FieldItem> BackendFields { get; set; } = new List<FieldItem>();

        /// <summary>Parsed GA info fields (fgahotelinfo / fgaez120xcursioninfo).</summary>
        public List<FieldItem> GaFields { get; set; } = new List<FieldItem>();

        // ── Validation ───────────────────────────────────────────────────────────
        public ValidationResult Validation { get; set; } = new ValidationResult();

        // ── Error state ──────────────────────────────────────────────────────────
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        // ── Computed helpers (used by the view — keeps @for body logic-free) ─────
        public bool HasMissing  { get { return Validation != null && Validation.MissingFields.Count > 0; } }
        public bool HasEmpty    { get { return Validation != null && Validation.EmptyFields.Count   > 0; } }
        public bool HasValid    { get { return Validation != null && Validation.ValidFields.Count   > 0; } }

        public int MissingCount { get { return Validation != null ? Validation.MissingFields.Count : 0; } }
        public int EmptyCount   { get { return Validation != null ? Validation.EmptyFields.Count   : 0; } }
        public int ValidCount   { get { return Validation != null ? Validation.ValidFields.Count   : 0; } }

        public ValidationResult SafeValidation
        {
            get { return Validation ?? new ValidationResult(); }
        }

        public string DisplayTitle
        {
            get { return !string.IsNullOrWhiteSpace(Title) ? Title : SriggleId; }
        }
    }

    /// <summary>
    /// Top-level view model passed from controller to view.
    /// </summary>
    public class CoveoSearchViewModel
    {
        public CoveoRequestModel Request { get; set; } = new CoveoRequestModel();
        public List<ComparisonResultModel> Results { get; set; } = new List<ComparisonResultModel>();
        public bool Searched { get; set; }
        public string GlobalError { get; set; }
    }
}
