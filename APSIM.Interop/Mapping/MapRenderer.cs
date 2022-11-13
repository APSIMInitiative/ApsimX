using System;
using SharpMap;
using MapTag = Models.Mapping.MapTag;
using System.Drawing;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Styles;
using System.Collections.Generic;
using System.Linq;
using GeoAPI;
using GeoAPI.Geometries;
using APSIM.Shared.Utilities;
using ProjNet.CoordinateSystems;
using SharpMap.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.IO;
using System.Text;
using GeoAPI.CoordinateSystems.Transformations;

namespace APSIM.Interop.Mapping
{
    /// <summary>
    /// This class can render world maps.
    /// </summary>
    public static class MapRenderer
    {
        /// <summary>
        /// Static constructor to perform 1-time initialisation.
        /// </summary>
        static MapRenderer()
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            GeometryServiceProvider.Instance = new NtsGeometryServices();
            var coordFactory = new CoordinateSystemFactory(System.Text.Encoding.Unicode);
            var css = new CoordinateSystemServices(coordFactory, new CoordinateTransformationFactory());
            css.AddCoordinateSystem(3857, coordFactory.CreateFromWkt("PROJCS[\"WGS 84 / Pseudo-Mercator\", GEOGCS[\"WGS 84\", DATUM[\"WGS_1984\", SPHEROID[\"WGS 84\", 6378137, 298.257223563, AUTHORITY[\"EPSG\", \"7030\"]], AUTHORITY[\"EPSG\", \"6326\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]], AUTHORITY[\"EPSG\", \"4326\"]], UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], PROJECTION[\"Mercator_1SP\"], PARAMETER[\"latitude_of_origin\", 0], PARAMETER[\"central_meridian\", 0], PARAMETER[\"scale_factor\", 1], PARAMETER[\"false_easting\", 0], PARAMETER[\"false_northing\", 0], AUTHORITY[\"EPSG\", \"3857\"]]"));
            css.AddCoordinateSystem(4326, coordFactory.CreateFromWkt("GEOGCS[\"WGS 84\", DATUM[\"WGS_1984\", SPHEROID[\"WGS 84\", 6378137, 298.257223563, AUTHORITY[\"EPSG\", \"7030\"]], AUTHORITY[\"EPSG\", \"6326\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]], AUTHORITY[\"EPSG\", \"4326\"]]"));
            Session.Instance.SetGeometryServices(GeoAPI.GeometryServiceProvider.Instance)
                            .SetCoordinateSystemServices(css)
                            .SetCoordinateSystemRepository(css);
        }

        /// <remarks>
        /// The world of mapping and GIS is a rather specialised and complex sub-field. Our needs here
        /// are fairly simple: we just want to be able to plot locations on a base map. But how are locations
        /// specified? Where do we get the map? What projection do we use? These remarks are intended to 
        /// (slightly) clarify what is going on.
        /// 
        /// We are using SharpMap to do the map rendering, and BruTile to fetch a suitable base map. The basemap 
        /// tiles use a "projected" coordinate system, specifically EPSG 3857 (also known as Web Mercator); 
        /// the units in this system are (perhaps surpisingly) metres. However, the point data we wish to plot
        /// is expressed as latitude and longitude (using a "geographic" coordinate system, specifically EPSG 4326),
        /// with units of decimal degrees. Note that both are based on WGS84, so they have the same underlying
        /// model of the shape of the earth, but use vastly different units. The two transformation objects defined 
        /// below handle coordinate transformation.
        /// 
        /// An earlier version of this unit made a call to SharpMap.Converters.WellKnownText.SpatialReference.GetAllReferenceSystems
        /// to obtain the co-ordinate systems. That call then attempted to generate a 3 MByte file with almost 4000 different
        /// systems in it; we only needed 2. We can generate those we need from their WKT descriptions. If additions reference
        /// systems are needed, their WKT descriptions should be available for download from spatialreference.org.
        /// </remarks>
        /// <summary>
        /// Performs coordinate transformation from latitude/longitude (WGS84) to metres (WebMercator).
        /// </summary>
        private static readonly ICoordinateTransformation latLonToMetres = new
                            CoordinateTransformationFactory().CreateFromCoordinateSystems(
                                GeographicCoordinateSystem.WGS84,
                                ProjectedCoordinateSystem.WebMercator);

        /// <summary>
        /// Performs coordinate transformation from  metres (WebMercator) to latitude/longitude (WGS84).
        /// </summary>
        private static readonly ICoordinateTransformation metresToLatLon = new
                            CoordinateTransformationFactory().CreateFromCoordinateSystems(
                                ProjectedCoordinateSystem.WebMercator,
                                GeographicCoordinateSystem.WGS84);

        /// <summary>
        /// Indicates the ratio between steps when zooming.
        /// </summary>
        private const double zoomStepFactor = 1.5;

        /// <summary>
        /// Render the map as a <see cref="System.Drawing.Image"/>.
        /// </summary>
        /// <param name="map">Map tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        public static Image ToImage(this MapTag map)
        {
            return map.ToSharpMap().GetMap();
        }

        /// <summary>
        /// Render the map as a <see cref="System.Drawing.Image"/> of the specified size.
        /// </summary>
        /// <param name="map">Map tag to be rendered.</param>
        /// <param name="width">Width of the map in px.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        public static Image ToImage(this MapTag map, int width)
        {
            Map exported = map.ToSharpMap();
            exported.Size = new Size(width, width);
            return exported.GetMap();
        }

        /// <summary>
        /// Create a <see cref="Map"/> representing this map object.
        /// </summary>
        /// <param name="map">A map to be exported/rendered.</param>
        public static Map ToSharpMap(this MapTag map)
        {
            Map result = InitMap();

            GeometryFactory gf = new GeometryFactory(new PrecisionModel(), 4326);
            List<IGeometry> locations = map.Markers.Select(c => gf.CreatePoint(new Coordinate(c.Longitude, c.Latitude))).ToList<IGeometry>();
            VectorLayer markerLayer = new VectorLayer("Markers");
            markerLayer.Style.Symbol = APSIM.Shared.Documentation.Image.LoadFromResource("Marker.png");
            markerLayer.Style.SymbolOffset = new PointF(0, -16); // Offset so the point is marked by the tip of the symbol, not its center
            markerLayer.DataSource = new GeometryProvider(locations);
            markerLayer.CoordinateTransformation = latLonToMetres;

            result.Layers.Add(markerLayer);

            // Compat check for old zoom units. Should really have used a converter...
            double zoom = map.Zoom - 1;
            if (zoom >= 60)
                zoom = 0;
            result.Zoom = result.MaximumZoom / Math.Pow(zoomStepFactor, zoom);

            Coordinate location = latLonToMetres.MathTransform.Transform(new Coordinate(map.Center.Longitude, map.Center.Latitude));
            result.Center = location;

            return result;
        }

        /// <summary>
        /// Get a coordinate transformation to convert a latitude and longitude to metres.
        /// </summary>
        public static ICoordinateTransformation GetLatLonToMetres() => latLonToMetres;

        /// <summary>
        /// Get a coordinate transformation to convert metres into a latitude and longitude.
        /// </summary>
        public static ICoordinateTransformation GetMetresToLatLon() => metresToLatLon;

        /// <summary>
        /// Get the zoom step factor. This is the ratio of each zoom increment to
        /// the previous zoom increment.
        /// </summary>
        /// <returns></returns>
        public static double GetZoomStepFactor() => zoomStepFactor;

        /// <summary>
        /// Initialise the map component.
        /// </summary>
        private static Map InitMap()
        {
            var result = new SharpMap.Map();
            result.BackColor = Color.LightBlue;
            result.Center = new Coordinate(0, 0);
            result.SRID = 3857;

            BruTile.Cache.FileCache fileCache = new BruTile.Cache.FileCache(Path.Combine(Path.GetTempPath(), "OSM Map Tiles"), "png", new TimeSpan(100, 0, 0, 0));
            TileLayer baseLayer = new TileLayer(BruTile.Predefined.KnownTileSources.Create(BruTile.Predefined.KnownTileSource.OpenStreetMap, persistentCache: fileCache, userAgent: "APSIM Next Generation"), "OpenStreetMap");
            result.BackgroundLayer.Add(baseLayer);
            result.MaximumZoom = baseLayer.Envelope.Width;

            // This layer is used only as a sort of backup in case the BruTile download times out
            // or is otherwise unavailable.
            // It should normally be invisible, as it will be covered by the BruTile tile layer.
            string apsimx = PathUtilities.GetAbsolutePath("%root%", null);
            string shapeFileName = Path.Combine(apsimx, "ApsimNG", "Resources", "world", "countries.shp");
            if (File.Exists(shapeFileName))
            {
                VectorLayer layWorld = new VectorLayer("Countries");
                layWorld.DataSource = new ShapeFile(shapeFileName, true);
                layWorld.Style = new VectorStyle();
                layWorld.Style.EnableOutline = true;
                // Color background = Colour.FromGtk(MainWidget.GetBackgroundColour(StateFlags.Normal));
                // Color foreground = Colour.FromGtk(MainWidget.GetForegroundColour(StateFlags.Normal));
                // layWorld.Style.Fill = new SolidBrush(background);
                // layWorld.Style.Outline.Color = foreground;
                layWorld.CoordinateTransformation = latLonToMetres;
                result.BackgroundLayer.Insert(0, layWorld);
            }

            return result;
        }
    }
}
