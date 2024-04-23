namespace UserInterface.Views
{
    public interface IEventScroll
    {
        object Direction { get; }
        bool DirectionIsSmooth { get; }
        int DeltaY { get; }
        bool IsDirectionDown { get; }
    }
}