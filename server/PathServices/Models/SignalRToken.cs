namespace PathApi.Server.PathServices.Models
{
    /// <summary>
    /// Data model representing a SingalR authentication token.
    /// </summary>
    public class SignalRToken
    {
        /// <summary>
        /// Gets or sets the URL of the SignalR hub for realtime event data.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the authentication JWT to be used to connect to SignalR.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the response message associated with the response.
        /// </summary>
        /// <remarks>
        /// Current use of this field is unknown.
        /// </remarks>
        public string Message { get; set; }
    }
}
