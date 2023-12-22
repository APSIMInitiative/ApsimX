using System;
using System.Collections.Generic;
using System.Linq;

using Models.Core;
using Models.Factorial;
using System.Data;
using Models.Storage;
using Models.Functions;

// TODO
// herbicides, weeds, machinery
// annual overheads & capital repayments
// table of annual paddock activity
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

        /// <summary> Initial bank balance </summary>
        [Description("Variable with name of current paddock")]
        public string currentPaddockName {get ; set ;}

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
        
        /// <summary> </summary>
        [EventSubscribe("DoIncome")]
        public void OnIncome(object sender, PaddockIncomeArgs e)
        {
           var f = this.FindChild<CostPriceInfo>(e.Name);
           double price = f?.Price ?? 0.0;
           double amount = price * e.Yield * e.Area;
            
           Summary.WriteMessage(this, $"{e.Description} Income = ${amount}", MessageType.Information); 
           Balance += amount;
           logIt(Clock.Today, simulation.Get(currentPaddockName).ToString(), amount, 0, e.Category, e.Description); 
        }
        /// <summary> </summary>
        [EventSubscribe("DoExpenditure")]
        public void DoExpenditure(object sender, PaddockExpenditureArgs e)
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
           }
           double amount = cost * e.Rate * e.Area;

           Balance -= amount;
           logIt(Clock.Today, simulation.Get(currentPaddockName).ToString(), 0, amount, e.Category, e.Description); 
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
            storage.Writer.WriteTable(table, deleteAllData: true);
        }
    }
}
