using System.Collections.Generic;

namespace CoveoValidator.Models.Domain
{
    /// <summary>
    /// Source-of-truth model for Hotel GA content validation.
    /// Each property maps to an expected field in fgahotelinfo50416.
    /// </summary>
    public class HotelInfoModel : SriggleBaseModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rating { get; set; }
        public string Distance { get; set; }
        public string Brand { get; set; }
        public string Location { get; set; }
        public string SpecialHotelInclusion { get; set; }
        public string PremiumVacationTags { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string SpecialNotes { get; set; }
        public string YasExpress { get; set; }
        public string YasExpressLink { get; set; }
        public string BreakfastSavings { get; set; }
        public string ExcludeHotelFromDiscount { get; set; }
        public string UseDefaultDiscount { get; set; }
        public string PropertyType { get; set; }
        public string LocationId { get; set; }
        public string HotelLocation { get; set; }
        public string HotelLocationId { get; set; }
        public string PropertyTypeId { get; set; }
        public string Tags { get; set; }
        public string Facilities { get; set; }
        public string YasNeighbourhoodBenefits { get; set; }
        public string YasNeighbourhoodProgram { get; set; }
        public string Images { get; set; }
        public string DigitalPass { get; set; }
        public string HotelUrl { get; set; }
        public string HotelUSPs { get; set; }
        public string HotelAmenities { get; set; }
        public string PremiumFreeInclusions { get; set; }
        public string FreeInclusionSectionTitle { get; set; }
        public string FreeInclusions { get; set; }
        public string DefaultSectionTitle { get; set; }
        public string DefaultSection { get; set; }
    }
}
