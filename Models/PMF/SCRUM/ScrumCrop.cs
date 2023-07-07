using Models.Climate;
using Models.Core;
using Models.PMF.Interfaces;
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

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        private Plant scrum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link]
        private GetTempSum ttSum = null;

        [Link]
        private Clock clock = null;

        /// <summary>The summary</summary>
        [Link]
        private ISummary summary = null;

        [Link(ByName = true)]
        private IHasDamageableBiomass product = null;

        [Link(ByName = true)]
        private IHasDamageableBiomass stover = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar crop = null;

        /// <summary> The date this instance of the crop will be harvested</summary>
        private DateTime HarvestDate { get; set; }

        private double ttEmergeToHarv { get; set; }

        /// <summary> Field loss assigned at sowing so can be used in harvest event </summary>
        private double FieldLoss { get; set; }

        /// <summary> ResiduesRemoved assigned at sowing so can be used in harvest event </summary>
        private double ResidueRemoval { get; set; }

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
        public void Establish(ScrumManagement management)
        {
            string cropName = this.Name;
            double depth = management.PlantingDepth;
            double maxCover = this.Acover;
            double population = 1.0;
            double rowWidth = 0.0;

            crop = coeffCalc(management);
            scrum.Children.Add(crop);
            scrum.Sow(cropName, population, depth, rowWidth, maxCover: maxCover);
            if (management.EstablishStage.ToString() != "Seed")
            {
                phenology.SetToStage(StageNumbers[management.EstablishStage.ToString()]);
            }
            summary.WriteMessage(this,"Some of the message above is not relevent as SCRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + management.CropName + " is established as " + management.EstablishStage + " and harvested at " +
                management.HarvestStage + ". Potential yield is " + management.ExpectedYield.ToString() + " t/ha with a moisture content of " + this.MoisturePc +
                " % and HarvestIndex of " + this.HarvestIndex.ToString() + ". It will be harvested on "+ this.HarvestDate.ToString("dd-MMM-yyyy")+
                ", "+ this.ttEmergeToHarv.ToString() +" oCd from now.",MessageType.Information);
        }

        /// <summary>
        /// Data structure that holds SCRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc(ScrumManagement management)
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
                ttEmergeToHarv = ttSum.GetTtSum(management.EstablishDate, (DateTime)management.HarvestDate, this.BaseT, this.OptT, this.MaxT);
                ttEmergeToHarv -= emergeTt; //Subtract out emergence tt
            }
            else
            {
                ttEmergeToHarv = management.TtEstabToHarv - emergeTt;
            }

            if ((management.HarvestDate == DateTime.MinValue)||(management.HarvestDate == null))
            {
                this.HarvestDate = ttSum.GetHarvestDate(management.EstablishDate, emergeTt + ttEmergeToHarv, this.BaseT, this.OptT, this.MaxT);
            }
            else
            {
                this.HarvestDate = (DateTime)management.HarvestDate;
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

            double fDM = ey * dmc * (1 / this.HarvestIndex) * (1/(1- this.Proot));
            double iDM = fDM * Math.Max(PropnMaxDM[management.EstablishStage], PropnMaxDM["Emergence"]);
            cropParams["InitialStoverWt"] += (iDM * (1-this.Proot)).ToString(); 
            cropParams["InitialRootWt"] += (Math.Max(0.01,iDM * this.Proot)).ToString();//Need to have some root mass at start or else get error

            cropParams["BaseT"] += this.BaseT.ToString();
            cropParams["OptT"] += this.OptT.ToString();
            cropParams["MaxT"] += this.MaxT.ToString();
            cropParams["MaxTt"] += (this.OptT - this.BaseT).ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            this.FieldLoss = management.FieldLoss/100;
            this.ResidueRemoval = management.ResidueRemoval/100;

            Cultivar CropValues = new Cultivar(this.Name, commands);
            return CropValues;
        }
        
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (clock.Today == HarvestDate)
            {
                product.RemoveBiomass(liveToRemove: 1-FieldLoss, deadToRemove: 1-FieldLoss, liveToResidue: FieldLoss, deadToResidue: FieldLoss);
                stover.RemoveBiomass(liveToRemove: ResidueRemoval, deadToRemove: ResidueRemoval, liveToResidue: 1 - ResidueRemoval, deadToResidue: 1 - ResidueRemoval);
                scrum.EndCrop();
                scrum.Children.Remove(crop);
            }
        }
    }
}
