using Models.Climate;
using Models.Core;
using Models.PMF.Phen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Models.PMF.Scrum
{
    /// <summary>
    /// Data structure that contains information for a specific crop type in Scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Plant))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumCrop: Model
    {
        /// <summary>Harvest Index</summary>
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

        /// <summary>Maximum green cover</summary>
        [Description("Maximum green cover (0-0.97)")]
        public double Acover { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Extinction coefficient (0-1)")]
        public double ExtinctCoeff { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Description("Root Nitrogen concentration (g/g)")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Stover Nitrogen concentration (g/g)")]
        public double StoverNConc { get; set; }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Product Nitrogen concentration (g/g)")]
        public double ProductNConc { get; set; }
        
        /// <summary>Is the crop a legume</summary>
        [Description("Is the crop a legume")]
        public bool Legume { get; set; }

        /// <summary>Base temperature for crop</summary>
        [Description("Base temperature for crop (oC)")]
        public double BaseT { get; set; }
        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for crop (oC)")]
        public double OptT { get; set; }
        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for crop (oC)")]
        public double MaxT { get; set; }


        /// <summary>The plant</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        private Plant scrum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link]
        private GetTempSum ttSum = null;

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish(ScrumManagement management)
        {
            string cropName = this.Name;
            double depth = 0;
            double maxCover = this.Acover;
            double population = 1.0;
            double rowWidth = 0.0;

            Cultivar crop = coeffCalc(management);
            scrum.Sow(cropName, population, depth, rowWidth, maxCover: maxCover, cultivarOverwrites: crop);
            scrum.Phenology.Emerged = true;
            if(management.EstablishStage == "Seedling")
            {
                phenology.SetToStage(2.0);
            }
        }
        
        /// <summary>Stages that Scrum crops may be established at</summary>
        public enum EstablishStages
        {
            /// <summary>Crop established as dry seed</summary>
            Seed,
            /// <summary>Crop established by transplanting seedlings</summary>
            Seedling,
        };

        /// <summary>Stages that Scrum crops may be established at</summary>
        public enum HarvestStages
        {
            /// <summary>Crop harvested prior to reproductive growth e.g lettice</summary>
            Vegetative,
            /// <summary>Crop harvested in an early reproductive state e.g brocolii</summary>
            EarlyReproductive,
            /// <summary>Crop harvested in mid reproductive state e.g green peas</summary>
            MidReproductive,
            /// <summary>Crop harvested in late reproductive state e.g canola</summary>
            LateReproductive,
            /// <summary>Crop harvested at maturity e.g wheat if dried in storage</summary>
            Maturity,
            /// <summary>Crop harvested after maturity e.g wheat dried standing</summary>
            Ripe,
        };

        /// <summary>Dictionary containing values for the proportion of maximum DM that occurs at each predefined crop stage</summary>
        [JsonIgnore]
        public static Dictionary<string, double> PropnMaxDM = new Dictionary<string, double>() { { "Seed", 0.0066 },{ "Seedling", 0.015 },{ "Vegetative", 0.5},{ "EarlyReproductive",0.75},
            { "MidReproductive",0.86},{  "LateReproductive",0.95},{"Maturity",0.9933},{"Ripe",0.9995 } };

        /// <summary>Dictionary containing values for the proportion of thermal time to maturity that has accumulate at each predefined crop stage</summary>
        [JsonIgnore]
        public static Dictionary<string, double> PropnTt = new Dictionary<string, double>() { { "Seed", 0 },{ "Seedling", 0.16 },{ "Vegetative", 0.5},{ "EarlyReproductive",0.61},
            { "MidReproductive",0.69},{  "LateReproductive",0.8},{"Maturity",1.0},{"Ripe",1.27} };

        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"ExpectedYield","[Product].ExpectedYield.FixedValue = "},
            {"HarvestIndex","[Product].HarvestIndex.FixedValue = "},
            {"DryMatterContent","[Product].DryMatterContent.FixedValue = "},
            {"RootProportion","[Root].RootProportion.FixedValue = "},
            {"ProductNConc","[Product].MaximumNConc.FixedValue = "},
            {"StoverNConc","[Stover].MaximumNConc.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"FixationRate","[Nodule].FixationRate.FixedValue = "},
            {"ExtinctCoeff","[Stover].ExtinctionCoefficient.FixedValue = "},
            {"ACover","[Stover].Cover.Expanding.SigCoverFunction.Ymax.FixedValue = "},
            {"XoCover","[Stover].Cover.Expanding.SigCoverFunction.Xo.FixedValue = "},
            {"bCover","[Stover].Cover.Expanding.SigCoverFunction.b.FixedValue = "},
            {"XoBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.Xo.FixedValue = "},
            {"bBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.b.FixedValue = " },
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"TtSeedling","[Phenology].Seedling.Target.FixedValue ="},
            {"TtVegetative","[Phenology].Vegetative.Target.FixedValue ="},
            {"TtEarlyReproductive","[Phenology].EarlyReproductive.Target.FixedValue ="},
            {"TtMidReproductive","[Phenology].MidReproductive.Target.FixedValue ="},
            {"TtLateReproductive","[Phenology].LateReproductive.Target.FixedValue ="},
            {"TtMaturity","[Phenology].Maturity.Target.FixedValue ="},
            {"TtRipe","[Phenology].Ripe.Target.FixedValue ="},
            {"InitialWt","[Stover].InitialWt.FixedValue = "},
            {"BaseT","[Phenology].ThermalTime.Response.Y[1] = "},
            {"OptT","[Phenology].ThermalTime.Response.Y[2] = " },
            {"MaxT","[Phenology].ThermalTime.Response.Y[3] = " }
        };

        /// <summary>
        /// Data structure that holds SCRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc(ScrumManagement management)
        {
            Dictionary<string, string> cropParams = blankParams;

            if (this.Legume)
                cropParams["FixationRate"] += "1000";
            else
                cropParams["FixationRate"] += "0.0";
            double dmc = (100 - this.MoisturePc) / 100;
            cropParams["DryMatterContent"] += dmc.ToString();
            double ey = management.ExpectedYield * 100;
            cropParams["ExpectedYield"] += ey.ToString();
            cropParams["HarvestIndex"] += this.HarvestIndex.ToString();
            cropParams["ProductNConc"] += this.ProductNConc.ToString();
            cropParams["StoverNConc"] += this.StoverNConc.ToString();
            cropParams["RootNConc"] += this.RootNConc.ToString();
            cropParams["MaxRootDepth"] += this.MaxRD.ToString();
            cropParams["RootProportion"] += this.Proot.ToString();
            cropParams["ACover"] += this.Acover.ToString();
            cropParams["ExtinctCoeff"] += this.ExtinctCoeff.ToString();

            // Derive Crop Parameters
            double Tt_Harv = 0.0;
            if (Double.IsNaN(management.HarvestTt) || (management.HarvestTt == 0))
            {
                Tt_Harv = ttSum.GetTtSum(management.EstablishDate, (DateTime)management.HarvestDate, this.BaseT);
            }
            else
            {
                Tt_Harv = management.HarvestTt;     
            }

            double Tt_estab = Tt_Harv * (PropnTt[management.EstablishStage] / PropnTt[management.HarvestStage]);
            double Xo_Biomass = (Tt_Harv + Tt_estab) * .45 * (1 / PropnTt[management.HarvestStage]);
            double b_Biomass = Xo_Biomass * .25;
            double T_mat = Xo_Biomass * 2.2222;
            double Xo_cov = Xo_Biomass * 0.4;
            double b_cov = Xo_cov * 0.2;

            cropParams["XoBiomass"] += Xo_Biomass.ToString();
            cropParams["bBiomass"] += b_Biomass.ToString();
            cropParams["XoCover"] += Xo_cov.ToString();
            cropParams["bCover"] += b_cov.ToString();

            cropParams["TtSeedling"] += (T_mat * (PropnTt["Seedling"] - PropnTt["Seed"])).ToString();
            cropParams["TtVegetative"] += (T_mat * (PropnTt["Vegetative"] - PropnTt["Seedling"])).ToString();
            cropParams["TtEarlyReproductive"] += (T_mat * (PropnTt["EarlyReproductive"] - PropnTt["Vegetative"])).ToString();
            cropParams["TtMidReproductive"] += (T_mat * (PropnTt["MidReproductive"] - PropnTt["EarlyReproductive"])).ToString();
            cropParams["TtLateReproductive"] += (T_mat * (PropnTt["LateReproductive"] - PropnTt["MidReproductive"])).ToString();
            cropParams["TtMaturity"] += (T_mat * (PropnTt["Maturity"] - PropnTt["LateReproductive"])).ToString();
            cropParams["TtRipe"] += (T_mat * (PropnTt["Ripe"] - PropnTt["Maturity"])).ToString();

            double finalDM = ey * dmc * (1 / this.HarvestIndex);
            cropParams["InitialWt"] += (finalDM * PropnMaxDM[management.EstablishStage]).ToString();  

            cropParams["BaseT"] += this.BaseT.ToString();
            cropParams["OptT"] += this.OptT.ToString();
            cropParams["MaxT"] += this.MaxT.ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar CropValues = new Cultivar(commands);
            return CropValues;
        }
    }
}
