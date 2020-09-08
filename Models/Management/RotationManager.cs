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

        /// <summary>
        /// Events service. Used to publish events when transitioning
        /// between stages/nodes.
        /// </summary>
        [NonSerialized]
        [Link]
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

        /// <summary>
        /// Called when a simulation commences. Performs one-time initialisation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("Commencing")]
        private void OnCommence(object sender, EventArgs e)
        {
            currentState = InitialState;
            eventService.Publish("transition", null);
            Summary.WriteMessage(this, "Initialised, state=" + currentState + "(of " + Nodes.Count + " total)");
        }

        /// <summary>
        /// Called once per day during the simulation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            bool more = true;
            while (more)
            {
                more = false;
                double bestScore = -1.0;
                RuleAction bestArc = null;
                foreach (var arc in Arcs.FindAll(arc => arc.SourceName == currentState))
                {
                    double score = 1;
                    foreach (string testCondition in arc.Conditions)
                    {
                        object value = FindByPath(testCondition)?.Value;
                        if (value == null)
                            throw new Exception($"Test condition '{testCondition}' returned nothing");
                        score *= Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    }
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestArc = arc;
                    }
                }
                if (bestScore > 0.0)
                {
                    TransitionTo(bestArc);
                    more = true;
                }
            }
        }

        /// <summary>
        /// Transition along an arc to another stage/node.
        /// </summary>
        /// <param name="transition">The arc to be followed.</param>
        private void TransitionTo(RuleAction transition)
        {
            try
            {
                currentState = transition.DestinationName;

                // We can now move to another stage.
                if (currentState != "")
                {
                    // Publish pre-transition events.
                    eventService.Publish("transition_from_" + currentState, null);
                    eventService.Publish("transition", null);
                }

                foreach (string action in transition.Actions)
                {
                    string thisAction = action;

                    // Treat '//' as a single-line comment - ignore everything after it.
                    int commentPosition = thisAction.IndexOf("//");
                    if (commentPosition >= 0)
                        thisAction = thisAction.Substring(0, commentPosition);

                    if (string.IsNullOrEmpty(thisAction))
                        continue;

                    // If the action doesn't contain an opening parenthesis,
                    // treat it as an event name and publish the event.
                    if (!thisAction.Contains("("))
                        eventService.Publish(thisAction, null /*new object[] { null, new EventArgs() }*/);
                    else
                        CallMethod(thisAction);
                }
                eventService.Publish("transition_to_" + currentState, null);
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to transition to state {currentState}", err);
            }
        }

        /// <summary>
        /// Call a method specified by the user. Method must be public.
        /// </summary>
        /// <param name="invocation">
        /// Method specification from user. e.g.
        /// [Wheat].Harvest()
        /// [Manager].SomeMethod("Blargle", -1)
        /// </param>
        /// <remarks>
        /// May need work, was largely copied from operations code.
        /// It's pretty crude and will probably fail for nested anything
        /// even remotely complicated (e.g. nested method calls).
        /// </remarks>
        private void CallMethod(string invocation)
        {
            // Need to separate method name from arguments.
            string argumentsString = StringUtilities.SplitOffBracketedValue(ref invocation, '(', ')');
            string[] arguments = argumentsString.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int posPeriod = invocation.LastIndexOf('.');
            if (posPeriod == -1)
                throw new ApsimXException(this, $"No module given for method call: '{invocation}'");

            string modelName = invocation.Substring(0, posPeriod);
            string methodName = invocation.Substring(posPeriod + 1).Replace(";", "").Trim();

            // Find the model to which the method belongs.
            IModel model = FindByPath(modelName)?.Value as IModel;
            if (model == null)
                throw new ApsimXException(this, $"Cannot find model: {modelName}");

            // Ensure the model contains a public method with the correct name.
            MethodInfo method = model.GetType().GetMethod(methodName);
            if (method == null)
                throw new ApsimXException(this, $"Cannot find method in model: {modelName}");

            // Parse arguments provided by user.
            object[] parameterValues = GetArgumentsForMethod(arguments, method);
            
            // Call the method.
            method.Invoke(model, parameterValues);
        }

        /// <summary>
        /// Parse user-inputted arguments to be provided to a method call.
        /// </summary>
        /// <param name="arguments">Arguments inputted by the user.</param>
        /// <param name="method">Method which is to be called.</param>
        private object[] GetArgumentsForMethod(string[] arguments, MethodInfo method)
        {
            ParameterInfo[] expectedParameters = method.GetParameters();
            if (expectedParameters.Length != arguments.Length)
                throw new Exception($"Unable to call method {method.Name}: expected {expectedParameters.Length} arguments but {arguments?.Length ?? 0} were given");

            // Convert arguments to an object array.
            object[] parameterValues = new object[expectedParameters.Length];
            if (arguments.Length > expectedParameters.Length)
                return null;

            // Retrieve the values for the named arguments that were provided.
            // Not all the named arguments for the method may have been provided.
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

                    // Find parameter with this name.
                    for (argumentIndex = 0; argumentIndex < expectedParameters.Length; argumentIndex++)
                    {
                        if (expectedParameters[argumentIndex].Name == argumentName)
                            break;
                    }
                    if (argumentIndex == expectedParameters.Length)
                        return null;
                    value = value.Substring(posColon + 1);
                }

                if (argumentIndex >= parameterValues.Length)
                    return null;

                // Convert value to correct type.
                parameterValues[argumentIndex] = ReflectionUtilities.StringToObject(expectedParameters[argumentIndex].ParameterType, value);
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
        public RuleAction(Arc a) : base(a) { Conditions = new List<string>(); Actions = new List<string>(); }

        /// <summary>Test conditions that need to be satisfied for this transition</summary>
        public List<string> Conditions { get; set; }

        /// <summary>Actions undertaken when making this transition</summary>
        public List<string> Actions { get; set; }

        /// <param name="other"></param>
        public void copyFrom(RuleAction other)
        {
            base.CopyFrom(other);
            this.Conditions = new List<string>(other.Conditions);
            this.Actions = new List<string>(other.Actions);
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
