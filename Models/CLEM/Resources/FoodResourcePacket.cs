using Models.CLEM.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Additional information for animal food requests
    ///</summary> 
    [Serializable]
    public class FoodResourcePacket: IFeed
    {
        /// <summary>
        /// Protein to nitrogen in milk conversion factor
        /// </summary>
        public static double MilkProteinToNitrogenFactor = 6.38;

        /// <summary>
        /// Protein to nitrogen in milk conversion factor
        /// </summary>
        public static double FeedProteinToNitrogenFactor = 6.25;

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
                return TypeOfFeed switch
                {
                    FeedType.HaySilage or
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical => MEContent,
                    FeedType.Concentrate => MEContent - (36 * FatContent / 100) - (14* UndegradableCrudeProteinContent/100),
                    _ => throw new NotImplementedException($"Cannot provide FMEContent for the TypeOfFeed: {TypeOfFeed}."),
                };
            }
        }

        /// <summary>
        /// Calculate Crude Protein from nitrogen content and amount
        /// </summary>
        public double CrudeProtein
        {
            get
            {
                return NitrogenContent * NitrogenToCrudeProteinFactor * Amount;
            }
        }

        /// <summary>
        /// Calculate Crude Protein percentage from nitrogen content
        /// </summary>
        public double CrudeProteinContent
        {
            get
            {
                return NitrogenContent * NitrogenToCrudeProteinFactor;
            }
        }

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
                return TypeOfFeed switch
                {
                    FeedType.HaySilage or
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical => CrudeProtein * Math.Min(0.84 * DryMatterDigestibility + 0.33, 1),
                    FeedType.Concentrate => RumenDegradableProteinContent * Amount,
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
            Amount = 0;
            MetabolisableEnergyContent = 0;
            RumenDegradableProteinContent = 0;
            AcidDetergentInsoluableProtein = 0;
        }

        /// <summary>
        /// Clone this packet 
        /// </summary>
        /// <returns>A copy of this packet</returns>
        public FoodResourcePacket Clone(double amount)
        {
            return new FoodResourcePacket()
            {
                TypeOfFeed = TypeOfFeed,
                MetabolisableEnergyContent = MetabolisableEnergyContent,
                Amount = amount,
                DryMatterDigestibility = DryMatterDigestibility,
                FatContent = FatContent,
                NitrogenContent = NitrogenContent,
                RumenDegradableProteinContent = RumenDegradableProteinContent,
                AcidDetergentInsoluableProtein = AcidDetergentInsoluableProtein,
            };
        }

    }
}
