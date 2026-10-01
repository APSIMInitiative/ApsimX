using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Functions;
using Models.Management;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Models.Soils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Data structure that contains information for a specific crop type in Strum
    /// </summary>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StrumTreeInstance : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>Pruning Event.</summary>
        public event EventHandler<EventArgs> Pruning;

        private double _RowSpacing = 6.0;
        private double _InterRowSpacing = 1.0;
        private double _AlleyZoneWidthFrac = 0.5;
        private int _AgeAtSimulationStart = 1;
        private int _YearsToMaxDimension = 7;
        private double _WoodBulkDensity = 650;
        private double _DBHatMaturity = 20;
        private double _MaximumLeafBiomass = 500;
        private double _MaximumRootBiomass = 300;
        private double _WoodStorageFraction = 0;
        private double _TypicalCanopyArea = 5.6;
        private double _MaxRD = 3.0;
        private double _CanopyBaseHeight = 1.0;
        private double _PrunedCanopyBaseHeight = 1.0;
        private double _MaxPrunedHeight = 3.0;
        private double _MaxHeight = 3.5;
        private double _MaxPrunedWidth = 2.0;
        private double _MaxWidth = 3.0;
        private double _RootNConc = 0.01;
        private double _LeafNConc = 0.03;
        private double _WoodNConc = 0.005;
        private double _FruitNConc = 0.01;
        private double _ExtinctCoeff = 0.7;
        private double _MaxCover = 0.98;
        private double _surfaceKL = 0.1;
        private double _RUE = 1.0;
        private double _pSBaseT = 5.0;
        private double _pSLOptT = 20.0;
        private double _pSUOptT = 25.0;
        private double _pSMaxT = 35.0;
        private double _BaseCover = 0.0;
        private double _GSMax = 0.006;
        private double _R50 = 100;
        private int _Number = 10;
        private double _PotentialFWPerFruit = 200;
        private double _DMC = 0.16;
        private double _FruitDensity = 0.6;
        private int _DAFStartLinearGrowth = 40;
        private int _DAFEndLinearGrowth = 150;
        private int _DAFMaxSize = 180;
        private double _NFixationFrac = 0;
        private double _FRemoveSummerPrune = 0;
        private static string _WinterSolsticeDate = "21-Jun";

        private string _BudBreakDate = DateTime.Parse(_WinterSolsticeDate + "-2000").AddDays(60).ToString("dd-MMM");
        private string _StartFullCanopyDate = DateTime.Parse(_WinterSolsticeDate + "-2000").AddDays(120).ToString("dd-MMM");
        private string _StartLeafFallDate = DateTime.Parse(_WinterSolsticeDate + "-2000").AddDays(230).ToString("dd-MMM");
        private string _EndLeafFallDate = DateTime.Parse(_WinterSolsticeDate + "-2000").AddDays(300).ToString("dd-MMM");

        private string WinterSolsticeDate
        {
            get
            {
                DateTime winterSolsticeDate = DateUtilities.GetDate(weather.WinterSolsticeDOY, clock.Today.Year);
                _WinterSolsticeDate = winterSolsticeDate.ToString("dd-MMM");
                return _WinterSolsticeDate;
            }
        }

        private double woodMassAtMaxDimension = 0;
        private double pruningFractionWood = 0;
        private double pruningFractionLeaf = 0;
        

        /// <summary>Is the tree decidious</summary>
        public bool Decidious
        {
            get
            {
                if (TreeType == "Evergreen")
                    return false;
                if (TreeType == "Deciduous")
                    return true;
                throw new Exception("Invalid tree type specified");
            }
        }

        /// <summary>Canopy shape options</summary>
        public enum TreeShape
        {
            /// <summary>Rounded column or sphere (if in row spacing > maximum width</summary>
            Round,
            /// <summary>Square column or cube (if in row spacing > maximum width</summary>
            Square,
            /// <summary>Triangular column or cone (if in row spacing > maximum width</summary>
            Triangular
        }

        ///<summary>Are trees decidious</summary>
        [Separator("Orchard Information")]
        [Description("Are trees decidious or ever green")]
        [Display(Type = DisplayType.StrumTreeTypes)]
        public string TreeType { get; set; }

        /// <summary>Distance between tree rows (0.01 - 100 m)</summary>
        [Description("Distance between tree rows (0.01 - 100 m)")]
        [Units("m")]
        [Bounds(Lower = 0.01, Upper = 100)]
        public double RowSpacing
        {
            get { return _RowSpacing; }
            set { _RowSpacing = constrain(value, 0.01, 100); }
        }

        /// <summary>Distance between trees within rows (0.01 - 100 m)</summary>
        [Description("Distance between trees within rows (0.01 - 100 m)")]
        [Units("m")]
        [Bounds(Lower = 0.01, Upper = 100)]
        public double InterRowSpacing
        {
            get { return _InterRowSpacing; }
            set { _InterRowSpacing = constrain(value, 0.01, 100); }
        }

        /// <summary>Proportion of the row width taken up by alley zone(0-1)</summary>
        [Description("Relative Alley Zone Width (Proportion of the distance between trees taken up by alley zone(0-1))")]
        [Units("0-1")]
        [Bounds(Lower = 0, Upper = 1.0)]
        public double AlleyZoneWidthFrac
        {
            get { return _AlleyZoneWidthFrac; }
            set { _AlleyZoneWidthFrac = constrain(value, 0, 0.99); }
        }

        /// <summary>Width of the alley zone between tree rows (0-1)</summary>
        [Units("m")]
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
                return RowSpacing * (1 - AlleyZoneWidthFrac);
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
                return MatureWidth * InterRowSpacing;
            }
        }

        /// <summary>Date for Bud Break</summary>
        [Separator("Tree Phenology.  Specify when canopy stages occur ")]
        [Description("Date for Bud Break")]
        public string BudBreakDate 
        {
            get { return _BudBreakDate; }
            set { _BudBreakDate = value; } 
        }

        /// <summary>Date for Start Full Canopy</summary>
        [Description("Date for Start Full Canopy")]
        public string StartFullCanopyDate
        {
            get { return _StartFullCanopyDate; }
            set { _StartFullCanopyDate = value; }
        }

        /// <summary>Date for Start of leaf fall</summary>
        [Description("Date for Start of leaf fall")]
        public string StartLeafFallDate
        {
            get { return _StartLeafFallDate; }
            set { _StartLeafFallDate = value; }
        }

        /// <summary>Date for End of Leaf fall</summary>
        [Description("Date for End of Leaf fall")]
        public string EndLeafFallDate
        {
            get { return _EndLeafFallDate; }
            set { _EndLeafFallDate = value; }
        }

        /// <summary>Diameter at breast height for mature orchard tree (5-100 cm)</summary>
        [Separator("Wood Growth.")]
        [Separator("Diameter Above Brest height is for a mature orchard tree of the specified canopy area.")]
        [Separator("The Model uses these parameters to calcualte DBH based on the canopy area modeled for this simulation")]
        [Description("Diameter at breast height (5-100 cm) for tree of canopy area specified below")]
        [Units("cm")]
        [Bounds(Lower = 5, Upper = 100)]
        public double DBHatMaturity
        {
            get { return _DBHatMaturity; }
            set { _DBHatMaturity = constrain(value, 5, 100); }
        }

        /// <summary>Height of the bottom of the canop (10-100000 mm)</summary>
        [Description("Area of canopy (0.1-1000 m^2) for tree with DBH specified above")]
        [Bounds(Lower = .1, Upper = 1000)]
        [Units("m^2")]
        public double TypicalCanopyArea
        {
            get { return _TypicalCanopyArea; }
            set { _TypicalCanopyArea = constrain(value, 0.1, 1000); }
        }

        /// <summary>Dry bulk density of wood wood (400-1600 kg/m^3)</summary>
        [Description("Dry bulk density of wood wood (400-1600 kg/m^3)")]
        [Units("kg/m^3")]
        [Bounds(Lower = 400, Upper = 1600)]
        public double WoodBulkDensity
        {
            get { return _WoodBulkDensity; }
            set { _WoodBulkDensity = constrain(value, 400, 1600); }
        }

        /// <summary>Carbohydrate storage capacity of wood (0 - 0.5 g/g)</summary>
        [Description("Carbohydrate storage capacity of wood (0 - 0.5 g/g)")]
        [Units("g/g")]
        [Bounds(Lower = 0, Upper = 0.5)]
        public double WoodStorageFraction
        {
            get { return _WoodStorageFraction; }
            set { _WoodStorageFraction = constrain(value, 0, 0.5); }
        }

        /// <summary>Leaf mass (g/m2 of canopy)</summary>
        [Separator("Maximum Leaf and Root mass.  Specify mass (g/m2) for organs of mature tree during late summer")]
        [Description("Leaf mass (g/m2 of canopy)")]
        [Units("g/m2")]
        [Bounds(Lower = 10, Upper = 1500)]
        public double MaximumLeafBiomass
        {
            get { return _MaximumLeafBiomass; }
            set { _MaximumLeafBiomass = (int)constrain((double)value, 10, 1500); }
        }

        /// <summary>Fine root mass (g/m2 of canopy)</summary>
        [Description("Fine root mass (g/m2 of canopy)")]
        [Units("g/m2")]
        [Bounds(Lower = 10, Upper = 1000)]
        public double MaximumRootBiomass
        {
            get { return _MaximumRootBiomass; }
            set { _MaximumRootBiomass = (int)constrain((double)value, 10, 1000); }
        }

        /// <summary>Tree Age At Start of Simulation (years)</summary>
        [Separator("Tree Dimenesions.  Values for trees at orchard mature size")]
        [Description("Tree Age At Start of Simulation (years)")]
        [Units("years")]
        [Bounds(Lower = 0, Upper = 300)]
        public double AgeAtSimulationStart
        {
            get { return _AgeAtSimulationStart; }
            set { _AgeAtSimulationStart = (int)constrain((double)value, 0, 300); }
        }

        /// <summary>Age of the tree when it reaches mature dimension (1-300 years)</summary>
        [Description("Age of the tree when it reaches mature dimension (1-300 years)")]
        [Units("years")]
        [Bounds(Lower = 1, Upper = 300)]
        public int YearsToMaxDimension
        {
            get { return _YearsToMaxDimension; }
            set { _YearsToMaxDimension = (int)constrain((double)value, 1, 300); }
        }

        /// <summary>Canopy shape.  If InterRowSpacing > MaxWidth assume separate sphere, cube or cone, else continuous row of shape</summary>
        [Description("Canopy shape.  If InterRowSpacing > MaxWidth assume separate sphere, cube or cone, else continuous row of shape)")]
        public TreeShape CrownShape { get; set; } = TreeShape.Round;

        
        /// <summary>Height of the bottom of the canop (0.1-100 m)</summary>
        [Description("Height of the mature canopy base before pruning (.1-10 m)")]
        [Bounds(Lower = 0.1, Upper = 100000)]
        [Units("m")]
        public double MatureCanopyBaseHeight
        {
            get { return _CanopyBaseHeight; }
            set { _CanopyBaseHeight = constrain(value, .1, 100000); }
        }

        /// <summary>Height of the bottom of the canop (0.1-100 m)</summary>
        [Description("Height of the mature canopy base after pruning (0.1-100 m)")]
        [Bounds(Lower = 0.1, Upper = 100000)]
        [Units("m")]
        public double MaturePrunedCanopyBaseHeight
        {
            get { return _PrunedCanopyBaseHeight; }
            set { _PrunedCanopyBaseHeight = constrain(value, 0.1, 100000); }
        }

        /// <summary>Height of top of the canopy before pruning (0.1- 200 m)</summary>
        [Description("Height of mature canopy before pruning (0.1-200 m)")]
        [Bounds(Lower = 0.1, Upper = 200000)]
        [Units("m")]
        public double MatureHeight
        {
            get { return _MaxHeight; }
            set { _MaxHeight = constrain(value, 0.1, 200000); }
        }

        /// <summary>Height of top of the canopy after pruning (0.5 - 200 m)</summary>
        [Description("Height of mature canopy after pruning (0.5 - 200 m)")]
        [Bounds(Lower = 0.5, Upper = 200000)]
        [Units("m")]
        public double MaturePrunedHeight
        {
            get { return _MaxPrunedHeight; }
            set { _MaxPrunedHeight = constrain(value, 0.5, 200000); }
        }

        /// <summary>Width of canopy before pruning (10-100000 mm)</summary>
        [Description("Width of mature canopy before pruning (0.1-100 m)")]
        [Bounds(Lower = 0.1, Upper = 100000)]
        [Units("m")]
        public double MatureWidth
        {
            get { return _MaxWidth; }
            set { _MaxWidth = constrain(value, 0.1, 100000); }
        }

        /// <summary>Width of canopy after pruning (0.1-100 m)</summary>
        [Description("Width of mature canopy after pruning  (0.1-100 m)")]
        [Bounds(Lower = 0.1, Upper = 100000)]
        [Units("m")]
        public double MaturePrunedWidth
        {
            get { return _MaxPrunedWidth; }
            set { _MaxPrunedWidth = constrain(value, 0.1, 100000); }
        }

        /// <summary>Dates for summer pruning</summary>
        [Separator("Pruning.  Proportion of biomass removed from leaf and wood determined from dimensions above.")]
        [Separator("Winter pruning occurs on EndLeafFallDate.  Picking occurs on the date maximum fruit size is reached")]
        [Description("Dates for summer pruning (coma seperated, dd-mmm for annual events or dd-mmm-yyyy for specific dates)")]
        public string[] SummerPruneDates { get; set; }

        /// <summary>Fraction of leaf and wood removed with each summer prune</summary>
        [Description("Fraction of leaf and wood removed with each summer prune (0-1)")]
        [Bounds(Lower = 0, Upper = 1)]
        [Units("0-1")]
        public double FRemoveSummerPrune
        {
            get { return _FRemoveSummerPrune; }
            set { _FRemoveSummerPrune = constrain(value, 0, 1); }
        }

        /// <summary>Root Nitrogen Concentration (g/g)</summary>
        [Separator("Tree Nitrogen contents")]
        [Description("Root Nitrogen concentration (g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        [Units("g/g")]
        public double RootNConc
        {
            get { return _RootNConc; }
            set { _RootNConc = constrain(value, 0.001, 0.1); }
        }

        /// <summary>Stover Nitrogen Concentration at maturity (g/g)</summary>
        [Description("Leaf Nitrogen concentration at maturity (g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        [Units("g/g")]
        public double LeafNConc
        {
            get { return _LeafNConc; }
            set { _LeafNConc = constrain(value, 0.001, 0.1); }
        }

        /// <summary>Wood and branch Nitrogen concentration (g/g)</summary>
        [Description("Wood and branch Nitrogen concentration (g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        [Units("g/g")]
        public double WoodNConc
        {
            get { return _WoodNConc; }
            set { _WoodNConc = constrain(value, 0.001, 0.1); }
        }

        /// <summary>Fruit Nitrogen concentration at maturity (g/g)</summary>
        [Description("Fruit Nitrogen concentration at maturity (g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        [Units("g/g")]
        public double FruitNConc
        {
            get{return _FruitNConc; }
            set{ _FruitNConc = constrain(value,0.001, 0.1); }
        }

        /// <summary>"N fixation potential of tree (0-0.05 kg N fixed per kg DM fixed)"</summary>
        [Description("N fixation potential of tree (0-0.05 kg N fixed per kg DM fixed)")]
        [Bounds(Lower = 0, Upper = 0.05)]
        [Units("0-1")]
        public double NFixationFrac
        {
            get { return _NFixationFrac; }
            set { _NFixationFrac = constrain(value, 0, .05); }
        }

        

        /// <summary>Grow roots into Alley zone (yes or no)</summary>
        [Separator("Root and Canopy parameters")]
        [Description("Grow roots into Alley zone (yes or no)")]
        public bool GRINZ { get; set; }

        /// <summary>Root depth at harvest (0.3 - 20 m)</summary>
        [Description("Root depth when mature (0.3 - 20 m)")]
        [Bounds(Lower = 0.3, Upper = 20000)]
        [Units("m")]
        public double MaxRD
        {
            get { return _MaxRD; }
            set { _MaxRD = constrain(value, 0.3, 20000); }
        }

        /// <summary>Extinction coefficient (0.1-1)</summary>
        [Description("Extinction coefficient (0.1-1)")]
        [Bounds(Lower = 0.1, Upper = 1.0)]
        [Units("0-1")]
        public double ExtinctCoeff
        {
            get{return _ExtinctCoeff; }
            set{ _ExtinctCoeff = constrain(value,0.1,1); }
        }

        /// <summary>Winter cover of tree canopy (0-0.98).  Zero for dicidious trees, >0 for evergreens </summary>
        [Description("Winter cover of tree canopy (0-0.98). Zero for dicidious trees, >0 for evergreens")]
        [Bounds(Lower = 0, Upper = 0.98)]
        [Units("0-1")]
        public double BaseCover
        {
            get{return _BaseCover; }
            set{ _BaseCover = constrain(value,0,0.98); }
        }

        /// <summary>Maximum cover of tree canopy (0.01-0.98).  This is the fraction of radiation that the tree canopy intercepts within its canopy area, not for the entire zone.</summary>
        [Description("Maximum fractional radiation interception of tree canopy (0.01-0.98).  For the canopy area, not the entire zone.")]
        [Bounds(Lower = 0.01, Upper = 0.98)]
        [Units("0-1")]
        public double MaxCover
        {
            get { return _MaxCover; }
            set { _MaxCover = constrain(value, 0.01, 0.98); }
        }

        /// <summary>"Radiation use efficiency (0.1 - 3.0 g/MJ)"</summary>
        [Separator("Biomass production temperature Responses")]
        [Description("Radiation use efficiency (0.1 - 3.0 g/MJ)")]
        [Bounds(Lower = 0.1, Upper = 3.0)]
        [Units("g/MJ")]
        public double RUE
        {
            get { return _RUE; }
            set { _RUE = constrain(value, 0.1, 3.0); }
        }

        /// <summary>Base temperature for photosynthesis (-10-10 oC)</summary>
        [Description("Base temperature for photosynthesis (-10-10 oC)")]
        [Bounds(Lower = -10, Upper = 10)]
        [Units("oC")]
        public double PSBaseT
        {
            get { return _pSBaseT; }
            set { _pSBaseT = constrain(value, -10, 10); }
        }

        /// <summary>Lower optimum temperature for photosynthesis (10-40 oC)</summary>
        [Description("Lower optimum temperature for photosynthesis (10-40 oC)")]
        [Bounds(Lower = 10, Upper = 40)]
        [Units("oC")]
        public double PSLOptT
        {
            get { return _pSLOptT; }
            set { _pSLOptT = constrain(value, 10, 40); }
        }

        /// <summary>Upper optimum temperature for photosynthesis (10-40 oC)</summary>
        [Description("Upper optimum temperature for photosynthesis (10-40 oC)")]
        [Bounds(Lower = 10, Upper = 50)]
        [Units("oC")]
        public double PSUOptT
        {
            get { return _pSUOptT; }
            set { _pSUOptT = constrain(value, 10, 50); }
        }

        /// <summary>Maximum temperature for photosynthesis (20-50 oC)</summary>
        [Description("Maximum temperature for photosynthesis (20-50 oC)")]
        [Bounds(Lower = 20, Upper = 50)]
        [Units("oC")]
        public double PSMaxT
        {
            get { return _pSMaxT; }
            set { _pSMaxT = constrain(value, 20, 50); }
        }

        /// <summary>Maximum canopy conductance (between 0.001 and 0.016 m/s) </summary>
        [Separator("Water demand and response")]
        [Description("Maximum canopy conductance (between 0.001 and 0.016 m/s)")]
        [Bounds(Lower = 0.001, Upper = 0.016)]
        [Units("m/s")]
        public double GSMax
        {
            get { return _GSMax; }
            set { _GSMax = constrain(value, 0.001, 0.016); }
        }

        /// <summary>Net radiation at 50% of maximum conductance (between 10 and 200 W/m^2)</summary>
        [Description("Net radiation at 50% of maximum conductance (between 10 and 200  W/m^2)")]
        [Bounds(Lower = 10, Upper = 200)]
        [Units("W/m^2")]
        public double R50
        {
            get { return _R50; }
            set { _R50 = constrain(value, 10, 200); }
        }

        /// <summary>KL in top soil layer (0.01 - 0.2)</summary>
        [Description("KL in top soil layer (0.01 - 0.2)")]
        [Bounds(Lower = 0.01, Upper = 0.2)]
        [Units("0-1")]
        public double SurfaceKL
        {
            get { return _surfaceKL; }
            set { _surfaceKL = constrain(value, 0.01, 0.2); }
        }

        /// <summary>"Does the crop respond to water stress?"</summary>
        [Description("Does the crop respond to water stress?")]
        public bool WaterStress { get; set; }

        /// <summary>Fruit number retained (per m2 tree canopy area)</summary>
        [Separator("Fruit parameters")]
        [Description("Fruit number retained (per m2 tree canopy area)")]
        [Bounds(Lower = 0, Upper = 1000000)]
        [Units("/m^2 canopy area")]
        public int Number
        {
            get { return _Number; }
            set { _Number = (int)constrain((double)value, 0, 1000000); }
        }

        /// <summary>Potential Fruit Fresh Wt (g/fruit) </summary>
        [Description("Potential Fruit Fresh Wt (g/fruit)")]
        [Bounds(Lower = 0, Upper = 10000)]
        [Units("g/fruit")]
        public double PotentialFWPerFruit
        {
            get { return _PotentialFWPerFruit; }
            set { _PotentialFWPerFruit = constrain(value, 0, 1000000); }
        }

        /// <summary>Fruit DM conc (g/g) </summary>
        [Description("Fruit DM conc (g/g)")]
        [Bounds(Lower = 0.01, Upper = 1.0)]
        [Units("g/g")]
        public double DMC
        {
            get { return _DMC; }
            set { _DMC = constrain(value, 0.01, 1.0); }
        }

        /// <summary>Fruit Fresh Density (g/cm3) </summary>
        [Description("Fruit Fresh Density (g/cm3)")]
        [Bounds(Lower = 0.1, Upper = 1.5)]
        [Units("g/cm^3")]
        public double FruitDensity
        {
            get { return _FruitDensity; }
            set { _FruitDensity = constrain(value, 0.1, 1.5); }
        }

        /// <summary> Date (d-mmm) of maximum bloom </summary>
        [Description("Date (d-mmm) of maximum bloom")]
        public string DateMaxBloom { get; set; }

        /// <summary>Start Linear Growth (0-200 Days After maximum bloom) </summary>
        [Description("Start Linear Growth (0-200 Days After maximum bloom)")]
        [Bounds(Lower = 0, Upper = 200)]
        [Units("days")]
        public int DAFStartLinearGrowth
        {
            get { return _DAFStartLinearGrowth; }
            set { _DAFStartLinearGrowth = (int)constrain((double)value, 0, 200); }
        }

        /// <summary>End Linear Growth (10-330 Days After maximum bloom) </summary>
        [Description("End Linear Growth (Days After maximum bloom)")]
        [Bounds(Lower = 10, Upper = 330)]
        [Units("days")]
        public int DAFEndLinearGrowth
        {
            get { return _DAFEndLinearGrowth; }
            set { _DAFEndLinearGrowth = (int)constrain((double)value, 10, 350); }
        }

        /// <summary>Max size (Days After maximum bloom) </summary>
        [Description("Max size (15 - 360 Days After maximum bloom)")]
        [Bounds(Lower = 15, Upper = 360)]
        [Units("days")]
        public int DAFMaxSize
        {
            get { return _DAFMaxSize; }
            set { _DAFMaxSize = (int)constrain((double)value, 10, 360); }
        }

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Plant strum = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        [Link(Type = LinkType.Scoped)]
        private Soil soil = null;

        [Link]
        private ISummary summary = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        [Link]
        private Weather weather = null;

        [Link]
        private Clock clock = null;

        /// <summary>The zones in the simulation/// </summary>
        //List<Zone> zones = null;

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SCRUM crop/// </summary>
        private Cultivar tree = null;

        /// <summary>Total daily solar radiation available per tree/// </summary>
        [Units("MJ per m tree")]
        [JsonIgnore]
        public double TotalSolarRadiation { get; set; }

        private bool hasAlleyZone = false;
        private bool hasRowZone = false;

        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"SpringDormancy","[STRUM].Phenology.SpringDormancy.DateToProgress = " },
            {"CanopyExpansion","[STRUM].Phenology.CanopyExpansion.DateToProgress = " },
            {"FullCanopy","[STRUM].Phenology.FullCanopy.DateToProgress = " },
            {"LeafFall", "[STRUM].Phenology.LeafFall.DateToProgress = " },
            {"WinterDormant", "[STRUM].Phenology.WinterDormancy.DateToProgress = " },
            {"MaxRootDepth","[STRUM].Root.Network.MaximumRootDepth.FixedValue = "},
            {"MaturePrunedCanopyBaseHeight","[STRUM].BaseHeight.PrunedCanopyBaseHeight.PrunedMatureCanopyBaseHeight.FixedValue = " },
            {"BaseHeightSeasonalIncrement","[STRUM].BaseHeight.SeasonalGrowth.BaseHeightSeasonalIncrement.FixedValue = " },
            {"MaturePrunedHeight","[STRUM].Height.PrunedCanopyDepth.MatureDepth.MaturePrunedHeight.FixedValue = " },
            {"SeasonalHeightIncrement","[STRUM].Height.SeasonalDepthGrowth.Increment.SeasonalHeightIncrement.FixedValue = " },
            {"MaturePrunedWidth","[STRUM].Width.PrunedWidth.MaturePrunedWidth.FixedValue = "},
            {"SeasonalWidthIncrement","[STRUM].Width.SeasonalGrowth.SeasonalWidthIncrement.FixedValue = " },
            {"ProductNConc","[STRUM].Fruit.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"ResidueNConc","[STRUM].Leaf.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"RootNConc","[STRUM].Root.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"WoodNConc","[STRUM].Wood.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"ExtinctCoeff","[STRUM].Leaf.Canopy.GreenExtinctionCoefficient.UnstressedCoeff.FixedValue = "},
            {"BaseLAI","[STRUM].Leaf.Canopy.GreenAreaIndex.Winter.BaseArea.FixedValue = " },
            {"AnnualDeltaLAI","[STRUM].Leaf.Canopy.GreenAreaIndex.SeasonalGrowth.Accumulated.DailyGrowth.Integral.AnnualDelta.FixedValue = " },
            {"DecidiousSenescence","[STRUM].Leaf.SenescenceRate.DecidiousSensecence.Switch.FixedValue = " },
            {"EverGreenSenescence", "[STRUM].Leaf.SenescenceRate.EvergreenSenescence.Coefficient.FixedValue = "},
            {"SummerPruneRemoval","[STRUM].Leaf.Canopy.GreenAreaIndex.SeasonalGrowth.Accumulated.FractionRemovedOnEvent = " },
            {"GSMax","[STRUM].Leaf.Canopy.Gsmax350 = " },
            {"R50","[STRUM].Leaf.Canopy.R50 = " },
            {"SurfaceKL","[STRUM].Root.Network.KLModifier.SurfaceKL.FixedValue = " },
            {"RUE","[STRUM].Leaf.Photosynthesis.RUE.FixedValue = " },
            {"PSBaseT","[STRUM].Leaf.Photosynthesis.FT.XYPairs.X[1] = " },
            {"PSLOptT","[STRUM].Leaf.Photosynthesis.FT.XYPairs.X[2] = " },
            {"PSUOptT","[STRUM].Leaf.Photosynthesis.FT.XYPairs.X[3] = " },
            {"PSMaxT","[STRUM].Leaf.Photosynthesis.FT.XYPairs.X[4] = " },
            {"FRGRUOptT", "[STRUM].Leaf.Canopy.FRGRer.FT.XYPairs.X[3] = " },
            {"FRGRMaxT", "[STRUM].Leaf.Canopy.FRGRer.FT.XYPairs.X[4] = " },
            {"FRGRMaxTY", "[STRUM].Leaf.Canopy.FRGRer.FT.XYPairs.Y[4] = " },
            {"InitialWoodWt","[STRUM].Wood.InitialWt.FixedValue = "},
            {"InitialRootWt", "[STRUM].Root.InitialWt.FixedValue = " },
            {"InitialFruitWt","[STRUM].Fruit.InitialWt.FixedValue = "},
            {"InitialLeafWt", "[STRUM].Leaf.InitialWt.FixedValue = " },
            {"YearsToMaturity","[STRUM].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"WoodWtAtMaturity","[STRUM].Wood.MatureWt.FixedValue = " },
            {"LeafWtAtMaturity","[STRUM].Leaf.TotalCarbonDemand.TotalDMDemand.SizeDemand.SizeDemand.SizeDemand.TargetSize.gPerM2.FixedValue = " },
            {"RootWtAtMaturity","[STRUM].Root.TotalCarbonDemand.TotalDMDemand.SizeDemand.SizeDemand.SizeDemand.TargetSize.gPerM2.FixedValue = " },
            {"WoodStorageFraction", "[STRUM].Wood.Carbon.PoolFractions.Storage.FixedValue = " },
            {"WoodStructuralFraction", "[STRUM].Wood.Carbon.PoolFractions.Structural.FixedValue = " },
            {"YearsToMaxRD","[STRUM].Root.Network.RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"Number","[STRUM].Fruit.Number.RetainedPostThinning.FixedValue = " },
            {"FruitDensity","[STRUM].Fruit.Density.FixedValue = " },
            {"DryMatterContent", "[STRUM].Fruit.DMC.FixedValue = " },
            {"DateMaxBloom","[STRUM].Phenology.DaysSinceFlowering.StartDate = "},
            {"DateResetFloweringDAS","[STRUM].Phenology.DaysSinceFlowering.ReduceDates = "},
            {"DAFStartLinearGrowth","[STRUM].Fruit.TotalCarbonDemand.RelativeFruitMass.Delta.Integral.XYPairs.X[2] = "},
            {"DAFEndLinearGrowth","[STRUM].Fruit.TotalCarbonDemand.RelativeFruitMass.Delta.Integral.XYPairs.X[3] = "},
            {"DAFMaxSize","[STRUM].Fruit.TotalCarbonDemand.RelativeFruitMass.Delta.Integral.XYPairs.X[4] = "},
            {"WaterStressPhoto","[STRUM].Leaf.Photosynthesis.Fw.XYPairs.Y[1] = "},
            {"WaterStressPhoto2","[STRUM].Leaf.Photosynthesis.Fw.XYPairs.Y[2] = "},
            {"WaterStressExtinct","[STRUM].Leaf.Canopy.GreenExtinctionCoefficient.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[STRUM].Root.Network.NUptakeSWFactor.XYPairs.Y[1] = "},
            {"PotentialFWPerFruit","[STRUM].Fruit.PotentialFWPerFruit.FixedValue = " },
            {"RowWidth","[STRUM].RowWidth.FixedValue = " },
            {"InterRowSpacing","[STRUM].InterRowSpacing.FixedValue = " },
            {"NFixationFrac","[STRUM].Nodule.Nitrogen.SupplyFunctions.Fixation.MaximumFixation.FixedValue = "}
        };


        /// <summary>
        /// Method that sets scurm running
        /// </summary>
        public void Establish()
        {
            double soilDepthMax = 0;

            var soilCrop = Structure.FindChild<SoilCrop>(strum.Name + "Soil", relativeTo: soil, recurse: true);
            var physical = Structure.FindChild<Physical>("Physical", relativeTo: soil, recurse: true);
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

            // STRUM sets soil KL to 1 and uses the KL modifier to determine appropriate kl based on root depth
            for (int d = 0; d < soilCrop.KL.Length; d++)
                soilCrop.KL[d] = 1.0;

            double rootDepth = Math.Min(MaxRD*1000, soilDepthMax);
            if (GRINZ)
            {  //Must add root zone prior to sowing the crop.  For some reason they (silently) dont add if you try to do so after the crop is established
                List<Zone> zones = Structure.FindChildren<Zone>(relativeTo: simulation).ToList();
                if (zones.Count > 2)
                    throw new Exception("Strip crop logic only set up for 2 zones, your simulation has more than this");
                if (zones.Count > 1)
                {
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
                }
            }
            else
            {
                List<Zone> zones = Structure.FindAll<Zone>(relativeTo: simulation).ToList();
                foreach (Zone z in zones)
                {
                    if (z.Name != zone.Name)
                    {
                        //This is the neighbour zone for the plant and roots are not growing into it so set to false
                        strum.ZonesToGrowRootsIn[z.Name] = false;
                    }
                }
            }

            string cropName = this.Name;
            double depth = Math.Min(this.MaxRD*1000 * this.AgeAtSimulationStart / this.YearsToMaxDimension, rootDepth);
            double population = TreePopulation;
            double rowWidth = RowSpacing;

            tree = CoeffCalc();
            strum.AddCultivar(tree);
            strum.Sow(cropName, population, depth, rowWidth);
            phenology.SetAge(AgeAtSimulationStart-1);
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

            double areaWidth = 0;
            woodMassAtMaxDimension = StrumBiomass.EstimateMatureWoodMassKg(
                                                                                    crownShape: CrownShape,
                                                                                    heightBottomPrePrune_m: MatureCanopyBaseHeight,  // ground → crown bottom, before prune
                                                                                    heightTopPrePrune_m: MatureHeight,     // crown top (mature height) before prune
                                                                                    heightTopPostPrune_m: MaturePrunedHeight,    // crown top after prune (topping allowed)
                                                                                    widthPrePrune_m: MatureWidth,         // canopy width before prune
                                                                                    widthPostPrune_m: MaturePrunedWidth,        // canopy width after prune
                                                                                    inRowSpacing_m: InterRowSpacing,          // per-tree length along row (spacing within row)
                                                                                    rowSpacing_m: RowSpacing,               // optional: distance between rows
                                                                                    woodDensity_kg_m3: WoodBulkDensity,       // oven-dry density [kg m^-3]
                                                                                    reffDBHatMaturity_cm: DBHatMaturity,            // diameter of stem at brest height when orchard tree is mature (cm)
                                                                                    reffArea_m2: TypicalCanopyArea
                                                                                   );

            if (this.WaterStress)
            {
                treeParams["WaterStressPhoto"] += "0.0";
                treeParams["WaterStressPhoto2"] += "0.2";
                treeParams["WaterStressExtinct"] += "0.2";
                treeParams["WaterStressNUptake"] += "0.0";
                treeParams["FRGRMaxTY"] += "0.0";
            }
            else
            {
                treeParams["WaterStressPhoto"] += "1.0";
                treeParams["WaterStressPhoto2"] += "1.0";
                treeParams["WaterStressExtinct"] += "1.0";
                treeParams["WaterStressNUptake"] += "1.0";
                treeParams["FRGRMaxTY"] += "1.0";
            }

            treeParams["SpringDormancy"] += BudBreakDate;
            treeParams["CanopyExpansion"] += StartFullCanopyDate;
            treeParams["FullCanopy"] += StartLeafFallDate;
            treeParams["LeafFall"] += EndLeafFallDate;
            treeParams["WinterDormant"] += WinterSolsticeDate;
            treeParams["MaxRootDepth"] += (1000 * MaxRD).ToString();
            treeParams["MaturePrunedCanopyBaseHeight"] += (1000 * MaturePrunedCanopyBaseHeight).ToString();
            treeParams["BaseHeightSeasonalIncrement"] += (1000 * (MaturePrunedCanopyBaseHeight - MatureCanopyBaseHeight)).ToString();
            treeParams["MaturePrunedHeight"] += (1000 * MaturePrunedHeight).ToString();
            treeParams["SeasonalHeightIncrement"] += (1000 * (MatureHeight - MaturePrunedHeight)).ToString();
            treeParams["MaturePrunedWidth"] += (1000 * MaturePrunedWidth).ToString();
            treeParams["SeasonalWidthIncrement"] += (1000 * (MatureWidth - MaturePrunedWidth)).ToString();
            treeParams["ProductNConc"] += FruitNConc.ToString();
            treeParams["ResidueNConc"] += LeafNConc.ToString();
            treeParams["RootNConc"] += RootNConc.ToString();
            treeParams["WoodNConc"] += WoodNConc.ToString();
            treeParams["ExtinctCoeff"] += ExtinctCoeff.ToString();
            treeParams["BaseLAI"] += ((Math.Log(1 - BaseCover) / (ExtinctCoeff * -1))).ToString();
            treeParams["AnnualDeltaLAI"] += ((Math.Log(1 - (MaxCover)) / (ExtinctCoeff * -1)) - (Math.Log(1 - BaseCover) / (ExtinctCoeff * -1))).ToString();
            treeParams["DecidiousSenescence"] += (Decidious ? "1" : "0");
            treeParams["EverGreenSenescence"] += ((Decidious ? 0 : 1)*0.0015).ToString();
            treeParams["SummerPruneRemoval"] += FRemoveSummerPrune.ToString();
            treeParams["GSMax"] += GSMax.ToString();
            treeParams["R50"] += R50.ToString();
            treeParams["RUE"] += RUE.ToString();
            treeParams["PSBaseT"] += PSBaseT.ToString();
            treeParams["PSLOptT"] += PSLOptT.ToString();
            treeParams["PSUOptT"] += PSUOptT.ToString();
            treeParams["PSMaxT"] += PSMaxT.ToString();
            treeParams["FRGRUOptT"] += PSUOptT.ToString();
            treeParams["FRGRMaxT"] += PSMaxT.ToString();
            treeParams["SurfaceKL"] += SurfaceKL.ToString();
            treeParams["YearsToMaturity"] += YearsToMaxDimension.ToString();
            treeParams["WoodWtAtMaturity"] += (woodMassAtMaxDimension * 1000).ToString();
            treeParams["LeafWtAtMaturity"] += MaximumLeafBiomass.ToString();
            treeParams["RootWtAtMaturity"] += MaximumRootBiomass.ToString();
            treeParams["WoodStorageFraction"] += WoodStorageFraction.ToString();
            treeParams["WoodStructuralFraction"] += (1-WoodStorageFraction).ToString();
            treeParams["YearsToMaxRD"] += YearsToMaxDimension.ToString();
            treeParams["Number"] += (Number*TreeCanopyArea).ToString();
            treeParams["FruitDensity"] += FruitDensity.ToString();
            treeParams["DryMatterContent"] += DMC.ToString();
            treeParams["DateMaxBloom"] += DateMaxBloom;
            treeParams["DateResetFloweringDAS"] += DateMaxBloom;
            treeParams["DAFStartLinearGrowth"] += DAFStartLinearGrowth.ToString();
            treeParams["DAFEndLinearGrowth"] +=  DAFEndLinearGrowth.ToString();
            treeParams["DAFMaxSize"] += DAFMaxSize.ToString();
            treeParams["PotentialFWPerFruit"] += PotentialFWPerFruit.ToString();
            treeParams["NFixationFrac"] += NFixationFrac.ToString();

            if (hasAlleyZone)
            {
                areaWidth = RowZoneWidth + AlleyZoneWidth;
                treeParams["RowWidth"] += (areaWidth).ToString();
                treeParams["InterRowSpacing"] += InterRowSpacing.ToString();
            }
            else
            {
                areaWidth = RowZoneWidth;
                treeParams["RowWidth"] += areaWidth.ToString();
                treeParams["InterRowSpacing"] += InterRowSpacing.ToString();
            }



            if (AgeAtSimulationStart <= 0)
                throw new Exception("STRUM needs to have a 'Tree Age at start of Simulation' > 1 years");
            if (woodMassAtMaxDimension <= 0)
                throw new Exception("STRUM needs to have a 'Wood Mass at maximum dimension > 0");
            
            
            pruningFractionWood = StrumBiomass.StrumPruningFraction(crownShape: CrownShape,
                                                                heightBottomPrePrune_m: MatureCanopyBaseHeight,  // ground → crown bottom, before prune
                                                                heightBottomPostPrune_m: MaturePrunedCanopyBaseHeight,
                                                                heightTopPrePrune_m: MatureHeight,     // crown top (mature height) before prune
                                                                heightTopPostPrune_m: MaturePrunedHeight,    // crown top after prune (topping allowed)
                                                                widthPrePrune_m: MatureWidth,         // canopy width before prune
                                                                widthPostPrune_m: MaturePrunedWidth,        // canopy width after prune
                                                                interRowSpacing_m: InterRowSpacing,          // per-tree length along row (spacing within row)
                                                                rowSpacing_m: RowSpacing,               // optional: distance between rows
                                                                matureDbh_cm: DBHatMaturity,
                                                                matureWoodMass: woodMassAtMaxDimension
                                                                );
            pruningFractionLeaf = (MatureWidth - MaturePrunedWidth) / MaturePrunedWidth;

            treeParams["InitialFruitWt"] += (0).ToString();
            double relativeInitialSize = Math.Min(1,(double)AgeAtSimulationStart / (double)YearsToMaxDimension);
            treeParams["InitialWoodWt"] += relativeInitialSize * woodMassAtMaxDimension * 1000 * (1 - pruningFractionWood);
            double PurnedCanopyArea = Math.Min(MaturePrunedWidth, areaWidth) * Math.Min(MaturePrunedWidth, InterRowSpacing);
            double InitialCanopyArea = PurnedCanopyArea * relativeInitialSize;
            treeParams["InitialLeafWt"] += (InitialCanopyArea * MaximumLeafBiomass * (Decidious ? 0 : 1)).ToString();
            treeParams["InitialRootWt"] += (InitialCanopyArea * MaximumRootBiomass * (Decidious ? 0.5 : 1)).ToString();

            string[] commands = new string[treeParams.Count];
            treeParams.Values.CopyTo(commands, 0);

            Cultivar TreeValues = new Cultivar(this.Name, commands);
            return TreeValues;
        }

        private void SetUpZones()
        {
            List<Zone> zones = Structure.FindChildren<Zone>(relativeTo: simulation).ToList();
            foreach (Zone z in zones)
            {
                if (z.Name == "Row")
                    hasRowZone = true;
                if (z.Name == "Alley")
                    hasAlleyZone = true;
            }
            if (hasRowZone == false)
                throw new Exception("Strum tree instance must be in a zone named Row");

            if (hasAlleyZone != false)
            {
                if (AlleyZoneWidth == 0)
                    throw new Exception("Alley Zone must have width > zero.  Either increase AlleyZoneWidthFrac to a positive value or remove alley zone is single zone simulation is required");
                Structure.Set("[Row].Width", (object)RowZoneWidth);
                Structure.Set("[Row].Length", (object)InterRowSpacing);
                Structure.Set("[Row].CanopyType", (object)"TreeRow");
                Structure.Set("[Alley].Width", (object)AlleyZoneWidth);
                Structure.Set("[Alley].Length", (object)InterRowSpacing);
                Structure.Set("[Alley].CanopyType", (object)"TreeRow");
            }
            else
            {
                Structure.Set("[Row].Width", (object)RowZoneWidth);
                Structure.Set("[Row].Length", (object)InterRowSpacing);
                Structure.Set("[Row].CanopyType", (object)"TreeRow");
            }


        }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartSimulation(object sender, EventArgs e)
        {
            if (DateUtilities.CompareDates(WinterSolsticeDate, clock.Today) < 0)
            {
                throw new Exception("STRUM simulations need to start on the winter solstice (" + WinterSolsticeDate + ") to ensure things initialise sensibly");
            }

            SetUpZones();
            Establish();
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            //Winter pruning
            if (DateUtilities.DatesAreEqual(EndLeafFallDate, clock.Today))
            {
                Prune(pruningFractionWood, pruningFractionLeaf);
            }

            //Fruit Picking
            string pickingDate = DateTime.Parse(DateMaxBloom + "-" + clock.Today.Year.ToString()).AddDays(DAFMaxSize).ToString("dd-MMM");
            if (DateUtilities.DatesAreEqual(pickingDate, clock.Today))
            {
                Pick();
            }

            //Summer pruning
            if ((SummerPruneDates != null) && (!String.IsNullOrEmpty(SummerPruneDates[0])))
            {
                foreach (string date in SummerPruneDates)
                {
                    DateTime pruneDate = DateUtilities.GetDate(date, clock.Today.Year);
                    if (clock.Today == pruneDate)
                    {
                        Prune(FRemoveSummerPrune, FRemoveSummerPrune);
                    }
                }
            }
        }

        private double constrain(double value, double min, double max)
        {
            if (value < min)
                throw new Exception(value.ToString() + " is lower than minimum allowed.  Enter a value greater than " + min.ToString());
            else if (value > max)
                throw new Exception(value.ToString() + " is higher than maximum allowed.  Enter a value less than " + max.ToString());
            else
            { }//Can we put something here to clear the status window so the error message goes away when a legit value is entered

            return MathUtilities.Bound(value, min, max);
        }

        /// <summary>
        /// Method called to invoke pruning.
        /// </summary>
        public void Prune(double fracWoodToResidue,double fracLeafToResidue)
        {
            Pruning?.Invoke(this, new EventArgs());

            IOrgan organ = Structure.FindChild<IOrgan>("Wood", relativeTo: (INodeModel)strum, recurse: true);
            (organ as IHasDamageableBiomass).RemoveBiomass(liveToRemove: 0,
                                                        deadToRemove: 0,
                                                        liveToResidue: fracWoodToResidue,
                                                        deadToResidue: 1.0);

            organ = Structure.FindChild<IOrgan>("Leaf", relativeTo: (INodeModel)strum, recurse: true);
            (organ as IHasDamageableBiomass).RemoveBiomass(liveToRemove: 0,
                                                        deadToRemove: 0,
                                                        liveToResidue: fracLeafToResidue,
                                                        deadToResidue: 1.0);
        }

        /// <summary>
        /// Method called to invoke pruning.
        /// </summary>
        public void Pick()
        {
            IOrgan organ = Structure.FindChild<IOrgan>("Fruit", relativeTo: (INodeModel)strum, recurse: true);
            (organ as IHasDamageableBiomass).RemoveBiomass(liveToRemove: 1.0,
                                                        deadToRemove: 0,
                                                        liveToResidue: 0,
                                                        deadToResidue: 1);
        }
    }
}
