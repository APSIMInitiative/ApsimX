using APSIM.Numerics;
using Docker.DotNet.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A store for a particular type of animal food used to track details as multiple instances are mixed in the diet.
    /// </summary>
    [Serializable]
    public class FoodResourceStore
    {
        /// <summary>
        /// Store for the number of days in time step
        /// </summary>
        [JsonIgnore]
        public int NumberOfDaysInTimestep { get; private set; } = 1;
        /// <summary>
        /// Store quality and quantity details.
        /// </summary>
        [JsonIgnore]
        public FoodResourcePacket Details { get; private set; } = new();

        /// <summary>
        /// The graze food store pools included in this group
        /// </summary>
        public List<GrazeFoodStorePool> Pools { get; private set; }

        /// <summary>
        /// The proportion of the total biomass from each pool
        /// </summary>
        public double[] PoolProportions { get; set; }

        /// <summary>
        /// The proportion of the group that is considered green
        /// </summary>
        public double ProportionGreen { get; private set; }

        /// <summary>
        /// Total crude protein in the store.
        /// </summary>
        public double CrudeProtein { get; set; }
        
        /// <summary>
        /// Total rumen degradable crude protein in the store.
        /// </summary>
        public double DegradableCrudeProtein { get; set; }
        
        /// <summary>
        /// Total rumen undegradable crude protein in the store.
        /// </summary>
        public double UndegradableCrudeProtein { get { return CrudeProtein - DegradableCrudeProtein; } }

        /// <summary>
        /// Link to the feed type to handle pending and returned feed from quality based reductions
        /// </summary>
        public ResourceRequest AssociatedResourceRequest { get; set; }

        /// <summary>
        /// Constructor for a FoodResourceStore with local packet details.
        /// </summary>
        /// <param name="foodResourcePacket">The food resource packet to initialise the resource with values</param>
        public FoodResourceStore(FoodResourcePacket foodResourcePacket)
        {
            Details.SetPropertiesFromPacket(foodResourcePacket, foodResourcePacket.Amount);
            CrudeProtein = (Details.CrudeProteinPercent / 100.0) * Details.Amount;
            DegradableCrudeProtein = CrudeProtein * (Details.RumenDegradableProteinPercent / 100.0);
        }

        /// <summary>
        /// Constructor for a FoodResourceStore with local packet details and a specified amount.
        /// </summary>
        /// <param name="foodResourcePacket">The food resource packet to initialise the resource with values</param>
        /// <param name="amount">Amount to initialise store with</param>
        /// <param name="request">Associated resource request for transactions</param>
        /// <param name="daysInTimeStep">Number of days in time step</param>
        public FoodResourceStore(FoodResourcePacket foodResourcePacket, double amount, ResourceRequest request = null, int daysInTimeStep = 1)
        {
            Details.SetPropertiesFromPacket(foodResourcePacket, amount);
            CrudeProtein = (Details.CrudeProteinPercent / 100.0) * Details.Amount;
            DegradableCrudeProtein = CrudeProtein * (Details.RumenDegradableProteinPercent / 100.0);
            if (request is not null)
                AssociatedResourceRequest = request;
            NumberOfDaysInTimestep = daysInTimeStep;
        }

        /// <summary>
        /// Constructor for a FoodResourceStore to create a shallow copy from another FoodResourceStore and a specified
        /// ensuring the Details with amount is only for the new FoodResourceStore.
        /// </summary>
        /// <param name="foodResourceStore">
        /// The food resource store to initialise the resource with values and pasture pools
        /// </param>
        /// <param name="amount">Amount to initialise store with</param>
        public FoodResourceStore(FoodResourceStore foodResourceStore, double amount)
        {
            Details.SetPropertiesFromPacket(foodResourceStore.Details, amount);
            if (Details.Amount > 0)
            {
                CrudeProtein = (Details.CrudeProteinPercent / 100.0) * Details.Amount;
                DegradableCrudeProtein = CrudeProtein * (Details.RumenDegradableProteinPercent / 100.0);
            }

            NumberOfDaysInTimestep = foodResourceStore.NumberOfDaysInTimestep;
            Pools = foodResourceStore.Pools;
            AssociatedResourceRequest = foodResourceStore.AssociatedResourceRequest;
            PoolProportions = foodResourceStore.PoolProportions;
            ProportionGreen = foodResourceStore.ProportionGreen;
        }

        /// <summary>
        /// Constructor to create a FoodResourceStore for pasture pool group given pasure pools, green age and the
        /// number of time steps
        /// </summary>
        /// <param name="pools">Graze food store pools included</param>
        /// <param name="greenAge">The age (months) below which considered green</param>
        /// <param name="numberOfTimesteps">
        /// The number of timesteps to convert from daily rates to toal for intake to total
        /// </param>
        public FoodResourceStore(List<GrazeFoodStorePool> pools, int greenAge, int numberOfTimesteps)
        {
            NumberOfDaysInTimestep = numberOfTimesteps;
            Pools = pools;
            double totalInPools = pools.Sum(p => p.AmountAvailable);
            PoolProportions = pools.Select(p => totalInPools > 0 ? p.AmountAvailable / totalInPools : 0).ToArray();
            foreach (var pool in pools)
            {
                if (pool.AmountAvailable > 0)
                {
                    Details.AddAndMix(pool, pool.AmountAvailable);
                }
            }
            Details.ClearAmount();
            ProportionGreen = pools.Count == 0 ? 0 : pools.Where(p => p.Age <= greenAge).Sum(a => a.AmountAvailable) / totalInPools;
        }

        /// <summary>
        /// Adds a FoodResourcePacket to this store and adjusts pool qualities.
        /// </summary>
        /// <param name="packet">Packet to add.</param>
        /// <param name="specifyAmount">Specify the amount to add rather than obtain from the packet.Amount</param>
        public void Add(FoodResourcePacket packet, double? specifyAmount)
        {
            if (!specifyAmount.HasValue)
            {
                specifyAmount = packet.Amount;
            }

            Details.GrossEnergyContent = ((Details.GrossEnergyContent * Details.Amount) + (packet.GrossEnergyContent * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.DryMatterDigestibility = ((Details.DryMatterDigestibility * Details.Amount) + (packet.DryMatterDigestibility * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.FatPercent = ((Details.FatPercent * Details.Amount) + (packet.FatPercent * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.NitrogenPercent = ((Details.NitrogenPercent * Details.Amount) + (packet.NitrogenPercent * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.CrudeProteinPercent = ((Details.CrudeProteinPercent * Details.Amount) + (packet.CrudeProteinPercent * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.MetabolisableEnergyContent = ((Details.MetabolisableEnergyContent * Details.Amount) + (packet.MEContent * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.AcidDetergentInsolubleProtein = ((Details.AcidDetergentInsolubleProtein * Details.Amount) + (packet.AcidDetergentInsolubleProtein * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.RumenDegradableProteinPercent = ((Details.RumenDegradableProteinPercent * Details.Amount) + (packet.RumenDegradableProteinPercent * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);
            Details.GutFill = ((Details.GutFill * Details.Amount) + (packet.GutFill * specifyAmount.Value)) / (Details.Amount + specifyAmount.Value);

            Details.AddAmount(specifyAmount.Value);

            CrudeProtein += packet.CrudeProtein;
            DegradableCrudeProtein += packet.DegradableProtein;
        }

        /// <summary>
        /// Simple add to store the total amount given the daily amount required.
        /// </summary>
        /// <param name="dailyAmount">The daily amount to record</param>
        public void Add(double dailyAmount)
        {
            if (dailyAmount <= 0)
                return;

            dailyAmount *= NumberOfDaysInTimestep;
            Details.AddAmount(dailyAmount);
            CrudeProtein += (Details.CrudeProteinPercent / 100.0) * dailyAmount;
            DegradableCrudeProtein += (Details.CrudeProteinPercent / 100.0) * dailyAmount * (Details.RumenDegradableProteinPercent / 100.0);
        }

        /// <summary>
        /// Reduce the store by specified amount as required by indake reduction due to quality
        /// </summary>
        /// <param name="dailyAmount">The daily amount to remove</param>
        public void ReturnPending(double dailyAmount)
        {
            if (dailyAmount <= 0)
                return;

            // do we need to modify the values in the foodStore.Details?
            dailyAmount = Details.ReduceAmount(dailyAmount);

            // reduce pending in graze food store
            // reduce pending in associated pools by amount and poolProportions[]
            AssociatedResourceRequest?.Resource.ReducePending(AssociatedResourceRequest, dailyAmount);

            if (Details.Amount == 0)
            {
                CrudeProtein = 0;
                DegradableCrudeProtein = 0;
                return;
            }
            CrudeProtein -= (Details.CrudeProteinPercent/100.0) * dailyAmount;
            DegradableCrudeProtein = CrudeProtein * (Details.RumenDegradableProteinPercent / 100.0);
        }

        /// <summary>
        /// Reduce from the pending take by a proportion
        /// </summary>
        /// <param name="proportion">the proportion to reduce by</param>
        /// <param name="reducePending">Whether to reduce the pending take from pools as well (default true)</param>

        public double ReduceByProportion(double proportion, bool reducePending = true)
        {
            proportion = Math.Min(1.0, Math.Max(proportion, 0));
            if (proportion <= 0)
                return 0;

            double amountToReduce = Details.Amount * proportion;
            ReturnPending(Details.Amount * proportion);
            return amountToReduce;
        }

        /// <summary>
        /// Reduce the rumen degradable protein by a proportion provided.
        /// </summary>
        /// <param name="factor">The reduction factor.</param>
        public void ReduceDegradableProtein(double factor)
        {
            DegradableCrudeProtein *= factor;
            CrudeProtein *= factor;
        }

        /// <summary>
        /// Digestibility Undegradable Protein.
        /// </summary>
        public double DUDP
        {
            get
            {
                return Details.TypeOfFeed switch
                {
                    FeedType.HaySilage or 
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical => Math.Max(0.05, Math.Min(5.5 * Details.CrudeProteinPercent - 0.178, 0.85)),
                    FeedType.Concentrate => 0.9 * (1 - (MathUtilities.IsGreaterThan(Details.UndegradableCrudeProteinPercent, 0)?Details.AcidDetergentInsolubleProtein / (Details.UndegradableCrudeProteinPercent / 100.0) : 0)),
                    _ => 0,
                };
            }
        }

        /// <summary>
        /// Metabolisable energy.
        /// </summary>
        public double ME 
        { 
            get 
            { 
                if (MathUtilities.IsLessThanOrEqual(Details.Amount, 0.0))
                {
                    return 0;
                }

                return Details.MEContent * Details.Amount; 
            } 
        }

        /// <summary>
        /// Fermentable metabolisable energy.
        /// </summary>
        public double FME 
        { 
            get 
            {
                // Freer assumes only concentrates account for fat as all other feeds considered low fat
                // For consistency we apply both the crude protein and fat given they are provided with any feed type.
                return ME - (23.6 * UndegradableCrudeProtein) - (39.3 * (Details.FatPercent / 100) * Details.Amount);
            }
        }

        /// <summary>
        /// Proportion of this feed that is legume required for grazing APSIM forages
        /// </summary>
        public double ProportionLegume { get; set; } = 0;

        /// <summary>
        /// Reset running stores.
        /// </summary>
        public void Reset()
        {
            CrudeProtein = 0;
            DegradableCrudeProtein = 0;
            Details.Reset();
        }
    }
}
