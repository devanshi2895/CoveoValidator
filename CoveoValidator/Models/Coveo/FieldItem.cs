namespace CoveoValidator.Models.Coveo
{
    /// <summary>
    /// Represents a single key-value field returned inside Coveo raw field JSON arrays.
    /// e.g. fgahotelinfo50416 / fgaez120xcursioninfo50416
    /// </summary>
    public class FieldItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
