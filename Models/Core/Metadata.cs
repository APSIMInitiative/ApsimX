using APSIM.Shared.Utilities;
using Models.Storage;
using System;
using System.Data;
using System.IO;

namespace Models.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class Metadata
    {
        private enum MetadataCategory
        {
            None,
            Example,
            Prototype,
            Validation
        }

        private Simulation _simulation = null;

        private IDataStore _dataStore = null;

        /// <summary></summary>
        public Metadata(Simulation simulation, IDataStore dataStore)
        {
            _simulation = simulation;
            _dataStore = dataStore;
        }

        /// <summary></summary>
        public void Save()
        {
            if (_dataStore != null)
            {
                //Save metadata about simulations
                DataTable metadataTable = new DataTable("_Metadata");
                metadataTable.Columns.Add("simulation", typeof(string));
                metadataTable.Columns.Add("catergory", typeof(string));
                metadataTable.Columns.Add("directory", typeof(string));
                metadataTable.Columns.Add("file", typeof(string));

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
                
                DataRow row = metadataTable.NewRow();
                row["simulation"] = name;
                row["catergory"] = category.ToString();
                row["directory"] = directory;
                row["file"] = filename;
                metadataTable.Rows.Add(row);

                _dataStore.Writer.WriteTable(metadataTable, false);
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