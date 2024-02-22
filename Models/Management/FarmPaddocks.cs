using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

using Models.Core;
using System.Data;
using Models.Storage;
using Models.Functions;
using Models.Utilities;
using Models.Interfaces;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;


namespace Models.Management
{
    /// <summary>
    /// A crop cost / price 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Manager))]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class FarmPaddocks : Model, IGridModel
    {
        /// <summary> </summary>
        public string[] PaddockNames {get; set;}
        /// <summary> </summary>
        public bool[] IsManaged {get; set;}
        /// <summary> </summary>
        public string[] InitialState {get; set;}

        /// <summary> </summary>
        public int[] DaysSinceHarvest {get; set;}

        /// <summary>Tabular data. Called by GUI.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                List<GridTableColumn> columns = new List<GridTableColumn>();
                // fixme - these should be dropdown lists
                columns.Add(new GridTableColumn("Paddock", 
                   new VariableProperty(this, GetType().GetProperty("PaddockNames"))));
                columns.Add(new GridTableColumn("Managed", 
                   new VariableProperty(this, GetType().GetProperty("IsManaged"))));
                columns.Add(new GridTableColumn("Initial State", 
                   new VariableProperty(this, GetType().GetProperty("InitialState"))));
                columns.Add(new GridTableColumn("Days since harvest (d)", 
                   new VariableProperty(this, GetType().GetProperty("DaysSinceHarvest"))));

                List<GridTable> tables = new List<GridTable>();
                tables.Add(new GridTable(Name, columns, this));
                return tables;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
           return(dt);
        }

        /// <summary>
        /// Ensure any new child components added by user are present in the arrays
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        { 
            var newFields = getFieldNames();

            var dtFields = new List<string>();
            for (var row = 0; row < dt.Rows.Count; row++) 
               dtFields.Add(dt.Rows[row]["Paddock"].ToString());

            foreach (var f in newFields)
               if ( ! dtFields.Contains(f)) {
                  DataRow newRow = dt.NewRow();
                  newRow["Paddock"] = f;
                  newRow["Managed"] = false;
                  newRow["Initial State"] = "NA";
                  newRow["Days since harvest (d)"] = 0;
                  dt.Rows.Add(newRow);
               }
            return(dt);
        }

        /// <summary> </summary>
        public List<string> getFieldNames () {
           var simulation = this.FindAncestor<Simulation>();
           var result  = simulation.FindAllChildren<Zone>().
                Select(i => i.Name).ToList();
           return result;
        }
    }
}