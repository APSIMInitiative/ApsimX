

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
        public double pond { get; set; }
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
        private Water Water = null;
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
        /// <summary>
        /// Allow infiltration processes to be switched off from the UI
        /// </summary>
        [Description("Do you want the soil water model to calculate infiltration processes.  Normally yes, this is for testing")]
        public bool CalculateInfiltration { get; set; }
        /// <summary>
        /// Allow drainage processes to be switched off from the UI
        /// </summary>
        [Description("Do you want the soil water model to calculate draiange processes.  Normally yes, this is for testing")]
        public bool CalculateDrainage { get; set; }
        #endregion

        #region Outputs
        /// <summary>
        /// The amount of water stored in the surface residue
        /// </summary>
        public double ResidueWater { get; set; }
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
        private double[] AbsorptionCapacity { get; set; }
        /// <summary>
        /// How much water can the profile below this layer absorb in the comming hour
        /// </summary>
        [Units("mm")]
        private double[] AbsorptionCapacityBelow { get; set; }
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
            ProfileLayers = Water.Thickness.Length;
            PoreCompartments = PoreBounds.Length - 1;
            AbsorptionCapacity = new double[ProfileLayers];
            AbsorptionCapacityBelow = new double[ProfileLayers];
            PercolationCapacityBelow = new double[ProfileLayers];
            pond = 0;
            SWmm = new double[ProfileLayers];
            SW = new double[ProfileLayers];
            ProfileParams = new ProfileParameters(ProfileLayers);

            double[] InitialWater = new double[Water.Thickness.Length];
            for (int L = 0; L < ProfileLayers; L++)
            {

            }

            Pores = new Pore[ProfileLayers][];
            for (int l = 0; l < ProfileLayers; l++)
            {
                ProfileParams.Ksat[l] = Water.KS[l] / 24; //Convert daily values to hourly
                ProfileParams.SaturatedWaterDepth[l] = Water.SAT[l] * Water.Thickness[l];
                double AccumVolume = 0;
                double AccumWaterVolume = 0;
                Pores[l] = new Pore[PoreCompartments];
                for (int c = PoreCompartments - 1; c >= 0; c--)
                {
                    Pores[l][c] = new Pore();
                    Pores[l][c].MaxDiameter = PoreBounds[c];
                    Pores[l][c].MinDiameter = PoreBounds[c + 1];
                    Pores[l][c].Thickness = Water.Thickness[l];
                    double PoreVolume = Water.SAT[l] * ProportionPoreVolume(PoreBounds[c], PoreBounds[c + 1]);
                    AccumVolume += PoreVolume;
                    Pores[l][c].Volume = PoreVolume;
                    double PoreWaterFilledVolume = Math.Min(PoreVolume, Soil.InitialWaterVolumetric[l] - AccumWaterVolume);
                    AccumWaterVolume += PoreWaterFilledVolume;
                    Pores[l][c].WaterDepth = PoreWaterFilledVolume * Water.Thickness[l];
                    Pores[l][c].HydraulicConductivityIn = ProfileParams.Ksat[l] * (PoreCompartments / (c + 1)) / PoreCompartments; //Arbitary function to give different KS values for each pore
                    Pores[l][c].HydraulicConductivityOut = Math.Max(0, Pores[l][c].HydraulicConductivityIn - ProfileParams.Ksat[l] * 0.5);//arbitary function to give a range of hydraulic conductivity over pores
                }
                if (Math.Abs(AccumVolume - Water.SAT[l]) > FloatingPointTolerance)
                    throw new Exception(this + " Pore volume has not been correctly partitioned between pore compartments in layer " + l);
                if (Math.Abs(AccumWaterVolume - Soil.InitialWaterVolumetric[l]) > FloatingPointTolerance)
                    throw new Exception(this + " Initial water content has not been correctly partitioned between pore compartments in layer" + l);
                SWmm[l] = LayerSum(Pores[l], "WaterDepth");
                SW[l] = LayerSum(Pores[l], "WaterDepth") / Water.Thickness[l];
                ProfileSaturation += Water.SAT[l] * Water.Thickness[1];
            }

            Hourly = new HourlyData();
            ProfileSaturation = MathUtilities.Sum(ProfileParams.SaturatedWaterDepth);
        }
        /// <summary>
        /// Called at the start of each daily timestep
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Irrigation = 0;
            IrrigationDuration = 0;
            Rainfall = 0;
            Drainage = 0;
            Infiltration = 0;
            Array.Clear(Hourly.Irrigation, 0, 24);
            Array.Clear(Hourly.Rainfall, 0, 24);
            Array.Clear(Hourly.Drainage, 0, 24);
            Array.Clear(Hourly.Infiltration, 0, 24);
        }
        /// <summary>
        /// Called when the model is ready to work out daily soil water deltas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {
            //
            
            
            //First we work out how much water is reaching the soil surface each hour
            doPrecipitation();
            for (int h = 0; h < 24; h++)
            {
                InitialProfileWater = MathUtilities.Sum(SWmm);
                InitialPondDepth = pond;
                InitialResidueWater = ResidueWater;
                //Update the depth of Surface water that may infiltrate this hour
                pond += Hourly.Rainfall[h] + Hourly.Irrigation[h];
                //Then we work out how much of this may percolate into the profile each hour
                doPercolationCapacity();
                //Now we know how much water can infiltrate into the soil, lets put it there if we have some
                double HourlyInfiltration = Math.Min(pond, PotentialInfiltration);
                if ((HourlyInfiltration>0)&&(CalculateInfiltration))
                    doInfiltration(HourlyInfiltration,h);
                //Next we redistribute water down the profile for draiange processes
                if (CalculateDrainage)
                doDrainage(h);
                doEvaporation();
                doTranspiration();
                doDownwardDiffusion();
                doUpwardDiffusion();
            }
            Infiltration = MathUtilities.Sum(Hourly.Infiltration);
            Drainage = MathUtilities.Sum(Hourly.Drainage);
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
            Irrigation += IrrigationData.Amount - ResidueInterception(IrrigationData.Amount);
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
        private double FloatingPointTolerance = 0.0000000001;
        /// <summary>
        /// This is the Irrigation ariving at the soil surface, less what has been intercepted by residue
        /// </summary>
        private double Irrigation {get;set; }
        private double IrrigationDuration { get; set; }
        /// <summary>
        /// This is the rainfall ariving at the soil surface, less what has been intercepted by residue
        /// </summary>
        private double Rainfall { get; set; }
        /// <summary>
        /// Variable used for checking mass balance
        /// </summary>
        private double InitialProfileWater { get; set;  }
        /// <summary>
        /// Variable used for checking mass balance
        /// </summary>
        private double InitialPondDepth { get; set; }
        /// <summary>
        /// Variable used for checking mass balance
        /// </summary>
        private double InitialResidueWater { get; set; }
        private double ProfileSaturation { get; set; }
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
            if (Irrigation > 0)
            { //On days when irrigation is applies spread it out into hourly increments
                if (IrrigationDuration > 24)
                    throw new Exception(this + " daily irrigation duration exceeds 24 hours.  There are only 24 hours in each day so it is not really possible to irrigate for longer that this");
                int Irrighours = (int)IrrigationDuration;
                double IrrigationRate = Irrigation / IrrigationDuration;

                for (int h = 0; h < Irrighours; h++)
                {
                    Hourly.Irrigation[h] = IrrigationRate;
                }
                if (Math.Abs(MathUtilities.Sum(Hourly.Irrigation) - Irrigation) > FloatingPointTolerance)
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
                    PotentialAbsorption += Math.Min(Pores[l][c].HydraulicConductivityIn, Pores[l][c].AirDepth);
                }
                AbsorptionCapacity[l] = PotentialAbsorption;
            }
            for (int l = ProfileLayers-1; l >=0; l--)
            {//Then step through each layer and work out how much water the profile below can take
                if (l == ProfileLayers - 1)
                {
                    //In the bottom layer of the profile absorption capaicity below is the amount of water this layer can absorb
                    AbsorptionCapacityBelow[l] = AbsorptionCapacity[l];
                    //In the bottom layer of the profile percolation capacity below is the conductance of the bottom of the profile
                    PercolationCapacityBelow[l] = BottomBoundryConductance;
                }
                else
                {
                    //For subsequent layers up the profile absorpbion capacity below adds the current layer to the sum of the layers below
                    AbsorptionCapacityBelow[l] = AbsorptionCapacityBelow[l + 1] + AbsorptionCapacity[l];
                    //For subsequent layers up the profile the percolation capacity below is the amount that the layer below may absorb
                    //plus the minimum of what may drain through the layer below (ksat of layer below) and what may potentially percolate
                    //Into the rest of the profile below that
                    PercolationCapacityBelow[l] = AbsorptionCapacity[l + 1] + Math.Min(ProfileParams.Ksat[l + 1],PercolationCapacityBelow[l+1]);
                }
            }
            //The amount of water that may percolate below the surface layer plus what ever the surface layer may absorb
            PotentialInfiltration = AbsorptionCapacity[0] + PercolationCapacityBelow[0];
        }
        private void doInfiltration(double WaterToInfiltrate, int h)
        {
            //Do infiltration processes each hour
            double RemainingInfiltration = WaterToInfiltrate;
            for (int l = 0; l < ProfileLayers && RemainingInfiltration > 0; l++)
            { //Start process in the top layer
                DistributWaterInFlux(l, ref RemainingInfiltration);
            }
            //Add infiltration to daily sum for reporting
            Hourly.Infiltration[h] = WaterToInfiltrate;
            pond -= WaterToInfiltrate;

            Hourly.Drainage[h] += RemainingInfiltration;
            //Error checking for debugging.  To be removed when model complted
            UpdateProfileValues();
            CheckMassBalance("Infiltration",h);
        }
        /// <summary>
        /// Gravity moves mobile water out of layers each time step
        /// </summary>
        private void doDrainage(int h)
        {
            for (int l = 0; l < ProfileLayers; l++)
            {//Step through each layer from the top down
                double PotentialDrainage = 0;
                for (int c = PoreCompartments - 1; c >= 0; c--)
                {//Step through each pore compartment and work out how much may drain
                    PotentialDrainage += Math.Min(Pores[l][c].HydraulicConductivityOut, Pores[l][c].WaterDepth);
                }
                //Limit drainage to that of what the layer may drain and that of which the provile below will allow to drain
                double OutFluxCurrentLayer = Math.Min(PotentialDrainage, PercolationCapacityBelow[l]);
                //Catch the drainage from this layer to be the InFlux to the next Layer down the profile
                double InFluxLayerBelow = OutFluxCurrentLayer;
                //Discharge water from current layer
                for (int c = 0; c < PoreCompartments && OutFluxCurrentLayer > 0; c++)
                {//Step through each pore compartment and remove the water that drains starting with the largest pores
                    double drain = Math.Min(OutFluxCurrentLayer, Pores[l][c].HydraulicConductivityOut);
                    Pores[l][c].WaterDepth -= drain;
                    OutFluxCurrentLayer -= drain;
                }
                if (OutFluxCurrentLayer != 0)
                    throw new Exception("Error in drainage calculation");

                //Distribute water from this layer into the profile below and record draiange out the bottom
                //Bring the layer below up to its maximum absorption then move to the next
                for (int l1 = l + 1; l1 < ProfileLayers + 1 && InFluxLayerBelow > 0; l1++)
                {
                    //Any water not stored by this layer will flow to the layer below as saturated drainage
                    if (l1 < ProfileLayers)
                        DistributWaterInFlux(l1, ref InFluxLayerBelow);
                    //If it is the bottom layer, any discharge recorded as drainage from the profile
                    else
                    {
                        Hourly.Drainage[h] += InFluxLayerBelow;
                    }
                }
            }
            //Error checking for debugging.  To be removed when model complted
            UpdateProfileValues();
            CheckMassBalance("Drainage",h); 
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
        /// <summary>
        /// Method takes water flowing into a layer and distributes it between the pore compartments in that layer
        /// </summary>
        /// <param name="l"></param>
        /// <param name="InFlux"></param>
        private void DistributWaterInFlux(int l, ref double InFlux)
        {
            for (int c = PoreCompartments - 1; c >= 0 && InFlux > 0; c--)
            {//Absorb Water onto samllest pores first followed by larger ones
                double PotentialAdsorbtion = Math.Min(Pores[l][c].HydraulicConductivityIn, Pores[l][c].AirDepth);
                double Absorbtion = Math.Min(InFlux, PotentialAdsorbtion);
                Pores[l][c].WaterDepth += Absorbtion;
                InFlux -= Absorbtion;
            }
            if ((LayerSum(Pores[l], "WaterDepth") - ProfileParams.SaturatedWaterDepth[l])>FloatingPointTolerance)
                throw new Exception("Water content of layer " + l + " exceeds saturation.  This is not really possible");
        }
        private void CheckMassBalance(string Process, int h)
        {
            double WaterIn = InitialProfileWater + InitialPondDepth + InitialResidueWater 
                             + Hourly.Rainfall[h] + Hourly.Irrigation[h];
            double ProfileWaterAtCalcEnd = MathUtilities.Sum(SWmm);
            double WaterOut = ProfileWaterAtCalcEnd + pond + ResidueWater + Hourly.Drainage[h];
            if (Math.Abs(WaterIn - WaterOut) > FloatingPointTolerance)
                throw new Exception(this + " " + Process + " calculations are violating mass balance");           
        }
        /// <summary>
        /// Function to update profile summary values
        /// </summary>
        private void UpdateProfileValues()
        {
            for (int l = ProfileLayers - 1; l >= 0; l--)
            {
                SWmm[l] = LayerSum(Pores[l], "WaterDepth");
                SW[l] = LayerSum(Pores[l], "WaterDepth") / Water.Thickness[l];
            }
        }
        #endregion
    }
}