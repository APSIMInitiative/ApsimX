using System;
using ApsimNG.Classes.DirectedGraph;

namespace UserInterface.EventArguments
{
    public class GraphChangedEventArgs : EventArgs
    {
        public Models.RotBubbleChart.RBGraph model { get; set; }
    }
    public class AddNodeEventArgs : EventArgs
    {
        public Models.RotBubbleChart.StateNode Node { get; set; }
    }
    public class DelNodeEventArgs : EventArgs
    {
        public string nodeNameToDelete;
    }
    public class AddArcEventArgs : EventArgs
    {
        public Models.RotBubbleChart.RuleAction Arc { get; set; }
    }
    public class DelArcEventArgs : EventArgs
    {
        public string arcNameToDelete;
    }
    public class InitialStateEventArgs : EventArgs
    {
        public string initialState;
    }
}
