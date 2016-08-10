

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
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class MultiPoreWater : Model, ISoilWater
    {
        #region IsoilInterface
        /// <summary>The amount of rainfall intercepted by surface residues</summary>
        [XmlIgnore]
        public double residueinterception { get; set; }
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
        public double[] dlt_sw { get;  set; }
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
        public void SetSWmm(int Layer, double NewSWmm){ }
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
        #endregion
        
        #region Structures
        /// <summary>
        /// This is the data structure that represents the soils layers and pore cagatories in each layer
        /// </summary>
        public Pore[][] Pores;
        /// <summary>
        /// Water reaching the soil surface
        /// </summary>
        public double[] SurfaceWater { get; set; }
        #endregion

        #region Parameters
        /// <summary>
        /// The maximum diameter of pore compartments
        /// </summary>
        [Units("nm")]
        [Description("The pore diameters the seperate modeled pore compartemnts")]
        public double[] PoreBounds { get; set; }
        #endregion

        #region Outputs
        /// <summary>
        /// The amount of water in the layer
        /// </summary>
        [XmlIgnore]
        public double[] Water { get; set; }
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
            PoreCompartments = PoreBounds.Length-1;
            SWmm = new double[ProfileLayers];
            
            double[] InitialWater = new double[Water2.Thickness.Length];
            for (int L = 0; L < ProfileLayers; L++)
            {
                
            }

            Pores = new Pore[ProfileLayers][];
            for (int l = 0; l < ProfileLayers; l++)
            {
                double AccumVolume = 0;
                double AccumWaterVolume = 0;
                Pores[l] = new Pore[PoreCompartments];
                for (int c = PoreCompartments-1; c >= 0; c--)
                {
                    Pores[l][c] = new Pore();
                    Pores[l][c].MaxDiameter = PoreBounds[c];
                    Pores[l][c].MinDiameter = PoreBounds[c+1];
                    Pores[l][c].Thickness = Water2.Thickness[l];
                    double PoreVolume = Water2.SAT[l] * ProportionPoreVolume(PoreBounds[c], PoreBounds[c + 1]);
                    AccumVolume += PoreVolume;
                    Pores[l][c].Volume = PoreVolume;
                    double PoreWaterFilledVolume = Math.Min(PoreVolume, Soil.InitialWaterVolumetric[l] - AccumWaterVolume);
                    AccumWaterVolume += PoreWaterFilledVolume;
                    Pores[l][c].WaterFilledVolume = PoreWaterFilledVolume;
                }
                if (Math.Abs(AccumVolume - Water2.SAT[l])>FloatingPointTolerance)
                    throw new Exception(this + " Pore volume has not been correctly partitioned between pore compartments in layer " + l);
                if (Math.Abs(AccumWaterVolume - Soil.InitialWaterVolumetric[l]) > FloatingPointTolerance)
                    throw new Exception(this + " Initial water content has not been correctly partitioned between pore compartments in layer" + l);
                SWmm[l] = LayerSum(Pores[l],"Waterdepth"); 
            }
        }
        /// <summary>
        /// Called at the start of each daily timestep
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            
        }
        /// <summary>
        /// Called when the model is ready to work out daily soil water deltas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {
            doPrecipitation();
            doInfiltration();
            doUnSaturatedDrainage();
            doEvaporation();
            doTranspiration();
            doDownwardDiffusion();
            doUpwardDiffusion();
        }
        #endregion

        #region Internal States
        private double FloatingPointTolerance = 0.000000001;
        #endregion

        #region Internal Properties and Methods
        /// <summary>
        /// Potential gradients moves water out of layers each time step
        /// </summary>
        private void doPrecipitation()
        {
            //calculate 
        }
        /// <summary>
        /// Carries out infiltration processes at each time step
        /// </summary>
        private void doInfiltration()
        {

        }
        /// <summary>
        /// Gravity moves mobile water out of layers each time step
        /// </summary>
        private void doUnSaturatedDrainage()
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