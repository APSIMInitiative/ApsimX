using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Models.Core;
using Models.PMF.Functions;


namespace Models.PMF.OldPlant
{
    class GenericArbitratorXY: Model
    {
        public string[] PartitionOrgans = null;

        public string[] PartitionRules = null;

        public Function RatioRootShoot { get; set; }

        [Link]
        Leaf1 Leaf = null;

        [Link]
        ISummary Summary = null;

         public double DMSupply;

         public double dlt_dm { get { return DMSupply; } } // needed by GRAZ

        public virtual double RatioRootPlant { get { return 0.0; } }

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
                    Organ.GiveDmGreen(RatioRootShoot.FunctionValue * DMSupply);
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


            if (!Utility.Math.FloatsAreEqual(dlt_dm_green_tot, DMSupply, 1.0E-4f))
            {
                string msg = "dlt_dm_green_tot mass balance is off: "
                             + dlt_dm_green_tot.ToString("f6")
                             + " vs "
                             + DMSupply.ToString("f6");
                Summary.WriteMessage(msg);
            }

            Util.Debug("Arbitrator.DMSupply=%f", DMSupply);
            Util.Debug("Arbitrator.dlt_dm_green_tot=%f", dlt_dm_green_tot);
        }

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
            double dlt_dm_green_retrans = dlt_dm_retrans_to_fruit * Utility.Math.Divide(dm_demand_differential, demand_differential_begin, 0.0);

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
            if (!Utility.Math.FloatsAreEqual(FinalRetranslocation, dlt_dm_green_retrans))
            {
                string msg = "dlt_dm_green_retrans_tot mass balance is off: "
                             + FinalRetranslocation.ToString("f6")
                             + " vs "
                             + dlt_dm_green_retrans.ToString("f6");
                Summary.WriteMessage(msg);
            }
            Util.Debug("Arbitrator.FinalRetranslocation=%f", -FinalRetranslocation);
        }

        private double FracDMRemainingInPart(string OrganName)
        {
            Function F = this.Find("FracDMRemainingIn" + OrganName) as Function;
            if (F == null)
                throw new Exception("In arbitrator, cannot find FracDMRemainingIn" + OrganName);
            return F.FunctionValue;
        }

        private Organ1 FindOrgan(string OrganName, List<Organ1> Organs)
        {
            foreach (Organ1 Organ in Organs)
                if (Organ.Name.Equals(OrganName, StringComparison.CurrentCultureIgnoreCase))
                    return Organ;
            throw new Exception("In arbitrator, cannot find an organ with name: " + OrganName);
        }

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
            n_excess = Utility.Math.Constrain(n_excess, 0.0, double.MaxValue);

            // find the proportion of uptake to be distributed to
            // each plant part and distribute it.
            foreach (Organ1 Organ in Organs)
            {
                if (n_excess > 0.0)
                {
                    double plant_part_fract = Utility.Math.Divide(Organ.NCapacity, NCapacityTotal, 0.0);
                    Organ.DoNPartition(Organ.NDemand + n_excess * plant_part_fract);
                }
                else
                {
                    double plant_part_fract = Utility.Math.Divide(Organ.NDemand, NDemandTotal, 0.0);
                    Organ.DoNPartition(nUptakeSum * plant_part_fract);
                }

            }


            // Check Mass Balance
            double GrowthNTotal = 0;
            foreach (Organ1 Organ in Organs)
                GrowthNTotal += Organ.Growth.N;

            if (!Utility.Math.FloatsAreEqual(GrowthNTotal - nUptakeSum, 0.0))
            {
                string msg = "Crop dlt_n_green mass balance is off: dlt_n_green_sum ="
                              + GrowthNTotal.ToString("f6")
                              + " vs n_uptake_sum ="
                              + nUptakeSum.ToString("f6");
                Summary.WriteMessage(msg);
            }
            Util.Debug("Arbitrator.nUptakeSum=%f", nUptakeSum);

            // Retranslocate N Fixed
            double NFixDemandTotal = Utility.Math.Constrain(NDemandTotal - nUptakeSum, 0.0, double.MaxValue); // total demand for N fixation (g/m^2)
            double NFixUptake = Utility.Math.Constrain(n_fix_pot, 0.0, NFixDemandTotal);

            double n_demand_differential = 0;
            foreach (Organ1 Organ in Organs)
                n_demand_differential += Organ.NDemandDifferential;

            // now distribute the n fixed to plant parts
            NFixUptake = NFixUptake * Utility.Math.Divide(n_demand_differential, NFixDemandTotal, 0.0);
            foreach (Organ1 Organ in Organs)
                Organ.DoNFixRetranslocate(NFixUptake, n_demand_differential);
            Util.Debug("Arbitrator.n_demand_differential=%f", n_demand_differential);
            Util.Debug("Arbitrator.NFixUptake=%f", NFixUptake);
            return NFixUptake;
        }

        /// <summary>
        /// Derives seneseced plant nitrogen (g N/m^2)
        /// </summary>
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
            navail = Utility.Math.Constrain(navail, 0.0, n_demand_tot);

            foreach (Organ1 Organ in Organs)
                Organ.DoNSenescedRetranslocation(navail, n_demand_tot);
        }
    }
}
