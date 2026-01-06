using APSIM.Core;
using Models.Storage;
using System;
using System.Data;

namespace Models.Core
{
    /// <summary>
    /// A simulation model
    /// </summary>
    public class Metadata
    {
        private Simulation simulation = null;

        private string catergory;

        private string model;

        /// <summary></summary>
        public Metadata(Simulation simulation)
        {
            this.simulation = simulation;
        }

        /// <summary></summary>
        public void Save()
        {
            DataStore datastore = simulation.Node.FindInScope<DataStore>();
            if (datastore != null)
            {
                var table = new DataTable("_Metadata");
                table.Columns.Add("catergory", typeof(string));
                table.Columns.Add("model", typeof(string));

                datastore.Writer.WriteTable(table, false);
            }
            return;
        }

        /// <summary></summary>
        public void OnSowing(object sender, EventArgs e)
        {
            return;
        }
    }
}