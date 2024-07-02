using APSIM.Shared.Utilities;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Manages tracking of Ruminant intake quality and quantity.
    /// </summary>
    [Serializable]
    public class RuminantIntake
    {
        private Dictionary<FeedType, FoodResourceStore> feedTypeStoreDict = new();
        private double dpls = 0;

        /// <summary>
        /// The potential and actual milk intake of the individual.
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer MilkDaily { get; set; } = new ExpectedActualContainer();

        /// <summary>
        /// The potential and actual solids intake of the individual.
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer SolidsDaily { get; set; } = new ExpectedActualContainer();

        /// <summary>
        /// A function to add intake and track rumen totals of N, CP, DMD, Fat and energy.
        /// </summary>
        /// <param name="packet">Feed packet containing intake information kg, %N, DMD.</param>
        /// <param name="bypassPotIntakeLimits">A switch to force animals to eat the amount provided.</param>
        /// <returns>The excess feed to the individual</returns>
        public double AddFeed(FoodResourcePacket packet, bool bypassPotIntakeLimits = false)
        {
            double excess = 0;
            if (packet.Amount > 0)
            {
                if (!bypassPotIntakeLimits && packet.TypeOfFeed != FeedType.Milk)
                {
                    // limit feed to animals maximum intake.
                    excess = StdMath.DIM(packet.Amount, SolidsDaily.Required);
                    packet.Amount -= excess;
                }

                if (!feedTypeStoreDict.TryGetValue(packet.TypeOfFeed, out FoodResourceStore frs))
                {
                    frs = new FoodResourceStore(packet);
                    feedTypeStoreDict[packet.TypeOfFeed] = frs;
                }
                frs.Add(packet);

                if (packet.TypeOfFeed == FeedType.Milk)
                    MilkDaily.Received += packet.Amount;
                else
                    SolidsDaily.Received += packet.Amount;
            }
            return excess;
        }

        /// <summary>
        /// Adjust intake consumed based on feed quality
        /// </summary>
        public void AdjustIntakeBasedOnFeedQuality(bool islactating, Ruminant ind)
        {
            if (ind.Parameters.Grow24_CI.IgnoreFeedQualityIntakeAdustment)
                return;

            // ========================================================================================================================
            // Freer et al. (2012) The GRAZPLAN animal biology model for sheep and cattle and the GrazFeed decision support tool
            // Equations 14-21
            // ========================================================================================================================
            double sumFs = 0;
            double iReduction = 0;

            //for each food type
            foreach (var item in feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).OrderByDescending(a => a.Value.Details.DryMatterDigestibility))
            {
                // RQ is relative quality
                // FS relative availability of the feed
                double FS = 0;
                double RS = 0;
                switch (item.Key)
                {
                    case FeedType.Concentrate:
                    case FeedType.HaySilage:
                        double RQ = Math.Min(1.0, 1 - 1.7 * (0.8 - (item.Value.Details.DryMatterDigestibility/100.0)));
                        double offered_adj = (item.Value.Details.Amount/SolidsDaily.Expected)/RQ;
                        double unsatisfied_adj = Math.Max(0, 1-sumFs);
                        double quality_adj = (islactating? ind.Parameters.Grow24_CI.QualityIntakeSubsititutionFactorLactating_CR20:ind.Parameters.Grow24_CI.QualityIntakeSubsititutionFactorNonLactating_CR11)/item.Value.Details.MEContent;
                        FS =  Math.Min(offered_adj, Math.Min(unsatisfied_adj, quality_adj));
                        RS = FS * RQ;
                        break;
                    case FeedType.PastureTemperate:
                    case FeedType.PastureTropical:
//                        RQ = 1.0-1.7*StdMath.DIM((0.8-(1 - item.Value.Details.ProportionLegumeInPasture)), item.Value.Details.DryMatterDigestibility/100.0);
                        // CLEM assumes you can only graze one pasture type in a time step. Technically we should be able to add two pastures of the same type during the time-step but not mixed tropical and temperate pastures.
                        double ZF = 1.0;
                        if (ind.Weight.RelativeSize < 0.5)
                        {
                            ZF = 1 + 0.5 - ind.Weight.RelativeSize;
                        }
//                        double RR = 1.0 - Math.Exp(-1 * 1.35 * (0.78 * 10e-3) * ZF * item.Value.Details.OverallPastureBiomass);
//                        double RT = 1 + (0.6 * Math.Exp(-1 * 1.35 * (0.74 * 10e-3) * ZF * item.Value.Details.OverallPastureBiomass));
                        unsatisfied_adj = Math.Max(0, 1 - sumFs);
//                        FS = unsatisfied_adj * RR * RT;
//                        RS = FS * RQ * (1 + (0.17 * item.Value.Details.ProportionLegumeInPasture * Math.Pow(sumFs,2)));
                        break;
                    default:
                        break;
                }
                iReduction = StdMath.DIM(item.Value.Details.Amount, RS * SolidsDaily.Expected);
                item.Value.ReduceAmount(iReduction);
                SolidsDaily.Unneeded += iReduction;
                sumFs += FS;
            }
        }

        /// <summary>
        /// Provides the amount of solid intake in diet.
        /// </summary>
        public double SolidIntake 
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.Details.Amount);
            }
        }

        /// <summary>
        /// Provides the proportion of milk in the diet.
        /// </summary>
        public double ProportionMilk
        {
            get
            {
                if (MilkDaily.Actual + SolidsDaily.Actual <= 0)
                    return 0;
                return MilkDaily.Actual / (MilkDaily.Actual + SolidsDaily.Actual);
            }
        }

        /// <summary>
        /// Determines the proportion of the potential intake actually achieved.
        /// </summary>
        public double ProportionOfPotentialIntakeObtained
        {
            get
            {
                if (SolidsDaily.Expected + MilkDaily.Expected <= 0)
                    return 0;
                return (SolidsDaily.Actual + MilkDaily.Actual) / (SolidsDaily.Expected + MilkDaily.Expected);
            }
        }

        /// <summary>
        /// Get the details of a food resource store identified by feed type .
        /// </summary>
        /// <param name="feedType">Feed type of the required store.</param>
        /// <returns>The food resource store or null if not found.</returns>
        public FoodResourcePacket GetStoreDetails(FeedType feedType)
        {
            return GetStore(feedType)?.Details;
        }

        /// <summary>
        /// Get the food resource store identified by feed type.
        /// </summary>
        /// <param name="feedType">Feed type of the required store.</param>
        /// <returns>The food resource packet containing feed store details.</returns>
        public FoodResourceStore GetStore(FeedType feedType)
        {
            if (feedTypeStoreDict.TryGetValue(feedType, out FoodResourceStore frs))
                return frs;
            return null;
        }

        /// <summary>
        /// Get all food stores available
        /// </summary>
        public Dictionary<FeedType, FoodResourceStore> GetAllStores { get { return feedTypeStoreDict; } }

        /// <summary>
        /// Reset all intake values.
        /// </summary>
        public void Reset()
        {
            // ToDo: consider clearing dictionary. Is there any benefit keeping old stores for loops?
            // clearing each time-step (for each individual e.g. 30,0000 head)
            // * would remove milk store after weaning (could add special clear)
            // * clearing would allow smallest number of feed types each step and variable feeding rules to be efficient
            // * reduce looping for kg and FMEI (across number of types fed in time-step)
            // * multiple garbage collection required for all ruminants every time-step (expensive, memory and cpu)
            // * ? how much are time-step feed types really likely to vary?
            // * just how many feed types are expected in most runs? I expect only few.
            // leaving them only means they don't have be be recreated each time fed (time-step), but 
            // * check if exists (happens regardless, may be faster with fewer entries), dictionary should be optimised
            // * saves .. if not present create object and add to dictionary
            // * saves loops over types and resetting
            // Outcome
            // * I think leaving them might be most optimised
            // * ensure they are cleared and removed on disposal of individual.

            foreach (var item in feedTypeStoreDict)
            {
                item.Value.Reset();
            }
        }

        /// <summary>
        /// Total degradable protein (kg/timestep).
        /// </summary>
        public double DegradableProtein 
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.DegradableCrudeProtein); 
            } 
        }

        /// <summary>
        /// Total crude protein (kg/timestep).
        /// </summary>
        public double CrudeProtein
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.CrudeProtein);
            }
        }

        /// <summary>
        /// Metabolisable energy from intake.
        /// </summary>
        public double ME
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.ME);
            }
        }

        /// <summary>
        /// Metabolisable energy from Milk intake.
        /// </summary>
        public double MilkME
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Key == FeedType.Milk).Sum(a => a.Value.ME);
            }
        }

        /// <summary>
        /// Metabolisable energy from solids (non-milk) intake.
        /// </summary>
        public double SolidsME
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.ME);
            }
        }


        /// <summary>
        /// Metabolisable energy density from solids intake.
        /// </summary>
        public double MDSolid
        {
            get
            {
                if (MathUtilities.IsGreaterThan(SolidIntake, 0))
                {
                    return feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.ME) / SolidIntake;
                }
                return 0;
            }
        }

        /// <summary>
        /// Dry matter digestibility of solid (non-milk) intake.
        /// </summary>
        public double DMD
        {
            get
            {
                double sumDMD = 0;
                double sumAmount = 0;
                foreach (var item in feedTypeStoreDict.Where(a => a.Key != FeedType.Milk))
                {
                    sumDMD += item.Value.Details.DryMatterDigestibility * item.Value.Details.Amount;
                    sumAmount += item.Value.Details.Amount;
                }
                if (sumAmount <= 0)
                    return 0;

                return sumDMD/sumAmount;
            }
        }

        /// <summary>
        /// Calculate Nitrogen content of solid (non-milk) intake.
        /// </summary>
        public double NitrogenContent
        {
            get
            {
                var total = feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.Details.Amount);
                var totalN = feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.Details.NitrogenContent * a.Value.Details.Amount);
                if(total > 0)
                    return totalN / total;
                return 0;
            }
        }

        /// <summary>
        /// Rumen Degradable Protein.
        /// </summary>
        public double RDP
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.DegradableCrudeProtein);
            }
        }

        /// <summary>
        /// Method to calculate the digestible protein leaving the stomach (Metabolisable Protein) based on RDP required.
        /// </summary>
        /// <param name="rdpRequired">The Rumen Digestible Protein (RDP) requirement</param>
        /// <param name="milkProteinDigestibility">The milk protein digestibility of the breed</param>
        /// <returns></returns>
        public void CalculateDigestibleProteinLeavingStomach(double rdpRequired, double milkProteinDigestibility)
        {
            // DPLS is simply 0.6 of the minimum of DPP from intake and RDP required from rumen.
            dpls = 0;
            RDPRequired = Math.Min(RDP, rdpRequired); 
            dpls = (0.6 * RDPRequired); // microbe component.
            foreach (var item in feedTypeStoreDict)
            {
                if(item.Key == FeedType.Milk)
                {
                    dpls += milkProteinDigestibility * item.Value.CrudeProtein; // CA5 = 0.92
                }
                else
                {
                    dpls += item.Value.DUDP * item.Value.UndegradableCrudeProtein;
                }
            }
        }

        /// <summary>
        /// Rumen Digestible Protein Required
        /// </summary>
        public double RDPRequired { get; set; }

        /// <summary>
        /// Digestible Protein Leaving the Stomach.
        /// </summary>
        public double DPLS
        {
            get
            {
                return dpls;
            }
        }

        /// <summary>
        /// Rumen Undegradable Protein (RUP = UDP).
        /// </summary>
        public double UDP {
            get
            {
                return CrudeProtein - RDP;
            }
        }

        /// <summary>
        /// Indigestible undegradable protein.
        /// </summary>
        public double IndigestibleUDP
        {
            get
            {
                double sumAmount = 0;
                foreach (var item in feedTypeStoreDict)
                {
                    sumAmount += (1-item.Value.DUDP) * item.Value.UndegradableCrudeProtein;
                }
                return sumAmount;
            }
        }
    }
}
