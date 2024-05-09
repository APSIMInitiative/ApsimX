using System;
using System.Collections.Generic;
using System.Linq;

using Models.Core;
using Models.Factorial;
using System.Data;
using Models.Storage;
using Models.Functions;

// TODO
// machinery
// annual overheads & capital repayments
namespace Models.Management
{
    /// <summary>
    /// Keeps a balance, writes a log.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Factorial.CompositeFactor))]
    [ValidParent(ParentType = typeof(Factorial.Factor))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class FarmEconomics : Model
    {
        /// <summary> Farm level expenditure</summary>
         //   expenditure {cost 42.0} {comment "description here"}
        public class FarmExpenditureArgs : EventArgs
        {
           /// <summary> </summary>
           public string Description { get; set; }
           /// <summary> </summary>
           public string Category { get; set; }
           /// <summary> </summary>
           public double Cost  { get; set; } 
        }        
        /// <summary>Field level expendiuture  </summary>
        //   expenditure {category seed} {name wheat} {rate 120} {comment "description here"}
        public class PaddockExpenditureArgs : EventArgs
        {
           /// <summary> </summary>
           public string Description { get; set; }
           /// <summary> </summary>
           public string Category { get; set; }
           /// <summary> </summary>
           public string Paddock { get; set; }
           /// <summary> </summary>
           public string Name { get; set; }
           /// <summary> </summary>
           public double Rate { get; set; }
           /// <summary> </summary>
           public double Area { get; set; } 
        }

        /// <summary> Farm level income</summary>
        //   income {amount 64000.0} {comment "description here"}
        public class FarmIncomeArgs : EventArgs
        {
           /// <summary> </summary>
           public string Description { get; set; }
           /// <summary> </summary>
           public string Category { get; set; }
           /// <summary> </summary>
           public double Amount  { get; set; } // whole $/farm
        }
        /// <summary> Paddock level income</summary>
        //   income {category cropprice} {name wheat} {yield 4000} {protein 12.3} {comment "description here"}
        //   income {category cropprice} {name sorghum} {yield 6000} {comment "description here"}
        public class PaddockIncomeArgs : EventArgs
        {
           /// <summary> </summary>
           public string Description { get; set; }
           /// <summary> </summary>
           public string Category { get; set; }
           /// <summary> </summary>
           public string Paddock { get; set; }

           /// <summary> </summary>
           public string Name { get; set; }
           /// <summary> </summary>
           public double Yield  { get; set; }
           /// <summary> </summary>
           public double Protein { get; set; }
           /// <summary> </summary>
           public double Area { get; set; }
        }
    
        [Link] private Summary Summary = null;
        
        /// <summary> Initial bank balance </summary>
        [Description("Initial Balance ($)")]
        public double InitialBalance {get ; set ;}
        
        /// <summary> Bank account</summary>
        public double Balance {get ; set ;}


        /// <summary> </summary>
        [EventSubscribe("StartOfSimulation")]
        public void OnStartOfSimulation(object sender, EventArgs e)
        {
            logItems = new List<Object[]>();
            Balance = InitialBalance;
        }

        /// <summary> </summary>
        [EventSubscribe("EndOfSimulation")]
        public void OnEndOfSimulation(object sender, EventArgs e)
        {
            if (logItems.Count > 0)
               writeLogs();
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
        
        /// <summary> Paddock (area based rate, eg kg/ha)</summary>
        [EventSubscribe("DoPaddockIncome")]
        public void DoPaddockIncome(object sender, PaddockIncomeArgs e)
        {
           var f = this.FindChild<CostPriceInfo>(e.Name);
           if (f == null)
               Summary.WriteMessage(this, $"DoPaddockIncome: {e.Name} ({e.Description}) has no cost/price information", MessageType.Warning);
           else
           {
               double amount = f.Price * e.Yield * e.Area;
            
               Summary.WriteMessage(this, $"{e.Description} Income = ${amount}", MessageType.Information); 
               Balance += amount;
               logIt(Clock.Today, e.Paddock, amount, 0, e.Category, e.Description); 
           }    
        }

        /// <summary> Whole farm ($) </summary>
        [EventSubscribe("DoFarmIncome")]
        public void DoFarmIncome(object sender, FarmIncomeArgs e)
        {
           Summary.WriteMessage(this, $"{e.Description} Income = ${e.Amount}", MessageType.Information); 
           Balance += e.Amount;
           logIt(Clock.Today, "", e.Amount, 0, e.Category, e.Description); 
        }

        /// <summary> Paddock (area based rate, eg kg/ha)</summary>
        [EventSubscribe("DoPaddockExpenditure")]
        public void DoPaddockExpenditure(object sender, PaddockExpenditureArgs e)
        {
           double cost = 0;
           var f = this.FindChild<CostPriceInfo>(e.Name);
           if (f != null) 
              cost = f.VariableCost;
           else 
           {   
              var f2 = this.FindChild<CostInfo>(e.Name);
              if (f2 != null) 
                 cost = f2.Cost;
              else 
                 Summary.WriteMessage(this, $"DoPaddockExpenditure: {e.Name} ({e.Description}) has no cost/price information", MessageType.Warning);
           }
           double amount = cost * e.Rate * e.Area;

           Summary.WriteMessage(this, $"{e.Description} Expenditure = ${amount}", MessageType.Information); 
           Balance -= amount;
           logIt(Clock.Today, e.Paddock, 0, amount, e.Category, e.Description); 
        }
        /// <summary> Whole farm ($) </summary>
        [EventSubscribe("DoFarmExpenditure")]
        public void DoFarmExpenditure(object sender, FarmExpenditureArgs e)
        {
           Summary.WriteMessage(this, $"{e.Description} Expenditure = ${e.Cost}", MessageType.Information); 
           Balance -= e.Cost;
           logIt(Clock.Today, "", 0, e.Cost, e.Category, e.Description); 
        }

        [Link]
        private IDataStore storage = null;

        [Link]
        private Simulation simulation = null;

        [Link]
        IClock Clock = null;

        private void logIt(DateTime t, string paddock, double income, double expenditure, string category, string desc) 
        {
            logItems.Add(new object[] {simulation.Name, t, paddock, income, expenditure, Balance, category, desc});
        }

        List<Object[]> logItems = new List<Object[]>();
        private void writeLogs()
        {
            string relativeModelPath = this.Name;

            DataTable items = new DataTable( relativeModelPath + "_Items");
            items.Columns.Add("SimulationName", typeof(string));
            items.Columns.Add("Date", typeof(DateTime));
            items.Columns.Add("Paddock", typeof(string));
            items.Columns.Add("Income", typeof(float));
            items.Columns.Add("Expenditure", typeof(float));
            items.Columns.Add("Balance", typeof(float));
            items.Columns.Add("Category", typeof(string));
            items.Columns.Add("Description", typeof(string));

            DataTable table = items.Clone();
            foreach (var item in logItems)
            {
                table.Rows.Add(item);
            }
            storage.Writer.WriteTable(table, deleteAllData: false);
        }
    }
}
