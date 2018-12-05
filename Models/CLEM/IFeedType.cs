using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM
{
    /// <summary>
    /// Interface for feet types
    /// </summary>
    public interface IFeedType: IResourceType
    {
        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        double DMD { get; set; }

        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        double Nitrogen { get; set; }

        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        double StartingAmount { get; set; }

        /// <summary>
        /// Amount (kg)
        /// </summary>
        new double Amount { get; }
    }
}
