namespace Models.PMF
{
    using Models.Core;
    using System;

    /// <summary>
    /// Data structure that contains information for a specific planting of scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Referee))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumManagement
    {
        /// <summary>Establishemnt Date</summary>
        [Description("Establishment Date")]
        public DateTime EstablishmentDate { get; set; }

        /// <summary>Establishment Stage</summary>
        [Description("Establishment Stage")]
        [Display(Type = DisplayType.CropStageName)]
        public string EstablishStage { get; set; }

        /// <summary>Planting Date</summary>
        [Description("Harvest Date")]
        public DateTime HarvestDate { get; set; }

        /// <summary>Planting Stage</summary>
        [Description("Planting Stage")]
        [Display(Type = DisplayType.CropStageName)]
        public string HarvestStage { get; set; }

        /// <summary>Expected Yield</summary>
        [Description("Expected Yield")]
        public double ExpectedYield { get; set; }

    }

}
