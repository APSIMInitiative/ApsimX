using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Functions;
using Models.Interfaces;
using Models.Management;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using Models.PMF.Phen;
using Models.Utilities;
using Newtonsoft.Json;
using PdfSharpCore.Pdf.Content.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Dynamic Environmental Response Of Phenology And Potential Yield
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class DEROPAPY : Model, IGridModel
    {
        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")]
        public string CoeffientFile { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Description("Name of the crop to in simulation")]
        public string CropName { get; set; }

        [JsonIgnore] private DataTable Covers { get; set; }

        ///<summary></summary> 
        [JsonIgnore] public string[] ParamName { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] ParamUnit { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] Description { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] Maize { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] Apple { get; set; }


        private void setCropCoefficients(string CropName)
        {
            Simulation sim = (Simulation)this.FindAllAncestors<Simulation>().FirstOrDefault();
            string fullFileName = PathUtilities.GetAbsolutePath(CoeffientFile, sim.FileName);

            ApsimTextFile textFile = new ApsimTextFile();
            textFile.Open(fullFileName);
            Covers = textFile.ToTable();
            textFile.Close();

            ParamName = repack(Covers, 0);
            ParamUnit = repack(Covers, 1);
            Description = repack(Covers, 2);
            Maize = repack(Covers, 3);  
            Apple =  repack(Covers, 4);
        }

        private string[] repack(DataTable tab, int colIndex)
        {
            string[] ret = new string[tab.Rows.Count];
            for (int i = 0; i<tab.Rows.Count; i++)
            {
                ret[i] = tab.Rows[i][colIndex].ToString();
            }
            return ret;
        }


       /// <summary>
       /// Gets or sets the table of values.
       /// </summary>
       [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                setCropCoefficients(CropName);
                
                List<GridTableColumn> columns = new List<GridTableColumn>();

                foreach (DataColumn col in Covers.Columns)
                {
                    columns.Add(new GridTableColumn(col.ColumnName, new VariableProperty(this, GetType().GetProperty(col.ColumnName))));
                }

                List<GridTable> tables = new List<GridTable>();
                tables.Add(new GridTable(Name, columns, this));

                return tables;
            }
        }

        /// <summary>
        /// Renames column headers for display
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            dt.Columns["ParamName"].ColumnName = "Param Name";
            dt.Columns["ParamUnit"].ColumnName = "Units";
            dt.Columns["Description"].ColumnName = "Description";
            dt.Columns["Maize"].ColumnName = "Maize";
            dt.Columns["Apple"].ColumnName = "Apple";
            return dt;
        }

        /// <summary>
        /// Renames the columns back to model property names
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            dt.Columns["Param Name"].ColumnName = "ParamName";
            dt.Columns["Units"].ColumnName = "ParamUnit";
            dt.Columns["Description"].ColumnName = "Description";
            dt.Columns["Maize"].ColumnName = "Maize";
            dt.Columns["Apple"].ColumnName = "Apple";
            return dt;
        }

        

}
}
