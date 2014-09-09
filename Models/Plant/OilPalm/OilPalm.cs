using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

using System.Reflection;
using System.Collections;
using Models.PMF.Functions;
using Models.Soils;
using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;


namespace Models.PMF.OilPalm
{
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class OilPalm : ModelCollectionFromResource, ICrop
    {

        public NewCanopyType CanopyData 
        { 
            get 
            {
                NewCanopyType LocalCanopyData = new NewCanopyType();
                LocalCanopyData.cover = cover_green;
                LocalCanopyData.cover_tot = cover_tot;
                LocalCanopyData.height = 10000;
                LocalCanopyData.depth = 10000;
                LocalCanopyData.lai = LAI;
                LocalCanopyData.lai_tot = LAI;
                return LocalCanopyData; 
            } 
        }
        [XmlIgnore]
        public string plant_status = "out";
        [Link]
        Clock Clock = null;
        [Link]
        WeatherFile MetData = null;
        [Link]
        Soils.Soil Soil = null;
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Type of crop
        /// </summary>
        [Units("")]
        public string CropType { get { return "OilPalm"; } }

        /// <summary>
        /// Root system information
        /// </summary>
        public RootSystem RootSystem { get { return new RootSystem(); } }

        private Cultivar cultivarDefinition;

        /// <summary>
        /// Gets a list of cultivar names
        /// </summary>
        public string[] CultivarNames
        {
            get
            {
                SortedSet<string> cultivarNames = new SortedSet<string>();
                foreach (Cultivar cultivar in this.Cultivars)
                {
                    cultivarNames.Add(cultivar.Name);
                    if (cultivar.Aliases != null)
                    {
                        foreach (string alias in cultivar.Aliases)
                            cultivarNames.Add(alias);
                    }
                }

                return new List<string>(cultivarNames).ToArray();
            }
        }

        /// <summary>
        /// Gets a list of all cultivar definitions.
        /// </summary>
        private List<Cultivar> Cultivars
        {
            get
            {
                List<Cultivar> cultivars = new List<Cultivar>();
                foreach (Model model in this.Children.MatchingMultiple(typeof(Cultivar)))
                {
                    cultivars.Add(model as Cultivar);
                }

                return cultivars;
            }
        }

        /// <summary>
        /// Factor for Relative Growth Rate
        /// </summary>
        [Units("0-1")]
        public double FRGR { get { return 1; } }

        /// <summary>
        /// Potential evapotranspiration
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double PotentialEP { get; set; }

        /// <summary>
        /// MicroClimate supplies LightProfile
        /// </summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary>
        /// Height to top of plant canopy
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double height = 10000.0;

        /// <summary>
        /// Total cover provided by plant canopies
        /// </summary>
        [Units("0-1")]
        public double cover_tot {
            get { return cover_green + (1 - cover_green) * UnderstoryCoverGreen; }
                }

        /// <summary>
        /// Amount of rainfall intercepted by the plant canopy
        /// </summary>
        [Units("mm")]
        public double interception = 0.0;


        [Description("Maximum understory cover")]
        [Units("0-1")]
        public double UnderstoryCoverMax { get; set; }
        [Description("Fraction of understory that is legume")]
        [Units("0-1")]
        public double UnderstoryLegumeFraction { get; set; }
        [Description("Fraction of rainfall intercepted by canopy")]
        [Units("0-1")]
        public double InterceptionFraction { get; set; }
        [Description("Maximum palm root depth")]
        [Units("mm")]
        public double MaximumRootDepth { get; set; }

        double Ndemand = 0.0;

        /// <summary>
        /// Palm Rooting Depth
        /// </summary>
        [Units("mm")]
        public double RootDepth {get; set;}
        
        double[] PotSWUptake;

        double[] SWUptake;

        /// <summary>
        /// Potential daily evapotranspiration for the palm canopy
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double PEP { get; set; }

        /// <summary>
        /// Daily evapotranspiration from the palm canopy
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double EP { get; set; }

        /// <summary>
        /// Daily total plant dry matter growth
        /// </summary>
        [Units("g/m2")]
        public double DltDM { get; set; }
        double Excess = 0.0;

        /// <summary>
        /// Factor for daily water stress effect on photosynthesis
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double FW { get; set; }

        /// <summary>
        /// Factor for daily water stress effect on canopy expansion
        /// </summary>
        [Units("0-1")]
        double FWexpan = 0.0;

        /// <summary>
        /// Factor for daily nitrogen stress effect on photosynthesis
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double Fn { get; set; }

        /// <summary>
        /// Cumulative frond production since planting
        /// </summary>
        [XmlIgnore]
        [Units("/palm")]
        public double CumulativeFrondNumber { get; set; }

        /// <summary>
        /// Cumulative bunch production since planting
        /// </summary>
        [XmlIgnore]
        [Units("/palm")]
        public double CumulativeBunchNumber { get; set; }

        /// <summary>
        /// Proportion of daily growth partitioned into reproductive parts
        /// </summary>
        [Units("0-1")]
        public double ReproductiveGrowthFraction {get; set;}

        /// <summary>
        /// Amount of carbon limitation for todays potential growth (ie supply/demand)
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double CarbonStress { get; set; }

        /// <summary>
        /// Number of bunches harvested on a harvesting event
        /// </summary>
        [XmlIgnore]
        [Units("/palm")]
        public double HarvestBunches { get; set; }

        /// <summary>
        /// Mass of harvested FFB on a harvesting event
        /// </summary>
        [XmlIgnore]
        [Units("t/ha")]
        public double HarvestFFB { get; set; }

        /// <summary>
        /// Nitrogen removed at a harvesting event
        /// </summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double HarvestNRemoved { get; set; }

        /// <summary>
        /// Mean size of bunches at a harvesting event
        /// </summary>
        [XmlIgnore]
        [Units("kg")]
        public double HarvestBunchSize { get; set; }

        /// <summary>
        /// Time since planting
        /// </summary>
        [XmlIgnore]
        [Units("y")]
        public double Age { get; set; }

        [XmlIgnore]
        [Units("/m^2")]
        public double Population { get; set; }

        [XmlIgnore]
        public SowPlant2Type SowingData = new SowPlant2Type();

        /// <summary>
        /// Potential daily nitrogen uptake from each soil layer by palms
        /// </summary>
        [Units("kg/ha")]
        double[] PotNUptake;

        /// <summary>
        /// Daily nitrogen uptake from each soil layer by palms
        /// </summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] NUptake { get; set; }

        /// <summary>
        /// Daily stem dry matter growth
        /// </summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public double StemGrowth { get; set; }
        /// <summary>
        /// Daily frond dry matter growth
        /// </summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public double FrondGrowth { get; set; }
        /// <summary>
        /// Daily root dry matter growth
        /// </summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public double RootGrowth { get; set; }
        /// <summary>
        /// Daily bunch dry matter growth
        /// </summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public double BunchGrowth { get; set; }

        [XmlIgnore]
        private List<FrondType> Fronds = new List<FrondType>();
        [XmlIgnore]
        private List<BunchType> Bunches = new List<BunchType>();
        [XmlIgnore]
        private List<RootType> Roots = new List<RootType>();

        [Link]
        [Description("Frond appearance rate under optimal temperature conditions.")]
        [Units("d")]
        Function FrondAppearanceRate = null;
        [Link]
        [Description("Relative rate of plant development (e.g. frond appearance) as affected by air temperature")]
        [Units("0-1")]
        Function RelativeDevelopmentalRate = null;
        [Link]
        [Description("Maximum area of an individual frond")]
        [Units("m^2")]
        Function FrondMaxArea = null;
        [Link]
        [Description("Beer-Lambert law extinction coefficient for direct beam radiation")]
        [Units("unitless")]
        Function DirectExtinctionCoeff = null;
        [Link]
        [Description("Beer-Lambert law extinction coefficient for diffuse beam radiation")]
        [Units("unitless")]
        Function DiffuseExtinctionCoeff = null;
        [Link]
        [Description("The number of expanding fronds at a given point in time.")]
        [Units("/palm")]
        Function ExpandingFronds = null;
        [Link]
        [Description("The number of fronds on the palm at planting")]
        [Units("/palm")]
        Function InitialFrondNumber = null;
        [Link]
        [Description("Radiation use efficiency for total short wave radiation.")]
        [Units("g/m^2")]
        Function RUE = null;
        [Link]
        [Description("Root front velocity")]
        [Units("mm/d")]
        Function RootFrontVelocity = null;
        [Link]
        [Description("Fraction of the live root system that senesces per day (ie first order decay coefficient)")]
        [Units("/d")]
        Function RootSenescenceRate = null;
        [Link]
        [Description("Amount of frond area per unit frond mass. Used to calculate frond dry matter demand")]
        [Units("m^2/g")]
        Function SpecificLeafArea = null;
        [Link]
        [Description("Maximum amount of frond area per unit frond mass. Used to limit area growth when dry matter is limiting")]
        [Units("m^2/g")]
        Function SpecificLeafAreaMax = null;
        [Link]
        [Description("Fraction of daily growth partitioned into the root system")]
        [Units("0-1")]
        Function RootFraction = null;
        [Link]
        [Description("Maximum bunch size on a dry mass basis")]
        [Units("g")]
        Function BunchSizeMax = null;
        [Link]
        [Description("Female fraction of a cohort's population of inflorescences as affected by age")]
        [Units("0-1")]
        Function FemaleFlowerFraction = null;
        [Link]
        [Description("Fraction of inflorescences that become female each day during the gender determination phase")]
        [Units("0-1")]
        Function FFFStressImpact = null;
        [Link]
        [Description("Ratio of stem to frond growth as affected by plant age")]
        [Units("g/g")]
        Function StemToFrondFraction = null;
        [Link]
        [Description("Fraction of inflorescences that become aborted each day during the flower abortion phase")]
        [Units("0-1")]
        Function FlowerAbortionFraction = null;
        [Link]
        [Description("Fraction of bunches that fail each day during the bunch failure phase")]
        [Units("0-1")]
        Function BunchFailureFraction = null;
        
        private double InitialRootDepth = 300;

        [Link]
        [Description("NO3 Uptake coefficient - Fraction of NO3 available at a soil concentration of 1ppm ")]
        [Units("/ppm")]
        Function KNO3 = null;

        [Link]
        [Description("Stem nitrogen concentration on dry mass basis")]
        [Units("%")]
        Function StemNConcentration = null;
        [Link]
        [Description("Bunch nitrogen concentration on dry mass basis")]
        [Units("%")]
        Function BunchNConcentration = null;
        [Link]
        [Description("Root nitrogen concentration on dry mass basis")]
        [Units("%")]
        Function RootNConcentration = null;
        [Link]
        [Description("Conversion factor to convert carbohydrate to bunch dry mass to account for oil content")]
        [Units("g/g")]
        Function BunchOilConversionFactor = null;
        [Link] 
        [Description("Fractional contribution of water to fresh bunch mass")]
        [Units("g/g")]
        Function RipeBunchWaterContent = null;
        [Link]
        [Description("Frond number removed when bunches are ready for harvest - used to determine harvest time")]
        [Units("/palm")]
        Function HarvestFrondNumber = null;
        [Link]
        [Description("Maximum frond nitrogen concentration on dry mass basis")]
        [Units("%")]
        Function FrondMaximumNConcentration = null;
        [Link]
        [Description("Critical frond nitrogen concentration on dry mass basis")]
        [Units("%")]
        Function FrondCriticalNConcentration = null;
        [Link]
        [Description("Minimum frond nitrogen concentration on dry mass basis")]
        [Units("%")]
        Function FrondMinimumNConcentration = null;
        
        /// <summary>
        /// Proportion of green cover provided by the understory canopy
        /// </summary>
        [Units("0-1")]
        public double UnderstoryCoverGreen { get; set; }
        private double UnderstoryKLmax = 0.12;

        /// <summary>
        /// Potential soil water uptake from each soil layer by understory
        /// </summary>
        double[] UnderstoryPotSWUptake;
        
        /// <summary>
        /// Actual Soil water uptake from each soil layer by understory
        /// </summary>
        double[] UnderstorySWUptake;
        /// <summary>
        /// Potential nitrogen water uptake from each soil layer by understory
        /// </summary>
        public double[] UnderstoryPotNUptake { get; set; }

        /// <summary>
        /// Actual soil nitrogen uptake from each soil layer by understory
        /// </summary>
        [XmlIgnore]
        public double[] UnderstoryNUptake { get; set; }

        /// <summary>
        /// Understory rooting depth
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double UnderstoryRootDepth = 500;

        /// <summary>
        /// Potential daily evapotranspiration for the understory
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double UnderstoryPEP = 0;

        /// <summary>
        /// Daily evapotranspiration for the understory
        /// </summary>
        [XmlIgnore]
        [Units("mm")]
        public double UnderstoryEP = 0;

        /// <summary>
        /// Understory plant water stress factor
        /// </summary>
        [XmlIgnore]
        [Units("0-1")]
        public double UnderstoryFW = 0;

        /// <summary>
        /// Daily understory dry matter growth
        /// </summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public double UnderstoryDltDM = 0;

        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);
        public event NitrogenChangedDelegate NitrogenChanged;

        /// <summary>
        /// Daily understory nitrogen fixation
        /// </summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double UnderstoryNFixation { get; set; }

        [Serializable]
        public class RootType
        {
            public double Mass = 0;
            public double N = 0;
            public double Length = 0;
        }

        [Serializable]
        public class FrondType
        {
            public double Mass; // g/frond
            public double N;    // g/frond
            public double Area; // m2/frond
            public double Age;  //days
        }

        [Serializable]
        public class BunchType
        {
            public double Mass = 0;
            public double N = 0;
            public double Age = 0;
            public double FemaleFraction = 1;
        }


        [XmlIgnore]
        //[Description("Stem mass on a dry matter basis")]
        [Units("g/m^2")]
        public double StemMass { get; set; }

        [XmlIgnore]
        //[Description("Stem nitrogen")]
        [Units("g/m^2")]
        public double StemN { get; set; }

        [XmlIgnore]
        [Description("Stem nitrogen concention on a dry mass basis")]
        [Units("%")]
        double StemNConc
        {
            get
            {
                if (StemMass > 0)
                    return StemN / StemMass * 100;
                else
                    return 0.0;
            }
        }

        private bool CropInGround = false;

        //[Description("Flag to indicate whether oil palm has been planted")]
        [Units("True/False")]
        [XmlIgnore]
        public bool IsCropInGround
        {
            get { return CropInGround; }
            set { CropInGround = value; }
        }

        // The following event handler will be called once at the beginning of the simulation
        public override void OnSimulationCommencing()
        {
            //zero public properties
            CumulativeFrondNumber = 0;
            CumulativeBunchNumber = 0;
            CarbonStress = 0;
            HarvestBunches = 0;
            HarvestFFB = 0;
            HarvestNRemoved = 0;
            HarvestBunchSize = 0;
            Age = 0;
            Population = 0;
            UnderstoryNFixation = 0;
            StemMass = 0;
            StemN = 0;
            CropInGround = false;
            NUptake = new double[] { 0 };
            UnderstoryNUptake = new double[] { 0 };
            UnderstoryCoverGreen = 0;
            StemGrowth = 0;
            RootGrowth = 0;
            FrondGrowth = 0;
            BunchGrowth = 0;
            Fn = 1;
            FW = 1;
            PEP = 0;
            EP = 0;
            RootDepth = 0;
            DltDM = 0;
            ReproductiveGrowthFraction = 0;

            Fronds = new List<FrondType>();
            Bunches = new List<BunchType>();
            Roots = new List<RootType>();

            
            //MyPaddock.Parent.ChildPaddocks
            PotSWUptake = new double[Soil.Thickness.Length];
            SWUptake = new double[Soil.Thickness.Length];
            PotNUptake = new double[Soil.Thickness.Length];
            NUptake = new double[Soil.Thickness.Length];

            UnderstoryPotSWUptake = new double[Soil.Thickness.Length];
            UnderstorySWUptake = new double[Soil.Thickness.Length];
            UnderstoryPotNUptake = new double[Soil.Thickness.Length];
            UnderstoryNUptake = new double[Soil.Thickness.Length];

            for (int i = 0; i < Soil.Thickness.Length; i++)
            {
                RootType R = new RootType();
                Roots.Add(R);
                Roots[i].Mass = 0.1;
                Roots[i].N = Roots[i].Mass * RootNConcentration.Value / 100;
            }

            for (int i = 0; i < (int)InitialFrondNumber.Value; i++)
            {
                FrondType F = new FrondType();
                F.Age = ((int)InitialFrondNumber.Value - i) * FrondAppearanceRate.Value;
                F.Area = SizeFunction(F.Age);
                F.Mass = F.Area / SpecificLeafArea.Value;
                F.N = F.Mass * FrondCriticalNConcentration.Value / 100.0;
                Fronds.Add(F);
                CumulativeFrondNumber += 1;
            }
            for (int i = 0; i < (int)InitialFrondNumber.Value + 60; i++)
            {
                BunchType B = new BunchType();
                B.FemaleFraction = FemaleFlowerFraction.Value;
                Bunches.Add(B);
            }
            RootDepth = InitialRootDepth;
        }

        public void Sow(string cultivar, double population, double depth = 100, double RowSpacing = 150, double MaxCover = 1, double BudNumber = 1, string cropClass = "Plant")
        {
            SowingData = new SowPlant2Type();
            SowingData.Population = population;
            this.Population = population;
            SowingData.Depth = depth;
            SowingData.Cultivar = cultivar;
            SowingData.MaxCover = MaxCover;
            SowingData.BudNumber = BudNumber;
            SowingData.RowSpacing = RowSpacing;
            SowingData.CropClass = cropClass;
            CropInGround = true;

            if (SowingData.Cultivar == "")
                throw new Exception("Cultivar not specified on sow line.");

            // Find cultivar and apply cultivar overrides.
            cultivarDefinition = Cultivar.Find(Cultivars, SowingData.Cultivar);
            cultivarDefinition.Apply(this);

            // Invoke a sowing event.
            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

            Summary.WriteMessage(FullPath, string.Format("A crop of OilPalm was sown today at a population of " + population + " plants/m2 with " + BudNumber + " buds per plant at a row spacing of " + RowSpacing + " and a depth of " + depth + " mm"));
        }

        /// <summary>
        /// Harvest the crop.
        /// </summary>
        public void Harvest()
        {
            // Invoke a harvesting event.
            if (Harvesting != null)
                Harvesting.Invoke(this, new EventArgs());
        }

        public event NewCropDelegate NewCrop;

        public event EventHandler Sowing;

        public event EventHandler Harvesting;

        public event FOMLayerDelegate IncorpFOM;

        public event BiomassRemovedDelegate BiomassRemoved;

        [EventSubscribe("Sow")]
        private void OnSow(SowPlant2Type Sow)
        {
            SowingData = Sow;
            plant_status = "alive";
            Population = SowingData.Population;

            if (NewCrop != null)
            {
                NewCropType Crop = new NewCropType();
                Crop.crop_type = CropType;
                Crop.sender = Name;
                NewCrop.Invoke(Crop);
            }

            if (Sowing != null)
                Sowing.Invoke(this, new EventArgs());

        }

        // The following event handler will be called each day at the beginning of the day
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            interception = MetData.Rain * InterceptionFraction;
        }

        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (!CropInGround)
                return;

            DoWaterBalance();
            DoGrowth();
            DoNBalance();
            DoDevelopment();
            DoFlowerAbortion();
            DoGenderDetermination();
            DoUnderstory();
        }

        /// <summary>
        /// Placeholder for SoilArbitrator
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Soils.UptakeInfo GetPotSWUptake(Soils.UptakeInfo info)
        {
            return info;
        }

        private void DoFlowerAbortion()
        {
            // Main abortion stage occurs around frond 11 over 3 plastochrons

            int B = Fronds.Count - 11;
            if (B > 0)
            {
                double AF = (1 - FlowerAbortionFraction.Value);
                Bunches[B - 1].FemaleFraction *= AF;
                Bunches[B].FemaleFraction *= AF;
                Bunches[B + 1].FemaleFraction *= AF;
            }

            // Bunch failure stage occurs around frond 21 over 1 plastochron
            B = Fronds.Count - 21;
            if (B > 0)
            {
                double BFF = (1 - BunchFailureFraction.Value);
                Bunches[B].FemaleFraction *= BFF;
            }

        }

        private void DoGenderDetermination()
        {
            // Main abortion stage occurs 25 plastochroons before spear leaf over 9 plastochrons
            // NH Try 20 as this allows for 26 per year and harvest at 32 - ie 26*2 - 32
            int B = 53; //Fronds.Count + 20;
            Bunches[B - 4].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B - 3].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B - 2].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B - 1].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 0].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 1].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 2].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 3].FemaleFraction *= (1.0 - FFFStressImpact.Value);
            Bunches[B + 4].FemaleFraction *= (1.0 - FFFStressImpact.Value);


        }
        private void DoRootGrowth(double Allocation)
        {
            int RootLayer = LayerIndex(RootDepth);
            RootDepth = RootDepth + RootFrontVelocity.Value * Soil.XF("OilPalm")[RootLayer];
            RootDepth = Math.Min(MaximumRootDepth, RootDepth);
            RootDepth = Math.Min(Utility.Math.Sum(Soil.SoilWater.dlayer), RootDepth);

            // Calculate Root Activity Values for water and nitrogen
            double[] RAw = new double[Soil.SoilWater.dlayer.Length];
            double[] RAn = new double[Soil.SoilWater.dlayer.Length];
            double TotalRAw = 0;
            double TotalRAn = 0;

            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
            {
                if (layer <= LayerIndex(RootDepth))
                    if (Roots[layer].Mass > 0)
                    {
                        RAw[layer] = SWUptake[layer] / Roots[layer].Mass
                                   * Soil.SoilWater.dlayer[layer]
                                   * RootProportion(layer, RootDepth);
                        RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning

                        RAn[layer] = NUptake[layer] / Roots[layer].Mass
                                   * Soil.SoilWater.dlayer[layer]
                                   * RootProportion(layer, RootDepth);
                        RAn[layer] = Math.Max(RAw[layer], 1e-10);  // Make sure small numbers to avoid lack of info for partitioning

                    }
                    else if (layer > 0)
                    {
                        RAw[layer] = RAw[layer - 1];
                        RAn[layer] = RAn[layer - 1];
                    }
                    else
                    {
                        RAw[layer] = 0;
                        RAn[layer] = 0;
                    }
                TotalRAw += RAw[layer];
                TotalRAn += RAn[layer];
            }
            double allocated = 0;
            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
            {
                if (TotalRAw > 0)

                    Roots[layer].Mass += Allocation * RAw[layer] / TotalRAw;
                else if (Allocation > 0)
                    throw new Exception("Error trying to partition root biomass");
                allocated += Allocation * RAw[layer] / TotalRAw;
            }



            // Do Root Senescence
            FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Soil.SoilWater.dlayer.Length];

            for (int layer = 0; layer < Soil.SoilWater.dlayer.Length; layer++)
            {
                double Fr = RootSenescenceRate.Value;
                double DM = Roots[layer].Mass * Fr * 10.0;
                double N = Roots[layer].N * Fr * 10.0;
                Roots[layer].Mass *= (1.0 - Fr);
                Roots[layer].N *= (1.0 - Fr);
                Roots[layer].Length *= (1.0 - Fr);


                FOMType fom = new FOMType();
                fom.amount = (float)DM;
                fom.N = (float)N;
                fom.C = (float)(0.44 * DM);
                fom.P = 0;
                fom.AshAlk = 0;

                FOMLayerLayerType Layer = new FOMLayerLayerType();
                Layer.FOM = fom;
                Layer.CNR = 0;
                Layer.LabileP = 0;

                FOMLayers[layer] = Layer;
            }
            FOMLayerType FomLayer = new FOMLayerType();
            FomLayer.Type = CropType;
            FomLayer.Layer = FOMLayers;
            IncorpFOM.Invoke(FomLayer);


        }
        private void DoGrowth()
        {
            double RUEclear = RUE.Value;
            double RUEcloud = RUE.Value * (1 + 0.33 * cover_green);
            double WF = DiffuseLightFraction;
            double RUEadj = WF * WF * RUEcloud + (1 - WF * WF) * RUEclear;
            DltDM = RUEadj * Fn * MetData.Radn * cover_green * FW;

            double DMAvailable = DltDM;
            double[] FrondsAge = new double[Fronds.Count];
            double[] FrondsAgeDelta = new double[Fronds.Count];

            //precalculate  above two arrays
            Parallel.For(0, Fronds.Count, i =>
                {
                    FrondsAge[i] = SizeFunction(Fronds[i].Age);
                    FrondsAgeDelta[i] = SizeFunction(Fronds[i].Age + DeltaT);
                });

            RootGrowth = (DltDM * RootFraction.Value);
            DMAvailable -= RootGrowth;
            DoRootGrowth(RootGrowth);

            double[] BunchDMD = new double[Bunches.Count];
            for (int i = 0; i < 6; i++)
                BunchDMD[i] = BunchSizeMax.Value / (6 * FrondAppearanceRate.Value / DeltaT) * Fn * Population * Bunches[i].FemaleFraction * BunchOilConversionFactor.Value;
            double TotBunchDMD = Utility.Math.Sum(BunchDMD);

            double[] FrondDMD = new double[Fronds.Count];
            Parallel.For(0, Fronds.Count, i =>
                FrondDMD[i] = (FrondsAgeDelta[i] - FrondsAge[i]) / SpecificLeafArea.Value * Population * Fn);
            double TotFrondDMD = Utility.Math.Sum(FrondDMD);

            double StemDMD = TotFrondDMD * StemToFrondFraction.Value;

            double Fr = Math.Min(DMAvailable / (TotBunchDMD + TotFrondDMD + StemDMD), 1.0);
            Excess = 0.0;
            if (Fr > 1.0)
                Excess = DMAvailable - (TotBunchDMD + TotFrondDMD + StemDMD);

            //why is this here? -JF
            if (Age > 10 && Fr < 1)
            { }

            BunchGrowth = 0; // zero the daily value before incrementally building it up again with today's growth of individual bunches

            for (int i = 0; i < 6; i++)
            {
                double IndividualBunchGrowth = BunchDMD[i] * Fr / Population / BunchOilConversionFactor.Value;
                Bunches[i].Mass += IndividualBunchGrowth;
                BunchGrowth += IndividualBunchGrowth * Population;
            }
            if (DltDM > 0)
                ReproductiveGrowthFraction = TotBunchDMD * Fr / DltDM;
            else
                ReproductiveGrowthFraction = 0;

            FrondGrowth = 0; // zero the daily value before incrementally building it up again with today's growth of individual fronds

            Parallel.For(0, Fronds.Count, i =>
            {
                double IndividualFrondGrowth = FrondDMD[i] * Fr / Population;
                Fronds[i].Mass += IndividualFrondGrowth;
                FrondGrowth += IndividualFrondGrowth * Population;
                if (Fr >= SpecificLeafArea.Value / SpecificLeafAreaMax.Value)
                    Fronds[i].Area += (FrondsAgeDelta[i] - FrondsAge[i]) * Fn;
                else
                    Fronds[i].Area += IndividualFrondGrowth * SpecificLeafAreaMax.Value;

            });

            StemGrowth = StemDMD * Fr;// +Excess; 
            StemMass += StemGrowth;

            CarbonStress = Fr;

        }
        private void DoDevelopment()
        {
            Age = Age + 1.0 / 365.0;
            //for (int i = 0; i < Frond.Length; i++)
            //    Frond[i].Age += 1;
            foreach (FrondType F in Fronds)
            {
                F.Age += DeltaT;
                //F.Area = SizeFunction(F.Age);
            }
            if (Fronds[Fronds.Count - 1].Age >= FrondAppearanceRate.Value)
            {
                FrondType F = new FrondType();
                Fronds.Add(F);
                CumulativeFrondNumber += 1;

                BunchType B = new BunchType();
                B.FemaleFraction = FemaleFlowerFraction.Value;
                Bunches.Add(B);
            }

            //if (Fronds[0].Age >= (40 * FrondAppRate.Value))
            if (FrondNumber > Math.Round(HarvestFrondNumber.Value))
            {
                HarvestBunches = Bunches[0].FemaleFraction;
                double HarvestYield = Bunches[0].Mass * Population / (1.0 - RipeBunchWaterContent.Value);
                HarvestFFB = HarvestYield / 100;
                HarvestNRemoved = Bunches[0].N * Population * 10;
                HarvestBunchSize = Bunches[0].Mass / (1.0 - RipeBunchWaterContent.Value) / Bunches[0].FemaleFraction;
                if (Harvesting != null)
                    Harvesting.Invoke(this, new EventArgs());
                // Now rezero these outputs - they can only be output non-zero on harvesting event.
                HarvestBunches = 0.0;
                HarvestYield = 0.0;
                HarvestFFB = 0.0;
                HarvestBunchSize = 0.0;
                HarvestNRemoved = 0.0;

                CumulativeBunchNumber += Bunches[0].FemaleFraction;
                Bunches.RemoveAt(0);

                BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
                BiomassRemovedData.crop_type = CropType;
                BiomassRemovedData.dm_type = new string[1] { "frond" };
                BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(Fronds[0].Mass * Population * 10) };
                BiomassRemovedData.dlt_dm_n = new float[1] { (float)(Fronds[0].N * Population * 10) };
                BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
                BiomassRemovedData.fraction_to_residue = new float[1] { 1 };
                Fronds.RemoveAt(0);
                BiomassRemoved.Invoke(BiomassRemovedData);
            }
        }
        private void DoWaterBalance()
        {
            PEP = Soil.SoilWater.eo * cover_green;


            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
                PotSWUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * Soil.KL("OilPalm")[j] * (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]));

            double TotPotSWUptake = Utility.Math.Sum(PotSWUptake);

            EP = 0.0;
            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                SWUptake[j] = PotSWUptake[j] * Math.Min(1.0, PEP / TotPotSWUptake);
                EP += SWUptake[j];
                Soil.SoilWater.sw_dep[j] = Soil.SoilWater.sw_dep[j] - SWUptake[j];

            }

            if (PEP > 0.0)
            {
                FW = EP / PEP;
                //FWexpan = Math.Max(0.0, Math.Min(1.0, (TotPotSWUptake / PEP - 0.5) / 0.6));
                FWexpan = Math.Max(0.0, Math.Min(1.0, (TotPotSWUptake / PEP - 0.5) / 1.0));

            }
            else
            {
                FW = 1.0;
                FWexpan = 1.0;
            }
        }

        private void DoNBalance()
        {
            NitrogenChangedType NUptakeType = new NitrogenChangedType();
            NUptakeType.Sender = Name;
            NUptakeType.SenderType = "Plant";
            NUptakeType.DeltaNO3 = new double[Soil.SoilWater.dlayer.Length];
            NUptakeType.DeltaNH4 = new double[Soil.SoilWater.dlayer.Length];

            double StartN = PlantN;

            double StemNDemand = StemGrowth * StemNConcentration.Value / 100.0 * 10.0;  // factor of 10 to convert g/m2 to kg/ha
            double RootNDemand = Math.Max(0.0, (RootMass * RootNConcentration.Value / 100.0 - RootN)) * 10.0;  // kg/ha
            double FrondNDemand = Math.Max(0.0, (FrondMass * FrondMaximumNConcentration.Value / 100.0 - FrondN)) * 10.0;  // kg/ha 
            double BunchNDemand = Math.Max(0.0, (BunchMass * BunchNConcentration.Value / 100.0 - BunchN)) * 10.0;  // kg/ha 

            Ndemand = StemNDemand + FrondNDemand + RootNDemand + BunchNDemand;  //kg/ha


            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                double swaf = 0;
                swaf = (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]) / (Soil.SoilWater.dul_dep[j] - Soil.SoilWater.ll15_dep[j]);
                swaf = Math.Max(0.0, Math.Min(swaf, 1.0));
                double no3ppm = Soil.SoilNitrogen.no3[j] * (100.0 / (Soil.BD[j] * Soil.SoilWater.dlayer[j]));
                PotNUptake[j] = Math.Max(0.0, RootProportion(j, RootDepth) * KNO3.Value * Soil.SoilNitrogen.no3[j] * swaf);
            }

            double TotPotNUptake = Utility.Math.Sum(PotNUptake);
            double Fr = Math.Min(1.0, Ndemand / TotPotNUptake);

            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                NUptake[j] = PotNUptake[j] * Fr;
                NUptakeType.DeltaNO3[j] = -NUptake[j];
            }

            if (NitrogenChanged != null)
                NitrogenChanged.Invoke(NUptakeType);

            Fr = Math.Min(1.0, Math.Max(0, Utility.Math.Sum(NUptake) / BunchNDemand));
            double DeltaBunchN = BunchNDemand * Fr;

            double Tot = 0;
            foreach (BunchType B in Bunches)
            {
                Tot += Math.Max(0.0, B.Mass * BunchNConcentration.Value / 100.0 - B.N) * Fr / SowingData.Population;
                B.N += Math.Max(0.0, B.Mass * BunchNConcentration.Value / 100.0 - B.N) * Fr;
            }

            // Calculate fraction of N demand for Vegetative Parts
            if ((Ndemand - DeltaBunchN) > 0)
                Fr = Math.Max(0.0, ((Utility.Math.Sum(NUptake) - DeltaBunchN) / (Ndemand - DeltaBunchN)));
            else
                Fr = 0.0;

            StemN += StemNDemand / 10 * Fr;

            double[] RootNDef = new double[Soil.SoilWater.ll15_dep.Length];
            double TotNDef = 1e-20;
            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
            {
                RootNDef[j] = Math.Max(0.0, Roots[j].Mass * RootNConcentration.Value / 100.0 - Roots[j].N);
                TotNDef += RootNDef[j];
            }
            for (int j = 0; j < Soil.SoilWater.ll15_dep.Length; j++)
                Roots[j].N += RootNDemand / 10 * Fr * RootNDef[j] / TotNDef;

            foreach (FrondType F in Fronds)
                F.N += Math.Max(0.0, F.Mass * FrondMaximumNConcentration.Value / 100.0 - F.N) * Fr;

            double EndN = PlantN;
            double Change = EndN - StartN;
            double Uptake = Utility.Math.Sum(NUptake) / 10.0;
            if (Math.Abs(Change - Uptake) > 0.001)
                throw new Exception("Error in N Allocation");

            double Nact = FrondNConc;
            double Ncrit = FrondCriticalNConcentration.Value;
            double Nmin = FrondMinimumNConcentration.Value;
            Fn = Math.Min(Math.Max(0.0, (Nact - Nmin) / (Ncrit - Nmin)), 1.0);

        }



        [Description("Leaf Area Index")]
        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                if (CropInGround)
                {
                    double FrondArea = 0.0;
                    foreach (FrondType F in Fronds)
                        FrondArea += F.Area;
                    return FrondArea * SowingData.Population;
                }
                else
                    return 0;
            }

        }

        [Description("Area of an average frond")]
        [Units("m^2")]
        public double FrondArea
        {
            get
            {
                double A = 0.0;

                foreach (FrondType F in Fronds)
                    A += F.Area;
                return A / Fronds.Count;
            }

        }

        [Units("m^2")]
        [Description("Area of the 17th frond")]
        public double Frond17Area
        {
            get
            {
                //note frond 17 is 18th frond because they ignore the spear leaf
                if (Fronds.Count > 18)
                    return Fronds[Fronds.Count - 18].Area;
                else
                    return 0;

            }

        }

        [Description("Frond mass on a dry mass basis")]
        [Units("g/m^2")]
        public double FrondMass
        {
            get
            {
                double FrondMass = 0.0;

                //for (int i = 0; i < Frond.Length; i++)
                //   FrondArea = FrondArea + Frond[i].Area;
                foreach (FrondType F in Fronds)
                    FrondMass += F.Mass;
                return FrondMass * Population;
            }

        }

        [Description("Frond nitrogen content")]
        [Units("g/m^2")]
        public double FrondN
        {
            get
            {
                double FrondN = 0.0;

                //for (int i = 0; i < Frond.Length; i++)
                //   FrondArea = FrondArea + Frond[i].Area;
                foreach (FrondType F in Fronds)
                    FrondN += F.N;
                return FrondN * SowingData.Population;
            }

        }

        [Description("Frond nitrogen concentration on a dry mass basis")]
        [Units("%")]
        public double FrondNConc
        {
            get
            {
                return FrondN / FrondMass * 100.0;
            }

        }

        [Description("Bunch mass on a dry mass basis")]
        [Units("g/m^2")]
        public double BunchMass
        {
            get
            {
                double BunchMass = 0.0;

                foreach (BunchType B in Bunches)
                    BunchMass += B.Mass;
                return BunchMass * SowingData.Population;
            }

        }

        [Description("Bunch nitrogen content")]
        [Units("g/m^2")]
        public double BunchN
        {
            get
            {
                double BunchN = 0.0;

                foreach (BunchType B in Bunches)
                    BunchN += B.N * SowingData.Population;
                return BunchN;
            }

        }

        [Description("Bunch nitrogen concentration on a dry mass basis")]
        [Units("%")]
        public double BunchNConc
        {
            get
            {
                if (BunchMass > 0)
                    return BunchN / BunchMass * 100.0;
                else
                    return 0;
            }

        }

        [Description("Root mass on a dry mass basis")]
        [Units("g/m^2")]
        public double RootMass
        {
            get
            {
                double RootMass = 0.0;

                foreach (RootType R in Roots)
                    RootMass += R.Mass;
                return RootMass;
            }

        }

        [Description("Root nitrogen content")]
        [Units("g/m^2")]
        public double RootN
        {
            get
            {
                double RootN = 0.0;

                foreach (RootType R in Roots)
                    RootN += R.N;
                return RootN;
            }

        }

        [Description("Root nitrogen concentration on a dry mass basis")]
        [Units("%")]
        public double RootNConc
        {
            get
            {
                return RootN / RootMass * 100.0;
            }

        }

        [Description("Total palm nitrogen content")]
        [Units("g/m^2")]
        public double PlantN
        {
            get
            {
                return FrondN + RootN + StemN + BunchN;
            }
        }

        [Description("Total number of fronds on a palm")]
        [Units("/palm")]
        public double TotalFrondNumber
        {
            get
            {
                return Fronds.Count;
            }
        }

        [Description("Number of expanded fronds on a palm")]
        [Units("/palm")]
        public double FrondNumber
        {
            get
            {
                return Math.Max(Fronds.Count - ExpandingFronds.Value, 0.0);
            }
        }

        [Description("Green canopy cover provided by the palms")]
        [Units("0-1")]
        public double cover_green
        {
            get
            {
                double DF = DiffuseLightFraction;
                double DirectCover = 1.0 - Math.Exp(-DirectExtinctionCoeff.Value * LAI);
                double DiffuseCover = 1.0 - Math.Exp(-DiffuseExtinctionCoeff.Value * LAI);
                return DF * DiffuseCover + (1 - DF) * DirectCover;
            }
        }

        [Description("Frond specific leaf area")]
        [Units("cm^2/g")]
        public double SLA
        {
            get { return LAI * 10000.0 / FrondMass; }
        }

        [Description("Female flower fraction of the oldest cohort of bunches")]
        [Units("0-1")]
        public double FFF
        {
            get { return Bunches[0].FemaleFraction; }
        }

        protected double SizeFunction(double Age)
        {
            double FMA = FrondMaxArea.Value;
            double GrowthDuration = ExpandingFronds.Value * FrondAppearanceRate.Value;
            double alpha = -Math.Log((1 / 0.99 - 1) / (FMA / (FMA * 0.01) - 1)) / GrowthDuration;
            double leafsize = FMA / (1 + (FMA / (FMA * 0.01) - 1) * Math.Exp(-alpha * Age));
            return leafsize;

        }
        private double RootProportion(int layer, double root_depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += Soil.SoilWater.dlayer[i];
            depth_to_layer_top = depth_to_layer_bottom - Soil.SoilWater.dlayer[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / Soil.SoilWater.dlayer[layer];
        }
        private int LayerIndex(double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < Soil.SoilWater.dlayer.Length; i++)
            {
                CumDepth = CumDepth + Soil.SoilWater.dlayer[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }
        private double DeltaT
        {
            get
            {
                //return Math.Min(Math.Pow(Fn,0.5),1.0);
                //return Math.Min(1.4 * Fn, RelativeDevelopmentalRate.Value);
                //return Math.Min(1.0 * Fn, RelativeDevelopmentalRate.Value);
                return Math.Min(1.25 * Fn, 1.0) * RelativeDevelopmentalRate.Value;
            }
        }


        private void DoUnderstory()
        {
            DoUnderstoryWaterBalance();
            DoUnderstoryGrowth();
            DoUnderstoryNBalance();

            // Now add today's growth to the soil - ie assume plants are in steady state.
            BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
            BiomassRemovedData.crop_type = "OilPalmUnderstory";
            BiomassRemovedData.dm_type = new string[1] { "litter" };
            BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(UnderstoryDltDM * 10) };
            BiomassRemovedData.dlt_dm_n = new float[1] { (float)(UnderstoryNFixation + Utility.Math.Sum(UnderstoryNUptake)) };
            BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
            BiomassRemovedData.fraction_to_residue = new float[1] { 1 };
            BiomassRemoved.Invoke(BiomassRemovedData);

        }
        private void DoUnderstoryGrowth()
        {
            double RUE = 1.3;
            UnderstoryDltDM = RUE * MetData.Radn * UnderstoryCoverGreen * (1 - cover_green) * UnderstoryFW;
        }

        private void DoUnderstoryWaterBalance()
        {

            UnderstoryCoverGreen = UnderstoryCoverMax * (1 - cover_green);
            UnderstoryPEP = Soil.SoilWater.eo * UnderstoryCoverGreen * (1 - cover_green);

            for (int j = 0; j < Soil.Thickness.Length; j++)
                UnderstoryPotSWUptake[j] = Math.Max(0.0, RootProportion(j, UnderstoryRootDepth) * UnderstoryKLmax * UnderstoryCoverGreen * (Soil.SoilWater.sw_dep[j] - Soil.SoilWater.ll15_dep[j]));

            double TotUnderstoryPotSWUptake = Utility.Math.Sum(UnderstoryPotSWUptake);

            UnderstoryEP = 0.0;
            double[] sw_dep = Soil.SoilWater.sw_dep;
            for (int j = 0; j < Soil.Thickness.Length; j++)
            {
                UnderstorySWUptake[j] = UnderstoryPotSWUptake[j] * Math.Min(1.0, UnderstoryPEP / TotUnderstoryPotSWUptake);
                UnderstoryEP += UnderstorySWUptake[j];
                sw_dep[j] = sw_dep[j] - UnderstorySWUptake[j];

            }
            Soil.SoilWater.sw_dep = sw_dep;

            if (UnderstoryPEP > 0.0)
                UnderstoryFW = UnderstoryEP / UnderstoryPEP;
            else
                UnderstoryFW = 1.0;

        }
        private void DoUnderstoryNBalance()
        {
            double LegumeNdemand = UnderstoryDltDM * UnderstoryLegumeFraction * 10 * 0.021;
            double NonLegumeNdemand = UnderstoryDltDM * (1 - UnderstoryLegumeFraction) * 10 * 0.005;
            double UnderstoryNdemand = LegumeNdemand + NonLegumeNdemand;
            UnderstoryNFixation = Math.Max(0.0, LegumeNdemand * .44);

            for (int j = 0; j < Soil.Thickness.Length; j++)
            {
                UnderstoryPotNUptake[j] = Math.Max(0.0, RootProportion(j, UnderstoryRootDepth) * Soil.SoilNitrogen.no3[j]);
            }

            double TotUnderstoryPotNUptake = Utility.Math.Sum(UnderstoryPotNUptake);
            double Fr = Math.Min(1.0, (UnderstoryNdemand - UnderstoryNFixation) / TotUnderstoryPotNUptake);

            double[] no3 = Soil.SoilNitrogen.no3;
            for (int j = 0; j < Soil.Thickness.Length; j++)
            {
                UnderstoryNUptake[j] = UnderstoryPotNUptake[j] * Fr;
                no3[j] = no3[j] - UnderstoryNUptake[j];
            }
            Soil.SoilNitrogen.no3 = no3;

            //UnderstoryNFixation += UnderstoryNdemand - Utility.Math.Sum(UnderstoryNUptake);

            //NFixation = Math.Max(0.0, Ndemand - Utility.Math.Sum(NUptake));

        }

        [XmlIgnore]
        public double DefoliationFraction
        {
            get
            {
                return 0;
            }
            set
            {
                FrondType Loss = new FrondType();
                foreach (FrondType F in Fronds)
                {
                    Loss.Mass += F.Mass * value;
                    Loss.N += F.N * value;

                    F.Mass = F.Mass * (1.0 - value);
                    F.N = F.N * (1.0 - value);
                    F.Area = F.Area * (1.0 - value);
                }


                // Now publish today's losses
                BiomassRemovedType BiomassRemovedData = new BiomassRemovedType();
                BiomassRemovedData.crop_type = "OilPalm";
                BiomassRemovedData.dm_type = new string[1] { "fronds" };
                BiomassRemovedData.dlt_crop_dm = new float[1] { (float)(Loss.Mass * SowingData.Population * 10.0) };
                BiomassRemovedData.dlt_dm_n = new float[1] { (float)(Loss.N * SowingData.Population * 10.0) };
                BiomassRemovedData.dlt_dm_p = new float[1] { 0 };
                BiomassRemovedData.fraction_to_residue = new float[1] { 0 };
                if (BiomassRemoved != null)
                    BiomassRemoved.Invoke(BiomassRemovedData);

            }
        }


        public double DiffuseLightFraction       // This was originally in the RUEModel class inside "Potential" function (PFR)
        {
            get
            {

                double Q = Q0(MetData.Latitude, Clock.Today.DayOfYear);
                double T = MetData.Radn / Q;
                double X1 = (0.80 - 0.0017 * MetData.Latitude + 0.000044 * MetData.Latitude * MetData.Latitude);
                double A1 = ((0.05 - 0.96) / (X1 - 0.26));
                double A0 = (0.05 - A1 * X1);

                return Math.Min(Math.Max(0.0, A0 + A1 * T), 1.0);  //Taken from Roderick paper Ag For Met(?)

            }
        }

        private double Q0(double lat, int day) 						// (PFR)
        {
            double DEC = (23.45 * Math.Sin(2.0 * 3.14159265 / 365.25 * (day - 79.25)));
            double DECr = (DEC * 2.0 * 3.14159265 / 360.0);
            double LATr = (lat * 2.0 * 3.14159265 / 360.0);
            double HS = Math.Acos(-Math.Tan(LATr) * Math.Tan(DECr));

            return 86400.0 * 1360.0 * (HS * Math.Sin(LATr) * Math.Sin(DECr) + Math.Cos(LATr) * Math.Cos(DECr) * Math.Sin(HS)) / 3.14159265 / 1000000.0;
        }
    }
}