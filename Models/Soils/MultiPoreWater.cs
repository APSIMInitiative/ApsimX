

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

        #region Soil Water Interface
        ///<summary> Who knows</summary>
        public string act_evap_method { get; set; }
        ///<summary> Who knows</summary>
        public double[] AIRDRY { get; }
        ///<summary> Who knows</summary>
        public double[] AIRDRYmm { get; }
        ///<summary> Who knows</summary>
        public double A_to_evap_fact { get; set; }
        ///<summary> Who knows</summary>
        public double canopy_eos_coef { get; set; }
        ///<summary> Who knows</summary>
        public double[] canopy_fact { get; set; }
        ///<summary> Who knows</summary>
        public double canopy_fact_default { get; set; }
        ///<summary> Who knows</summary>
        public double[] canopy_fact_height { get; set; }
        ///<summary> Who knows</summary>
        public double catchment_area { get; set; }
        ///<summary> Who knows</summary>
        public double CN2Bare { get; set; }
        ///<summary> Who knows</summary>
        public double cn2_new { get; }
        ///<summary> Who knows</summary>
        public double CNCov { get; set; }
        ///<summary> Who knows</summary>
        public double CNRed { get; set; }
        ///<summary> Who knows</summary>
        public string[] Depth { get; set; }
        ///<summary> Who knows</summary>
        public double DiffusConst { get; set; }
        ///<summary> Who knows</summary>
        public double DiffusSlope { get; set; }
        ///<summary> Who knows</summary>
        public double discharge_width { get; set; }
        ///<summary> Who knows</summary>
        public double[] dlayer { get; }
        ///<summary> Who knows</summary>
        public double[] dlt_sw { get; set; }
        ///<summary> Who knows</summary>
        public double[] dlt_sw_dep { get; set; }
        ///<summary> Who knows</summary>
        public double Drainage { get; }
        ///<summary> Who knows</summary>
        public double[] DUL { get; }
        ///<summary> Who knows</summary>
        public double[] DULmm { get; }
        ///<summary> Who knows</summary>
        public double Eo { get; }
        ///<summary> Who knows</summary>
        public double Eos { get; }
        ///<summary> Who knows</summary>
        public double Es { get; }
        ///<summary> Who knows</summary>
        public double ESW { get; }
        ///<summary> Who knows</summary>
        public double[] flow { get; }
        ///<summary> Who knows</summary>
        public double[] flow_nh4 { get; }
        ///<summary> Who knows</summary>
        public double[] flow_no3 { get; }
        ///<summary> Who knows</summary>
        public double[] flow_urea { get; }
        ///<summary> Who knows</summary>
        public double[] flux { get; }
        ///<summary> Who knows</summary>
        public double gravity_gradient { get; set; }
        ///<summary> Who knows</summary>
        public double hydrol_effective_depth { get; set; }
        ///<summary> Who knows</summary>
        public string[] immobile_solutes { get; set; }
        ///<summary> Who knows</summary>
        public double Infiltration { get; }
        ///<summary> Who knows</summary>
        public int IrrigLayer { get; }
        ///<summary> Who knows</summary>
        public double[] KLAT { get; set; }
        ///<summary> Who knows</summary>
        public double LeachNH4 { get; }
        ///<summary> Who knows</summary>
        public double LeachNO3 { get; }
        ///<summary> Who knows</summary>
        public double LeachUrea { get; }
        ///<summary> Who knows</summary>
        public double[] LL15 { get; }
        ///<summary> Who knows</summary>
        public double[] LL15mm { get; }
        ///<summary> Who knows</summary>
        ///<summary> Who knows</summary>
        public double max_albedo { get; set; }
        ///<summary> Who knows</summary>
        public double max_crit_temp { get; set; }
        ///<summary> Who knows</summary>
        public double max_pond { get; set; }
        ///<summary> Who knows</summary>
        public double min_crit_temp { get; set; }
        ///<summary> Who knows</summary>
        public string[] mobile_solutes { get; set; }
        ///<summary> Who knows</summary>
        public double[] outflow_lat { get; }
        ///<summary> Who knows</summary>
        public double pond { get; }
        ///<summary> Who knows</summary>
        public double pond_evap { get; }
        ///<summary> Who knows</summary>
        public double Runoff { get; }
        ///<summary> Who knows</summary>
        public double Salb { get; set; }
        ///<summary> Who knows</summary>
        public double[] SAT { get; }
        ///<summary> Who knows</summary>
        public double[] SATmm { get; }
        ///<summary> Who knows</summary>
        public double slope { get; set; }
        ///<summary> Who knows</summary>
        public double[] solute_flow_eff { get; set; }
        ///<summary> Who knows</summary>
        public double[] solute_flux_eff { get; set; }
        ///<summary> Who knows</summary>
        public double specific_bd { get; set; }
        ///<summary> Who knows</summary>
        public double sumes1_max { get; set; }
        ///<summary> Who knows</summary>
        public double sumes2_max { get; set; }
        ///<summary> Who knows</summary>
        public double SummerCona { get; set; }
        ///<summary> Who knows</summary>
        public string SummerDate { get; set; }
        ///<summary> Who knows</summary>
        public double SummerU { get; set; }
        ///<summary> Who knows</summary>
        public double[] SW { get; set; }
        ///<summary> Who knows</summary>
        public double[] SWCON { get; set; }
        ///<summary> Who knows</summary>
        public double[] SWmm { get; set; }
        ///<summary> Who knows</summary>
        public double sw_top_crit { get; set; }
        ///<summary> Who knows</summary>
        public double t { get; }
        ///<summary> Who knows</summary>
        public double[] Thickness { get; set; }
        ///<summary> Who knows</summary>
        public double WaterTable { get; set; }
        ///<summary> Who knows</summary>
        public double WinterCona { get; set; }
        ///<summary> Who knows</summary>
        public string WinterDate { get; set; }
        ///<summary> Who knows</summary>
        public double WinterU { get; set; }

        /////<summary> Who knows</summary>
        //public event NitrogenChangedDelegate NitrogenChanged;

        ///<summary> Who knows</summary>
        public void Reset()
        { }
        ///<summary> Who knows</summary>
        public void SetMaxPond(double NewDepth)
        { }
        ///<summary> Who knows</summary>
        public void SetSWmm(int Layer, double NewSWmm)
        { }
        ///<summary> Who knows</summary>
        public void SetWaterTable(double InitialDepth)
        { }
        ///<summary> Who knows</summary>
        public void SetWater_frac(double[] New_SW)
        { }
        ///<summary> Who knows</summary>
        public void SetWater_mm(double[] New_SW_dep)
        { }
        ///<summary> Who knows</summary>
        public void Tillage(TillageType Data)
        { }
        ///<summary> Who knows</summary>
        public void Tillage(string DefaultTillageName)
        { }
        #endregion

        /// <summary>
        /// The amount of water in the layer
        /// </summary>
        [XmlIgnore]
        public double[] Water { get; set; }

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
            Water = new double[] { 0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00 };
        }
    }

  }