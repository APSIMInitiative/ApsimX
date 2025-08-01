using APSIM.Core;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace Models.Utilities
{
    /// <summary>
    /// Reads a .csv file containing parameter values and sets those into the specified model for the specified simulatoin only.
    /// Must have a column with a header called "SimulatoinName" containing simulation names that match the simulation you are wanting to overwrite parameters in.
    /// Contains additional columns for each model parameter that is to be overwritten.
    /// Each parameter column must have a header name that matches a full parameter address (e.g [Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue)
    /// Set desired parameter values down the column to match the SimulationName specified in each row.
    /// </summary>
    [Core.Description("Reads a .csv file containing parameter values and sets those into the specified model for the specified simulatoin only.  \n" +
                      "Must have a column with a header called \"SimulatoinName\" containing simulation names that match the simulation you are wanting to overwrite parameters in.  \n" +
                      "Contains additional columns for each model parameter that is to be overwritten.  \n" +
                      "Each parameter column must have a header name that matches a full parameter address (e.g [Wheat].Phenology.Phyllochron.BasePhyllochron.FixedValue)  \n" +
                      "Set desired parameter values down the column to match the SimulationName specified in each row.")]

    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SetModelParamsBySimulation : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        /// <summary>Location of file with crop specific coefficients</summary>
        [Core.Description("File path for parameter file")]
        [Display(Type = DisplayType.FileName)]
        public string ParameterFile { get; set; }

        /// <summary>Location of file with crop specific coefficients</summary>
        [Core.Description("Event to apply sets on")]
        public string SetEventName { get; set; }


        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

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
                    return PathUtilities.GetAbsolutePath(ParameterFile, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(ParameterFile, simulations.FileName);
                    else
                        return ParameterFile;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    ParameterFile = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    ParameterFile = value;
                readCSVandUpdateProperties();
            }
        }

        private string CurrentSimulationName
        {
            get
            { if (simulation != null)
                    return  simulation.Name;
            else
                    return null;
            }
        }

        ///<summary></summary>
        [JsonIgnore] public Dictionary<string, string> CurrentSimParams { get; set; }

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;


        ////// This secton contains the components that get values from the csv coefficient file to    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////// display in the grid view and set them back to the csv when they are changed in the grid !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        private DataTable readCSVandUpdateProperties()
        {
            DataTable readData = new DataTable();
            readData = ApsimTextFile.ToTable(FullFileName);
            if (readData.Rows.Count == 0)
                throw new Exception("Failed to read any rows of data from " + FullFileName);
            return readData;
        }

        /// <summary>Gets or sets the table of values.</summary>
        [JsonIgnore]
        public List<DataTable> Tables
        {
            get
            {
                List<DataTable> tables = new List<DataTable>
                {
                    //new DataTable("", new List<DataTableColumn>(), this)
                };
                return tables;
            }
        }

        /// <summary>
        /// Reads in the csv data and sends it as a datatable to the grid
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            DataTable dt2 = new DataTable();
            try
            {
                dt2 = readCSVandUpdateProperties();
            }
            catch
            {
                dt2 = new DataTable();
            }
            return dt2;
        }

        /// <summary>
        /// Writes out changes from the grid to the csv file
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            return new DataTable();
        }


        /// <summary>
        /// Gets the parameter set from the CoeffientFile for the CropName specified and returns in a dictionary maped to paramter names.
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="simName"></param>
        /// <param name="varName"></param>
        /// <returns></returns>
        private Dictionary<string, string> getCurrentParams(DataTable tab, string simName, string varName)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            for (int i = 0; i < tab.Rows.Count; i++)
            {
                ret.Add(tab.Rows[i]["SimulationName"].ToString(), tab.Rows[i][varName].ToString());
            }
            return ret;
        }

        /// <summary>
        /// Connect event handlers.
        /// </summary>
        /// <param name="sender">Sender object..</param>
        /// <param name="args">Event data.</param>
        [EventSubscribe("SubscribeToEvents")]
        private void OnConnectToEvents(object sender, EventArgs args)
        {
            if (!string.IsNullOrEmpty(SetEventName))
            {
                events.Subscribe(SetEventName, onSetEvent);
            }
        }

        /// <summary>
        /// Method to find the current simulation row in ParameterFile and step throuh each column applying parameter sets
        /// </summary>
        private void onSetEvent(object sender, EventArgs e)
        {
            DataTable setVars = readCSVandUpdateProperties();
            string varName = "";
            foreach (DataColumn column in setVars.Columns)
            {
                varName = column.ColumnName.ToString();
                if (varName != "SimulationName")
                {
                    CurrentSimParams = getCurrentParams(setVars, CurrentSimulationName, varName);
                    object Pval = 0;
                    try
                    {
                        Pval = CurrentSimParams[CurrentSimulationName];
                    }
                    catch
                    {
                        throw new WarningException(Name + " was not able to set any model parameters because simulatoin name " +
                                                 CurrentSimulationName + " is not present in the SimulationName column in " + FullFileName);
                    }
                    if (Pval.ToString() != "")
                        Structure.Set(varName, Pval);
                }
            }
        }
    }
}
