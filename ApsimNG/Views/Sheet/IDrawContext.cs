namespace UserInterface.Views
{
    public enum States
    {
        Normal,
        Selected,
        Insensitive
    }

    public interface IDrawContext
    {
        void SetLineWidth(double lineWidth);
        States State { get; set; }

        (int Left, int Right, int Width, int Height) GetPixelExtents(string text, bool bold, bool italics);
        void Rectangle(CellBounds rectangle);
        void Clip();
        void Stroke();
        void MoveTo(double x, double y);
        void ResetClip();
        void DrawFilledRectangle(int left, int top, int v1, int v2);
        void DrawFilledRectangle();
        void SetColour((int Red, int Green, int Blue) color);
        void DrawText(string text, bool bold, bool italics);
    }
}