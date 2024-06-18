using System;
using System.Collections.Generic;
using System.Linq;

using System.Data;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;
using Models.Functions;
using System.Collections;
using System.Drawing;

namespace Models.Management
{
   /// <summary>This model logs details of the rotation manager.</summary>
   [Serializable]
   [ViewName("UserInterface.Views.RugPlotView")]
   [PresenterName("UserInterface.Presenters.RugPlotPresenter")]
   [ValidParent(ParentType = typeof(RotationManager))]
   public class RotationRugplot : Model
   {
      /// <summary>
      /// Constructor
      /// </summary>
      public RotationRugplot() { }

      /// <summary>A link to a storage service</summary>
      [Link]
      private IDataStore storage = null;

      /// <summary>A link to the simulation root</summary>
      [Link]
      private Simulation simulation = null;

      /// <summary>A link to a clock</summary>
      [Link]
      IClock Clock = null;

      /// <summary>The current paddock under examination (eg [Manager].Script.currentPaddock) FIXME needs UI element </summary>
      [Description("The name of the current paddock under investigation")]
      public string CurrentPaddockString { get; set; }

      [EventSubscribe("Commencing")]
      private void OnSimulationCommencing(object sender, EventArgs e)
      {
         _RVPs = new List<RVPair>();
         _RVIndices = new Dictionary<DateTime, int>();
         _Transitions = new List<Transition>();
         _States = new List<string>();
         ruleHashes = new Dictionary<string, int>();
         targetHashes = new Dictionary<string, int>();
         SimulationName = simulation.Name;
      }

      /// <summary>
      /// log a rule evaluation
      /// </summary>
      public void DoRuleEvaluation(string target, string rule, double value)
      {
         if (!RVIndices.Keys.Contains(Clock.Today))
            RVIndices.Add(Clock.Today, RVPs.Count);

         // Find which padddock is being managed right now
         var cp = simulation.Get(CurrentPaddockString);
         if (cp is IFunction function)
            cp = function.Value();
         string currentPaddock = cp?.ToString();

         if (!targetHashes.ContainsKey(target))
            targetHashes.Add(target, targetHashes.Keys.Count);

         if (!ruleHashes.ContainsKey(rule))
            ruleHashes.Add(rule, ruleHashes.Keys.Count);

         RVPs.Add(new RVPair
         {
            Date = Clock.Today,
            paddock = currentPaddock,
            target = targetHashes[target],
            rule = ruleHashes[rule],
            value = value
         });

      }

      /// <summary>
      /// log a transition
      /// </summary>
      public void DoTransition(string state)
      {
         // Find which padddock is being managed right now
         var cp = simulation.Get(CurrentPaddockString);
         if (cp is IFunction function)
            cp = function.Value();
         string currentPaddock = cp?.ToString();
         _Transitions.Add(new Transition { Date = Clock.Today, paddock = currentPaddock, state = state });
      }

      [EventSubscribe("EndOfSimulation")]
      private void onEndSimulation(object sender, EventArgs args)
      {
         writeRVs();
         writeIndexes();
         writeTransitions();
         writeRuleHashes();
         writeTargetHashes();
      }

      private string myLocalName()
      {
         return (this.Parent.Name);
      }
      private void writeRVs()
      {
         string relativeModelPath = myLocalName();

         DataTable messages = new DataTable("_" + simulation.Name + "_" + relativeModelPath + "_Values");
         messages.Columns.Add("Date", typeof(DateTime));
         messages.Columns.Add("Paddock", typeof(string));
         messages.Columns.Add("Target", typeof(int));
         messages.Columns.Add("Rule", typeof(int));
         messages.Columns.Add("Value", typeof(double));


         DataTable table = messages.Clone();
         foreach (var rv in RVPs)
         {
            DataRow row = table.NewRow();
            row[0] = rv.Date;
            row[1] = rv.paddock;
            row[2] = rv.target;
            row[3] = rv.rule;
            row[4] = rv.value;
            table.Rows.Add(row);
         }
         storage?.Writer?.WriteTable(table, deleteAllData : true);
      }

      private void writeIndexes()
      {
         string relativeModelPath = myLocalName();
         DataTable messages = new DataTable("_" + simulation.Name + "_" + relativeModelPath + "_Indices");
         messages.Columns.Add("Date", typeof(DateTime));
         messages.Columns.Add("Index", typeof(int));

         DataTable table = messages.Clone();   
         foreach (var idx in RVIndices)
         {
            DataRow row = table.NewRow();
            row[0] = idx.Key;
            row[1] = idx.Value;
            table.Rows.Add(row);
         }
         storage?.Writer?.WriteTable(table, deleteAllData : true);
      }
      private void writeRuleHashes()
      {
         string relativeModelPath = myLocalName();
         DataTable messages = new DataTable("_" + simulation.Name + "_" + relativeModelPath + "_ruleHashes");
         messages.Columns.Add("Key", typeof(string));
         messages.Columns.Add("Value", typeof(int));

         DataTable table = messages.Clone();   // fixme - I dont understand why this is needed??
         foreach (var idx in ruleHashes)
         {
            DataRow row = table.NewRow();
            row[0] = idx.Key;
            row[1] = idx.Value;
            table.Rows.Add(row);
         }
         storage?.Writer?.WriteTable(table, deleteAllData : true);
      }

      private void writeTargetHashes()
      {
         string relativeModelPath = myLocalName();
         DataTable messages = new DataTable("_" + simulation.Name + "_" + relativeModelPath + "_targetHashes");
         messages.Columns.Add("Key", typeof(string));
         messages.Columns.Add("Value", typeof(int));

         DataTable table = messages.Clone();   // fixme - I dont understand why this is needed??
         foreach (var idx in targetHashes)
         {
            DataRow row = table.NewRow();
            row[0] = idx.Key;
            row[1] = idx.Value;
            table.Rows.Add(row);
         }
         storage?.Writer?.WriteTable(table, deleteAllData : true);
      }

      private void writeTransitions()
      {
         string relativeModelPath = myLocalName();
         DataTable messages = new DataTable("_" + simulation.Name + "_" + relativeModelPath + "_Transitions");
         messages.Columns.Add("Date", typeof(DateTime));
         messages.Columns.Add("Paddock", typeof(string));
         messages.Columns.Add("State", typeof(string));

         DataTable table = messages.Clone();   // fixme - I dont understand why this is needed??
         foreach (var t in Transitions)
         {
            DataRow row = table.NewRow();
            row[0] = t.Date;
            row[1] = t.paddock;
            row[2] = t.state;
            table.Rows.Add(row);
         }
         storage?.Writer?.WriteTable(table, deleteAllData : true);
      }

      /// <summary> convert strings/ints </summary>
      [NonSerialized]
      public Dictionary<string, int> ruleHashes = null;
      /// <summary>The list of rules and evaluations </summary>
      [NonSerialized]
      public Dictionary<string, int> targetHashes = null;

      [NonSerialized]
      private List<RVPair> _RVPs = null;
      /// <summary>The list of rules and evaluations </summary>
      public List<RVPair> RVPs
      {
         get
         {
            if (_RVPs == null) loadIt();
            return (_RVPs);
         }
         private set { _RVPs = value; }
      }

      [NonSerialized]
      private Dictionary<DateTime, int> _RVIndices = null;
      /// <summary>Indices (by date) into the big list of rules &amp; evaluations </summary>
      public Dictionary<DateTime, int> RVIndices
      {
         get
         {
            if (_RVIndices == null) loadIt();
            return (_RVIndices);
         }
         private set { _RVIndices = value; }
      }
      [NonSerialized]
      private List<Transition> _Transitions = null;

      /// <summary> The zones we know about</summary>
      public List<Transition> Transitions
      {
         get
         {
            if (_Transitions == null) loadIt();
            return (_Transitions);
         }
         private set { _Transitions = value; }
      }

      private List<string> _States = null;
      /// <summary> The zones we know about</summary>
      public List<string> States
      {
         get
         {
            if (_States == null) loadIt();
            return (_States);
         }
         private set { _States = value; }
      }

      /// <summary>
      /// The simulation name to load
      /// </summary>
      public string SimulationName = "";
/// <summary>
/// The view is asking for a different set of data
/// </summary>
/// <param name="s"></param>
      public void SetSimulationName(string s) {
          SimulationName = s;
          loadIt();
      }
      private void loadIt()
      {
         storage = this.FindInScope<IDataStore>();
         if (storage == null) { throw new Exception("No storage"); }

         if (! GetSimulationNames().Contains(SimulationName) )
             SimulationName = GetSimulationNames()[0];

         _RVPs = new List<RVPair>();
         DataTable table = storage.Reader.GetData("_" + SimulationName +  "_" + myLocalName() + "_Values");
         if (table == null) { throw new Exception("No rule/value table in storage"); }
         if (table?.Rows?.Count > 0)
         {
            var Dates = table.AsEnumerable().Select(r => r.Field<DateTime>("Date")).ToArray();
            var AllPaddocks = table.AsEnumerable().Select(r => r.Field<string>("Paddock")).ToArray();
            var AllTargets = table.AsEnumerable().Select(r => r.Field<int>("Target")).ToArray();
            var Rules = table.AsEnumerable().Select(r => r.Field<int>("Rule")).ToArray();
            var Values = table.AsEnumerable().Select(r => r.Field<double>("Value")).ToArray();
            for (int i = 0; i < Dates.Length; i++)
            {
               _RVPs.Add(new RVPair
               {
                  Date = Dates[i],
                  paddock = AllPaddocks[i],
                  target = AllTargets[i],
                  rule = Rules[i],
                  value = Values[i]
               });
            }
         }

         _RVIndices = new Dictionary<DateTime, int>();
         table = storage.Reader.GetData("_" + SimulationName +  "_" + myLocalName() + "_Indices");
         if (table == null) { throw new Exception("No index table in storage"); }
         if (table?.Rows?.Count > 0)
         {
            var Dates = table.AsEnumerable().Select(r => r.Field<DateTime>("Date")).ToArray();
            var Indices = table.AsEnumerable().Select(r => r.Field<int>("Index")).ToArray();
            for (int i = 0; i < Dates.Length; i++)
            {
               if (!_RVIndices.ContainsKey(Dates[i]))
                  _RVIndices.Add(Dates[i], Indices[i]);
            }
         }

         _Transitions = new List<Transition>();
         table = storage.Reader.GetData("_" + SimulationName +  "_" + myLocalName() + "_Transitions");
         if (table?.Rows?.Count > 0)
         {
            var Dates = table.AsEnumerable().Select(r => r.Field<DateTime>("Date")).ToArray();
            var AllPaddocks = table.AsEnumerable().Select(r => r.Field<string>("Paddock")).ToArray();
            var AllStates = table.AsEnumerable().Select(r => r.Field<string>("State")).ToArray();
            for (int i = 0; i < Dates.Length; i++)
            {
               _Transitions.Add(new Transition { Date = Dates[i], paddock = AllPaddocks[i], state = AllStates[i] });
            }
            _States = AllStates.Distinct().ToList();
         }

         ruleHashes = new Dictionary<string, int>();
         table = storage.Reader.GetData("_" + SimulationName +  "_" + myLocalName() + "_ruleHashes");
         if (table.Rows.Count > 0)
         {
            var keys = table.AsEnumerable().Select(r => r.Field<string>("Key")).ToArray();
            var values = table.AsEnumerable().Select(r => r.Field<int>("Value")).ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
               ruleHashes.Add(keys[i], values[i]);
            }
         }
         targetHashes = new Dictionary<string, int>();
         table = storage.Reader.GetData("_" + SimulationName +  "_" + myLocalName() + "_targetHashes");
         if (table.Rows.Count > 0)
         {
            var keys = table.AsEnumerable().Select(r => r.Field<string>("Key")).ToArray();
            var values = table.AsEnumerable().Select(r => r.Field<int>("Value")).ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
               targetHashes.Add(keys[i], values[i]);
            }
         }
      }
      /// <summary>
      /// Get the simulation names 
      /// </summary>
      /// <returns></returns>
      public string[] GetSimulationNames()
        {
            // populate the simulation names in the view.
            ScopingRules scope = new();
            IModel scopedParent = scope.FindScopedParentModel(this);

            if (scopedParent is Simulation parentSimulation)
            {
                if (scopedParent.Parent is Experiment)
                    scopedParent = scopedParent.Parent;
                else
                {
                    return new string[] { parentSimulation.Name };
                }
            }

            if (scopedParent is Experiment experiment)
            {
                return(experiment.GenerateSimulationDescriptions().Select(s => s.Name).ToArray());
            }
            else
            {
                List<ISimulationDescriptionGenerator> simulations = this.FindAllInScope<ISimulationDescriptionGenerator>().Cast<ISimulationDescriptionGenerator>().ToList();
                simulations.RemoveAll(s => s is Simulation && (s as IModel).Parent is Experiment);
                List<string> simulationNames = simulations.SelectMany(m => m.GenerateSimulationDescriptions()).Select(m => m.Name).ToList();
                simulationNames.AddRange(this.FindAllInScope<Models.Optimisation.CroptimizR>().Select(x => x.Name));
                return(simulationNames.ToArray());
            }
        }
   }
   /// <summary>
   /// A rule:value pair
   /// </summary>
   public class Transition
   {
      /// <summary>
      /// 
      /// </summary>
      public Transition() { }
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
      public string state;
   }
   /// <summary>
   /// A rule:value pair
   /// </summary>
   public class RVPair
   {
      /// <summary>
      /// 
      /// </summary>
      public RVPair() { }
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
      public int target;

      /// <summary>
      /// 
      /// </summary>
      public int rule;

      /// <summary>
      /// 
      /// </summary>
      public double value;
   }
   /// <summary>
   /// 
   /// </summary>
   public class hashTable
   {
      private Dictionary<string, int> dict = new Dictionary<string, int>();
      private int[] values = null;
      /// <summary>
      /// 
      /// </summary>
      public hashTable(string[] _values)
      {
         values = new int[_values.Length];
         for (var i = 0; i < _values.Length; i++)
         {
            if (!dict.ContainsKey(_values[i]))
               dict.Add(_values[i], dict.Keys.Count);
            values[i] = dict[_values[i]];
         }
      }
      /// <summary>
      /// 
      /// </summary>
      public int[] Hashes()
      {
         return (values);
      }

      /// <summary>
      /// 
      /// </summary>
      public Dictionary<int, string> Keys()
      {
         Dictionary<int, string> result = new Dictionary<int, string>();
         foreach (var k in dict)
            result.Add(k.Value, k.Key);
         return (result);
      }

   }
}
