namespace CoveoValidator.Models.Domain
{
    /// <summary>
    /// Source-of-truth model for Excursion GA content validation.
    /// Each property maps to an expected field in fgaez120xcursioninfo50416.
    /// </summary>
    public class ExcursionInfoModel : SriggleBaseModel
    {
        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string ContractId { get; set; }
        public string Reference { get; set; }
        public string Tags { get; set; }
        public string Location { get; set; }
        public string SpecialNotes { get; set; }
        public string EventDiscount { get; set; }
        public string TitleLogos { get; set; }
        public string Images { get; set; }
        public string DigitalPass { get; set; }
        public string AdditionalEssentialInformation { get; set; }
        public string EssentialInformation { get; set; }
        public string CTA { get; set; }
    }
}
