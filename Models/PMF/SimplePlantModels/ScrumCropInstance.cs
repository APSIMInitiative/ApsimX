using Models.Climate;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    public class ScrumCropInstance : Model
    {
        /// <summary>Establishemnt Date</summary>
        public string CropName { get {return this.Name; } }

        /// <summary>Harvest Index</summary>
        [Separator("Parameters for this crop instance are specified in the section below")]

        [Description("Harvest Index (0-1)")]
        public double HarvestIndex { get; set; }

        /// <summary>Moisture percentage of product</summary>
        [Description("Moisture percentage of product (%)")]
        public double MoisturePc { get; set; }

        /// <summary>Root biomass proportion</summary>
        [Description("Root Biomass proportion (0-1)")]
        public double Proot { get; set; }

        /// <summary>Root depth at harvest (mm)</summary>
        [Description("Root depth at harvest (mm)")]
        public double MaxRD { get; set; }

        /// <summary>Crop height at maturity (mm)</summary>
        [Description("Crop height at maturity (mm)")]
        public double MaxHeight { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Maximum green cover (0-0.97)")]
        public double Acover { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Extinction coefficient (0-1)")]
        public double ExtinctCoeff { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Description("Root Nitrogen concentration (g/g)")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration at maturity</summary>
        [Description("Stover Nitrogen concentration at maturity (g/g)")]
        public double StoverNConc { get; set; }

        /// <summary>Product Nitrogen Concentration at maturity</summary>
        [Description("Product Nitrogen concentration at maturity (g/g)")]
        public double ProductNConc { get; set; }

        /// <summary>Product Nitrogen Concentration at maturity</summary>
        [Description("Plant Nitrogen concentration at Seedling (g/g)")]
        public double SeedlingNConc { get; set; }


        /// <summary>Base temperature for crop</summary>
        [Description("Base temperature for crop (oC)")]
        public double BaseT { get; set; }

        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for crop (oC)")]
        public double OptT { get; set; }

        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for crop (oC)")]
        public double MaxT { get; set; }

        /// <summary>Is the crop a legume</summary>
        [Description("Is the crop a legume")]
        public bool Legume { get; set; }

        /// <summary>"Does the crop respond to water stress?"</summary>
        [Description("Does the crop respond to water stress?")]
        public bool WaterStress { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Separator("Management data for this crop can be specified below.  Alternatively this information can be sent from a manager script and left blank below")]
        [Description("Establishment Date")]
        public Nullable<DateTime> EstablishDate { get { return _establishDate; } set { _establishDate = value; } }
        private Nullable<DateTime> _establishDate { get; set; }

        /// <summary>Establishment Stage</summary>
        [Description("Establishment Stage")]
        [Display(Type = DisplayType.ScrumEstablishStages)]
        public string EstablishStage { get { return _establishStage; } set { _establishStage = value; } }
        private string _establishStage { get; set; }

        /// <summary>Planting depth (mm)</summary>
        [Description("Planting depth (mm)")]
        public double PlantingDepth { get { return _plantingDepth; } set { _plantingDepth = value; } }
        private double _plantingDepth { get; set; }

        /// <summary>Harvest Date</summary>
        [Separator("Scrum needs to have a valid harvest date or Tt duration (from establishment to harvest) specified")]
        [Description("Harvest Date")]
        //public Nullable<DateTime> harvestDate { get; set; }
        public Nullable<DateTime> HarvestDate { get { return _harvestDate; } set { _harvestDate = value;} }
        private Nullable<DateTime> _harvestDate { get; set; }
        private DateTime nonNullHarvestDate { get; set; }

        /// <summary>Harvest Tt (oCd establishment to harvest)</summary>
        [Description("TT from Establish to Harvest (oCd")]
        public double TtEstabToHarv { get { return _ttEstabToHarv; } set { _ttEstabToHarv = value; } }
        private double _ttEstabToHarv { get; set; }

        /// <summary>Planting Stage</summary>
        [Description("Harvest Stage")]
        [Display(Type = DisplayType.ScrumHarvestStages)]
        public string HarvestStage { get { return _harvestStage; } set { _harvestStage = value; } }
        private string _harvestStage { get; set; }


        /// <summary>Expected Yield (g FW/m2)</summary>
        [Separator("Specify an appropriate potential yeild for the location, sowing date and assumed genotype \nScrum will reduce yield below potential if water or N stress are predicted")]
        [Description("Expected Yield (t/Ha)")]
        public double ExpectedYield { get { return _expectedYield; } set { _expectedYield = value; } }
        private double _expectedYield { get; set; }

        /// <summary>Field loss (i.e the proportion of expected yield that is left in the field 
        /// because of diseaese, poor quality or lack of market)</summary>
        [Separator("Specify proportion of field loss and residue removal at harvest")]
        [Description("Field loss (0-1)")]
        public double FieldLoss { get { return _fieldLoss; } set { _fieldLoss = value; } }
        private double _fieldLoss { get; set; }

        /// <summary>Residue Removal (i.e the proportion of residues that are removed from the field 
        /// by bailing or some other means)</summary>
        [Description("Residue removal (0-1)")]
        public double ResidueRemoval { get { return _residueRemoval; } set { _residueRemoval = value; } }
        private double _residueRemoval { get; set; }


        


        [Link(Type = LinkType.Scoped)]
        private Clock clock = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        public Plant scrum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link]
        Weather weather = null;

        /// <summary>The summary</summary>
        [Link]
        private ISummary summary = null;

        [Link(ByName = true)]
        private IHasDamageableBiomass product = null;

        [Link(ByName = true)]
        private IHasDamageableBiomass stover = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar crop = null;

        private double ttEmergeToHarv { get; set; }

        /// <summary>Dictionary containing values for the proportion of maximum DM that occurs at each predefined crop stage</summary>
        [JsonIgnore]
        public static Dictionary<string, int> StageNumbers = new Dictionary<string, int>() { {"Seed",1 },{ "Emergence", 2 },{ "Seedling", 3 },
            { "Vegetative", 4},{ "EarlyReproductive",5},{ "MidReproductive",6},{  "LateReproductive",7},{"Maturity",8},{"Ripe",9 } };

        /// <summary>Dictionary containing values for the proportion of maximum DM that occurs at each predefined crop stage</summary>
        [JsonIgnore]
        public static Dictionary<string, double> PropnMaxDM = new Dictionary<string, double>() { {"Seed",0.0 },{ "Emergence", 0.018 },{ "Seedling", 0.05 },
            { "Vegetative", 0.5},{ "EarlyReproductive",0.7},{ "MidReproductive",0.86},{  "LateReproductive",0.95},{"Maturity",0.9925},{"Ripe",0.9995 } };


        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"InvertedRelativeMaturity","[SCRUM].TotalDMAtHarvest.InvertedRelativeMaturityAtHarvest.FixedValue = " },
            {"ExpectedYield","[Product].ExpectedYield.FixedValue = "},
            {"HarvestIndex","[Product].HarvestIndex.FixedValue = "},
            {"DryMatterContent","[Product].DryMatterContent.FixedValue = "},
            {"RootProportion","[Root].RootProportion.FixedValue = "},
            {"ProductNConc","[Product].MaxNConcAtMaturity.FixedValue = "},
            {"StoverNConc","[Stover].MaxNConcAtMaturity.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"SeedlingNConc","[SCRUM].MaxNConcAtSeedling.FixedValue = " },
            {"FixationRate","[Nodule].FixationRate.FixedValue = "},
            {"ExtinctCoeff","[Stover].ExtinctionCoefficient.FixedValue = "},
            {"ACover","[Stover].Cover.Expanding.SigCoverFunction.Ymax.FixedValue = "},
            {"XoCover","[Stover].Cover.Expanding.SigCoverFunction.Xo.FixedValue = "},
            {"bCover","[Stover].Cover.Expanding.SigCoverFunction.b.FixedValue = "},
            {"XoBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.Xo.FixedValue = "},
            {"bBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.b.FixedValue = " },
            {"MaxHeight","[Stover].HeightFunction.Ymax.FixedValue = "},
            {"XoHig","[Stover].HeightFunction.Xo.FixedValue = " },
            {"bHig","[Stover].HeightFunction.b.FixedValue = " },
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"TtSeedling","[Phenology].Seedling.Target.FixedValue ="},
            {"TtVegetative","[Phenology].Vegetative.Target.FixedValue ="},
            {"TtEarlyReproductive","[Phenology].EarlyReproductive.Target.FixedValue ="},
            {"TtMidReproductive","[Phenology].MidReproductive.Target.FixedValue ="},
            {"TtLateReproductive","[Phenology].LateReproductive.Target.FixedValue ="},
            {"TtMaturity","[Phenology].Maturity.Target.FixedValue ="},
            {"TtRipe","[Phenology].Ripe.Target.FixedValue ="},
            {"InitialStoverWt","[Stover].InitialWt.FixedValue = "},
            {"InitialRootWt", "[Root].InitialWt.Structural.FixedValue = " },
            {"BaseT","[Phenology].ThermalTime.XYPairs.X[1] = "},
            {"OptT","[Phenology].ThermalTime.XYPairs.X[2] = " },
            {"MaxT","[Phenology].ThermalTime.XYPairs.X[3] = " },
            {"MaxTt","[Phenology].ThermalTime.XYPairs.Y[2] = "},
            {"WaterStressSens","[Stover].Photosynthesis.WaterStressFactor.XYPairs.Y[1] = "}
        };

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish(ScrumManagementInstance management=null)
        {
            if (management != null) 
            {
                this._establishDate = management.EstablishDate;
                this._establishStage = management.EstablishStage;
                this._plantingDepth = management.PlantingDepth;
                this._harvestStage = management.HarvestStage;
                this._expectedYield = management.ExpectedYield;
                this._harvestDate = management.HarvestDate;
                this._ttEstabToHarv = management.TtEstabToHarv;
                this._fieldLoss = management.FieldLoss;
                this._residueRemoval = management.ResidueRemoval;
            }
            else
            {
                management = new ScrumManagementInstance(this.CropName, (DateTime)this._establishDate, this._establishStage, this._plantingDepth, this._harvestStage, this._expectedYield,
                                                 this._harvestDate, this._ttEstabToHarv, this._fieldLoss, this._residueRemoval);
            }

            if (this._expectedYield == 0.0)
                throw new Exception(this.Name + "must have a yield > 0 set for the scrum crop to grow");
            if ((this.HarvestDate == null) && (Double.IsNaN(this.TtEstabToHarv)))
                throw new Exception("Scrum requires a valid harvest date or harvest Tt to be specified");

            crop = coeffCalc(management);
            scrum.Children.Add(crop);
            double population = 1.0;
            double rowWidth = 0.0;

            scrum.Sow(CropName, population,this._plantingDepth, rowWidth, maxCover: this.Acover);
            if (management.EstablishStage.ToString() != "Seed")
            {
                phenology.SetToStage(StageNumbers[management.EstablishStage.ToString()]);
            }
            summary.WriteMessage(this, "Some of the message above is not relevent as SCRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + management.CropName + " is established as " + management.EstablishStage + " and harvested at " +
                management.HarvestStage + ". Potential yield is " + management.ExpectedYield.ToString() + " t/ha with a moisture content of " + this.MoisturePc +
                " % and HarvestIndex of " + this.HarvestIndex.ToString() + ". It will be harvested on " + nonNullHarvestDate.ToString("dd-MMM-yyyy") +
                ", " + this.ttEmergeToHarv.ToString() + " oCd from now.", MessageType.Information);
        }

        /// <summary>
        /// Data structure that holds SCRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc(ScrumManagementInstance management)
        {
            Dictionary<string, string> cropParams = new Dictionary<string, string>(blankParams);

            if (this.Legume)
                cropParams["FixationRate"] += "1000";
            else
                cropParams["FixationRate"] += "0.0";
            if (this.WaterStress)
                cropParams["WaterStressSens"] += "0.0";
            else
                cropParams["WaterStressSens"] += "1.0";

            cropParams["InvertedRelativeMaturity"] += (1 / PropnMaxDM[management.HarvestStage]).ToString();
            double dmc = (100 - this.MoisturePc) / 100;
            cropParams["DryMatterContent"] += dmc.ToString();
            double ey = management.ExpectedYield * 100;
            cropParams["ExpectedYield"] += ey.ToString();
            cropParams["HarvestIndex"] += this.HarvestIndex.ToString();
            cropParams["ProductNConc"] += this.ProductNConc.ToString();
            cropParams["StoverNConc"] += this.StoverNConc.ToString();
            cropParams["RootNConc"] += this.RootNConc.ToString();
            cropParams["SeedlingNConc"] += this.SeedlingNConc.ToString();
            cropParams["MaxRootDepth"] += this.MaxRD.ToString();
            cropParams["MaxHeight"] += this.MaxHeight.ToString();
            cropParams["RootProportion"] += this.Proot.ToString();
            cropParams["ACover"] += this.Acover.ToString();
            cropParams["ExtinctCoeff"] += this.ExtinctCoeff.ToString();



            // Derive the proportion of Tt that has accumulated at each stage from the proporiton of DM at each stage and the logistic funciton rearanged
            Dictionary<string, double> PropnTt = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> entry in PropnMaxDM)
            {
                if (entry.Key == "Seed")
                {
                    PropnTt.Add(entry.Key, 0.0);
                }
                else
                {
                    double propTt = (Math.Log((1 / entry.Value) - 1) * -11.25 + 45) / 100;
                    PropnTt.Add(entry.Key, propTt);
                }
            }

            double emergeTt = 0.0;
            if (management.EstablishStage == "Seed")
                emergeTt = management.PlantingDepth * 5.0; //This is Phenology.Emerging.Target.ShootRate value 

            // Derive Crop Parameters
            ttEmergeToHarv = 0.0;
            if (Double.IsNaN(management.TtEstabToHarv) || (management.TtEstabToHarv == 0))
            {
                ttEmergeToHarv = GetTtSum(management.EstablishDate, (DateTime)management.HarvestDate, this.BaseT, this.OptT, this.MaxT);
                ttEmergeToHarv -= emergeTt; //Subtract out emergence tt
            }
            else
            {
                ttEmergeToHarv = management.TtEstabToHarv - emergeTt;
            }

            if ((management.HarvestDate == DateTime.MinValue) || (management.HarvestDate == null))
            {
                this._harvestDate = GetHarvestDate(management.EstablishDate, emergeTt + ttEmergeToHarv, this.BaseT, this.OptT, this.MaxT);
                this.nonNullHarvestDate = (DateTime)this._harvestDate;
            }
            else
            {
                this._harvestDate = (DateTime)management.HarvestDate;
            }

            double PropnTt_EstToHarv = PropnTt[management.HarvestStage] - PropnTt[management.EstablishStage];
            double Tt_mat = ttEmergeToHarv * 1 / PropnTt_EstToHarv;
            double Xo_Biomass = Tt_mat * .45;
            double b_Biomass = Xo_Biomass * .25;
            double Xo_cov = Xo_Biomass * 0.4;
            double b_cov = Xo_cov * 0.2;
            double Xo_hig = Xo_Biomass * 0.7;
            double b_hig = Xo_hig * 0.2;

            cropParams["XoBiomass"] += Xo_Biomass.ToString();
            cropParams["bBiomass"] += b_Biomass.ToString();
            cropParams["XoCover"] += Xo_cov.ToString();
            cropParams["bCover"] += b_cov.ToString();
            cropParams["XoHig"] += Xo_hig.ToString();
            cropParams["bHig"] += b_hig.ToString();

            cropParams["TtSeedling"] += (Tt_mat * (PropnTt["Seedling"] - PropnTt["Emergence"])).ToString();
            cropParams["TtVegetative"] += (Tt_mat * (PropnTt["Vegetative"] - PropnTt["Seedling"])).ToString();
            cropParams["TtEarlyReproductive"] += (Tt_mat * (PropnTt["EarlyReproductive"] - PropnTt["Vegetative"])).ToString();
            cropParams["TtMidReproductive"] += (Tt_mat * (PropnTt["MidReproductive"] - PropnTt["EarlyReproductive"])).ToString();
            cropParams["TtLateReproductive"] += (Tt_mat * (PropnTt["LateReproductive"] - PropnTt["MidReproductive"])).ToString();
            cropParams["TtMaturity"] += (Tt_mat * (PropnTt["Maturity"] - PropnTt["LateReproductive"])).ToString();
            cropParams["TtRipe"] += (Tt_mat * (PropnTt["Ripe"] - PropnTt["Maturity"])).ToString();

            double fDM = ey * dmc * (1 / this.HarvestIndex) * (1 / (1 - this.Proot));
            double iDM = fDM * Math.Max(PropnMaxDM[management.EstablishStage], PropnMaxDM["Emergence"]);
            cropParams["InitialStoverWt"] += (iDM * (1 - this.Proot)).ToString();
            cropParams["InitialRootWt"] += (Math.Max(0.01, iDM * this.Proot)).ToString();//Need to have some root mass at start or else get error

            cropParams["BaseT"] += this.BaseT.ToString();
            cropParams["OptT"] += this.OptT.ToString();
            cropParams["MaxT"] += this.MaxT.ToString();
            cropParams["MaxTt"] += (this.OptT - this.BaseT).ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar CropValues = new Cultivar(this.Name, commands);
            return CropValues;
        }
        
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if ((zone != null) && (clock != null))
            {
                if (clock.Today == _establishDate)
                {
                    Establish();
                }

                if (clock.Today == HarvestDate)
                {
                    product.RemoveBiomass(liveToRemove: 1 - FieldLoss, deadToRemove: 1 - FieldLoss, liveToResidue: FieldLoss, deadToResidue: FieldLoss);
                    stover.RemoveBiomass(liveToRemove: ResidueRemoval, deadToRemove: ResidueRemoval, liveToResidue: 1 - ResidueRemoval, deadToResidue: 1 - ResidueRemoval);
                    scrum.EndCrop();
                    scrum.Children.Remove(crop);
                }
            }
        }

        /// <summary>
        /// Calculates the accumulated thermal time between two dates
        /// </summary>
        /// <param name="start">Start Date</param>
        /// <param name="end">End Date</param>
        /// <param name="BaseT">Base temperature</param>
        /// <param name="OptT">Optimal temperature</param>
        /// <param name="MaxT">Maximum temperature</param>
        public double GetTtSum(DateTime start, DateTime end, double BaseT, double OptT, double MaxT)
        {
            double[] xs = new double[] { BaseT, OptT, MaxT };
            double[] ys = new double[] { 0, OptT - BaseT, 0 };
            XYPairs TtResponse = new XYPairs() { X = xs, Y = ys };

            double TtSum = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                DailyMetDataFromFile TodaysMetData = weather.GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
            }

            return TtSum;
        }

        /// <summary>
        /// Calculates the accumulated thermal time between two dates
        /// </summary>
        /// <param name="start">Start Date</param>
        /// <param name="HarvTt">Thermal time from establishment to Harvest</param>
        /// <param name="BaseT">Base Temperature</param>
        /// <param name="OptT">Optimum temperature</param>
        /// <param name="MaxT">Maximum Temperautre</param>
        public DateTime GetHarvestDate(DateTime start, double HarvTt, double BaseT, double OptT, double MaxT)
        {
            double[] xs = new double[] { BaseT, OptT, MaxT };
            double[] ys = new double[] { 0, OptT - BaseT, 0 };
            XYPairs TtResponse = new XYPairs { X = xs, Y = ys };

            double TtSum = 0;
            DateTime d = start;
            while (TtSum < HarvTt)
            {
                DailyMetDataFromFile TodaysMetData = weather.GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
                d = d.AddDays(1);
            }

            return d;
        }
    }



}
