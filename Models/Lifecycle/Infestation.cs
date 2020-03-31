namespace Models.LifeCycle
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Sets and infestation event for Lifecycle model.  
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Infestation : Model, IInfest
    {
        /// <summary> Clock </summary>
        [Link]
        public Clock Clock = null;

        /// <summary>Sets the type of infestation event</summary>
        [Description("Select the type of infestation event")]
        public InfestationType TypeOfInfestation { get; set; }
        
        /// <summary>Options for types of infestation</summary>
        public enum InfestationType
        {
            /// <summary>infestation on Simulation start</summary>
            OnStart,
            /// <summary>infestation on InfestationDate</summary>
            OnDate,
            /// <summary>Daily infestation between Infestation Date and InfestationEndDate</summary>
            BetweenDates,
            /// <summary>Daily infestation for duration of simulation</summary>
            Continious
        }

        /// <summary>"The name of organisum that arrives in the zone" </summary>
        [Description("The name of organisum that infests the zone")]
        [Display(Type = DisplayType.LifeCycleName)]
        public string InfestingOrganisumName { get; set; }

        /// <summary>"The organisum that arrives the zone" </summary>
        private LifeCycle InfestingOrganisum { get; set; }

        /// <summary>"The LifeCyclePhase of the organism when it arrives" </summary>
        [Description("The LifeCyclePhase of the organism when it arrives")]
        [Display(Type = DisplayType.LifePhaseName)]
        public string InfestingPhaseName { get; set; }

        /// <summary>"The arriving organisum LifeCyclePhase" </summary>
        private LifeCyclePhase InfestingPhase { get; set; }

        /// <summary>Date of infestation</summary>
        [Description("Date of infestation (dd-MMM)")]
        public string InfestationDate { get; set; }

        /// <summary>Date of infestation</summary>
        [Description("Date of infestation (dd-MMM)")]
        public string InfestationEndDate { get; set; }

        /// <summary>The number of immigrants arriving with infestation event</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction NumberOfImmigrants = null;

        /// <summary>The chronoligical age of immigrants arriving with infestation event</summary>
        [Description("Chronological Age of Immigrants (days)")]
        public int ChronoAgeOfImmigrants { get; set; }

        /// <summary>The Physiological age of immigrants arriving with infestation event</summary>
        [Description("Physiological Age of Immigrants (0-1)")]
        public double PhysAgeOfImmigrants { get; set; }

        private bool Start = true;

        /// <summary>Method to send infestation event to LifeCycle</summary>
        public void Infest()
        {
            Cohort Immigrants = new Cohort(InfestingPhase);
            Immigrants.Population = NumberOfImmigrants.Value();
            Immigrants.ChronologicalAge = ChronoAgeOfImmigrants;
            Immigrants.PhysiologicalAge = PhysAgeOfImmigrants;
            InfestingOrganisum.Infest(Immigrants);
        }

        /// <summary>At the start of the simulation find the infesting lifecycle and phase</summary>
        /// <param name="sender"></param> 
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            InfestingOrganisum = Apsim.Find(this.Parent, InfestingOrganisumName) as LifeCycle;
            if (InfestingOrganisum == null)
                throw new Exception(Apsim.FullPath(this) + " Could not find an infesting organisum called " + InfestingOrganisumName);
            InfestingPhase = Apsim.Child(InfestingOrganisum, InfestingPhaseName) as LifeCyclePhase;
        }

        /// <summary>Call infest() events at specified time steps</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if ((Start) && (TypeOfInfestation == InfestationType.OnStart))
            {
                Infest();
                Start = false;
                return;
            }
            else if (TypeOfInfestation == InfestationType.OnDate)
            {
                if (DateUtilities.DatesEqual(InfestationDate, Clock.Today))
                    Infest();
                return;
            }
            else if (TypeOfInfestation == InfestationType.BetweenDates)
            {
                if (DateUtilities.WithinDates(InfestationDate, Clock.Today, InfestationEndDate))
                    Infest();
                return;
            }
            else if (TypeOfInfestation == InfestationType.Continious)
            {
                Infest();
                return;
            }
        }
    }
}
