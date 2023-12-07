using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Functions;
using Models.Interfaces;
using Models.Management;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Soils;
using Models.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Dynamic Environmental Response Of Phenology And Potential Yield
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class DEROPAPY : Model, IGridModel
    {
        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")]
        public string CoeffientFile { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Description("Name of the crop to in simulation")]
        public string CropName { get; set; }

        [JsonIgnore] private DataTable CropCoeffs { get; set; }

        ///<summary></summary> 
        [JsonIgnore] public string[] ParamName { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] ParamUnit { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] Description { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] Maize { get; set; }
        ///<summary></summary> 
        [JsonIgnore] public string[] Apple { get; set; }

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant deropapy = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        //[Link(Type = LinkType.Scoped, ByName = true)]
        //private Weather weather = null;

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

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar derochild = null;


        private void setCropCoefficients()
        {
            Simulation sim = (Simulation)this.FindAllAncestors<Simulation>().FirstOrDefault();
            string fullFileName = PathUtilities.GetAbsolutePath(CoeffientFile, sim.FileName);

            ApsimTextFile textFile = new ApsimTextFile();
            textFile.Open(fullFileName);
            CropCoeffs = textFile.ToTable();
            textFile.Close();

            ParamName = repack(CropCoeffs, 0);
            ParamUnit = repack(CropCoeffs, 1);
            Description = repack(CropCoeffs, 2);
            Maize = repack(CropCoeffs, 3);
            Apple = repack(CropCoeffs, 4);
        }

        private string[] repack(DataTable tab, int colIndex)
        {
            string[] ret = new string[tab.Rows.Count];
            for (int i = 0; i < tab.Rows.Count; i++)
            {
                ret[i] = tab.Rows[i][colIndex].ToString();
            }
            return ret;
        }


        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                setCropCoefficients();

                List<GridTableColumn> columns = new List<GridTableColumn>();

                foreach (DataColumn col in CropCoeffs.Columns)
                {
                    columns.Add(new GridTableColumn(col.ColumnName, new VariableProperty(this, GetType().GetProperty(col.ColumnName))));
                }

                List<GridTable> tables = new List<GridTable>();
                tables.Add(new GridTable(Name, columns, this));

                return tables;
            }
        }

        /// <summary>
        /// Renames column headers for display
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            dt.Columns["ParamName"].ColumnName = "Param Name";
            dt.Columns["ParamUnit"].ColumnName = "Units";
            dt.Columns["Description"].ColumnName = "Description";
            dt.Columns["Maize"].ColumnName = "Maize";
            dt.Columns["Apple"].ColumnName = "Apple";
            return dt;
        }

        /// <summary>
        /// Renames the columns back to model property names
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            dt.Columns["Param Name"].ColumnName = "ParamName";
            dt.Columns["Units"].ColumnName = "ParamUnit";
            dt.Columns["Description"].ColumnName = "Description";
            dt.Columns["Maize"].ColumnName = "Maize";
            dt.Columns["Apple"].ColumnName = "Apple";
            return dt;
        }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartSimulation(object sender, EventArgs e)
        {
            Establish();
        }

        private bool RootThyNeighbour = false;
        private double MaxRD = 3000;
        private double AgeAtSimulationStart = 1.0;
        private double YearsToMaxDimension = 1.0;

        /// <summary> Method that sets DEROPAPY running</summary>
        public void Establish()
        {
            double soilDepthMax = 0;

            var soilCrop = soil.FindDescendant<SoilCrop>(deropapy.Name + "Soil");
            var physical = soil.FindDescendant<Physical>("Physical");
            if (soilCrop == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {deropapy.Name}Soil");

            double[] xf = soilCrop.XF;

            // Limit root depth for impeded layers
            for (int i = 0; i < physical.Thickness.Length; i++)
            {
                if (xf[i] > 0)
                    soilDepthMax += physical.Thickness[i];
                else
                    break;
            }

            //double rootDepth = Math.Min(MaxRD, soilDepthMax);
            double rootDepth = 1500;
            
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

            derochild = coeffCalc();
            deropapy.Children.Add(derochild);
            deropapy.Sow(cropName, population, depth, rowWidth);
            phenology.SetAge(AgeAtSimulationStart);
            summary.WriteMessage(this, "Some of the message above is not relevent as STRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + this.Name + " is established as " + this.AgeAtSimulationStart.ToString() + " Year old plant "
                , MessageType.Information); 
        }


        /// <summary>
        /// Data structure that holds STRUM parameter names and the cultivar overwrite they map to
        /// </summary>
        public Cultivar coeffCalc()
        {
            Dictionary<string, string> treeParams = new Dictionary<string, string>(blankParams);

            /*
            treeParams["SpringDormancy"] += BudBreakDAWS.ToString();
            treeParams["CanopyExpansion"] += StartFullCanopyDAWS.ToString();
            treeParams["FullCanopy"] += StartLeafFallDAWS.ToString();
            treeParams["LeafFall"] += EndLeafFallDAWS.ToString();
            pruneDAWS = EndLeafFallDAWS;
            treeParams["MaxRootDepth"] += MaxRD.ToString();
            treeParams["Proot"] += Proot.ToString();
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
            treeParams["MaxLAI"] += ((Math.Log(1 - MaxCover) / (ExtinctCoeff * -1))).ToString();
            treeParams["GSMax"] += GSMax.ToString();
            treeParams["R50"] += R50.ToString();
            treeParams["YearsToMaturity"] += YearsToMaxDimension.ToString();
            treeParams["YearsToMaxRD"] += YearsToMaxDimension.ToString();
            treeParams["Number"] += Number.ToString();
            treeParams["Size"] += Size.ToString();
            treeParams["Density"] += Density.ToString();
            treeParams["DryMatterContent"] += DMC.ToString();
            treeParams["DAWSMaxBloom"] += DAWSMaxBloom.ToString();
            treeParams["DAWSLinearGrowth"] += DAWSLinearGrowth.ToString();
            treeParams["DAWSEndLinearGrowth"] += ((int)(DAWSLinearGrowth + (DAWSMaxSize - DAWSLinearGrowth) * .6)).ToString();
            treeParams["DAWSMaxSize"] += DAWSMaxSize.ToString();
            harvestDAWS = DAWSMaxSize;


            if (AgeAtSimulationStart <= 0)
                throw new Exception("SPRUMtree needs to have a 'Tree Age at start of Simulation' > 1 years");
            if (TrunkMassAtMaxDimension <= 0)
                throw new Exception("SPRUMtree needs to have a 'Trunk Mass at maximum dimension > 0");
            treeParams["InitialTrunkWt"] += ((double)AgeAtSimulationStart / (double)YearsToMaxDimension * TrunkMassAtMaxDimension * 100).ToString();
            treeParams["InitialRootWt"] += ((double)AgeAtSimulationStart / (double)YearsToMaxDimension * TrunkMassAtMaxDimension * 40).ToString();
            treeParams["InitialFruitWt"] += (0).ToString();
            treeParams["InitialLeafWt"] += (0).ToString();
            */

            string[] commands = new string[treeParams.Count];
            treeParams.Values.CopyTo(commands, 0);
            Cultivar TreeValues = new Cultivar(this.Name, commands);
            return TreeValues;
        }

        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"SpringDormancy","[STRUM].Phenology.SpringDormancy.DAWStoProgress = " },
            {"CanopyExpansion","[STRUM].Phenology.CanopyExpansion.DAWStoProgress = " },
            {"FullCanopy","[STRUM].Phenology.FullCanopy.DAWStoProgress = " },
            {"LeafFall", "[STRUM].Phenology.LeafFall.DAWStoProgress = " },
            {"MaxRootDepth","[STRUM].Root.MaximumRootDepth.FixedValue = "},
            {"Proot","[STRUM].Root.DMDemands.Structural.DMDemandFunction.PartitionFraction.AcitveGrowth.Constant.FixedValue = " },
            {"MaxPrunedHeight","[STRUM].MaxPrunedHeight.FixedValue = " },
            {"CanopyBaseHeight","[STRUM].Height.CanopyBaseHeight.Maximum.FixedValue = " },
            {"MaxSeasonalHeight","[STRUM].Height.SeasonalGrowth.Maximum.FixedValue = " },
            {"MaxPrunedWidth","[STRUM].Width.PrunedWidth.Maximum.FixedValue = "},
            {"MaxSeasonalWidth","[STRUM].Width.SeasonalGrowth.Maximum.FixedValue = " },
            {"ProductNConc","[STRUM].Fruit.MaxNConcAtHarvest.FixedValue = "},
            {"ResidueNConc","[STRUM].Leaf.MaxNConcAtStartLeafFall.FixedValue = "},
            {"RootNConc","[STRUM].Root.MaximumNConc.FixedValue = "},
            {"WoodNConc","[STRUM].Trunk.MaximumNConc.FixedValue = "},
            {"ExtinctCoeff","[STRUM].Leaf.ExtinctionCoefficient.UnstressedCoeff.FixedValue = "},
            {"MaxLAI","[STRUM].Leaf.Area.Maximum.FixedValue = " },
            {"GSMax","[STRUM].Leaf.Gsmax350 = " },
            {"R50","[STRUM].Leaf.R50 = " },
            {"InitialTrunkWt","[STRUM].Trunk.InitialWt.Structural.FixedValue = "},
            {"InitialRootWt", "[STRUM].Root.InitialWt.Structural.FixedValue = " },
            {"InitialFruitWt","[STRUM].Fruit.InitialWt.Structural.FixedValue = "},
            {"InitialLeafWt", "[STRUM].Leaf.InitialWt.FixedValue = " },
            {"YearsToMaturity","[STRUM].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"YearsToMaxRD","[STRUM].Root.RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"Number","[STRUM].Fruit.Number.RetainedPostThinning.FixedValue = " },
            {"Size","[STRUM].Fruit.MaximumSize.FixedValue = " },
            {"Density","[STRUM].Fruit.Density.FixedValue = " },
            {"DryMatterContent", "[STRUM].Fruit.MinimumDMC.FixedValue = " },
            {"DAWSMaxBloom","[STRUM].Fruit.DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[2] = "},
            {"DAWSLinearGrowth","[STRUM].Fruit.DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[3] = "},
            {"DAWSEndLinearGrowth","[STRUM].Fruit.DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[4] = "},
            {"DAWSMaxSize","[STRUM].Fruit.DMDemands.Structural.RelativeFruitMass.Delta.Integral.XYPairs.X[5] = "},
            {"WaterStressPhoto","[STRUM].Leaf.Photosynthesis.Fw.XYPairs.Y[1] = "},
            {"WaterStressExtinct","[STRUM].Leaf.ExtinctionCoefficient.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[STRUM].Root.NUptakeSWFactor.XYPairs.Y[1] = "},
        };
    }
}
