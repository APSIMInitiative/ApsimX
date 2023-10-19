using Docker.DotNet.Models;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A store for a particular type of animal food used to track details as multiple instances are mixed in the diet
    /// </summary>
    public class FoodResourceStore
    {
        /// <summary>
        /// Store quality and quantity details
        /// </summary>
        public FoodResourcePacket Details { get; set; } = new FoodResourcePacket();

        /// <summary>
        /// Total crude protein in the store
        /// </summary>
        public double CrudeProtein { get; set; }
        /// <summary>
        /// Total rumen degradable crude protein in the store
        /// </summary>
        public double DegradableCrudeProtein { get; set; }
        /// <summary>
        /// Total rumen undegradable crude protein in the store
        /// </summary>
        public double UndegradableCrudeProtein { get { return CrudeProtein - DegradableCrudeProtein; } }

        /// <summary>
        /// Adds a FoodResourcePacket to this store and adjusts pool qualtities
        /// </summary>
        /// <param name="packet">Packet to add</param>
        public void Add(FoodResourcePacket packet)
        {
            Details.DryMatterDigestibility = ((Details.DryMatterDigestibility * Details.Amount) + (packet.DryMatterDigestibility * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.FatContent = ((Details.FatContent * Details.Amount) + (packet.FatContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.NitrogenContent = ((Details.NitrogenContent * Details.Amount) + (packet.NitrogenContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.EnergyContent = ((Details.EnergyContent * Details.Amount) + (packet.MEContent * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.ADIP = ((Details.ADIP * Details.Amount) + (packet.ADIP * packet.Amount)) / (Details.Amount + packet.Amount);
            Details.RumenDegradableProteinContent = ((Details.RumenDegradableProteinContent * Details.Amount) + (packet.RumenDegradableProteinContent * packet.Amount)) / (Details.Amount + packet.Amount);

            Details.Amount += packet.Amount;

            CrudeProtein = Details.CrudeProtein;
            DegradableCrudeProtein += packet.DegradableProtein;
        }

        /// <summary>
        /// Reduce the rumen degradable protein by a proportion provided
        /// </summary>
        /// <param name="factor">The reduction factor</param>
        public void ReduceDegradableProtein(double factor)
        {
            DegradableCrudeProtein = factor * (DegradableCrudeProtein * Math.Min(0.84 * Details.DryMatterDigestibility + 0.33, 1.0));
            CrudeProtein = Details.CrudeProtein;
        }

        /// <summary>
        /// 
        /// </summary>
        public double ME { get { return Details.EnergyContent * Details.Amount; } }

        /// <summary>
        /// Reset running stores
        /// </summary>
        public void Reset()
        {
            CrudeProtein = 0;
            DegradableCrudeProtein = 0;
        }
    }
}
