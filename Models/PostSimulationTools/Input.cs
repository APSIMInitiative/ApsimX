using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Interfaces;
using Models.Storage;
using Models.Utilities;
using Newtonsoft.Json;

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
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
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
        public string[] FileNames { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// </summary>
        [JsonIgnore]
        [Description("EXCEL file name")]
        public string[] FullFileNames
        {
            get
            {
                if (FileNames == null)
                    return null;

                if (storage == null)
                    return FileNames.Select(f => PathUtilities.GetAbsolutePath(f, FindAncestor<Simulations>().FileName)).ToArray();
                return FileNames.Select(f => PathUtilities.GetAbsolutePath(f, storage.FileName)).ToArray();
            }

            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                this.FileNames = value.Select(v => PathUtilities.GetRelativePath(v, simulations.FileName)).ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        [Display]
        public DataTable Data
        {
            get
            {
                return GetTable();
            }
        }

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return FileNames;
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            for (int i = 0; i < FileNames.Length; i++)
                FileNames[i] = Path.GetFileName(FileNames[i]);
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run()
        {
            if (FullFileNames != null)
            {
                foreach (string fileName in FullFileNames)
                {
                    if (string.IsNullOrEmpty(fileName))
                        continue;

                    DataTable data = GetTable(fileName);
                    if (data != null)
                    {
                        data.TableName = Name;
                        storage.Writer.WriteTable(data);
                    }
                }
            }
        }

        /// <summary>
        /// Return a datatable for this input file. Returns null if no data.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            if (FullFileNames != null)
                if (FullFileNames.Length > 0 && !String.IsNullOrEmpty(FullFileNames[0])) 
                        return GetTable(FullFileNames[0]);
            return null;
        }

        /// <summary>
        /// Return a datatable for this input file. Returns null if no data.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable(string fileName)
        {
            ApsimTextFile textFile = new ApsimTextFile();
            try
            {
                if (File.Exists(fileName))
                {
                    textFile.Open(fileName);
                    DataTable data = textFile.ToTable();
                    foreach (DataColumn column in data.Columns)
                        column.ReadOnly = true;
                    return data;
                }
                else
                    throw new Exception($"The specified file '{fileName}' does not exist.");
            }
            catch (Exception err)
            {
                throw new Exception($"Error in input file {Name} while reading file {fileName}", err);
            }
            finally
            {
                textFile?.Close();
            }
        }
    }
}
