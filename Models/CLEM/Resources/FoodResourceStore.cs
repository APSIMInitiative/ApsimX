using APSIM.Shared.Utilities;
using Docker.DotNet.Models;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
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
        /// Store quality and quantity details.
        /// </summary>
        [JsonIgnore]
        public FoodResourcePacket Details { get; private set; } = new();

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
        /// Constructor for a FoodResourceStore.
        /// </summary>
        /// <param name="foodResourcePacket">The food resource packet to initialise the resource with values</param>
        public FoodResourceStore(FoodResourcePacket foodResourcePacket)
        {
            Details.TypeOfFeed = foodResourcePacket.TypeOfFeed;
        }

        /// <summary>
        /// Adds a FoodResourcePacket to this store and adjusts pool qualtities.
        /// </summary>
        /// <param name="packet">Packet to add.</param>
        public void Add(FoodResourcePacket packet)
        {
            Details.GrossEnergyContent = ((Details.GrossEnergyContent * Details.Amount) + (packet.GrossEnergyContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.DryMatterDigestibility = ((Details.DryMatterDigestibility * Details.Amount) + (packet.DryMatterDigestibility * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.FatPercent = ((Details.FatPercent * Details.Amount) + (packet.FatPercent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.NitrogenPercent = ((Details.NitrogenPercent * Details.Amount) + (packet.NitrogenPercent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.CrudeProteinPercent = ((Details.CrudeProteinPercent * Details.Amount) + (packet.CrudeProteinPercent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.MetabolisableEnergyContent = ((Details.MetabolisableEnergyContent * Details.Amount) + (packet.MEContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.AcidDetergentInsoluableProtein = ((Details.AcidDetergentInsoluableProtein * Details.Amount) + (packet.AcidDetergentInsoluableProtein * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.RumenDegradableProteinPercent = ((Details.RumenDegradableProteinPercent * Details.Amount) + (packet.RumenDegradableProteinPercent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.Amount += packet.Amount;

            CrudeProtein += packet.CrudeProtein;
            DegradableCrudeProtein += packet.DegradableProtein;
        }

        /// <summary>
        /// Reduce current amount supplied
        /// </summary>
        /// <param name="amount">the amount to add or subtract</param>
        public void ReduceAmount(double amount)
        {
            if (MathUtilities.IsGreaterThan(amount, Details.Amount))
            {
                Details.Amount = 0;
                CrudeProtein = 0;
                DegradableCrudeProtein = 0;
                return;
            }
            Details.Amount -= amount;
            CrudeProtein -= (Details.CrudeProteinPercent/100.0) * amount;
            DegradableCrudeProtein = CrudeProtein * (Details.RumenDegradableProteinPercent / 100.0);
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
        /// Digestibility Undegradable Pprotein.
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
                    FeedType.Concentrate => 0.9 * (1 - (MathUtilities.IsGreaterThan(Details.UndegradableCrudeProteinPercent, 0)?Details.AcidDetergentInsoluableProtein / (Details.UndegradableCrudeProteinPercent / 100.0) : 0)),
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
                    return 0;
                return Details.MEContent * Details.Amount; 
            } 
        }

        /// <summary>
        /// Fermentable metabolisable energy.
        /// </summary>
        public double FME { get { return 0.7 * ME; } } 

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
