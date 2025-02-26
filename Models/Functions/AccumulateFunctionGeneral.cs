using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Phen;
using System.Linq;
using APSIM.Shared.Utilities;


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
    public class AccumulateFunctionGeneral : Model, IFunction
    {
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

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>The event that accumulation happens on</summary>
        [Separator("Event to accumulate on.  (Typically PostPhenology for plants but could be any event)")]
        [Description("Event to accumulate on")]
        public string AccumulateEventName { get; set; }

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
        [Separator("Optional, reduce accumulation")]
        [Description("(optional for plant models) Stage name to reduce accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string ReduceStageName { get; set; }

        /// <summary>The end event</summary>
        [Description("(optional for any model)  Event name to reduce accumulation.")]
        public string ReduceEventName { get; set; }

        /// <summary>The end event</summary>
        [Description("(optional for any model)  Date (d-mmm) to reduce accumulation.")]
        public string ReduceEventDate { get; set; }

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

            if (!String.IsNullOrEmpty(StartEventName) && !String.IsNullOrEmpty(StartStageName) && !String.IsNullOrEmpty(StartDate))
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
            if (!String.IsNullOrEmpty(StartEventName))
            { 
                events.Subscribe(StartEventName, OnStartEvent); 
            }
            if (!String.IsNullOrEmpty(EndEventName))
            { 
                events.Subscribe(EndEventName, OnEndEvent); 
            }
            if (!String.IsNullOrEmpty(AccumulateEventName))
            { 
                events.Subscribe(AccumulateEventName, OnAccumulateEvent); 
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
                if (DateUtilities.WithinDates(StartDate, clock.Today, StartDate))
                {
                    AccumulateToday = true;
                }
            }

            if (!String.IsNullOrEmpty(EndDate)) 
            {
                if (DateUtilities.WithinDates(EndDate, clock.Today, EndDate))
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

            if (!String.IsNullOrEmpty(ReduceEventDate))
            {
                if (DateUtilities.WithinDates(ReduceEventDate, clock.Today, ReduceEventDate))
                {
                    AccumulatedValue -= FractionRemovedOnReduce * AccumulatedValue;
                }
            }

        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (!String.IsNullOrEmpty(StartStageName))
            {
                if (phaseChange.StageName == StartStageName)
                    AccumulateToday = true;
            }

            if (!String.IsNullOrEmpty(EndStageName))
            {
                if(phaseChange.StageName == EndStageName)
                    AccumulateToday = false;
            }
            
            if (!String.IsNullOrEmpty(ReduceStageName))
            {
                if (phaseChange.StageName == ReduceStageName)
                    if (!Double.IsNaN(FractionRemovedOnReduce))
                    {
                        AccumulatedValue -= FractionRemovedOnReduce * AccumulatedValue;
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
            if (!Double.IsNaN(FractionRemovedOnReduce))
            {
                AccumulatedValue -= FractionRemovedOnReduce * AccumulatedValue;
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
                AccumulatedValue -= FractionRemovedOnCut * AccumulatedValue;
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
                AccumulatedValue -= FractionRemovedOnHarvest * AccumulatedValue;
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
                AccumulatedValue -= FractionRemovedOnGraze * AccumulatedValue;
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
                AccumulatedValue -= FractionRemovedOnPrune * AccumulatedValue;
            }
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
