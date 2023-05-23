using Models.Core;
using System;
namespace Models.PMF.Scrum
{
    /// <summary>
    /// Data structure that contains information for a specific planting of scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Referee))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumManagement : Model
    {
        /// <summary>Establishemnt Date</summary>
        [Description("Crop to plant")]
        public string CropName { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Description("Establishment Date")]
        public DateTime EstablishmentDate { get; set; }

        /// <summary>Establishment Stage</summary>
        [Description("Establishment Stage")]
        [Display(Type = DisplayType.CropStageName)]
        public string EstablishStage { get; set; }

        /// <summary>Harvest Date</summary>
        [Description("Harvest Date")]
        public Nullable <DateTime> HarvestDate { get; set; }

        /// <summary>Harvest Tt (oCd establishment to harvest)</summary>
        [Description("Harvest Tt (oCd to Harvest")]
        public double HarvestTt { get; set; }

        /// <summary>Planting Stage</summary>
        [Description("Planting Stage")]
        [Display(Type = DisplayType.CropStageName)]
        public string HarvestStage { get; set; }

        /// <summary>Expected Yield (g FW/m2)</summary>
        [Description("Expected Yield (t/Ha)")]
        public double ExpectedYield { get; set; }

        /// <summary>
        /// Management class constructor
        /// </summary>
        public ScrumManagement(string cropName, DateTime establishmentDate, string establishStage, string harvestStage, double expectedYield,
             Nullable<DateTime> harvestDate = null, double harvestTt = Double.NaN)
        {
            if ((harvestDate == null) && (Double.IsNaN(harvestTt)))
                throw new Exception("Scrum requires a valid harvest date or harvest Tt to be specified");
            CropName = cropName;
            EstablishmentDate = establishmentDate;
            EstablishStage = establishStage;
            HarvestDate = harvestDate;
            HarvestTt = harvestTt;
            HarvestStage = harvestStage;
            ExpectedYield = expectedYield;
        }
    }
}
