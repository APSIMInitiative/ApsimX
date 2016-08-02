

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
        /// <summary>The amount of rainfall intercepted by surface residues</summary>
        public double residueinterception { get; set; }
        ///<summary> Who knows</summary>
        public double catchment_area { get; set; }
        ///<summary> Who knows</summary>
        public double CN2Bare { get; set; }
        ///<summary> Who knows</summary>
        public double CNCov { get; set; }
        ///<summary> Who knows</summary>
        public double CNRed { get; set; }
        ///<summary> Who knows</summary>
        public double DiffusConst { get; set; }
        ///<summary> Who knows</summary>
        public double DiffusSlope { get; set; }
        ///<summary> Who knows</summary>
        public double discharge_width { get; set; }
        ///<summary> Who knows</summary>
        public double[] dlt_sw_dep { get; set; }
        ///<summary> Who knows</summary>
        public double Drainage { get; set; }
        ///<summary> Who knows</summary>
        public double[] DUL { get; set; }
        ///<summary> Who knows</summary>
        public double[] DULmm { get; set; }
        ///<summary> Who knows</summary>
        public double Eo { get; set; }
        ///<summary> Who knows</summary>
        public double Eos { get; set; }
        ///<summary> Who knows</summary>
        public double Es { get; set; }
        ///<summary> Who knows</summary>
        public double ESW { get; set; }
        ///<summary> Who knows</summary>
        public double[] flow { get; set; }
        ///<summary> Who knows</summary>
        public double[] flow_nh4 { get; set; }
        ///<summary> Who knows</summary>
        public double[] flow_no3 { get; set; }
        ///<summary> Who knows</summary>
        public double[] flow_urea { get; set; }
        ///<summary> Who knows</summary>
        public double[] flux { get; set; }
        ///<summary> Who knows</summary>
        public double gravity_gradient { get; set; }
        ///<summary> Who knows</summary>
        public double Infiltration { get; set; }
        ///<summary> Who knows</summary>
        public double[] KLAT { get; set; }
        ///<summary> Who knows</summary>
        public double LeachNH4 { get; set; }
        ///<summary> Who knows</summary>
        public double LeachNO3 { get; set; }
        ///<summary> Who knows</summary>
        public double LeachUrea { get; set; }
        ///<summary> Who knows</summary>
        public double[] LL15 { get; set; }
        ///<summary> Who knows</summary>
        public double[] LL15mm { get; set; }
        ///<summary> Who knows</summary>
        public double max_pond { get; set; }
        ///<summary> Who knows</summary>
        ///<summary> Who knows</summary>
        public double[] outflow_lat { get; set; }
        ///<summary> Who knows</summary>
        public double pond { get; set; }
        ///<summary> Who knows</summary>
        public double pond_evap { get; set; }
        ///<summary> Who knows</summary>
        public double Runoff { get; set; }
        ///<summary> Who knows</summary>
        public double Salb { get; set; }
        ///<summary> Who knows</summary>
        public double[] SATmm { get; set; }
        ///<summary> Who knows</summary>
        public double slope { get; set; }
        ///<summary> Who knows</summary>
        public double[] solute_flow_eff { get; set; }
        ///<summary> Who knows</summary>
        public double[] solute_flux_eff { get; set; }
        ///<summary> Who knows</summary>
        public double specific_bd { get; set; }
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
        public double[] Thickness { get; set; }
        ///<summary> Who knows</summary>
        public double WaterTable { get; set; }
        ///<summary> Who knows</summary>
        public double WinterCona { get; set; }
        ///<summary> Who knows</summary>
        public string WinterDate { get; set; }
        ///<summary> Who knows</summary>
        public double WinterU { get; set; }
        ///<summary> Who knows</summary>
        public void SetSWmm(int Layer, double NewSWmm){ }

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