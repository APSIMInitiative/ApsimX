using System;
using ApsimNG.Classes.DirectedGraph;

namespace UserInterface.EventArguments
{
    public class AddNodeEventArgs : EventArgs
    {
        public string Name { get; set; }
        public System.Drawing.Color Background { get; set; }
        public System.Drawing.Color Outline { get; set; }
    }
    public class DupNodeEventArgs : EventArgs
    {
        public string nodeNameToDuplicate;
    }
    public class DelNodeEventArgs : EventArgs
    {
        public string nodeNameToDelete;
    }
}
