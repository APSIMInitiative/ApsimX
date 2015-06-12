using Models.Core;
using Models.PMF.Functions;
using System;
using Models.Interfaces;

namespace Models.PMF.Phen
{

    /// <summary>
    /// Vernalisation model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Vernalisation : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The vd model</summary>
        [Link]
        AirTemperatureFunction VDModel = null;

        /// <summary>The weather</summary>
        [Link]
        IWeather Weather = null;

        /// <summary>The start stage</summary>
        [Description("StartStage")]
        public string StartStage { get; set; }
        /// <summary>The end stage</summary>
        [Description("EndStage")]
        public string EndStage { get; set; }

        /// <summary>The cumulative vd</summary>
        private double CumulativeVD = 0;

        /// <summary>Trap the DoDailyInitialisation event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (Phenology.Between(StartStage, EndStage))
                DoVernalisation(Weather.MaxT, Weather.MinT);
        }

        /// <summary>Initialise everything</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CumulativeVD = 0;
        }

        /// <summary>Do our vernalisation</summary>
        /// <param name="Maxt">The maxt.</param>
        /// <param name="Mint">The mint.</param>
        public void DoVernalisation(double Maxt, double Mint)
        {
            CumulativeVD += VDModel.Value;
        }


    }
}