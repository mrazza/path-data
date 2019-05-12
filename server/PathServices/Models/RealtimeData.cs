namespace PathApi.Server.PathServices.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

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
        /// The headsign for the train (think train name).
        /// </summary>
        public string Headsign { get; set; }

        /// <summary>
        /// The last time this data was updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The time at which this data is no longer considered valid.
        /// </summary>
        public DateTime DataExpiration { get; set; }

        /// <summary>
        /// The route this train operates on.
        /// </summary>
        public RouteLine Route { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);

        public override bool Equals(object obj)
        {
            return obj is RealtimeData data &&
                   this.ExpectedArrival == data.ExpectedArrival &&
                   this.ArrivalTimeMessage == data.ArrivalTimeMessage &&
                   (this.LineColors != null ? this.LineColors.Equals(data.LineColors) : this.LineColors == data.LineColors) &&
                   this.Headsign == data.Headsign &&
                   this.LastUpdated == data.LastUpdated &&
                   this.DataExpiration == data.DataExpiration &&
                   this.Route == data.Route;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 23) + this.ExpectedArrival.GetHashCode();
            hash = (hash * 23) + (this.ArrivalTimeMessage?.GetHashCode() ?? 0);
            hash = (hash * 23) + (this.LineColors?.GetHashCode() ?? 0);
            hash = (hash * 23) + (this.Headsign?.GetHashCode() ?? 0);
            hash = (hash * 23) + this.LastUpdated.GetHashCode();
            hash = (hash * 23) + this.DataExpiration.GetHashCode();
            hash = (hash * 23) + this.Route.GetHashCode();
            return hash;
        }
    }
}