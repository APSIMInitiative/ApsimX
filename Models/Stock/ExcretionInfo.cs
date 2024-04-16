using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// ExcretionInfo type. Totalled amounts of excretion                           
    /// </summary>
    [Serializable]
    public class ExcretionInfo
    {
        /// <summary>
        /// Organic faeces pool
        /// </summary>
        public GrazType.DM_Pool OrgFaeces = new GrazType.DM_Pool();

        /// <summary>
        /// Inorganic faeces pool
        /// </summary>
        public GrazType.DM_Pool InOrgFaeces = new GrazType.DM_Pool();

        /// <summary>
        /// Urine pool
        /// </summary>
        public GrazType.DM_Pool Urine = new GrazType.DM_Pool();

        /// <summary>
        /// Number in the time step by all animals (not including unweaned young)
        /// </summary>
        public double Defaecations;

        /// <summary>
        /// Volume per defaecation, m^3 (fresh basis)
        /// </summary>
        public double DefaecationVolume;

        /// <summary>
        /// Area per defaecation, m^2 (fresh basis)
        /// </summary>
        public double DefaecationArea;

        /// <summary>
        /// Eccentricity of faeces
        /// </summary>
        public double DefaecationEccentricity;

        /// <summary>
        /// Proportion of faecal inorganic N that is nitrate
        /// </summary>
        public double FaecalNO3Propn;

        /// <summary>
        /// Number in the time step by all animals (not including unweaned young)
        /// </summary>
        public double Urinations;

        /// <summary>
        /// Fluid volume per urination, m^3
        /// </summary>
        public double UrinationVolume;

        /// <summary>
        /// Area covered by each urination at the soil surface, m^2
        /// </summary>
        public double UrinationArea;

        /// <summary>
        /// Eccentricity of urinations
        /// </summary>
        public double dUrinationEccentricity;
    }
}
