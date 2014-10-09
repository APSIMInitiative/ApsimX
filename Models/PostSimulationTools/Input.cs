using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Data;
using System.Xml.Serialization;

namespace Models.PostSimulationTools
{


    /// <summary>
    /// Reads the contents of a file (in apsim format) and stores into the DataStore. 
    /// If the file has a column name of 'SimulationName' then this model will only input data for those rows
    /// where the data in column 'SimulationName' matches the name of the simulation under which
    /// this input model sits. 
    /// 
    /// If the file does NOT have a 'SimulationName' column then all data will be input.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.InputView")]
    [PresenterName("UserInterface.Presenters.InputPresenter")]
    public class Input : Model, IPostSimulationTool
    {
        /// <summary>
        /// Gets or sets the file name to read from.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// </summary>
        [XmlIgnore]
        [Description("EXCEL file name")]
        public string FullFileName
        {
            get
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null && simulations.FileName != null)
                    return Utility.PathUtils.GetAbsolutePath(this.FileName, simulations.FileName);
                return null;
            }

            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                this.FileName = Utility.PathUtils.GetRelativePath(value, simulations.FileName);
            }
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run(DataStore dataStore)
        {
            string fullFileName = FullFileName;
            if (fullFileName != null)
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;

                dataStore.DeleteTable(Name);
                DataTable data = GetTable();
                dataStore.WriteTable(null, this.Name, data);
            }
        }

        /// <summary>
        /// Return a datatable for this input file. Returns null if no data.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            DataTable returnDataTable = null;
            string fullFileName = FullFileName;
            if (fullFileName != null)
            {
                if ( File.Exists(fullFileName))
                {
                    Utility.ApsimTextFile textFile = new Utility.ApsimTextFile();
                    textFile.Open(fullFileName);
                    DataTable table = textFile.ToTable();
                    textFile.Close();

                    if (returnDataTable == null)
                        returnDataTable = table;
                    else
                        returnDataTable.Merge(table);
                }
            }
         
            return returnDataTable;
        }
    }
}
