using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace Models.PMF
{
    /// <summary>
    /// Dynamic Environmental Response Of Phenology And Potential Yield
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class SetCropParamsInSimulation : Model 
    {
        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")]
        [Display(Type = DisplayType.FileName)]
        public string CoefficientFile { get; set; }


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
        private Zone zone = null;

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
            saveToCSV(FullFileName, dt);

            return new DataTable();
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
        /// Procedures that occur for crops that go into the EndCrop Phase
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        [EventSubscribe("Sowing")]
        private void onSowing(object sender, EventArgs e)
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
                    Pval = CurrentSimParams[CurrentSimulationName];
                    zone.Set(varName, Pval);
                }
            }
        }

        /// <summary>
        /// Helper method that takes data from cs and gets into format needed to be a for Cultivar overwrite
        /// </summary>
        /// <param name="dirty"></param>
        /// <returns></returns>
        private string clean(string dirty)
        {
            string ret = dirty.Replace("(", "").Replace(")", "");
            Regex sWhitespace = new Regex(@"\s+");
            return sWhitespace.Replace(ret, ",");
        }

    }
}
