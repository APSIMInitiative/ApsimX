using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Phen;
using Models.Soils;
using Models.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Dynamic Environmental Response Of Phenology And Potential Yield
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    public class DEROPAPY : Model
    {
        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")]
        [Display(Type = DisplayType.FileName)]
        public string CoefficientFile { get; set ; }
        
        
        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this.
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                Simulation simulation = FindAncestor<Simulation>();
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(this.CoefficientFile, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this.CoefficientFile, simulations.FileName);
                    else
                        return this.CoefficientFile;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    this.CoefficientFile = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this.CoefficientFile = value;
                readCSVandUpdateProperties();
            }
        }

        /// <summary>
        /// The Name of the crop from the CSV table to be grown in this simulation
        /// </summary>
        [Description("The Name of the crop from the CSV table to be grown in this simulation")]
        [Display(Type = DisplayType.CSVCrops)]
        public string CurrentCropName { get; set; }

        ///<summary></summary> 
        [JsonIgnore] public string[] ParamName { get; set; }

        /// <summary>
        /// List of crops specified in the CoefficientFile
        /// </summary>
        [JsonIgnore] public string[] CropNames { get; set; }

        ///<summary></summary> 
        [JsonIgnore] public Dictionary<string, string> CurrentCropParams { get; set; }

        ///<summary>The days after the winter solstice when the crop must end and rewind to the start of its cycle for next season</summary> 
        private int EndSeasonDAWS { get; set; }

        ///<summary>bool to indicate if crop has already done a phenology rewind this season</summary> 
        private bool HasRewondThisSeason { get; set; }

        ///<summary>bool to indicate if crop started growth this season</summary> 
        private bool HasStartedGrowthhisSeason { get; set; } = false;

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant deropapy = null;

        /// <summary>
        /// clock
        /// </summary>
        [Link]
        public Clock clock = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link(Type = LinkType.Scoped)]
        private Soil soil = null;

        [Link(Type = LinkType.Scoped)]
        private Weather weather = null;

        [Link]
        private ISummary summary = null;

        [Link(Type = LinkType.Scoped)]
        private RootNetwork root = null;

        [Link(Type = LinkType.Scoped)]
        private EnergyBalance canopy = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Organ leaf = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar derochild = null;

        private DataTable readData;

        ////// This secton contains the components that get values from the csv coefficient file to    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////// display in the grid view and set them back to the csv when they are changed in the grid !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        private DataTable readCSVandUpdateProperties()
        {
            readData = new DataTable();
            readData = ApsimTextFile.ToTable(FullFileName);

            foreach (DataColumn column in readData.Columns)
                column.ReadOnly = true;
            
            if (readData.Rows.Count == 0)
                throw new Exception("Failed to read any rows of data from " + FullFileName);
            if ((CurrentCropName != null)&&(CurrentCropName != ""))
            {
                CurrentCropParams = getCurrentParams(readData, CurrentCropName);
            }
            CropNames = readData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray().Skip(3).ToArray();
            return readData;
        }

        /// <summary>Gets or sets the table of values.</summary>
        [Display]
        public DataTable Data
        {
            get
            {
                readCSVandUpdateProperties();
                return readData;
            }
        }

        /// <summary>
        /// Gets the parameter set from the CoeffientFile for the CropName specified and returns in a dictionary maped to paramter names.
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="cropName"></param>
        /// <returns></returns>
        private Dictionary<string, string> getCurrentParams(DataTable tab, string cropName)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            for (int i = 0; i < tab.Rows.Count; i++)
            {
                ret.Add(tab.Rows[i]["ParamName"].ToString(), tab.Rows[i][cropName].ToString());
            }
            return ret;
        }

        ////// This secton contains the components take the coeffcient values and write them into the DEROPAPY !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////// instance to give a model parameterised with the values in the grid for the current simulation   !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if ((!deropapy.IsAlive) &&(CurrentCropName!="None"))
            {
                readCSVandUpdateProperties();
                Establish();
            }

            if ((weather.DaysSinceWinterSolstice==EndSeasonDAWS)&&(HasRewondThisSeason==false)&&(HasStartedGrowthhisSeason==true))
            {
                phenology.SetToStage((double)phenology.IndexFromPhaseName("EndOrHarvest")+1);
            }
        }

        /// <summary> Method that sets DEROPAPY running</summary>
        public void Establish()
        {
            EndSeasonDAWS = (int)Double.Parse(CurrentCropParams["D_EndGrowth"]);
            HasRewondThisSeason = false;

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

            double rootDepth = Math.Min(Double.Parse(CurrentCropParams["MaxRootDepth"]), soilDepthMax);

            bool RootsInNeighbourZone = bool.Parse(CurrentCropParams["RootsInNeighbourZone"]);
            if (RootsInNeighbourZone)
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

            double AgeAtSimulationStart = Double.Parse(CurrentCropParams["AgeAtStartSimulation"]);
            string cropName = this.Name;
            double depth = Math.Min(Double.Parse(CurrentCropParams["MaxRootDepth"]) * (AgeAtSimulationStart) / Double.Parse(CurrentCropParams["AgeToMaxDimension"]), rootDepth);
            double population = 1.0;
            double rowWidth = 0.0;

            derochild = coeffCalc();
            deropapy.Children.Add(derochild);
            deropapy.Sow(cropName, population, depth, rowWidth);
            phenology.SetAge(AgeAtSimulationStart);
            summary.WriteMessage(this, "Some of the message above is not relevent as DEROPAPY has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + this.Name + " is established as " + AgeAtSimulationStart.ToString() + " Year old plant "
                , MessageType.Information);
        }


        /// <summary>
        /// Procedures that occur for crops that go into the EndCrop Phase
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        [EventSubscribe("EndCrop")]
        private void onEndCrop(object sender, EventArgs e)
        {
            HasRewondThisSeason = true;
        }

        /// <summary>
        /// Procedures that occur for crops that go into the HarvestAndPrune Phase
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        [EventSubscribe("Harvesting")]
        private void onHarvesting(object sender, EventArgs e)
        {
            canopy.resetCanopy();
            HasRewondThisSeason = true;
            HasStartedGrowthhisSeason = false;
        }

        /// <summary>
        /// Procedures that occur when new growth cycle starts.  initial biomass etc
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("NewGrowthPhaseStarting")]
        private void onStartNewGrowthCycle(object sender, EventArgs e)
        {
            //Reset leaf biomass so it is ready for new growth
            if (CurrentCropParams["DefoliateOrDevelop"] == "FullCover")
            {
                leaf.initialiseBiomass();
                HasRewondThisSeason = false;
                HasStartedGrowthhisSeason = true;
            }
        }

        /// <summary>
        /// Procedures that occur for crops that go into the Graze Phase
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        [EventSubscribe("Grazing")]
        private void onGrazing(object sender, EventArgs e)
        {

        }



        /// <summary>
        /// Data structure that appends a parameter value to each address in the base deroParams dictionary 
        /// then writes them into a commands property in a Cultivar object.
        /// </summary>
        /// <returns>a Cultivar object with the overwrites set for the CropName selected using the parameters displayed 
        /// in the grid view that come from the CoeffientFile selected 
        /// </returns>
        public Cultivar coeffCalc()
        {
            Dictionary<string, string> thisDero = new Dictionary<string, string>(deroParams);

            thisDero["CropType"] += clean(CurrentCropParams["CropType"]);
            thisDero["TT_Temp_X"] += clean(CurrentCropParams["TT_Temp_X"]);
            thisDero["TT_Acc_Y"] += clean(CurrentCropParams["TT_Acc_Y"]);
            thisDero["D_StartGrowth_00"] += clean(CurrentCropParams["D_StartGrowth_00"]);
            thisDero["T_StartGrowth_00"] += clean(CurrentCropParams["T_StartGrowth_00"]);
            thisDero["Tt_Growing_01"] += clean(CurrentCropParams["Tt_Growing_01"]);
            thisDero["DefoliateOrDevelop"] += clean(CurrentCropParams["DefoliateOrDevelop"]);
            thisDero["Pp_FullCover_02"] += clean(CurrentCropParams["Pp_FullCover_02"]);
            thisDero["Tt_FullCover_02"] += clean(CurrentCropParams["Tt_FullCover_02"]);
            thisDero["Tt_Senescent_03"] += clean(CurrentCropParams["Tt_Senescent_03"]);
            thisDero["Tt_Mature_04"] += clean(CurrentCropParams["Tt_Mature_04"]);
            thisDero["EndOrHarvest"] += clean(CurrentCropParams["EndOrHarvest"]);
            thisDero["Chill_Temp_X"] += clean(CurrentCropParams["Chill_Temp_X"]);
            thisDero["Chill_Acc_Y"] += clean(CurrentCropParams["Chill_Acc_Y"]);
            thisDero["AC_Dormant_05"] += clean(CurrentCropParams["AC_Dormant_05"]);
            thisDero["Tt_Dormant_05"] += clean(CurrentCropParams["Tt_Dormant_05"]);
            thisDero["Pp_Vegetative"] += clean(CurrentCropParams["Pp_Vegetative"]);
            thisDero["VegetativeStartStage"] += clean(CurrentCropParams["VegetativeStartStage"]);
            thisDero["Tt_Vegetative"] += clean(CurrentCropParams["Tt_Vegetative"]);
            thisDero["Tt_Flowering"] += clean(CurrentCropParams["Tt_Flowering"]);
            thisDero["Tt_Reproductive"] += clean(CurrentCropParams["Tt_Reproductive"]);
            thisDero["MaxCanopyBaseHeight"] += clean(CurrentCropParams["MaxCanopyBaseHeight"]);
            thisDero["MaxCanopyPrunedHeight"] += clean(CurrentCropParams["MaxCanopyPrunedHeight"]);
            thisDero["MaxCanopyHeight"] += clean(CurrentCropParams["MaxCanopyHeight"]);
            thisDero["MaxCanopyPrunedWidth"] += clean(CurrentCropParams["MaxCanopyPrunedWidth"]);
            thisDero["MaxCanopyWidth"] += clean(CurrentCropParams["MaxCanopyWidth"]);
            thisDero["AgeToMaxDimension"] += clean(CurrentCropParams["AgeToMaxDimension"]);
            thisDero["SeasonalDimensionPattern"] += clean(CurrentCropParams["SeasonalDimensionPattern"]);
            thisDero["Gsmax350"] += clean(CurrentCropParams["Gsmax350"]);
            thisDero["R50"] += clean(CurrentCropParams["R50"]);
            thisDero["RelSlowLAI"] += clean(CurrentCropParams["RelSlowLAI"]);
            thisDero["LAIbase"] += clean(CurrentCropParams["LAIbase"]);
            thisDero["LAIbaseInitial"] += clean(CurrentCropParams["LAIbase"]);
            thisDero["LAIAnnualGrowth"] += clean(CurrentCropParams["LAIAnnualGrowth"]);
            thisDero["ExtCoeff"] += clean(CurrentCropParams["ExtCoeff"]);
            thisDero["RUEtotal"] += clean(CurrentCropParams["RUEtotal"]);
            thisDero["RUETempThresholds"] += clean(CurrentCropParams["RUETempThresholds"]);
            thisDero["PhotosynthesisType"] += clean(CurrentCropParams["PhotosynthesisType"]);
            thisDero["LeafPartitionFrac"] += clean(CurrentCropParams["LeafPartitionFrac"]);
            thisDero["ProductPartitionFrac"] += clean(CurrentCropParams["ProductPartitionFrac"]);
            thisDero["RootPartitionFrac"] += clean(CurrentCropParams["RootPartitionFrac"]);
            thisDero["TrunkWtAtMaxDimension"] += clean(CurrentCropParams["TrunkWtAtMaxDimension"]);
            double relativeAge = MathUtilities.Divide(Double.Parse(clean(CurrentCropParams["AgeAtStartSimulation"])),
                                                     Double.Parse(clean(CurrentCropParams["AgeToMaxDimension"])), 0);
            double initialTrunkwt = Double.Parse(clean(CurrentCropParams["TrunkWtAtMaxDimension"])) * relativeAge;
            thisDero["InitialTrunkWt"] += initialTrunkwt.ToString();
            thisDero["InitialRootWt"] += (50 * relativeAge).ToString();
            thisDero["LeafMaxNConc"] += clean(CurrentCropParams["LeafMaxNConc"]);
            thisDero["LeafMinNConc"] += clean(CurrentCropParams["LeafMinNConc"]);
            thisDero["ProductMaxNConc"] += clean(CurrentCropParams["ProductMaxNConc"]);
            thisDero["ProductMinNConc"] += clean(CurrentCropParams["ProductMinNConc"]);
            thisDero["RootMaxNConc"] += clean(CurrentCropParams["RootMaxNConc"]);
            thisDero["RootMinNConc"] += clean(CurrentCropParams["RootMinNConc"]);
            thisDero["TrunkMaxNConc"] += clean(CurrentCropParams["TrunkMaxNConc"]);
            thisDero["TrunkMinNConc"] += clean(CurrentCropParams["TrunkMinNConc"]);
            thisDero["MaxRootDepth"] += clean(CurrentCropParams["MaxRootDepth"]);
            thisDero["Frost_Temp_X"] += clean(CurrentCropParams["Frost_Temp_X"]);
            thisDero["Frost_Frac_Y"] += clean(CurrentCropParams["Frost_Frac_Y"]);
            thisDero["WaterStressLAI_Fw_X"] += clean(CurrentCropParams["WaterStressLAI_Fw_X"]);
            thisDero["WaterStressLAI_Frac_Y"] += clean(CurrentCropParams["WaterStressLAI_Frac_Y"]);
            thisDero["WaterStressExtCoeff_Fw_X"] += clean(CurrentCropParams["WaterStressExtCoeff_Fw_X"]);
            thisDero["WaterStressExtCoeff_Frac_Y"] += clean(CurrentCropParams["WaterStressExtCoeff_Frac_Y"]);
            thisDero["WaterStressRUE_Fw_X"] += clean(CurrentCropParams["WaterStressRUE_Fw_X"]);
            thisDero["WaterStressRUE_Fract_Y"] += clean(CurrentCropParams["WaterStressRUE_Fract_Y"]);
            thisDero["WaterStressLAISenes_X"] += clean(CurrentCropParams["WaterStressLAISenes_X"]);
            thisDero["WaterStressLAISenes_Y"] += clean(CurrentCropParams["WaterStressLAISenes_Y"]);
            thisDero["FlowerNumberMax"] += clean(CurrentCropParams["FlowerNumberMax"]);
            thisDero["FlowerMaxTempStress_Temp_X"] += clean(CurrentCropParams["FlowerMaxTempStress_Temp_X"]);
            thisDero["FlowerMaxTempStress_Factor_Y"] += clean(CurrentCropParams["FlowerMaxTempStress_Factor_Y"]);
            thisDero["FlowerMinTempStress_Temp_X"] += clean(CurrentCropParams["FlowerMinTempStress_Temp_X"]);
            thisDero["FlowerMinTempStress_Factor_Y"] += clean(CurrentCropParams["FlowerMinTempStress_Factor_Y"]);
            thisDero["ProduceDryMatterFrac"] += clean(CurrentCropParams["ProduceDryMatterFrac"]);
            thisDero["FruitWeightPotential"] += clean(CurrentCropParams["FruitWeightPotential"]);
            thisDero["RainfallExcessDamage_mm_X"] += clean(CurrentCropParams["RainfallExcessDamage_mm_X"]);
            thisDero["RainfallExcessDamage_Fract_Y"] += clean(CurrentCropParams["RainfallExcessDamage_Fract_Y"]);

            string[] commands = new string[deroParams.Count];
            thisDero.Values.CopyTo(commands, 0);
            Cultivar deroValues = new Cultivar(this.Name, commands);
            return deroValues;
        }

        /// <summary>
        /// Helper method that takes data from cs and gets into format needed to be a for Cultivar overwrite
        /// </summary>
        /// <param name="dirty"></param>
        /// <returns></returns>
        private string clean(string dirty)
        {
            string ret = dirty.Replace("(", "").Replace(")", "");
            Regex sWhitespace = new Regex(@"\s+");
            return sWhitespace.Replace(ret, ",");
        }

        /// <summary>
        /// Method to extract a value from an array of parameter inputs for DEROPAPY.  Inputs as comma seperated string
        /// </summary>
        /// <param name="vect"></param>
        /// <param name="pos"></param>
        /// <returns>The number you want</returns>
        public double GetValueFromStringVector(string vect, int pos)
        {
            string cleaned = clean(vect);
            string[] strung = cleaned.Split(',');
            double[] doubles = new double[strung.Length];
            for (int i = 0; i < strung.Length; i++) 
            {
                doubles[i] = Double.Parse(strung[i]);
            }
            return doubles[pos];
        }

        /// <summary>
        /// Base dictionary with DEROPAPY parameters and the locations they map to in the DEROPAPY.json model.
        /// </summary>
        [JsonIgnore]
        private Dictionary<string, string> deroParams = new Dictionary<string, string>()
        {
            {"CropType","[DEROPAPY].PlantType = " },
            {"TT_Temp_X","[DEROPAPY].Phenology.ThermalTime.XYPairs.X = " },
            {"TT_Acc_Y","[DEROPAPY].Phenology.ThermalTime.XYPairs.Y = " },
            {"D_StartGrowth_00","[DEROPAPY].Phenology.Waiting.DOYtoProgress = " },
            {"T_StartGrowth_00","[DEROPAPY].Phenology.Waiting.TemptoProgress = " },
            {"Tt_Growing_01","[DEROPAPY].Phenology.Growing.Target.FixedValue = " },
            {"DefoliateOrDevelop","[DEROPAPY].Phenology.DefoliateOrDevelop.PhaseNameToGoto = "},
            {"Pp_FullCover_02","[DEROPAPY].Phenology.FullCover.Target.XYPairs.X = " },
            {"Tt_FullCover_02","[DEROPAPY].Phenology.FullCover.Target.XYPairs.Y = " },
            {"Tt_Senescent_03","[DEROPAPY].Phenology.Senescent.Target.FixedValue = " },
            {"Tt_Mature_04","[DEROPAPY].Phenology.Mature.Target.FixedValue = " },
            {"EndOrHarvest"," [DEROPAPY].Phenology.EndOrHarvest.PhaseNameToGoto = " },
            {"Chill_Temp_X","[DEROPAPY].Phenology.DailyChill.XYPairs.X = " },
            {"Chill_Acc_Y","[DEROPAPY].Phenology.DailyChill.XYPairs.Y = "},
            {"AC_Dormant_05","[DEROPAPY].Phenology.Dormant.Target.XYPairs.X = " },
            {"Tt_Dormant_05","[DEROPAPY].Phenology.Dormant.Target.XYPairs.Y = " },
            {"VegetativeStartStage","[DEROPAPY].Phenology.Vegetative.StartStage = "},
            {"Pp_Vegetative","[DEROPAPY].Phenology.Vegetative.Target.XYPairs.X = " },
            {"Tt_Vegetative","[DEROPAPY].Phenology.Vegetative.Target.XYPairs.Y = " },
            {"Tt_Flowering","[DEROPAPY].Phenology.Flowering.Target.FixedValue = " },
            {"Tt_Reproductive","[DEROPAPY].Phenology.Reproductive.Target.FixedValue = " },
            {"MaxCanopyBaseHeight","[DEROPAPY].Height.CanopyBaseHeight.Maximum.FixedValue = " },
            {"MaxCanopyPrunedHeight","[DEROPAPY].Height.PrunedCanopyDepth.Maximum.MaxPrunedHeight.FixedValue = " },
            {"MaxCanopyHeight","[DEROPAPY].Height.SeasonalGrowth.Maximum.MaxHeight.FixedValue = " },
            {"MaxCanopyPrunedWidth","[DEROPAPY].Width.PrunedWidth.Maximum.FixedValue = " },
            {"MaxCanopyWidth","[DEROPAPY].Width.SeasonalGrowth.Maximum.MaxWidth.FixedValue = " },
            {"AgeToMaxDimension","[DEROPAPY].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"SeasonalDimensionPattern","[DEROPAPY].RelativeSeasonalDimension.XYPairs.Y = " },
            {"Gsmax350", "[DEROPAPY].Leaf.Canopy.Gsmax350 = " },
            {"R50", "[DEROPAPY].Leaf.Canopy.R50 = " },
            {"RelSlowLAI",  "[DEROPAPY].Leaf.Canopy.ExpandedGreenArea.Expansion.Delta.Integral.GrowthPattern.XYPairs.X[2] = "},
            {"LAIbase","[DEROPAPY].Leaf.Canopy.GreenAreaIndex.WinterBase.PrunThreshold.FixedValue = " },                                   
            {"LAIbaseInitial", "[DEROPAPY].Leaf.Canopy.GreenAreaIndex.WinterBase.GAICarryover.PreEventValue.FixedValue = "},
            {"LAIAnnualGrowth","[DEROPAPY].Leaf.Canopy.ExpandedGreenArea.Expansion.Delta.Integral.LAIAnnualGrowth.FixedValue = " },
            {"ExtCoeff","[DEROPAPY].Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue = " },
            {"RUEtotal","[DEROPAPY].Leaf.Photosynthesis.RUE.FixedValue = " },
            {"RUETempThresholds","[DEROPAPY].Leaf.Photosynthesis.FT.XYPairs.X = " },
            {"PhotosynthesisType","[DEROPAPY].Leaf.Photosynthesis.FCO2.PhotosyntheticPathway = " },
            {"LeafPartitionFrac","[DEROPAPY].Leaf.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"ProductPartitionFrac","[DEROPAPY].Product.TotalCarbonDemand.TotalDMDemand.AllometricDemand.AllometricDemand.Const = " },
            {"RootPartitionFrac","[DEROPAPY].Root.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"TrunkWtAtMaxDimension","[DEROPAPY].Trunk.MatureWt.FixedValue = "},
            {"InitialTrunkWt","[DEROPAPY].Trunk.InitialWt.FixedValue = "},
            {"InitialRootWt","[DEROPAPY].Root.InitialWt.FixedValue = "},
            {"LeafMaxNConc","[DEROPAPY].Leaf.Nitrogen.ConcFunctions.Maximum.FixedValue = " },
            {"LeafMinNConc","[DEROPAPY].Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"ProductMaxNConc","[DEROPAPY].Product.Nitrogen.ConcFunctions.Maximum.FixedValue = " },
            {"ProductMinNConc","[DEROPAPY].Product.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"RootMaxNConc","[DEROPAPY].Root.Nitrogen.ConcFunctions.Maximum.FixedValue = " },
            {"RootMinNConc","[DEROPAPY].Root.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"TrunkMaxNConc","[DEROPAPY].Trunk.Nitrogen.ConcFunctions.Maximum.FixedValue = " },
            {"TrunkMinNConc","[DEROPAPY].Trunk.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"MaxRootDepth","[DEROPAPY].Root.Network.MaximumRootDepth.FixedValue = " },
            {"Frost_Temp_X","[DEROPAPY].Leaf.FrostFraction.XYPairs.X = " },
            {"Frost_Frac_Y","[DEROPAPY].Leaf.FrostFraction.XYPairs.Y = " },
            {"WaterStressLAI_Fw_X","[DEROPAPY].Leaf.Canopy.ExpandedGreenArea.Expansion.WaterStressFactor.XYPairs.X = " },
            {"WaterStressLAI_Frac_Y","[DEROPAPY].Leaf.Canopy.ExpandedGreenArea.Expansion.WaterStressFactor.XYPairs.Y = " },
            {"WaterStressExtCoeff_Fw_X","[DEROPAPY].Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.X = " },
            {"WaterStressExtCoeff_Frac_Y","[DEROPAPY].Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y = " },
            {"WaterStressRUE_Fw_X","[DEROPAPY].Leaf.Photosynthesis.FW.XYPairs.X = " },
            {"WaterStressRUE_Fract_Y","[DEROPAPY].Leaf.Photosynthesis.FW.XYPairs.Y = " },
            {"WaterStressLAISenes_X", "[DEROPAPY].Leaf.Canopy.DeadAreaIndex.DroughtedLAI.DailyDroughtSenescence.WaterStressFactor.XYPairs.X = " },
            {"WaterStressLAISenes_Y", "[DEROPAPY].Leaf.Canopy.DeadAreaIndex.DroughtedLAI.DailyDroughtSenescence.WaterStressFactor.XYPairs.Y = " },
            {"FlowerNumberMax","[DEROPAPY].Product.FlowerNumber.Maximum.FixedValue = " },
            {"FlowerMaxTempStress_Temp_X","[DEROPAPY].Product.FlowerNumber.StressDuringFlowering.MaxTempStress.XYPairs.X = " },
            {"FlowerMaxTempStress_Factor_Y","[DEROPAPY].Product.FlowerNumber.StressDuringFlowering.MaxTempStress.XYPairs.Y = " },
            {"FlowerMinTempStress_Temp_X","[DEROPAPY].Product.FlowerNumber.StressDuringFlowering.MinTempStress.XYPairs.X = " },
            {"FlowerMinTempStress_Factor_Y","[DEROPAPY].Product.FlowerNumber.StressDuringFlowering.MinTempStress.XYPairs.Y = " },
            {"ProduceDryMatterFrac","[DEROPAPY].Product.FreshWeight.DryMatterProportion.FixedValue = " },
            {"FruitWeightPotential","[DEROPAPY].Product.PotentialFruitDryWt.FixedValue = " },
            {"RainfallExcessDamage_mm_X","[DEROPAPY].Product.RainfallExcessFactor.XYPairs.X = " },
            {"RainfallExcessDamage_Fract_Y","[DEROPAPY].Product.RainfallExcessFactor.XYPairs.Y = " },
    };
    }
}
