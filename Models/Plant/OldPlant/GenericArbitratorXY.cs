using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using APSIM.Shared.Utilities;


namespace Models.PMF.OldPlant
{
    /// <summary>
    /// A generic arbitrator
    /// </summary>
    [Serializable]
    public class GenericArbitratorXY : Model
    {
        /// <summary>Gets or sets the partition organs.</summary>
        /// <value>The partition organs.</value>
        public string[] PartitionOrgans { get; set; }

        /// <summary>Gets or sets the partition rules.</summary>
        /// <value>The partition rules.</value>
        public string[] PartitionRules { get; set; }

        /// <summary>The ratio root shoot</summary>
        [Link]
        IFunction RatioRootShoot = null;

        /// <summary>The leaf</summary>
        [Link]
        Leaf1 Leaf = null;

        /// <summary>The summary</summary>
        [Link]
        ISummary Summary = null;

        /// <summary>The dm supply</summary>
         public double DMSupply;

         /// <summary>Gets the DLT_DM.</summary>
         /// <value>The DLT_DM.</value>
         public double dlt_dm { get { return DMSupply; } } // needed by GRAZ

         /// <summary>Gets the ratio root plant.</summary>
         /// <value>The ratio root plant.</value>
        public virtual double RatioRootPlant { get { return 0.0; } }

        /// <summary>Partitions the dm.</summary>
        /// <param name="Organs">The organs.</param>
        /// <exception cref="System.Exception">Unknown Partition Rule  + PartitionRules[i]</exception>
        internal void PartitionDM(List<Organ1> Organs)
        {
            // Get all DM supply terms.
            DMSupply = 0;
            foreach (Organ1 Organ in Organs)
                DMSupply += Organ.DMSupply;

            // Tell each organ to do it's DM Demand
            foreach (Organ1 Organ in Organs)
                Organ.DoDMDemand(DMSupply);

            foreach (Organ1 Organ in Organs)
                Organ.Growth.Clear();

            double dm_remaining = DMSupply;
            double dlt_dm_green_tot = 0.0;

            for (int i = 0; i != PartitionOrgans.Length; i++)
            {
                Organ1 Organ = FindOrgan(PartitionOrgans[i], Organs);

                if (PartitionRules[i] == "magic")
                    Organ.GiveDmGreen(RatioRootShoot.Value * DMSupply);
                else if (PartitionRules[i] == "seasonal")                  // (PFR)
                {
                    double uptake = RatioRootPlant * dm_remaining;
                    Organ.GiveDmGreen(uptake);                                // (PFR)
                    dm_remaining = dm_remaining - uptake;                    // Here total RUE is used so remaining discounts root uptake(PFR)
                    dlt_dm_green_tot = dlt_dm_green_tot + uptake;
                }

                else
                {
                    double uptake;
                    if (PartitionRules[i] == "demand")
                        uptake = Math.Min(Organ.DMGreenDemand, dm_remaining);
                    else if (PartitionRules[i] == "frac")
                        uptake = Math.Min(FracDMRemainingInPart(Organ.Name) * dm_remaining, Organ.DMGreenDemand);
                    else if (PartitionRules[i] == "remainder")
                        uptake = dm_remaining;
                    else
                        throw new Exception("Unknown Partition Rule " + PartitionRules[i]);

                    Organ.GiveDmGreen(uptake);
                    dm_remaining = dm_remaining - uptake;
                    dlt_dm_green_tot = dlt_dm_green_tot + uptake;
                }
            }


            if (!MathUtilities.FloatsAreEqual(dlt_dm_green_tot, DMSupply, 1.0E-4f))
            {
                string msg = "dlt_dm_green_tot mass balance is off: "
                             + dlt_dm_green_tot.ToString("f6")
                             + " vs "
                             + DMSupply.ToString("f6");
                Summary.WriteWarning(this, msg);
            }

            Util.Debug("Arbitrator.DMSupply=%f", DMSupply);
            Util.Debug("Arbitrator.dlt_dm_green_tot=%f", dlt_dm_green_tot);
        }

        /// <summary>Retranslocates the dm.</summary>
        /// <param name="Organs">The organs.</param>
        internal void RetranslocateDM(List<Organ1> Organs)
        {
            double dlt_dm_retrans_part;                    // carbohydrate removed from part (g/m^2)
            double dm_part_avail;                          // carbohydrate avail from part(g/m^2)
            double dm_retranslocate = 0.0;

            // now translocate carbohydrate between plant components
            // this is different for each stage

            // plant.All().dlt_dm_green_retrans_hack( 0.0 );   ????????

            double demand_differential_begin = 0.0;
            foreach (Organ1 Organ in Organs)
                demand_differential_begin += Organ.DMDemandDifferential;
            double demand_differential = demand_differential_begin;

            // get available carbohydrate from supply pools
            foreach (Organ1 Organ in Organs)
            {
                dm_part_avail = Organ.DMRetransSupply;

                dlt_dm_retrans_part = Math.Min(demand_differential, dm_part_avail);

                //assign and accumulate
                Organ.Retranslocation.NonStructuralWt = -dlt_dm_retrans_part;
                dm_retranslocate += -dlt_dm_retrans_part;

                demand_differential = demand_differential - dlt_dm_retrans_part;
            }

            double dlt_dm_retrans_to_fruit = -dm_retranslocate;

            double dm_demand_differential = 0;
            foreach (Organ1 Organ in Organs)
                dm_demand_differential += Organ.DMDemandDifferential;
            double dlt_dm_green_retrans = dlt_dm_retrans_to_fruit * MathUtilities.Divide(dm_demand_differential, demand_differential_begin, 0.0);

            // get available carbohydrate from local supply pools
            double Retranslocation = 0.0;

            foreach (Organ1 Organ in Organs)
                Retranslocation += Organ.Retranslocation.Wt;

            double dlt_dm_green_retrans_tot = dlt_dm_green_retrans; // +-Retranslocation;  

            // now distribute the assimilate to plant parts
            double FinalRetranslocation = 0.0;
            foreach (Organ1 Organ in Organs)
            {
                Organ.DoDmRetranslocate(dlt_dm_green_retrans_tot, dm_demand_differential);
                if (Organ.Retranslocation.Wt > 0)  // movement of assimilate  into organ
                    FinalRetranslocation += Organ.Retranslocation.Wt;
            }

            // do mass balance check
            if (!MathUtilities.FloatsAreEqual(FinalRetranslocation, dlt_dm_green_retrans))
            {
                string msg = "dlt_dm_green_retrans_tot mass balance is off: "
                             + FinalRetranslocation.ToString("f6")
                             + " vs "
                             + dlt_dm_green_retrans.ToString("f6");
                Summary.WriteWarning(this, msg);
            }
            Util.Debug("Arbitrator.FinalRetranslocation=%f", -FinalRetranslocation);
        }

        /// <summary>Fracs the dm remaining in part.</summary>
        /// <param name="OrganName">Name of the organ.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">In arbitrator, cannot find FracDMRemainingIn + OrganName</exception>
        private double FracDMRemainingInPart(string OrganName)
        {
            IFunction F = Apsim.Find(this, "FracDMRemainingIn" + OrganName) as IFunction;
            if (F == null)
                throw new Exception("In arbitrator, cannot find FracDMRemainingIn" + OrganName);
            return F.Value;
        }

        /// <summary>Finds the organ.</summary>
        /// <param name="OrganName">Name of the organ.</param>
        /// <param name="Organs">The organs.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">In arbitrator, cannot find an organ with name:  + OrganName</exception>
        private Organ1 FindOrgan(string OrganName, List<Organ1> Organs)
        {
            foreach (Organ1 Organ in Organs)
                if (Organ.Name.Equals(OrganName, StringComparison.CurrentCultureIgnoreCase))
                    return Organ;
            throw new Exception("In arbitrator, cannot find an organ with name: " + OrganName);
        }

        /// <summary>Does the n retranslocate.</summary>
        /// <param name="GrainNDemand">The grain n demand.</param>
        /// <param name="Tops">The tops.</param>
        internal void DoNRetranslocate(double GrainNDemand, List<Organ1> Tops)
        {
            // Calculate the nitrogen retranslocation from the various plant parts
            // to the grain.

            //! available N does not include roots or grain
            //! this should not presume roots and grain are 0.
            // grain N potential (supply)

            double availableRetranslocateN = 0;
            foreach (Organ1 Organ in Tops)
                availableRetranslocateN += Organ.AvailableRetranslocateN;

            foreach (Organ1 Organ in Tops)
                Organ.DoNRetranslocate(availableRetranslocateN, GrainNDemand);     //FIXME - divy up?
        }

        /// <summary>Does the n partition.</summary>
        /// <param name="n_fix_pot">The n_fix_pot.</param>
        /// <param name="Organs">The organs.</param>
        /// <returns></returns>
        internal double DoNPartition(double n_fix_pot, List<Organ1> Organs)
        {
            double NDemandTotal = 0;
            double NCapacityTotal = 0;
            double nUptakeSum = 0;
            foreach (Organ1 Organ in Organs)
            {
                NDemandTotal += Organ.NDemand;
                NCapacityTotal += Organ.NCapacity;
                nUptakeSum += Organ.NUptake;
            }
            double n_excess = nUptakeSum - NDemandTotal;
            n_excess = MathUtilities.Constrain(n_excess, 0.0, double.MaxValue);

            // find the proportion of uptake to be distributed to
            // each plant part and distribute it.
            foreach (Organ1 Organ in Organs)
            {
                if (n_excess > 0.0)
                {
                    double plant_part_fract = MathUtilities.Divide(Organ.NCapacity, NCapacityTotal, 0.0);
                    Organ.DoNPartition(Organ.NDemand + n_excess * plant_part_fract);
                }
                else
                {
                    double plant_part_fract = MathUtilities.Divide(Organ.NDemand, NDemandTotal, 0.0);
                    Organ.DoNPartition(nUptakeSum * plant_part_fract);
                }

            }


            // Check Mass Balance
            double GrowthNTotal = 0;
            foreach (Organ1 Organ in Organs)
                GrowthNTotal += Organ.Growth.N;

            if (!MathUtilities.FloatsAreEqual(GrowthNTotal - nUptakeSum, 0.0))
            {
                string msg = "Crop dlt_n_green mass balance is off: dlt_n_green_sum ="
                              + GrowthNTotal.ToString("f6")
                              + " vs n_uptake_sum ="
                              + nUptakeSum.ToString("f6");
                Summary.WriteWarning(this, msg);
            }
            Util.Debug("Arbitrator.nUptakeSum=%f", nUptakeSum);

            // Retranslocate N Fixed
            double NFixDemandTotal = MathUtilities.Constrain(NDemandTotal - nUptakeSum, 0.0, double.MaxValue); // total demand for N fixation (g/m^2)
            double NFixUptake = MathUtilities.Constrain(n_fix_pot, 0.0, NFixDemandTotal);

            double n_demand_differential = 0;
            foreach (Organ1 Organ in Organs)
                n_demand_differential += Organ.NDemandDifferential;

            // now distribute the n fixed to plant parts
            NFixUptake = NFixUptake * MathUtilities.Divide(n_demand_differential, NFixDemandTotal, 0.0);
            foreach (Organ1 Organ in Organs)
                Organ.DoNFixRetranslocate(NFixUptake, n_demand_differential);
            Util.Debug("Arbitrator.n_demand_differential=%f", n_demand_differential);
            Util.Debug("Arbitrator.NFixUptake=%f", NFixUptake);
            return NFixUptake;
        }

        /// <summary>Derives seneseced plant nitrogen (g N/m^2)</summary>
        /// <param name="Organs">The organs.</param>
        internal void doNSenescedRetranslocation(List<Organ1> Organs)
        {
            //! now get N to retranslocate out of senescing leaves
            foreach (Organ1 Organ in Organs)
                Organ.ZeroDltNSenescedTrans();

            double dlt_n_in_senescing_leaf = Leaf.Senescing.Wt * Leaf.Live.NConc;

            double n_demand_tot = 0;
            foreach (Organ1 Organ in Organs)
                n_demand_tot += Organ.NDemand;

            double navail = dlt_n_in_senescing_leaf - Leaf.Senescing.N;
            navail = MathUtilities.Constrain(navail, 0.0, n_demand_tot);

            foreach (Organ1 Organ in Organs)
                Organ.DoNSenescedRetranslocation(navail, n_demand_tot);
        }
    }
}
