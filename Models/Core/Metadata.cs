using APSIM.Shared.Utilities;
using Models.Storage;
using System;
using System.Data;
using System.IO;

namespace Models.Core
{
    /// <summary>
    /// The metadata class is used to gather information about each simulation 
    /// that is run and saves that to the datastore in a _Metadata table.
    /// This class is not a model and instead sits within the Simulation model
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// Types of simulations that exist within the APSIM validation system
        /// </summary>
        private enum MetadataCategory
        {
            None,
            Example,
            Prototype,
            Validation
        }

        private Simulation _simulation = null;

        private IDataStore _dataStore = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public Metadata(Simulation simulation, IDataStore dataStore)
        {
            _simulation = simulation;
            _dataStore = dataStore;
        }

        /// <summary>
        /// Save metadata to the datastore
        /// </summary>
        public void Save()
        {
            if (_dataStore != null)
            {
                //setup table
                DataTable metadataTable = new DataTable("_Metadata");
                metadataTable.Columns.Add("simulation", typeof(string));
                metadataTable.Columns.Add("catergory", typeof(string));
                metadataTable.Columns.Add("directory", typeof(string));
                metadataTable.Columns.Add("file", typeof(string));

                //gather details
                string name = _simulation.Name;

                string apsimDirectory = PathUtilities.GetApsimXDirectory() + "/";
                string filepath = PathUtilities.ConvertSlashes(_simulation.FileName);
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
                
                //create a row in the table
                DataRow row = metadataTable.NewRow();
                row["simulation"] = name;
                row["catergory"] = category.ToString();
                row["directory"] = directory;
                row["file"] = filename;
                metadataTable.Rows.Add(row);

                //write to datastore
                _dataStore.Writer.WriteTable(metadataTable, false);
            }
            return;
        }

        /// <summary>
        /// A function that is called when a sowing event happens. As metadata 
        /// isn't a model, Simulation actually listens for the event, and then 
        /// calls this to pass the details along.
        /// Currently not used, but keeping the connection intact for later.
        /// </summary>
        public void OnSowing(object sender, EventArgs e)
        {
            return;
        }
    }
}