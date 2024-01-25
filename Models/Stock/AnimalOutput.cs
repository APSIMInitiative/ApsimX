using System;
using APSIM.Shared.Utilities;

namespace Models.GrazPlan
{

    /// <summary>
    /// The Animal outputs object
    /// </summary>
    [Serializable]
    public class AnimalOutput
    {
        /// <summary>
        /// Potential intake, after correction for legume content of the diet
        /// </summary>
        public double IntakeLimitLegume;

        /// <summary>
        /// Intakes for interface with pasture model
        /// </summary>
        public GrazType.GrazingOutputs IntakePerHead = new GrazType.GrazingOutputs();

        // ...... Intake-related values - exclude grain that passes the gut undamaged. ......
        /// <summary>
        /// Intakes summarised for use in the nutrition model
        /// </summary>
        public GrazType.IntakeRecord PaddockIntake = new GrazType.IntakeRecord();

        /// <summary>
        /// Intakes summarised for use in the nutrition model
        /// </summary>
        public GrazType.IntakeRecord SuppIntake = new GrazType.IntakeRecord();

        /// <summary>
        /// Daily dry matter intake (kg) - not milk
        /// </summary>
        public Diet DM_Intake = new Diet();

        /// <summary>
        /// Daily crude protein intake (kg)
        /// </summary>
        public Diet CP_Intake = new Diet();

        /// <summary>
        /// Daily phosphorus intake (kg)
        /// </summary>
        public Diet Phos_Intake = new Diet();

        /// <summary>
        /// Daily sulphur intake (kg)
        /// </summary>
        public Diet Sulf_Intake = new Diet();

        /// <summary>
        /// Metabolizable energy intake (MJ)
        /// </summary>
        public Diet ME_Intake = new Diet();

        /// <summary>
        /// Digestibility of diet components (0-1)
        /// </summary>
        public Diet Digestibility = new Diet();

        /// <summary>
        /// Crude protein concentrations (0-1)
        /// </summary>
        public Diet ProteinConc = new Diet();

        /// <summary>
        /// ME:dry matter ratios (MJ/kg)
        /// </summary>
        public Diet ME_2_DM = new Diet();

        /// <summary>
        /// Proportion of each component in the diet 
        /// </summary>
        public Diet DietPropn = new Diet();

        /// <summary>
        /// Degradability of protein in diet (0-1), corrected 
        /// </summary>
        public Diet CorrDgProt = new Diet();

        // ..................................................................
        /// <summary>
        /// Microbial crude protein (kg)
        /// </summary>
        public double MicrobialCP;

        /// <summary>
        /// Digestible protein leaving the stomach (kg): total
        /// </summary>
        public double DPLS;

        /// <summary>
        /// Digestible protein leaving the stomach (kg): from milk
        /// </summary>
        public double DPLS_Milk;

        /// <summary>
        /// Digestible protein leaving the stomach (kg): from MCP
        /// </summary>
        public double DPLS_MCP;

        /// <summary>
        /// DPLS available for wool growth (kg)
        /// </summary>
        public double DPLS_Avail_Wool;

        /// <summary>
        /// Intake of undegradable protein (kg)
        /// </summary>
        public double UDP_Intake;

        /// <summary>
        /// Digestibility of UDP (0-1)
        /// </summary>
        public double UDP_Dig;

        /// <summary>
        /// Requirement for UDP (kg)
        /// </summary>
        public double UDP_Reqd;

        /// <summary>
        /// Daily intake for RDP (kg)
        /// </summary>
        public double RDP_Intake;

        /// <summary>
        /// Daily requirement for RDP (kg)
        /// </summary>
        public double RDP_Reqd;

        //// Allocation of energy and protein to various uses.                          
        /// <summary>
        /// Allocation of energy
        /// </summary>
        public PhysiologicalState EnergyUse = new PhysiologicalState();

        /// <summary>
        /// Allocation of protein
        /// </summary>
        public PhysiologicalState ProteinUse = new PhysiologicalState();

        /// <summary>
        /// Physiology record
        /// </summary>
        public PhysiologicalState Phos_Use = new PhysiologicalState();

        /// <summary>
        /// Sulphur use
        /// </summary>
        public PhysiologicalState Sulf_Use = new PhysiologicalState();

        /// <summary>
        /// Efficiencies of ME use (0-1)
        /// </summary>
        public PhysiologicalState Efficiency = new PhysiologicalState();

        /// <summary>
        /// Endogenous faecal losses      (N,S,P)
        /// </summary>
        public GrazType.DM_Pool EndoFaeces = new GrazType.DM_Pool();

        /// <summary>
        /// Total organic faecal losses   (DM,N,S,P)
        /// </summary>
        public GrazType.DM_Pool OrgFaeces = new GrazType.DM_Pool();

        /// <summary>
        /// Total inorganic faecal losses (N,S,P)
        /// </summary>
        public GrazType.DM_Pool InOrgFaeces = new GrazType.DM_Pool();

        /// <summary>
        /// Total urinary losses of       (N,S,P)
        /// </summary>
        public GrazType.DM_Pool Urine = new GrazType.DM_Pool();

        /// <summary>
        /// N in dermal losses (kg)
        /// </summary>
        public double DermalNLoss;

        /// <summary>
        /// 
        /// </summary>
        public double GainEContent;

        /// <summary>
        /// 
        /// </summary>
        public double GainPContent;

        /// <summary>
        /// Increase in conceptus weight (kg/d)
        /// </summary>
        public double ConceptusGrowth;

        /// <summary>
        /// Net energy retained in wool (MJ)
        /// </summary>
        public double TotalWoolEnergy;

        /// <summary>
        /// Thermoneutral heat production (MJ)
        /// </summary>
        public double Therm0HeatProdn;

        /// <summary>
        /// Lower critical temperature from the chilling submodel (oC)      
        /// </summary>
        public double LowerCritTemp;

        /// <summary>
        /// 
        /// </summary>
        public double RDP_IntakeEffect;

        /// <summary>
        /// Copy a AnimalOutput object
        /// </summary>
        /// <returns>The clone of an animal output</returns>
        public AnimalOutput Copy()
        {
            return ReflectionUtilities.Clone(this) as AnimalOutput;
        }
    }
}
