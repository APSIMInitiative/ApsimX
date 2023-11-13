using System;
using Models.Management;

namespace ApsimNG.EventArguments.DirectedGraph
{
    public class AddArcEventArgs : EventArgs
    {
        public RuleAction Arc { get; set; }
    }
}