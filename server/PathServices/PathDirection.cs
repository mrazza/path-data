namespace PathApi.Server.PathServices
{
    /// <summary>
    /// Enum representing the direction of travel of a train.
    /// </summary>
    internal enum PathDirection
    {
        /// <summary>
        /// For trains that start in NJ and terminate in NY.
        /// </summary>
        ToNY,

        /// <summary>
        /// For trains that start in NY and terminate in NJ.
        /// </summary>
        ToNJ
    }
}