using Cairo;

namespace UserInterface.Views
{
    public class CellBounds
    {
        public CellBounds(int cellX, int cellY, int cellWidth, int cellHeight)
        {
            Left = cellX;
            Top = cellY;
            Width = cellWidth;
            Height = cellHeight;
        }
        public int Left { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }
        public int Right => Left + Width;
        public int Bottom => Top + Height;

        public Rectangle ToRectangle()
        {
            return new Rectangle(Left, Top, Width, Height);
        }

        public Rectangle ToClippedRectangle(int windowWidth, int windowHeight)
        {
            if (Right > windowWidth || Bottom > windowHeight)
                return new Rectangle(Left, Top, windowWidth - Left, windowHeight - Top);
            else
                return ToRectangle();
        }
    }
}
