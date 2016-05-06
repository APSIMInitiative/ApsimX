// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    ///using GMap.NET.WindowsForms;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    ///using System.Windows.Forms;

    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IMapView
    {
        /// <summary>Show the map</summary>
        void ShowMap(List<Models.Map.Coordinate> coordinates);

        /// <summary>Export the map to an image.</summary>
        Image Export();

    }

    /// <summary>
    /// A Windows forms implementation of an AxisView
    /// </summary>
    public partial class MapView : /* UserControl, TBI */ IMapView
    {
        /// <summary>Construtor</summary>
        public MapView()
        {
            /// TBI InitializeComponent();
        }

        /// <summary>Show the map</summary>
        public void ShowMap(List<Models.Map.Coordinate> coordinates)
        {
            /* TBI
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
            */
        }

        /// <summary>
        /// Export the map to an image.
        /// </summary>
        public Image Export()
        {
            return null; /// TBI  mapControl.ToImage();
        }

     
    }
}
