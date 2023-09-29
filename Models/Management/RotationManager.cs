using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Management
{

    /// <summary>
    /// The rotation manager model
    /// </summary>
    /// <remarks>
    /// The rotation manager visualizes and helps to implement the logic
    /// in a crop rotation. By itself, the rotation manager understands
    /// very little of the components with which it is interacting.
    /// Instead, it relies on other components (usually manager scripts)
    /// for their specific knowledge. An example crop rotation is provided
    /// in the RotationManager.apsimx example file.
    /// 
    /// todo:
    ///
    /// - Implement node/arc ID separate from name?
    /// - dynamic / auto layout of new nodes/arcs
    /// - ?intellisense isn't picking up member functions? events are OK.
    /// - Syntax checking of rules / actions.
    /// - "fixme" where noted in code
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.BubbleChartView")]
    [PresenterName("UserInterface.Presenters.BubbleChartPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class RotationManager : Model, IBubbleChart, IPublisher
    {
        /// <summary>For logging</summary>
        [Link] private Summary summary = null;

        /// <summary>
        /// Events service. Used to publish events when transitioning
        /// between stages/nodes.
        /// </summary>
        [NonSerialized]
        [Link]
        private Events eventService = null;

        /// <summary>
        /// The nodes of the graph. These represent states of the rotation.
        /// </summary>
        public List<StateNode> Nodes { get; set; } = new List<StateNode>();

        /// <summary>
        /// The arcs on the bubble chart which define transition
        /// between stages (nodes).
        /// </summary>
        public List<RuleAction> Arcs { get; set; } = new List<RuleAction>();

        /// <summary>
        /// Initial state of the rotation.
        /// </summary>
        [Description("Initial State")]
        [Tooltip("Initial state of the rotation")]
        [Display(Type = DisplayType.DropDown, Values = nameof(States))]
        public string InitialState { get; set; }

        /// <summary>
        /// Iff true, the rotation manager will print debugging diagnostics
        /// to the summary file during execution.
        /// </summary>
        [Description("Verbose Mode")]
        [Tooltip("When enabled, the rotation manager will print debugging diagnostics to the summary file during execution")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Current State of the rotation.
        /// </summary>
        [JsonIgnore]
        public string CurrentState { get; private set; }

        /// <summary>
        /// All dynamic events published by the rotation manager.
        /// </summary>
        /// <remarks>
        /// fixme:
        /// If any nodes are disconnected from the rest of the graph,
        /// their names will still be included in this list.
        /// </remarks>
        public IEnumerable<string> Events
        {
            get
            {
                foreach (StateNode state in Nodes)
                {
                    yield return $"TransitionFrom{state}";
                    yield return $"TransitionTo{state}";
                }
            }
        }

        /// <summary>
        /// Called when transitioning between states.
        /// </summary>
        public event EventHandler Transition;

        private string[] States()
        {
            return Nodes.Select(n => n.Name).ToArray();
        }

        /// <summary>
        /// Called when a simulation commences. Performs one-time initialisation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("Commencing")]
        private void OnCommence(object sender, EventArgs e)
        {
            CurrentState = InitialState;
            if (Verbose)
                summary.WriteMessage(this, $"Initialised, state={CurrentState} (of {Nodes.Count} total)", MessageType.Diagnostic);
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
                foreach (var arc in Arcs.FindAll(arc => arc.SourceName == CurrentState))
                {
                    double score = 1;
                    foreach (string testCondition in arc.Conditions)
                    {
                        object value = FindByPath(testCondition)?.Value;
                        if (value == null)
                            throw new Exception($"Test condition '{testCondition}' returned nothing");
                        score *= Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    }

                    if (Verbose)
                    {
                        string arcName = $"Transition from {arc.SourceName} to {arc.DestinationName}";
                        string message;
                        if (score > 0)
                        {
                            if (score > bestScore)
                                message = $"{arcName} is possible and weight of {score} exceeds previous best weight of {bestScore}";
                            else
                                message = $"{arcName} is possible but weight of {score} does not exceed previous best weight of {bestScore}";
                        }
                        else
                            message = $"{arcName} is not possible. Weight = {score}";
                        summary.WriteMessage(this, message, MessageType.Diagnostic);
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
                if (Verbose)
                    summary.WriteMessage(this, $"Transitioning from {transition.SourceName} to {transition.DestinationName}", MessageType.Diagnostic);
                // Publish pre-transition events.
                eventService.Publish($"TransitionFrom{CurrentState}", null);
                Transition?.Invoke(this, EventArgs.Empty);

                CurrentState = transition.DestinationName;

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
                eventService.Publish($"TransitionTo{CurrentState}", null);
                if (Verbose)
                    summary.WriteMessage(this, $"Current state is now {CurrentState}", MessageType.Diagnostic);
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to transition to state {CurrentState}", err);
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
        public RuleAction(Arc a) : base(a)
        {
            Conditions = new List<string>();
            Actions = new List<string>();
        }

        /// <summary>Test conditions that need to be satisfied for this transition</summary>
        public List<string> Conditions { get; set; }

        /// <summary>Actions undertaken when making this transition</summary>
        public List<string> Actions { get; set; }

        /// <param name="other"></param>
        public void CopyFrom(RuleAction other)
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
