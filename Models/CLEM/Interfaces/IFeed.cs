using Models.Core;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for nutritional quality of feed types 
    /// </summary>
    public interface IFeed
    {
        /// <summary>
        /// Defines the broad type of this feed
        /// </summary>
        FeedType TypeOfFeed { get; set; }

        /// <summary>
        /// Gross energy content (MJ/kg DM)
        /// </summary>
        double GrossEnergyContent { get; set; }

        /// <summary>
        /// Metabolisable energy content (MJ/kg DM). User provided value. Use MEContent of FoodResourcePacket in model 
        /// </summary>
        double MetabolisableEnergyContent { get; set; }

        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        double DryMatterDigestibility { get; set; }

        /// <summary>
        /// Percent fat content (ether extract) (%)
        /// </summary>
        [Description("Fat percent (%)")]
        double FatPercent { get; set; }

        /// <summary>
        /// Percent nitrogen content (%)
        /// </summary>
        [Description("Nitrogen percent (%)")]
        double NitrogenPercent { get; set; }

        /// <summary>
        /// Percent crude protein content (%)
        /// </summary>
        [Description("Crude protein percent (%)")]
        double CrudeProteinPercent { get; set; }

        /// <summary>
        /// Percent rumen Degradable protein as percent (g/g CP * 100) (100-Rumen Undegradable Protein)
        /// </summary>
        [Description("Degradable protein percent (%, g/g CP * 100)")]
        public double RumenDegradableProteinPercent { get; set; }

        /// <summary>
        /// Acid detergent insoluable protein
        /// </summary>
        [Description("Acid detergent insoluable protein")]
        public double AcidDetergentInsoluableProtein { get; set; }
    }
}
