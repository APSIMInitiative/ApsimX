using Cairo;
using UserInterface.Views;

namespace UserInterface.Extensions
{
    public static class CellBoundsExtensions
    {
        /// <summary>Convert the cell bounds into a Cairo rectange.</summary>
        /// <param name="cell">The cell to be converted.</param>
        public static Rectangle ToRectangle(this CellBounds cell)
        {
            return new Rectangle(cell.Left, cell.Top, cell.Width, cell.Height);
        }

        /// <summary>Converts the cell bounds into a Cairo rectange clipped to a window width and height.</summary>
        /// <param name="cell">The cell to be converted.</param>
        /// <param name="windowWidth">Width of a window in pixels.</param>
        /// <param name="windowHeight">Height of a window in pixels.</param>
        public static Rectangle ToClippedRectangle(this CellBounds cell, int windowWidth, int windowHeight)
        {
            if (cell.Right > windowWidth || cell.Bottom > windowHeight)
                return new Rectangle(cell.Left, cell.Top, windowWidth - cell.Left, windowHeight - cell.Top);
            else
                return cell.ToRectangle();
        }
    }
}