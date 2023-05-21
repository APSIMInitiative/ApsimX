namespace UserInterface.Views
{
    using System;

    /// <summary>An event argument structure with a string.</summary>
    public class TabClosingEventArgs : EventArgs
    {
        public bool LeftTabControl;
        public string Name;
        public int Index;
        public bool AllowClose = true;
    }

}
