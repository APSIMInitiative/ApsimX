// -----------------------------------------------------------------------
// <copyright file="AxisView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    ///using GMap.NET.WindowsForms;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading;
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
    public class MapView : HTMLView, IMapView
    {
        /// <summary>Construtor</summary>
        public MapView(ViewBase owner) : base(owner)
        {
        }

        /// <summary>Show the map</summary>
        public void ShowMap(List<Models.Map.Coordinate> coordinates)
        {
            string html =
@"<html>
<head>
 <script src='http://maps.googleapis.com/maps/api/js?key=AIzaSyC6OF6s7DwSHwibtQqAKC9GtOQEwTkCpkw'>
 </script>
 <script>
  var locations = [";

            for (int i = 0; i < coordinates.Count; i++)
            {
                html += "[" + coordinates[i].Latitude.ToString() + ", " + coordinates[i].Longitude.ToString() + "]";
                if (i < coordinates.Count - 1)
                    html += ',';
            }
            html += @"
  ];
  
  var myCenter;
  if (locations.length > 0)
    myCenter = new google.maps.LatLng(locations[0][0], locations[0][1]);
  else
    myCenter = new google.maps.LatLng(0.0, 0.0);
  var map = null;
  function initialize()
  {
     var mapProp = {
       center:myCenter,
       zoom: 6,
       mapTypeId: google.maps.MapTypeId.TERRAIN
     };

     var infowindow = new google.maps.InfoWindow({maxWidth: 120});
     
     map = new google.maps.Map(document.getElementById('googleMap'), mapProp);

     var marker, i;
     for (i = 0; i < locations.length; i++)
     {
        marker = new google.maps.Marker({position: new google.maps.LatLng(locations[i][0], locations[i][1])});
        marker.setMap(map);
        google.maps.event.addListener(marker, 'click', (function(marker, i) {
         return function(event) {
             infowindow.setContent('Latitude: ' + locations[i][0] + '<br>Longitude: ' + locations[i][1]);
             infowindow.open(map, marker);
         }
        })(marker, i));
     }
   }
      
google.maps.event.addDomListener(window, 'load', initialize);
</script>
</head>
<body>
<div id='googleMap' style='width: 100%; height: 100%;'></div>
</body>
</html>";
            SetContents(html, false);
        }

    /// <summary>
    /// Export the map to an image.
    /// </summary>
    public Image Export()
        {
            // Create a Bitmap and draw the DataGridView on it.
            int width;
            int height;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (watch.ElapsedMilliseconds < 10) // Give the browser a fraction of a second to run all its scripts
                Gtk.Application.RunIteration();
            Gdk.Window gridWindow = MainWidget.GdkWindow;
            gridWindow.GetSize(out width, out height);
            Gdk.Pixbuf screenshot = Gdk.Pixbuf.FromDrawable(gridWindow, gridWindow.Colormap, 0, 0, 0, 0, width, height);
            byte[] buffer = screenshot.SaveToBuffer("png");
            MemoryStream stream = new MemoryStream(buffer);
            System.Drawing.Bitmap bitmap = new Bitmap(stream);
            return bitmap;
        }

     
    }
}
