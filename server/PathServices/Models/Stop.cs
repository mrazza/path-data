namespace PathApi.Server.PathServices.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

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

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override bool Equals(object obj)
        {
            var stop = obj as Stop;
            return stop != null &&
                   this.Id == stop.Id &&
                   this.Name == stop.Name &&
                   this.Latitude == stop.Latitude &&
                   this.Longitude == stop.Longitude &&
                   this.Timezone == stop.Timezone &&
                   this.ParentStopId == stop.ParentStopId &&
                   this.LocationType == stop.LocationType;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (this.Id ?? string.Empty).GetHashCode();
            hash = hash * 23 + (this.Name ?? string.Empty).GetHashCode();
            hash = hash * 23 + this.Latitude.GetHashCode();
            hash = hash * 23 + this.Longitude.GetHashCode();
            hash = hash * 23 + (this.Timezone ?? string.Empty).GetHashCode();
            hash = hash * 23 + (this.ParentStopId ?? string.Empty).GetHashCode();
            hash = hash * 23 + this.LocationType.GetHashCode();
            return hash;
        }
    }
}