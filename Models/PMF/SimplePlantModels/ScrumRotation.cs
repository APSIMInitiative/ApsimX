using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;



namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Dynamic Environmental Response Of Phenology And Potential Yield
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class ScrumRotation : Model
    {
        private DataTable dataTable;

        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")]
        [Display(Type = DisplayType.FileName)]
        public string CoefficientFile { get; set ; }
        
        
        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this.
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                Simulation simulation = FindAncestor<Simulation>();
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(this.CoefficientFile, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this.CoefficientFile, simulations.FileName);
                    else
                        return this.CoefficientFile;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    this.CoefficientFile = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this.CoefficientFile = value;
                readCSVandUpdateProperties();
            }
        }

        ///<summary></summary> 
        [JsonIgnore] public string[] ParamName { get; set; }

        /// <summary>
        /// List of crops specified in the CoefficientFile
        /// </summary>
        [JsonIgnore] public string[] CropNames { get; set; }

        ///<summary>parameters for the current crop</summary> 
        [JsonIgnore] public ScrumManagementInstance CurrentCropManagement { get; set; }

        /// <summary>clock</summary>
        [Link]
        public Clock clock = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        ////// This secton contains the components that get values from the csv coefficient file to    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////// display in the grid view and set them back to the csv when they are changed in the grid !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        private DataTable readCSVandUpdateProperties()
        {
            dataTable = new DataTable();
            using (StreamReader reader = new StreamReader(FullFileName))
            {
                string[] headers = reader.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    dataTable.Columns.Add(header);
                }
                while (!reader.EndOfStream)
                {
                    string[] rows = reader.ReadLine().Split(',');
                    dataTable.Rows.Add(rows);
                }
            }
            dataTable.RowChanged += OnRowChanged;
            return dataTable;
        }

        /// <summary>Gets or sets the table of values.</summary>
        [Display]
        public DataTable Data
        {
            get
            {
                readCSVandUpdateProperties();
                return dataTable;
            }
        }

        /// <summary>
        /// Invoked when a row of the table is changed by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRowChanged(object sender, DataRowChangeEventArgs e)
        {
            saveToCSV(FullFileName, dataTable);
        }

        /// <summary>
        /// Writes the data from the grid to the csv file
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="dt"></param>
        /// <exception cref="Exception"></exception>
        private void saveToCSV(string filepath, DataTable dt)
        {
            try
            {
                string contents = "";

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dt.Columns[i].ColumnName))
                    {
                        contents += dt.Columns[i].ColumnName.ToString();
                    }
                    if (i < dt.Columns.Count - 1)
                    {
                        contents += ",";
                    }
                }
                contents += "\n";

                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            contents += dr[i].ToString();
                        }
                        if (i < dt.Columns.Count - 1)
                        {
                            contents += ",";
                        }
                    }
                    contents += "\n";
                }

                StreamWriter s = new StreamWriter(filepath, false);
                s.Write(contents);
                s.Close();
            }
            catch
            {
                throw new Exception("Error Writing File");
            }
        }

        ////// This secton contains the components that take the management parameters from the table and sends them to SCRUM on sow date !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private int rotPos = 0;
        private ScrumCropInstance currentCrop = null;
        private bool currentCropEstablished;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            tabDat = readCSVandUpdateProperties();
            rotPos = 1;
            setCurrentCrop(rotPos);
            currentCropEstablished = false;
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if ((clock.Today == CurrentCropManagement.EstablishDate)&& (CurrentCropManagement.HarvestDate <= clock.EndDate))
            {
                currentCrop.Establish(CurrentCropManagement);
                currentCropEstablished = true;

            }
        }

        private DataTable tabDat = null;

        [EventSubscribe("DoManagementCalculations")]
        private void OnDoManagementCalculations(object sender, EventArgs e)
        {
            if ((clock.Today == CurrentCropManagement.HarvestDate)&&(currentCropEstablished))
            {
                currentCropEstablished = false;
                rotPos +=1;
                if (rotPos>tabDat.Columns.Count-1)
                {
                    rotPos = 1;
                }
                setCurrentCrop(rotPos);
            }
        }

        private void setCurrentCrop(int rotPos)
        {
            CurrentCropManagement = getCurrentParams(tabDat, rotPos);
            currentCrop = zone.FindDescendant<ScrumCropInstance>(CurrentCropManagement.CropName);
            if (currentCrop == null) { throw new Exception("Can not find a ScrumCropInstance named " + CurrentCropManagement.CropName + " in the simulation"); }
        }

        /// <summary>
        /// Gets the parameter set from the CoeffientFile for the Rotation position specified and returns in a ScrumManagementInstance.
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="rotPos"></param>
        /// <returns></returns>
        private ScrumManagementInstance getCurrentParams(DataTable tab, int rotPos)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            //ret.Add("CropName", tab.Columns[rotPos].ToString());
            for (int i = 0; i < tab.Rows.Count; i++)
            {
                ret.Add(tab.Rows[i]["Inputfield"].ToString(), tab.Rows[i][rotPos].ToString());
            }
            ScrumManagementInstance retSMI = new ScrumManagementInstance(ret, clock.Today);
            return retSMI;
        }
        
    }
}
