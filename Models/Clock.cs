using System;
using Models.Core;
using Models.Core.Run;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models
{
    /// <summary>
    /// The clock model is responsible for controlling the daily timestep in APSIM. It
    /// keeps track of the simulation date and loops from the start date to the end
    /// date, publishing events that other models can subscribe to.
    /// </summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Clock : Model, IClock
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
                IWeather weather = this.FindInScope<IWeather>();
                if (weather != null)
                    return weather.StartDate;

                throw new Exception($"No start date provided in clock {this.FullPath} and no weather file could be found.");
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
                IWeather weather = this.FindInScope<IWeather>();
                if (weather != null)
                    return weather.EndDate;

                throw new Exception($"No end date provided in {this.FullPath}: and no weather file could be found.");
            }
            set
            {
                End = value;
            }
        }

        // Public events that we're going to publish.
        /// <summary>Occurs once at the start of the simulation.</summary>
        public event EventHandler StartOfSimulation;
        /// <summary>Occurs once at the start of the first day of the simulation.</summary>
        public event EventHandler StartOfFirstDay;
        /// <summary>Occurs at start of each day.</summary>
        public event EventHandler StartOfDay;
        /// <summary>Occurs at start of each month.</summary>
        public event EventHandler StartOfMonth;
        /// <summary>Occurs at start of each year.</summary>
        public event EventHandler StartOfYear;
        /// <summary>Occurs at start of each week.</summary>
        public event EventHandler StartOfWeek;
        /// <summary>Occurs at end of each day</summary>
        public event EventHandler EndOfDay;
        /// <summary>Occurs at end of each month.</summary>
        public event EventHandler EndOfMonth;
        /// <summary>Occurs at end of each year.</summary>
        public event EventHandler EndOfYear;
        /// <summary>Occurs at end of each week.</summary>
        public event EventHandler EndOfWeek;
        /// <summary>Occurs at end of simulation.</summary>
        public event EventHandler EndOfSimulation;
        /// <summary>Final Initialise event. Occurs once at start of simulation.</summary>
        public event EventHandler FinalInitialise;

        /// <summary>Occurs first each day to allow yesterdays values to be caught</summary>
        public event EventHandler DoCatchYesterday;
        /// <summary>Occurs each day to calculuate weather</summary>
        public event EventHandler DoWeather;
        /// <summary>Occurs each day to do daily updates to models</summary>
        public event EventHandler DoDailyInitialisation;
        /// <summary>Occurs each day to make the intial summary</summary>
        public event EventHandler DoInitialSummary;
        /// <summary>Occurs each day to do management actions and changes</summary>
        public event EventHandler DoManagement;
        /// <summary>Invoked to perform all fertiliser applications.</summary>
        public event EventHandler DoFertiliserApplications;
        /// <summary>Occurs to do Pest/Disease actions</summary>
        public event EventHandler DoPestDiseaseDamage;
        /// <summary>Occurs when the canopy energy balance needs to be calculated with MicroCLimate</summary>
        public event EventHandler DoEnergyArbitration;                                //MicroClimate
        /// <summary>Occurs each day to do water calculations such as irrigation, swim, water balance etc</summary>
        public event EventHandler DoSoilWaterMovement;                                //Soil module
        /// <summary>Occurs to tell soil erosion to perform its calculations.</summary>
        public event EventHandler DoSoilErosion;
        /// <summary>Occurs to perform soil temperature calculations to do solute processes.</summary>
        public event EventHandler DoSoilTemperature;
        /// <summary>Occurs each day</summary>
        public event EventHandler DoSolute;
        /// <summary>Occurs each day to perform daily calculations of organic soil matter</summary>
        public event EventHandler DoSurfaceOrganicMatterPotentialDecomposition;
        /// <summary>Occurs each day to perform daily calculations of organic soil matter</summary>
        public event EventHandler DoSoilOrganicMatter;                                 //SurfaceOM
        /// <summary>Occurs each day to do the daily residue decomposition</summary>
        public event EventHandler DoSurfaceOrganicMatterDecomposition;                 //SurfaceOM
        /// <summary>Occurs each day to do daily growth increment of total plant biomass</summary>
        public event EventHandler DoUpdateWaterDemand;
        /// <summary>Occurs each day to do water arbitration</summary>
        public event EventHandler DoWaterArbitration;                                  //Arbitrator
        /// <summary>Initiates water calculations for the Pasture model</summary>
        public event EventHandler DoPastureWater;
        /// <summary>Occurs between DoWaterArbitration and DoPhenology. Performs sorghum final leaf no calcs.</summary>
        public event EventHandler PrePhenology;
        /// <summary>Occurs each day to perform phenology</summary>
        public event EventHandler DoPhenology;                                         // Plant
        /// <summary>Occurs each day to do potential growth</summary>
        public event EventHandler DoPotentialPlantGrowth;                              //Refactor to DoWaterLimitedGrowth  Plant
        /// <summary>Occurs each day to do the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        public event EventHandler DoPotentialPlantPartioning;                          // PMF OrganArbitrator.
        /// <summary>Occurs each day to do nutrient arbitration</summary>
        public event EventHandler DoNutrientArbitration;                               //Arbitrator
        /// <summary>Occurs each day to do nutrient allocations</summary>
        public event EventHandler DoActualPlantPartioning;                             // PMF OrganArbitrator.
        /// <summary>Occurs each day to do nutrient allocations. Pasture growth</summary>
        public event EventHandler DoActualPlantGrowth;                                 //Refactor to DoNutirentLimitedGrowth Plant
        /// <summary>Occurs each day to finish partitioning</summary>
        public event EventHandler PartitioningComplete;
        /// <summary>Occurs near end of each day to do checks and finalising</summary>
        public event EventHandler DoUpdate;
        /// <summary>Occurs each day to process stock methods in GrazPlan Stock</summary>
        public event EventHandler DoStock;
        /// <summary>Occurs each day to process a Pest and Disease lifecycle object</summary>
        public event EventHandler DoLifecycle;
        /// <summary>Occurs each day after the simulation is done. Does managment calculations</summary>
        public event EventHandler DoManagementCalculations;
        /// <summary>Occurs after pasture growth and sends material to SOM</summary>
        public event EventHandler DoEndPasture;
        /// <summary>Occurs when [do report calculations].</summary>
        public event EventHandler DoReportCalculations;
        /// <summary>Occurs at end of each day</summary>
        public event EventHandler DoReport;

        /// <summary>
        /// Occurs each day when when dcaps performs its calculations. This must happen
        /// between DoPotentialPlantGrowth and DoPotentialPlantPartitioning.
        /// </summary>
        public event EventHandler DoDCAPST;


        // Public properties available to other models.
        /// <summary>Gets the today.</summary>
        /// <value>The today.</value>
        [JsonIgnore]
        public DateTime Today { get; private set; }

        /// <summary>
        /// Returns the current fraction of the overall simulation which has been completed
        /// </summary>
        [JsonIgnore]
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

        /// <summary>Is the current simulation date at end of month?</summary>
        public bool IsStartMonth => Today.Day == 1;

        /// <summary>Is the current simulation date at end of month?</summary>
        public bool IsStartYear => Today.DayOfYear == 1;

        /// <summary>Is the current simulation date at end of month?</summary>
        public bool IsEndMonth => Today.AddDays(1).Day == 1;

        /// <summary>Is the current simulation date at end of month?</summary>
        public bool IsEndYear => Today.AddDays(1).DayOfYear == 1;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Today = StartDate;
        }

        /// <summary>An event handler to signal start of a simulation.</summary>
        /// <param name="_">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoCommence")]
        private void OnDoCommence(object _, CommenceArgs e)
        {
            Today = StartDate;

            if (DoInitialSummary != null)
                DoInitialSummary.Invoke(this, args);

            if (StartOfSimulation != null)
                StartOfSimulation.Invoke(this, args);

            if (FinalInitialise != null)
                FinalInitialise.Invoke(this, args);

            if (StartOfFirstDay != null)
                StartOfFirstDay.Invoke(this, args);

            while (Today <= EndDate && (e.CancelToken == null || !e.CancelToken.IsCancellationRequested))
            {
                if (DoCatchYesterday != null)
                    DoCatchYesterday.Invoke(this, args);

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

                DoFertiliserApplications?.Invoke(this, args);

                if (DoPestDiseaseDamage != null)
                    DoPestDiseaseDamage.Invoke(this, args);

                if (DoEnergyArbitration != null)
                    DoEnergyArbitration.Invoke(this, args);

                DoSoilErosion?.Invoke(this, args);

                if (DoSoilWaterMovement != null)
                    DoSoilWaterMovement.Invoke(this, args);

                if (DoSoilTemperature != null)
                    DoSoilTemperature.Invoke(this, args);

                if (DoSolute != null)
                    DoSolute.Invoke(this, args);

                if (DoSurfaceOrganicMatterPotentialDecomposition != null)
                    DoSurfaceOrganicMatterPotentialDecomposition.Invoke(this, args);

                if (DoSoilOrganicMatter != null)
                    DoSoilOrganicMatter.Invoke(this, args);

                if (DoSurfaceOrganicMatterDecomposition != null)
                    DoSurfaceOrganicMatterDecomposition.Invoke(this, args);

                if (DoUpdateWaterDemand != null)
                    DoUpdateWaterDemand.Invoke(this, args);

                DoDCAPST?.Invoke(this, args);

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

                if (DoPastureWater != null)
                    DoPastureWater.Invoke(this, args);

                if (DoNutrientArbitration != null)
                    DoNutrientArbitration.Invoke(this, args);

                if (DoActualPlantPartioning != null)
                    DoActualPlantPartioning.Invoke(this, args);

                if (DoActualPlantGrowth != null)
                    DoActualPlantGrowth.Invoke(this, args);

                if (PartitioningComplete != null)
                    PartitioningComplete.Invoke(this, args);

                if (DoStock != null)
                    DoStock.Invoke(this, args);

                if (DoLifecycle != null)
                    DoLifecycle.Invoke(this, args);

                if (DoUpdate != null)
                    DoUpdate.Invoke(this, args);

                if (DoManagementCalculations != null)
                    DoManagementCalculations.Invoke(this, args);

                if (DoEndPasture != null)
                    DoEndPasture.Invoke(this, args);

                if (DoReportCalculations != null)
                    DoReportCalculations.Invoke(this, args);

                if (Today.DayOfWeek == DayOfWeek.Saturday && EndOfWeek != null)
                    EndOfWeek.Invoke(this, args);

                if (Today.Day == 31 && Today.Month == 12 && EndOfYear != null)
                    EndOfYear.Invoke(this, args);

                if (Today.AddDays(1).Day == 1 && EndOfMonth != null) // is tomorrow the start of a new month?
                    EndOfMonth.Invoke(this, args);

                if (EndOfDay != null)
                    EndOfDay.Invoke(this, args);

                if (DoReport != null)
                    DoReport.Invoke(this, args);

                Today = Today.AddDays(1);
            }
            Today = EndDate;

            if (EndOfSimulation != null)
                EndOfSimulation.Invoke(this, args);

            Summary?.WriteMessage(this, "Simulation terminated normally", MessageType.Information);
        }
    }
}