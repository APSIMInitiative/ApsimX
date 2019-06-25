namespace Models.Soils.Nutrients
{
    /// <summary>Interface for a nutrient pool.</summary>
    public interface INutrientPool
    {
        /// <summary>Amount of carbon (kg/ha)</summary>
        double[] C { get; set; }

        /// <summary>Initial carbon/nitrogen ratio</summary>
        double[] CNRatio { get; }

        /// <summary>Fraction of each layer occupied by this pool.</summary>
        double[] LayerFraction { get; set; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        double[] N { get; set; }

        /// <summary>Add C and N into nutrient pool</summary>
        /// <param name="CAdded">Amount of carbon added (kg/ha).</param>
        /// <param name="NAdded">Amount of nitrogen added (kg/ha).</param>
        void Add(double[] CAdded, double[] NAdded);

        /// <summary>Set nutrient pool to initialisation state</summary>
        void Reset();
    }
}