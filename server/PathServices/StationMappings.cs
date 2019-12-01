namespace PathApi.Server.PathServices
{
    using PathApi.V1;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Various PATH station metadata.
    /// </summary>
    internal static class StationMappings
    {
        /// <summary>
        /// Dictionary mapping <see cref="Station"/> enums to short string values used by PATH.
        /// </summary>
        public static readonly ReadOnlyDictionary<Station, string> StationToServiceBusTopic =
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

        public static readonly ReadOnlyDictionary<Station, string> StationToSignalRTokenName =
            new ReadOnlyDictionary<Station, string>(new Dictionary<Station, string>()
            {
                { Station.Newark, "Newark" },
                { Station.Harrison, "Harrison" },
                { Station.JournalSquare, "Journal Square" },
                { Station.GroveStreet, "Grove Street" },
                { Station.ExchangePlace, "Exchange Place" },
                { Station.WorldTradeCenter, "World Trade Center" },
                { Station.Newport, "Newport" },
                { Station.Hoboken, "Hoboken" },
                { Station.ChristopherStreet, "Christopher Street" },
                { Station.NinthStreet, "9th Street" },
                { Station.FourteenthStreet, "14th Street" },
                { Station.TwentyThirdStreet, "23rd Street" },
                { Station.ThirtyThirdStreet, "33rd Street" }
            });

        public static readonly ReadOnlyDictionary<Station, int> StationToDatabaseId =
            new ReadOnlyDictionary<Station, int>(new Dictionary<Station, int>()
            {
                { Station.Newark, 26733 },
                { Station.Harrison, 26729 },
                { Station.JournalSquare, 26731 },
                { Station.GroveStreet, 26728 },
                { Station.ExchangePlace, 26727 },
                { Station.Newport, 26732 },
                { Station.Hoboken, 26730 },
                { Station.WorldTradeCenter, 26734 },
                { Station.ThirtyThirdStreet, 26724 },
                { Station.TwentyThirdStreet, 26723 },
                { Station.FourteenthStreet, 26722 },
                { Station.NinthStreet, 26725 },
                { Station.ChristopherStreet, 26726 }
            });
    }
}