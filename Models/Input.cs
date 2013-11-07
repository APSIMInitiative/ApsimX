using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Data;

namespace Models
{


    /// <summary>
    /// Reads the contents of a file (in apsim format) and stores into the DataStore
    /// </summary>
    [ViewName("UserInterface.Views.InputView")]
    [PresenterName("UserInterface.Presenters.InputPresenter")]
    public class Input : Model
    {
        [Link] DataStore DataStore = null;
        [Link] Simulation Simulation = null;

        public string FileName { get; set; }

        [EventSubscribe("AllCompleted")]
        private void AllCompleted(object sender, EventArgs e)
        {
            if (DataStore == null)
                throw new ApsimXException(this.FullPath, "Cannot find data store.");

            if (FileName != null && File.Exists(FileName))
                DataStore.CreateTable(Simulation.Name, this.Name, GetTable());
        }

        /// <summary>
        /// Return a datatable for this input file.
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            if (FileName != null && File.Exists(FileName))
            {
                Utility.ApsimTextFile textFile = new Utility.ApsimTextFile();
                textFile.Open(FileName);
                DataTable table = textFile.ToTable();
                textFile.Close();
                return table;
            }
            return null;
        }

    }




}
