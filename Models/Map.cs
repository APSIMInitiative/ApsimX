using System;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// # [Name]
    /// [DocumentView]
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MapView")]
    [PresenterName("UserInterface.Presenters.MapPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class Map : Model, AutoDocumentation.ITag
    {
        /// <summary>
        /// Class for representing a latitude and longitude.
        /// </summary>
        [Serializable]
        public class Coordinate
        {
            /// <summary>The latitude</summary>
            public double Latitude { get; set; }

            /// <summary>The longitude</summary>
            public double Longitude { get; set; }
        }

        /// <summary>List of coordinates to show on map</summary>
        public List<Coordinate> GetCoordinates()
        {
            List<Coordinate> coordinates = new List<Coordinate>();

            foreach (Weather weather in Apsim.FindAll(this, typeof(Weather)))
            {
                weather.OpenDataFile();
                double latitude = weather.Latitude;
                double longitude = weather.Longitude;
                weather.CloseDataFile();
                if (latitude != 0 && longitude != 0)
                {
                    Coordinate coordinate = new Coordinate();
                    coordinate.Latitude = latitude;
                    coordinate.Longitude = longitude;
                    coordinates.Add(coordinate);
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Coordinate of map center
        /// </summary>
        private Coordinate _Center = new Coordinate() { Latitude = 0.0, Longitude = 0.0 };

        /// <summary>
        /// Coordinate of the center of the map
        /// </summary>
        public Coordinate Center
        {
            get
            {
                return _Center;
            }
            set { _Center = value; }
        }

        /// <summary>
        /// Zoom level
        /// </summary>
        private Double _Zoom = 1.4;

        /// <summary>
        /// Zoom factor for the map
        /// </summary>
        public Double Zoom
        {
            get
            {
                return _Zoom;
            }
            set { _Zoom = value; }
        }
    }
}
