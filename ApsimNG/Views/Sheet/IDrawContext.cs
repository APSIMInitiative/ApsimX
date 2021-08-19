using Cairo;

namespace UserInterface.Views
{
    public interface IDrawContext
    {
        double LineWidth { get; set; }

        object GetPixelExtents(string text);
        void Rectangle(Rectangle rectangle);
        void Clip();
        void Stroke();
        void MoveTo(double x, double y);
        void ResetClip();
    }
}