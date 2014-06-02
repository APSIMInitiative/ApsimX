using System;
using System.Xml.Serialization;
using Models.Core;

namespace Models
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [AllowDropOn("Simulation")]
    public class Clock : Model
    {
        private EventArgs args = new EventArgs();

        // Links
        [Link]
        private ISummary Summary = null;

        [Description("The start date of the simulation")]
        public DateTime StartDate { get; set; }

        [Description("The end date of the simulation")]
        public DateTime EndDate { get; set; }

        // Public events that we're going to publish.
        public event EventHandler DoWeather;
        public event EventHandler DoDailyInitialisation;
        public event EventHandler DoManagement;
        public event EventHandler DoCanopyEnergyBalance;
        public event EventHandler DoSoilWaterMovement;
        public event EventHandler DoSurfaceOrganicMatterDecomposition;
        public event EventHandler DoSoilArbitration;
        public event EventHandler DoCanopy;
        public event EventHandler DoPlantGrowth;
        public event EventHandler DoUpdate;
        public event EventHandler DoManagementCalculations;
        public event EventHandler DoReport;

        // Public properties available to other models.
        [XmlIgnore]
        public DateTime Today { get; private set; }

        /// <summary>
        /// An event handler to allow us to initialise ourselves.
        /// </summary>
        public override void OnSimulationCommencing()
        {
            Today = StartDate;
        }

        /// <summary>
        /// An event handler to signal start of a simulation.
        /// </summary>
        [EventSubscribe("Commenced")]
        private void OnCommenced(object sender, EventArgs e)
        {
            System.ComponentModel.BackgroundWorker bw = sender as System.ComponentModel.BackgroundWorker;

            while (Today <= EndDate)
            {
                // If this is being run on a background worker thread then check for cancellation
                if (bw != null && bw.CancellationPending)
                {
                    Summary.WriteMessage(FullPath, "Simulation cancelled");
                    return;
                }
                

                if (DoWeather != null)
                    DoWeather.Invoke(this, args);

                if (DoDailyInitialisation != null)
                    DoDailyInitialisation.Invoke(this, args);

                if (DoManagement != null)
                    DoManagement.Invoke(this, args);

                if (DoCanopyEnergyBalance != null)
                    DoCanopyEnergyBalance.Invoke(this, args);

                if (DoSoilWaterMovement != null)
                    DoSoilWaterMovement.Invoke(this, args);

                if (DoSurfaceOrganicMatterDecomposition != null)
                    DoSurfaceOrganicMatterDecomposition.Invoke(this, args);

                if (DoSoilArbitration != null)
                    DoSoilArbitration.Invoke(this, args);

                if (DoCanopy != null)
                    DoCanopy.Invoke(this, args);

                if (DoPlantGrowth != null)
                    DoPlantGrowth.Invoke(this, args);

                if (DoUpdate != null)
                    DoUpdate.Invoke(this, args);

                if (DoManagementCalculations != null)
                    DoManagementCalculations.Invoke(this, args);

                if (DoReport != null)
                    DoReport.Invoke(this, args);

                Today = Today.AddDays(1);
            }


            Summary.WriteMessage(FullPath, "Simulation terminated normally");
        }
    }
}