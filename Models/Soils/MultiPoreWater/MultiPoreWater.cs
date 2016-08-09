

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
        //[Link]
        //Soil Soil = null;
        #endregion
        
        #region Structures
        /// <summary>
        /// This is the data structure that represents the soils layers and pore cagatories in each layer
        /// </summary>
        public Pore[][] Pores;
        #endregion

        #region Parameters
        /// <summary>
        /// The maximum diameter of pore compartments
        /// </summary>
        [Units("nm")]
        [Description("The upper boundary of poresize for each pore group")]
        public double[] PoreMaxDiameter { get; set; }
        /// <summary>
        /// The minimum diameter of pore compartments
        /// </summary>
        [Units("nm")]
        [Description("The lower boundary of poresize for each pore group")]
        public double[] PoreMinDiameter { get; set; }
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
             
            if (PoreMaxDiameter.Length != PoreMinDiameter.Length)
                throw new Exception(this + "Must enter the same number of max and min pore diameters");
            ProfileLayers = Thickness.Length;
            PoreCompartments = PoreMaxDiameter.Length;
            

            double[] InitialWater = new double[Thickness.Length];
            for (int L = 0; L < ProfileLayers; L++)
            {
                
            }

            Pores = new Pore[ProfileLayers][];
            for (int l = 0; l < ProfileLayers; l++)
            {
                Pores[l] = new Pore[PoreCompartments];
                for (int c = 0; c < PoreCompartments; c++)
                {
                    Pores[l][c].MaxDiameter = PoreMaxDiameter[c];
                    Pores[l][c].MinDiameter = PoreMinDiameter[c];
                    Pores[l][c].Thickness = Thickness[l];
                    Pores[l][c].Volume = 0;
                    Pores[l][c].Watermass = 0;
                }
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

        }
        #endregion

        #region Internal States

        #endregion

        #region Internal Properties and Methods

        #endregion
    }

}