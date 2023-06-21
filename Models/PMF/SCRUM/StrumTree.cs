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
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StrumTree: Model
    {
        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Separator("Tree Age")]
        [Description("Tree Age At Start of Simulation")]
        public int AgeAtSimulationStart { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Years from planting to reach Maximum dimension (years)")]
        public int YearsToMaxDimension { get; set; }

        /// <summary>Bud Break (Days after Winter Solstice)</summary>
        [Separator("Tree Phenology.  Specify when canopy stages occur in days since the winter solstice")]
        [Description("Bud Break (Days after Winter Solstice)")]
        public double BudBreakDAWS { get; set; }

        /// <summary>Start Full Canopy (Days after Winter Solstice)</summary>
        [Description("Start Full Canopy (Days after Winter Solstice)")]
        public double StartFullCanopyDAWS { get; set; }

        /// <summary>Start of leaf fall (Days after Winter Solstice)</summary>
        [Description("Start of leaf fall (Days after Winter Solstice)")]
        public double StartLeafFallDAWS { get; set; }

        /// <summary>Start Fruit Growth (Days after Winter Solstice)</summary>
        [Description("End of Leaf fall (Days after Winter Solstice)")]
        public double EndLeafFallDAWS { get; set; }

        /// <summary>Root depth at harvest (mm)</summary>
        [Separator("Plant Dimnesions")]
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
        
        /// <summary>Root Nitrogen Concentration</summary>
        [Separator("Plant Nitrogen contents")]
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

        /// <summary>Maximum green cover</summary>
        [Separator("Canopy parameters")]
        [Description("Extinction coefficient (0-1)")]
        public double ExtinctCoeff { get; set; }

        /// <summary>Maximum LAI of mature tree prior to pruning (m2/m2)</summary>
        [Description("Maximum LAI of mature tree prior to pruning (m2/m2)")]
        public double MaxLAI { get; set; }

        /// <summary>Maximum green cover</summary>
        [Separator("Fruit parameters")]
        [Description("Fruit number retained (/m2 post thinning)")]
        public double Number { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Potential Fruit Size (mm diameter)")]
        public double Size { get; set; }

        /// <summary>Fruit Density </summary>
        [Description("Fruit Density (g/cm3)")]
        public double Density { get; set; }

        /// <summary>Fruit DM conc </summary>
        [Description("Fruit DM conc (g/g)")]
        public double DMC { get; set; }

        /// <summary>Maximum Bloom </summary>
        [Description("Maximum Bloom (Days After Winter Solstice)")]
        public double DAWSMaxBloom { get; set; }

        /// <summary>Start Linear Growth </summary>
        [Description("Start Linear Growth (Days After Winter Solstice)")]
        public double DAWSLinearGrowth { get; set; }

        /// <summary>Maximum Size </summary>
        [Description("Max Size (Days After Winter Solstice)")]
        public double DAWSMaxSize { get; set; }


        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant strum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link]
        private ISummary summary = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar tree = null;

        
        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"SpringDormancy","[Phenology].SpringDormancy.DAWStoProgress = " },
            {"CanopyExpansion","[Phenology].CanopyExpansion.DAWStoProgress = " },
            {"FullCanopy","[Phenology].FullCanopy.DAWStoProgress = " },
            {"LeafFall", "[Phenology].LeafFall.DAWStoProgress = " },
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"MaxPrunedHeight","[MaxPrunedHeight].FixedValue = " },
            {"CanopyBaseHeight","[Height].CanopyBaseHeight.Maximum.FixedValue = " },
            {"MaxSeasonalHeight","[Height].SeasonalGrowth.Maximum.FixedValue = " },
            {"MaxPrunedWidth","[Width].PrunedWidth.Maximum.FixedValue = "},
            {"MaxSeasonalWidth","[Width].SeasonalGrowth.Maximum.FixedValue = " },
            {"FruitNConc","[Fruit].MaxNConcAtHarvest.FixedValue = "},
            {"LeafNConc","[Leaf].MaxNConcAtStartLeafFall.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"TrunkNConc","[Trunk].MaximumNConc.FixedValue = "},
            {"ExtinctCoeff","[Leaf].ExtinctionCoefficient.FixedValue = "},
            {"MaxLAI","[Leaf].Area.Maximum.FixedValue = " },
            {"InitialTrunkWt","[Trunk].InitialWt.Structural.FixedValue = "},
            {"InitialRootWt", "[Root].InitialWt.Structural.FixedValue = " },
            {"InitialFruitWt","[Fruit].InitialWt.Structural.FixedValue = "},
            {"InitialLeafWt", "[Leaf].InitialWt.FixedValue = " },
            {"YearsToMaturity","[RelativeAnnualDimension].XYPairs.X[2] = " },
            {"YearsToMaxRD","[Root].RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"Number","[Fruit].Number.RetainedPostThinning.FixedValue = " },
            {"Size","[Fruit].MaximumSize.FixedValue = " },
            {"Density","[Fruit].Density.FixedValue = " },
            {"DMC", "[Fruit].MinimumDMC.FixedValue = " },
            {"DAWSMaxBloom","[Fruit].DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[2] = "},
            {"DAWSLinearGrowth","[Fruit].DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[3] = "},
            {"DAWSEndLinearGrowth","[Fruit].DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[4] = "},
            {"DAWSMaxSize","[Fruit].DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[5] = "},
        };

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish()
        {
            string cropName = this.Name;
            double depth = this.MaxRD * this.AgeAtSimulationStart / this.YearsToMaxDimension;
            double population = 1.0;
            double rowWidth = 0.0;

            tree = coeffCalc();
            strum.Children.Add(tree);
            strum.Sow(cropName, population, depth, rowWidth);
            phenology.SetAge(AgeAtSimulationStart);
            summary.WriteMessage(this,"Some of the message above is not relevent as STRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + this.Name + " is established as " + this.AgeAtSimulationStart.ToString() + " Year old plant "
                ,MessageType.Information); 
        }

        /// <summary>
        /// Data structure that holds STRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc()
        {
            Dictionary<string, string> cropParams = new Dictionary<string, string>(blankParams);

            cropParams["SpringDormancy"] += BudBreakDAWS.ToString();
            cropParams["CanopyExpansion"] += StartFullCanopyDAWS.ToString();
            cropParams["FullCanopy"] += StartLeafFallDAWS.ToString();
            cropParams["LeafFall"] += EndLeafFallDAWS.ToString();
            cropParams["MaxRootDepth"] += MaxRD.ToString();
            cropParams["MaxPrunedHeight"] += MaxPrunedHeight.ToString();
            cropParams["CanopyBaseHeight"] += CanopyBaseHeight.ToString();
            cropParams["MaxSeasonalHeight"] += SeasonalHeightGrowth.ToString();
            cropParams["MaxPrunedWidth"] += MaxPrunedWidth.ToString();
            cropParams["MaxSeasonalWidth"] += SeasonalWidthGrowth.ToString();
            cropParams["FruitNConc"] += FruitNConc.ToString();
            cropParams["LeafNConc"] += LeafNConc.ToString();
            cropParams["RootNConc"] += RootNConc.ToString();
            cropParams["TrunkNConc"] += TrunkNConc.ToString();
            cropParams["ExtinctCoeff"] += ExtinctCoeff.ToString();
            cropParams["MaxLAI"] += MaxLAI.ToString();
            cropParams["YearsToMaturity"] += YearsToMaxDimension.ToString();
            cropParams["YearsToMaxRD"] += YearsToMaxDimension.ToString();
            cropParams["Number"] += Number.ToString();
            cropParams["Size"] += Size.ToString();
            cropParams["Density"] += Density.ToString();
            cropParams["DMC"] += DMC.ToString();
            cropParams["DAWSMaxBloom"] += DAWSMaxBloom.ToString();
            cropParams["DAWSLinearGrowth"] += DAWSLinearGrowth.ToString();
            cropParams["DAWSEndLinearGrowth"] += ((int)(DAWSLinearGrowth+(DAWSMaxSize - DAWSLinearGrowth)*.6)).ToString();
            cropParams["DAWSMaxSize"] += DAWSMaxSize.ToString();

            if (AgeAtSimulationStart <= 0)
                throw new Exception("SPRUMtree needs to have a 'Tree Age at start of Simulation' > 0 years");
            cropParams["InitialTrunkWt"] += ((double)AgeAtSimulationStart/ (double)YearsToMaxDimension * 1000).ToString();
            cropParams["InitialRootWt"] += ((double)AgeAtSimulationStart / (double)YearsToMaxDimension * 1000).ToString();
            cropParams["InitialFruitWt"] += (0).ToString();
            cropParams["InitialLeafWt"] += (0).ToString();
                
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar TreeValues = new Cultivar(this.Name, commands);
            return TreeValues;
        }
        
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {

        }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartSimulation(object sender, EventArgs e)
        {
            Establish();
        }
    }
}
