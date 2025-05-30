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
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SprumPastureInstance: Model
    {
        /// <summary>Date the pasture is established.  if blank starts of first day of simulation</summary>
        [Separator("Pasture Age")]
        [Description("Establish Date (d-mmm or dd/mm/yyyy)")]
        public string EstablishDate { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Age At Start of Simulation (years)")]
        public double AgeAtSimulationStart 
        {
            get { return _AgeAtSimulationStart; }
            set { _AgeAtSimulationStart = constrain(value, 0, 100); }
        }
        private double _AgeAtSimulationStart { get; set; }

        /// <summary>Years from planting to reach Maximum dimension (years)</summary>
        [Description("Years from planting to reach Maximum root depth (years)")]
        public double YearsToMaxDimension 
        {
            get { return _YearsToMaxDimension; }
            set { _YearsToMaxDimension = constrain(value, 0, 100); }
        }
        private double _YearsToMaxDimension { get; set; }

        /// <summary>Maximum growth rate of pasture (g/MJ)</summary>
        [Separator("Pasture growth")]
        [Description("Radiation use efficiency (g/MJ)")]
        public double RUE { get; set; }
        private double _RUE
        {
            get { return _RUE; }
            set { _RUE = constrain(value, 0.1, 3.0); }
        }

        /// <summary>Residue Biomass proportion (0-0.5)</summary>
        [Description("Residue Biomass proportion (0-0.5)")]
        public double Presidue
        {
            get { return _Presidue; }
            set { _Presidue = constrain(value, 0, 0.5); }
        }
        private double _Presidue { get; set; }

        /// <summary>Residue senescenc rate (0-1)</summary>
        [Description("Residue senescence rate (0-1)")]
        public double RsenRate
        {
            get { return _RsenRate; }
            set { _RsenRate = constrain(value, 0, 1); }
        }
        private double _RsenRate { get; set; }

        /// <summary>Root Biomass proportion (0-1)</summary>
        [Description("Root Biomass proportion (0-1)")]
        public double Proot
        {
            get { return _Proot; }
            set { _Proot = constrain(value, 0, 1); }
        }
        private double _Proot { get; set; }

        /// <summary>Base temperature for crop</summary>
        [Separator("Biomass production temperature Responses")]
        [Description("Base temperature for photosynthesis (oC)")]
        public double PSBaseT 
        { 
            get{return _PSBaseT; }
            set{_PSBaseT = constrain(value,-10,10); } 
        }
        private double _PSBaseT { get; set; }

        /// <summary>Optimum temperature for crop</summary>
        [Description("Lower optimum temperature for photosynthesis (oC)")]
        public double PSLOptT
        {
            get { return _PSLOptT; }
            set { _PSLOptT = constrain(value, 10, 40); }
        }
        private double _PSLOptT { get; set; }

        /// <summary>Optimum temperature for crop</summary>
        [Description("Upper optimum temperature for photosynthesis (oC)")]
        public double PSUOptT 
        {
            get { return _PSUOptT; } 
            set{ _PSUOptT = constrain(value, 10,40); } }
        private double _PSUOptT { get; set; }

        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for photosynthesis (oC)")]
        public double PSMaxT 
        { 
            get{return _PSMaxT; } 
            set{_PSMaxT = constrain(value,20,50); } 
        }
        private double _PSMaxT { get; set; }

        /// <summary>Grow roots into neighbouring zone (yes or no)</summary>
        [Description("Grow roots into neighbouring zone (yes or no)")]
        [Separator("Plant Dimnesions")]
        public bool GRINZ { get; set; }

        /// <summary>Root depth (mm)</summary>
        [Description("Root depth (mm)")]
        public double MaxRD
        {
            get { return _MaxRD; }
            set { _MaxRD = constrain(value, 200, 5000); }
        }
        private double _MaxRD { get; set; }

        /// <summary>Pasture height at grazing (mm)</summary>
        [Description("Pasture height at grazing (mm)")]
        public double MaxHeight
        {
            get { return _MaxHeight; }
            set { _MaxHeight = constrain(value, 100, 3000); }
        }
        private double _MaxHeight { get; set; }

        /// <summary>Pasture height after grazing (mm)</summary>
        [Description("Pasture height after grazing(mm)")]
        public double MaxPrunedHeight
        {
            get{return _MaxPrunedHeight; }
            set{_MaxPrunedHeight = constrain(value,0,2000); }
        }
        private double _MaxPrunedHeight { get; set; }

        /// <summary>Maximum green cover</summary>
        [Separator("Canopy parameters")]
        [Description("Maximum green cover (0-0.97)")]
        public double MaxCover 
        { 
            get{return _MaxCover; } 
            set{_MaxCover = constrain(value, 0.01,0.97); } 
        }
        private double _MaxCover { get; set; }

        /// <summary>Min green cover</summary>
        [Description("Green cover post defoliation (0-MaxCover)")]
        public double MinCover 
        { 
            get{return _MinCover; } 
            set{_MinCover = constrain(value, 0.01,0.97); }
        }
        private double _MinCover { get; set; }

        /// <summary>Maximum green cover</summary>
        [Description("Extinction coefficient (0-1)")]
        public double ExtinctCoeff 
        { 
            get{return _ExtinctCoeff; } 
            set{_ExtinctCoeff = constrain(value,0.2,1.0); }
        }
        private double _ExtinctCoeff { get; set; }

        /// <summary>tt duration of regrowth period</summary>
        [Description("Regrowth duration  (oCd)")]
        public double RegrowthDuration 
        { 
            get{return _RegrowthDuration; }
            set{_RegrowthDuration = constrain(value,50,1000000); } 
        }
        private double _RegrowthDuration { get; set; }

        /// <summary>tt duration of regrowth period</summary>
        [Description("Full Canopy duration  (oCd)")]
        public double FullCanopyDuration 
        { 
            get{return _FullCanopyDuration; } 
            set{_FullCanopyDuration = constrain(value,0,100000000000); } 
        }
        private double _FullCanopyDuration { get; set; }

        /// <summary>Base temperature for crop</summary>
        [Separator("Canopy expansion Temperature Responses")]
        [Description("Base temperature for Canopy expansion (oC)")]
        public double BaseT 
        { 
            get{return _BaseT; } 
            set{_BaseT = constrain(value,-10,10); } 
        }
        private double _BaseT { get; set; }

        /// <summary>Optimum temperature for crop</summary>
        [Description("Optimum temperature for Canopy expansion (oC)")]
        public double OptT 
        { 
            get{return _OptT; } 
            set{_OptT = constrain(value,10,40); } 
        }
        private double _OptT { get; set; }

        /// <summary>Maximum temperature for crop</summary>
        [Description("Maximum temperature for Canopy expansion (oC)")]
        public double MaxT 
        { 
            get{return _MaxT; } 
            set{_MaxT = constrain(value,20,50); } 
        }
        private double _MaxT { get; set; }

        /// <summary>Root Nitrogen Concentration</summary>
        [Separator("Pasture Nitrogen contents")]
        [Description("Root Nitrogen concentration (g/g)")]
        public double RootNConc 
        { 
            get{return _RootNConc; }
            set { _RootNConc = constrain(value, 0.001, 0.1); } 
        }
        private double _RootNConc { get; set; }

        /// <summary>Stover Nitrogen Concentration</summary>
        [Description("Leaf Nitrogen concentration (g/g)")]
        public double LeafNConc 
        { 
            get{return _LeafNConc; }
            set { _LeafNConc = constrain(value, 0.001, 0.1); } 
        }
        private double _LeafNConc { get; set; }

        /// <summary>Product Nitrogen Concentration</summary>
        [Description("Residue Nitrogen concentration(g/g)")]
        public double ResidueNConc 
        { 
            get{return _ResidueNConc; } 
            set{_ResidueNConc = constrain(value,0.001,0.1); } 
        }
        private double _ResidueNConc { get; set; }

        /// <summary>Proportion of pasture mass that is leguem (0-1)</summary>
        [Description("Proportion of pasture mass that is leguem (0-1)")]
        public double LegumePropn 
        { 
            get{return _LegumePropn ; } 
            set{_LegumePropn = constrain(value,0,1); } 
        }
        private double _LegumePropn { get; set; }

        /// <summary>Maximum canopy conductance (typically varies between 0.001 and 0.016 m/s).</summary>
        [Separator(" Parameters defining water demand and response")]
        [Description(" Maximum canopy conductance (between 0.001 and 0.016 m/s):")]
        public double GSMax
        {
            get { return _GSMax; }
            set { _GSMax = constrain(value, 0.001, 0.016); }
        }
        private double _GSMax { get; set; }

        /// <summary>Net radiation at 50% of maximum conductance (typically varies between 50 and 200 W/m2).</summary>
        [Description(" Net radiation at 50% of maximum conductance (between 50 and 200 W/m^2):")]
        public double R50
        {
            get { return _R50; }
            set { _R50 = constrain(value, 50, 200); }
        }
        private double _R50 { get; set; }

        /// <summary>"Does the crop respond to water stress?"</summary>
        [Description("Does the crop respond to water stress?")]
        public bool WaterStress { get; set; }

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
        private Root root = null;

        [Link(Type = LinkType.Ancestor)]
        private Zone zone = null;

        [Link(Type = LinkType.Ancestor)]
        private Simulation simulation = null;

        /// <summary>The cultivar object representing the current instance of the SPRUM pasture/// </summary>
        private Cultivar pasture = null;

        private DateTime establishDate;

        [JsonIgnore]
        private Dictionary<string, string> blankParams = new Dictionary<string, string>()
        {
            {"YearsToMaturity","[SPRUM].RelativeAnnualDimension.XYPairs.X[2] = " },
            {"YearsToMaxRD","[SPRUM].Root.RootFrontVelocity.RootGrowthDuration.YearsToMaxDepth.FixedValue = " },
            {"RUE","[SPRUM].Leaf.Photosynthesis.RUE.FixedValue = "},
            {"PSBaseT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[1] = "},
            {"PSLOptT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[2] = "},
            {"PSUOptT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[3] = "},
            {"PSMaxT","[SPRUM].Leaf.Photosynthesis.FT.XYPairs.X[4] = "},
            {"Presidue","[SPRUM].Residue.DMDemands.Structural.DMDemandFunction.PartitionFraction.FixedValue = " },
            {"RsenRate","[SPRUM].Residue.SenescenceRate.FixedValue = " },
            {"Proot","[SPRUM].Root.DMDemands.Structural.DMDemandFunction.PartitionFraction.FixedValue = " },
            {"Pleaf","[SPRUM].Leaf.DMDemands.Structural.DMDemandFunction.PartitionFraction.FixedValue = " },
            {"HightOfRegrowth","[SPRUM].Height.SeasonalPattern.HightOfRegrowth.MaxHeightFromRegrowth.FixedValue = "},
            {"MaxPrunedHeight","[SPRUM].Height.SeasonalPattern.PostGrazeHeight.FixedValue ="},
            {"MaxRootDepth","[SPRUM].Root.MaximumRootDepth.FixedValue = "},
            {"MaxCover","[SPRUM].Leaf.Cover.Regrowth.Expansion.Delta.Integral.SeasonalPattern.Ymax.FixedValue = "},
            {"MinCover","[SPRUM].Leaf.Cover.Residual.FixedValue = " },
            {"XoCover","[SPRUM].Leaf.Cover.Regrowth.Expansion.Delta.Integral.SeasonalPattern.Xo.FixedValue = "},
            {"bCover","[SPRUM].Leaf.Cover.Regrowth.Expansion.Delta.Integral.SeasonalPattern.b.FixedValue = "},
            {"ExtinctCoeff","[SPRUM].Leaf.ExtinctionCoefficient.FixedValue = "},
            {"ResidueNConc","[SPRUM].Residue.MaximumNConc.FixedValue = "},
            {"ProductNConc","[SPRUM].Leaf.MaximumNConc.FixedValue = "},
            {"RootNConc","[SPRUM].Root.MaximumNConc.FixedValue = "},
            {"GSMax","[SPRUM].Leaf.Gsmax350 = " },
            {"R50","[SPRUM].Leaf.R50 = " },
            {"LegumePropn","[SPRUM].LegumePropn.FixedValue = "},
            {"RegrowDurat","[SPRUM].Phenology.Regrowth.Target.FixedValue =" },
            {"FullCanDurat","[SPRUM].Phenology.FullCanopy.Target.FixedValue =" },
            {"BaseT","[SPRUM].Phenology.ThermalTime.XYPairs.X[1] = "},
            {"OptT","[SPRUM].Phenology.ThermalTime.XYPairs.X[2] = " },
            {"MaxT","[SPRUM].Phenology.ThermalTime.XYPairs.X[3] = " },
            {"MaxTt","[SPRUM].Phenology.ThermalTime.XYPairs.Y[2] = "},
            {"WaterStressPhoto","[SPRUM].Leaf.Photosynthesis.FW.XYPairs.Y[1] = "},
            {"WaterStressCover","[SPRUM].Leaf.Cover.Regrowth.Expansion.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[SPRUM].Root.NUptakeSWFactor.XYPairs.Y[1] = "},
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

            pastureParams["XoCover"] += ((this.RegrowthDuration *.3) * (0.5-this.MinCover) * 2.78 ).ToString();
            pastureParams["bCover"] += (this.RegrowthDuration/6).ToString();
            pastureParams["RegrowDurat"] += this.RegrowthDuration.ToString();
            pastureParams["FullCanDurat"] += this.FullCanopyDuration.ToString();
            pastureParams["YearsToMaturity"] += ((float)this.YearsToMaxDimension/4.0).ToString();
            pastureParams["YearsToMaxRD"] += this.YearsToMaxDimension.ToString();
            pastureParams["RUE"] += RUE.ToString();
            pastureParams["PSBaseT"] += this.PSBaseT.ToString();
            pastureParams["PSLOptT"] += this.PSLOptT.ToString();
            pastureParams["PSUOptT"] += this.PSUOptT.ToString();
            pastureParams["PSMaxT"] += this.PSMaxT.ToString();
            pastureParams["Proot"] += this.Proot.ToString();
            pastureParams["Presidue"] += this.Presidue.ToString();
            pastureParams["RsenRate"] += this.RsenRate.ToString();
            pastureParams["Pleaf"] += (Math.Max(0, 1 - this.Presidue - this.Proot)).ToString();
            pastureParams["HightOfRegrowth"] += (this.MaxHeight- this.MaxPrunedHeight).ToString();
            pastureParams["MaxPrunedHeight"] += this.MaxPrunedHeight.ToString();
            pastureParams["MaxRootDepth"] += this.MaxRD.ToString();
            pastureParams["MaxCover"] += this.MaxCover.ToString();
            pastureParams["MinCover"] += this.MinCover.ToString();
            pastureParams["ExtinctCoeff"] += this.ExtinctCoeff.ToString();
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
                ErrorMessage = value.ToString() + " is lower than minimum allowed so has been constrained to the minimum (" + min.ToString() + ")";
            else if (value > max)
                ErrorMessage = value.ToString() + " is higher than maximum allowed so has been constrained to the maximum (" + min.ToString() + ")";
            else
                ErrorMessage = string.Empty;

            return MathUtilities.Bound(value, min, max);
        }

        /// <summary>
        /// Provides an error message to display if something is wrong.
        /// Used by the UserInterface to give a warning of what is wrong
        /// 
        /// When the user selects a file using the browse button in the UserInterface 
        /// and the file can not be displayed for some reason in the UserInterface.
        /// </summary>
        [JsonIgnore]
        public string ErrorMessage = string.Empty;
    }
}
