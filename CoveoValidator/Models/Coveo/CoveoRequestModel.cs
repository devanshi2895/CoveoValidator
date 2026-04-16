namespace CoveoValidator.Models.Coveo
{
    /// <summary>
    /// View model for the Coveo search form submitted by the user.
    /// Bearer token is read from Web.config — not exposed in the UI.
    /// </summary>
    public class CoveoRequestModel
    {
        /// <summary>"Hotel" or "Excursion"</summary>
        public string ContentType { get; set; }

        /// <summary>Comma-separated Sriggle IDs entered by the user.</summary>
        public string SriggleIds { get; set; }

        /// <summary>"en" or "ar"</summary>
        public string Language { get; set; }
    }
}
