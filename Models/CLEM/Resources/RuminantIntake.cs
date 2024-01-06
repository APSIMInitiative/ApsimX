using APSIM.Shared.Utilities;
using Docker.DotNet.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Manages tracking of Ruminant intake quality and quantity.
    /// </summary>
    [Serializable]
    public  class RuminantIntake
    {
        private Dictionary<FeedType, FoodResourceStore> feedTypeStoreDict = new();
        private double dpls = 0;

        /// <summary>
        /// The potential and actual milk intake of the individual.
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer Milk { get; private set; } = new ExpectedActualContainer();

        /// <summary>
        /// The potential and actual feed intake of the individual.
        /// </summary>
        [JsonIgnore]
        public ExpectedActualContainer Feed { get; private set; } = new ExpectedActualContainer();

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
                    Milk.Actual += packet.Amount;
                else
                    Feed.Actual += packet.Amount;
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
                if (Milk.Actual + Feed.Actual <= 0)
                    return 0;
                return Milk.Actual / (Milk.Actual + Feed.Actual);
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
        /// Reset all intake values.
        /// </summary>
        public void Reset()
        {
            Feed.Reset();
            Milk.Reset();
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
                foreach (var item in feedTypeStoreDict)
                {
                    if(item.Key != FeedType.Milk)
                    {
                        sumDMD += item.Value.Details.DryMatterDigestibility * item.Value.Details.Amount;
                        sumAmount += item.Value.Details.Amount;
                    }
                }
                if (sumAmount <= 0)
                    return 0;

                return sumDMD/sumAmount;
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

            Feed.Actual = 0;

            // reduce all solid intake amounts
            foreach (var item in feedTypeStoreDict)
            {
                if (item.Key != FeedType.Milk)
                {
                    item.Value.Details.Amount *= reductionFactor;
                    Feed.Actual += item.Value.Details.Amount;
                }
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
        /// Method to calculate the digestible protein leaving the stomach based on RDP required.
        /// </summary>
        /// <param name="rdpRequired">The Rumen Digestible Protein requirement</param>
        /// <returns></returns>
        public void CalculateDigestibleProteinLeavingStomach(double rdpRequired)
        {
            RDPRequired = rdpRequired;
            dpls = 0;
            foreach (var item in feedTypeStoreDict)
            {
                if(item.Key != FeedType.Milk)
                {
                    dpls += 0.92 * item.Value.CrudeProtein;
                }
                else
                {
                    dpls += item.Value.DUDP * item.Value.UndegradableCrudeProtein;
                }
            }
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

        /// <summary>
        /// Reduce the rumen degradable protein by a proportion provided.
        /// </summary>
        /// <param name="feedType">The type of feed this applies to.</param>
        /// <param name="factor">The reduction factor.</param>
        public void ReduceDegradableProtein(FeedType feedType, double factor)
        {
            if (feedTypeStoreDict.TryGetValue(feedType, out FoodResourceStore frs))
                frs.ReduceDegradableProtein(factor);
        }
    }
}
