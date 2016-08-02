// -----------------------------------------------------------------------
// <copyright file="IUptake.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System.Collections.Generic;
    using Models.Soils.Arbitrator;
    using Soils;

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface ISoilWater
    {
        ///<summary> Who knows</summary>
        string act_evap_method { get; set; }
        ///<summary> Who knows</summary>
        double[] AIRDRY { get; }
        ///<summary> Who knows</summary>
        double[] AIRDRYmm { get; }
        ///<summary> Who knows</summary>
        double A_to_evap_fact { get; set; }
        ///<summary> Who knows</summary>
        double canopy_eos_coef { get; set; }
        ///<summary> Who knows</summary>
        double[] canopy_fact { get; set; }
        ///<summary> Who knows</summary>
        double canopy_fact_default { get; set; }
        ///<summary> Who knows</summary>
        double[] canopy_fact_height { get; set; }
        ///<summary> Who knows</summary>
        double catchment_area { get; set; }
        ///<summary> Who knows</summary>
        double CN2Bare { get; set; }
        ///<summary> Who knows</summary>
        double cn2_new { get; }
        ///<summary> Who knows</summary>
        double CNCov { get; set; }
        ///<summary> Who knows</summary>
        double CNRed { get; set; }
        ///<summary> Who knows</summary>
        string[] Depth { get; set; }
        ///<summary> Who knows</summary>
        double DiffusConst { get; set; }
        ///<summary> Who knows</summary>
        double DiffusSlope { get; set; }
        ///<summary> Who knows</summary>
        double discharge_width { get; set; }
        ///<summary> Who knows</summary>
        double[] dlayer { get; }
        ///<summary> Who knows</summary>
        double[] dlt_sw { set; }
        ///<summary> Who knows</summary>
        double[] dlt_sw_dep { set; }
        ///<summary> Who knows</summary>
        double Drainage { get; }
        ///<summary> Who knows</summary>
        double[] DUL { get; }
        ///<summary> Who knows</summary>
        double[] DULmm { get; }
        ///<summary> Who knows</summary>
        double Eo { get; }
        ///<summary> Who knows</summary>
        double Eos { get; }
        ///<summary> Who knows</summary>
        double Es { get; }
        ///<summary> Who knows</summary>
        double ESW { get; }
        ///<summary> Who knows</summary>
        double[] flow { get; }
        ///<summary> Who knows</summary>
        double[] flow_nh4 { get; }
        ///<summary> Who knows</summary>
        double[] flow_no3 { get; }
        ///<summary> Who knows</summary>
        double[] flow_urea { get; }
        ///<summary> Who knows</summary>
        double[] flux { get; }
        ///<summary> Who knows</summary>
        double gravity_gradient { get; set; }
        ///<summary> Who knows</summary>
        double hydrol_effective_depth { get; set; }
        ///<summary> Who knows</summary>
        string[] immobile_solutes { get; set; }
        ///<summary> Who knows</summary>
        double Infiltration { get; }
        ///<summary> Who knows</summary>
        int IrrigLayer { get; }
        ///<summary> Who knows</summary>
        double[] KLAT { get; set; }
        ///<summary> Who knows</summary>
        double LeachNH4 { get; }
        ///<summary> Who knows</summary>
        double LeachNO3 { get; }
        ///<summary> Who knows</summary>
        double LeachUrea { get; }
        ///<summary> Who knows</summary>
        double[] LL15 { get; }
        ///<summary> Who knows</summary>
        double[] LL15mm { get; }
        ///<summary> Who knows</summary>
        ///<summary> Who knows</summary>
        double max_albedo { get; set; }
        ///<summary> Who knows</summary>
        double max_crit_temp { get; set; }
        ///<summary> Who knows</summary>
        double max_pond { get; set; }
        ///<summary> Who knows</summary>
        double min_crit_temp { get; set; }
        ///<summary> Who knows</summary>
        string[] mobile_solutes { get; set; }
        ///<summary> Who knows</summary>
        double[] outflow_lat { get; }
        ///<summary> Who knows</summary>
        double pond { get; }
        ///<summary> Who knows</summary>
        double pond_evap { get; }
        ///<summary> Who knows</summary>
        double Runoff { get; }
        ///<summary> Who knows</summary>
        double Salb { get; set; }
        ///<summary> Who knows</summary>
        double[] SAT { get; }
        ///<summary> Who knows</summary>
        double[] SATmm { get; }
        ///<summary> Who knows</summary>
        double slope { get; set; }
        ///<summary> Who knows</summary>
        double[] solute_flow_eff { get; set; }
        ///<summary> Who knows</summary>
        double[] solute_flux_eff { get; set; }
        ///<summary> Who knows</summary>
        double specific_bd { get; set; }
        ///<summary> Who knows</summary>
        double sumes1_max { get; set; }
        ///<summary> Who knows</summary>
        double sumes2_max { get; set; }
        ///<summary> Who knows</summary>
        double SummerCona { get; set; }
        ///<summary> Who knows</summary>
        string SummerDate { get; set; }
        ///<summary> Who knows</summary>
        double SummerU { get; set; }
        ///<summary> Who knows</summary>
        double[] SW { get; set; }
        ///<summary> Who knows</summary>
        double[] SWCON { get; set; }
        ///<summary> Who knows</summary>
        double[] SWmm { get; set; }
        ///<summary> Who knows</summary>
        double sw_top_crit { get; set; }
        ///<summary> Who knows</summary>
        double t { get; }
        ///<summary> Who knows</summary>
        double[] Thickness { get; set; }
        ///<summary> Who knows</summary>
        double WaterTable { get; set; }
        ///<summary> Who knows</summary>
        double WinterCona { get; set; }
        ///<summary> Who knows</summary>
        string WinterDate { get; set; }
        ///<summary> Who knows</summary>
        double WinterU { get; set; }

        /////<summary> Who knows</summary>
        //event NitrogenChangedDelegate NitrogenChanged;
        
        ///<summary> Who knows</summary>
        void Reset();
        ///<summary> Who knows</summary>
        void SetMaxPond(double NewDepth);
        ///<summary> Who knows</summary>
        void SetSWmm(int Layer, double NewSWmm);
        ///<summary> Who knows</summary>
        void SetWaterTable(double InitialDepth);
        ///<summary> Who knows</summary>
        void SetWater_frac(double[] New_SW);
        ///<summary> Who knows</summary>
        void SetWater_mm(double[] New_SW_dep);
        ///<summary> Who knows</summary>
        void Tillage(TillageType Data);
        ///<summary> Who knows</summary>
        void Tillage(string DefaultTillageName);
    }
}
