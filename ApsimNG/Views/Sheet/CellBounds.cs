using Cairo;

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

        /// <summary>Convert the cell bounds into a Cairo rectange.</summary>
        public Rectangle ToRectangle()
        {
            return new Rectangle(Left, Top, Width, Height);
        }

        /// <summary>Converts the cell bounds into a Cairo rectange clipped to a window width and height.</summary>
        /// <param name="windowWidth">Width of a window in pixels.</param>
        /// <param name="windowHeight">Height of a window in pixels.</param>
        public Rectangle ToClippedRectangle(int windowWidth, int windowHeight)
        {
            if (Right > windowWidth || Bottom > windowHeight)
                return new Rectangle(Left, Top, windowWidth - Left, windowHeight - Top);
            else
                return ToRectangle();
        }
    }
}