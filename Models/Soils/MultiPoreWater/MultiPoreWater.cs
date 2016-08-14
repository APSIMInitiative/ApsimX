

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Models.Core;
using Models;
using System.Xml.Serialization;
using Models.PMF;
using System.Runtime.Serialization;
using Models.SurfaceOM;
using Models.Soils;
using Models.Soils.SoilWaterBackend;
using Models.Interfaces;
using APSIM.Shared.Utilities;


namespace Models.Soils
{

    /// <summary>
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class MultiPoreWater : Model, ISoilWater
    {
        #region IsoilInterface
        /// <summary>The amount of rainfall intercepted by surface residues</summary>
        [XmlIgnore]
        public double residueinterception { get { return ResidueWater; } set { } }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double catchment_area { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double CN2Bare { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double CNCov { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double CNRed { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double DiffusConst { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double DiffusSlope { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double discharge_width { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] dlayer { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] dlt_sw { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] dlt_sw_dep { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Drainage { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] DULmm { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Eo { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Eos { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Es { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double ESW { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] flow { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] flow_nh4 { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] flow_no3 { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] flow_urea { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] flux { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double gravity_gradient { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Infiltration { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] KLAT { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double LeachNH4 { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double LeachNO3 { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double LeachUrea { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] LL15mm { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double max_pond { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] outflow_lat { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double pond { get { return SurfaceWater; } set { } }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double pond_evap { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Runoff { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double Salb { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] SATmm { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double slope { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] solute_flow_eff { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] solute_flux_eff { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double specific_bd { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double SummerCona { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public string SummerDate { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double SummerU { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] SW { get; set; }
        ///<summary> Who knows</summary>

        public double[] SWCON { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double[] SWmm { get; set; }
        ///<summary> Who knows</summary>
        public double[] Thickness { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double WaterTable { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double WinterCona { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public string WinterDate { get; set; }
        ///<summary> Who knows</summary>
        [XmlIgnore]
        public double WinterU { get; set; }
        ///<summary> Who knows</summary>
        public void SetSWmm(int Layer, double NewSWmm) { }
        ///<summary> Who knows</summary>
        public void Reset() { }
        ///<summary> Who knows</summary>
        public void SetWaterTable(double InitialDepth) { }
        ///<summary> Who knows</summary>
        public void Tillage(TillageType Data) { }
        ///<summary> Who knows</summary>
        public void Tillage(string DefaultTillageName) { }
        #endregion

        #region Class Dependancy Links
        [Link]
        private Water Water2 = null;
        [Link]
        private Soil Soil = null;
        [Link]
        private SurfaceOrganicMatter SurfaceOM = null;
        [Link]
        private Weather Met = null;
        #endregion

        #region Structures
        /// <summary>
        /// This is the data structure that represents the soils layers and pore cagatories in each layer
        /// </summary>
        public Pore[][] Pores;
        /// <summary>
        /// Contains data extrapolated out to hourly values
        /// </summary>
        public HourlyData Hourly;
        /// <summary>
        /// Contains parameters specific to each layer in the soil
        /// </summary>
        public ProfileParameters ProfileParams = null;
        #endregion

        #region Parameters
        /// <summary>
        /// The maximum diameter of pore compartments
        /// </summary>
        [Units("nm")]
        [Description("The pore diameters the seperate modeled pore compartemnts")]
        public double[] PoreBounds { get; set; }
        /// <summary>
        /// The hydraulic conductance below the bottom of the specified profile
        /// </summary>
        [Units("mm/h")]
        [Description("The amount of water that will pass the bottom of the profile")]
        public double BottomBoundryConductance { get; set; }
        #endregion

        #region Outputs
        /// <summary>
        /// The amount of water in the layer
        /// </summary>
        [XmlIgnore]
        public double[] Water { get; set; }
        /// <summary>
        /// The amount of water stored in the surface residue
        /// </summary>
        public double ResidueWater { get; set; }
        /// <summary>
        /// The amount of water ponded on the surface
        /// </summary>
        public double SurfaceWater { get; set; }
        #endregion

        #region Properties
        /// <summary>
        /// The number of layers in the soil profiel
        /// </summary>
        private int ProfileLayers { get; set; }
        /// <summary>
        /// The number of compartments the soils porosity is divided into
        /// </summary>
        private int PoreCompartments { get; set; }
        /// <summary>
        /// How much of the current air filled volume of a layer may be water filled in the comming hour
        /// </summary>
        [Units("mm")]
        private double[] AdsorptionCapacity { get; set; }
        /// <summary>
        /// How much water can the profile below this layer absorb in the comming hour
        /// </summary>
        [Units("mm")]
        private double[] AdsorptionCapacityBelow { get; set; }
        /// <summary>
        /// The amount of water that may flow into and through the profile below this layer in the comming hour
        /// </summary>
        [Units("mm")]
        private double[] PercolationCapacityBelow { get; set; }
        /// <summary>
        /// The amount of water that may enter the surface of the soil each hour
        /// </summary>
        private double PotentialInfiltration { get; set; }
         
        #endregion

        #region Event Handlers
        /// <summary>
        /// Called when [simulation commencing].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">
        /// SoilWater module has detected that the Soil has no layers.
        /// </exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ProfileLayers = Water2.Thickness.Length;
            PoreCompartments = PoreBounds.Length - 1;
            AdsorptionCapacity = new double[ProfileLayers];
            AdsorptionCapacityBelow = new double[ProfileLayers];
            PercolationCapacityBelow = new double[ProfileLayers];
            SurfaceWater = 0;
            SWmm = new double[ProfileLayers];
            SW = new double[ProfileLayers];
            ProfileParams = new ProfileParameters(ProfileLayers);

            double[] InitialWater = new double[Water2.Thickness.Length];
            for (int L = 0; L < ProfileLayers; L++)
            {

            }

            Pores = new Pore[ProfileLayers][];
            for (int l = 0; l < ProfileLayers; l++)
            {
                ProfileParams.Ksat[l] = Water2.KS[l] / 24; //Convert daily values to hourly
                double AccumVolume = 0;
                double AccumWaterVolume = 0;
                Pores[l] = new Pore[PoreCompartments];
                for (int c = PoreCompartments - 1; c >= 0; c--)
                {
                    Pores[l][c] = new Pore();
                    Pores[l][c].MaxDiameter = PoreBounds[c];
                    Pores[l][c].MinDiameter = PoreBounds[c + 1];
                    Pores[l][c].Thickness = Water2.Thickness[l];
                    double PoreVolume = Water2.SAT[l] * ProportionPoreVolume(PoreBounds[c], PoreBounds[c + 1]);
                    AccumVolume += PoreVolume;
                    Pores[l][c].Volume = PoreVolume;
                    double PoreWaterFilledVolume = Math.Min(PoreVolume, Soil.InitialWaterVolumetric[l] - AccumWaterVolume);
                    AccumWaterVolume += PoreWaterFilledVolume;
                    Pores[l][c].WaterFilledVolume = PoreWaterFilledVolume;
                    Pores[l][c].HydraulicConductivityIn = ProfileParams.Ksat[l] * (PoreCompartments / (c + 1)) / PoreCompartments; //Arbitary function to give different KS values for each pore
                    Pores[l][c].HydraulicConductivityOut = Math.Max(0, Pores[l][c].HydraulicConductivityIn - ProfileParams.Ksat[l] * 0.5);//arbitary function to give a range of hydraulic conductivity over pores
                }
                if (Math.Abs(AccumVolume - Water2.SAT[l]) > FloatingPointTolerance)
                    throw new Exception(this + " Pore volume has not been correctly partitioned between pore compartments in layer " + l);
                if (Math.Abs(AccumWaterVolume - Soil.InitialWaterVolumetric[l]) > FloatingPointTolerance)
                    throw new Exception(this + " Initial water content has not been correctly partitioned between pore compartments in layer" + l);
                SWmm[l] = LayerSum(Pores[l], "WaterDepth");
                SW[l] = LayerSum(Pores[l], "WaterDepth") / Water2.Thickness[l];
            }

            Hourly = new HourlyData();
        }
        /// <summary>
        /// Called at the start of each daily timestep
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            DailyIrrigation = 0;
            IrrigationDuration = 0;
            DailyRainfall = 0;
            Infiltration = 0;
            Array.Clear(Hourly.Irrigation, 0, 24);
            Array.Clear(Hourly.Rainfall, 0, 24);
        }
        /// <summary>
        /// Called when the model is ready to work out daily soil water deltas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {
            //First we work out how much water is reaching the soil surface each hour
            doPrecipitation();
            for (int h = 0; h < 24; h++)
            {
                //Update the depth of Surface water that may infiltrate this hour
                SurfaceWater += Hourly.Rainfall[h] + Hourly.Irrigation[h];
                //The we work out how much of this may percolate into the profile each hour
                doPercolationCapacity();
                //Now we know how much water can infiltrate into the soil, lets put it there if we have some
                //SurfaceWater = Math.Max(0,SurfaceWater - PotentialInfiltration);
                double HourlyInfiltration = Math.Min(SurfaceWater, PotentialInfiltration);
                if (HourlyInfiltration>0)
                    doInfiltration(HourlyInfiltration);
                doDrainage();
                doEvaporation();
                doTranspiration();
                doDownwardDiffusion();
                doUpwardDiffusion();

                for (int l = ProfileLayers - 1; l >= 0; l--)
                {
                    SWmm[l] = LayerSum(Pores[l], "WaterDepth");
                    SW[l] = LayerSum(Pores[l], "WaterDepth") / Water2.Thickness[l];
                }
            }
        }
        /// <summary>
        /// Adds irrigation events into daily total
        /// </summary>
        /// <param name="sender">Irrigation</param>
        /// <param name="IrrigationData">The irrigation data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, Models.Soils.IrrigationApplicationType IrrigationData)
        {
            ResidueWater = ResidueWater + ResidueInterception(IrrigationData.Amount);
            DailyIrrigation += IrrigationData.Amount - ResidueInterception(IrrigationData.Amount);
            //Fix me.  Need to subtract out canopy interception also
            IrrigationDuration += IrrigationData.Duration;
        }
        /// <summary>
        /// sets up daily met data
        /// </summary>
        [EventSubscribe("PreparingNewWeatherData")]
        private void OnPreparingNewWeatherData(object sender, EventArgs e)
        {
            if (Met.Rain > 0)
            {
                ResidueWater = ResidueWater + ResidueInterception(Met.Rain);
                double DailyRainfall = Met.Rain - ResidueInterception(Met.Rain);
                //Fix me.  Need to subtract out canopy interception also
            }
        }
        #endregion

        #region Internal States
        private double FloatingPointTolerance = 0.000000001;
        /// <summary>
        /// This is the Irrigation ariving at the soil surface, less what has been intercepted by residue
        /// </summary>
        private double DailyIrrigation {get;set; }
        private double IrrigationDuration { get; set; }
        /// <summary>
        /// This is the rainfall ariving at the soil surface, less what has been intercepted by residue
        /// </summary>
        private double DailyRainfall { get; set; }
        #endregion

        #region Internal Properties and Methods
        private double ResidueInterception(double Precipitation)
        {
            double ResidueWaterCapacity = 0.0002 * SurfaceOM.Wt; //Fixme coefficient should be obtained from surface OM
            return Math.Min(Precipitation * SurfaceOM.Cover, ResidueWaterCapacity - ResidueWater);
        }
        /// <summary>
        /// Potential gradients moves water out of layers each time step
        /// </summary>
        private double Infiltrate(Pore P)
        {
            double PotentialAdsorbtion = Math.Min(P.HydraulicConductivityIn, P.AirDepth);
            return PotentialAdsorbtion;
        }
        private void doPrecipitation()
        {
            if (DailyIrrigation > 0)
            { //On days when irrigation is applies spread it out into hourly increments
                if (IrrigationDuration > 24)
                    throw new Exception(this + " daily irrigation duration exceeds 24 hours.  There are only 24 hours in each day so it is not really possible to irrigate for longer that this");
                int Irrighours = (int)IrrigationDuration;
                double IrrigationRate = DailyIrrigation / IrrigationDuration;

                for (int h = 0; h < Irrighours; h++)
                {
                    Hourly.Irrigation[h] = IrrigationRate;
                }
                if (Math.Abs(MathUtilities.Sum(Hourly.Irrigation) - DailyIrrigation) > FloatingPointTolerance)
                    throw new Exception(this + " hourly irrigation partition has gone wrong.  Check you are specifying a Duration > 0 in the irrigation method call");
            }
            if (Met.Rain > 0)
            {  //On days when rainfall occurs put it into hourly increments
                int RainHours = (int)Met.RainfallHours;
                double RainRate = Met.Rain / RainHours;
                for (int h = 0; h < RainHours; h++)
                {
                    Hourly.Rainfall[h] = RainRate;
                }
                if (Math.Abs(MathUtilities.Sum(Hourly.Rainfall) - Met.Rain) > FloatingPointTolerance)
                    throw new Exception(this + " hourly rainfall partition has gone wrong");
            }
        }
        /// <summary>
        /// Carries out infiltration processes at each time step
        /// </summary>
        private void doPercolationCapacity()
        {
            for (int l = 0; l < ProfileLayers; l++)
            {//Step through each layer
                double PotentialAbsorption = 0;
                for (int c = PoreCompartments - 1; c >= 0; c--)
                {//Workout how much water may be adsorbed into each pore
                    PotentialAbsorption = Math.Min(Pores[l][c].HydraulicConductivityIn, Pores[l][c].AirDepth);
                }
                AdsorptionCapacity[l] = PotentialAbsorption;
            }
            for (int l = ProfileLayers-1; l >=0; l--)
            {//Then step through each layer and work out how much water the profile below can take
                if (l == ProfileLayers - 1)
                {
                    //In the bottom layer of the profile absorption capaicity below is the amount of water this layer can absorb
                    AdsorptionCapacityBelow[l] = AdsorptionCapacity[l];
                    //In the bottom layer of the profile percolation capacity below is the conductance of the bottom of the profile
                    PercolationCapacityBelow[l] = BottomBoundryConductance;
                }
                else
                {
                    //For subsequent layers up the profile absorpbion capacity below adds the current layer to the sum of the layers below
                    AdsorptionCapacityBelow[l] = AdsorptionCapacityBelow[l + 1] + AdsorptionCapacity[l];
                    //For subsequent layers up the profile the percolation capacity below is the amount that the layer below may absorb
                    //plus the minimum of what may drain through the layer below (ksat of layer below) and what may potentially percolate
                    //Into the rest of the profile below that
                    PercolationCapacityBelow[l] = AdsorptionCapacity[l + 1] + Math.Min(ProfileParams.Ksat[l + 1],PercolationCapacityBelow[l+1]);
                }
            }
            //The amount of water that may percolate below the surface layer plus what ever the surface layer may absorb
            PotentialInfiltration = AdsorptionCapacity[0] + PercolationCapacityBelow[0];
        }
        private void doInfiltration(double WaterToInfiltrate)
        {
            //Do infiltration processes each hour
            double RemainingInfiltration = WaterToInfiltrate;
            for (int l = 0; l < ProfileLayers; l++)
            { //Start process in the top layer
                if (RemainingInfiltration>0)
                for (int c = PoreCompartments - 1; c >= 0; c--)
                {//AdsorbWater onto samllest pores first followed by larger ones
                    if (RemainingInfiltration > 0) //Only do adsorption if there is something to absorb
                    {
                        double PotentialAdsorbtion = Math.Min(Pores[l][c].HydraulicConductivityIn, Pores[l][c].AirDepth);
                        double Adsorbtion = Math.Min(RemainingInfiltration, PotentialAdsorbtion);
                        Pores[l][c].WaterFilledVolume += Adsorbtion/Pores[l][c].Thickness;
                        RemainingInfiltration -= Adsorbtion;
                    }
                }
            }
            //Add infiltration to daily sum for reporting
            Infiltration += WaterToInfiltrate;
            SurfaceWater -= WaterToInfiltrate;
        }
        /// <summary>
        /// Gravity moves mobile water out of layers each time step
        /// </summary>
        private void doDrainage()
        {

        }
        /// <summary>
        /// Potential gradients moves water out of layers each time step
        /// </summary>
        private void doEvaporation()
        {
            //Evaporate water from top layer
        }
        /// <summary>
        /// Potential gradients moves water out of layers each time step
        /// </summary>
        private void doTranspiration()
        {
            //write some temporary stuff to be replaced by arbitrator at some stage
        }
        /// <summary>
        /// Potential gradients moves water out of layers each time step
        /// </summary>
        private void doDownwardDiffusion()
        {
            //Move water down into lower layers if they are dryer than above
        }
        /// <summary>
        /// Potential gradients moves water out of layers each time step
        /// </summary>
        private void doUpwardDiffusion()
        {
            //Move water up into lower layers if they are dryer than below
        }
        /// <summary>
        /// Calculates the proportion of total porosity the resides between the two specified pore diameters
        /// </summary>
        /// <param name="MaxDiameter"></param>
        /// <param name="MinDiameter"></param>
        /// <returns>proportion of totol porosity</returns>
        private double ProportionPoreVolume(double MaxDiameter, double MinDiameter)
        {
            return CumPoreVolume(MaxDiameter) - CumPoreVolume(MinDiameter);
        }
        /// <summary>
        /// Calculates the proportion of total porosity below the specified pore diameter
        /// </summary>
        /// <param name="PoreDiameter"></param>
        /// <returns>proportion of total porosity</returns>
        private double CumPoreVolume(double PoreDiameter)
        {
            double PoreVolume = PoreDiameter * 0.0003;
            return Math.Min(1,PoreVolume);
        }
        /// <summary>
        /// Utility to sum the specified propertie from all pore compartments in the pore layer input 
        /// </summary>
        /// <param name="Compartments"></param>
        /// <param name="Property"></param>
        /// <returns>sum</returns>
        private double LayerSum(Pore[] Compartments, string Property)
        {
            double Sum = 0;
            foreach (Pore P in Compartments)
            {
                object o = ReflectionUtilities.GetValueOfFieldOrProperty(Property, P);
                if (o == null)
                    throw new NotImplementedException();
                Sum += (double)o;
            }
            return Sum;
        }
        #endregion
    }

}