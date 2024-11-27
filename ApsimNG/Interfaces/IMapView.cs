using System;
using System.Collections.Generic;
using Models.Mapping;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// Describes an interface for an axis view.
    /// </summary>
    interface IMapView
    {
        /// <summary>
        /// Invoked when the zoom level or map center is changed
        /// </summary>
        event EventHandler ViewChanged;

        /// <summary>Show the map</summary>
        void ShowMap(List<Coordinate> coordinates, List<string> locNames, double zoom, Coordinate center);

        /// <summary>Export the map to an image.</summary>
        Gdk.Pixbuf Export();

        /// <summary>
        /// Get or set the zoom factor of the map
        /// </summary>
        double Zoom { get; set; }

        /// <summary>
        /// Get or set the center position of the map
        /// </summary>
        Coordinate Center { get; set; }

        /// <summary>
        /// Store current position and zoom settings
        /// </summary>
        void StoreSettings();

        /// <summary>
        /// Hide zoom controls.
        /// </summary>
        void HideZoomControls();

        IPropertyView PropertiesView { get; }
    }
}
