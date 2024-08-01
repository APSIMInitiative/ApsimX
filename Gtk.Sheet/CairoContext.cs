using Cairo;
using Gtk;
using APSIM.Interop.Drawing;
using Rectangle = System.Drawing.Rectangle;

namespace Gtk.Sheet
{
    public class CairoContext : IDrawContext
    {
        private Context cr;
        private Widget layoutGenerator;
        private States state;

        /// <summary>
        /// Create a new <see cref="CairoContext"/> instance.
        /// </summary>
        /// <param name="cr">The cairo context.</param>
        /// <param name="layoutGenerator">A gtk widget which can be used to create text layouts.</param>
        public CairoContext(Context cr, Widget layoutGenerator)
        {
            this.cr = cr;
            this.layoutGenerator = layoutGenerator;
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

                if (state == States.Insensitive)
                    layoutGenerator.StyleContext.State = Gtk.StateFlags.Insensitive;
                else if (state == States.Selected)
                    layoutGenerator.StyleContext.State = Gtk.StateFlags.Selected;
                else
                    layoutGenerator.StyleContext.State = Gtk.StateFlags.Normal;
                var c = layoutGenerator.StyleContext.GetColor(layoutGenerator.StyleContext.State);
                if (layoutGenerator.StyleContext.State == Gtk.StateFlags.Normal && state == States.Calculated)
                    cr.SetSourceColor(System.Drawing.Color.Red.ToCairo());
                else
                    cr.SetSourceColor(new Cairo.Color(c.Red, c.Green, c.Blue, c.Alpha));
            }
        }

        public void DrawFilledRectangle(int left, int top, int width, int height)
        {

            layoutGenerator.StyleContext.RenderBackground(cr, left, top, width, height);

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

        public (int Left, int Top, int Width, int Height) GetPixelExtents(string text, bool bold, bool italics)
        {
            var layout = CreateTextLayout(text, bold, italics);
            layout.GetPixelExtents(out Pango.Rectangle inkRectangle, out Pango.Rectangle logicalRectangle);
            return (logicalRectangle.X, logicalRectangle.Y, logicalRectangle.Width, logicalRectangle.Height);
        }

        public void MoveTo(double x, double y)
        {
            cr.MoveTo(x, y);
        }

        public void Rectangle(Rectangle rectangle)
        {
            cr.Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
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
            var layout = layoutGenerator.CreatePangoLayout(text);
            layout.FontDescription = new Pango.FontDescription();
            if (italics)
                layout.FontDescription.Style = Pango.Style.Italic;
            if (bold)
                layout.FontDescription.Weight = Pango.Weight.Bold;
            return layout;
        }

        public void SetColour(System.Drawing.Color color)
        {
            cr.SetSourceColor(color.ToCairo());
        }

        public void NewPath()
        {
            cr.NewPath();
        }

        public void CurveTo(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            cr.CurveTo(x0, y0, x1, y1, x2, y2);
        }

        public void LineTo(double x, double y)
        {
            cr.LineTo(x, y);
        }

        public void Arc(double xc, double yc, double radius, double angle1, double angle2)
        {
            cr.Arc(xc, yc, radius, angle1, angle2);
        }

        public void StrokePreserve()
        {
            cr.StrokePreserve();
        }

        public void Fill()
        {
            cr.Fill();
        }

        public void SetFontSize(double size)
        {
            cr.SetFontSize(size);
        }

        public Rectangle GetTextExtents(string text)
        {
            TextExtents extents = cr.TextExtents(text);
            return new Rectangle((int)extents.XBearing, (int)extents.YBearing, (int)extents.Width, (int)extents.Height);
        }

        public void ShowText(string text)
        {
            cr.ShowText(text);
        }
    }
}