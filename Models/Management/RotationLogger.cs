using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using System.Data;
using Models.Core;
using Models.Storage;
using Models.Functions;

namespace Models.Management
{
    /// <summary>This model logs details of the rotation manager.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RotationManager))]
    public class rotationRugplot : Model
    {
        /// <summary>A link to a storage service</summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>A link to the simulation root</summary>
        [Link]
        private Simulation simulation = null;

        /// <summary>A link to a clock</summary>
        [Link] 
        IClock Clock = null;

         /// <summary>The current paddock under examination (eg [Manager].Script.currentPaddock </summary>
        [Description("Current Paddock under investigation")]
        public string CurrentPaddockString {get; set;}

        [NonSerialized]
        private DataTable messages, table;

        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs args)
        {
             messages = new DataTable("_RuleValues");
             messages.Columns.Add("SimulationName", typeof(string));
             messages.Columns.Add("ComponentName", typeof(string));
             messages.Columns.Add("Date", typeof(DateTime));
             messages.Columns.Add("Paddock", typeof(string));
             messages.Columns.Add("Rule", typeof(string));
             messages.Columns.Add("Value", typeof(double));
        }

/// <summary>
/// log a rule evaluation
/// </summary>
       public void DoRuleEvaluation(string rule, double value) 
       {
            // Remove the path of the simulation within the .apsimx file.
            string relativeModelPath = this.Parent.FullPath.Replace($"{simulation.FullPath}.", string.Empty);

            // Find which padddock is being managed right now
            var cp = simulation.Get(CurrentPaddockString);
            if (cp is IFunction function)
                cp = function.Value();
            string currentPaddock = cp.ToString();

            table = messages.Clone();
            DataRow row = table.NewRow();
            row[0] = simulation.Name;
            row[1] = relativeModelPath;
            row[2] = Clock.Today;
            row[3] = currentPaddock;
            row[4] = rule;
            row[5] = (double) value;
            table.Rows.Add(row);

            // The messages table will be automatically cleaned prior to a simulation
            // run, so we don't need to delete existing data in this call to WriteTable().
            storage?.Writer?.WriteTable(table, false);
       }
/// <summary>
/// log a transition
/// </summary>
       public void DoTransition(string state) 
       {

       }
    }
}