using Models.Core;
using System;
using System.Collections.Generic;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Data structure that contains information for a specific planting of scrum
    /// </summary>
    [Serializable]
    public class ScrumManagementInstance : Model
    {
        /// <summary>Establishemnt Date</summary>
        public string CropName { get; set; }

        /// <summary>Establishemnt Date</summary>
        public DateTime EstablishDate { get; set; }

        /// <summary>Establishment Stage</summary>
        public string EstablishStage { get; set; }
        
        /// <summary>Planting depth (mm)</summary>
        public double PlantingDepth { get; set; }

        /// <summary>Harvest Date</summary>
        public Nullable <DateTime> HarvestDate { get; set; }

        /// <summary>Harvest Tt (oCd establishment to harvest)</summary>
        public double TtEstabToHarv { get; set; }

        /// <summary>Planting Stage</summary>
        public string HarvestStage { get; set; }
        
        /// <summary>Expected Yield (g FW/m2)</summary>
        public double ExpectedYield { get; set; }

        /// <summary>Field loss (i.e the proportion of expected yield that is left in the field 
        /// because of diseaese, poor quality or lack of market)</summary>
        public double FieldLoss { get; set; }

        /// <summary>Residue Removal (i.e the proportion of residues that are removed from the field 
        /// by bailing or some other means)</summary>
        public double ResidueRemoval { get; set; }

        /// <summary>Residue incorporation (i.e the proportion of residues that are incorporated by cultivation  
        /// at or soon after harvest)</summary>
        public double ResidueIncorporation { get; set; }

        /// <summary>Residue incorporation depth (i.e the depth residues are incorporated to by cultivation  
        /// at or soon after harvest)</summary>
        public double ResidueIncorporationDepth { get; set; }

        /// <summary>Can fertiliser be applied to this crop.  
        /// Note, this is a flag for managers to use, Scrum does not calculate its own fertliser applications</summary>
        public bool IsFertilised { get; set; }

        /// <summary>First Date for Fert application to this crop.  
        /// Note, this is a flag for managers to use, Scrum does not calculate its own fertliser applications</summary>
        public Nullable<DateTime> FirstFertDate { get; set; }

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public ScrumManagementInstance(){ }

        /// <summary>
        /// Management class constructor
        /// </summary>
        public ScrumManagementInstance(string cropName, DateTime establishmentDate, string establishStage, double plantingDepth, string harvestStage, double expectedYield,
             double fieldLoss, double residueRemoval, double residueIncorporation, double residueIncorporatinDepth, Nullable<DateTime> harvestDate = null, double harvestTt = Double.NaN, 
              bool isFertilised = true, Nullable<DateTime> firstFertDate = null)
        {
            CropName = cropName;
            EstablishDate = establishmentDate;
            EstablishStage = establishStage;
            PlantingDepth = plantingDepth;
            HarvestStage = harvestStage;
            ExpectedYield = expectedYield;
            FieldLoss = fieldLoss;
            ResidueRemoval = residueRemoval;
            ResidueIncorporation = residueIncorporation;
            ResidueIncorporationDepth = residueIncorporatinDepth;
            HarvestDate = harvestDate;
            TtEstabToHarv = harvestTt;
            IsFertilised = isFertilised;
            FirstFertDate = (FirstFertDate == null) ? establishmentDate : firstFertDate;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cropParams"></param>
        /// <param name="today"></param>
        public ScrumManagementInstance(Dictionary<string, string> cropParams, DateTime today)
        {
            CropName = cropParams["CropName"];
            DateTime testEstablishDate = DateTime.Parse(cropParams["EstablishDate"]+"-"+today.Year) ;
            if (testEstablishDate > today)
            {
                EstablishDate = testEstablishDate;
            }
            else
            {
                EstablishDate = DateTime.Parse(cropParams["EstablishDate"] + "-" + (today.Year + 1));
            }
            EstablishStage = cropParams["EstablishStage"];
            PlantingDepth = Double.Parse(cropParams["PlantingDepth"]);
            HarvestStage = cropParams["HarvestStage"];
            ExpectedYield = Double.Parse(cropParams["ExpectedYield"]);
            if (cropParams["HarvestDate"] != "")
            {
                DateTime testHarvestDate = DateTime.Parse(cropParams["HarvestDate"] + "-" + today.Year);
                if (testHarvestDate > EstablishDate)
                {
                    HarvestDate = testHarvestDate;
                }
                else
                {
                    HarvestDate = DateTime.Parse(cropParams["HarvestDate"] + "-" + (today.Year + 1));
                    if (HarvestDate <= EstablishDate)
                        HarvestDate = DateTime.Parse(cropParams["HarvestDate"] + "-" + (today.Year + 2));
                }
            }
            if (cropParams["TtEstabToHarv"] != "")
            {
                TtEstabToHarv = Double.Parse(cropParams["TtEstabToHarv"]);
            }
            FieldLoss = Double.Parse(cropParams["FieldLoss"]);
            ResidueRemoval = Double.Parse(cropParams["ResidueRemoval"]);
            ResidueIncorporation = Double.Parse(cropParams["ResidueIncorporation"]);
            ResidueIncorporationDepth = Double.Parse(cropParams["ResidueIncorporationDepth"]);
            try
            { IsFertilised = bool.Parse(cropParams["IsFertilised"]); }
            catch 
            { IsFertilised = true; }
            try
            { FirstFertDate = DateTime.Parse(cropParams["FirstFertDate"] + "-" + today.Year); }
            catch
            { FirstFertDate = EstablishDate; }
        }
    }
}
