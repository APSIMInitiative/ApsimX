namespace UserInterface.Views
{
    /// <summary>Encapsulates the xy pixel bounds of a sheet cell.</summary>
    public class CellBounds
    {
        /// <summary>Constructor</summary>
        /// <param name="cellX">The top left corner x position of the cell in pixels.</param>
        /// <param name="cellY">The top left corner y position of the cell in pixels.</param>
        /// <param name="cellWidth">The width of the cell in pixels.</param>
        /// <param name="cellHeight">The height of the cell in pixels.</param>
        public CellBounds(int cellX, int cellY, int cellWidth, int cellHeight)
        {
            Left = cellX;
            Top = cellY;
            Width = cellWidth;
            Height = cellHeight;
        }

        /// <summary>The top left corner x position of the cell in pixels.</summary>
        public int Left { get; }

        /// <summary>The top left corner y position of the cell in pixels.</summary>
        public int Top { get; }

        /// <summary>The width of the cell in pixels.</summary>
        public int Width { get; }

        /// <summary>The height of the cell in pixels.</summary>
        public int Height { get; }

        /// <summary>The top right corner x position of the cell in pixels</summary>
        public int Right => Left + Width;

        /// <summary>The bottom right corner y position of the cell in pixels</summary>
        public int Bottom => Top + Height;

        /// <summary>Converts the cell bounds into a Cairo rectange clipped to a window width and height.</summary>
        /// <param name="windowWidth">Width of a window in pixels.</param>
        /// <param name="windowHeight">Height of a window in pixels.</param>
        public CellBounds ToClippedRectangle(int windowWidth, int windowHeight)
        {
            if (Right > windowWidth || Bottom > windowHeight)
                return new CellBounds(Left, Top, windowWidth - Left, windowHeight - Top);
            else
                return this;
        }

        /// <summary>
        /// Test for equality between CellBound instances.
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public override bool Equals(object rhs)
        {
            if (rhs is CellBounds cellBounds)
            {
                if (Left != cellBounds.Left)
                    return false;
                if (Top != cellBounds.Top)
                    return true;
                if (Right != cellBounds.Right)
                    return false;
                if (Bottom != cellBounds.Bottom)
                    return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (Left, Top, Right, Bottom).GetHashCode();
        }
    }
}