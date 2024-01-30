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
        public FoodResourcePacket Details { get; set; } = new FoodResourcePacket();

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
        /// Adds a FoodResourcePacket to this store and adjusts pool qualtities.
        /// </summary>
        /// <param name="packet">Packet to add.</param>
        public void Add(FoodResourcePacket packet)
        {
            Details.GrossEnergyContent = ((Details.GrossEnergyContent * Details.Amount) + (packet.GrossEnergyContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.DryMatterDigestibility = ((Details.DryMatterDigestibility * Details.Amount) + (packet.DryMatterDigestibility * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.FatContent = ((Details.FatContent * Details.Amount) + (packet.FatContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.NitrogenContent = ((Details.NitrogenContent * Details.Amount) + (packet.NitrogenContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.CrudeProteinContent = ((Details.CrudeProteinContent * Details.Amount) + (packet.CrudeProteinContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.MetabolisableEnergyContent = ((Details.MetabolisableEnergyContent * Details.Amount) + (packet.MEContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.AcidDetergentInsoluableProtein = ((Details.AcidDetergentInsoluableProtein * Details.Amount) + (packet.AcidDetergentInsoluableProtein * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.RumenDegradableProteinContent = ((Details.RumenDegradableProteinContent * Details.Amount) + (packet.RumenDegradableProteinContent * packet.Amount)) / (Details.Amount + packet.Amount);

            Details.Amount += packet.Amount;

            CrudeProtein += packet.CrudeProtein;
            DegradableCrudeProtein += packet.DegradableProtein;
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
        /// Digestibility undegradable protein.
        /// </summary>
        public double DUDP
        {
            get
            {
                return Details.TypeOfFeed switch
                {
                    FeedType.HaySilage or 
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical => Math.Max(0.05, Math.Min(5.5 * Details.CrudeProteinContent - 0.178, 0.85)),
                    FeedType.Concentrate => 0.9 * (1 - ((Details.AcidDetergentInsoluableProtein / Details.UndegradableCrudeProteinContent))),
                    _ => 0,
                };
            }
        }

        /// <summary>
        /// Metabolisable energy.
        /// </summary>
        public double ME { get { return Details.MEContent * Details.Amount; } }

        /// <summary>
        /// Fermentable metabolisable energy.
        /// </summary>
        public double FME { get { return Details.FMEContent * Details.Amount; } }

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
