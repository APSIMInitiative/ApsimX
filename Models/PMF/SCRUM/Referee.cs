using Models.Climate;
using Models.Core;
using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
namespace Models.PMF.Scrum
{
    /// <summary>
    /// This model derives SCRUM parameters from basic user information and sets the correct values in the model when it runs.
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Referee : Model
    {
        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        public Plant scrum = null;

        [Link]
        private Clock clock = null;

        [Link]
        private CoefficientCalculator coeffCalc = null;

        private ScrumCrop currentCrop = null;
        private ScrumManagement currentManagement = null;

        //[Link(Type = LinkType.Child)]
        //private ScrumCrop refsCrop = null;
        /// <summary>List of the crops that may be planted.</summary>
        [JsonIgnore]
        public ScrumCrop[] Crops { get; private set; }


        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void SetScrumRunning(ScrumManagement management)
        {
            this.currentCrop = Crops.FirstOrDefault(c => c.Name == management.CropName); ;
            this.currentManagement = management;

            string cropName = this.currentCrop.Name;
            double depth = 0;
            double maxCover = this.currentCrop.Acover;
            double population = 1.0;
            double rowWidth = 0.0;


            Cultivar crop = coeffCalc.Values(currentCrop, ref currentManagement);
            scrum.Sow(cropName, population, depth, rowWidth, maxCover: maxCover, cultivarOverwrites: crop);
            scrum.Phenology.Emerged = true;
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (clock.Today == currentManagement.HarvestDate)
            {
                scrum.Harvest();
                scrum.EndCrop();
            }
        }


       /* private double CalcCropTt()
        {
            double cropTt = 0;
            for (DateTime d = currentManagement.EstablishmentDate; d <= currentManagement.HarvestDate; d = d.AddDays(1))
            {
                DailyMetDataFromFile todaysWeather = weather.GetMetData(d);
                double dailyTt = Math.Max(0, (todaysWeather.MinT + todaysWeather.MaxT) / 2 - currentCrop.BaseT);
                cropTt += dailyTt;
            }

            return cropTt;
        }*/

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<ScrumCrop> crops = new List<ScrumCrop>();
            foreach (ScrumCrop crop in this.FindAllChildren<ScrumCrop>())
                crops.Add(crop);

            Crops = crops.ToArray();
        }
    }
}
