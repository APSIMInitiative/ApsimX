using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Data;
using System.Xml.Serialization;

namespace Models
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
    public class Input : Model
    {
        public string[] FileNames { get; set; }

        /// <summary>
        /// Go find the top level simulations object.
        /// </summary>
        protected Simulations Simulations
        {
            get
            {
                Model obj = this;
                while (obj.Parent != null && obj.GetType() != typeof(Simulations))
                    obj = obj.Parent;
                if (obj == null)
                    throw new ApsimXException(FullPath, "Cannot find a root simulations object");
                return obj as Simulations;
            }
        }


        public override void OnAllCompleted()
        {
            if (FileNames != null)
            {
                DataStore dataStore = new DataStore();
                dataStore.Connect(Path.ChangeExtension(Simulations.FileName, ".db"), readOnly: false);
                dataStore.DeleteTable(Name);
                foreach (string fileName in FileNames)
                {
                    if (fileName != null && File.Exists(fileName))
                    {
                        DataTable data = GetTable();
                        string[] simulationNames = Utility.DataTable.GetDistinctValues(data, "SimulationName").ToArray();
                        foreach (string simulationName in simulationNames)
                        {
                            DataView filteredData = new DataView(data);
                            filteredData.RowFilter = "SimulationName = '" + simulationName + "'";
                            dataStore.WriteTable(simulationName, this.Name, filteredData.ToTable());
                        }
                    }
                }
                dataStore.Disconnect();
            }
        }

        /// <summary>
        /// Return a datatable for this input file. Returns null if no data.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            DataTable returnDataTable = null;
            if (FileNames != null)
            {
                foreach (string fileName in FileNames)
                {
                    if (fileName != null && File.Exists(fileName))
                    {
                        Utility.ApsimTextFile textFile = new Utility.ApsimTextFile();
                        textFile.Open(fileName);
                        DataTable table = textFile.ToTable();
                        textFile.Close();

                        if (returnDataTable == null)
                            returnDataTable = table;
                        else
                            returnDataTable.Merge(table);
                    }
                }
            }
            return returnDataTable;

        }

    }




}
