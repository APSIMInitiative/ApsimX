using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;
using Models.PMF.Library;
using Models.PMF.Phen;
using System.Linq;
using APSIM.Shared.Documentation;


namespace Models.Functions
{
    /// <summary>Accumulates a child function between a start and end stage or start and end events.
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous accumulation each time the Specified Accumulate Event occurs." +
        "Option for crop models is to specify start and end stages for accumulation." +
        "Option for all models is to specify stant and and events for accumulation." +
        "If neither are specified will accumulate every time the accumulate event occurs." +
        "Optional full or partial removal of accumulated values can occur on specified events or stages")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateFunction : Model, IFunction
    {
        ///Links
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>The phenology</summary>
        [Link]
        Phenology phenology = null;

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// Private class members
        /// -----------------------------------------------------------------------------------------------------------

        private int startStageIndex;

        private int endStageIndex;
       
        private double AccumulatedValue = 0;

        private bool AccumulateToday = false;

        private IEnumerable<IFunction> ChildFunctions;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>The event that accumulation happens on</summary>
        [Separator("Event to accumulate on.  (Typically PostPhenology for plants but could be any event)")]
        [Description("Event to accumulate on")]
        public string AccumulateEventName { get; set; }

        /// <summary>The start stage name</summary>
        [Separator("Optional, specify stages or events to accumulate between")]
        [Separator("Can not specify both start/end events and stages")]
        [Description("(optional for plant models) Stage name to start accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string StartStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("(optional for plant models) Stage name to stop accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string EndStageName { get; set; }

        /// <summary>The start event</summary>
        [Description("(optional for any model) Event name to start accumulation")]
        public string StartEventName { get; set; }

        /// <summary>The end event</summary>
        [Description("(optional for any model) Event name to stop accumulation")]
        public string EndEventName { get; set; }

        /// <summary>The reset stage name</summary>
        [Separator("Optional, reduce accumulation")]
        [Separator("Can not specify both events and stages for reducing accumulation")]
        [Description("(optional for plant models) Stage name to reduce accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string ReduceStageName { get; set; }

        /// <summary>The end event</summary>
        [Description("(optional for any model)  Event name to reduce accumulation.")]
        public string ReduceEventName { get; set; }

        /// <summary>Fraction to reduce accumalation to</summary>
        [Description("Fraction to remove on stage or event specified above")]
        [Units("0-1")]
        public double FractionRemovedOnReduce { get; set; }

        /// <summary>The fraction removed on Cut event</summary>
        [Separator("Optional, for plant models, fractions to remove on defoliation events")]
        [Description("(optional) Fraction to remove on Cut")]
        public double FractionRemovedOnCut { get; set; }

        /// <summary>The fraction removed on Harvest event</summary>
        [Description("(optional) Fraction to remove on Harvest")]
        public double FractionRemovedOnHarvest { get; set; }

        /// <summary>The fraction removed on Graze event</summary>
        [Description("(optional) Fraction to remove on Graze")]
        public double FractionRemovedOnGraze { get; set; }

        /// <summary>The fraction removed on Prun event</summary>
        [Description("(optional) Fraction to remove on Prun")]
        public double FractionRemovedOnPrune { get; set; }

        /// <summary>String list of child functions</summary>
        public string ChildFunctionList
        {
            get
            {
                return AutoDocumentation.ChildFunctionList(this.FindAllChildren<IFunction>().ToList());
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
            startStageIndex = phenology.StartStagePhaseIndex(StartStageName);
            endStageIndex = phenology.EndStagePhaseIndex(EndStageName);
            if (StartEventName == null)
            {
                AccumulateToday = true;
            }
            else
            {
                AccumulateToday = false;
            }
        }

        /// <summary>
        /// Connect event handlers.
        /// </summary>
        /// <param name="sender">Sender object..</param>
        /// <param name="args">Event data.</param>
        [EventSubscribe("SubscribeToEvents")]
        private void OnConnectToEvents(object sender, EventArgs args)
        {
            events.Subscribe(StartEventName, OnStartEvent);
            events.Subscribe(EndEventName, OnEndEvent);
            events.Subscribe(AccumulateEventName, OnAccumulateEvent);
            events.Subscribe(ReduceEventName, OnRemoveEvent);
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        private void OnAccumulateEvent(object sender, EventArgs e)
        {
            if ((StartStageName != null) && (EndStageName != null))
            {
                if (phenology.Between(startStageIndex, endStageIndex))
                {
                    AccumulateToday = true;
                }
                else
                {
                    AccumulateToday = false;
                }
            }

            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            if (AccumulateToday)
            {
                double DailyIncrement = 0.0;
                foreach (IFunction function in ChildFunctions)
                {
                    DailyIncrement += function.Value();
                }
                AccumulatedValue += DailyIncrement;
            }

        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == ReduceStageName)
                AccumulatedValue -= FractionRemovedOnReduce * AccumulatedValue; ;
        }

        private void OnStartEvent(object sender, EventArgs args)
        {
            AccumulateToday = true;
        }

        private void OnEndEvent(object sender, EventArgs args)
        {
            AccumulateToday = false;
        }

        private void OnRemoveEvent(object sender, EventArgs args)
        {
            AccumulatedValue -= FractionRemovedOnReduce * AccumulatedValue;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return AccumulatedValue;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public override IEnumerable<ITag> Document()
        {
            yield return new Paragraph($"*{Name}* = Accumulated {ChildFunctionList} between {StartStageName.ToLower()} and {EndStageName.ToLower()}");

            foreach (var child in Children)
                foreach (var tag in child.Document())
                    yield return tag;
        }

        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCut(object sender, EventArgs e)
        {
            AccumulatedValue -= FractionRemovedOnCut * AccumulatedValue;
        }

        /// <summary>Called when harvest.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Harvesting")]
        private void OnHarvest(object sender, EventArgs e)
        {
            AccumulatedValue -= FractionRemovedOnHarvest * AccumulatedValue;
        }
        /// <summary>Called when Graze.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Grazing")]
        private void OnGraze(object sender, EventArgs e)
        {
            AccumulatedValue -= FractionRemovedOnGraze * AccumulatedValue;
        }

        /// <summary>Called when winter pruning.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Pruning")]
        private void OnPrune(object sender, EventArgs e)
        {
            AccumulatedValue -= FractionRemovedOnPrune * AccumulatedValue;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
        }
    }
}
