using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Models.Core;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Data structure that contains management information for a specific instance of SCRUM
    /// </summary>
    [Serializable]
    public class ScrumManagementInstance : Model
    {
        /// <summary>Name of this crop instance.</summary>
        public string CropName { get; set; }

        /// <summary>Establishment date.</summary>
        public DateTime EstablishDate { get; set; }

        /// <summary>Establishment stage.</summary>
        public string EstablishStage { get; set; }
        
        /// <summary>Planting depth (mm).</summary>
        public double PlantingDepth { get; set; }

        /// <summary>Planting stage.</summary>
        public string HarvestStage { get; set; }

        /// <summary>Expected crop yield, fresh weight (t/ha)</summary>
        public double ExpectedYield { get; set; }

        /// <summary>Harvest date.</summary>
        public Nullable <DateTime> HarvestDate { get; set; }

        /// <summary>Thermal time sum from establishment to harvest (oCd).</summary>
        public double TtEstabToHarv { get; set; }

        /// <summary>Proportion of expected yield that is left in the field at harvest (0-1).</summary>
        public double FieldLoss { get; set; }

        /// <summary>Proportion of stover that is removed from the field at harvest (0-1).</summary>
        public double ResidueRemoval { get; set; }

        /// <summary>Proportion of residues that are incorporated into the soil by cultivation at harvest (0-1)</summary>
        public double ResidueIncorporation { get; set; }

        /// <summary>Depth down to which residues are incorporated into the soil by cultivation at harvest (mm).</summary>
        public double ResidueIncorporationDepth { get; set; }

        /// <summary>Flag whether fertiliser be applied to this crop (to be used by manager scripts).</summary>
        /// <remarks>Note, this is a flag for managers to use, SCRUM does not calculate its own fertiliser applications.</remarks>
        public bool IsFertilised { get; set; }

        /// <summary>First date apply fertiliser to this crop (to be used by manager scripts).</summary>
        /// <remarks>Note, this is a flag for managers to use, SCRUM does not calculate its own fertiliser applications.</remarks>
        public Nullable<DateTime> FirstFertDate { get; set; }

        /// <summary>Default constructor</summary>
        public ScrumManagementInstance(){ }

        /// <summary>Management class constructor</summary>
        /// /// <param name="cropName">Name of the crop</param>
        /// <param name="establishDate">Date to establish the crop</param>
        /// <param name="establishStage">Phenology stage at establishment</param>
        /// <param name="harvestStage">Phenology stage at harvest</param>
        /// <param name="expectedYield">Crop expected yield (t/ha)</param>
        /// <param name="harvestDate">Date to harvest the crop</param>
        /// <param name="ttEstabToHarv">Sum of thermal time from establishment to harvest</param>
        /// <param name="plantingDepth">Planting depth (mm)</param>
        /// <param name="fieldLoss">Proportion of product lost at harvest, returned to field (0-1)</param>
        /// <param name="residueRemoval">Proportion of stover removed off field at harvest (0-1)</param>
        /// <param name="residueIncorporation">Proportion of residues to incorporate into the soil at harvest (0-1)</param>
        /// <param name="residueIncorporationDepth">Depth to incorporate the residues (mm)</param>
        /// <param name="isFertilised">Flag whether the crop raises an event with fertiliser requirements</param>
        /// <param name="firstFertDate">Date of first fertiliser application, passed on the fertiliser event</param>
        public ScrumManagementInstance(string cropName, DateTime establishDate, string establishStage,
                                       string harvestStage, double expectedYield,
                                       Nullable<DateTime> harvestDate = null, double ttEstabToHarv = double.NaN,
                                       double plantingDepth = 15, double fieldLoss = 0, double residueRemoval = 0,
                                       double residueIncorporation = 1, double residueIncorporationDepth = 150,
                                       bool isFertilised = true, Nullable<DateTime> firstFertDate = null)
        {
            // check harvest timing setup
            if (((harvestDate == null) || (harvestDate < establishDate)) && (double.IsNaN(ttEstabToHarv) || (ttEstabToHarv == 0)))
            {
                throw new Exception("A valid harvest date OR a non-zero thermal time sum (from establish to harvest) must be provided when initialising a ScrumManagementInstance");
            }

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

        /// <summary>Constructor</summary>
        /// <param name="cropParams">Dictionary with the list of crop parameters</param>
        /// <param name="today">A date to check for establishment</param>
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
            PlantingDepth = double.Parse(cropParams["PlantingDepth"]);
            HarvestStage = cropParams["HarvestStage"];
            ExpectedYield = double.Parse(cropParams["ExpectedYield"]);
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
                TtEstabToHarv = double.Parse(cropParams["TtEstabToHarv"]);
            }
            else 
            {
                throw new Exception("A valid harvest date OR thermal time sum from establish to harvest must be provided when initialising a ScrumManagementInstance");
            }

            try
            {
                FieldLoss = double.Parse(cropParams["FieldLoss"]);
            }
            catch
            {
                FieldLoss = 0.0;
            }

            try
            {
                ResidueRemoval = double.Parse(cropParams["ResidueRemoval"]);
            }
            catch
            {
                ResidueRemoval = 0.0;
            }

            try
            {
                ResidueIncorporation = double.Parse(cropParams["ResidueIncorporation"]);
            }
            catch
            {
                ResidueIncorporation= 1.0;
            }

            try
            {
                ResidueIncorporationDepth = double.Parse(cropParams["ResidueIncorporationDepth"]);
            }
            catch
            {
                ResidueIncorporationDepth = 150;
            }

            try
            {
                IsFertilised = bool.Parse(cropParams["IsFertilised"]);
            }
            catch
            {
                IsFertilised = true;
            }

            try
            {
                FirstFertDate = DateTime.Parse(cropParams["FirstFertDate"] + "-" + today.Year);
            }
            catch
            {
                FirstFertDate = EstablishDate;
            }
        }
    }
}
