using System.Collections.Generic;

namespace CoveoValidator.Models.Coveo
{
    /// <summary>
    /// Categorised validation outcome for a single GA field.
    /// </summary>
    public enum FieldStatus
    {
        Valid,
        Empty,
        Missing
    }

    /// <summary>
    /// Validation result for one field property.
    /// </summary>
    public class FieldValidationResult
    {
        public string FieldName { get; set; }
        public FieldStatus Status { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Complete validation summary for one Coveo result item.
    /// </summary>
    public class ValidationResult
    {
        public List<FieldValidationResult> ValidFields { get; set; } = new List<FieldValidationResult>();
        public List<FieldValidationResult> EmptyFields { get; set; } = new List<FieldValidationResult>();
        public List<FieldValidationResult> MissingFields { get; set; } = new List<FieldValidationResult>();
    }
}
