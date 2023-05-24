using APSIM.Shared.Utilities;
using CommandLine;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
using System;
using System.Linq;

namespace Models.PMF.Scrum
{
    /// <summary>
    /// Data structure that contains information for a specific planting of scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Manager))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumManagement : Model
    {
        /// <summary>Establishemnt Date</summary>
        [Description("Crop to plant")]
        [Display(Type = DisplayType.SCRUMcropName)]
        public string CropName { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Description("Establishment Date")]
        public DateTime EstablishDate { get; set; }

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
        [Description("Harvest Stage")]
        [Display(Type = DisplayType.CropStageName)]
        public string HarvestStage { get; set; }

        /// <summary>Expected Yield (g FW/m2)</summary>
        [Description("Expected Yield (t/Ha)")]
        public double ExpectedYield { get; set; }

        [Link(Type =LinkType.Scoped)]
        private Clock clock = null;
        
        [Link(Type=LinkType.Ancestor)]
        private Zone zone = null;

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public ScrumManagement()
        { }

        /// <summary>
        /// Management class constructor
        /// </summary>
        public ScrumManagement(string cropName, DateTime establishmentDate, string establishStage, string harvestStage, double expectedYield,
             Nullable<DateTime> harvestDate = null, double harvestTt = Double.NaN)
        {
            CropName = cropName;
            EstablishDate = establishmentDate;
            EstablishStage = establishStage;
            HarvestStage = harvestStage;
            if (expectedYield == 0.0)
                throw new Exception(this.Name + "must have a yield > 0 set for the scrum crop to grow");
            ExpectedYield = expectedYield;
            if ((harvestDate == null) && (Double.IsNaN(harvestTt)))
                throw new Exception("Scrum requires a valid harvest date or harvest Tt to be specified");
            else
            {
                HarvestDate = harvestDate;
                HarvestTt = harvestTt;
            }
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if ((zone != null)&&(clock!=null) )
            {
                if (clock.Today == EstablishDate)
                {
                    ScrumCrop currentCrop = zone.FindDescendant<ScrumCrop>(CropName);
                    currentCrop.Establish(this);
                }
            }
        }
    }
}
