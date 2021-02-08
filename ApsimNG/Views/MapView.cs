namespace UserInterface.Views
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using APSIM.Shared.Utilities;
    using System.Globalization;
    using Interfaces;
    using Models;
    using SharpMap.Styles;
    using SharpMap.Layers;
    using SharpMap.Data.Providers;
    using System.Drawing.Imaging;
    using GeoAPI.Geometries;
    using GeoAPI;
    using NetTopologySuite;
    using NetTopologySuite.Geometries;
    using System.Linq;
    using System.Reflection;
    using Gtk;
    using Utility;
    using SharpMap.Data;
    using SharpMap.Rendering;
    using Extensions;

#if NETCOREAPP
    using ExposeEventArgs = Gtk.DrawnArgs;
    using StateType = Gtk.StateFlags;
#endif

    public class MapView : ViewBase, IMapView
    {
        private const double scrollIncrement = 60;

        private SharpMap.Map map;
        private Gtk.Image image;

        /// <summary>
        /// Is the user dragging the mouse?
        /// </summary>
        private bool isDragging;

        /// <summary>
        /// Position of the mouse when the user starts dragging.
        /// </summary>
        private Map.Coordinate mouseAtDragStart;

        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        public double Zoom
        {
            get
            {
                if (map == null)
                    return 0;
                return map.Zoom;
            }
            set
            {
                // Refreshing the map is a bit slow, so only do it if
                // the incoming value is different to the old value.
                if (map != null && !MathUtilities.FloatsAreEqual(value, map.Zoom))
                {
                    map.Zoom = value;
                    RefreshMap();
                }
            }
        }

        /// <summary>
        /// Center of the map.
        /// </summary>
        public Map.Coordinate Center
        {
            get
            {
                if (map == null)
                    return null;
                return new Map.Coordinate(map.Center.Y, map.Center.X);
            }
            set
            {
                // Refreshing the map is a bit slow, so only do it if
                // the incoming value is different to the old value.
                if (map != null && 
                    (!MathUtilities.FloatsAreEqual(value.Longitude, map.Center.X)
                    || !MathUtilities.FloatsAreEqual(value.Latitude, map.Center.Y)) )
                {
                    map.Center = new Coordinate(value.Longitude, value.Latitude);
                    RefreshMap();
                }
            }
        }

        /// <summary>
        /// GridView widget used to show properties. Could be refactored out.
        /// </summary>
        public IGridView Grid { get; private set; }

        /// <summary>
        /// Called when the view is changed by the user.
        /// </summary>
        public event EventHandler ViewChanged;

        /// <summary>
        /// Static constructor to perform 1-time initialisation.
        /// </summary>
        static MapView()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            GeoAPI.GeometryServiceProvider.Instance = new NtsGeometryServices();
            var css = new SharpMap.CoordinateSystems.CoordinateSystemServices(
            new ProjNet.CoordinateSystems.CoordinateSystemFactory(System.Text.Encoding.Unicode),
            new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory(),
            SharpMap.Converters.WellKnownText.SpatialReference.GetAllReferenceSystems());
            SharpMap.Session.Instance
            .SetGeometryServices(GeoAPI.GeometryServiceProvider.Instance)
            .SetCoordinateSystemServices(css)
            .SetCoordinateSystemRepository(css);
        }

        /// <summary>
        /// Constructor. Initialises the widget and will show a world
        /// map with no markers until <see cref="ShowMap" /> is called.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public MapView(ViewBase owner) : base(owner)
        {
            image = new Gtk.Image();
            var container = new Gtk.EventBox();
            container.Add(image);

            VPaned box = new VPaned();
            Grid = new GridView(this);
            box.Pack1(((ViewBase)Grid).MainWidget, true, false);
            box.Pack2(container, true, true);
            
            container.AddEvents(
              (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask
            | (int)Gdk.EventMask.ScrollMask);
            container.ButtonPressEvent += OnButtonPress;
            container.ButtonReleaseEvent += OnButtonRelease;
            image.SizeAllocated += OnSizeAllocated;
#if NETFRAMEWORK
            image.ExposeEvent += OnImageExposed;
#else
            image.Drawn += OnImageExposed;
#endif
            container.Destroyed += OnMainWidgetDestroyed;
            container.ScrollEvent += OnMouseScroll;

            mainWidget = box;
            mainWidget.ShowAll();
        }

        /// <summary>
        /// Initialise the map component.
        /// </summary>
        private SharpMap.Map InitMap()
        {
            var result = new SharpMap.Map();
            result.MaximumZoom = 720;
            result.BackColor = Color.LightBlue;
            result.Center = new Coordinate(0, 0);
            result.Zoom = result.MaximumZoom;
            
            VectorLayer layWorld = new VectorLayer("Countries");
            string bin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimx = Directory.GetParent(bin).FullName;
            string shapeFileName = Path.Combine(apsimx, "ApsimNG", "Resources", "world", "countries.shp");
            layWorld.DataSource = new ShapeFile(shapeFileName, true);
            layWorld.Style = new VectorStyle();
            layWorld.Style.EnableOutline = true;
            Color background = Colour.FromGtk(MainWidget.GetBackgroundColour(StateType.Normal));
            Color foreground = Colour.FromGtk(MainWidget.GetForegroundColour(StateType.Normal));
            layWorld.Style.Fill = new SolidBrush(background);
            layWorld.Style.Outline.Color = foreground;
            result.Layers.Add(layWorld);

            return result;
        }

        /// <summary>
        /// Export the map to an image.
        /// </summary>
        public System.Drawing.Image Export()
        {
            return map.GetMap();
        }

        public void HideZoomControls()
        {
            // Not applicable.
        }

        public void StoreSettings()
        {
            // Not applicable.
        }

        /// <summary>
        /// Show the given markers on the map and set the center/zoom level.
        /// </summary>
        /// <param name="coordinates">Coordinates of the markers.</param>
        /// <param name="locNames">Names of the marekrs (unused currently).</param>
        /// <param name="zoom">Zoom level of the map.</param>
        /// <param name="center">Location of the center of the map.</param>
        public void ShowMap(List<Map.Coordinate> coordinates, List<string> locNames, double zoom, Map.Coordinate center)
        {
            if (map != null)
                map.Dispose();
            map = InitMap();

            GeometryFactory gf = new GeometryFactory(new PrecisionModel(), 3857);
            List<IGeometry> locations = coordinates.Select(c => gf.CreatePoint(new Coordinate(c.Longitude, c.Latitude))).ToList<IGeometry>();
            VectorLayer markerLayer = new VectorLayer("Markers");
            markerLayer.Style.Symbol = GetResourceImage("ApsimNG.Resources.Marker.png");
            markerLayer.DataSource = new GeometryProvider(locations);
            map.Layers.Add(markerLayer);
            map.Zoom = zoom;
            map.Center = new Coordinate(center.Longitude, center.Latitude);
            if (image.Allocation.Width > 1 && image.Allocation.Height > 1)
                RefreshMap();
        }

        /// <summary>
        /// Refresh the map image shown in the UI.
        /// </summary>
        /// <remarks>
        /// This is fairly slow (often ~200ms), so try not to call it unnecessarily.
        /// </remarks>
        private void RefreshMap()
        {
            if (map != null)
                image.Pixbuf = ImageToPixbuf(map.GetMap());
        }

        /// <summary>
        /// Convert a System.Drawing.Image to a Gdk.Pixbuf.
        /// </summary>
        /// <param name="image">Image to be converted.</param>
        private static Gdk.Pixbuf ImageToPixbuf(System.Drawing.Image image)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                return new Gdk.Pixbuf(stream);
            }
        }

        /// <summary>
        /// Get an image from an embedded resource.
        /// </summary>
        /// <param name="resourceName">Name of the embedded resource.</param>
        private static System.Drawing.Image GetResourceImage(string resourceName)
        {
            //var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                return System.Drawing.Image.FromStream(stream);
        }

        /// <summary>
        /// Converts screen x/y coordinates to latitude/longitude on the map.
        /// Note that x/y must be relative to the GtkImage (image)'s GdkWindow.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="lat">Latitude.</param>
        /// <param name="lon">Longitude.</param>
        private void CartesianToGeoCoords(double x, double y, out double lat, out double lon)
        {
            Envelope viewport = map.Envelope;
            lat = y / map.Size.Height * (viewport.MinY - viewport.MaxY) + viewport.MaxY;
            lon = x / map.Size.Width * (viewport.MaxX - viewport.MinX) + viewport.MinX;
        }
    
        /// <summary>
        /// Traps the Exposed event for the image. This event fires after
        /// size/space allocation has occurred but before it is actually
        /// drawn on the screen. Because the size-allocated signal is emitted
        /// several times, we don't want to refresh the map each time.
        /// Therefore, we refresh the map once, during the expose event.
        /// 
        /// We also disconnect the event handler after refreshing the map so
        /// that we don't refresh it multiple times unnecessarily.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnImageExposed(object sender, ExposeEventArgs args)
        {
            try
            {
                RefreshMap();
#if NETFRAMEWORK
                image.ExposeEvent -= OnImageExposed;
#else
                image.Drawn -= OnImageExposed;
#endif
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the mouse button is pressed down. Records the
        /// mouse position, to be used to move map center when the
        /// mouse button is released.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnButtonPress(object sender, ButtonPressEventArgs args)
        {
            try
            {
                isDragging = true;
                CartesianToGeoCoords(args.Event.X, args.Event.Y, out double lat, out double lon);
                mouseAtDragStart = new Map.Coordinate(lat, lon);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the mouse button is released.
        /// Handles the map drag logic.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnButtonRelease(object sender, ButtonReleaseEventArgs args)
        {
            try
            {
                if (isDragging)
                {
                    CartesianToGeoCoords(args.Event.X, args.Event.Y, out double lat, out double lon);
                    double dy = lat - mouseAtDragStart.Latitude;
                    double dx = lon - mouseAtDragStart.Longitude;

                    map.Center = new Coordinate(map.Center.X - dx, map.Center.Y - dy);
                    RefreshMap();
                    ViewChanged?.Invoke(this, EventArgs.Empty);
                }
                isDragging = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the user scrolls with the mouse.
        /// Handles the zoom in/out logic.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnMouseScroll(object sender, ScrollEventArgs args)
        {
            try
            {
                Envelope viewport = map.Envelope;
                double mouseLat = args.Event.Y / map.Size.Height * (viewport.MinY - viewport.MaxY) + viewport.MaxY;
                double mouseLon = args.Event.X / map.Size.Width * (viewport.MaxX - viewport.MinX) + viewport.MinX;

                if (args.Event.Direction == Gdk.ScrollDirection.Up || args.Event.Direction == Gdk.ScrollDirection.Down)
                {
                    // Adjust zoom level on map.
                    double sign = args.Event.Direction == Gdk.ScrollDirection.Up ? -1 : 1;
                    map.Zoom = MathUtilities.Bound(map.Zoom + scrollIncrement * sign, 1, map.MaximumZoom);

                    // Adjust center of map, so that coordinates at mouse cursor are the same
                    // as previously.
                    viewport = map.Envelope;
                    double newMouseLat = args.Event.Y / map.Size.Height * (viewport.MinY - viewport.MaxY) + viewport.MaxY;
                    double newMouseLon = args.Event.X / map.Size.Width * (viewport.MaxX - viewport.MinX) + viewport.MinX;

                    double dx = newMouseLon - mouseLon;
                    double dy = newMouseLat - mouseLat;
                    map.Center = new Coordinate(map.Center.X - dx, map.Center.Y - dy);
                    RefreshMap();
                    ViewChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the image widget is allocated space.
        /// Changes the map's size to match allocated size.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnSizeAllocated(object sender, EventArgs args)
        {
            try
            {
                // Update the map size iff the allocated width and height are both > 0,
                // and width or height have changed.
                if (image != null && map != null &&
                    image.Allocation.Width > 0 && image.Allocation.Height > 0
                 && (image.Allocation.Width != map.Size.Width || image.Allocation.Height != map.Size.Height) )
                {
                    image.SizeAllocated -= OnSizeAllocated;
                    map.Size = new Size(image.Allocation.Width, image.Allocation.Height);
                    RefreshMap();
                    image.WidthRequest = image.Allocation.Width;
                    image.HeightRequest = image.Allocation.Height;
                    image.SizeAllocated += OnSizeAllocated;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Called when the main widget is destroyed.
        /// Detaches event handlers.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnMainWidgetDestroyed(object sender, EventArgs args)
        {
            try
            {
                mainWidget.ButtonPressEvent -= OnButtonPress;
                mainWidget.ButtonReleaseEvent -= OnButtonRelease;
                mainWidget.ScrollEvent -= OnMouseScroll;
                image.SizeAllocated -= OnSizeAllocated;
                mainWidget.Destroyed -= OnMainWidgetDestroyed;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}