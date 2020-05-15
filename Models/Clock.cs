namespace Models
{
    using Models.Core;
    using Models.Core.Run;
    using Models.Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System.Data;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// The clock model is resonsible for controlling the daily timestep in APSIM. It 
    /// keeps track of the simulation date and loops from the start date to the end
    /// date, publishing events that other models can subscribe to.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Clock : Model, IClock, ICustomDocumentation
    {
        /// <summary>The arguments</summary>
        private EventArgs args = new EventArgs();

        /// <summary>The summary</summary>
        [Link]
        private ISummary Summary = null;

        /// <summary>The start date of the simulation.</summary>
        [Summary]
        [Description("The start date of the simulation")]
        public DateTime? Start { get; set; }

        /// <summary>The end date of the simulation.</summary>
        [Summary]
        [Description("The end date of the simulation")]
        public DateTime? End { get; set; }

        /// <summary>
        /// Gets the start date for the simulation.
        /// </summary>
        /// <remarks>
        /// If the user did not
        /// not provide a start date, attempt to locate a weather file
        /// and use its start date. If no weather file can be found,
        /// throw an exception.
        /// </remarks>
        [JsonIgnore]
        public DateTime StartDate
        {
            get
            {
                if (Start != null)
                    return (DateTime)Start;

                // If no start date provided, try and find a weather component and use its start date.
                IWeather weather = Apsim.Find(this, typeof(IWeather)) as IWeather;
                if (weather != null)
                    return weather.StartDate;

                throw new Exception($"No start date provided in clock {Apsim.FullPath(this)} and no weather file could be found.");
            }
            set
            {
                Start = value;
            }
        }

        /// <summary>
        /// Gets or sets the end date for the simulation.
        /// </summary>
        /// <remarks>
        /// If the user did not
        /// not provide a end date, attempt to locate a weather file
        /// and use its end date. If no weather file can be found,
        /// throw an exception.
        /// </remarks>
        [JsonIgnore]
        public DateTime EndDate
        {
            get
            {
                if (End != null)
                    return (DateTime)End;

                // If no start date provided, try and find a weather component and use its start date.
                IWeather weather = Apsim.Find(this, typeof(IWeather)) as IWeather;
                if (weather != null)
                    return weather.EndDate;

                throw new Exception($"No end date provided in {Apsim.FullPath(this)}: and no weather file could be found.");
            }
            set
            {
                End = value;
            }
        }

        // Public events that we're going to publish.
        /// <summary>Occurs when [start of simulation].</summary>
        public event EventHandler StartOfSimulation;
        /// <summary>Occurs when [start of day].</summary>
        public event EventHandler StartOfDay;
        /// <summary>Occurs when [start of month].</summary>
        public event EventHandler StartOfMonth;
        /// <summary>Occurs when [start of year].</summary>
        public event EventHandler StartOfYear;
        /// <summary>Occurs when [start of week].</summary>
        public event EventHandler StartOfWeek;
        /// <summary>Occurs when [end of day].</summary>
        public event EventHandler EndOfDay;
        /// <summary>Occurs when [end of month].</summary>
        public event EventHandler EndOfMonth;
        /// <summary>Occurs when [end of year].</summary>
        public event EventHandler EndOfYear;
        /// <summary>Occurs when [end of week].</summary>
        public event EventHandler EndOfWeek;
        /// <summary>Occurs when [end of simulation].</summary>
        public event EventHandler EndOfSimulation;
        /// <summary>Last initialisation event.</summary>
        public event EventHandler FinalInitialise;

        /// <summary>Occurs when [do weather].</summary>
        public event EventHandler DoWeather;
        /// <summary>Occurs when [do daily initialisation].</summary>
        public event EventHandler DoDailyInitialisation;
        /// <summary>Occurs when [do initial summary].</summary>
        public event EventHandler DoInitialSummary;
        /// <summary>Occurs when [do management].</summary>
        public event EventHandler DoManagement;
        /// <summary>Occurs when [do PestDisease damage]</summary>
        public event EventHandler DoPestDiseaseDamage;
        /// <summary>Occurs when [do energy arbitration].</summary>
        public event EventHandler DoEnergyArbitration;                                //MicroClimate
        /// <summary>Occurs when [do soil water movement].</summary>
        public event EventHandler DoSoilWaterMovement;                                //Soil module
        /// <summary>Occurs when [do soil temperature].</summary>
        public event EventHandler DoSoilTemperature;
        //DoSoilNutrientDynamics will be here
        /// <summary>Occurs when [do soil organic matter].</summary>
        public event EventHandler DoSoilOrganicMatter;                                 //SurfaceOM
        /// <summary>Occurs when [do surface organic matter decomposition].</summary>
        public event EventHandler DoSurfaceOrganicMatterDecomposition;                 //SurfaceOM
        /// <summary>Occurs when [do update transpiration].</summary>                   
        public event EventHandler DoUpdateWaterDemand;
        /// <summary>Occurs when [do water arbitration].</summary>
        public event EventHandler DoWaterArbitration;                                  //Arbitrator
        /// <summary>Occurs between DoWaterArbitration and DoPhenology. Performs sorghum final leaf no calcs.</summary>
        public event EventHandler PrePhenology;
        /// <summary>Occurs when [do phenology].</summary>                             
        public event EventHandler DoPhenology;                                         // Plant 
        /// <summary>Occurs when [do potential plant growth].</summary>
        public event EventHandler DoPotentialPlantGrowth;                              //Refactor to DoWaterLimitedGrowth  Plant        
        /// <summary>Occurs when [do potential plant partioning].</summary>
        public event EventHandler DoPotentialPlantPartioning;                          // PMF OrganArbitrator.
        /// <summary>Occurs when [do nutrient arbitration].</summary>
        public event EventHandler DoNutrientArbitration;                               //Arbitrator
        /// <summary>Occurs when [do potential plant partioning].</summary>
        public event EventHandler DoActualPlantPartioning;                             // PMF OrganArbitrator.
        /// <summary>Occurs when [do actual plant growth].</summary>
        public event EventHandler DoActualPlantGrowth;                                 //Refactor to DoNutirentLimitedGrowth Plant
        /// <summary>Occurs when [do update].</summary>
        public event EventHandler DoUpdate;
        /// <summary> Process stock methods in GrazPlan Stock </summary>
        public event EventHandler DoStock;
        /// <summary> Process a Pest and Disease lifecycle object </summary>
        public event EventHandler DoLifecycle;
        /// <summary>Occurs when [do management calculations].</summary>
        public event EventHandler DoManagementCalculations;
        /// <summary>Occurs when [do report calculations].</summary>
        public event EventHandler DoReportCalculations;
        /// <summary>Occurs when [do report].</summary>
        public event EventHandler DoReport;

        /// <summary>CLEM initialise Resources occurs once at start of simulation</summary>
        public event EventHandler CLEMInitialiseResource;
        /// <summary>CLEM initialise Activity occurs once at start of simulation</summary>
        public event EventHandler CLEMInitialiseActivity;
        /// <summary>CLEM validate all data entry</summary>
        public event EventHandler CLEMValidate;
        /// <summary>CLEM start of timestep event</summary>
        public event EventHandler CLEMStartOfTimeStep;
        /// <summary>CLEM set labour availability after start of timestep and financial considerations.</summary>
        public event EventHandler CLEMUpdateLabourAvailability;
        /// <summary>CLEM update pasture</summary>
        public event EventHandler CLEMUpdatePasture;
        /// <summary>CLEM detach pasture</summary>
        public event EventHandler CLEMDetachPasture;
        /// <summary>CLEM pasture has been added and is ready for use</summary>
        public event EventHandler CLEMPastureReady;
        /// <summary>CLEM cut and carry</summary>
        public event EventHandler CLEMDoCutAndCarry;
        /// <summary>CLEM Do Animal (Ruminant and Other) Breeding and milk calculations</summary>
        public event EventHandler CLEMAnimalBreeding;
        /// <summary>Get potential intake. This includes suckling milk consumption</summary>
        public event EventHandler CLEMPotentialIntake;
        /// <summary>Request and allocate resources to all Activities based on UI Tree order of priority. Some activities will obtain resources here and perform actions later</summary>
        public event EventHandler CLEMCalculateManure;
        /// <summary>Request and allocate resources to all Activities based on UI Tree order of priority. Some activities will obtain resources here and perform actions later</summary>
        public event EventHandler CLEMCollectManure;
        /// <summary>Request and perform the collection of maure after resources are allocated and manure produced in time-step</summary>
        public event EventHandler CLEMGetResourcesRequired;
        /// <summary>CLEM Calculate Animals (Ruminant and Other) milk production</summary>
        public event EventHandler CLEMAnimalMilkProduction;
        /// <summary>CLEM Calculate Animals(Ruminant and Other) weight gain</summary>
        public event EventHandler CLEMAnimalWeightGain;
        /// <summary>CLEM Do Animal (Ruminant and Other) death</summary>
        public event EventHandler CLEMAnimalDeath;
        /// <summary>CLEM Do Animal (Ruminant and Other) milking</summary>
        public event EventHandler CLEMAnimalMilking;
        /// <summary>CLEM Calculate ecological state after all deaths and before management</summary>
        public event EventHandler CLEMCalculateEcologicalState;
        /// <summary>CLEM Do Animal (Ruminant and Other) Herd Management (Kulling, Castrating, Weaning, etc.)</summary>
        public event EventHandler CLEMAnimalManage;
        /// <summary>CLEM stock animals to pasture availability or other metrics</summary>
        public event EventHandler CLEMAnimalStock;
        /// <summary>CLEM sell animals to market including transporting and labour</summary>
        public event EventHandler CLEMAnimalSell;
        /// <summary>CLEM buy animals including transporting and labour</summary>
        public event EventHandler CLEMAnimalBuy;
        /// <summary>CLEM Age your resources (eg. Decomose Fodder, Age your labour, Age your Animals)</summary>
        public event EventHandler CLEMAgeResources;
        /// <summary>CLEM event to calculate monthly herd summary</summary>
        public event EventHandler CLEMHerdSummary;
        /// <summary>CLEM end of timestep event</summary>
        public event EventHandler CLEMEndOfTimeStep;

        // Public properties available to other models.
        /// <summary>Gets the today.</summary>
        /// <value>The today.</value>
        [XmlIgnore]
        public DateTime Today { get; private set; }

        /// <summary>
        /// Returns the current fraction of the overall simulation which has been completed
        /// </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                if (Today == DateTime.MinValue)
                    return 0;

                TimeSpan fullSim = EndDate - StartDate;
                if (fullSim.Equals(TimeSpan.Zero))
                    return 1.0;
                else
                {
                    TimeSpan completedSpan = Today - StartDate;
                    return completedSpan.TotalDays / fullSim.TotalDays;
                }
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Today = StartDate;
        }

        /// <summary>An event handler to signal start of a simulation.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoCommence")]
        private void OnDoCommence(object sender, CommenceArgs e)
        {
            Today = StartDate;

            if (DoInitialSummary != null)
                DoInitialSummary.Invoke(this, args);

            if (StartOfSimulation != null)
                StartOfSimulation.Invoke(this, args);

            if (CLEMInitialiseResource != null)
                CLEMInitialiseResource.Invoke(this, args);

            if (CLEMInitialiseActivity != null)
                CLEMInitialiseActivity.Invoke(this, args);

            if (CLEMValidate != null)
                CLEMValidate.Invoke(this, args);

            if (FinalInitialise != null)
                FinalInitialise.Invoke(this, args);

            while (Today <= EndDate && (e.CancelToken == null || !e.CancelToken.IsCancellationRequested))
            {
                if (DoWeather != null)
                    DoWeather.Invoke(this, args);

                if (DoDailyInitialisation != null)
                    DoDailyInitialisation.Invoke(this, args);

                if (StartOfDay != null)
                    StartOfDay.Invoke(this, args);

                if (Today.Day == 1 && StartOfMonth != null)
                    StartOfMonth.Invoke(this, args);

                if (Today.DayOfYear == 1 && StartOfYear != null)
                    StartOfYear.Invoke(this, args);

                if (Today.DayOfWeek == DayOfWeek.Sunday && StartOfWeek != null)
                    StartOfWeek.Invoke(this, args);

                if (DoManagement != null)
                    DoManagement.Invoke(this, args);

                if (DoPestDiseaseDamage != null)
                    DoPestDiseaseDamage.Invoke(this, args);

                if (DoEnergyArbitration != null)
                    DoEnergyArbitration.Invoke(this, args);

                if (DoSoilWaterMovement != null)
                    DoSoilWaterMovement.Invoke(this, args);

                if (DoSoilTemperature != null)
                    DoSoilTemperature.Invoke(this, args);

                if (DoSoilOrganicMatter != null)
                    DoSoilOrganicMatter.Invoke(this, args);

                if (DoSurfaceOrganicMatterDecomposition != null)
                    DoSurfaceOrganicMatterDecomposition.Invoke(this, args);

                if (DoUpdateWaterDemand != null)
                    DoUpdateWaterDemand.Invoke(this, args);

                if (DoWaterArbitration != null)
                    DoWaterArbitration.Invoke(this, args);

                if (PrePhenology != null)
                    PrePhenology.Invoke(this, args);

                if (DoPhenology != null)
                    DoPhenology.Invoke(this, args);

                if (DoPotentialPlantGrowth != null)
                    DoPotentialPlantGrowth.Invoke(this, args);

                if (DoPotentialPlantPartioning != null)
                    DoPotentialPlantPartioning.Invoke(this, args);

                if (DoNutrientArbitration != null)
                    DoNutrientArbitration.Invoke(this, args);

                if (DoActualPlantPartioning != null)
                    DoActualPlantPartioning.Invoke(this, args);

                if (DoActualPlantGrowth != null)
                    DoActualPlantGrowth.Invoke(this, args);

                if (DoStock != null)
                    DoStock.Invoke(this, args);

                if (DoLifecycle != null)
                    DoLifecycle.Invoke(this, args);

                if (DoUpdate != null)
                    DoUpdate.Invoke(this, args);

                if (DoManagementCalculations != null)
                    DoManagementCalculations.Invoke(this, args);

                if (DoReportCalculations != null)
                    DoReportCalculations.Invoke(this, args);

                if (Today.DayOfWeek == DayOfWeek.Saturday && EndOfWeek != null)
                    EndOfWeek.Invoke(this, args);

                if (Today.Day == 31 && Today.Month == 12 && EndOfYear != null)
                    EndOfYear.Invoke(this, args);

                if (Today.AddDays(1).Day == 1 && EndOfMonth != null) // is tomorrow the start of a new month?
                {
                    // CLEM events performed before APSIM EndOfMonth
                    if (CLEMStartOfTimeStep != null)
                        CLEMStartOfTimeStep.Invoke(this, args);
                    if (CLEMUpdateLabourAvailability != null)
                        CLEMUpdateLabourAvailability.Invoke(this, args);
                    if (CLEMUpdatePasture != null)
                        CLEMUpdatePasture.Invoke(this, args);
                    if (CLEMPastureReady != null)
                        CLEMPastureReady.Invoke(this, args);
                    if (CLEMDoCutAndCarry != null)
                        CLEMDoCutAndCarry.Invoke(this, args);
                    if (CLEMAnimalBreeding != null)
                        CLEMAnimalBreeding.Invoke(this, args);
                    if (CLEMAnimalMilkProduction != null)
                        CLEMAnimalMilkProduction.Invoke(this, args);
                    if (CLEMPotentialIntake != null)
                        CLEMPotentialIntake.Invoke(this, args);
                    if (CLEMGetResourcesRequired != null)
                        CLEMGetResourcesRequired.Invoke(this, args);
                    if (CLEMAnimalWeightGain != null)
                        CLEMAnimalWeightGain.Invoke(this, args);
                    if (CLEMCalculateManure != null)
                        CLEMCalculateManure.Invoke(this, args);
                    if (CLEMCollectManure != null)
                        CLEMCollectManure.Invoke(this, args);
                    if (CLEMAnimalDeath != null)
                        CLEMAnimalDeath.Invoke(this, args);
                    if (CLEMAnimalMilking != null)
                        CLEMAnimalMilking.Invoke(this, args);
                    if (CLEMCalculateEcologicalState != null)
                        CLEMCalculateEcologicalState.Invoke(this, args);
                    if (CLEMAnimalManage != null)
                        CLEMAnimalManage.Invoke(this, args);
                    if (CLEMAnimalStock != null)
                        CLEMAnimalStock.Invoke(this, args);
                    if (CLEMAnimalSell != null)
                        CLEMAnimalSell.Invoke(this, args);
                    if (CLEMDetachPasture != null)
                        CLEMDetachPasture.Invoke(this, args);
                    if (CLEMHerdSummary != null)
                        CLEMHerdSummary.Invoke(this, args);
                    if (CLEMAgeResources != null)
                        CLEMAgeResources.Invoke(this, args);
                    if (CLEMAnimalBuy != null)
                        CLEMAnimalBuy.Invoke(this, args);
                    if (CLEMEndOfTimeStep != null)
                        CLEMEndOfTimeStep.Invoke(this, args);
                    EndOfMonth.Invoke(this, args);
                }

                if (EndOfDay != null)
                    EndOfDay.Invoke(this, args);

                if (DoReport != null)
                    DoReport.Invoke(this, args);

                Today = Today.AddDays(1);
            }
            Today = EndDate;

            if (EndOfSimulation != null)
                EndOfSimulation.Invoke(this, args);

            Summary?.WriteMessage(this, "Simulation terminated normally");
        }

        /// <summary>
        /// Create clock documentation.
        /// </summary>
        /// <param name="tags">Documentation tags.</param>
        /// <param name="headingLevel">The heading level.</param>
        /// <param name="indent">The indent level.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            tags.Add(new AutoDocumentation.Heading("Clock", headingLevel));
            AutoDocumentation.DocumentModelSummary(this, tags, headingLevel+1, indent, false);
            DocumentEventOrder(tags, headingLevel + 1, indent);
        }

        /// <summary>
        /// Document the event order. Cannot assume the order based on the order
        /// of the event declarations.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="headingLevel">The heading level.</param>
        /// <param name="indent">The indent level.</param>
        private void DocumentEventOrder(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        { 
            // The strategy here is to create an instance of clock and call it's
            // timestep method (OnDoCommence) for two days. WE will subscribe to
            // all the clock methods via a method (Handler) in an instance of an 
            // EventHanderClass. This method will then sort out what is a event 
            // from the daily time step, what is an event from before the timestep
            // and post timestep events.
            var clock = new Clock();
            var methodInfo = typeof(EventHandlerClass).GetMethod("Handler", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var preTimestepEvents = new List<string>();
            var timestepEvents = new List<string>();
            var postTimestepEvents = new List<string>();
            foreach (var eventMember in clock.GetType().GetEvents())
            {
                var handlerInstance = new EventHandlerClass()
                {
                    EventName = eventMember.Name,
                    PreTimestepEvents = preTimestepEvents,
                    TimestepEvents = timestepEvents,
                    PostTimestepEvents = postTimestepEvents
                };
                var handler =
                     Delegate.CreateDelegate(eventMember.EventHandlerType,
                     handlerInstance,                        
                     methodInfo);
                eventMember.AddEventHandler(
                    clock,
                    handler);
            }

            clock.Start = new DateTime(1900, 1, 1);
            clock.End = new DateTime(1900, 1, 2);

            var commenceMethod = clock.GetType().GetMethod("OnDoCommence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            commenceMethod.Invoke(clock, new object[] { clock, new CommenceArgs() });

            var table = new DataTable();
            tags.Add(new AutoDocumentation.Heading("Pre-timestep events (in order)", headingLevel));
            DataTableUtilities.AddColumn(table, "Events", preTimestepEvents.ToArray());
            tags.Add(new AutoDocumentation.Table(table, indent));

            var table2 = new DataTable();
            tags.Add(new AutoDocumentation.Heading("Timestep events (in order)", headingLevel));
            DataTableUtilities.AddColumn(table2, "Events", timestepEvents.ToArray());
            tags.Add(new AutoDocumentation.Table(table2, indent));

            var table3 = new DataTable();
            tags.Add(new AutoDocumentation.Heading("Post-timestep events (in order)", headingLevel));
            DataTableUtilities.AddColumn(table3, "Events", postTimestepEvents.ToArray());
            tags.Add(new AutoDocumentation.Table(table3, indent));
        }

        /// <summary>
        /// A helper class for determining which of the clock events are timestep
        /// events and what come before or after the timestep.
        /// </summary>
        private class EventHandlerClass
        {
            public string EventName { get; set; }
            public List<string> PreTimestepEvents { get; set; }
            public List<string> TimestepEvents { get; set; }
            public List<string> PostTimestepEvents { get; set; }

            public void Handler(object sender, EventArgs e)
            {
                if (EventName == "EndOfSimulation")
                    PostTimestepEvents.Add(EventName);
                else if (PreTimestepEvents.Contains(EventName))
                {
                    PreTimestepEvents.Remove(EventName);
                    TimestepEvents.Add(EventName);
                }
                else
                    PreTimestepEvents.Add(EventName);
            }
        }
    }
}