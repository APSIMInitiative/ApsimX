namespace UserInterface.Views
{
    using APSIM.Interop.Mapping;
    using APSIM.Shared.Utilities;
    using Extensions;
    using GeoAPI.Geometries;
    using Gtk;
    using Interfaces;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Utility;
    using ApsimCoordinate = Models.Mapping.Coordinate;
    using Coordinate = GeoAPI.Geometries.Coordinate;
    using MapTag = Models.Mapping.MapTag;

    /// <remarks>
    /// This view is intended to diplay sites on a map. For the most part, in works, but it has a few flaws
    /// and room for improvement. 
    /// 
    /// Probably the main flaw is that maps are often very slow to render, as the basemap needs
    /// to be downloaded. SharpMap does allow map tiles to be loaded async, but when trying that approach I found
    /// it difficult to know when to update the map, and when it had been fully loaded (which is required when 
    /// generating auto-docs).
    /// 
    /// Another flaw (a problem with SharpMap) is that it doesn't know how to "wrap" the map at the antimeridion 
    /// (International Date Line). This makes it impossible to produce a Pacific-centered map.
    /// 
    /// One enhancement that should be fairly easy to implement would be to allow the user to select the basemap.
    /// Currently it's just using OpenStreetMaps, but Bing maps and several others are readily available. The user
    /// could be presented with a drop-down list of alternative.
    /// 
    /// There is a little quirk I don't understand at all - the location marker simply disappears from the map
    /// at high resolutions. For our purposes, this generally shouldn't be a problem, but it would be nice to 
    /// know why it happens.
    /// 
    /// </remarks>
    public class MapView : ViewBase, IMapView
    {
        /// <summary>
        /// Width of the map as shown in the GUI. I'm setting
        /// this to 718 to match the default page width of the autodocs
        /// documents.
        /// </summary>
        /// <remarks>
        /// todo: should really check the default page size dynamically.
        /// </remarks>
        private const int defaultWidth = 718;

        /// <summary>
        /// Height of the map as shown in the GUI. I'm setting
        /// this to 718 to match the default page width of the autodocs
        /// documents.
        /// </summary>
        /// <remarks>
        /// todo: should really check the default page size dynamically.
        /// </remarks>
        private const int defaultHeight = 718;

        private SharpMap.Map map;
        private Gtk.Image image;
        private Gtk.EventBox container;

        /// <summary>
        /// Is the user dragging the mouse?
        /// </summary>
        private bool isDragging;

        /// <summary>
        /// Position of the mouse when the user starts dragging.
        /// </summary>
        private ApsimCoordinate mouseAtDragStart;

        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        public double Zoom
        {
            get
            {
                if (map == null)
                    return 0;
                return Math.Round(Math.Log(map.MaximumZoom / map.Zoom, MapRenderer.GetZoomStepFactor()) + 1.0, 2);
             }
            set
            {
                // Refreshing the map is a bit slow, so only do it if
                // the incoming value is different to the old value.
                if (map != null && !MathUtilities.FloatsAreEqual(value, Zoom))
                {
                    double setValue = value - 1.0;
                    if (value >= 60.0) // Convert any "old" zoom levels into whole-world maps
                        setValue = 0.0;
                    map.Zoom = map.MaximumZoom / Math.Pow(MapRenderer.GetZoomStepFactor(), setValue);
                    RefreshMap();
                }
            }
        }

        /// <summary>
        /// Center of the map.
        /// </summary>
        public ApsimCoordinate Center
        {
            get
            {
                if (map == null)
                    return null;
                Coordinate centerLatLon = MapRenderer.GetMetresToLatLon().MathTransform.Transform(map.Center);
                return new ApsimCoordinate(Math.Round(centerLatLon.Y, 4), Math.Round(centerLatLon.X, 4));
            }
            set
            {
                Coordinate centerMetric = MapRenderer.GetLatLonToMetres().MathTransform.Transform(new Coordinate(value.Longitude, value.Latitude));
                // Refreshing the map is a bit slow, so only do it if
                // the incoming value is different to the old value.
                if (map != null && 
                    (!MathUtilities.FloatsAreEqual(centerMetric.X, map.Center.X)
                    || !MathUtilities.FloatsAreEqual(centerMetric.Y, map.Center.Y)) )
                {
                    map.Center = centerMetric;
                    RefreshMap();
                }
            }
        }

        /// <summary>
        /// GridView widget used to show properties. Could be refactored out.
        /// </summary>
        public IPropertyView PropertiesView { get; private set; }

        /// <summary>
        /// Called when the view is changed by the user.
        /// </summary>
        public event EventHandler ViewChanged;


        /// <summary>
        /// Constructor. Initialises the widget and will show a world
        /// map with no markers until <see cref="ShowMap" /> is called.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public MapView(ViewBase owner) : base(owner)
        {
            image = new Gtk.Image();

            image.Halign = Align.Start;
            image.Valign = Align.Start;

            container = new Gtk.EventBox();
            container.Add(image);

            VPaned box = new VPaned();
            PropertiesView = new PropertyView(this);
            box.Pack1(((ViewBase)PropertiesView).MainWidget, true, false);

            if ( ((ViewBase)PropertiesView).MainWidget is ScrolledWindow scroller)
                scroller.VscrollbarPolicy = PolicyType.Never;

            box.Pack2(container, true, true);
            
            container.AddEvents(
              (int)Gdk.EventMask.ButtonPressMask
            | (int)Gdk.EventMask.ButtonReleaseMask
            | (int)Gdk.EventMask.ScrollMask);
            container.ButtonPressEvent += OnButtonPress;
            container.ButtonReleaseEvent += OnButtonRelease;

            image.Drawn += OnImageExposed;

            container.Destroyed += OnMainWidgetDestroyed;
            container.ScrollEvent += OnMouseScroll;

            mainWidget = box;
            mainWidget.ShowAll();
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
        public void ShowMap(List<ApsimCoordinate> coordinates, List<string> locNames, double zoom, ApsimCoordinate center)
        {
            if (map != null)
                map.Dispose();

            map = new MapTag(center, zoom, coordinates).ToSharpMap();
            map.Size = new Size(defaultWidth, defaultHeight);
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
                stream.Seek(0, SeekOrigin.Begin);
                return new Gdk.Pixbuf(stream);
            }
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
            Coordinate coord = map.ImageToWorld(new PointF((float)x, (float)y), true);
            lat = coord.Y;
            lon = coord.X;
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
        private void OnImageExposed(object sender, DrawnArgs args)
        {
            try
            {
                RefreshMap();

                image.Drawn -= OnImageExposed;

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
                mouseAtDragStart = new ApsimCoordinate(lat, lon);
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
                    double sign = args.Event.Direction == Gdk.ScrollDirection.Up ? 1 : -1;
                    Zoom = MathUtilities.Bound(Zoom + sign, 1.0, 25.0);

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
        /// Called when the main widget is destroyed.
        /// Detaches event handlers.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        private void OnMainWidgetDestroyed(object sender, EventArgs args)
        {
            try
            {
                image.Drawn -= OnImageExposed;
                image.Dispose();
                (PropertiesView as PropertyView).Dispose();
                container.ButtonPressEvent -= OnButtonPress;
                container.ButtonReleaseEvent -= OnButtonRelease;
                container.ScrollEvent -= OnMouseScroll;
                container.Destroyed -= OnMainWidgetDestroyed;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}