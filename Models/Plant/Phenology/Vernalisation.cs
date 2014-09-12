using Models.Core;
using Models.PMF.Functions;
using System;

namespace Models.PMF.Phen
{

    [Serializable]
    public class Vernalisation : Model
    {
        [Link]
        Phenology Phenology = null;

        [Link]
        AirTemperatureFunction VDModel = null;

        [Link]
        WeatherFile Weather = null;

        public string StartStage = "";
        public string EndStage = "";
        
        private double CumulativeVD = 0;

        /// <summary>
        /// Trap the NewMet event.
        /// </summary>
        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable()
        {
            if (Phenology.Between(StartStage, EndStage))
                DoVernalisation(Weather.MetData.Maxt, Weather.MetData.Mint);
        }

        /// <summary>
        /// Initialise everything
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            CumulativeVD = 0;
        }

        /// <summary>
        /// Do our vernalisation
        /// </summary>
        public void DoVernalisation(double Maxt, double Mint)
        {
            CumulativeVD += VDModel.Value;
        }


    }
}