namespace PathApi.Server.PathServices
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Data model representing a realtime train arrival.
    /// </summary>
    internal sealed class RealtimeData
    {
        /// <summary>
        /// The time the train is expected to arrive in the station.
        /// </summary>
        public DateTime ExpectedArrival { get; set; }

        /// <summary>
        /// The message displayed on arrival signs.
        /// </summary>
        public string ArrivalTimeMessage { get; set; }

        /// <summary>
        /// The line colors for this train (ex. blue for Hoboken).
        /// </summary>
        public List<string> LineColors { get; set; }

        /// <summary>
        /// The head sign for the train (think train name).
        /// </summary>
        public string HeadSign { get; set; }

        /// <summary>
        /// The last time this data was updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
}