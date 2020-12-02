using System;
using System.Text;
using Models.Core;
using Newtonsoft.Json;
using System.Xml;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Climate;

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

        /// <summary>List of coordinates to show on map</summary>
        public List<Coordinate> GetCoordinates(List<string> names = null)
        {
            List<Coordinate> coordinates = new List<Coordinate>();
            if (names != null)
                names.Clear();

            foreach (Weather weather in this.FindAllInScope<Weather>())
            {
                weather.OpenDataFile();
                double latitude = weather.Latitude;
                double longitude = weather.Longitude;
                weather.CloseDataFile();
                if (latitude != 0 && longitude != 0)
                {
                    Coordinate coordinate = new Coordinate(latitude, longitude);
                    coordinates.Add(coordinate);
                    if (names != null)
                        names.Add(System.IO.Path.GetFileName(weather.FileName));
                }
            }

            if (coordinates.Count == 0)
            {
                foreach (var soil in this.FindAllInScope<Models.Soils.Soil>())
                {
                    double latitude = soil.Latitude;
                    double longitude = soil.Longitude;
                    if (latitude != 0 && longitude != 0)
                    {
                        Coordinate coordinate = new Coordinate(latitude, longitude);
                        coordinates.Add(coordinate);
                        names.Add(soil.Name);
                    }
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Zoom factor for the map
        /// </summary>
        [Description("Zoom level")]
        public Double Zoom
        {
            get
            {
                return _Zoom;
            }
            set { _Zoom = value; }
        }

        /// <summary>
        /// Coordinate of map center
        /// </summary>
        private Coordinate _Center = new Coordinate(0, 0);

        /// <summary>
        /// Coordinate of the center of the map
        /// </summary>
        [Description("Map Center")]
        [Separator("Coordinates for center of map")]
        [Display(Type = DisplayType.SubModel)]
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
        private Double _Zoom = 360;
    }
}
