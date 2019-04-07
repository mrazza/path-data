namespace PathApi.Server.PathServices
{
    using PathApi.V1;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Various PATH station metadata.
    /// </summary>
    internal static class StationData
    {
        /// <summary>
        /// Dictionary mapping <see cref="Station"/> enums to short string values used by PATH.
        /// </summary>
        public static readonly ReadOnlyDictionary<Station, string> StationToShortName =
            new ReadOnlyDictionary<Station, string>(new Dictionary<Station, string>()
            {
                { Station.Newark, "nwk" },
                { Station.Harrison, "har" },
                { Station.JournalSquare, "jsq" },
                { Station.GroveStreet, "grv" },
                { Station.ExchangePlace, "EXP" },
                { Station.WorldTradeCenter, "wtc" },
                { Station.Newport, "new" },
                { Station.Hoboken, "HOB" },
                { Station.ChristopherStreet, "chr" },
                { Station.NinthStreet, "09s" },
                { Station.FourteenthStreet, "14s" },
                { Station.TwentyThirdStreet, "23s" },
                { Station.ThirtyThirdStreet, "33s" }
            });
    }
}