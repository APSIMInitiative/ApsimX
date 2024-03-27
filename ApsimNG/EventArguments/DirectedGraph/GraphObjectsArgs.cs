namespace ApsimNG.EventArguments.DirectedGraph
{
    using System;
    using System.Collections.Generic;
    using APSIM.Shared.Graphing;

    public class GraphObjectsArgs : EventArgs
    {
        public List<GraphObject> Objects { get; set; }
        public GraphObjectsArgs(List<GraphObject> objs)
        {
            Objects = objs;
        }
    }
}