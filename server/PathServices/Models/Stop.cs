namespace PathApi.Server.PathServices.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Data model representing a stop in the PATH database.
    /// </summary>
    internal sealed class Stop
    {
        /// <summary>
        /// The ID of the stop in the GTFS document.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the stop.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The geographic latitude of the stop.
        /// </summary>
        public double Latitude { get; set; }


        /// <summary>
        /// The geographic longitude of the stop.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// The timezone of the stop.
        /// </summary>
        public string Timezone { get; set; }

        /// <summary>
        /// The ID of the parent to this stop (if this is not a station).
        /// </summary>
        public string ParentStopId { get; set; }

        /// <summary>
        /// The type of this stop.
        /// </summary>
        public LocationType LocationType { get; set; }
    }
}