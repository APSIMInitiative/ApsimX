namespace Models.Soils.Nutrients
{
    /// <summary>Interface for a nutrient pool.</summary>
    public interface INutrientPool
    {
        /// <summary>Amount of carbon (kg/ha)</summary>
        double[] C { get; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        double[] N { get; }

        /// <summary>Amount of phosphorus (kg/ha)</summary>
        double[] P { get; }

        /// <summary>
        /// Fraction of each layer occupied by this pool.
        /// /// </summary>
        double[] LayerFraction { get; }

        /// <summary>
        /// Set nutrient pool to initialisation state
        /// </summary>
        public void Reset()
        {

        }
    }
}