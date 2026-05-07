// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using System;
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// Paddock and amount of ration
    /// </summary>
    [Serializable]
    public class SuppToStockType : Model, ISuppInfo
    {
        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        [Units("-")]
        public string Paddock { get; set; }

        /// <summary>
        /// Gets or sets the amount of ration (kg).
        /// </summary>
        /// <value>
        /// The amount of ration (kg).
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the flag to feed supplement before pasture. Bail feeding.
        /// </summary>
        [Units("-")]
        public bool FeedSuppFirst { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a roughage.
        /// </summary>
        public bool IsRoughage { get; set; }

        /// <summary>
        /// Gets or sets the dry matter content.
        /// </summary>
        public double DMContent { get; set; }

        /// <summary>
        /// Gets or sets the dry matter digestibility.
        /// </summary>
        public double DMD { get; set; }

        /// <summary>
        /// Gets or sets the metabolizable energy content.
        /// </summary>
        public double MEContent { get; set; }

        /// <summary>
        /// Gets or sets the crude protein concentration.
        /// </summary>
        public double CPConc { get; set; }

        /// <summary>
        /// Gets or sets the protein degradability.
        /// </summary>
        public double ProtDg { get; set; }

        /// <summary>
        /// Gets or sets the phosphorus concentration.
        /// </summary>
        public double PConc { get; set; }

        /// <summary>
        /// Gets or sets the sulphur concentration.
        /// </summary>
        public double SConc { get; set; }

        /// <summary>
        /// Gets or sets the ether extract concentration.
        /// </summary>
        public double EEConc { get; set; }

        /// <summary>
        /// Gets or sets the acid detergent insoluble protein to crude protein ratio.
        /// </summary>
        public double ADIP2CP { get; set; }

        /// <summary>
        /// Gets or sets the ash alkalinity.
        /// </summary>
        public double AshAlk { get; set; }

        /// <summary>
        /// Gets or sets the maximum passage rate.
        /// </summary>
        public double MaxPassage { get; set; }
    }
}
