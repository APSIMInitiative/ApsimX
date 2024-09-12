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
        public double FatPercent { get; set; }

        /// <inheritdoc/>
        public double NitrogenPercent { get; set; }

        private double rumenDegradableProteinPercent;

        /// <inheritdoc/>
        public double RumenDegradableProteinPercent
        {
            get
            {
                return rumenDegradableProteinPercent;
            }
            set
            {
                rumenDegradableProteinPercent = value;
                AcidDetergentInsoluableProtein = FoodResourcePacket.CalculateAcidDetergentInsoluableProtein(rumenDegradableProteinPercent, TypeOfFeed);
            }
        }

        /// <inheritdoc/>
        public double AcidDetergentInsoluableProtein { get; set; }

        /// <summary>
        /// Method to calculate the Acid Detergent Insoluable Protein based on the rumen degradable protein and type of feed
        /// </summary>
        /// <param name="rumenDegradableProteinPercent">RDP of feed</param>
        /// <param name="typeOfFeed">Type of feed to identify forage</param>
        /// <returns>ADIP</returns>
        public static double CalculateAcidDetergentInsoluableProtein(double rumenDegradableProteinPercent, FeedType typeOfFeed)
        {
            if (typeOfFeed == FeedType.Concentrate | typeOfFeed == FeedType.Milk)
                return Math.Max(0.03, 0.87 - (1.09 * rumenDegradableProteinPercent / 100));
            else
                return 0.19 * (1 - rumenDegradableProteinPercent / 100);
        }

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
                    FeedType.Concentrate => ((0.134 * DryMatterDigestibility) + (0.235 * FatPercent) + 1.23),
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
        /// Calculate Crude Protein from nitrogen content and amount (g/g DM)
        /// </summary>
        public double CrudeProtein
        {
            get
            {
                return (CrudeProteinPercent / 100.0) * Amount;
            }
        }

        /// <summary>
        /// Calculate Crude Protein percentage from nitrogen content (%)
        /// </summary>
        public double CrudeProteinPercent { get; set; }

        /// <summary>
        /// Calculate Undegradable Crude Protein percent (CP% - RDP%)
        /// </summary>
        public double UndegradableCrudeProteinPercent
        {
            get
            {
                return 100.0 - RumenDegradableProteinPercent;
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
                // It assumes we don't know RDPContent for non-concentrates and will estimate based on DMD
                // New approach assumes RDPContent is 0.7 (used in concentrate and may be user altered)

                return TypeOfFeed switch
                {
                    FeedType.HaySilage or
                    FeedType.PastureTemperate or
                    FeedType.PastureTropical or //=> CrudeProtein * Math.Min(0.84 * (DryMatterDigestibility/100.0) + 0.33, 1),
                    FeedType.Concentrate => (RumenDegradableProteinPercent / 100.0) * CrudeProtein,
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
            FatPercent = 0;
            NitrogenPercent = 0;
            CrudeProteinPercent = 0;
            Amount = 0;
            MetabolisableEnergyContent = 0;
            RumenDegradableProteinPercent = 0;
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
            FatPercent = packet.FatPercent;
            NitrogenPercent = packet.NitrogenPercent;
            CrudeProteinPercent = packet.CrudeProteinPercent;
            RumenDegradableProteinPercent = packet.RumenDegradableProteinPercent;
            AcidDetergentInsoluableProtein = packet.AcidDetergentInsoluableProtein;
            GrossEnergyContent = packet.GrossEnergyContent;
        }

    }
}
