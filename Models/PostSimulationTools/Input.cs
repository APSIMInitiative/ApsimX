namespace Models.PostSimulationTools
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Models.Storage;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Threading;
    using System.Xml.Serialization;

    /// <summary>
    /// # [Name]
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
    [ValidParent(ParentType=typeof(DataStore))]
    public class Input : Model, IPostSimulationTool, IReferenceExternalFiles
    {
        /// <summary>
        /// The DataStore.
        /// </summary>
        [Link]
        private IDataStore storage = null;

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
                if (storage == null)
                    return PathUtilities.GetAbsolutePath(this.FileName, (Apsim.Parent(this, typeof(Simulations)) as Simulations).FileName);
                return PathUtilities.GetAbsolutePath(this.FileName, storage.FileName);
            }

            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                this.FileName = PathUtilities.GetRelativePath(value, simulations.FileName);
            }
        }

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return new string[] { FileName };
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run()
        {
            string fullFileName = FullFileName;
            if (fullFileName != null)
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;

                DataTable data = GetTable();
                if (data != null)
                {
                    data.TableName = this.Name;
                    storage.Writer.WriteTable(data);
                }
            }
        }

        /// <summary>
        /// Provides an error message to display if something is wrong.
        /// </summary>
        [JsonIgnore]
        [NonSerialized]
        public string ErrorMessage = string.Empty;

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
                if (File.Exists(fullFileName))
                {
                    ApsimTextFile textFile = new ApsimTextFile();
                    try
                    {
                        textFile.Open(fullFileName);
                    }
                    catch (Exception err)
                    {
                        ErrorMessage = err.Message;
                        return null;
                    }
                    DataTable table = textFile.ToTable();
                    textFile.Close();

                    if (returnDataTable == null)
                        returnDataTable = table;
                    else
                        returnDataTable.Merge(table);
                }
                else
                {
                    ErrorMessage = "The specified file does not exist.";
                }
            }
            else
            {
                ErrorMessage = "Please select a file to use.";
            }

            return returnDataTable;
        }       
    }
}
