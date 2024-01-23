using APSIM.Shared.Utilities;
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
        public void AddFeed(FoodResourcePacket packet)
        {
            if (packet.Amount > 0)
            {
                if (!feedTypeStoreDict.TryGetValue(packet.TypeOfFeed, out FoodResourceStore frs))
                {
                    frs = new FoodResourceStore();
                    feedTypeStoreDict[packet.TypeOfFeed] = frs;
                }
                frs.Add(packet);

                if (packet.TypeOfFeed == FeedType.Milk)
                    MilkDaily.Actual += packet.Amount;
                else
                    SolidsDaily.Actual += packet.Amount;
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
                return feedTypeStoreDict.Sum(a => a.Value.Details.MEContent * a.Value.Details.Amount);
            }
        }

        /// <summary>
        /// Metabolisable energy from Milk intake.
        /// </summary>
        public double MilkME
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Key == FeedType.Milk).Sum(a => a.Value.Details.MEContent * a.Value.Details.Amount);
            }
        }

        /// <summary>
        /// Metabolisable energy from solids (non-milk) intake.
        /// </summary>
        public double SolidsME
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.Details.MEContent * a.Value.Details.Amount);
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
                    return feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.Details.MEContent * a.Value.Details.Amount) / SolidIntake;
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
        /// Adjust intake by a reduction factor.
        /// </summary>
        /// <param name="reductionFactor">factor (0-1) to adjust by.</param>
        /// <returns>Boolen indicating whether any adjustment was made.</returns>
        public bool AdjustIntakeByRumenProteinRequired(double reductionFactor)
        {
            if (reductionFactor >= 1) return false;

            SolidsDaily.Actual = 0;

            // reduce all solid intake amounts
            foreach (var item in feedTypeStoreDict.Where(a => a.Key != FeedType.Milk))
            {
                item.Value.Details.Amount *= reductionFactor;
                SolidsDaily.Actual += item.Value.Details.Amount;
            }
            return true;
        }

        /// <summary>
        /// Rumen Degradable Protein.
        /// </summary>
        public double RDP
        {
            get
            {
                double sumAmount = 0;
                foreach (var item in feedTypeStoreDict)
                {
                    sumAmount += item.Value.DegradableCrudeProtein;
                }
                return sumAmount;
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
            RDPRequired = rdpRequired;
            dpls = 0;
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
            // add mircobial crude protein from RDP (CA7 0.6)
            dpls += (0.6 * RDPRequired); 
        }

        /// <summary>
        /// Rumen Digestible Protein Required
        /// </summary>
        public double RDPRequired { get; set; }

        /// <summary>
        /// Digestible Protein Leaving the Sotmach.
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

        ///// <summary>
        ///// Adjust all amounts to change rate
        ///// </summary>
        ///// <param name="factor"></param>
        //public void AdjustAmounts(double factor)
        //{
        //    foreach (var item in feedTypeStoreDict)
        //    {
        //        item.Value.Details.Amount *= factor;
        //    }
        //    SolidsDaily.AdjustAmounts(factor);
        //    MilkDaily.AdjustAmounts(factor);
        //}
    }
}
