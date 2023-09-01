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
        ///// <summary>
        ///// Store of combined feed details for the timestep based on broad feed type
        ///// </summary>
        //public Dictionary<FeedType, FoodResourcePacket> FeedDetails { get; set; } = new Dictionary<FeedType, FoodResourcePacket>();

        public FoodResourcePacket CombinedDetails { get; set; } = new FoodResourcePacket();

        /// <summary>
        /// The potential and actual milk intake of the individual
        /// </summary>
        public ExpectedActualContainer Milk { get; set; }

        /// <summary>
        /// The potential and actual feed intake of the individual
        /// </summary>
        public ExpectedActualContainer Feed { get; set; }

        /// <summary>
        /// A function to add intake and track totals of N, CP, DMD, Fat and energy
        /// </summary>
        /// <param name="packet">Feed packet containing intake information kg, %N, DMD</param>
        public void AddIntake(FoodResourcePacket packet)
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
                METotal += packet.MEContent * packet.Amount;

                CombinedDetails.Amount += packet.Amount;
                Feed.Actual = CombinedDetails.Amount;
            }
        }

        ///// <summary>
        ///// A function to add intake and track totals of N, CP, DMD, Fat and energy
        ///// </summary>
        ///// <param name="packet">Feed packet containing intake information kg, %N, DMD</param>
        //public void AddIntake(FoodResourcePacket packet)
        //{
        //    if (packet.Amount > 0)
        //    {
        //        // does feed pool exist for the intake type
        //        FoodResourcePacket feedDetail;
        //        if (!FeedDetails.ContainsKey(packet.TypeOfFeed))
        //        {
        //            // create new store.
        //            feedDetail = new FoodResourcePacket() { TypeOfFeed = packet.TypeOfFeed };
        //            FeedDetails[packet.TypeOfFeed] = feedDetail;
        //        }
        //        else
        //        {
        //            feedDetail = FeedDetails[packet.TypeOfFeed];
        //        }
        //        feedDetail.DryMatterDigestibility = ((feedDetail.DryMatterDigestibility * feedDetail.Amount) + (packet.DryMatterDigestibility * packet.Amount)) / (feedDetail.Amount + packet.Amount);
        //        feedDetail.FatContent = ((feedDetail.FatContent * feedDetail.Amount) + (packet.FatContent * packet.Amount)) / (feedDetail.Amount + packet.Amount);
        //        feedDetail.EnergyContent = ((feedDetail.EnergyContent * feedDetail.Amount) + (packet.EnergyContent * packet.Amount)) / (feedDetail.Amount + packet.Amount);
        //        feedDetail.NitrogenContent = ((feedDetail.NitrogenContent * feedDetail.Amount) + (packet.NitrogenContent * packet.Amount)) / (feedDetail.Amount + packet.Amount);
        //        feedDetail.CPDegradability = ((feedDetail.CPDegradability * feedDetail.Amount) + (packet.CPDegradability * packet.Amount)) / (feedDetail.Amount + packet.Amount);
        //        feedDetail.Amount += packet.Amount;

        //        // Track combined MEContent
        //        MEContent = ((MEContent * Feed.Actual) + (packet.MEContent * packet.Amount)) / (Feed.Actual + packet.Amount);
        //        // Track combined METotal
        //        METotal = ((METotal * Feed.Actual) + (packet.EnergyContent * packet.Amount)) / (Feed.Actual + packet.Amount);

        //        Feed.Actual += packet.Amount;
        //    }
        //}


        /// <summary>
        /// Reset all intake values
        /// </summary>
        public void Reset()
        {
            MEContent = 0;
            METotal = 0;
            MEAvailableForGain = 0;
            Feed.Reset();
            Milk.Reset();
            CombinedDetails.Reset();
            //foreach (var detail in FeedDetails)
            //{
            //    detail.Value.DryMatterDigestibility = 0;
            //    detail.Value.FatContent = 0;
            //    detail.Value.EnergyContent = 0;
            //    detail.Value.NitrogenContent = 0;
            //    detail.Value.CPDegradability = 0;
            //    detail.Value.Amount = 0;
            //}
        }

        /// <summary>
        /// Dictionary of all 
        /// </summary>
        public Dictionary<string, ExpectedActualContainer> Energy { get; set; } = new Dictionary<string, ExpectedActualContainer>()
        {
            { "Intake", new ExpectedActualContainer() },
            { "Maintenance", new ExpectedActualContainer() },
            { "Wool", new ExpectedActualContainer() },
            { "Lactation", new ExpectedActualContainer() },
            { "Pregnancy", new ExpectedActualContainer() },
            { "Growth", new ExpectedActualContainer() }
        };

        /// <summary>
        /// Metabolisable Energy available for gain
        /// </summary>
        public double MEAvailableForGain { get; set; }

        /// <summary>
        /// Metabolisable Energy content from combined intake
        /// </summary>
        public double MEContent { get; set; }

        /// <summary>
        /// Total Metabolisable Energy from combined intake
        /// </summary>
        public double METotal { get; set; }
    }
}
