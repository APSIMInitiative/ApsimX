using System;
using Models.Core;
using Models.PMF.Phen;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.PMF;
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
    public class SetOnEvent : Model, IFunction, IScopeDependency
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

        /// <summary>The pre event value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PreSetValue = null;
        /// <summary>The post event value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PostSetValue = null;


        /// Private class members
        /// -----------------------------------------------------------------------------------------------------------

        private double setValue = 0;

        private Phenology parentPhenology = null;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>Name of crop to trigger events</summary>
        [Separator("Crop to Link to.  Optional parameter if crop stages are for setting or resetting values.  Not needed if this function is a child of plant model)")]
        [Description("Crop to link to.")]
        [Display(Type = DisplayType.PlantName)]
        public string NameOfPlantToLink { get; set; }

        /// <summary>Event that sets return to PostEventValue</summary>
        [Separator("Events options to set return to PostEventValue. Can have more that one option")]
        [Description("Event that sets return to PostEventValue")]
        public string SetEventName { get; set; }

        /// <summary>Crop stage that sets return to PostEventValue</summary>
        [Description("Crop stage that sets return to PostEventValue")]
        [Display(Type = DisplayType.CropStageName)]
        public string SetStageName { get; set; }

        /// <summary>Dates when return is set to PostEventValue</summary>
        [Description("Dates when return is set to PostEventValue (comma separated, dd/mm/yyyy or dd-mmm):")]
        public string[] SetDates { get; set; }


        /// <summary>Event that resets return to PreEventValue</summary>
        [Separator("Events options to reset return to PreEventValue. Can have more that one option")]
        [Description("Event that resets return to PreEventValue")]
        public string ReSetEventName { get; set; }

        /// <summary>Event that resets return to PreEventValue</summary>
        [Description("CropStage that resets return to PreEventValue")]
        [Display(Type = DisplayType.CropStageName)]
        public string ReSetStageName { get; set; }

        /// <summary>The end event</summary>
        [Description("Dates that resets return to PreEventValue (comma separated, dd/mm/yyyy or dd-mmm):")]
        public string[] ReSetDates { get; set; }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            setValue = PreSetValue.Value();
            if (!String.IsNullOrEmpty(NameOfPlantToLink))
            {
                parentPhenology = scope.Find<Plant>(NameOfPlantToLink).Phenology;
            }
            else
            {
                parentPhenology = FindAllAncestors<Plant>().FirstOrDefault()?.Phenology;
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
            if (!String.IsNullOrEmpty(SetEventName))
            {
                events.Subscribe(SetEventName, OnSetEvent);
            }

            if (!String.IsNullOrEmpty(ReSetEventName))
            {
                events.Subscribe(ReSetEventName, OnReSetEvent);
            }
        }

        /// <summary>Called when set event is triggered</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        private void OnSetEvent(object sender, EventArgs e)
        {
            setValue = PostSetValue.Value();
        }

        /// <summary>Called when set event is triggered</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        private void OnReSetEvent(object sender, EventArgs e)
        {
            setValue = PreSetValue.Value();
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (!String.IsNullOrEmpty(SetStageName))
            {
                if (phaseChange.StageName == SetStageName)
                {
                    setValue = PostSetValue.Value();
                }
            }

            if (!String.IsNullOrEmpty(ReSetStageName))
            {
                if (phaseChange.StageName == ReSetStageName)
                {
                    setValue = PreSetValue.Value();
                }
            }
        }

        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if ((SetDates != null) && (!String.IsNullOrEmpty(SetDates[0])))
            {
                foreach (string date in SetDates)
                {
                    DateTime SetDate = DateUtilities.GetDate(date, clock.Today.Year);
                    if (DateTime.Compare(clock.Today, SetDate) == 0)
                    {
                        setValue = PostSetValue.Value();
                    }
                }
            }

            if ((ReSetDates != null) && (!String.IsNullOrEmpty(ReSetDates[0])))
            {
                foreach (string date in ReSetDates)
                {
                    DateTime ReSetDate = DateUtilities.GetDate(date, clock.Today.Year);
                    if (DateTime.Compare(clock.Today, ReSetDate) == 0)
                    {
                        setValue = PreSetValue.Value();
                    }
                }

            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return setValue;
        }
    }
}
