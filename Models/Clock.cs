using System;
using System.Xml.Serialization;
using Models.Core;

namespace Models
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(typeof(Simulation))]
    public class Clock : Model
    {
        private EventArgs args = new EventArgs();

        // Links
        [Link]
        private ISummary Summary = null;

        [Summary]
        [Description("The start date of the simulation")]
        public DateTime StartDate { get; set; }

        [Summary]
        [Description("The end date of the simulation")]
        public DateTime EndDate { get; set; }

        // Public events that we're going to publish.
        public event EventHandler StartOfDay;
        public event EventHandler StartOfMonth;
        public event EventHandler StartOfYear;
        public event EventHandler EndOfDay;
        public event EventHandler EndOfMonth;
        public event EventHandler EndOfYear;

        public event EventHandler DoWeather;
        public event EventHandler DoDailyInitialisation;
        public event EventHandler DoInitialSummary;
        public event EventHandler DoManagement;
        public event EventHandler DoEnergyArbitration;                                //MicroClimate
        public event EventHandler DoCanopy;                            //This will be removed when comms are better sorted  do not use  MicroClimate only
        public event EventHandler DoCanopyEnergyBalance;               //This will be removed when comms are better sorted  do not use  MicroClimate only
        public event EventHandler DoSoilWaterMovement;                                //Soil module
        //DoSoilTemperature will be here
        //DoSoilNutrientDynamics will be here
        public event EventHandler DoSoilOrganicMatter;                                 //SurfaceOM
        public event EventHandler DoSurfaceOrganicMatterDecomposition;                 //SurfaceOM
        public event EventHandler DoWaterArbitration;                                  //Arbitrator
        public event EventHandler DoPotentialPlantGrowth;                              //Refactor to DoWaterLimitedGrowth  Plant
        public event EventHandler DoNutrientArbitration;                               //Arbitrator
        public event EventHandler DoActualPlantGrowth;                                 //Refactor to DoNutirentLimitedGrowth Plant
        public event EventHandler DoPlantGrowth;                       //This will be removed when comms are better sorted  do not use  MicroClimate only
        public event EventHandler DoUpdate;                
        public event EventHandler DoManagementCalculations;
        public event EventHandler DoReport;

        // Public properties available to other models.
        [XmlIgnore]
        public DateTime Today { get; private set; }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Today = StartDate;
        }

        /// <summary>
        /// An event handler to signal start of a simulation.
        /// </summary>
        [EventSubscribe("DoCommence")]
        private void OnDoCommence(object sender, EventArgs e)
        {
            System.ComponentModel.BackgroundWorker bw = sender as System.ComponentModel.BackgroundWorker;

            if (DoInitialSummary != null)
                DoInitialSummary.Invoke(this, args);

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

                if (Today.Day == 1 && StartOfMonth != null)
                {
                    StartOfMonth.Invoke(this, args);
                }

                if (Today.DayOfYear == 1 && StartOfYear != null)
                {
                    StartOfYear.Invoke(this, args);
                }

                if (DoManagement != null)
                    DoManagement.Invoke(this, args);

                if (DoEnergyArbitration != null)
                    DoEnergyArbitration.Invoke(this, args);

                if (DoCanopy != null)
                    DoCanopy.Invoke(this, args);

                if (DoCanopyEnergyBalance != null)
                    DoCanopyEnergyBalance.Invoke(this, args);

                if (DoSoilWaterMovement != null)
                    DoSoilWaterMovement.Invoke(this, args);

                if (DoSoilOrganicMatter != null)
                    DoSoilOrganicMatter.Invoke(this, args);

                if (DoSurfaceOrganicMatterDecomposition != null)
                    DoSurfaceOrganicMatterDecomposition.Invoke(this, args);

                if (DoWaterArbitration != null)
                    DoWaterArbitration.Invoke(this, args);

                if (DoPotentialPlantGrowth != null)
                    DoPotentialPlantGrowth.Invoke(this, args);

                if (DoNutrientArbitration != null)
                    DoNutrientArbitration.Invoke(this, args);

                if (DoActualPlantGrowth != null)
                    DoActualPlantGrowth.Invoke(this, args);

                if (DoPlantGrowth != null)
                    DoPlantGrowth.Invoke(this, args);

                if (DoUpdate != null)
                    DoUpdate.Invoke(this, args);

                if (DoManagementCalculations != null)
                    DoManagementCalculations.Invoke(this, args);

                if (Today.AddDays(1).Day == 1 && EndOfMonth != null) // is tomorrow the start of a new month?
                {
                    EndOfMonth.Invoke(this, args);
                }

                if (Today.Day == 31 && Today.Month == 12 && EndOfYear != null)
                {
                    EndOfYear.Invoke(this, args);
                }

                if (DoReport != null)
                    DoReport.Invoke(this, args);

                Today = Today.AddDays(1);
            }


            Summary.WriteMessage(this, "Simulation terminated normally");
        }
    }
}