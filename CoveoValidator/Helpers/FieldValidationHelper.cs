using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoveoValidator.Models.Coveo;

namespace CoveoValidator.Helpers
{
    /// <summary>
    /// Reflection-based helper that validates a list of GA <see cref="FieldItem"/>s
    /// against the expected properties defined on a domain model type.
    /// </summary>
    public static class FieldValidationHelper
    {
        /// <summary>
        /// Compares <paramref name="gaFields"/> against all public string properties
        /// declared on <typeparamref name="TModel"/> (and its entire inheritance chain).
        /// </summary>
        /// <typeparam name="TModel">Domain model type used as the source of truth.</typeparam>
        /// <param name="gaFields">GA fields deserialized from the Coveo raw field.</param>
        /// <returns>A populated <see cref="ValidationResult"/>.</returns>
        public static ValidationResult Validate<TModel>(List<FieldItem> gaFields)
        {
            return Validate(typeof(TModel), gaFields);
        }

        /// <summary>
        /// Non-generic overload — useful when the model type is resolved at runtime.
        /// </summary>
        public static ValidationResult Validate(Type modelType, List<FieldItem> gaFields)
        {
            var result = new ValidationResult();

            // Build a case-insensitive lookup of the GA fields by name.
            var gaLookup = (gaFields ?? new List<FieldItem>())
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);

            // Walk the full inheritance chain to collect every public property.
            var modelProperties = GetAllPublicProperties(modelType);

            foreach (var prop in modelProperties)
            {
                var fieldName = prop.Name;

                if (!gaLookup.TryGetValue(fieldName, out var rawValue))
                {
                    // Field not present in GA data at all.
                    result.MissingFields.Add(new FieldValidationResult
                    {
                        FieldName = fieldName,
                        Status = FieldStatus.Missing,
                        Value = null
                    });
                }
                else if (IsEmpty(rawValue))
                {
                    // Field exists but carries no meaningful content.
                    result.EmptyFields.Add(new FieldValidationResult
                    {
                        FieldName = fieldName,
                        Status = FieldStatus.Empty,
                        Value = rawValue
                    });
                }
                else
                {
                    result.ValidFields.Add(new FieldValidationResult
                    {
                        FieldName = fieldName,
                        Status = FieldStatus.Valid,
                        Value = rawValue
                    });
                }
            }

            return result;
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns all distinct public instance properties declared on
        /// <paramref name="type"/> and every class in its inheritance chain.
        /// </summary>
        private static IEnumerable<PropertyInfo> GetAllPublicProperties(Type type)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = type;

            while (current != null && current != typeof(object))
            {
                foreach (var prop in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (seen.Add(prop.Name))
                        yield return prop;
                }
                current = current.BaseType;
            }
        }

        /// <summary>
        /// Returns <c>true</c> when a value is considered empty:
        /// null, whitespace-only, empty JSON array "[]", or empty JSON object "{}".
        /// </summary>
        private static bool IsEmpty(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;

            var trimmed = value.Trim();
            return trimmed == "[]" || trimmed == "{}";
        }
    }
}
