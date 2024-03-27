namespace APSIM.Interop.Visualisation
{
    using System.Drawing;
    using APSIM.Interop.Drawing;

    /// <summary>
    /// An arc on a directed graph.
    /// </summary>
    public class DGRectangle
    {
        Rectangle rect;

        /// <summary>Constrcutor</summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="width">width</param>
        /// <param name="height">height</param>
        public DGRectangle(int x, int y, int width, int height)
        {
            this.rect = new Rectangle(x, y, width, height);
        }

        /// <summary>Paint on the graphics context</summary>
        /// <param name="context">The graphics context to draw on</param>
        public void Paint(IDrawContext context)
        {
            context.SetColour(Color.Yellow);
            context.SetLineWidth(2);
            context.Rectangle(rect);
            context.StrokePreserve();
        }
        public Rectangle GetRectangle()
        {
            return rect;
        }
    }
}
