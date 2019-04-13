namespace PathApi.Server.PathServices.Models
{
    /// <summary>
    /// Enum representing the direction of travel of a train.
    /// </summary>
    /// <remarks>
    /// The values of these enums map to the direction IDs of the PATH database.
    /// </remarks>
    internal enum RouteDirection
    {
        /// <summary>
        /// For trains that start in NY and terminate in NJ.
        /// </summary>
        ToNJ = 0,

        /// <summary>
        /// For trains that start in NJ and terminate in NY.
        /// </summary>
        ToNY = 1
    }
}