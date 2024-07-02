using Models.CLEM.Interfaces;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Additional information for animal food requests
    ///</summary> 
    [Serializable]
    public class FoodResourcePacket: IFeed
    {
        private const double feedCP2N = 6.25;
        private const double milkCP2N = 6.38;

        /// <summary>
        /// Protein to nitrogen in milk conversion factor
        /// </summary>
        public static double MilkProteinToNitrogenFactor = milkCP2N;

        /// <summary>
        /// Protein to nitrogen in milk conversion factor
        /// </summary>
        public static double FeedProteinToNitrogenFactor = feedCP2N;

        /// <inheritdoc/>
        public FeedType TypeOfFeed { get; set; }

        /// <inheritdoc/>
        public double GrossEnergyContent { get; set; }

        /// <inheritdoc/>
        public double MetabolisableEnergyContent { get; set; }

        /// <inheritdoc/>
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        public double FatContent { get; set; }

        /// <inheritdoc/>
        public double NitrogenContent { get; set; }

        /// <inheritdoc/>
        public double RumenDegradableProteinContent { get; set; }

        /// <inheritdoc/>
        public double AcidDetergentInsoluableProtein { get; set; }

        /// <summary>
        /// Factor used to convert the Nitrogen percentage and DM to crude protein
        /// </summary>
        public double NitrogenToCrudeProteinFactor 
        {
            get
            {
                if (TypeOfFeed == FeedType.Milk)
                    return MilkProteinToNitrogenFactor;
                return FeedProteinToNitrogenFactor;
            }
        }

        ///<summary>
        /// Amount of food supplied
        ///</summary> 
        public double Amount { get; set; }

        /// <summary>
        /// Metabolic Energy Content of the food resource packet
        /// </summary>
        public double MEContent 
        { 
            get
            {
                if(MetabolisableEnergyContent > 0)
                {
                    return MetabolisableEnergyContent;
                }
                return TypeOfFeed switch
                {
                    FeedType.HaySilage or
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical => ((0.172 * DryMatterDigestibility) - 1.707),
                    FeedType.Concentrate => ((0.134 * DryMatterDigestibility) + (0.235 * FatContent) + 1.23),
                    _ => throw new NotImplementedException($"Cannot provide MEContent for the TypeOfFeed: {TypeOfFeed}."),
                };
            }
        }

        /// <summary>
        /// Fermetable Metabolic Energy Content of the food resource packet
        /// </summary>
        public double FMEContent
        {
            get
            {
                return 0.7 * MEContent;
            }
        }

        /// <summary>
        /// Calculate Crude Protein from nitrogen content and amount
        /// </summary>
        public double CrudeProtein
        {
            get
            {
                return (CrudeProteinContent/100.0) * Amount;
            }
        }

        /// <summary>
        /// Calculate Crude Protein percentage from nitrogen content (%)
        /// </summary>
        public double CrudeProteinContent { get; set; }

        /// <summary>
        /// Calculate Undegradable Crude Protein content
        /// </summary>
        public double UndegradableCrudeProteinContent
        {
            get
            {
                return CrudeProteinContent - RumenDegradableProteinContent;
            }
        }

        /// <summary>
        /// Calculate the Degradable Protein based on feed type equations
        /// </summary>
        public double DegradableProtein
        {
            get
            {
                // Return from non-concetrate is taken from APSIM
                // It assumes we don't know RDPCOntent for non-concentrates and will estimate based on DMD
                // New approach assumes RDPContent is 0.7 (used in concentrate and may be user altered)

                return TypeOfFeed switch
                {
                    FeedType.HaySilage or
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical or //=> CrudeProtein * Math.Min(0.84 * (DryMatterDigestibility/100.0) + 0.33, 1),
                    FeedType.Concentrate => RumenDegradableProteinContent * CrudeProtein,
                    FeedType.Milk => 0,
                    _ => throw new NotImplementedException($"Cannot provide degradable protein for the FeedType {TypeOfFeed}"),
                };
            }
        }

        /// <summary>
        /// Reset all stores
        /// </summary>
        public void Reset()
        {
            DryMatterDigestibility = 0;
            FatContent = 0;
            NitrogenContent = 0;
            CrudeProteinContent = 0;
            Amount = 0;
            MetabolisableEnergyContent = 0;
            RumenDegradableProteinContent = 0;
            AcidDetergentInsoluableProtein = 0;
            GrossEnergyContent = 0;
        }

        /// <summary>
        /// Clone this packet 
        /// </summary>
        /// <returns>A copy of this packet</returns>
        public FoodResourcePacket Clone(double amount)
        {
            return new FoodResourcePacket(this)
            {
                Amount = amount,
            };
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FoodResourcePacket()
        {
                
        }

        /// <summary>
        /// Constructor based on an IFeed clone
        /// </summary>
        /// <param name="packet"></param>
        public FoodResourcePacket(IFeed packet)
        {
            TypeOfFeed = packet.TypeOfFeed;
            MetabolisableEnergyContent = packet.MetabolisableEnergyContent;
            DryMatterDigestibility = packet.DryMatterDigestibility;
            FatContent = packet.FatContent;
            NitrogenContent = packet.NitrogenContent;
            CrudeProteinContent = packet.CrudeProteinContent;
            RumenDegradableProteinContent = packet.RumenDegradableProteinContent;
            AcidDetergentInsoluableProtein = packet.AcidDetergentInsoluableProtein;
            GrossEnergyContent = packet.GrossEnergyContent;
        }

    }
}
