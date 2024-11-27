using System;
using System.ComponentModel;

namespace APSIM.Shared.Documentation.Mapping
{
    /// <summary>
    /// Class for representing a latitude and longitude.
    /// </summary>
    [Serializable]
    public class Coordinate
    {
        /// <summary>The latitude</summary>
        [Description("Latitude")]
        public double Latitude { get; set; }

        /// <summary>The longitude</summary>
        [Description("Longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Convenience constructor.
        /// </summary>
        /// <param name="latitude">Latitude.</param>
        /// <param name="longitude">Longitude.</param>
        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
