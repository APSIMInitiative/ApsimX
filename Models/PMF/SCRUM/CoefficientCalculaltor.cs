using Models.Climate;
using Models.Core;
using System;
using System.Collections.Generic;
namespace Models.PMF.Scrum
{
    /// <summary>
    /// This model derives SCRUM parameters from basic user information and sets the correct values in the model when it runs.
    /// </summary>
    [ValidParent(ParentType = typeof(Referee))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CoefficientCalculator : Model
    {
        [Link(Type = LinkType.Child)]
        private GetTempSum ttSum = null;

        /// <summary>Dictionary containing values for the proportion of maximum DM that occurs at each predefined crop stage</summary>
        public static Dictionary<string, double> PropnMaxDM = new Dictionary<string, double>() { { "Sowing", 0.0066 },{ "Seedling", 0.015 },{ "Vegetative", 0.5},{ "EarlyReproductive",0.75},
            { "MidReproductive",0.86},{  "LateReproductive",0.95},{"Maturity",0.9933},{"Ripe",0.9995 } };

        /// <summary>Dictionary containing values for the proportion of thermal time to maturity that has accumulate at each predefined crop stage</summary>
        public static Dictionary<string, double> PropnTt = new Dictionary<string, double>() { { "Sowing", 0 },{ "Seedling", 0.16 },{ "Vegetative", 0.5},{ "EarlyReproductive",0.61},
            { "MidReproductive",0.69},{  "LateReproductive",0.8},{"Maturity",1.0},{"Ripe",1.27} };

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
            {"BaseT","[Phenology].ThermalTime.Response.Y[1] = "},
            {"OptT","[Phenology].ThermalTime.Response.Y[2] = " },
            {"MaxT","[Phenology].ThermalTime.Response.Y[3] = " }
        };

        /// <summary>
        /// Data structure that holds SCRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar Values(ScrumCrop crop, ref ScrumManagement management)
        {
            Dictionary<string, string> cropParams = blankParams;

            if (crop.Legume)
                cropParams["FixationRate"] += "1000";
            else
                cropParams["FixationRate"] += "0.0";
            cropParams["DryMatterContent"] += ((100 - crop.MoisturePc) / 100).ToString();
            cropParams["ExpectedYield"] += management.ExpectedYield.ToString();
            cropParams["HarvestIndex"] += crop.HarvestIndex.ToString();
            cropParams["ProductNConc"] += crop.ProductNConc.ToString();
            cropParams["StoverNConc"] += crop.StoverNConc.ToString();
            cropParams["RootNConc"] += crop.RootNConc.ToString();
            cropParams["MaxRootDepth"] += crop.MaxRD.ToString();
            cropParams["RootProportion"] += crop.Proot.ToString();
            cropParams["ACover"] += crop.Acover.ToString();
            cropParams["ExtinctCoeff"] += crop.ExtinctCoeff.ToString();

            // Derive Crop Parameters
            double Tt_Harv = 0.0;
            if (Double.IsNaN(management.HarvestTt)||(management.HarvestTt==0))
            {
                Tt_Harv = ttSum.GetTtSum(management.EstablishDate, (DateTime)management.HarvestDate, crop.BaseT);
            }
            else
            {
                Tt_Harv = management.HarvestTt;
                management.HarvestDate = ttSum.GetHarvestDate(management.EstablishDate, management.HarvestTt, crop.BaseT);
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

            cropParams["TtSeedling"] += (T_mat * (PropnTt["Seedling"] - PropnTt["Sowing"])).ToString();
            cropParams["TtVegetative"] += (T_mat * (PropnTt["Vegetative"] - PropnTt["Seedling"])).ToString();
            cropParams["TtEarlyReproductive"] += (T_mat * (PropnTt["EarlyReproductive"] - PropnTt["Vegetative"])).ToString();
            cropParams["TtMidReproductive"] += (T_mat * (PropnTt["MidReproductive"] - PropnTt["EarlyReproductive"])).ToString();
            cropParams["TtLateReproductive"] += (T_mat * (PropnTt["LateReproductive"] - PropnTt["MidReproductive"])).ToString();
            cropParams["TtMaturity"] += (T_mat * (PropnTt["Maturity"] - PropnTt["LateReproductive"])).ToString();
            cropParams["TtRipe"] += (T_mat * (PropnTt["Ripe"] - PropnTt["Maturity"])).ToString();

            cropParams["BaseT"] += crop.BaseT.ToString();
            cropParams["OptT"] += crop.OptT.ToString();
            cropParams["MaxT"] += crop.MaxT.ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar CropValues = new Cultivar(commands);
            return CropValues;
        }
    }
}
