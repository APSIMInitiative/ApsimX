namespace Gtk.Sheet
{
    /// <summary>Keys enumeration.</summary>
    internal enum Keys
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
        Delete,
        Tab,
        Period
    }

    internal class SheetEventKey
    {
        public Keys Key = Keys.None;
        public char KeyValue;
        public bool Control;
        public bool Shift;
    }
}