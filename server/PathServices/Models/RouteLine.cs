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
        // The route this RouteLine represents.
        public Route Route { get; set; }

        // The GTFS ID of this route.
        public string Id { get; set; }

        // The long name (that does not account for direction) of this route.
        public string LongName { get; set; }

        // The display name for this route.
        public string DisplayName { get; set; }

        // The headsign for trains that use this route and direction.
        public string Headsign { get; set; }

        // The color of the headsign/train for this route.
        public string Color { get; set; }

        // The direction of travel along this route.
        public RouteDirection Direction { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override bool Equals(object obj)
        {
            var route = obj as RouteLine;
            return route != null &&
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
            hash = hash * 23 + this.Route.GetHashCode();
            hash = hash * 23 + (this.Id?.GetHashCode() ?? 0);
            hash = hash * 23 + (this.LongName?.GetHashCode() ?? 0);
            hash = hash * 23 + (this.DisplayName?.GetHashCode() ?? 0);
            hash = hash * 23 + (this.Headsign?.GetHashCode() ?? 0);
            hash = hash * 23 + (this.Color?.GetHashCode() ?? 0);
            hash = hash * 23 + this.Direction.GetHashCode();
            return hash;
        }
    }
}