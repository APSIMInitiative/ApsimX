//
// TO DO
//
// implement node colours & descriptions, separate name from id
// dynamic / auto layout of new nodes & arcs
// implement command history
// nodes & arcs referenced by ID, not names
// intellisense to display simulation events that we can publish / subscribe to
// strip blank lines , newlines from rules / actions. Syntax checking?
// "fixme" where noted in code

namespace Models
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Graph;

    /// <summary>
    /// The rotation manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.RotBubbleChartView")]
    [PresenterName("UserInterface.Presenters.RotBubbleChartPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class RotBubbleChart : Model
    {
        /// <summary>Constructor</summary>
        public RotBubbleChart ()
        {
            Nodes = new List<StateNode>();
            Arcs = new List<RuleAction>();
        }
        /// <summary>Rules and actions required for a transition</summary>
        [Serializable]
        public class RuleAction : Arc
        {
            /// <summary>
            /// Contructor
            /// </summary>
            public RuleAction(Arc a) : base(a) {  testCondition = new List<string>(); action = new List<string>(); }
            /// <summary>Test conditions that need to be satisfied for this transition</summary>
            public List<string> testCondition { get; set; }
            /// <summary>Actions undertaken when making this transition</summary>
            public List<string> action { get; set; }
            /// <param name="other"></param>
            public void copyFrom(RuleAction other) {
                base.CopyFrom(other);
                this.testCondition = new List<string>(other.testCondition);
                this.action = new List<string>(other.action);
            }
        }
        /// <summary>A state in the DG</summary>
        [Serializable]
        public class StateNode : Node
        {
            /// <summary>
            /// Constructor
            /// </summary>
            public StateNode(Node n) : base (n) { }
            /// <summary>
            /// The name the user calls this node. "Name" is a unique id. Should be unique
            /// </summary>
            /// <returns></returns>
            public string NodeName { get; set; }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="other"></param>
            public void copyFrom(StateNode other) { this.NodeName = other.NodeName; base.CopyFrom(other); }
        }

        /// <summary>
        /// 
        /// </summary>
        [Description("Node list")]
        public List<StateNode> Nodes { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Description("Arc list")]
        public List<RuleAction> Arcs { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("Initial state of graph")]
        public string InitialState;

        /// <summary>
        /// Add a node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public void AddNode(StateNode node)
        {
            var myNode = Nodes.Find(delegate (StateNode n) { return (n.Name == node.Name); });
            if (myNode == null)
                Nodes.Add(node);
            else
                myNode.copyFrom( node );
        }

        /// <summary>
        /// Remove a node
        /// </summary>
        /// <param name="nodeName"></param>
        public void DelNode(string nodeName)
        {
            Nodes.RemoveAll(delegate (StateNode n) { return (n.Name == nodeName); });
        }
        /// <summary>
        /// add a transition between two nodes
        /// </summary>
        public void AddRuleAction(RuleAction value)
        {
            if (Nodes.Find(delegate (StateNode n) { return (n.Name == value.SourceName); }) == null ||
                Nodes.Find(delegate (StateNode n) { return (n.Name == value.DestinationName); }) == null)
                throw new Exception("Target empty in arc");
            var myArc = Arcs.Find(delegate (RuleAction a) { return (a.Name == value.Name); });
            if (myArc == null)
                Arcs.Add(value);
            else
                myArc.CopyFrom(value);
        }
        /// <summary>
        /// delete an arc
        /// </summary>
        /// <param name="arcToDelete"></param>
        public void DelRuleAction(string arcToDelete)
        {
            Arcs.RemoveAll(delegate (RuleAction a) { return (a.Name == arcToDelete); });
        }

        /// <summary>Encapsulates the nodes &amp; arcs of a directed graph</summary>
        public class RBGraph 
        {
            /// <summary>A collection of nodes</summary>
            public List<StateNode> Nodes = new List<StateNode>();

            /// <summary>A collection of arcs</summary>
            public List<RuleAction> Arcs = new List<RuleAction>();
        }

        /// <summary>Get/set directed graph from model </summary>
        public RBGraph getGraph()
        {
                RBGraph g = new RBGraph();
                Nodes.ForEach(n => { g.Nodes.Add(n); /* Console.WriteLine("model get " + n.Name + "=" + n.NodeName); */});
                Arcs.ForEach(a => g.Arcs.Add(a));
                return (g);
        }
        /// <summary>Get/set directed graph from model </summary>
        public void setGraph(RBGraph value) 
            {
                Nodes.Clear(); Arcs.Clear();
                value.Nodes.ForEach(n => { Nodes.Add(n); /* Console.WriteLine("model set " + n.Name + "=" + n.NodeName); */});
                value.Arcs.ForEach(a => Arcs.Add(a));
            }

        // Simulation runtime from here on
        /// <summary>
        /// The simulation object used to get/publish with 
        /// </summary>
        [Link] public Simulation MySimulation;
        /// <summary>
        /// For logging
        /// </summary>
        [Link] public Summary Summary;

        /// <summary>
        /// Current State of DG
        /// </summary>
        [Units("")]
        [Description("Current State of DG")]
        public string currentState { get; private set; }

        [EventSubscribe("Commencing")]
        private void OnCommence(object sender, EventArgs e)
        {
            currentState = InitialState;
            eventService = (Events) MySimulation.GetEventService(this);
            eventService.Publish("transition", null);
            Summary.WriteMessage(this, "Initialised, state=" + currentState + "(of "+ Nodes.Count + " total)");
        }

        [NonSerialized]
        private Events eventService = null;

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            bool more = true;
            while (more)
            {
                more = false;
                double bestScore = -1.0;
                RuleAction bestArc = null;
                //Summary.WriteMessage(this, "process 0: state=" + currentState);
                foreach (var arc in Arcs.FindAll(arc => arc.SourceName == currentState)) 
                {
                    double score = 1;
                    foreach (string testCondition in arc.testCondition)
                    {
                        var v = MySimulation.Get(testCondition);
                        if (v == null) throw new Exception("Test condition \"" + testCondition + "\" returned nothing");
                        //Summary.WriteMessage(this, "process 1: test=" + testCondition + " value=" + v);
                        double c = System.Convert.ToDouble(v);
                        score *= c;
                    }
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestArc = arc;
                    }
                }
                if (bestScore > 0.0)
                {
                    if (currentState != "")
                    {
                        eventService.Publish("transition_from_" + currentState, null);
                        eventService.Publish("transition", null);
                        currentState = bestArc.DestinationName;
                    }
                    foreach (string action in bestArc.action)
                    {
                        string thisAction = action;
                        int commentPosition = thisAction.IndexOf("//");
                        if (commentPosition >= 0)
                            thisAction = thisAction.Substring(0, commentPosition);

                        Summary.WriteMessage(this, ">>process 2: action = '" + thisAction + "'");
                        if (thisAction.Trim() != string.Empty)
                        {
#if true
                        eventService.Publish(thisAction, null /*new object[] { null, new EventArgs() }*/);
#else
                            // Methodology from operations module
                            string argumentsString = StringUtilities.SplitOffBracketedValue(ref thisAction, '(', ')');
                        string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                        int posPeriod = thisAction.LastIndexOf('.');
                        if (posPeriod == -1)
                            throw new ApsimXException(this, "Bad operations action found: " + operation.Action);
                        string modelName = thisAction.Substring(0, posPeriod);
                        string methodName = thisAction.Substring(posPeriod + 1).Replace(";", "").Trim();

                        Model model = Apsim.Get(this, modelName) as Model;
                        if (model == null)
                            throw new ApsimXException(this, "Cannot find model: " + modelName);


                        MethodInfo[] methods = model.GetType().GetMethods();
                        if (methods == null)
                            throw new ApsimXException(this, "Cannot find method: " + methodName + " in model: " + modelName);

                        object[] parameterValues = null;
                        foreach (MethodInfo method in methods)
                        {
                            if (method.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase))
                            {
                                parameterValues = GetArgumentsForMethod(arguments, method);

                                // invoke method.
                                if (parameterValues != null)
                                {
                                    try
                                    {
                                        method.Invoke(model, parameterValues);
                                    }
                                    catch (Exception err)
                                    {
                                        throw err.InnerException;
                                    }
                                    break;
                                }
                            }
                        }

                        if (parameterValues == null)
                            throw new ApsimXException(this, "Cannot find method: " + methodName + " in model: " + modelName);
#endif
                        }
                    }
                    eventService.Publish("transition_to_" + currentState, null);
                    more = true;
                }
            }
        }
    }
}
