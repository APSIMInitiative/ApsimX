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

        /// <summary>detailed logging component</summary>
        [Link(Type = LinkType.Child, IsOptional = true)]
        public RotationRugplot detailedLogger = null;

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
        public List<Node> Nodes { get; set; } = new List<Node>();

        /// <summary>
        /// The arcs on the bubble chart which define transition
        /// between stages (nodes).
        /// </summary>
        public List<Arc> Arcs { get; set; } = new List<Arc>();

        /// <summary>
        /// Whether this component is a toplevel manager that does things by itself, or working in conjuction with another manager component
        /// </summary>
        [Description("Top Level")]
        [Tooltip("When enabled, this component will control and interact other (sub) management components, if not it does nothing unless requested")]
        public bool TopLevel { get; set; }

        /// <summary>
        /// Initial state of the rotation. Not relevant if we're in a multipaddock (non-toplevel) simulation.
        /// </summary>
        [Description("Initial State")]
        [Tooltip("Initial state of the rotation")]
        [Display(Type = DisplayType.DropDown, Values = nameof(States), VisibleCallback = "TopLevel")]
        public string InitialState { get; set; }

        /// <summary>
        /// Iff true, the rotation manager will print debugging diagnostics
        /// to the summary file during execution.
        /// </summary>
        [Description("Verbose Mode")]
        [Tooltip("When enabled, the rotation manager will print debugging diagnostics to the summary file during execution")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Next State ID of the rotation.
        /// </summary>
        [JsonIgnore]
        public int NextStateId { get; set; }

        /// <summary>
        /// Next State of the rotation.
        /// </summary>
        [JsonIgnore]
        public string NextState
        {
            get { return NextStateName; }
            set { NextStateId = getStateIDByName(value); }
        }

        /// <summary>
        /// Name of the Next State
        /// </summary>
        [JsonIgnore]
        public string NextStateName
        {
            get { return getStateNameByID(NextStateId); }
        }

        /// <summary>
        /// Current State ID of the rotation.
        /// </summary>
        [JsonIgnore]
        public int CurrentStateId { get; set; }

        /// <summary>
        /// Current State of the rotation.
        /// </summary>
        [JsonIgnore]
        public string CurrentState
        {
            get { return CurrentStateName; }
            set { CurrentStateId = getStateIDByName(value); }
        }

        /// <summary>
        /// Name of the Current State
        /// </summary>
        [JsonIgnore]
        public string CurrentStateName
        {
            get { return getStateNameByID(CurrentStateId); }
        }

        /// <summary>
        /// All dynamic events published by the rotation manager.
        /// </summary>
        public IEnumerable<string> Events
        {
            get
            {
                foreach (Node state in Nodes)
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

        private string getStateNameByID(int id)
        {
            foreach (Node state in Nodes)
                if (state.ID == id)
                    return state.Name;
            return "No State";
        }

        private int getStateIDByName(string name)
        {
            foreach (Node state in Nodes)
                if (state.Name == name)
                    return state.ID;
            return 0;
        }

        /// <summary>
        /// Called when a simulation commences. Performs one-time initialisation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("Commencing")]
        private void OnCommence(object sender, EventArgs e)
        {
            CurrentStateId = 0;
            for (int i = 0; i < Nodes.Count && CurrentStateId == 0; i++)
                if (Nodes[i].Name == InitialState)
                    CurrentStateId = Nodes[i].ID;

            if (Verbose)
                summary.WriteMessage(this, $"Initialised, state={CurrentStateName} (of {Nodes.Count} total)", MessageType.Diagnostic);
        }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            DoLogState();
        }

        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
        {
            DoLogState();
        }

        /// <summary>
        /// Called once per day during the simulation.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (!TopLevel) { return; }

            MadeAChange = false;
            bool more = true;
            while (more)
            {
                more = false;
                double bestScore = -1.0;
                Arc bestArc = null;
                foreach (var arc in Arcs.FindAll(arc => arc.SourceID == CurrentStateId))
                {
                    double score = 1;
                    foreach (string testCondition in arc.Conditions)
                    {
                        if (testCondition.Length > 0)
                        {
                            object value;
                            try
                            {
                                value = FindByPath(testCondition)?.Value;
                                if (value == null)
                                    throw new Exception("Test condition returned nothing");
                            }
                            catch (Exception ex)
                            {
                                throw new AggregateException($"Error while evaluating transition from {getStateNameByID(arc.SourceID)} to {getStateNameByID(arc.DestinationID)} - rule '{testCondition}': " + ex.Message);
                            }
                            double result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            detailedLogger?.DoRuleEvaluation(getStateNameByID(arc.DestinationID), testCondition, result);
                            score *= result;
                        }
                    }

                    if (Verbose)
                    {
                        string arcName = $"Transition from {getStateNameByID(arc.SourceID)} to {getStateNameByID(arc.DestinationID)} by {arc.Name}";
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
                    detailedLogger?.DoTransition(getStateNameByID(bestArc.DestinationID));
                    TransitionTo(bestArc);
                    more = true;
                    MadeAChange = true;
                }
            }
        }

        private bool MadeAChange;

        /// <summary>
        /// Do our rule evaluation when asked by method call
        /// </summary>
        public bool DoManagement()
        {
            bool oldState = TopLevel; // I can't see why this method would called when it is a toplevel, but...
            TopLevel = true;
            OnDoManagement(null, new EventArgs());
            TopLevel = oldState;
            return (MadeAChange);
        }

        /// <summary>
        /// Log the state of the system (usually beginning/end of simulation)
        /// </summary>
        public void DoLogState()
        {
            detailedLogger?.DoTransition(CurrentStateName);
        }

        /// <summary>
        /// Set the current state to the first node with the given name
        /// </summary>
        public void SetCurrentStateByName(string name)
        {
            CurrentStateId = getStateIDByName(name);
            return;
        }

        /// <summary>
        /// Set the current state to the first node with the given name
        /// </summary>
        public string GetCurrentStateName()
        {
            return getStateNameByID(CurrentStateId);
        }

        /// <summary>
        /// Transition along an arc to another stage/node.
        /// </summary>
        /// <param name="transition">The arc to be followed.</param>
        private void TransitionTo(Arc transition)
        {
            try
            {
                if (Verbose)
                    summary.WriteMessage(this, $"Transitioning from {getStateNameByID(transition.SourceID)} to {getStateNameByID(transition.DestinationID)} by {transition.Name}", MessageType.Diagnostic);
                // Publish pre-transition events.
                eventService.Publish($"TransitionFrom{CurrentStateName}", null);

                NextStateId = transition.DestinationID;
                Transition?.Invoke(this, EventArgs.Empty);
                CurrentStateId = transition.DestinationID;

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
                eventService.Publish($"TransitionTo{NextStateName}", null);
                if (Verbose)
                    summary.WriteMessage(this, $"Current state is now {CurrentStateName}", MessageType.Diagnostic);
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to transition to state {CurrentStateName}", err);
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
}
