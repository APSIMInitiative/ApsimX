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
        private string[] fileNames;
        public string[] FileNames
        {
            get
            {
                return fileNames;
            }
            set
            {
                //attempt to use relative path for files in ApsimX install directory
                fileNames = new string[value.Length];
                for (int i = 0; i < fileNames.Length; i++)
                    fileNames[i] = value[i].Replace(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                        .Substring(0, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 3), "");
            }
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run(DataStore dataStore)
        {     
            if (FileNames != null)
            {
                Simulations simulations = ParentOfType(typeof(Simulations)) as Simulations;

                dataStore.DeleteTable(Name);
                DataTable data = GetTable(simulations.FileName);
                for (int i=0;i< FileNames.Length; i++)
                {
                    if (FileNames[i] == null)
                        continue;

                    dataStore.WriteTable(null, this.Name, data);
                }
            }
        }

        /// <summary>
        /// Return a datatable for this input file. Returns null if no data.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable(string SimPath)
        {
            DataTable returnDataTable = null;
            if (FileNames != null)
            {
                for (int i=0;i <FileNames.Length;i++)
                {
                    if (FileNames[i] == null)
                        continue;

                    string file = Utility.PathUtils.GetAbsolutePath(fileNames[i], SimPath);

                    if ( File.Exists(file))
                    {
                        Utility.ApsimTextFile textFile = new Utility.ApsimTextFile();
                        textFile.Open(file);
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
