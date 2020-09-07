using System;
using System.Collections.Generic;
using ApsimNG.Classes.DirectedGraph;
using Models.Management;

namespace UserInterface.EventArguments
{
    public class GraphChangedEventArgs : EventArgs
    {
        public List<StateNode> Nodes { get; set; }
        public List<RuleAction> Arcs { get; set; }
    }
    public class AddNodeEventArgs : EventArgs
    {
        public StateNode Node { get; set; }
    }
    public class DelNodeEventArgs : EventArgs
    {
        public string nodeNameToDelete { get; set; }
    }
    public class AddArcEventArgs : EventArgs
    {
        public RuleAction Arc { get; set; }
    }
    public class DelArcEventArgs : EventArgs
    {
        public string arcNameToDelete { get; set; }
    }
    public class InitialStateEventArgs : EventArgs
    {
        public string initialState { get; set; }
    }
}