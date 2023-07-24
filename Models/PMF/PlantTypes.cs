using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Library;
using Models.Soils;
using Models.Surface;

namespace Models.PMF
{

    /// <summary>
    /// Data passed to leaf tip appearance occurs.
    /// </summary>
    [Serializable]
    public class ApparingLeafParams : EventArgs
    {
        /// <summary>The numeric rank of the cohort appaeraing</summary>
        public int CohortToAppear { get; set; }
        /// <summary>The populations of leaves in the appearing cohort</summary>
        public double TotalStemPopn { get; set; }
        /// <summary>The Tt age of the the cohort appearing</summary>
        public double CohortAge { get; set; }
        /// <summary>The proportion of the cohort appearing if final cohort</summary>
        public double FinalFraction { get; set; }
    }

    /// <summary>
    /// Data passed to leaf tip appearance occurs.
    /// </summary>
    [Serializable]
    public class CohortInitParams : EventArgs
    {
        /// <summary>The numeric rank of the cohort appaeraing</summary>
        public int Rank { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public class WaterUptakesCalculatedUptakesType
    {
        /// <summary>The name</summary>
        public String Name = "";
        /// <summary>The amount</summary>
        public Double[] Amount;
    }
    /// <summary>
    /// 
    /// </summary>
    public class WaterUptakesCalculatedType
    {
        /// <summary>The uptakes</summary>
        public WaterUptakesCalculatedUptakesType[] Uptakes;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void WaterUptakesCalculatedDelegate(WaterUptakesCalculatedType Data);

    /// <summary>
    /// 
    /// </summary>
    public class KillLeafType
    {
        /// <summary>The kill fraction</summary>
        public Single KillFraction;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void FOMLayerDelegate(FOMLayerType Data);
    /// <summary>
    /// 
    /// </summary>
    public delegate void NullTypeDelegate();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);

    /// <summary>
    /// 
    /// </summary>
    public class BiomassRemovedType
    {
        /// <summary>The crop_type</summary>
        public String crop_type = "";
        /// <summary>The dm_type</summary>
        public String[] dm_type;
        /// <summary>The dlt_crop_dm</summary>
        public Single[] dlt_crop_dm;
        /// <summary>The DLT_DM_N</summary>
        public Single[] dlt_dm_n;
        /// <summary>The DLT_DM_P</summary>
        public Single[] dlt_dm_p;
        /// <summary>The fraction_to_residue</summary>
        public Single[] fraction_to_residue;
    }
}
