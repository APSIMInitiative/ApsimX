using APSIM.Shared.Utilities;
using Models.PMF;
using Models.Storage;
using System;
using System.Collections.Generic;
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

        private List<string> cultivars;

        /// <summary></summary>
        public Metadata(Simulation simulation, IDataStore datastore)
        {
            this.simulation = simulation;
            this.datastore = datastore;
            cultivars = new List<string>();
        }

        /// <summary></summary>
        public void Save()
        {
            if (this.datastore != null)
            {
                var table = new DataTable("_Metadata");
                table.Columns.Add("Simulation", typeof(string));
                table.Columns.Add("Catergory", typeof(string));
                table.Columns.Add("Directory", typeof(string));
                table.Columns.Add("Models", typeof(string));
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

                string directory = "";
                if (category != MetadataCategory.None)
                {
                    string categoryString = category.ToString();
                    int position = filepath.IndexOf(categoryString) + categoryString.Length + 1;
                    int positionOfNextSlash = filepath.IndexOf('/', position);
                    directory = filepath.Substring(position, positionOfNextSlash - position);
                }

                IEnumerable<IModel> models = simulation.Node.FindChildren<IModel>(recurse: true);
                List<string> modelNames = new List<string>();
                string modelsString = "";
                foreach(Model m in models)
                {
                    string type;
                    if (m.GetType() == typeof(Plant))
                    {
                        type = m.Name.ToString();
                    }
                    else
                    {
                        type = m.GetType().ToString();
                        type = type.Substring(type.LastIndexOf('.')+1);
                    }
                    if (!modelNames.Contains(type))
                    {
                        modelNames.Add(type);
                        modelsString += type + ",";
                    }
                }
                
                DataRow row = table.NewRow();
                row["Simulation"] = name;
                row["Catergory"] = category.ToString();
                row["Directory"] = directory;
                row["Models"] = modelsString;
                row["File"] = filename;
                row["Cultivars"] = filename;
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