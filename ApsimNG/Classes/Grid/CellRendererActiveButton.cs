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
        /// Render the cell in the window
        /// </summary>
        /// <param name="window">The owning window</param>
        /// <param name="widget">The widget</param>
        /// <param name="background_area">Background area</param>
        /// <param name="cell_area">The cell area</param>
        /// <param name="expose_area">Expose the area</param>
        /// <param name="flags">Render flags</param>
        protected override void Render(Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
        {
            lastRect = new Gdk.Rectangle(cell_area.X, cell_area.Y, cell_area.Width, cell_area.Height);
            Gdk.GC gc = new Gdk.GC(window);
            window.DrawPixbuf(gc, Pixbuf, 0, 0, cell_area.X, cell_area.Y, Pixbuf.Width, Pixbuf.Height, Gdk.RgbDither.Normal, 0, 0);
        }

        public Gdk.Rectangle lastRect;
    }
}
