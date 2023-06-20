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
    public class StrumTree: Model
    {
        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Years from planting to reach Maximum dimension (years)")]
        public double YearsToMaxDimension { get; set; }

        /// <summary>Bud Break (Days after Winter Solstice)</summary>
        [Description("Bud Break (Days after Winter Solstice)")]
        public double BudBreakDAWS { get; set; }

        /// <summary>Start Fruit Growth (Days after Winter Solstice)</summary>
        [Description("Start Fruit Growth (Days after Winter Solstice)")]
        public double StartFruitGrowthDAWS { get; set; }

        /// <summary>Fruit Ripe (Days after Winter Solstice)</summary>
        [Description("Fruit Ripe (Days after Winter Solstice)")]
        public double FruitRipeDAWS { get; set; }

        /// <summary>Start Fruit Growth (Days after Winter Solstice)</summary>
        [Description("Start Fruit Growth (Days after Winter Solstice)")]
        public double BareTreeDAWS { get; set; }

        /// <summary>Root depth at harvest (mm)</summary>
        [Description("Root depth when mature (mm)")]
        public double MaxRD { get; set; }

        /// <summary>Hight of the bottom of the canop (mm)</summary>
        [Description("Hight of the bottom of the canopy (mm)")]
        public double CanopyBaseHeight { get; set; }

        /// <summary>Hight of the top of the canopy after pruning mature tree (mm)</summary>
        [Description("Hight of the top of the canopy after pruning mature tree (mm)")]
        public double MaxPrunedHeight { get; set; }

        /// <summary>Increase in hight between bud burst and pruning of mature tree  (mm)</summary>
        [Description("Increase in hight between bud burst and pruning of mature tree (mm)")]
        public double SeasonalHeightGrowth { get; set; }

        /// <summary>Hight of the top of the canopy after pruning mature tree (mm)</summary>
        [Description("Width of the top of the canopy after pruning mature tree (mm)")]
        public double MaxPrunedWidth { get; set; }

        /// <summary>Increase in width between bud burst and pruning of mature tree (mm)</summary>
        [Description("Increase in width between bud burst and pruning of mature tree (mm)")]
        public double SeasonalWidthGrowth { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Extinction coefficient (0-1)")]
        public double ExtinctCoeff { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Description("Root Nitrogen concentration (g/g)")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration at maturity</summary>
        [Description("Leaf Nitrogen concentration at maturity (g/g)")]
        public double LeafNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration at maturity</summary>
        [Description("Trunk and branch Nitrogen concentration (g/g)")]
        public double TrunkNConc { get; set; }

        /// <summary>Product Nitrogen Concentration at maturity</summary>
        [Description("Fruit Nitrogen concentration at maturity (g/g)")]
        public double FruitNConc { get; set; }

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

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        private Plant strum = null;

        //[Link(Type = LinkType.Scoped, ByName = true)]
        //private Phenology phenology = null;

        //[Link]
        //private Clock clock = null;

        //[Link]
        //private ISummary summary = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar tree = null;

        
        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"DryMatterContent","[Product].DryMatterContent.FixedValue = "},
            {"RootProportion","[Root].RootProportion.FixedValue = "},
            {"ProductNConc","[Product].MaxNConcAtMaturity.FixedValue = "},
            {"LeafNConc","[Stover].MaxNConcAtMaturity.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"FixationRate","[Nodule].FixationRate.FixedValue = "},
            {"ExtinctCoeff","[Stover].ExtinctionCoefficient.FixedValue = "},
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"InitialTrunkWt","[Stover].InitialWt.FixedValue = "},
            {"InitialRootWt", "[Root].InitialWt.Structural.FixedValue = " },
            {"InitialFruitWt","[Fruit].InitialWt.FixedValue = "},
            {"InitialLeafWt", "[Leaf].InitialWt.Structural.FixedValue = " },
            {"BaseT","[Phenology].ThermalTime.XYPairs.X[1] = "},
            {"OptT","[Phenology].ThermalTime.XYPairs.X[2] = " },
            {"MaxT","[Phenology].ThermalTime.XYPairs.X[3] = " },
            {"MaxTt","[Phenology].ThermalTime.XYPairs.Y[2] = "},
        };

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish(ScrumManagement management)
        {
            string cropName = this.Name;
            double depth = management.PlantingDepth;
            double population = 1.0;
            double rowWidth = 0.0;

            tree = coeffCalc();
            strum.Children.Add(tree);
            strum.Sow(cropName, population, depth, rowWidth);
            if (management.EstablishStage.ToString() != "Seed")
            {
               // phenology.SetToStage(StageNumbers[management.EstablishStage.ToString()]);
            }
           /* summary.WriteMessage(this,"Some of the message above is not relevent as SCRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + management.CropName + " is established as " + management.EstablishStage + " and harvested at " +
                management.HarvestStage + ". Potential yield is " + management.ExpectedYield.ToString() + " t/ha with a moisture content of " + this.MoisturePc +
                " % and HarvestIndex of " + this.HarvestIndex.ToString() + ". It will be harvested on "+ this.HarvestDate.ToString("dd-MMM-yyyy")+
                ", "+ this.ttEmergeToHarv.ToString() +" oCd from now.",MessageType.Information); */
        }

        /// <summary>
        /// Data structure that holds SCRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc()
        {
            Dictionary<string, string> cropParams = new Dictionary<string, string>(blankParams);

            if (this.Legume)
                cropParams["FixationRate"] += "1000";
            else
                cropParams["FixationRate"] += "0.0";
            cropParams["FruitNConc"] += this.FruitNConc.ToString();
            cropParams["FruitNConc"] += this.TrunkNConc.ToString();
            cropParams["RootNConc"] += this.RootNConc.ToString();
            cropParams["LeafNConc"] += this.LeafNConc.ToString();
            cropParams["MaxRootDepth"] += this.MaxRD.ToString();
            cropParams["ExtinctCoeff"] += this.ExtinctCoeff.ToString();

            
            cropParams["BaseT"] += this.BaseT.ToString();
            cropParams["OptT"] += this.OptT.ToString();
            cropParams["MaxT"] += this.MaxT.ToString();
            cropParams["MaxTt"] += (this.OptT - this.BaseT).ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar TreeValues = new Cultivar(this.Name, commands);
            return TreeValues;
        }
        
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {

        }
    }
}
