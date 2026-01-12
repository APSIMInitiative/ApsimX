using APSIM.Shared.Utilities;
using Models.Storage;
using System;
using System.Data;
using System.IO;

namespace Models.Core
{
    enum MetadataCategory
    {
        None,
        Example,
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
                table.Columns.Add("SimulationName", typeof(string));
                table.Columns.Add("Catergory", typeof(string));
                table.Columns.Add("Model", typeof(string));
                table.Columns.Add("File", typeof(string));


                string name = simulation.Name;

                string apsimDirectory = PathUtilities.GetApsimXDirectory() + "/";
                string filepath = PathUtilities.ConvertSlashes(simulation.FileName);
                string filename = Path.GetFileNameWithoutExtension(Path.GetFileName(filepath));
                filepath = filepath.Replace(apsimDirectory, "");
                
                MetadataCategory category = MetadataCategory.None;
                foreach(MetadataCategory cat in Enum.GetValues(typeof(MetadataCategory)))
                    if (filepath.Contains(cat.ToString()))
                        category = cat;

                string model = "";
                if (category != MetadataCategory.None)
                {
                    string categoryString = category.ToString();
                    int position = filepath.IndexOf(categoryString) + categoryString.Length + 1;
                    int positionOfNextSlash = filepath.IndexOf('/', position);
                    model = filepath.Substring(position, positionOfNextSlash - position);
                }
                
                DataRow row = table.NewRow();
                row["SimulationName"] = name;
                row["Catergory"] = category.ToString();
                row["Model"] = model;
                row["File"] = filename;
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