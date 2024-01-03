namespace UserInterface.EventArguments
{
    using System;
    using APSIM.Interop.Visualisation;

    public class GraphObjectSelectedArgs : EventArgs
    {
        public GraphObjectSelectedArgs()
        {
            Object1 = null;
            Object2 = null;
        }
        public GraphObjectSelectedArgs(DGObject a)
        {
            Object1 = a;
            Object2 = null;
        }
        public GraphObjectSelectedArgs(DGObject a, DGObject b)
        {
            Object1 = a;
            Object2 = b;
        }
        public DGObject Object1 { get; set; }
        public DGObject Object2 { get; set; }
    }
}