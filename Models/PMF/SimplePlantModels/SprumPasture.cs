using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing.Charts;
using Models.Climate;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Soils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    public class SprumPasture: Model
    {
        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Separator("Pasture Age")]
        [Description("Age At Start of Simulation (years)")]
        public double AgeAtSimulationStart { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Years from planting to reach Maximum root depth (years)")]
        public int YearsToMaxDimension { get; set; }

        /// <summary>Maximum growth rate of pasture (g/m2/oCd)</summary>
        [Separator("Pasture growth")]
        [Description("Maximum growth rate of pasture (g/m2/oCd)")]
        public double PotentialGrowthRate { get; set; }

        /// <summary>Root Biomass proportion (0-1)</summary>
        [Description("Root Biomass proportion (0-1)")]
        public double Proot { get; set; }

        /// <summary>Root depth (mm)</summary>
        [Separator("Plant Dimnesions")]
        [Description("Root depth (mm)")]
        public double MaxRD { get; set; }

        /// <summary>Pasture height at grazing (mm)</summary>
        [Description("Pasture height at grazing (mm)")]
        public double MaxHeight { get; set; }

        /// <summary>Pasture height after grazing (mm)</summary>
        [Description("Pasture height after grazing(mm)")]
        public double MaxPrunedHeight { get; set; }

        /// <summary>Grow roots into neighbouring zone (yes or no)</summary>
        [Description("Grow roots into neighbouring zone (yes or no)")]
        public bool RootThyNeighbour { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Separator("Plant Nitrogen contents")]
        [Description("Root Nitrogen concentration (g/g)")]
        public double RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Leaf Nitrogen concentration (g/g)")]
        public double LeafNConc { get; set; }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Residue Nitrogen concentration(g/g)")]
        public double ResidueNConc { get; set; }

        /// <summary>Maximum canopy conductance (between 0.001 and 0.016) </summary>
        [Description("Maximum canopy conductance (between 0.001 and 0.016)")]
        public double GSMax { get; set; }

        /// <summary>Net radiation at 50% of maximum conductance (between 50 and 200)</summary>
        [Description("Net radiation at 50% of maximum conductance (between 50 and 200)")]
        public double R50 { get; set; }

        /// <summary>Proportion of pasture mass that is leguem (0-1)</summary>
        [Description("Proportion of pasture mass that is leguem (0-1)")]
        public double LegumePropn { get; set; }


        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant sprum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link(Type = LinkType.Scoped)]
        private Soil soil = null;

        [Link]
        private ISummary summary = null;

        [Link(Type = LinkType.Scoped)]
        private Root root = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SPRUM pasture/// </summary>
        private Cultivar pasture = null;

        
        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"YearsToMaturity","[SPRUM].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"YearsToMaxRD","[SPRUM].Root.RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"PotentialGrowthRate","[SPRUM].Leaf.Photosynthesis.Potential.g_per_oCd.FixedValue = "},
            {"Proot","[SPRUM].Root.DMDemands.Structural.DMDemandFunction.PartitionFraction.FixedValue = " },
            {"HightOfRegrowth","[SPRUM].Height.SeasonalPattern.HightOfRegrowth.MaxHeightFromRegrowth.FixedValue = "},
            {"MaxPrunedHeight","[SPRUM].Height.SeasonalPattern.PostGrazeHeight.FixedValue ="},
            {"MaxRootDepth","[SPRUM].Root.MaximumRootDepth.FixedValue = "},
            {"ResidueNConc","[SPRUM].Residue.MaximumNConc.FixedValue = "},
            {"LeafNConc","[SPRUM].Leaf.MaximumNConc.FixedValue = "},
            {"RootNConc","[SPRUM].Root.MaximumNConc.FixedValue = "},
            {"GSMax","[SPRUM].Leaf.Gsmax350 = " },
            {"R50","[SPRUM].Leaf.R50 = " },
            {"LegumePropn","[SPRUM].LegumePropn.FixedValue = "},
        };

        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish()
        {
            double soilDepthMax = 0;
            
            var soilCrop = soil.FindDescendant<SoilCrop>(sprum.Name + "Soil");
            var physical = soil.FindDescendant<Physical>("Physical");
            if (soilCrop == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {sprum.Name}Soil");

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
            if (RootThyNeighbour)
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
                    root.ZoneRootDepths.Add(rootDepth);
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
                    root.ZoneInitialDM.Add(InitialDM);
                }
            }

            string cropName = this.Name;
            double depth = Math.Min(this.MaxRD * this.AgeAtSimulationStart / this.YearsToMaxDimension, rootDepth);
            double population = 1.0;
            double rowWidth = 0.0;

            pasture = coeffCalc();
            sprum.Children.Add(pasture);
            sprum.Sow(cropName, population, depth, rowWidth);
            phenology.SetAge(AgeAtSimulationStart);
            summary.WriteMessage(this,"Some of the message above is not relevent as SPRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + this.Name + " is established "
                ,MessageType.Information); 
        }

        /// <summary>
        /// Data structure that holds STRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc()
        {
            Dictionary<string, string> pastureParams = new Dictionary<string, string>(blankParams);

            pastureParams["YearsToMaturity"] += YearsToMaxDimension.ToString();
            pastureParams["YearsToMaxRD"] += YearsToMaxDimension.ToString();
            pastureParams["PotentialGrowthRate"] += PotentialGrowthRate.ToString();
            pastureParams["Proot"] += Proot.ToString();
            pastureParams["HightOfRegrowth"] += (MaxHeight- MaxPrunedHeight).ToString();
            pastureParams["MaxPrunedHeight"] += MaxPrunedHeight.ToString();
            pastureParams["MaxRootDepth"] += MaxRD.ToString();
            pastureParams["ResidueNConc"] += ResidueNConc.ToString();
            pastureParams["LeafNConc"] += LeafNConc.ToString();
            pastureParams["RootNConc"] += RootNConc.ToString();
            pastureParams["GSMax"] += GSMax.ToString();
            pastureParams["R50"] += R50.ToString();
            pastureParams["LegumePropn"] += LegumePropn.ToString();

                
            string[] commands = new string[pastureParams.Count];
            pastureParams.Values.CopyTo(commands, 0);

            Cultivar PastureValues = new Cultivar(this.Name, commands);
            return PastureValues;
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
