//
// TO DO
//
// implement node colours & descriptions, separate name from id
// dynamic / auto layout of new nodes & arcs
// implement command history
// nodes & arcs referenced by ID, not names
// ?intellisense isn't picking up member functions? events are OK.
// Syntax checking of rules / actions.
// "fixme" where noted in code

using Models.Interfaces;
namespace Models.Management
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Globalization;
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// The rotation manager model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.BubbleChartView")]
    [PresenterName("UserInterface.Presenters.BubbleChartPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class RotationManager : Model, IBubbleChart
    {
        /// <summary>For logging</summary>
        [Link] public Summary Summary;

        [NonSerialized] [Link]
        private Events eventService = null;

        /// <summary>
        /// The nodes of the graph.
        /// </summary>
        [Description("Node list")]
        public List<StateNode> Nodes { get; set; } = new List<StateNode>();

        /// <summary>
        /// The arcs on the bubble chart.
        /// </summary>
        [Description("Arc list")]
        public List<RuleAction> Arcs { get; set; } = new List<RuleAction>();

        /// <summary>
        /// Initial state of the graph.
        /// </summary>
        [Description("Initial state of graph")]
        public string InitialState { get; set; }

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
                myNode.CopyFrom(node);
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
            var myArc = Arcs.Find(a => a.Name == value.Name);
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
            eventService.Publish("transition", null);
            Summary.WriteMessage(this, "Initialised, state=" + currentState + "(of " + Nodes.Count + " total)");
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            bool more = true;
            while (more)
            {
                more = false;
                double bestScore = -1.0;
                RuleAction bestArc = null;
                //Console.WriteLine("process 0: state=" + currentState);
                foreach (var arc in Arcs.FindAll(arc => arc.SourceName == currentState))
                {
                    double score = 1;
                    foreach (string testCondition in arc.testCondition)
                    {
                        var v = FindByPath(testCondition);
                        if (v == null) throw new Exception("Test condition \"" + testCondition + "\" returned nothing");
                        //Console.WriteLine("process 1: test=" + testCondition + " value=" + v);
                        double c = System.Convert.ToDouble(v, CultureInfo.InvariantCulture);
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

                        if ((thisAction = thisAction.Trim()) == string.Empty)
                            continue;

                        //Console.WriteLine( ">>process 2: action = '" + thisAction + "'");
                        if (!thisAction.Contains("("))
                        {
                            // Publish as an event
                            eventService.Publish(thisAction, null /*new object[] { null, new EventArgs() }*/);
                        }
                        else
                        {
                            // Call method directly - copied from operations module
                            string argumentsString = StringUtilities.SplitOffBracketedValue(ref thisAction, '(', ')');
                            string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            int posPeriod = thisAction.LastIndexOf('.');
                            if (posPeriod == -1)
                                throw new ApsimXException(this, "No module given for method call: \"" + thisAction + "\"");
                            string modelName = thisAction.Substring(0, posPeriod);
                            string methodName = thisAction.Substring(posPeriod + 1).Replace(";", "").Trim();

                            IModel model = FindByPath(modelName)?.Value as IModel;
                            if (model == null)
                                throw new ApsimXException(this, $"Cannot find model: {modelName}");

                            MethodInfo[] methods = model.GetType().GetMethods();
                            if (methods == null)
                                throw new ApsimXException(this, "Cannot find any methods in model: " + modelName);

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
                        }
                    }
                    eventService.Publish("transition_to_" + currentState, null);
                    more = true;
                }
            }
        }
        private object[] GetArgumentsForMethod(string[] arguments, MethodInfo method)
        {
            // convert arguments to an object array.
            ParameterInfo[] parameters = method.GetParameters();
            object[] parameterValues = new object[parameters.Length];
            if (arguments.Length > parameters.Length)
                return null;

            //retrieve the values for the named arguments that were provided. (not all the named arguments for the method may have been provided)
            for (int i = 0; i < arguments.Length; i++)
            {
                string value = arguments[i];
                int argumentIndex;
                int posColon = arguments[i].IndexOf(':');
                if (posColon == -1)
                    argumentIndex = i;
                else
                {
                    string argumentName = arguments[i].Substring(0, posColon).Trim();
                    // find parameter with this name.
                    for (argumentIndex = 0; argumentIndex < parameters.Length; argumentIndex++)
                    {
                        if (parameters[argumentIndex].Name == argumentName)
                            break;
                    }
                    if (argumentIndex == parameters.Length)
                        return null;
                    value = value.Substring(posColon + 1);
                }

                if (argumentIndex >= parameterValues.Length)
                    return null;

                // convert value to correct type.
                if (parameters[argumentIndex].ParameterType == typeof(double))
                    parameterValues[argumentIndex] = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                else if (parameters[argumentIndex].ParameterType == typeof(float))
                    parameterValues[argumentIndex] = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                else if (parameters[argumentIndex].ParameterType == typeof(int))
                    parameterValues[argumentIndex] = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                else if (parameters[argumentIndex].ParameterType == typeof(bool))
                    parameterValues[argumentIndex] = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                else if (parameters[argumentIndex].ParameterType == typeof(string))
                    parameterValues[argumentIndex] = value.Replace("\"", "").Trim();
                else if (parameters[argumentIndex].ParameterType.IsEnum)
                {
                    value = value.Trim();
                    int posLastPeriod = value.LastIndexOf('.');
                    if (posLastPeriod != -1)
                        value = value.Substring(posLastPeriod + 1);
                    parameterValues[argumentIndex] = Enum.Parse(parameters[argumentIndex].ParameterType, value);
                }
            }

            //if there were missing named arguments in the method call then use the default values for them.
            for (int i = 0; i < parameterValues.Length; i++)
            {
                if (parameterValues[i] == null)
                {
                    parameterValues[i] = parameters[i].DefaultValue;
                }
            }

            return parameterValues;
        }
    }

    /// <summary>Rules and actions required for a transition</summary>
    [Serializable]
    public class RuleAction : Arc
    {
        /// <summary>
        /// Contructor
        /// </summary>
        public RuleAction(Arc a) : base(a) { testCondition = new List<string>(); action = new List<string>(); }
        /// <summary>Test conditions that need to be satisfied for this transition</summary>
        public List<string> testCondition { get; set; }
        /// <summary>Actions undertaken when making this transition</summary>
        public List<string> action { get; set; }
        /// <param name="other"></param>
        public void copyFrom(RuleAction other)
        {
            base.CopyFrom(other);
            this.testCondition = new List<string>(other.testCondition);
            this.action = new List<string>(other.action);
        }
    }

    /// <summary>A state in the directed graph.</summary>
    [Serializable]
    public class StateNode : Node
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="n">Node from which properties will be copied.</param>
        /// <param name="description">Description of the node.</param>
        public StateNode(Node n, string description = null) : base(n) => Description = description;

        /// <summary>
        /// Description of the node.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Copy all properties of another node into this one.
        /// </summary>
        /// <param name="other">A node whose properties will be copied.</param>
        public void CopyFrom(StateNode other)
        {
            Description = other.Description;
            base.CopyFrom(other);
        }
    }
}
