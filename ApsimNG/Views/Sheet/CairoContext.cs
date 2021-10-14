using Cairo;

namespace UserInterface.Views
{
    internal class CairoContext : IDrawContext
    {
        private Context cr;
        private SheetWidget sheetWidget;
        private States state;

        public CairoContext(Context cr, SheetWidget sheetWidget)
        {
            this.cr = cr;
            this.sheetWidget = sheetWidget;
        }

        public void SetLineWidth(double lineWidth)
        {
            cr.LineWidth = lineWidth;
        }

        public States State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
#if NETCOREAPP
                if (state == States.Insensitive)
                    sheetWidget.StyleContext.State = Gtk.StateFlags.Insensitive;
                else if (state == States.Selected)
                    sheetWidget.StyleContext.State = Gtk.StateFlags.Selected;
                else
                    sheetWidget.StyleContext.State = Gtk.StateFlags.Normal;
                var c = sheetWidget.StyleContext.GetColor(sheetWidget.StyleContext.State);
                cr.SetSourceColor(new Cairo.Color(c.Red, c.Green, c.Blue, c.Alpha));
#endif
            }
        }

        /// <summary>
        /// Set the current colour.
        /// </summary>
        /// <param name="colour"></param>
        public void SetColour((int Red, int Green, int Blue) colour)
        {
            cr.SetSourceColor(new Cairo.Color(colour.Red/255.0, colour.Green/255.0, colour.Blue/255.0));
        }

        public void DrawFilledRectangle(int left, int top, int width, int height)
        {
#if NETCOREAPP
            sheetWidget.StyleContext.RenderBackground(cr, left, top, width, height);
#endif
        }

        public void DrawFilledRectangle()
        {
            cr.Fill();
        }

        public void Clip()
        {
            cr.Clip();
        }

        public void DrawText(string text, bool bold, bool italics)
        {
            Pango.CairoHelper.ShowLayout(cr, CreateTextLayout(text, bold, italics));
        }

        public (int Left, int Right, int Width, int Height) GetPixelExtents(string text, bool bold, bool italics)
        {
            var layout = CreateTextLayout(text, bold, italics);
            layout.GetPixelExtents(out Pango.Rectangle inkRectangle, out Pango.Rectangle logicalRectangle);
            return (logicalRectangle.X, logicalRectangle.Y, logicalRectangle.Width, logicalRectangle.Height);
        }

        public void MoveTo(double x, double y)
        {
            cr.MoveTo(x, y);
        }

        public void Rectangle(CellBounds rectangle)
        {
            cr.Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
        }

        public void ResetClip()
        {
            cr.ResetClip();
        }

        public void Stroke()
        {
            cr.Stroke();
        }

        private Pango.Layout CreateTextLayout(string text, bool bold, bool italics)
        {
            var layout = sheetWidget.CreatePangoLayout(text);
            layout.FontDescription = new Pango.FontDescription();
            if (italics)
                layout.FontDescription.Style = Pango.Style.Italic;
            if (bold)
                layout.FontDescription.Weight = Pango.Weight.Bold;
            return layout;
        }
    }
}