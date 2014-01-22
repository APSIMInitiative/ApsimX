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
    /// Reads the contents of a file (in apsim format) and stores into the DataStore
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.InputView")]
    [PresenterName("UserInterface.Presenters.InputPresenter")]
    public class Input : Model
    {
        [Link] Simulation Simulation = null;

        public string FileName { get; set; }

        // A property providing a full file name. The user interface uses this.
        [XmlIgnore]
        public string FullFileName
        {
            get
            {
                string FullFileName = FileName;
                if (Path.GetFullPath(FileName) != FileName)
                    FullFileName = Path.Combine(Path.GetDirectoryName(Simulation.FileName), FileName);
                return FullFileName;
            }
            set
            {
                FileName = value;

                // try and convert to path relative to the Simulations.FileName.
                FileName = FileName.Replace(Path.GetDirectoryName(Simulation.FileName) + @"\", "");
            }
        }

        public override void OnCompleted()
        {
            if (FileName != null && File.Exists(FullFileName))
            {
                DataStore dataStore = new DataStore();
                dataStore.Connect(Path.ChangeExtension(Simulation.FileName, ".db"));
                if (dataStore.TableNames.Contains(Name))
                {
                    // delete the old data.
                    dataStore.DeleteOldContentInTable(Simulation.Name, Name);
                }
                dataStore.CreateTable(Simulation.Name, this.Name, GetTable());
                dataStore.Disconnect();
            }
        }

        /// <summary>
        /// Return a datatable for this input file.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            if (FileName != null && File.Exists(FullFileName))
            {
                Utility.ApsimTextFile textFile = new Utility.ApsimTextFile();
                textFile.Open(FullFileName);
                DataTable table = textFile.ToTable();
                textFile.Close();
                return table;
            }
            return null;
        }

    }




}
