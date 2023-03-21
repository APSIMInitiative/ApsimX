namespace Models.PMF
{
   using Models.Core;
   using System;

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

        /// <summary>Planting Date</summary>
        [Description("Planting Date")]
        public DateTime PlantingDate { get; set; }

        /// <summary>Planting Date</summary>
        [Description("Harvest Date")]
        public DateTime HarvestDate { get; set; }

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void SetScrumRunning(DateTime plantingDate, DateTime harvestDate, string cultivar, double depth, double maxCover)
        {   
            double population = 1.0;
            double rowWidth = 0.0;
            Cultivar crop = new Cultivar();
            scrum.Sow(cultivar, population, depth, rowWidth, maxCover:maxCover, cultivarOverwrites:crop);
        }
    }
}
