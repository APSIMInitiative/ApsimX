using System;
using System.Collections.Generic;
using System.Linq;

using Models.Core;
using Newtonsoft.Json;
using System.Data;
using Models.Utilities;
using Models.Functions;
using Models.Interfaces;

// TODO
// fuel & maintenace costs
// replacement, lifetime 
// operation queue
namespace Models.Management
{
    /// <summary>
    /// Track machinery availability, operating costs
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Factorial.CompositeFactor))]
    [ValidParent(ParentType = typeof(Factorial.Factor))]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class FarmMachinery : Model, IGridModel
    {
        /// <summary> Machinery </summary>
        /// <param name="tractor" />
        /// <param name="implement" />
        public bool MachineryAvailable (string tractor, string implement)
        {

           return false;
        }        

        /// <summary> </summary>
        [EventSubscribe("Operate")]
        public void OnOperate(object sender, FarmMachineryOperateArgs e)
        {
           var tractor = this.FindChild<FarmMachineryItem>(e.Tractor);
           var implement = this.FindChild<FarmMachineryItem>(e.Implement);
           int iRow;
           for(iRow = 0; iRow < TractorNames.Length; iRow++) {
               if (TractorNames[iRow] == e.Tractor &&
                   ImplementNames[iRow] == e.Implement)
                  break;
           }

           if (iRow >= TractorNames.Length)
               throw new Exception($"Cant find work rates for {e.Tractor} and {e.Implement}");

           var workRate = WorkRates[iRow];

           // fixme
           //double price = f?.Price ?? 0.0;
           //double amount = price * e.Yield * e.Area;
            
           Summary.WriteMessage(this, $"Operating {e.Tractor} and {e.Implement}", MessageType.Information); 
        }

        /// <summary>Operate a tractor/implement combo  </summary>
        public class FarmMachineryOperateArgs : EventArgs
        {
           /// <summary> </summary>
           public string Tractor { get; set; }
           /// <summary> </summary>
           public string Implement { get; set; }
           /// <summary> </summary>
           public string Paddock { get; set; }
           /// <summary> </summary>
           public double Area { get; set; }
        }        
    
        [Link] private Summary Summary = null;

        /// <summary>
        /// return a list of tractors we know about
        /// </summary>
        public List<string> getTractorNames () {
           var result  = this.FindAllChildren<FarmMachineryItem>().
                Where(i => i.MachineryType == MachineryType.Tractor).
                Select(i => i.Name).ToList();
           return result;
        }

        /// <summary>
        /// return a list of tractors we know about
        /// </summary>
        public List<string> getImplementNames () {
           var result  = this.FindAllChildren<FarmMachineryItem>().
                Where(i => i.MachineryType == MachineryType.Implement).
                Select(i => i.Name).ToList();
           return result;
        }

        /// <summary>Tabular data. Called by GUI.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {

                List<GridTableColumn> columns = new List<GridTableColumn>();
                columns.Add(new GridTableColumn("Tractor", new VariableProperty(this, GetType().GetProperty("TractorNames"))));
                columns.Add(new GridTableColumn("Implement", new VariableProperty(this, GetType().GetProperty("ImplementNames"))));
                columns.Add(new GridTableColumn("Work Rate (ha/hr)", new VariableProperty(this, GetType().GetProperty("WorkRates"))));
                columns.Add(new GridTableColumn("Fuel Consumption (l/hr)", new VariableProperty(this, GetType().GetProperty("FuelConsRates"))));

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
            var newCombos = new List<string>();
            foreach (var t in getTractorNames()) 
               foreach(var i in getImplementNames()) 
                  newCombos.Add(t + "." + i);

            var dtCombos = new List<string>();
            for (var row = 0; row < dt.Rows.Count; row++) 
               dtCombos.Add(dt.Rows[row]["Tractor"].ToString() + "." + dt.Rows[row]["Implement"].ToString());

            foreach (var combo in newCombos)
               if ( ! dtCombos.Contains(combo)) {
                  DataRow newRow = dt.NewRow();
                  newRow["Tractor"] = combo.Split(".")[0];
                  newRow["Implement"] = combo.Split(".")[1];
                  dt.Rows.Add(newRow);
               }
            return(dt);
        }

        /// <summary> </summary>
        public string[] TractorNames {get; set;}
        /// <summary> </summary>
        public string[] ImplementNames {get; set;}

        /// <summary> </summary>
        public double[] WorkRates {get; set;}
    
        /// <summary> </summary>
        public double[] FuelConsRates {get; set;}

        /// <summary> </summary>
        [EventSubscribe("StartOfSimulation")]
        public void OnStartOfSimulation(object sender, EventArgs e)
        {
        }

        /// <summary> </summary>
        [EventSubscribe("EndOfSimulation")]
        public void OnEndOfSimulation(object sender, EventArgs e)
        {
        }

        /// <summary> </summary>
        [EventSubscribe("StartOfDay")]
        public void DoStartOfDay(object sender, EventArgs e) 
        {
        }
        
        /// <summary> </summary>
        [EventSubscribe("EndOfDay")]
        public void DoEndOfDay(object sender, EventArgs e) 
        {
        }

        /// <summary> </summary>
        [EventSubscribe("DoManagement")]
        public void DoManagement(object sender, EventArgs e)
        {
        }
        
        //[Link]
        //private Simulation simulation = null;

        //[Link] IClock Clock = null;
    }
}
