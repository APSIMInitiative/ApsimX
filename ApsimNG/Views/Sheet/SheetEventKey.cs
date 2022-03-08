namespace UserInterface.Views
{
    /// <summary>Keys enumeration.</summary>
    public enum Keys
    {
        Right,
        Left,
        Up,
        Down,
        PageUp,
        PageDown,
        Return
    }

    public class SheetEventKey
    {
        public Keys Key;
        public bool Control;
    }
}