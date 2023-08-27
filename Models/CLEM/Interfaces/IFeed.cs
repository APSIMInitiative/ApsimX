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
        double EnergyContent { get; set; }

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


        //ToDo: Do we need this, or just CrudeProtein (%)

        /// <summary>
        /// Crude protein degradability
        /// </summary>
        [Description("Crude protein degradability")]
        double CPDegradability { get; set; }
    }
}
