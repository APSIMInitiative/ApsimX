using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Dynamic Environmental Response Of Phenology And Potential Yield
    /// </summary>
    [ValidParent(ParentType = typeof(Plant))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class DEROPAPY: Model
    {
        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")] 
        public string CoeffientFile { get; set; }

        [JsonIgnore] private DataTable Covers { get; set; }


        //[Link] Simulation simulation;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
          //  string fullFileName = PathUtilities.GetAbsolutePath(CoeffientFile, simulation.FileName);

            //ApsimTextFile textFile = new ApsimTextFile();
            //textFile.Open(fullFileName);
            //Covers = textFile.ToTable();
            //textFile.Close();
        }
    }
}
