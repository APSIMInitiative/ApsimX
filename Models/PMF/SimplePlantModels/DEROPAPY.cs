using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing;
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
using System.Drawing;
using System.Drawing.Text;
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
    public class DEROPAPY : Model, IGridModel
    {
        /// <summary>Location of file with crop specific coefficients</summary>
        [Description("File path for coefficient file")]
        [Display(Type = DisplayType.FileName)]
        public string CoeffientFile
        {
            get { return coefficientFile; }
            set { coefficientFile = value; readCSVandUpdateProperties(); }
        }
        private string coefficientFile = null;

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

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant deropapy = null;

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

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar derochild = null;

        ////// This secton contains the components that get values from the csv coefficient file to    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////// display in the grid view and set them back to the csv when they are changed in the grid !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        private DataTable readCSVandUpdateProperties()
        {
            DataTable readData = new DataTable();
            readData = ApsimTextFile.ToTable(CoeffientFile);
            if (readData.Rows.Count == 0)
                throw new Exception("Failed to read any rows of data from " + CoeffientFile);
            if (CurrentCropName != null)
            {
                CurrentCropParams = getCurrentParams(readData, CurrentCropName);
            }
            CropNames = readData.Columns.Cast<DataColumn>().Select(x => x.ColumnName).ToArray().Skip(3).ToArray();
            return readData;
        }

        /// <summary>Gets or sets the table of values.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                List<GridTable> tables = new List<GridTable>
                {
                    new GridTable("", new List<GridTableColumn>(), this)
                };
                return tables;
            }
        }

        /// <summary>
        /// Reads in the csv data and sends it as a datatable to the grid
        /// </summary>
        public DataTable ConvertModelToDisplay(DataTable dt)
        {
            DataTable dt2 = new DataTable();
            try
            {
                dt2 = readCSVandUpdateProperties();
            }
            catch
            {
                dt2 = new DataTable();
            }
            return dt2;
        }

        /// <summary>
        /// Writes out changes from the grid to the csv file
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            saveToCSV(CoeffientFile, dt);

            return new DataTable();
        }

        /// <summary>
        /// Writes the data from the grid to the csv file
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="dt"></param>
        /// <exception cref="Exception"></exception>
        private void saveToCSV(string filepath, DataTable dt)
        {
            try
            {
                string contents = "";

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dt.Columns[i].ColumnName))
                    {
                        contents += dt.Columns[i].ColumnName.ToString();
                    }
                    if (i < dt.Columns.Count - 1)
                    {
                        contents += ",";
                    }
                }
                contents += "\n";

                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            contents += dr[i].ToString();
                        }
                        if (i < dt.Columns.Count - 1)
                        {
                            contents += ",";
                        }
                    }
                    contents += "\n";
                }

                StreamWriter s = new StreamWriter(filepath, false);
                s.Write(contents);
                s.Close();
            }
            catch
            {
                throw new Exception("Error Writing File");
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
            if (!deropapy.IsAlive)
            {
                readCSVandUpdateProperties();
                Establish();
            }
        }

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

            double rootDepth = Math.Min(Double.Parse(CurrentCropParams["MaxRootDepth"]), soilDepthMax);

            bool RootThyNeighbour = bool.Parse(CurrentCropParams["RootTheNeighboursZone"]);
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

            double AgeAtSimulationStart = Double.Parse(CurrentCropParams["AgeAtStartSimulation"]);
            string cropName = this.Name;
            double depth = Math.Min(Double.Parse(CurrentCropParams["MaxRootDepth"]) * AgeAtSimulationStart / Double.Parse(CurrentCropParams["AgeToMaxDimension"]), rootDepth);
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
            //Set Root Depth back to zero.
            root.Depth = 0;
        }

        /// <summary>
        /// Procedures that occur for crops that go into the HarvestAndPrune Phase
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        [EventSubscribe("Harvesting")]
        private void onHarvesting(object sender, EventArgs e)
        { 

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
            thisDero["Tt_Vegetative_01"] += clean(CurrentCropParams["Tt_Vegetative_01"]);
            thisDero["DefoliateOrDevelop"] += clean(CurrentCropParams["DefoliateOrDevelop"]);
            thisDero["Pp_Reproductive_02"] += clean(CurrentCropParams["Pp_Reproductive_02"]);
            thisDero["Tt_Reproductive_02"] += clean(CurrentCropParams["Tt_Reproductive_02"]);
            thisDero["Tt_Senescent_03"] += clean(CurrentCropParams["Tt_Senescent_03"]);
            thisDero["Tt_Mature_04"] += clean(CurrentCropParams["Tt_Mature_04"]);
            thisDero["EndOrHarvest"] += clean(CurrentCropParams["EndOrHarvest"]);
            thisDero["Chill_Temp_X"] += clean(CurrentCropParams["Chill_Temp_X"]);
            thisDero["Chill_Acc_Y"] += clean(CurrentCropParams["Chill_Acc_Y"]);
            thisDero["AC_Dormant_05"] += clean(CurrentCropParams["AC_Dormant_05"]);
            thisDero["Tt_Dormant_05"] += clean(CurrentCropParams["Tt_Dormant_05"]);
            thisDero["MaxCanopyBaseHeight"] += clean(CurrentCropParams["MaxCanopyBaseHeight"]);
            thisDero["MaxCanopyPrunedHeight"] += clean(CurrentCropParams["MaxCanopyPrunedHeight"]);
            thisDero["MaxCanopyHeight"] += clean(CurrentCropParams["MaxCanopyHeight"]);
            thisDero["MaxCanopyPrunedWidth"] += clean(CurrentCropParams["MaxCanopyPrunedWidth"]);
            thisDero["MaxCanopyWidth"] += clean(CurrentCropParams["MaxCanopyWidth"]);
            thisDero["AgeToMaxDimension"] += clean(CurrentCropParams["AgeToMaxDimension"]);
            thisDero["SeasonalDimensionPattern"] += clean(CurrentCropParams["SeasonalDimensionPattern"]);
            thisDero["LAImax"] += clean(CurrentCropParams["LAImax"]);
            thisDero["ExtCoeff"] += clean(CurrentCropParams["ExtCoeff"]);
            thisDero["LAIWaterStressSens"] += clean(CurrentCropParams["LAIWaterStressSens"]);
            thisDero["ExtCoeffWaterStressSens"] += clean(CurrentCropParams["ExtCoeffWaterStressSens"]);
            thisDero["RUEtotal"] += clean(CurrentCropParams["RUEtotal"]);
            thisDero["RUETempThresholds"] += clean(CurrentCropParams["RUETempThresholds"]);
            thisDero["PhotosynthesisType"] += clean(CurrentCropParams["PhotosynthesisType"]);
            thisDero["LeafPartitionFrac"] += clean(CurrentCropParams["LeafPartitionFrac"]);
            thisDero["ProductPartitionFrac"] += clean(CurrentCropParams["ProductPartitionFrac"]);
            thisDero["RootPartitionFrac"] += clean(CurrentCropParams["RootPartitionFrac"]);
            thisDero["TrunkPartitionFrac"] += clean(CurrentCropParams["TrunkPartitionFrac"]);
            thisDero["LeafMaxNConc"] += clean(CurrentCropParams["LeafMaxNConc"]);
            thisDero["LeafMinNConc"] += clean(CurrentCropParams["LeafMinNConc"]);
            thisDero["ProductMaxNConc"] += clean(CurrentCropParams["ProductMaxNConc"]);
            thisDero["ProductMinNConc"] += clean(CurrentCropParams["ProductMinNConc"]);
            thisDero["RootMaxNConc"] += clean(CurrentCropParams["RootMaxNConc"]);
            thisDero["RootMinNConc"] += clean(CurrentCropParams["RootMinNConc"]);
            thisDero["TrunkMaxNConc"] += clean(CurrentCropParams["TrunkMaxNConc"]);
            thisDero["TrunkMinNConc"] += clean(CurrentCropParams["TrunkMinNConc"]);
            thisDero["MaxRootDepth"] += clean(CurrentCropParams["MaxRootDepth"]);

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
            {"Tt_Vegetative_01","[DEROPAPY].Phenology.Vegetative.Target.FixedValue = " },
            {"DefoliateOrDevelop","[DEROPAPY].Phenology.DefoliateOrDevelop.PhaseNameToGoto = "},
            {"Pp_Reproductive_02","[DEROPAPY].Phenology.Reproductive.Target.XYPairs.X = " },
            {"Tt_Reproductive_02","[DEROPAPY].Phenology.Reproductive.Target.XYPairs.Y = " },
            {"Tt_Senescent_03","[DEROPAPY].Phenology.Senescent.Target.FixedValue = " },
            {"Tt_Mature_04","[DEROPAPY].Phenology.Mature.Target.FixedValue = " },
            {"EndOrHarvest"," [DEROPAPY].Phenology.EndOrHarvest.PhaseNameToGoto = " },
            {"Chill_Temp_X","[DEROPAPY].Phenology.Chill.DailyChill.XYPairs.X = " },
            {"Chill_Acc_Y","[DEROPAPY].Phenology.Chill.DailyChill.XYPairs.Y = "},
            {"AC_Dormant_05","[DEROPAPY].Phenology.Dormant.Target.XYPairs.X = " },
            {"Tt_Dormant_05","[DEROPAPY].Phenology.Dormant.Target.XYPairs.Y = " },
            {"MaxCanopyBaseHeight","[DEROPAPY].Height.CanopyBaseHeight.Maximum.FixedValue = " },
            {"MaxCanopyPrunedHeight","[DEROPAPY].Height.PrunedCanopyDepth.Maximum.MaxPrunedHeight.FixedValue = " },
            {"MaxCanopyHeight","[DEROPAPY].Height.SeasonalGrowth.Maximum.MaxHeight.FixedValue = " },
            {"MaxCanopyPrunedWidth","[DEROPAPY].Width.PrunedWidth.Maximum.FixedValue = " },
            {"MaxCanopyWidth","[DEROPAPY].Width.SeasonalGrowth.Maximum.MaxWidth.FixedValue = " },
            {"AgeToMaxDimension","[DEROPAPY].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"SeasonalDimensionPattern","[DEROPAPY].RelativeSeasonalDimension.XYPairs.Y = " },
            {"LAImax","[DEROPAPY].Leaf.Canopy.GreenAreaExpansion.Expansion.Delta.Integral.LAIMax.FixedValue = " },
            {"ExtCoeff","[DEROPAPY].Leaf.Canopy.GreenExtinctionCoefficient.PotentialExtinctionCoeff.FixedValue = " },
            {"LAIWaterStressSens","[DEROPAPY].Leaf.Canopy.GreenAreaExpansion.Expansion.WaterStressFactor.XYPairs.Y[1] = " },
            {"ExtCoeffWaterStressSens","[DEROPAPY].Leaf.Canopy.GreenExtinctionCoefficient.WaterStress.XYPairs.Y[1] = " },
            {"RUEtotal","[DEROPAPY].Leaf.Photosynthesis.RUE.FixedValue = " },
            {"RUETempThresholds","[DEROPAPY].Leaf.Photosynthesis.FT.XYPairs.X = " },
            {"PhotosynthesisType","[DEROPAPY].Leaf.Photosynthesis.FCO2.PhotosyntheticPathway = " },
            {"LeafPartitionFrac","[DEROPAPY].Leaf.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"ProductPartitionFrac","[DEROPAPY].Product.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"RootPartitionFrac","[DEROPAPY].Root.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"TrunkPartitionFrac","[DEROPAPY].Trunk.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"LeafMaxNConc","[DEROPAPY].Leaf.Nitrogen.ConcFunctions.Maximum.XYPairs.Y[2] = " },
            {"LeafMinNConc","[DEROPAPY].Leaf.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"ProductMaxNConc","[DEROPAPY].Product.Nitrogen.ConcFunctions.Maximum.XYPairs.Y[2] = " },
            {"ProductMinNConc","[DEROPAPY].Product.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"RootMaxNConc","[DEROPAPY].Root.Nitrogen.ConcFunctions.Maximum.FixedValue = " },
            {"RootMinNConc","[DEROPAPY].Root.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"TrunkMaxNConc","[DEROPAPY].Trunk.Nitrogen.ConcFunctions.Maximum.FixedValue = " },
            {"TrunkMinNConc","[DEROPAPY].Trunk.Nitrogen.ConcFunctions.Minimum.FixedValue = " },
            {"MaxRootDepth","[DEROPAPY].Root.Network.MaximumRootDepth.FixedValue = " },
        };
    }
}
