using Gtk;

namespace UserInterface.Classes
{
    /// <summary>
    /// We want to have a "button" we can press within a grid cell. We could use a Gtk CellRendererPixbuf for this, 
    /// but that doesn't provide an easy way to detect a button press, so instead we can use a "toggle", but 
    /// override the Render function to simply display our Pixbuf
    /// </summary>
    public class CellRendererActiveButton : CellRendererToggle
    {
        /// <summary>
        /// Gets or sets the pixbuf object
        /// </summary>
        public Gdk.Pixbuf Pixbuf { get; set; }

        /// <summary>
        /// Location of the cell? If so, which coordinate system is this in?
        /// </summary>
        public Gdk.Rectangle LastRect { get; set; }


        /// <summary>
        /// Override the OnRender function to add a button to the cell.
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="widget"></param>
        /// <param name="background_area"></param>
        /// <param name="cell_area"></param>
        /// <param name="flags"></param>
        protected override void OnRender(Cairo.Context cr, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
        {
            //base.OnRender(cr, widget, background_area, cell_area, flags);
            LastRect = new Gdk.Rectangle(cell_area.X, cell_area.Y, cell_area.Width, cell_area.Height);
            // This probably doesn't work
            cr.SetSource(new Cairo.SurfacePattern(new Cairo.ImageSurface(Pixbuf.SaveToBuffer("png"), Cairo.Format.ARGB32, cell_area.Width, cell_area.Height, 1)));
        }

    }
}
