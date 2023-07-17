using APSIM.Shared.Utilities;
using CommandLine;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using static Models.PMF.Scrum.ScrumCrop;

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
        [Separator("Choose one of your specified scrum crops to plant")]
        [Description("Crop to plant")]
        [Display(Type = DisplayType.SCRUMcropName)]
        public string CropName { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Description("Establishment Date")]
        public DateTime EstablishDate { get; set; }

        /// <summary>Establishment Stage</summary>
        [Description("Establishment Stage")]
        [Display(Type = DisplayType.ScrumEstablishStages)]
        public string EstablishStage { get; set; }
        
        /// <summary>Planting depth (mm)</summary>
        [Description("Planting depth (mm)")]
        public double PlantingDepth { get; set; }

        /// <summary>Harvest Date</summary>
        [Separator("Scrum needs to have a valid harvest date or Tt duration (from establishment to harvest) specified")]
        [Description("Harvest Date")]
        public Nullable <DateTime> HarvestDate { get; set; }

        /// <summary>Harvest Tt (oCd establishment to harvest)</summary>
        [Description("TT from Establish to Harvest (oCd")]
        public double TtEstabToHarv { get; set; }

        /// <summary>Planting Stage</summary>
        [Description("Harvest Stage")]
        [Display(Type = DisplayType.ScrumHarvestStages)]
        public string HarvestStage { get; set; }
        
        /// <summary>Expected Yield (g FW/m2)</summary>
        [Separator("Specify an appropriate potential yeild for the location, sowing date and assumed genotype \nScrum will reduce yield below potential if water or N stress are predicted")]
        [Description("Expected Yield (t/Ha)")]
        public double ExpectedYield { get; set; }

        /// <summary>Field loss (i.e the proportion of expected yield that is left in the field 
        /// because of diseaese, poor quality or lack of market)</summary>
        [Separator("Specify percentaces of field loss and residue removal at harvest")]
        [Description("Field loss (0-1)")]
        public double FieldLoss { get; set; }

        /// <summary>Residue Removal (i.e the proportion of residues that are removed from the field 
        /// by bailing or some other means)</summary>
        [Description("Residue removal (0-1)")]
        public double ResidueRemoval { get; set; }

        [Link(Type =LinkType.Scoped)]
        private Clock clock = null;
        
        [Link(Type=LinkType.Ancestor)]
        private Zone zone = null;

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        public Plant scrum = null;

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public ScrumManagement(){ }

        /// <summary>
        /// Management class constructor
        /// </summary>
        public ScrumManagement(string cropName, DateTime establishmentDate, string establishStage, double plantingDepth, string harvestStage, double expectedYield,
             Nullable<DateTime> harvestDate = null, double harvestTt = Double.NaN, double fieldLoss = 0, double residueRemoval = 0)
        {
            CropName = cropName;
            EstablishDate = establishmentDate;
            EstablishStage = establishStage;
            PlantingDepth = plantingDepth;
            HarvestStage = harvestStage;
            if (expectedYield == 0.0)
                throw new Exception(this.Name + "must have a yield > 0 set for the scrum crop to grow");
            ExpectedYield = expectedYield;
            if ((harvestDate == null) && (Double.IsNaN(harvestTt)))
                throw new Exception("Scrum requires a valid harvest date or harvest Tt to be specified");
            else
            {
                HarvestDate = harvestDate;
                TtEstabToHarv = harvestTt;
            }
            FieldLoss = fieldLoss;
            ResidueRemoval = residueRemoval;    
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
