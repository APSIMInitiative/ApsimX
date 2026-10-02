// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
namespace Models.GrazPlan
{
    /// <summary>
    /// The type used when calling OnConserve()
    /// </summary>
    public class ConserveType
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name;

        /// <summary>
        /// Gets or sets the fresh weight (kg)
        /// </summary>
        /// <value>The fresh weight (kg)</value>
        public double FreshWt;

        /// <summary>
        /// Gets or sets the dry matter content of the supplement (kg/kg FW).
        /// </summary>
        /// <value>Dry matter content of the supplement (kg/kg)</value>
        public double DMContent;

        /// <summary>
        /// Gets or sets the dry matter digestibility of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Dry matter digestibiility of the supplement (kg/kg)</value>
        public double DMD;

        /// <summary>
        /// Gets or sets the phosphorus content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Phosphorus content of the supplement (kg/kg)</value>
        public double NConc;

        /// <summary>
        /// Gets or sets the nitrogen content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Nitrogen content of the supplement (kg/kg)</value>
        public double PConc;

        /// <summary>
        /// Gets or sets the sulfur content of the supplement (kg/kg DM).
        /// </summary>
        /// <value>Sulfur content of the supplement (kg/kg)</value>
        public double SConc;

        /// <summary>
        /// Gets or sets the ash alkalinity of the supplement (mol/kg DM).
        /// </summary>
        /// <value>Ash alkalinity of the supplement (mol/kg)</value>
        public double AshAlk;
    }
}
