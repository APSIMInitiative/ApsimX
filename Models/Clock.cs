using System;
using System.Xml.Serialization;
using Models.Core;

namespace Models
{
    /// <summary>
    /// The clock model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Clock : Model
    {
        /// <summary>The arguments</summary>
        private EventArgs args = new EventArgs();

        // Links
        /// <summary>The summary</summary>
        [Link]
        private ISummary Summary = null;

        /// <summary>Gets or sets the start date.</summary>
        /// <value>The start date.</value>
        [Summary]
        [Description("The start date of the simulation")]
        public DateTime StartDate { get; set; }

        /// <summary>Gets or sets the end date.</summary>
        /// <value>The end date.</value>
        [Summary]
        [Description("The end date of the simulation")]
        public DateTime EndDate { get; set; }

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

        /// <summary>Occurs when [do weather].</summary>
        public event EventHandler DoWeather;
        /// <summary>Occurs when [do daily initialisation].</summary>
        public event EventHandler DoDailyInitialisation;
        /// <summary>Occurs when [do initial summary].</summary>
        public event EventHandler DoInitialSummary;
        /// <summary>Occurs when [do management].</summary>
        public event EventHandler DoManagement;
        /// <summary>Occurs when [do energy arbitration].</summary>
        public event EventHandler DoEnergyArbitration;                                //MicroClimate
        /// <summary>Occurs when [do soil water movement].</summary>
        public event EventHandler DoSoilWaterMovement;                                //Soil module
        //DoSoilTemperature will be here
        //DoSoilNutrientDynamics will be here
        /// <summary>Occurs when [do soil organic matter].</summary>
        public event EventHandler DoSoilOrganicMatter;                                 //SurfaceOM
        /// <summary>Occurs when [do surface organic matter decomposition].</summary>
        public event EventHandler DoSurfaceOrganicMatterDecomposition;                 //SurfaceOM
        /// <summary>Occurs when [do water arbitration].</summary>
        public event EventHandler DoWaterArbitration;                                  //Arbitrator
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
        /// <summary>Occurs when [do plant growth].</summary>
        public event EventHandler DoPlantGrowth;                       //This will be removed when comms are better sorted  do not use  MicroClimate only
        /// <summary>Occurs when [do update].</summary>
        public event EventHandler DoUpdate;
        /// <summary>Occurs when [do management calculations].</summary>
        public event EventHandler DoManagementCalculations;
        /// <summary>Occurs when [do report calculations].</summary>
        public event EventHandler DoReportCalculations;
        /// <summary>Occurs when [do report].</summary>
        public event EventHandler DoReport;

        // Public properties available to other models.
        /// <summary>Gets the today.</summary>
        /// <value>The today.</value>
        [XmlIgnore]
        public DateTime Today { get; private set; }

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
        private void OnDoCommence(object sender, EventArgs e)
        {
            System.ComponentModel.BackgroundWorker bw = sender as System.ComponentModel.BackgroundWorker;

            if (DoInitialSummary != null)
                DoInitialSummary.Invoke(this, args);

            if (StartOfSimulation != null)
                StartOfSimulation.Invoke(this, args);

            while (Today <= EndDate)
            {
                // If this is being run on a background worker thread then check for cancellation
                if (bw != null && bw.CancellationPending)
                {
                    Summary.WriteMessage(this, "Simulation cancelled");
                    return;
                }

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

                if (Today.DayOfWeek == DayOfWeek.Saturday && EndOfWeek != null)
                    EndOfWeek.Invoke(this, args);

                if (DoManagement != null)
                    DoManagement.Invoke(this, args);

                if (DoEnergyArbitration != null)
                    DoEnergyArbitration.Invoke(this, args);

                if (DoSoilWaterMovement != null)
                    DoSoilWaterMovement.Invoke(this, args);

                if (DoSoilOrganicMatter != null)
                    DoSoilOrganicMatter.Invoke(this, args);

                if (DoSurfaceOrganicMatterDecomposition != null)
                    DoSurfaceOrganicMatterDecomposition.Invoke(this, args);
                if (Today.DayOfYear == 16)
                { }
                if (DoWaterArbitration != null)
                    DoWaterArbitration.Invoke(this, args);

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

                if (DoPlantGrowth != null)
                    DoPlantGrowth.Invoke(this, args);

                if (DoUpdate != null)
                    DoUpdate.Invoke(this, args);

                if (DoManagementCalculations != null)
                    DoManagementCalculations.Invoke(this, args);

                if (DoReportCalculations != null)
                    DoReportCalculations.Invoke(this, args);

                if (Today == EndDate && EndOfSimulation != null)
                    EndOfSimulation.Invoke(this, args);

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

            Summary.WriteMessage(this, "Simulation terminated normally");
        }
    }
}