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
        public string CoeffientFile { get; set; }

        /// <summary>Establishemnt Date</summary>
        [Description("Name of the crop to in simulation")]
        public string CropName { get; set; }

        [JsonIgnore] private DataTable CropCoeffs { get; set; }

        ///<summary></summary> 
        [JsonIgnore] public string[] ParamName { get; set; }
      
        ///<summary></summary> 
        [JsonIgnore] public Dictionary<string, string> Current { get; set; }


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

        private string fullFileName = null;
        ////// This secton contains the components that get values from the csv coefficient file to    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ////// display in the grid view and set them back to the csv when they are changed in the grid !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        private void setCropCoefficients()
        {
            Simulation sim = (Simulation)this.FindAllAncestors<Simulation>().FirstOrDefault();
            fullFileName = PathUtilities.GetAbsolutePath(CoeffientFile, sim.FileName);

            ApsimTextFile textFile = new ApsimTextFile();
            textFile.Open(fullFileName);
            CropCoeffs = textFile.ToTable();
            textFile.Close();

            //Current = getCurrentParams(CropCoeffs, CropName);
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
            try
            {
                Simulation sim = (Simulation)this.FindAllAncestors<Simulation>().FirstOrDefault();
                fullFileName = PathUtilities.GetAbsolutePath(CoeffientFile, sim.FileName);
                ApsimTextFile textFile = new ApsimTextFile();
                textFile.Open(fullFileName);
                CropCoeffs = textFile.ToTable();
                textFile.Close();
            }
            catch
            {
                CropCoeffs = new DataTable();
            }
            return CropCoeffs;
        }

        /// <summary>
        /// Writes out changes from the grid to the csv file
        /// </summary>
        public DataTable ConvertDisplayToModel(DataTable dt)
        {
            saveToCSV(fullFileName, dt);
            
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
        /// Gets the parameter set from the CoeffientFil for the CropName specified and returns in a dictionary maped to paramter names.
        /// </summary>
        /// <param name="tab"></param>
        /// <param name="cropName"></param>
        /// <returns></returns>
        private Dictionary<string, string> getCurrentParams(DataTable tab, string cropName)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            for (int i = 0; i < tab.Rows.Count; i++)
            {
                //ret.Add(ParamName[i], tab.Rows[i][cropName].ToString());
                ret.Add("test", tab.Rows[i][cropName].ToString());
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
                Current = getCurrentParams(CropCoeffs, CropName);
                //setCropCoefficients();
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

            double rootDepth = Math.Min(Double.Parse(Current["MaxRootDepth"]), soilDepthMax);

            bool RootThyNeighbour = bool.Parse(Current["RootTheNeighboursZone"]);
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

            double AgeAtSimulationStart = Double.Parse(Current["AgeAtStartSimulation"]);
            string cropName = this.Name;
            double depth = Math.Min(Double.Parse(Current["MaxRootDepth"]) * AgeAtSimulationStart / Double.Parse(Current["AgeToMaxDimension"]), rootDepth);
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
        /// Data structure that appends a parameter value to each address in the base deroParams dictionary 
        /// then writes them into a commands property in a Cultivar object.
        /// </summary>
        /// <returns>a Cultivar object with the overwrites set for the CropName selected using the parameters displayed 
        /// in the grid view that come from the CoeffientFile selected 
        /// </returns>
        public Cultivar coeffCalc()
        {
            Dictionary<string, string> thisDero = new Dictionary<string, string>(deroParams);

            thisDero["CropType"] += clean(Current["CropType"]);
            thisDero["TT_Temp_X"] += clean(Current["TT_Temp_X"]);
            thisDero["TT_Acc_Y"] += clean(Current["TT_Acc_Y"]);
            thisDero["D_StartGrowth_00"] += clean(Current["D_StartGrowth_00"]);
            thisDero["T_StartGrowth_00"] += clean(Current["T_StartGrowth_00"]);
            thisDero["Tt_Vegetative_01"] += clean(Current["Tt_Vegetative_01"]);
            thisDero["DefoliateOrDevelop"] += clean(Current["DefoliateOrDevelop"]);
            thisDero["Pp_Reproductive_02"] += clean(Current["Pp_Reproductive_02"]);
            thisDero["Tt_Reproductive_02"] += clean(Current["Tt_Reproductive_02"]);
            thisDero["Tt_Senescent_03"] += clean(Current["Tt_Senescent_03"]);
            thisDero["Tt_Mature_04"] += clean(Current["Tt_Mature_04"]);
            thisDero["EndOrHarvest"] += clean(Current["EndOrHarvest"]);
            thisDero["Chill_Temp_X"] += clean(Current["Chill_Temp_X"]);
            thisDero["Chill_Acc_Y"] += clean(Current["Chill_Acc_Y"]);
            thisDero["AC_Dormant_05"] += clean(Current["AC_Dormant_05"]);
            thisDero["Tt_Dormant_05"] += clean(Current["Tt_Dormant_05"]);
            thisDero["MaxCanopyBaseHeight"] += clean(Current["MaxCanopyBaseHeight"]);
            thisDero["MaxCanopyPrunedHeight"] += clean(Current["MaxCanopyPrunedHeight"]);
            thisDero["MaxCanopyHeight"] += clean(Current["MaxCanopyHeight"]);
            thisDero["MaxCanopyPrunedWidth"] += clean(Current["MaxCanopyPrunedWidth"]);
            thisDero["MaxCanopyWidth"] += clean(Current["MaxCanopyWidth"]);
            thisDero["AgeToMaxDimension"] += clean(Current["AgeToMaxDimension"]);
            thisDero["SeasonalDimensionPattern"] += clean(Current["SeasonalDimensionPattern"]);
            thisDero["LAImax"] += clean(Current["LAImax"]);
            thisDero["ExtCoeff"] += clean(Current["ExtCoeff"]);
            thisDero["LAIWaterStressSens"] += clean(Current["LAIWaterStressSens"]);
            thisDero["ExtCoeffWaterStressSens"] += clean(Current["ExtCoeffWaterStressSens"]);
            thisDero["RUEtotal"] += clean(Current["RUEtotal"]);
            thisDero["RUETempThresholds"] += clean(Current["RUETempThresholds"]);
            thisDero["PhotosynthesisType"] += clean(Current["PhotosynthesisType"]);
            thisDero["LeafPartitionFrac"] += clean(Current["LeafPartitionFrac"]);
            thisDero["ProductPartitionFrac"] += clean(Current["ProductPartitionFrac"]);
            thisDero["RootPartitionFrac"] += clean(Current["RootPartitionFrac"]);
            thisDero["TrunkPartitionFrac"] += clean(Current["TrunkPartitionFrac"]);
            thisDero["LeafMaxNConc"] += clean(Current["LeafMaxNConc"]);
            thisDero["LeafMinNConc"] += clean(Current["LeafMinNConc"]);
            thisDero["ProductMaxNConc"] += clean(Current["ProductMaxNConc"]);
            thisDero["ProductMinNConc"] += clean(Current["ProductMinNConc"]);
            thisDero["RootMaxNConc"] += clean(Current["RootMaxNConc"]);
            thisDero["RootMinNConc"] += clean(Current["RootMinNConc"]);
            thisDero["TrunkMaxNConc"] += clean(Current["TrunkMaxNConc"]);
            thisDero["TrunkMinNConc"] += clean(Current["TrunkMinNConc"]);
            thisDero["MaxRootDepth"] += clean(Current["MaxRootDepth"]);

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
