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
        /// Fat content (%)
        /// </summary>
        [Description("Fat content (%)")]
        double FatContent { get; set; }

        /// <summary>
        /// Nitrogen content (%)
        /// </summary>
        [Description("Nitrogen content (%)")]
        double NitrogenContent { get; set; }

        /// <summary>
        /// Crude protein content (%)
        /// </summary>
        [Description("Crude protein content (%)")]
        double CrudeProteinContent { get; set; }

        /// <summary>
        /// Rumen Degradable Protein (g/g DM) (1-Rumen Undegradable Protein)
        /// </summary>
        [Description("Degradable protein content (g/g DM)")]
        public double RumenDegradableProteinContent { get; set; }

        /// <summary>
        /// Acid detergent insoluable protein
        /// </summary>
        [Description("Acid detergent insoluable protein")]
        public double AcidDetergentInsoluableProtein { get; set; }
    }
}
