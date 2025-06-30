using Models.Climate;
using Models.Core;
using Models.Functions;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Soils;
using Models.Zones;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Data structure that contains information for a specific crop type in Scrum
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StrumTreeInstance : Model
    {
        ///<summary>Is the tree decidious</summary>
        [Separator("Tree Type")]
        [Description("Is the tree decidious or ever green")]
        [Display(Type = DisplayType.StrumTreeTypes)]
        public string TreeType { get; set; }

        /// <summary>Is the tree decidious</summary>
        public bool Decidious
        {
            get
            {
                if (TreeType == "Ever green")
                    return false;
                if (TreeType == "Deciduous")
                    return true;
                throw new Exception("Invalid tree type specified");
            }
        }
        /// <summary>Distance between tree rows (years)</summary>
        [Separator("Orchid Information")]
        [Description("Row spacing (m)")]
        [Units("m")]
        public double RowSpacing { get; set; }

        /// <summary>Distance between trees within rows (years)</summary>
        [Description("InterRow spacing (m)")]
        [Units("m")]
        public double InterRowSpacing { get; set; }

        /// <summary>Proportion of the row width taken up by alley zone(0-1)</summary>
        [Description("Alley Zone Width (Proportion of the row width taken up by alley zone(0-1))")]
        [Units("0-1")]
        public double AlleyZoneWidthFrac { get; set; }

        /// <summary>Width of the alley zone between tree rows (0-1)</summary>
        [Units("0-1")]
        public double AlleyZoneWidth
        {
            get
            {
                if ((AlleyZoneWidthFrac >= 1) || (AlleyZoneWidthFrac < 0))
                    throw new Exception("Alley zone width fraction must be greater than zero and less than 1");
                return RowSpacing * AlleyZoneWidthFrac;
            }
        }

        /// <summary>Width of the zone trees are planted in (m)</summary>
        [Units("m")]
        public double RowZoneWidth
        {
            get
            {
                if (AlleyZoneWidth > RowSpacing)
                    throw new Exception("Alley Zone Width can not exceed Row spacing");
                return RowSpacing * (1-AlleyZoneWidthFrac);
            }
        }

        /// <summary>Tree population density (/ha)</summary>
        [Units("/ha)")]
        public double TreePopulation
        { 
            get
            {
                return 10000 / (RowSpacing * InterRowSpacing);
            }
        }

        /// <summary>
        /// The Area of the tree canopy at maximum width.  Assums canopy remains touching within rows
        /// </summary>
        [Units("m2")]
        public double TreeCanopyArea
        {
            get 
            {
                return MaxWidth/1000 * InterRowSpacing;
            }
        }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Tree Age At Start of Simulation (years)")]
        public double AgeAtSimulationStart { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Years from planting to reach Maximum dimension (years)")]
        public int YearsToMaxDimension { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Trunk mass when maximum dimension reached (kg/tree)")]
        public double TrunkMassAtMaxDimension { get; set; }

        /// <summary>Wood density</summary>
        [Description("WoodDensity (g/l)")]
        public double WoodDensity { get; set; }

        /// <summary>Bud Break (Days after Winter Solstice)</summary>
        [Separator("Tree Phenology.  Specify when canopy stages occur in days since the winter solstice")]
        [Description("Bud Break (Days after Winter Solstice)")]
        public int BudBreakDAWS { get; set; }

        /// <summary>Start Full Canopy (Days after Winter Solstice)</summary>
        [Description("Start Full Canopy (Days after Winter Solstice)")]
        public int StartFullCanopyDAWS { get; set; }

        /// <summary>Start of leaf fall (Days after Winter Solstice)</summary>
        [Description("Start of leaf fall (Days after Winter Solstice)")]
        public int StartLeafFallDAWS { get; set; }

        /// <summary>Start Fruit Growth (Days after Winter Solstice)</summary>
        [Description("End of Leaf fall (Days after Winter Solstice)")]
        public int EndLeafFallDAWS { get; set; }

        /// <summary>Grow roots into neighbouring zone (yes or no)</summary>
        [Separator("Tree Dimnesions")]
        [Description("Grow roots into neighbouring zone (yes or no)")]
        public bool GRINZ { get; set; }

        /// <summary>Root depth at harvest (mm)</summary>
        [Description("Root depth when mature (mm)")]
        public double MaxRD { get; set; }

        /// <summary>Root Biomass proportion (0-1)</summary>
        [Description("Root Biomass proportion (0-1)")]
        public double Proot { get; set; }

        /// <summary>Leaf Biomass proportion (0-1)</summary>
        [Description("Leaf Biomass proportion (0-1)")]
        public double Pleaf { get; set; }

        /// <summary>Trunk Biomass proportion (0-1)</summary>
        [Description("Trunk Biomass proportion (0-1)")]
        public double Ptrunk { get; set; }

        /// <summary>Hight of the bottom of the canop (mm)</summary>
        [Description("Hight of the bottom of the canopy (mm)")]
        public double CanopyBaseHeight { get; set; }

        /// <summary>Hight of mature tree after pruning (mm)</summary>
        [Description("Hight of mature tree after pruning (mm)")]
        public double MaxPrunedHeight { get; set; }

        /// <summary>maximum hight of mature tree before pruning (mm)</summary>
        [Description("maximum hight of mature tree before pruning (mm)")]
        public double MaxHeight { get; set; }

        /// <summary>Width of mature tree after pruning (mm)</summary>
        [Description("Width of mature tree after pruning  (mm)")]
        public double MaxPrunedWidth { get; set; }

        /// <summary>Width of mature tree before pruning (mm)</summary>
        [Description("Width of mature tree before pruning (mm)")]
        public double MaxWidth { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Separator("Tree Nitrogen contents")]
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

        /// <summary>Winter cover of tree canopy (0-1)</summary>
        [Description("Winter cover of tree canopy (0-1)")]
        public double BaseCover { get; set; }

        /// <summary>Maximum cover of tree canopy (0-1)</summary>
        [Description("Maximum cover of tree canopy (0-1)")]
        public double MaxCover { get; set; }

        /// <summary>Radiation Use Efficiency</summary>
        [Description("Radiation use efficiency (g/MJ)")]
        public double RUE { get; set; }

        /// <summary>Maximum canopy conductance (between 0.001 and 0.016) </summary>
        [Separator("Water demand and response")]
        [Description("Maximum canopy conductance (between 0.001 and 0.016)")]
        public double GSMax { get; set; }

        /// <summary>Net radiation at 50% of maximum conductance (between 50 and 200)</summary>
        [Description("Net radiation at 50% of maximum conductance (between 50 and 200)")]
        public double R50 { get; set; }

        /// <summary>"Does the crop respond to water stress?"</summary>
        [Description("Does the crop respond to water stress?")]
        public bool WaterStress { get; set; }

        /// <summary>Fruit number retained (per tree)</summary>
        [Separator("Fruit parameters")]

        [Description("Fruit number retained (per m2 tree canopy area)")]
        public int Number { get; set; }

        /// <summary>Potential Fruit Fresh Wt </summary>
        [Description("Potential Fruit Fresh Wt (g)")]
        public double PotentialFWPerFruit { get; set; }

        /// <summary>Fruit DM conc </summary>
        [Description("Product DM conc (g/g)")]
        public double DMC { get; set; }

        /// <summary>Fruit Density </summary>
        [Description("Fruit Density (g/cm3)")]
        public double FruitDensity { get; set; }

        /// <summary> Date of year max bloom occurs </summary>
        [Description("Date (d-mmm) of maximum bloom")]
        public string DateMaxBloom { get; set; }

        /// <summary>Start Linear Growth </summary>
        [Description("Start Linear Growth (Days After maximum bloom)")]
        public int DAFStartLinearGrowth { get; set; }

        /// <summary>End Linear Growth </summary>
        [Description("End Linear Growth (Days After maximum bloom)")]
        public int DAFEndLinearGrowth { get; set; }

        /// <summary>Max Size </summary>
        [Description("Max size (Days After maximum bloom)")]
        public int DAFMaxSize {get; set;}

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant strum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link(Type = LinkType.Scoped)]
        private Soil soil = null;

        [Link]
        private ISummary summary = null;

        [Link(Type = LinkType.Scoped)]
        private RootNetwork root = null;

        
        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        /// <summary>The zones in the simulation/// </summary>
        //List<Zone> zones = null;

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar tree = null;

        /// <summary>Total daily solar radiation available per tree/// </summary>
        [Units("MJ per m tree")]
        public double TotalSolarRadiation { get; set; }

        private bool hasAlleyZone = false;
        private bool hasRowZone = false;

        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"SpringDormancy","[STRUM].Phenology.SpringDormancy.DAWStoProgress = " },
            {"CanopyExpansion","[STRUM].Phenology.CanopyExpansion.DAWStoProgress = " },
            {"FullCanopy","[STRUM].Phenology.FullCanopy.DAWStoProgress = " },
            {"LeafFall", "[STRUM].Phenology.LeafFall.DAWStoProgress = " },
            {"MaxRootDepth","[STRUM].Root.Network.MaximumRootDepth.FixedValue = "},
            {"Proot","[STRUM].Root.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"Pleaf","[STRUM].Leaf.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"Ptrunk","[STRUM].Trunk.TotalCarbonDemand.TotalDMDemand.Available.PartitionFraction.FixedValue = " },
            {"MaxPrunedHeight","[STRUM].MaxPrunedHeight.FixedValue = " },
            {"CanopyBaseHeight","[STRUM].Height.CanopyBaseHeight.Maximum.FixedValue = " },
            {"MaxSeasonalHeight","[STRUM].Height.SeasonalGrowth.Maximum.FixedValue = " },
            {"MaxPrunedWidth","[STRUM].Width.PrunedWidth.Maximum.FixedValue = "},
            {"MaxSeasonalWidth","[STRUM].Width.SeasonalGrowth.Maximum.FixedValue = " },
            {"ProductNConc","[STRUM].Fruit.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"ResidueNConc","[STRUM].Leaf.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"WoodDensity","[STRUM].Trunk.EnergyBalance.DeadAreaIndex.VolumePerM2ofTreeArea.Density.FixedValue = " },
            {"RootNConc","[STRUM].Root.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"WoodNConc","[STRUM].Trunk.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"ExtinctCoeff","[STRUM].Leaf.Canopy.GreenExtinctionCoefficient.UnstressedCoeff.FixedValue = "},
            {"BaseLAI","[STRUM].Leaf.Canopy.GreenAreaIndex.Winter.BaseArea.FixedValue = " },
            {"AnnualDeltaLAI","[STRUM].Leaf.Canopy.GreenAreaIndex.SeasonalGrowth.AnnualDelta.FixedValue = " },
            {"DecidiousSenescence","[STRUM].Leaf.SenescenceRate.DecidiousSensecence.LeafFall.MultiplyFunction.Switch.FixedValue = " },
            {"EverGreenSenescence", "[STRUM].Leaf.SenescenceRate.EvergreenSenescence.Coefficient.FixedValue = "},
            {"GSMax","[STRUM].Leaf.Canopy.Gsmax350 = " },
            {"R50","[STRUM].Leaf.Canopy.R50 = " },
            {"RUE","[STRUM].Leaf.Photosynthesis.RUE.FixedValue = " },
            {"InitialTrunkWt","[STRUM].Trunk.InitialWt.FixedValue = "},
            {"InitialRootWt", "[STRUM].Root.InitialWt.FixedValue = " },
            {"InitialFruitWt","[STRUM].Fruit.InitialWt.FixedValue = "},
            {"InitialLeafWt", "[STRUM].Leaf.InitialWt.FixedValue = " },
            {"YearsToMaturity","[STRUM].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"TrunkWtAtMaturity","[STRUM].Trunk.MatureWt.FixedValue = " },
            {"YearsToMaxRD","[STRUM].Root.Network.RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"Number","[STRUM].Fruit.Number.RetainedPostThinning.FixedValue = " },
            {"FruitDensity","[STRUM].Fruit.Density.FixedValue = " },
            {"DryMatterContent", "[STRUM].Fruit.MinimumDMC.FixedValue = " },
            {"DateMaxBloom","[STRUM].Phenology.DaysSinceFlowering.StartDate = "},
            {"DAFStartLinearGrowth","[STRUM].Fruit.TotalCarbonDemand.RelativeFruitMass.Delta.Integral.XYPairs.X[2] = "},
            {"DAFEndLinearGrowth","[STRUM].Fruit.TotalCarbonDemand.RelativeFruitMass.Delta.Integral.XYPairs.X[3] = "},
            {"DAFMaxSize","[STRUM].Fruit.TotalCarbonDemand.RelativeFruitMass.Delta.Integral.XYPairs.X[4] = "},
            {"WaterStressPhoto","[STRUM].Leaf.Photosynthesis.Fw.XYPairs.Y[1] = "},
            {"WaterStressExtinct","[STRUM].Leaf.Canopy.GreenExtinctionCoefficient.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[STRUM].Root.Network.NUptakeSWFactor.XYPairs.Y[1] = "},
            {"PotentialFWPerFruit","[STRUM].Fruit.PotentialFWPerFruit.FixedValue = " },
            {"RowWidth","[STRUM].RowWidth.FixedValue = " },
            {"InterRowSpacing","[STRUM].InterRowSpacing.FixedValue = " }
        };

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish()
        {
            double soilDepthMax = 0;
            
            var soilCrop = soil.FindDescendant<SoilCrop>(strum.Name + "Soil");
            var physical = soil.FindDescendant<Physical>("Physical");
            if (soilCrop == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {strum.Name}Soil");

            double[] xf = soilCrop.XF;

            // Limit root depth for impeded layers
            for (int i = 0; i < physical.Thickness.Length; i++)
            {
                if (xf[i] > 0)
                    soilDepthMax += physical.Thickness[i];
                else
                    break;
            }

            double rootDepth = Math.Min(MaxRD, soilDepthMax);
            if (GRINZ)
            {  //Must add root zone prior to sowing the crop.  For some reason they (silently) dont add if you try to do so after the crop is established
                string neighbour = "";
                List<Zone> zones = simulation.FindAllChildren<Zone>().ToList();
                if (zones.Count > 2)
                    throw new Exception("Strip crop logic only set up for 2 zones, your simulation has more than this");
                if (zones.Count > 1)
                {
                    foreach (Zone z in zones)
                    {
                        if (z.Name != zone.Name)
                            neighbour = z.Name;
                    }
                    root.ZoneNamesToGrowRootsIn.Add(neighbour);
                    //root.ZoneRootDepths.Add(rootDepth);
                    NutrientPoolFunctions InitialDM = new NutrientPoolFunctions();
                    Constant InitStruct = new Constant();
                    InitStruct.FixedValue = 10;
                    InitialDM.Structural = InitStruct;
                    Constant InitMetab = new Constant();
                    InitMetab.FixedValue = 0;
                    InitialDM.Metabolic = InitMetab;
                    Constant InitStor = new Constant();
                    InitStor.FixedValue = 0;
                    InitialDM.Storage = InitStor;
                    //root.ZoneInitialDM.Add(InitialDM);
                }
            }

            string cropName = this.Name;
            double depth = Math.Min(this.MaxRD * this.AgeAtSimulationStart / this.YearsToMaxDimension, rootDepth);
            double population = 1.0;
            double rowWidth = 0.0;

            tree = CoeffCalc();
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
        public Cultivar CoeffCalc()
        {
            Dictionary<string, string> treeParams = new Dictionary<string, string>(blankParams);

            if (this.WaterStress)
            {
                treeParams["WaterStressPhoto"] += "0.0";
                //treeParams["WaterStressCover"] += "0.2";
                treeParams["WaterStressExtinct"] += "0.2"; 
                treeParams["WaterStressNUptake"] += "0.0";
            }
            else
            {
                treeParams["WaterStressPhoto"] += "1.0";
                //treeParams["WaterStressCover"] += "1.0";
                treeParams["WaterStressExtinct"] += "1.0";
                treeParams["WaterStressNUptake"] += "1.0";
            }

            treeParams["SpringDormancy"] += BudBreakDAWS.ToString();
            treeParams["CanopyExpansion"] += StartFullCanopyDAWS.ToString();
            treeParams["FullCanopy"] += StartLeafFallDAWS.ToString();
            treeParams["LeafFall"] += EndLeafFallDAWS.ToString();
            treeParams["MaxRootDepth"] += MaxRD.ToString();
            treeParams["Proot"] += Proot.ToString();
            treeParams["Pleaf"] += Pleaf.ToString();
            treeParams["Ptrunk"] += Ptrunk.ToString();
            treeParams["WoodDensity"] += WoodDensity.ToString();
            treeParams["MaxPrunedHeight"] += MaxPrunedHeight.ToString();
            treeParams["CanopyBaseHeight"] += CanopyBaseHeight.ToString();
            treeParams["MaxSeasonalHeight"] += (MaxHeight - MaxPrunedHeight).ToString();
            treeParams["MaxPrunedWidth"] += MaxPrunedWidth.ToString();
            treeParams["MaxSeasonalWidth"] += (MaxWidth - MaxPrunedWidth).ToString();
            treeParams["ProductNConc"] += FruitNConc.ToString();
            treeParams["ResidueNConc"] += LeafNConc.ToString();
            treeParams["RootNConc"] += RootNConc.ToString();
            treeParams["WoodNConc"] += TrunkNConc.ToString();
            treeParams["ExtinctCoeff"] += ExtinctCoeff.ToString();
            treeParams["BaseLAI"] += ((Math.Log(1 - BaseCover) / (ExtinctCoeff * -1))).ToString();
            treeParams["AnnualDeltaLAI"] += ((Math.Log(1 - (MaxCover)) / (ExtinctCoeff * -1)) - (Math.Log(1 - BaseCover) / (ExtinctCoeff * -1))).ToString();
            treeParams["DecidiousSenescence"] += (Decidious ? 1 : 0);
            treeParams["EverGreenSenescence"] += (Decidious ? 0 : 1);
            treeParams["GSMax"] += GSMax.ToString();
            treeParams["R50"] += R50.ToString();
            treeParams["RUE"] += RUE.ToString();
            treeParams["YearsToMaturity"] += YearsToMaxDimension.ToString();
            treeParams["TrunkWtAtMaturity"] += (TrunkMassAtMaxDimension * 1000).ToString();
            treeParams["YearsToMaxRD"] += YearsToMaxDimension.ToString();
            treeParams["Number"] += (Number*TreeCanopyArea).ToString();
            treeParams["FruitDensity"] += FruitDensity.ToString();
            treeParams["DryMatterContent"] += DMC.ToString();
            treeParams["DateMaxBloom"] += DateMaxBloom;
            treeParams["DAFStartLinearGrowth"] += DAFStartLinearGrowth.ToString();
            treeParams["DAFEndLinearGrowth"] +=  DAFEndLinearGrowth.ToString();
            treeParams["DAFMaxSize"] += DAFMaxSize.ToString();
            treeParams["PotentialFWPerFruit"] += PotentialFWPerFruit.ToString();


            if (hasAlleyZone)
            {
                treeParams["RowWidth"] += (RowZoneWidth + AlleyZoneWidth).ToString();
                treeParams["InterRowSpacing"] += InterRowSpacing.ToString();
            }
            else
            {
                treeParams["RowWidth"] += RowZoneWidth.ToString();
                treeParams["InterRowSpacing"] += InterRowSpacing.ToString();
            }
            


            if (AgeAtSimulationStart <= 0)
                throw new Exception("SPRUMtree needs to have a 'Tree Age at start of Simulation' > 1 years");
            if (TrunkMassAtMaxDimension <= 0)
                throw new Exception("SPRUMtree needs to have a 'Trunk Mass at maximum dimension > 0");
            double relativeInitialSize = Math.Min(1,(double)AgeAtSimulationStart / (double)YearsToMaxDimension);
            treeParams["InitialTrunkWt"] += (relativeInitialSize * TrunkMassAtMaxDimension * 1000).ToString();
            treeParams["InitialRootWt"] += (relativeInitialSize * TrunkMassAtMaxDimension * 150).ToString();
            treeParams["InitialFruitWt"] += (0).ToString();
            treeParams["InitialLeafWt"] += (relativeInitialSize * TrunkMassAtMaxDimension * 400 * (Decidious ? 0 : 1 )).ToString();
                
            string[] commands = new string[treeParams.Count];
            treeParams.Values.CopyTo(commands, 0);

            Cultivar TreeValues = new Cultivar(this.Name, commands);
            return TreeValues;
        }

        private void SetUpZones()
        {
            List<Zone> zones = simulation.FindAllChildren<Zone>().ToList();
            foreach (Zone z in zones)
            {
                if (z.Name == "Row")
                    hasRowZone = true;
                if (z.Name == "Alley")
                    hasAlleyZone = true;
            }
            if (hasRowZone == false)
                throw new Exception("Strum tree instance must be in a zone named Row");

            simulation.Set("[Row].CanopyType", (object)"TreeRow");

            if (hasAlleyZone != false)
            {
                simulation.Set("[Row].Width", (object)RowZoneWidth);
                simulation.Set("[Row].Length", (object)InterRowSpacing);
                simulation.Set("[Alley].Width", (object)AlleyZoneWidth);
                simulation.Set("[Alley].Length", (object)InterRowSpacing);
            }
            else
            {
                simulation.Set("[Row].Width", (object)RowZoneWidth);
                simulation.Set("[Row].Length", (object)InterRowSpacing);
            }


        }
        
        [EventSubscribe("StartOfSimulation")]
        private void OnStartSimulation(object sender, EventArgs e)
        {
            SetUpZones();
            Establish();
        }
 
            



            

        
       

    }
}
