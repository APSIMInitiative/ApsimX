namespace Models.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a checkpoint from the db
    /// </summary>
    public class Checkpoint
    {
        /// <summary>The ID of the checkpoint.</summary>
        public int ID;

        /// <summary>Show the checkpoint on graphs?</summary>
        public bool ShowOnGraphs;
    }
}
