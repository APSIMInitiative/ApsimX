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
        [Link] DataStore DataStore = null;
        [Link] Simulation Simulation = null;
        [Link] Simulations Simulations = null;

        public string FileName { get; set; }

        // A property providing a full file name. The user interface uses this.
        [XmlIgnore]
        public string FullFileName
        {
            get
            {
                string FullFileName = FileName;
                if (Path.GetFullPath(FileName) != FileName)
                    FullFileName = Path.Combine(Path.GetDirectoryName(Simulations.FileName), FileName);
                return FullFileName;
            }
            set
            {
                FileName = value;

                // try and convert to path relative to the Simulations.FileName.
                FileName = FileName.Replace(Path.GetDirectoryName(Simulations.FileName) + @"\", "");
            }
        }

        [EventSubscribe("AllCompleted")]
        private void AllCompleted(object sender, EventArgs e)
        {
            if (DataStore == null)
                throw new ApsimXException(this.FullPath, "Cannot find data store.");

            if (FileName != null && File.Exists(FullFileName))
                DataStore.CreateTable(Simulation.Name, this.Name, GetTable());
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
