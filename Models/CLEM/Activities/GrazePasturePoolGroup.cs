using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// A collection of pasture pool feed as individual feed
    /// </summary>
    public class GrazePasturePoolGroup
    {
        /// <summary>
        /// Name of group for tracking changes
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Details of the combined packet of feed for this group
        /// </summary>
        public FoodResourcePacket CombinedPacket { get; set; } = new FoodResourcePacket();

        /// <summary>
        /// the pools included in this group
        /// </summary>
        public IEnumerable<GrazeFoodStorePool> Pools { get; set; }

        /// <summary>
        /// Total biomass for this group
        /// </summary>
        public double Total => Pools.Sum(b => b.Amount);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of group</param>
        /// <param name="pools">Pools in group</param>
        public GrazePasturePoolGroup(string name, IEnumerable<GrazeFoodStorePool> pools)
        {
            Name = name;
            Pools = pools;
            CombinedPacket = new FoodResourcePacket();
            foreach (var pool in pools)
            {
                CombinedPacket.AddAndMix(pool, pool.Amount);
            }
        }
    }
}
