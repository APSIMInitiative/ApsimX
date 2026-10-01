// -----------------------------------------------------------------------
// The GrazPlan Supplement objects
// -----------------------------------------------------------------------
namespace Models.GrazPlan
{
    /// <summary>
    /// Supplement information
    /// </summary>
    public interface ISuppInfo
    {
        /// <summary>
        /// Gets or sets a value indicating whether the supplement is a roughage.
        /// </summary>
        /// <value>True if the supplement is a roughage</value>
        public bool IsRoughage { get; set; }

        /// <summary>
        /// Gets or sets the dry matter content of the supplement (kg/kg FW).
        /// </summary>
        /// <value>Dry matter content of the supplement (kg/kg)</value>
        public double DMContent { get; set; }

        /// <summary>
        /// Gets or sets the dry matter digestibility of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Dry matter digestibiility of the supplement (kg/kg)</value>
        public double DMD { get; set; }

        /// <summary>
        /// Gets or sets the metabolizable energy content of the supplement (MJ/kg).
        /// </summary>
        /// <value>Metabolizable energy content of the supplement (MJ/kg)</value>
        public double MEContent { get; set; }

        /// <summary>
        /// Gets or sets the crude protein content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Crude protein content of the supplement (kg/kg)</value>
        public double CPConc { get; set; }

        /// <summary>
        /// Gets or sets the degradability of the protein of the supplement (kg/kg CP).
        /// </summary>
        /// <value>Degradability of the protein of the supplement (kg/kg)</value>
        public double ProtDg { get; set; }

        /// <summary>
        /// Gets or sets the phosphorus content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Phosphorus content of the supplement (kg/kg)</value>
        public double PConc { get; set; }

        /// <summary>
        /// Gets or sets the sulfur content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Sulfur content of the supplement (kg/kg)</value>
        public double SConc { get; set; }

        /// <summary>
        /// Gets or sets the ether-extractable content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Ether-extractable content of the supplement (kg/kg)</value>
        public double EEConc { get; set; }

        /// <summary>
        /// Gets or sets the ratio of acid detergent insoluble protein to CP for the supplement (kg/kg CP).
        /// </summary>
        /// <value>Ratio of acid detergent insoluble protein to CP for the supplement (kg/kg)</value>
        public double ADIP2CP { get; set; }

        /// <summary>
        /// Gets or sets the ash alkalinity of the supplement (mol/kg DM).
        /// </summary>
        /// <value>Ash alkalinity of the supplement (mol/kg)</value>
        public double AshAlk { get; set; }

        /// <summary>
        /// Gets or sets the maximum passage rate of the supplement (0-1).
        /// </summary>
        /// <value>Maximum passage rate of the supplement (kg/kg)</value>
        public double MaxPassage { get; set; }
    }
}
