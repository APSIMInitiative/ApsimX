using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
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
    [ValidParent(ParentType = typeof(Simulations))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SprumPastureInstance: Model
    {

        /// <summary>The clock</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Clock clock = null;

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
        private RootNetwork root = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SPRUM pasture/// </summary>
        private Cultivar pasture = null;

        private DateTime establishDate;

        private double _ageAtSimulationStart = 3;
        private double _yearsToMaxDimension = 1;
        private double _rUE = 1;
        private double _presidue = 0.1;
        private double _rsenRate = 0.01;
        private double _proot = 0.1;
        private double _pSBaseT = 3;
        private double _pSLOptT = 20;
        private double _pSUOptT = 25;
        private double _pSMaxT = 35;
        private double _maxRD = 1200;
        private double _surfaceKL = 0.1;
        private double _maxPrunedHeight = 100;
        private double _maxHeight = 400;
        private double _maxCover = 0.95;
        private double _minCover = 0.5;
        private double _extinctCoeff = 0.6;
        private double _regrowthDuration = 300;
        private double _fullCanopyDuration = 0;
        private double _baseT = 3;
        private double _optT = 25;
        private double _maxT = 35;
        private double _rootNConc = 0.01;
        private double _leafNConc = 0.03;
        private double _residueNConc = 0.01;
        private double _legumePropn = 0;
        private double _gSMax = 0.005;
        private double _r50 = 100;

        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"YearsToMaturity","[SPRUM].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"YearsToMaxRD","[SPRUM].Root.Network.RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"RUE","[SPRUM].Leaf.Photosynthesis.RUE.FixedValue = "},
            {"PSBaseT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[1] = "},
            {"PSLOptT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[2] = "},
            {"PSUOptT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[3] = "},
            {"PSMaxT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[4] = "},
            {"FRGRBaseT", "[SPRUM].Leaf.Canopy.FRGRer.FRGRFunctionTemp.Response.X[1] = " },
            {"FRGRLOptT", "[SPRUM].Leaf.Canopy.FRGRer.FRGRFunctionTemp.Response.X[2] = " },
            {"FRGRUOptT", "[SPRUM].Leaf.Canopy.FRGRer.FRGRFunctionTemp.Response.X[3] = " },
            {"FRGRMaxT", "[SPRUM].Leaf.Canopy.FRGRer.FRGRFunctionTemp.Response.X[4] = " },
            {"Presidue","[SPRUM].Residue.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"RsenRate","[SPRUM].Residue.SenescenceRate.FixedValue = " },
            {"Proot","[SPRUM].Root.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"Pleaf","[SPRUM].Leaf.TotalCarbonDemand.TotalDMDemand.PartitionFraction.FixedValue = " },
            {"HightOfRegrowth","[SPRUM].Height.SeasonalPattern.HightOfRegrowth.MaxHeightFromRegrowth.FixedValue = "},
            {"MaxPrunedHeight","[SPRUM].Height.SeasonalPattern.PostGrazeHeight.FixedValue ="},
            {"MaxRootDepth","[SPRUM].Root.Network.MaximumRootDepth.FixedValue = "},
            {"SurfaceKL","[SPRUM].Root.Network.KLModifier.SurfaceKL.FixedValue = " },
            {"MaxCover","[SPRUM].Leaf.Canopy.GreenCover.Regrowth.Expansion.Delta.Integral.SeasonalPattern.Ymax.FixedValue = "},
            {"MinCover","[SPRUM].Leaf.Canopy.GreenCover.Residual.FixedValue = " },
            {"XoCover","[SPRUM].Leaf.Canopy.GreenCover.Regrowth.Expansion.Delta.Integral.SeasonalPattern.Xo.FixedValue = "},
            {"bCover","[SPRUM].Leaf.Canopy.GreenCover.Regrowth.Expansion.Delta.Integral.SeasonalPattern.b.FixedValue = "},
            {"ExtinctCoeff","[SPRUM].Leaf.Canopy.GreenExtinctionCoefficient.FixedValue = "},
            {"ResidueNConc","[SPRUM].Residue.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"ProductNConc","[SPRUM].Leaf.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"RootNConc","[SPRUM].Root.Nitrogen.ConcFunctions.Maximum.FixedValue = "},
            {"GSMax","[SPRUM].Leaf.Canopy.Gsmax350 = " },
            {"R50","[SPRUM].Leaf.Canopy.R50 = " },
            {"LegumePropn","[SPRUM].LegumePropn.FixedValue = "},
            {"RegrowDurat","[SPRUM].Phenology.Regrowth.Target.FixedValue =" },
            {"FullCanDurat","[SPRUM].Phenology.FullCanopy.Target.FixedValue =" },
            {"BaseT","[SPRUM].Phenology.ThermalTime.XYPairs.X[1] = "},
            {"OptT","[SPRUM].Phenology.ThermalTime.XYPairs.X[2] = " },
            {"MaxT","[SPRUM].Phenology.ThermalTime.XYPairs.X[3] = " },
            {"MaxTt","[SPRUM].Phenology.ThermalTime.XYPairs.Y[2] = "},
            {"WaterStressPhoto","[SPRUM].Leaf.Photosynthesis.FW.XYPairs.Y[1] = "},
            {"WaterStressCover","[SPRUM].Leaf.Canopy.GreenCover.Regrowth.Expansion.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[SPRUM].Root.Network.NUptakeSWFactor.XYPairs.Y[1] = "},
        };

        /// <summary>Date the pasture is established.  if blank starts of first day of simulation</summary>
        [Separator("Pasture Age")]
        [Description("Establish Date (d-mmm or dd/mm/yyyy)")]
        public string EstablishDate { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Age At Start of Simulation (0-100 years)")]
        [Bounds(Lower =0,Upper =100)]
        public double AgeAtSimulationStart 
        {
            get { return _ageAtSimulationStart; }
            set { _ageAtSimulationStart = constrain(value, 0, 100); }
        }

        /// <summary>Years from establishment to reach Maximum dimension (years)</summary>
        [Description("Years from establishment to reach Maximum root depth (0-100 years)")]
        [Bounds(Lower = 0, Upper = 100)]
        public double YearsToMaxDimension 
        {
            get { return _yearsToMaxDimension; }
            set { _yearsToMaxDimension = constrain(value, 0, 100); }
        }

        /// <summary>Maximum growth rate of pasture (g/MJ)</summary>
        [Separator("Pasture growth")]
        [Description("Radiation use efficiency (0.1 - 3.0 g/MJ)")]
        [Bounds(Lower = 0.1, Upper = 3.0)]
        public double RUE
        {
            get { return _rUE; }
            set { _rUE = constrain(value, 0.1, 3.0); }
        }
        
        /// <summary>Residue Biomass proportion (0-0.5)</summary>
        [Description("Residue Biomass proportion (0-0.5)")]
        [Bounds(Lower = 0, Upper = 0.5)]
        public double Presidue
        {
            get { return _presidue; }
            set { _presidue = constrain(value, 0, 0.5); }
        }

        /// <summary>Residue senescenc rate (0-1)</summary>
        [Description("Residue senescence rate (0-1)")]
        [Bounds(Lower = 0, Upper = 1.0)]
        public double RsenRate
        {
            get { return _rsenRate; }
            set { _rsenRate = constrain(value, 0, 1); }
        }

        /// <summary>Root Biomass proportion (0-1)</summary>
        [Description("Root Biomass proportion (0-0.5)")]
        [Bounds(Lower = 0, Upper = 0.5)]
        public double Proot
        {
            get { return _proot; }
            set { _proot = constrain(value, 0, 0.5); }
        }

        /// <summary>Base temperature for crop</summary>
        [Separator("Biomass production temperature Responses")]
        [Description("Base temperature for photosynthesis (-10-10 oC)")]
        [Bounds(Lower = -10, Upper = 10)]
        public double PSBaseT 
        { 
            get{return _pSBaseT; }
            set{_pSBaseT = constrain(value,-10,10); } 
        }
        
        /// <summary>Optimum temperature for crop</summary>
        [Description("Lower optimum temperature for photosynthesis (10-40 oC)")]
        [Bounds(Lower = 10, Upper = 40)]
        public double PSLOptT
        {
            get { return _pSLOptT; }
            set { _pSLOptT = constrain(value, 10, 40); }
        }

        /// <summary>Optimum temperature for crop</summary>
        [Description("Upper optimum temperature for photosynthesis (10-40 oC)")]
        [Bounds(Lower = 10, Upper = 50)]
        public double PSUOptT
        {
            get { return _pSUOptT; }
            set { _pSUOptT = constrain(value, 10, 50); }
        }
        
        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for photosynthesis (20-50 oC)")]
        [Bounds(Lower = 20, Upper = 50)]
        public double PSMaxT 
        { 
            get{return _pSMaxT; } 
            set{_pSMaxT = constrain(value, 20, 50); } 
        }

        /// <summary>Grow roots into neighbouring zone (yes or no)</summary>
        [Description("Grow roots into neighbouring zone (yes or no)")]
        [Separator("Plant Dimnesions")]
        public bool GRINZ { get; set; }

        /// <summary>Root depth (mm)</summary>
        [Description("Root depth (200-5000mm)")]
        [Bounds(Lower = 200, Upper = 5000)]
        public double MaxRD
        {
            get { return _maxRD; }
            set { _maxRD = constrain(value, 200, 5000); }
        }

        /// <summary>Pasture height at grazing (mm)</summary>
        [Description("Pasture height at grazing (100-3000 mm)")]
        [Bounds(Lower = 0, Upper = 1.0)]
        public double MaxHeight
        {
            get { return _maxHeight; }
            set { _maxHeight = constrain(value, 100, 3000); }
        }

        /// <summary>Pasture height after grazing (mm)</summary>
        [Description("Pasture height after grazing(0-2000 mm)")]
        [Bounds(Lower = 0, Upper = 2000)]
        public double MaxPrunedHeight
        {
            get{return _maxPrunedHeight; }
            set{_maxPrunedHeight = constrain(value,0,2000); }
        }

        /// <summary>Maximum green cover</summary>
        [Separator("Canopy parameters")]
        [Description("Maximum green cover (0-0.98)")]
        [Bounds(Lower = 0, Upper = 0.98)]
        public double MaxCover 
        { 
            get{return _maxCover; } 
            set{_maxCover = constrain(value, 0.01,0.98); } 
        }
        
        /// <summary>Min green cover</summary>
        [Description("Green cover post defoliation (0-MaxCover)")]
        [Bounds(Lower = 0.01, Upper = 0.97)]
        public double MinCover 
        { 
            get{return _minCover; } 
            set{_minCover = constrain(value, 0.01,0.97); }
        }

        /// <summary>Maximum green cover</summary>
        [Description("Extinction coefficient (0.2-1)")]
        [Bounds(Lower = 0.2, Upper = 1.0)]
        public double ExtinctCoeff 
        { 
            get{return _extinctCoeff; } 
            set{_extinctCoeff = constrain(value,0.2,1.0); }
        }

        /// <summary>tt duration of regrowth period</summary>
        [Description("Regrowth duration  (50-10000 oCd)")]
        [Bounds(Lower = 50, Upper = 10000)]
        public double RegrowthDuration 
        { 
            get{return _regrowthDuration; }
            set{_regrowthDuration = constrain(value,50,10000); } 
        }

        /// <summary>tt duration of regrowth period</summary>
        [Description("Full Canopy duration  (0- oCd)")]
        [Bounds(Lower = 0, Upper = 100000000000)]
        public double FullCanopyDuration 
        { 
            get{return _fullCanopyDuration; } 
            set{_fullCanopyDuration = constrain(value,0,100000000000); } 
        }

        /// <summary>Base temperature for crop</summary>
        [Separator("Canopy expansion Temperature Responses")]
        [Description("Base temperature for Canopy expansion (-10 - 10 oC)")]
        [Bounds(Lower = -10, Upper = 10)]
        public double BaseT 
        { 
            get{return _baseT; } 
            set{_baseT = constrain(value,-10,10); } 
        }

        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for Canopy expansion (10-40 oC)")]
        [Bounds(Lower = 10, Upper = 40)]
        public double OptT 
        { 
            get{return _optT; } 
            set{_optT = constrain(value,10,40); } 
        }

        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for Canopy expansion (15-50 oC)")]
        [Bounds(Lower = 15, Upper = 50)]
        public double MaxT 
        { 
            get{return _maxT; } 
            set{_maxT = constrain(value,15,50); } 
        }

        /// <summary>Root Nitrogen Concentration</summary>
        [Separator("Pasture Nitrogen contents")]
        [Description("Root Nitrogen concentration (0.001-0.1 g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        public double RootNConc 
        { 
            get{return _rootNConc; }
            set { _rootNConc = constrain(value, 0.001, 0.1); } 
        }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Leaf Nitrogen concentration (0.001 - 0.1 g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        public double LeafNConc 
        { 
            get{return _leafNConc; }
            set { _leafNConc = constrain(value, 0.001, 0.1); } 
        }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Residue Nitrogen concentration(0.001 - 0.1 g/g)")]
        [Bounds(Lower = 0.001, Upper = 0.1)]
        public double ResidueNConc 
        { 
            get{return _residueNConc; } 
            set{_residueNConc = constrain(value,0.001,0.1); } 
        }

        /// <summary>Proportion of pasture mass that is leguem (0-1)</summary>
        [Description("Proportion of pasture mass that is leguem (0-1)")]
        [Bounds(Lower = 0, Upper = 1)]
        public double LegumePropn 
        { 
            get{return _legumePropn ; } 
            set{_legumePropn = constrain(value,0,1); } 
        }

        /// <summary>Maximum canopy conductance (typically varies between 0.001 and 0.016 m/s).</summary>
        [Separator(" Parameters defining water demand and response")]
        [Description(" Maximum canopy conductance (between 0.001 and 0.016 m/s):")]
        [Bounds(Lower = 0.001, Upper = 0.016)]
        public double GSMax
        {
            get { return _gSMax; }
            set { _gSMax = constrain(value, 0.001, 0.016); }
        }

        /// <summary>Net radiation at 50% of maximum conductance (typically varies between 50 and 200 W/m2).</summary>
        [Description(" Net radiation at 50% of maximum conductance (between 50 and 200 W/m^2):")]
        [Bounds(Lower = 50, Upper = 200)]
        public double R50
        {
            get { return _r50; }
            set { _r50 = constrain(value, 50, 200); }
        }

        /// <summary>KL in top soil layer (0.01 - 0.2)</summary>
        [Description("KL in top soil layer (0.01 - 0.2)")]
        [Bounds(Lower = 0.01, Upper = 0.2)]
        public double SurfaceKL
        {
            get { return _surfaceKL; }
            set { _surfaceKL = constrain(value, 0.01, 0.2); }
        }

        /// <summary>"Does the crop respond to water stress?"</summary>
        [Description("Does the crop respond to water stress?")]
        public bool WaterStress { get; set; }

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

            // SPRUM sets soil KL to 1 and uses the KL modifier to determine appropriate kl based on root depth
            for (int d = 0; d < soilCrop.KL.Length; d++)
                soilCrop.KL[d] = 1.0; 


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

            pasture = CoeffCalc();
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
        public Cultivar CoeffCalc()
        {
            Dictionary<string, string> pastureParams = new Dictionary<string, string>(blankParams);

            if (this.WaterStress)
            {
                pastureParams["WaterStressPhoto"] += "0.0";
                pastureParams["WaterStressCover"] += "0.0";
                pastureParams["WaterStressNUptake"] += "0.0";
            }
            else
            {
                pastureParams["WaterStressPhoto"] += "1.0";
                pastureParams["WaterStressCover"] += "1.0";
                pastureParams["WaterStressNUptake"] += "1.0";
            }

            if (this.MinCover >= this.MaxCover)
            {
                throw new Exception("Maximum Green Cover must be greater that Green Cover post defoliation");
            } 
            
            double b = this.RegrowthDuration / 7;
            double Xo = b * Math.Log(this.MaxCover / this.MinCover - 1);
            pastureParams["XoCover"] += Xo.ToString();
            pastureParams["bCover"] += b.ToString();
            pastureParams["MaxCover"] += this.MaxCover.ToString();
            pastureParams["MinCover"] += this.MinCover.ToString();
            pastureParams["ExtinctCoeff"] += this.ExtinctCoeff.ToString();
            pastureParams["RegrowDurat"] += this.RegrowthDuration.ToString();
            pastureParams["FullCanDurat"] += this.FullCanopyDuration.ToString();
            pastureParams["YearsToMaturity"] += ((float)this.YearsToMaxDimension/4.0).ToString();
            pastureParams["YearsToMaxRD"] += this.YearsToMaxDimension.ToString();
            pastureParams["RUE"] += RUE.ToString();
            pastureParams["PSBaseT"] += this.PSBaseT.ToString();
            pastureParams["PSLOptT"] += this.PSLOptT.ToString();
            pastureParams["PSUOptT"] += this.PSUOptT.ToString();
            pastureParams["PSMaxT"] += this.PSMaxT.ToString();
            pastureParams["FRGRBaseT"] += this.PSBaseT.ToString();
            pastureParams["FRGRLOptT"] += this.PSLOptT.ToString();
            pastureParams["FRGRUOptT"] += this.PSUOptT.ToString();
            pastureParams["FRGRMaxT"] += this.PSMaxT.ToString();
            pastureParams["Proot"] += this.Proot.ToString();
            pastureParams["Presidue"] += this.Presidue.ToString();
            pastureParams["RsenRate"] += this.RsenRate.ToString();
            pastureParams["Pleaf"] += (Math.Max(0, 1 - this.Presidue - this.Proot)).ToString();
            pastureParams["HightOfRegrowth"] += (this.MaxHeight- this.MaxPrunedHeight).ToString();
            pastureParams["MaxPrunedHeight"] += this.MaxPrunedHeight.ToString();
            pastureParams["MaxRootDepth"] += this.MaxRD.ToString();
            pastureParams["ResidueNConc"] += this.ResidueNConc.ToString();
            pastureParams["ProductNConc"] += this.LeafNConc.ToString();
            pastureParams["RootNConc"] += this.RootNConc.ToString();
            pastureParams["GSMax"] += this.GSMax.ToString();
            pastureParams["R50"] += this.R50.ToString();
            pastureParams["LegumePropn"] += this.LegumePropn.ToString();
            pastureParams["BaseT"] += this.BaseT.ToString();
            pastureParams["OptT"] += this.OptT.ToString();
            pastureParams["MaxT"] += this.MaxT.ToString();
            pastureParams["MaxTt"] += (this.OptT - this.BaseT).ToString();
            pastureParams["SurfaceKL"] += this.SurfaceKL.ToString();
            
            string[] commands = new string[pastureParams.Count];
            pastureParams.Values.CopyTo(commands, 0);

            Cultivar PastureValues = new Cultivar(this.Name, commands);
            return PastureValues;
        }
        
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(EstablishDate))
            {
                if (DateTime.Compare(clock.Today, establishDate) == 0)
                {
                    Establish();
                }
            }
        }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartSimulation(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(EstablishDate))
            {
                Establish();
            }
            else
            {
                establishDate = DateUtilities.GetDate(EstablishDate, clock.Today.Year);

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
    }
}
