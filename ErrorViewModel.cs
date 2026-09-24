namespace BDTechMarket.Models
{
    /// <summary>
    /// View Model: Application exception ba error-er somoy diagnostic data carry kore.
    /// User-friendly error message dekhano ebong developer-er jonno trace ID rakhar jonno proyojon.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Unique identifier for the current request. 
        /// Database ba logs theke specific error khuje pete eta lagbe.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// HTTP Status Code (e.g., 404, 500, 403).
        /// Error page-e specific message (Page Not Found vs Server Error) dekhate help kore.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// RequestId blank na thakle UI-te ID-ti show korar permission dey.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}