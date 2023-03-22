namespace Models.PMF
{
    using Models.Climate;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;

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
        private Weather weather = null;

        private ScrumCrop crop = new ScrumCrop();
        private ScrumManagement management = new ScrumManagement();

        [Link]
        private ScrumCrop refsCrop = null;

        [Link]
        private ScrumManagement refsManagement = null;

        /// <summary>Dictionary containing values for the proportion of maximum DM that occurs at each predefined crop stage</summary>
        public static Dictionary<string, double> PropnMaxDM = new Dictionary<string, double>() { { "Seed", 0.0066 },{ "Seedling", 0.015 },{ "Vegetative", 0.5},{ "EarlyReproductive",0.75},
            { "MidReproductive",0.86},{  "LateReproductive",0.95},{"Maturity",0.9933},{"Ripe",0.9995 } };

        /// <summary>Dictionary containing values for the proportion of thermal time to maturity that has accumulate at each predefined crop stage</summary>
        public static Dictionary<string, double> PropnTt = new Dictionary<string, double>() { { "Seed", 0 },{ "Seedling", 0.16 },{ "Vegetative", 0.5},{ "EarlyReproductive",0.61},
            { "MidReproductive",0.69},{  "LateReproductive",0.8},{"Maturity",1.0},{"Late",1.27} };

        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"ExpectedYield","[Product].ExpectedYield.FixedValue = "},
            {"HarvestIndex","[Product].HarvestIndex.FixedValue = "},
            {"DryMatterContent","[Product].DryMatterContent.FixedValue = "},
            {"RootProportion","[Root].RootProportion.FixedValue = "},
            {"ProductNConc","[Product].MaximumNConc.FixedValue = "},
            {"StoverConc","[Stover].MaximumNConc.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"FixationRate","[Nodule].FixationRate.FixedValue = 0"},
            {"ACover","[Stover].Cover.Expanding.SigCoverFunction.Ymax.FixedValue = "},
            {"XoCover","[Stover].Cover.Expanding.SigCoverFunction.Xo.FixedValue = "},
            {"bCover","[Stover].Cover.Expanding.SigCoverFunction.b.FixedValue = "},
            {"XoBiomass","[Stover].XoBiomass.FixedValue = "},
            {"bBiomass","[Stover].bBiomass.FixedValue = " },
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"KLModifiers","[Root].KLModifier.XYPairs.Y = "},
            {"TtSeedling","[Phenology].Seedling.Target.FixedValue ="},
            {"TtVegetative","[Phenology].Vegtative.Target.FixedValue ="},
            {"TtEarlyReproductive","[Phenology].EarlyReproductive.Target.FixedValue ="},
            {"TtMidReproductive","[Phenology].MidReproductive.Target.FixedValue ="},
            {"TtLateReproductive","[Phenology].LateReproductive.Target.FixedValue ="},
            {"TtMaturity","[Phenology].Maturity.Target.FixedValue ="},
            {"TtRipe","[Phenology].Ripening.Target.FixedValue ="},
            {"BaseT","[Phenology].ThermalTime.Response[0] = 0"},
            {"OptT","[Phenology].ThermalTime.Response[1] = 30" },
            {"MaxT","[Phenology].ThermalTime.Response[3] = 40" }
        };

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void SetScrumRunning(ScrumCrop senderCrop = null, ScrumManagement senderManagement = null)
        {
            if (senderCrop != null)
                this.crop = senderCrop;
            else
                this.crop = refsCrop;
            if (senderManagement != null)
                this.management = senderManagement;
            else
                this.management = refsManagement;

            string cropName = this.Name;
            double depth = 0;
            double maxCover = this.crop.Acover;
            double population = 1.0;
            double rowWidth = 0.0;


            Cultivar crop = calcCropValues();
            scrum.Sow(cropName, population, depth, rowWidth, maxCover: maxCover, cultivarOverwrites: crop);
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (clock.Today == management.HarvestDate)
            {
                scrum.Harvest();
                scrum.EndCrop();
            }
        }


        /// <summary>
        /// Data structure that holds SCRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        private Cultivar calcCropValues()
        {
            Dictionary<string, string> cropParams = blankParams;

            if (crop.Legume)
                cropParams["FixationRate"] += "1000";
            cropParams["DryMatterContent"] += ((100 - crop.MoisturePc)/100).ToString();
            cropParams["ExpectedYield"] += management.ExpectedYield.ToString();
            cropParams["HarvestIndex"] += crop.HarvestIndex.ToString();
            cropParams["ProductNConc"] += crop.ProductNConc.ToString();
            cropParams["KLModifiers"] += CalcKL();
            cropParams["RootNConc"] += crop.RootNConc.ToString();
            cropParams["MaxRootDepth"] += crop.MaxRD.ToString();
            cropParams["RootProportion"] += crop.Proot.ToString();

            // Derive Crop Parameters
            double Tt_Harv = CalcCropTt();
            double Tt_estab = Tt_Harv * (PropnTt[management.EstablishStage] / PropnTt[management.HarvestStage]);
            double Xo_Biomass = (Tt_Harv + Tt_estab) * .45 * (1 / PropnTt[management.HarvestStage]);
            double b_Biomass = Xo_Biomass * .25;
            double T_mat = Xo_Biomass * 2.2222;
            double Xo_cov = Xo_Biomass * 0.4 / crop.rCover;
            double b_cov = Xo_cov * 0.2;
            
            cropParams["XoBiomass"] = Xo_Biomass.ToString();
            cropParams["bBiomass"] = b_Biomass.ToString();
            cropParams["XoCover"] = Xo_cov.ToString();
            cropParams["bCover"] = b_cov.ToString();
            cropParams["XoBiomass"] = Xo_Biomass.ToString();

            cropParams["TtSeedling"] = (T_mat * (PropnTt["Seedling"] - PropnTt["Seed"])).ToString();
            cropParams["TtVegetative"] = (T_mat * (PropnTt["Vegetative"]- PropnTt["Seed"])).ToString();
            cropParams["TtEarlyReproductive"] = (T_mat * (PropnTt["EarlyReproductive"] -PropnTt["Vegetative"])).ToString();
            cropParams["TtMidReproductive"] = (T_mat * (PropnTt["MidReproductive"]- PropnTt["EarlyReproductive"])).ToString();
            cropParams["TtLateReproductive"] = (T_mat * (PropnTt["LateReproductive"] - PropnTt["EarlyReproductive"])).ToString();
            cropParams["TtMaturity"] = (T_mat * (PropnTt["Maturity"] - PropnTt["LateReproductive"])).ToString();
            cropParams["TtRipe"] = (T_mat * (PropnTt["Ripe"] - PropnTt["Maturity"])).ToString();

            string[] overwrites = new string[cropParams.Count];
            Cultivar CropValues = new Cultivar();
            cropParams.Values.CopyTo(CropValues.Command, 0);
            return CropValues;
        }

        private string CalcKL()
        {
            return "0.1,0.3";
        }

        private double CalcCropTt()
        {
            double cropTt = 0;
            for (DateTime d = management.EstablishmentDate; d <= management.HarvestDate; d = d.AddDays(1))
            {
                DailyMetDataFromFile todaysWeather = weather.GetMetData(d);
                double dailyTt = Math.Max(0, (todaysWeather.MinT + todaysWeather.MaxT) / 2 - crop.BaseT);
                cropTt += dailyTt;
            }
                
            return cropTt;
        }




    }


    /// <summary>
    /// Data structure that contains information for a specific planting of scrum
    /// </summary>
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

    /// <summary>
    /// Data structure that contains information for a specific crop type in Scrum
    /// </summary>
    public class ScrumCrop
    {
        /// <summary>Harvest Index</summary>
        [Description("Harvest Index")]
        public double HarvestIndex { get; set; }

        /// <summary>Moisture percentage of product</summary>
        [Description("Moisture percentage of product")]
        public double MoisturePc { get; set; }

        /// <summary>Root biomass proportion</summary>
        [Description("Root Biomass proportion")]
        public double Proot { get; set; }

        /// <summary>Root depth at harvest</summary>
        [Description("Root depth at harvest")]
        public double MaxRD { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Maximum green cover")]
        public double Acover { get; set; }

        /// <summary>Cover rate </summary>
        [Description("Cover rate")]
        public double rCover { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Description("Root Nitrogen concentration")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Stover Nitrogen concentration")]
        public double StoverNConc { get; set; }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Product Nitrogen concentration")]
        public double ProductNConc { get; set; }
        
        /// <summary>Is the crop a legume</summary>
        [Description("Is the crop a legume")]
        public bool Legume { get; set; }

        /// <summary>Base temperature for crop</summary>
        [Description("Base temperature for crop")]
        public double BaseT { get; set; }
        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for crop")]
        public double OptT { get; set; }
        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for crop")]
        public double MaxT { get; set; }
    }

}
