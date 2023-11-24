using System.Collections.Generic;
using System;

namespace APSIM.Shared.Graphing
{
    /// <summary>Encapsulates an arc on a directed graph</summary>
    [Serializable]
    public class Arc : DirectedGraphObject
    {
        /// <summary>Source node (where arc starts)</summary>
        public int SourceID { get; set; }

        /// <summary>Destination node (where arc finishes)</summary>
        public int DestinationID { get; set; }

        /// <summary>Test conditions that need to be satisfied for this transition</summary>
        public List<string> Conditions { get; set; }

        /// <summary>Actions undertaken when making this transition</summary>
        public List<string> Actions { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Arc()
        {
        }

        /// <summary>
        /// Create a copy of the given arc.
        /// </summary>
        /// <param name="x">An arc to be copied.</param>
        public Arc(Arc x)
        {
            if (x != null)
                CopyFrom(x);
        }

        /// <summary>
        /// Copy all properties from a given arc.
        /// </summary>
        /// <param name="x">An <see cref="Arc" />.</param>
        public void CopyFrom(Arc x)
        {
            ID = x.ID;
            Name = x.Name;
            Location = x.Location;
            Colour = x.Colour;
            SourceID = x.SourceID;
            DestinationID = x.DestinationID;
            Conditions = new List<string>(x.Conditions);
            Actions = new List<string>(x.Actions);
        }
    }
}
