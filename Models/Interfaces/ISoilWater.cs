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
        /// <summary>The amount of rainfall intercepted by surface residues</summary>
        double residueinterception { get; set; }
        ///<summary> Who knows</summary>
        double catchment_area { get; set; }
        ///<summary> Who knows</summary>
        double CN2Bare { get; set; }
        ///<summary> Who knows</summary>
        double CNCov { get; set; }
        ///<summary> Who knows</summary>
        double CNRed { get; set; }
        ///<summary> Who knows</summary>
        double DiffusConst { get; set; }
        ///<summary> Who knows</summary>
        double DiffusSlope { get; set; }
        ///<summary> Who knows</summary>
        double discharge_width { get; set; }
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
        double Infiltration { get; }
        ///<summary> Who knows</summary>
        double[] KLAT { get; }
        ///<summary> Who knows</summary>
        double LeachNH4 { get; }
        ///<summary> Who knows</summary>
        double LeachNO3 { get; }
        ///<summary> Who knows</summary>
        double LeachUrea { get; }
        ///<summary> Who knows</summary>
        double[] LL15mm { get; }
        ///<summary> Who knows</summary>
        double max_pond { get; set; }
        ///<summary> Who knows</summary>
        ///<summary> Who knows</summary>
        double[] outflow_lat { get; }
        ///<summary> Who knows</summary>
        double pond { get; }
        ///<summary> Who knows</summary>
        ///<summary> Who knows</summary>
        double Runoff { get; }
        ///<summary> Who knows</summary>
        double Salb { get; set; }
        ///<summary> Who knows</summary>
        double[] SATmm { get; }
        ///<summary> Who knows</summary>
        double slope { get; set; }
        ///<summary> Who knows</summary>
        double[] solute_flow_eff { get; set; }
        ///<summary> Who knows</summary>
        double[] solute_flux_eff { get; set; }
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
        double[] Thickness { get; set; }
        ///<summary> Who knows</summary>
        double WaterTable { get; set; }
        ///<summary> Who knows</summary>
        double WinterCona { get; set; }
        ///<summary> Who knows</summary>
        string WinterDate { get; set; }
        ///<summary> Who knows</summary>
        double WinterU { get; set; }
        ///<summary> Who knows</summary>
        void SetSWmm(int Layer, double NewSWmm);
        }
}
