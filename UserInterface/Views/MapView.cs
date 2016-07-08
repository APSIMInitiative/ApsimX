// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using GMap.NET.WindowsForms;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IMapView
    {
        /// <summary>
        /// Invoked when the zoom level is changed
        /// </summary>
        event EventHandler ZoomChanged;

        /// <summary>
        /// Invoked when the map center is changed
        /// </summary>
        event EventHandler PositionChanged;

        /// <summary>Show the map</summary>
        void ShowMap(List<Models.Map.Coordinate> coordinates);

        /// <summary>Export the map to an image.</summary>
        Image Export();

        /// <summary>
        /// Get or set the zoom factor of the map
        /// </summary>
        double Zoom { get; set; }

        /// <summary>
        /// Get or set the center position of the map
        /// </summary>
        Models.Map.Coordinate Center { get; set; }
    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class MapView : UserControl, IMapView
    {
        /// <summary>
        /// Invoked when the zoom level is changed
        /// </summary>
        public event EventHandler ZoomChanged;

        /// <summary>
        /// Invoked when the map center is changed
        /// </summary>
        public event EventHandler PositionChanged;

        /// <summary>Construtor</summary>
        public MapView()
        {
            InitializeComponent();
        }

        /// <summary>Show the map</summary>
        public void ShowMap(List<Models.Map.Coordinate> coordinates)
        {
            mapControl.ScaleMode = ScaleModes.Fractional;
            mapControl.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            mapControl.MinZoom = 1;
            mapControl.MaxZoom = 17;
            mapControl.Zoom = 1.4; //2;
            
            var overlayOne = new GMapOverlay("OverlayOne");
            mapControl.Overlays.Add(overlayOne);

            foreach (Models.Map.Coordinate coordinate in coordinates)
            {
                GMap.NET.PointLatLng point = new GMap.NET.PointLatLng(coordinate.Latitude,coordinate.Longitude);
                overlayOne.Markers.Add(new GMap.NET.WindowsForms.Markers.GMarkerGoogle(point, 
                                       GMap.NET.WindowsForms.Markers.GMarkerGoogleType.green));
            }
        }

        /// <summary>
        /// Get or set the zoom factor of the map
        /// </summary>
        public double Zoom
        {
            get
            {
                return mapControl.Zoom;
            }
            set
            {
                mapControl.Zoom = value;
            }
        }

        /// <summary>
        /// Get or set the center position of the map
        /// </summary>
        public Models.Map.Coordinate Center
        {
            get
            {
                Models.Map.Coordinate center = new Models.Map.Coordinate();
                center.Latitude = mapControl.Position.Lat;
                center.Longitude = mapControl.Position.Lng;
                return center;
            }
            set
            {
                mapControl.Position = new GMap.NET.PointLatLng(value.Latitude, value.Longitude);
            }
        }

        /// <summary>
        /// Export the map to an image.
        /// </summary>
        public Image Export()
        {
            return mapControl.ToImage();
        }

        private void mapControl_OnPositionChanged(GMap.NET.PointLatLng point)
        {
            if (PositionChanged != null)
            {
                PositionChanged.Invoke(mapControl, EventArgs.Empty);
            }

        }

        private void mapControl_OnMapZoomChanged()
        {
            if (ZoomChanged != null)
            {
                ZoomChanged.Invoke(mapControl, EventArgs.Empty);
            }
        }
    }
}
