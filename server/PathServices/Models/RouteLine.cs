namespace PathApi.Server.PathServices.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using PathApi.V1;

    /// <summary>
    /// Data model representing a route in the PATH database.
    /// </summary>
    internal sealed class RouteLine
    {
        /// <summary>
        /// The route this RouteLine represents.
        /// </summary>
        public Route Route { get; set; }

        /// <summary>
        /// The GTFS ID of this route.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The long name (that does not account for direction) of this route.
        /// </summary>
        public string LongName { get; set; }

        /// <summary>
        /// The display name for this route.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The headsign for trains that use this route and direction.
        /// </summary>
        public string Headsign { get; set; }

        /// <summary>
        /// The color of the headsign/train for this route.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// The direction of travel along this route.
        /// </summary>
        public RouteDirection Direction { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);

        public override bool Equals(object obj)
        {
            return obj is RouteLine route &&
                   this.Route == route.Route &&
                   this.Id == route.Id &&
                   this.LongName == route.LongName &&
                   this.DisplayName == route.DisplayName &&
                   this.Headsign == route.Headsign &&
                   this.Color == route.Color &&
                   this.Direction == route.Direction;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 23) + this.Route.GetHashCode();
            hash = (hash * 23) + (this.Id?.GetHashCode() ?? 0);
            hash = (hash * 23) + (this.LongName?.GetHashCode() ?? 0);
            hash = (hash * 23) + (this.DisplayName?.GetHashCode() ?? 0);
            hash = (hash * 23) + (this.Headsign?.GetHashCode() ?? 0);
            hash = (hash * 23) + (this.Color?.GetHashCode() ?? 0);
            hash = (hash * 23) + this.Direction.GetHashCode();
            return hash;
        }
    }
}