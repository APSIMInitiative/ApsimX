namespace UserInterface.Views
{
    /// <summary>Keys enumeration.</summary>
    public enum Keys
    {
        None,
        Right,
        Left,
        Up,
        Down,
        PageUp,
        PageDown,
        Home,
        End,
        Return,
        Delete
    }

    public class SheetEventKey
    {
        public Keys Key = Keys.None;
        public char KeyValue;
        public bool Control;
        public bool Shift;
    }
}