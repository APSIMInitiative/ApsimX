using APSIM.Core;
using Models.Storage;
using System;
using System.Data;

namespace Models.Core
{

    enum MetadataCategory
    {
        Prototype,
        Validation
    }

    /// <summary>
    /// A simulation model
    /// </summary>
    public class Metadata
    {
        private Simulation simulation = null;

        private IDataStore datastore = null;

        /// <summary></summary>
        public Metadata(Simulation simulation, IDataStore datastore)
        {
            this.simulation = simulation;
            this.datastore = datastore;
        }

        /// <summary></summary>
        public void Save()
        {
            if (this.datastore != null)
            {
                var table = new DataTable("_Metadata");
                table.Columns.Add("SimulationID", typeof(int));
                table.Columns.Add("SimulationName", typeof(string));
                table.Columns.Add("Catergory", typeof(string));
                table.Columns.Add("Model", typeof(string));

                DataRow row = table.NewRow();
                row["SimulationID"] = 1;
                row["SimulationName"] = simulation.Name;
                row["Catergory"] = MetadataCategory.Validation.ToString();
                row["Model"] = "Model";
                table.Rows.Add(row);

                this.datastore.Writer.WriteTable(table, false);
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