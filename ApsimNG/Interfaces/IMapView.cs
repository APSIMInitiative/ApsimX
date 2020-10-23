using System;
using System.Collections.Generic;
using System.Drawing;

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
        void ShowMap(List<Models.Map.Coordinate> coordinates, List<string> locNames, double zoom, Models.Map.Coordinate center);

        /// <summary>Export the map to an image.</summary>
        System.Drawing.Image Export();

        /// <summary>
        /// Get or set the zoom factor of the map
        /// </summary>
        double Zoom { get; set; }

        /// <summary>
        /// Get or set the center position of the map
        /// </summary>
        Models.Map.Coordinate Center { get; set; }

        /// <summary>
        /// Store current position and zoom settings
        /// </summary>
        void StoreSettings();

        /// <summary>
        /// Hide zoom controls.
        /// </summary>
        void HideZoomControls();

        IGridView Grid { get; }
    }
}
