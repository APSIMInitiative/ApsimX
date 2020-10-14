using System;
using System.Text;
using Models.Core;
using Newtonsoft.Json;
using System.Xml;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Climate;
using System.Drawing;
using SharpMap.Styles;
using SharpMap.Layers;
using SharpMap.Data.Providers;
using System.Drawing.Imaging;
using GeoAPI.Geometries;
using GeoAPI;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Reflection;
using System.IO;
using System.Linq;

namespace Models
{
    /// <summary>
    /// # [Name]
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
        /// Export the map to an image.
        /// </summary>
        public Image Export()
        {
            var map = new SharpMap.Map();
            map.Size = new Size(700, 700);
            map.MaximumZoom = 720;
            map.BackColor = Color.LightBlue;
            map.Center = new GeoAPI.Geometries.Coordinate(0, 0);
            map.Zoom = map.MaximumZoom;
            
            // Read shapefile.
            VectorLayer layWorld = new VectorLayer("Countries");
            string bin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimx = Directory.GetParent(bin).FullName;
            string shapeFileName = Path.Combine(apsimx, "ApsimNG", "Resources", "world", "countries.shp");
            layWorld.DataSource = new ShapeFile(shapeFileName, true);
            layWorld.Style = new VectorStyle();
            layWorld.Style.EnableOutline = true;
            //layWorld.Style.Fill = new SolidBrush(background);
            //layWorld.Style.Outline.Color = foreground;
            map.Layers.Add(layWorld);

            // Add country names to map.
            // Note this doesn't appear to work under mono for now.
            LabelLayer countryNames = new LabelLayer("Country labels");
			countryNames.DataSource = layWorld.DataSource;
            //countryNames.Enabled = true;
            countryNames.LabelColumn = "Name";
            countryNames.MultipartGeometryBehaviour = LabelLayer.MultipartGeometryBehaviourEnum.Largest;
            countryNames.Style = new LabelStyle();
            //^countryNames.Style.BackColor = new SolidBrush(foreground);
            //countryNames.Style.ForeColor = foreground;
            //countryNames.Style.Font = new Font(FontFamily.GenericSerif, 8);
            //countryNames.MaxVisible = 90;
            countryNames.Style.HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center;
            map.Layers.Add(countryNames);

            // Add the markers to the map.
            GeometryFactory gf = new GeometryFactory(new PrecisionModel(), 3857);
            List<IGeometry> locations = GetCoordinates().Select(c => gf.CreatePoint(new GeoAPI.Geometries.Coordinate(c.Longitude, c.Latitude))).ToList<IGeometry>();
            VectorLayer markerLayer = new VectorLayer("Markers");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Models.Resources.Marker.png"))
                markerLayer.Style.Symbol = System.Drawing.Image.FromStream(stream);
            markerLayer.DataSource = new GeometryProvider(locations);
            map.Layers.Add(markerLayer);
            map.Zoom = Zoom;
            map.Center = new GeoAPI.Geometries.Coordinate(Center.Longitude, Center.Latitude);
            
            return map.GetMap();
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
