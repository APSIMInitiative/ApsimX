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
using MigraDocCore.DocumentObjectModel.Tables;
using SixLabors.ImageSharp.Processing;


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
      /// Convert the table to model variables, ensure blanks are removed.
      /// </summary>
      public DataTable ConvertDisplayToModel(DataTable dt)
      {
         var myPaddockNames = new List<string>();
         var myIsManaged = new List<bool>();
         var myInitialState = new List<string>();
         var myDaysSinceHarvest = new List<int>();
         for (var row = 0; row < dt.Rows.Count; row++)
         {
            if ((dt.Rows[row][0] as string) != null)
            {
               myPaddockNames.Add(dt.Rows[row][0] as string);
               myIsManaged.Add(dt.Rows[row][1].ToString().ToLower() == "true" ? true : false);
               myInitialState.Add(dt.Rows[row][2] as string);
               int dsh = 0;
               int.TryParse(dt.Rows[row][3].ToString(), out dsh);
               myDaysSinceHarvest.Add(dsh);
            }  
         }
         PaddockNames = myPaddockNames.ToArray();
         IsManaged = myIsManaged.ToArray();
         InitialState = myInitialState.ToArray();
         DaysSinceHarvest = myDaysSinceHarvest.ToArray();
         return (dt);
      }

      /// <summary>
      /// Ensure any new child components added by user are present in the arrays
      /// </summary>
      public DataTable ConvertModelToDisplay(DataTable dt)
        { 
            var newFields = getFieldNames();

            var dtFields = new List<string>();
            for (var row = 0; row < dt.Rows.Count; row++) 
               if (dt.Rows[row]["Paddock"].ToString() != "")
                  dtFields.Add(dt.Rows[row]["Paddock"].ToString());

            foreach (var f in newFields)
               if ( ! dtFields.Contains(f)) {
                  DataRow newRow = dt.NewRow();
                  newRow["Paddock"] = f;
                  newRow["Managed"] = true;
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
