using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Phen;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.PMF;
using System.Data;
using APSIM.Core;


namespace Models.Functions
{
    /// <summary>Accumulates a child function between a start and end stage or start and end events.
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous accumulation each time the Specified Accumulate Event occurs.  " +
        "Option for crop models is to specify start and end stages for accumulation.  " +
        "Option for all models is to specify start and end events for accumulation.  " +
        "Option for all models is to specify start and end dates for accumulation.  " +
        "If none are specified will accumulate every time the accumulate event occurs.  " +
        "Optional full or partial removal of accumulated values can occur on specified events, stages or dates")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateFunctionGeneral : Model, IFunction, IScopeDependency
    {
        [NonSerialized]
        private IScope scope;

        /// <summary>Scope supplied by APSIM.core.</summary>
        public void SetScope(IScope scope) => this.scope = scope;

        ///Links
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        [Link]
        private Clock clock = null;


        /// Private class members
        /// -----------------------------------------------------------------------------------------------------------

        private double AccumulatedValue = 0;

        private bool AccumulateToday = false;

        private IEnumerable<IFunction> ChildFunctions;

        private Phenology parentPhenology = null;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>The event that accumulation happens on</summary>
        [Separator("Event to accumulate on.  ([Clock].DoReportCalculations by default. Type valid event to overwrite default)")]
        [Description("Event to accumulate on")]
        public string AccumulateEventName { get; set; }

        /// <summary>Name of crop to remove biomass from.</summary>
        [Separator("Crop to Link to.  Optional parameter if crop stages are used to start, stop or reduce.  Not needed if this function is a child of plant model)")]
        [Description("Crop to link to.  (Not needed is function is child of plant model)")]
        [Display(Type = DisplayType.PlantName)]
        public string NameOfPlantToLink { get; set; }

        /// <summary>The start stage name</summary>
        [Separator("Optional, specify stages or events to accumulate between, accumulates for duration of simulation if all are blank")]
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

        /// <summary>The start event</summary>
        [Description("(optional for any model) Date (d-mmm) to start accumulation")]
        public string StartDate { get; set; }

        /// <summary>The end event</summary>
        [Description("(optional for any model) Date (d-mmm) to stop accumulation")]
        public string EndDate { get; set; }

        /// <summary>The reset stage name</summary>
        [Separator("Optional, reduce accumulation at specific crop stage")]
        [Description("(optional for plant models) Stage name to reduce accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string ReduceStageName { get; set; }

        /// <summary>Fraction to reduce accumalation to</summary>
        [Description("Fraction to remove on stage specified above")]
        [Units("0-1")]
        public double FractionRemovedOnStage { get; set; }

        /// <summary>The end event</summary>
        [Separator("Optional, reduce accumulation on specified event")]
        [Description("(optional for any model)  Event name to reduce accumulation.")]
        public string ReduceEventName { get; set; }

        /// <summary>Fraction to reduce accumalation to</summary>
        [Description("Fraction to remove on event specified above")]
        [Units("0-1")]
        public double FractionRemovedOnEvent { get; set; }

        /// <summary>The end event</summary>
        [Separator("Optional, reduce accumulation on specified date or dates")]
        [Description("List of dates for removal events (comma separated, dd/mm/yyyy or dd-mmm):")]
        public string[] ReduceDates { get; set; }

        /// <summary>Fraction to reduce accumalation to</summary>
        [Description("Fraction to remove on date specified above")]
        [Units("0-1")]
        public double FractionRemovedOnDate { get; set; }

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

        /// <summary>The fraction removed on Prune event</summary>
        [Description("(optional) Fraction to remove on Prune")]
        public double FractionRemovedOnPrune { get; set; }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
            AccumulateToday = true;
            if (!String.IsNullOrEmpty(NameOfPlantToLink))
            {
                parentPhenology = scope.Find<Plant>(NameOfPlantToLink).Phenology;
            }
            else
            {
                parentPhenology = FindAllAncestors<Plant>().FirstOrDefault()?.Phenology;
            }

            if (!String.IsNullOrEmpty(StartEventName))
            {
                AccumulateToday = false;
            }

            if(Convert.ToInt32(!String.IsNullOrEmpty(StartEventName)) + Convert.ToInt32(!String.IsNullOrEmpty(StartStageName)) + Convert.ToInt32(!String.IsNullOrEmpty(StartDate)) > 1)
            {
                throw new Exception("Can only select one option for starting accumulation, Stage, Date or Event.  Currently more than one are specified for " + this.Name);
            }

            if (Convert.ToInt32(!String.IsNullOrEmpty(EndEventName)) + Convert.ToInt32(!String.IsNullOrEmpty(EndStageName)) + Convert.ToInt32(!String.IsNullOrEmpty(EndDate)) > 1)
            {
                throw new Exception("Can only select one option for stoping accumulation, Stage, Date or Event.  Currently more than one are specified for " + this.Name);
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
            if (!String.IsNullOrEmpty(AccumulateEventName))
            {
                if (AccumulateEventName == "[Clock].EndOfDay")
                    throw new Exception(this.Name + " cannot use [Clock].EndOfDay for accumulate event as this is reserved for doing removals on the final event of the day to ensure it happens after accmulation.  Choose another option for Event to accumulate on");
                events.Subscribe(AccumulateEventName, OnAccumulateEvent);
            }
            else
            {
                events.Subscribe("[Clock].DoReportCalculations", OnAccumulateEvent);
            }

            if (!String.IsNullOrEmpty(StartEventName))
            {
                events.Subscribe(StartEventName, OnStartEvent);
            }
            if (!String.IsNullOrEmpty(EndEventName))
            {
                events.Subscribe(EndEventName, OnEndEvent);
            }

            if (!String.IsNullOrEmpty(ReduceEventName))
            {
                events.Subscribe(ReduceEventName, OnRemoveEvent);
            }
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        private void OnAccumulateEvent(object sender, EventArgs e)
        {

            if (!String.IsNullOrEmpty(StartDate))
            {
                DateTime startDate = DateUtilities.GetDate(StartDate, clock.Today.Year);
                AccumulateToday = (DateTime.Compare(clock.Today, startDate) > 0);
            }

            if (!String.IsNullOrEmpty(EndDate))
            {
                DateTime endDate = DateUtilities.GetDate(EndDate, clock.Today.Year);
                if (DateTime.Compare(clock.Today, endDate) > 0)
                {
                    AccumulateToday = false;
                }
            }

            if (!String.IsNullOrEmpty(StartStageName))
            {
                AccumulateToday = parentPhenology.Beyond(StartStageName);
            }

            if (!String.IsNullOrEmpty(EndStageName))
            {
                if (parentPhenology.Beyond(EndStageName))
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

            if ((ReduceDates!=null)&&(!String.IsNullOrEmpty(ReduceDates[0])))
            {
                foreach (string date in ReduceDates)
                {
                    DateTime reduceDate = DateUtilities.GetDate(date, clock.Today.Year);
                    if (DateTime.Compare(clock.Today, reduceDate) == 0)
                    {
                        if (!Double.IsNaN(FractionRemovedOnDate))
                        {
                            reduceDateToday = true;
                        }
                    }
                }
            }

        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (!String.IsNullOrEmpty(ReduceStageName))
            {
                if (phaseChange.StageName == ReduceStageName)
                    if (!Double.IsNaN(FractionRemovedOnStage))
                    {
                        reduceStageToday = true;
                    }
            }
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
            if (!Double.IsNaN(FractionRemovedOnEvent))
            {
                reduceEventToday = true;
            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return AccumulatedValue;
        }

        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCut(object sender, EventArgs e)
        {
            if (!Double.IsNaN(FractionRemovedOnCut))
            {
                cutToday = true;
            }
        }

        /// <summary>Called when harvest.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Harvesting")]
        private void OnHarvest(object sender, EventArgs e)
        {
            if (!Double.IsNaN(FractionRemovedOnHarvest))
            {
                harvestToday = true;
            }
        }
        /// <summary>Called when Graze.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Grazing")]
        private void OnGraze(object sender, EventArgs e)
        {
            if (!Double.IsNaN(FractionRemovedOnGraze))
            {
                grazeToday = true;
            }
        }

        /// <summary>Called when winter pruning.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Pruning")]
        private void OnPrune(object sender, EventArgs e)
        {
            if (!Double.IsNaN(FractionRemovedOnPrune))
            {
                pruneToday = true;
            }
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if ((parentPhenology != null)&&String.IsNullOrEmpty(NameOfPlantToLink))
            {
                AccumulatedValue = 0;
            }
        }

        private bool harvestToday = false;
        private bool cutToday = false;
        private bool grazeToday = false;
        private bool pruneToday = false;
        private bool reduceDateToday = false;
        private bool reduceEventToday = false;
        private bool reduceStageToday = false;

        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            if (pruneToday)
            {
                AccumulatedValue -= FractionRemovedOnPrune * AccumulatedValue;
                pruneToday = false;
            }
            if (grazeToday)
            {
                AccumulatedValue -= FractionRemovedOnGraze * AccumulatedValue;
                grazeToday = false;
            }
            if (cutToday)
            {
                AccumulatedValue -= FractionRemovedOnCut * AccumulatedValue;
                cutToday = false;
            }
            if (harvestToday)
            {
                AccumulatedValue -= FractionRemovedOnHarvest * AccumulatedValue;
                harvestToday = false;
            }
            if (reduceDateToday)
            {
                AccumulatedValue -= FractionRemovedOnDate * AccumulatedValue;
                reduceDateToday = false;
            }
            if(reduceEventToday)
            {
                AccumulatedValue -= FractionRemovedOnEvent * AccumulatedValue;
                reduceEventToday = false;
            }
            if(reduceStageToday)
            {
                AccumulatedValue -= FractionRemovedOnStage * AccumulatedValue;
                reduceStageToday = false;
            }
        }
    }
}
