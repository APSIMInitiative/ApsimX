using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using System.Data;
using Models.Core;
using Models.Storage;
using Models.Functions;
using System.Collections;
using System.Xml.Serialization;

namespace Models.Management
{
    /// <summary>This model logs details of the rotation manager.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.RugPlotView")]
    [PresenterName("UserInterface.Presenters.RugPlotPresenter")]
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

        /// <summary>The current paddock under examination (eg [Manager].Script.currentPaddock) </summary>
        [Description("The name of the current paddock under investigation")]
        public string CurrentPaddockString {get; set;}

        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs args)
        {
             RVPs = new List<RVPair>();
             RVIndices  = new Dictionary<DateTime, int>();
        }

        /// <summary>
        /// log a rule evaluation
        /// </summary>
       public void DoRuleEvaluation(string rule, double value) 
       {
            if (!RVIndices.Keys.Contains(Clock.Today))
                 RVIndices.Add(Clock.Today, RVPs.Count);

            // Find which padddock is being managed right now
            var cp = simulation.Get(CurrentPaddockString);
            if (cp is IFunction function)
                cp = function.Value();
            string currentPaddock = cp.ToString();

            RVPs.Add(new RVPair{Date = Clock.Today, paddock = currentPaddock,
                                 rule = rule, value = value});

       }
       /// <summary>
       /// log a transition
       /// </summary>
       public void DoTransition(string state) 
       {
             //fixme - nothing for now..
       }

       [EventSubscribe("EndOfSimulation")]
        private void onEndSimulation(object sender, EventArgs args) {
              writeRVs();
              writeIndexes();
        }

        private string myLocalName()
        {
              return(this.Parent.Name);
        }
        private void writeRVs() {
             // Remove the path of the simulation within the .apsimx file. 
             string relativeModelPath = myLocalName();

             DataTable messages = new DataTable("_" + relativeModelPath + "_Values");
             messages.Columns.Add("SimulationName", typeof(string));
             messages.Columns.Add("ComponentName", typeof(string));
             messages.Columns.Add("Date", typeof(DateTime));
             messages.Columns.Add("Paddock", typeof(string));
             messages.Columns.Add("Rule", typeof(string));
             messages.Columns.Add("Value", typeof(double));


            DataTable table = messages.Clone();   // fixme - I dont understand why this is needed??
            foreach (var rv in RVPs) {
               DataRow row = table.NewRow();
               row[0] = simulation.Name;
               row[1] = relativeModelPath;
               row[2] = rv.Date;
               row[3] = rv.paddock;
               row[4] = rv.rule;
               row[5] = rv.value;
               table.Rows.Add(row);
            }
            storage?.Writer?.WriteTable(table, false);
        }

        private void writeIndexes () {
             string relativeModelPath = myLocalName();
             DataTable messages = new DataTable("_" + relativeModelPath + "_Indices");
             messages.Columns.Add("SimulationName", typeof(string));
             messages.Columns.Add("ComponentName", typeof(string));
             messages.Columns.Add("Date", typeof(DateTime));
             messages.Columns.Add("Index", typeof(int));

            DataTable table = messages.Clone();   // fixme - I dont understand why this is needed??
            foreach (var idx in RVIndices) {
               DataRow row = table.NewRow();
               row[0] = simulation.Name;
               row[1] = relativeModelPath;
               row[2] = idx.Key;
               row[3] = idx.Value;
               table.Rows.Add(row);
            }
            storage?.Writer?.WriteTable(table, false);
       }
        private List<RVPair> _RVPs = null;
        /// <summary>The list of rules and evaluations </summary>
       public List<RVPair> RVPs {
            get {
               if (_RVPs == null) loadIt(); 
               return(_RVPs);
            } 
            private set {_RVPs = value;}
       }

       private Dictionary<DateTime, int> _RVIndices = null;
        /// <summary>Indices (by date) into the big list of rules &amp; evaluations </summary>
        public Dictionary<DateTime, int> RVIndices {
            get {
               if (_RVIndices == null) loadIt(); 
               return(_RVIndices);
            } 
            private set {_RVIndices = value;}
       }

        private void loadIt() 
        {
             storage = this.FindInScope<IDataStore>();
             if (storage == null) {throw new Exception("No storage");}
             DataTable table = storage?.Reader?.GetData("_" + myLocalName() + "_Values" /*, 
                                                        simulationNames: new [] { simulation.Name} links aren't resolved by here */);
             if (table == null) {throw new Exception("No rule/value table in storage");}
             var Dates = table.AsEnumerable().Select(r => r.Field<DateTime>("Date")).ToArray();
             var Paddocks = table.AsEnumerable().Select(r => r.Field<string>("Paddock")).ToArray();
             var Rules = table.AsEnumerable().Select(r => r.Field<string>("Rule")).ToArray();
             var Values = table.AsEnumerable().Select(r => r.Field<double>("Value")).ToArray();
             _RVPs = new List<RVPair>();
             for(int i = 0; i < Dates.Length; i++) {
                _RVPs.Add(new RVPair{Date =Dates[i], paddock = Paddocks[i],
                                     rule = Rules[i], value = Values[i]});
             }

             table = storage?.Reader?.GetData("_" + myLocalName() + "_Indices" /*, 
                                                        simulationNames: new [] { simulation.Name} links aren't resolved by here */);
             if (table == null) {throw new Exception("No index table in storage");}
             Dates = table.AsEnumerable().Select(r => r.Field<DateTime>("Date")).ToArray();
             var Indices = table.AsEnumerable().Select(r => r.Field<int>("Index")).ToArray();
             _RVIndices = new Dictionary<DateTime, int>();
             for(int i = 0; i < Dates.Length; i++) {
                _RVIndices.Add(Dates[i], Indices[i]);
             }
        }
    }

    /// <summary>
    /// A rule:value pair
    /// </summary>
    public class RVPair {
       /// <summary>
       /// 
       /// </summary>
       public RVPair(/*Dictionary<string,int> paddocks, Dictionary<string,int> rules*/) {

       }
       /// <summary>
       /// 
       /// </summary>
       public DateTime Date;

       /// <summary>
       /// 
       /// </summary>
       public string paddock;

       /// <summary>
       /// 
       /// </summary>
       public string rule;

       /// <summary>
       /// 
       /// </summary>
       public double value;
    }
}