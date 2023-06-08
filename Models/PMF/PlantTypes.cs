using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Library;
using Models.Soils;

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

    ///<summary>Data passed to each organ when a biomass remove event occurs.  The proportion of biomass to be removed from each organ is the sum of the FractionToRemove and the FractionToRedidues</summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(BiomassRemoval))]
    public class OrganBiomassRemovalType : Model
    {
        /// <summary>
        /// The amount of live biomass taken from each organ and removeed from the zone on harvest, cut, graze or prune.
        /// </summary>
        [Description("Fraction of live biomass to remove from plant (remove from the system)")]
        public double FractionLiveToRemove { get; set; }

        /// <summary>
        /// The amount of dead biomass taken from each organ and removeed from the zone on harvest, cut, graze or prune.
        /// </summary>
        [Description("Fraction of dead biomass to remove from plant (remove from the system)")]
        public double FractionDeadToRemove { get; set; }

        /// <summary>
        /// The amount of live biomass to removed from each organ and passed to residue pool on on harvest, cut, graze or prune
        /// </summary>
        [Description("Fraction of live biomass to remove from plant (send to surface organic matter")]
        public double FractionLiveToResidue { get; set; }

        /// <summary>
        /// The amount of dead biomass to removed from each organ and passed to residue pool on on harvest, cut, graze or prune
        /// </summary>
        [Description("Fraction of dead biomass to remove from plant (send to surface organic matter")]
        public double FractionDeadToResidue { get; set; }
    }

    ///<summary>Data structure to hold removal and residue returns fractions for all plant organs</summary>
    [Serializable]
    public class RemovalFractions
    {
        private static RemovalFractions phenologyToEnd = new RemovalFractions() { SetPhenologyToEnd = true };

        /// <summary>
        /// The list of BiomassRemovalTypes for each organ
        ///</summary>
        private Dictionary<string, OrganBiomassRemovalType> removalValues = new Dictionary<string, OrganBiomassRemovalType>();
        /// <summary>
        /// The Phenological stage that biomass removal resets phenology to.
        ///</summary>
        public double SetThinningProportion { get; set; }

        /// <summary>
        /// The Phenological stage that biomass removal resets phenology to.
        ///</summary>
        public double SetPhenologyStage { get; set; }

        /// <summary>
        /// The Phenological stage to the last stage.
        ///</summary>
        public bool SetPhenologyToEnd { get; set; }

        /// <summary>
        /// The nunber of Main-stem nodes to remove
        /// </summary>
        public int NodesToRemove { get; set; }

        /// <summary>Set phenology to the last stage.</summary>
        public static RemovalFractions PhenologyToEnd => phenologyToEnd;

        /// <summary>
        /// Method to set the FractionToRemove for specified Organ
        ///</summary>
        public void SetFractionToRemove(string organName, double fraction, string biomassType = "live")
        {
            if ((fraction > 1.0) || (fraction < 0.0))
            {
                throw new Exception("Fraction removed from " +organName+" must be between zero and one");
            }
            else
            {
                if (removalValues.ContainsKey(organName))
                {
                    if (biomassType.ToLower() == "live")
                        removalValues[organName].FractionLiveToRemove = fraction;
                    else if (biomassType.ToLower() == "dead")
                        removalValues[organName].FractionDeadToRemove = fraction;
                    else
                        throw new Exception("Type of biomass to remove should be either \"live\" or \"dead\"");
                }
                else
                {
                    if (biomassType.ToLower() == "live")
                        removalValues.Add(organName, new OrganBiomassRemovalType() { FractionLiveToRemove = fraction });
                    else if (biomassType.ToLower() == "dead")
                        removalValues.Add(organName, new OrganBiomassRemovalType() { FractionDeadToRemove = fraction });
                    else
                        throw new Exception("Type of biomass to remove should be either \"live\" or \"dead\"");
                }
            }
        }

        /// <summary>
        /// Method to set the FractionToResidue for specified Organ
        ///</summary>
        public void SetFractionToResidue(string organName, double fraction, string biomassType = "live")
        {
            if ((fraction > 1.0) || (fraction < 0.0))
            {
                throw new Exception("Residue Fraction removed from " +organName+" must be between zero and one");
            }
            else
            { 
                if (removalValues.ContainsKey(organName))
                {
                    if (biomassType.ToLower() == "live")
                        removalValues[organName].FractionLiveToResidue = fraction;
                    else if (biomassType.ToLower() == "dead")
                        removalValues[organName].FractionDeadToResidue = fraction;
                    else
                        throw new Exception("Type of biomass to send to residue should be either \"live\" or \"dead\"");
                }
                else
                {
                    if (biomassType.ToLower() == "live")
                        removalValues.Add(organName, new OrganBiomassRemovalType() { FractionLiveToResidue = fraction });
                    else if (biomassType.ToLower() == "dead")
                        removalValues.Add(organName, new OrganBiomassRemovalType() { FractionDeadToResidue = fraction });
                    else
                        throw new Exception("Type of biomass to send to residue should be either \"live\" or \"dead\"");
                }
            }
        }

        /// <summary>
        /// Gets the removal fractions for the specified organ or null if not found.
        /// </summary>
        /// <param name="organName">The organ name to look for.</param>
        public OrganBiomassRemovalType GetFractionsForOrgan(string organName)
        {
            if (removalValues.ContainsKey(organName))
                return removalValues[organName];
            else
                return null;
        }
    }
}
