namespace ApsimNG.EventArguments.DirectedGraph
{
    using System;
    using System.Collections.Generic;
    using APSIM.Interop.Visualisation;

    public class GraphObjectsArgs : EventArgs
    {
        public List<DGObject> Objects { get; set; }
        public GraphObjectsArgs(List<DGObject> objs)
        {
            Objects = objs;
        }
    }
}