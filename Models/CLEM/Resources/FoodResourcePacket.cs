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
        /// <inheritdoc/>
        public FeedType TypeOfFeed { get; set; }

        /// <inheritdoc/>
        public double EnergyContent { get; set; }

        /// <inheritdoc/>
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        public double FatContent { get; set; }

        /// <inheritdoc/>
        public double NitrogenContent { get; set; }

        /// <inheritdoc/>
        public double RumenDegradableProteinContent { get; set; }

        /// <inheritdoc/>
        public double ADIP { get; set; }

        /// <inheritdoc/>
        public double NitrogenToCrudeProteinFactor { get; set; } = 6.25;

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
                if(EnergyContent > 0)
                {
                    return EnergyContent;
                }
                return TypeOfFeed switch
                {
                    FeedType.Forage => ((0.172 * DryMatterDigestibility) - 1.707),
                    FeedType.Concentrate => ((0.134 * DryMatterDigestibility) + (0.235 * FatContent) + 1.23),
                    _ => throw new NotImplementedException($"Cannot provide MEContent for the TypeOfFeed: {TypeOfFeed}."),
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
                    FeedType.Forage => CrudeProtein * Math.Min(0.84 * DryMatterDigestibility + 0.33, 1),
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
            EnergyContent = 0;
            RumenDegradableProteinContent = 0;
            ADIP = 0;
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
                EnergyContent = EnergyContent,
                Amount = amount,
                DryMatterDigestibility = DryMatterDigestibility,
                FatContent = FatContent,
                NitrogenContent = NitrogenContent,
                RumenDegradableProteinContent = RumenDegradableProteinContent,
                ADIP = ADIP,
                NitrogenToCrudeProteinFactor = NitrogenToCrudeProteinFactor
            };
        }
    }
}
