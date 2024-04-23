using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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
        public ScrumManagementInstance(string cropName, DateTime establishDate, string establishStage, string harvestStage, double expectedYield, 
                                       Nullable<DateTime> harvestDate = null, double ttEstabToHarv = Double.NaN, double plantingDepth = 15, 
                                       double fieldLoss = 0, double residueRemoval = 0, double residueIncorporation = 1, double residueIncorporationDepth = 150,
                                       bool isFertilised = true, Nullable<DateTime> firstFertDate = null)
        {
            if (((harvestDate == null)||(harvestDate < establishDate)) && (Double.IsNaN(ttEstabToHarv) || (ttEstabToHarv == 0)))
                throw new Exception("A valid harvestDate OR a non-zero ttEstabToHarv must be provided when inititialising a ScrumManagementInstance");
            CropName = cropName;
            EstablishDate = establishDate;
            EstablishStage = establishStage;
            PlantingDepth = plantingDepth;
            HarvestStage = harvestStage;
            ExpectedYield = expectedYield;
            FieldLoss = fieldLoss;
            ResidueRemoval = residueRemoval;
            ResidueIncorporation = residueIncorporation;
            ResidueIncorporationDepth = residueIncorporationDepth;
            HarvestDate = harvestDate;
            TtEstabToHarv = ttEstabToHarv;
            IsFertilised = isFertilised;
            FirstFertDate = (FirstFertDate == null) ? establishDate : firstFertDate;
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
            else if (cropParams["TtEstabToHarv"] != "")
            {
                TtEstabToHarv = Double.Parse(cropParams["TtEstabToHarv"]);
            }
            else 
            { throw new Exception("A valid harvest date OR Tt from establish to harvest must be provided when inititialising a ScrumManagementInstance"); }
            try
            { FieldLoss = Double.Parse(cropParams["FieldLoss"]); }
            catch 
            { FieldLoss = 0; }
            try
            { ResidueRemoval = Double.Parse(cropParams["ResidueRemoval"]); }
            catch 
            { ResidueRemoval = 0; }
            try
            { ResidueIncorporation = Double.Parse(cropParams["ResidueIncorporation"]); }
            catch
            { ResidueIncorporation= 1; }
            try
            { ResidueIncorporationDepth = Double.Parse(cropParams["ResidueIncorporationDepth"]); }
            catch
            { ResidueIncorporationDepth = 150; }
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
