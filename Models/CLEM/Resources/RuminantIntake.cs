using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Manages tracking of Ruminant intake quality and quantity
    /// </summary>
    public  class RuminantIntake
    {
        public FoodResourcePacket CombinedDetails { get; private set; } = new FoodResourcePacket();

        /// <summary>
        /// The potential and actual milk intake of the individual
        /// </summary>
        public ExpectedActualContainer Milk { get; private set; }

        /// <summary>
        /// The potential and actual feed intake of the individual
        /// </summary>
        public ExpectedActualContainer Feed { get; private set; }

        /// <summary>
        /// A function to add intake and track totals of N, CP, DMD, Fat and energy
        /// </summary>
        /// <param name="packet">Feed packet containing intake information kg, %N, DMD</param>
        public void AddFeed(FoodResourcePacket packet)
        {
            if (packet.Amount > 0)
            {
                CombinedDetails.DryMatterDigestibility = ((CombinedDetails.DryMatterDigestibility * CombinedDetails.Amount) + (packet.DryMatterDigestibility * packet.Amount)) / (CombinedDetails.Amount + packet.Amount);
                CombinedDetails.FatContent = ((CombinedDetails.FatContent * CombinedDetails.Amount) + (packet.FatContent * packet.Amount)) / (CombinedDetails.Amount + packet.Amount);
                //CombinedDetails.EnergyContent = ((CombinedDetails.EnergyContent * CombinedDetails.Amount) + (packet.EnergyContent * packet.Amount)) / (CombinedDetails.Amount + packet.Amount);
                CombinedDetails.NitrogenContent = ((CombinedDetails.NitrogenContent * CombinedDetails.Amount) + (packet.NitrogenContent * packet.Amount)) / (CombinedDetails.Amount + packet.Amount);
                CombinedDetails.CPDegradability = ((CombinedDetails.CPDegradability * CombinedDetails.Amount) + (packet.CPDegradability * packet.Amount)) / (CombinedDetails.Amount + packet.Amount);

                // TODO: think about consequences
                // removed the separation of braod feed types (supp and forage) as we can calculate the ME Content differences on the fly and after combined no reason to keep them in memory.

                // Track combined MEContent
                MEContent = ((MEContent * Feed.Actual) + (packet.MEContent * packet.Amount)) / (Feed.Actual + packet.Amount);
                // Track combined METotal
                MEIntakeFeed += packet.MEContent * packet.Amount;

                CombinedDetails.Amount += packet.Amount;
                Feed.Actual = CombinedDetails.Amount;
            }
        }

        /// <summary>
        /// A function to add milk intake and track energy from milk
        /// </summary>
        /// <param name="amount">Amount consumed (L) in timestep</param>
        /// <param name="energyContent">Energy content of milk consumed</param>
        public void AddMilk(double amount, double energyContent)
        {
            if (amount > 0)
            {
                Milk.Actual += amount;
                MEIntakeMilk += energyContent * amount;
            }
        }

        /// <summary>
        /// Reset all intake values
        /// </summary>
        public void Reset()
        {
            MEContent = 0;
            MEIntakeFeed = 0;
            MEIntakeMilk = 0;
            Feed.Reset();
            Milk.Reset();
            CombinedDetails.Reset();
        }

        /// <summary>
        /// Metabolisable Energy content from combined intake
        /// </summary>
        public double MEContent { get; private set; }

        /// <summary>
        /// Total Metabolisable Energy from combined feed intake
        /// </summary>
        public double MEIntakeFeed { get; private set; }

        /// <summary>
        /// Total Metabolisable Energy from milk intake
        /// </summary>
        public double MEIntakeMilk { get; private set; }

        /// <summary>
        /// Total Metabolisable Energy from combined milk and feed intake
        /// </summary>
        public double MEIntakeTotal { get { return MEIntakeFeed + MEIntakeMilk; } }
    }
}
