using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Manages tracking of Ruminant intake quality and quantity
    /// </summary>
    public  class RuminantIntake
    {
        private Dictionary<FeedType, FoodResourceStore> feedTypeStoreDict = new Dictionary<FeedType, FoodResourceStore>();

        /// <summary>
        /// The potential and actual milk intake of the individual
        /// </summary>
        public ExpectedActualContainer Milk { get; private set; }

        /// <summary>
        /// The potential and actual feed intake of the individual
        /// </summary>
        public ExpectedActualContainer Feed { get; private set; }

        /// <summary>
        /// A function to add intake and track rumen totals of N, CP, DMD, Fat and energy
        /// </summary>
        /// <param name="packet">Feed packet containing intake information kg, %N, DMD</param>
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
        /// Provides the amount of solod intake in diet.
        /// </summary>
        public double SolidIntake 
        {
            get
            {
                return feedTypeStoreDict.Where(a => a.Key != FeedType.Milk).Sum(a => a.Value.Details.Amount);
            }
        }

        /// <summary>
        /// Get the details of a food resource store identified by feed type 
        /// </summary>
        /// <param name="feedType">Feed type of the required store</param>
        /// <returns>The food resource store or null if not found</returns>
        public FoodResourcePacket GetStoreDetails(FeedType feedType)
        {
            return GetStore(feedType)?.Details;
        }

        /// <summary>
        /// Get the food resource store identified by feed type 
        /// </summary>
        /// <param name="feedType">Feed type of the required store</param>
        /// <returns>The food resource packet containing feed store details</returns>
        public FoodResourceStore GetStore(FeedType feedType)
        {
            if (feedTypeStoreDict.TryGetValue(feedType, out FoodResourceStore frs))
                return frs;
            return null;
        }

        /// <summary>
        /// Reset all intake values
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
        /// Total degradable protein (kg/timestep)
        /// </summary>
        public double DegradableProtein 
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.DegradableCrudeProtein); 
            } 
        }

        /// <summary>
        /// Total crude protein (kg/timestep)
        /// </summary>
        public double CrudeProtein
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.CrudeProtein);
            }
        }

        /// <summary>
        /// Metabolisable energy from intake
        /// </summary>
        public double ME
        {
            get
            {
                return feedTypeStoreDict.Sum(a => a.Value.Details.EnergyContent * a.Value.Details.Amount);
            }
        }

        /// <summary>
        /// Dry matter digestibility of solid (non-milk) intake
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
        /// 
        /// </summary>
        /// <param name="reductionFactor"></param>
        /// <returns></returns>
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
        /// Reduce the rumen degradable protein by a proportion provided
        /// </summary>
        /// <param name="feedType">The type of feed this applies to</param>
        /// <param name="factor">The reduction factor</param>
        public void ReduceDegradableProtein(FeedType feedType, double factor)
        {
            if (feedTypeStoreDict.TryGetValue(feedType, out FoodResourceStore frs))
                frs.ReduceDegradableProtein(factor);
        }
    }
}
