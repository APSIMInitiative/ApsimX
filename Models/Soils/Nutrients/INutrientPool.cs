namespace Models.Soils.Nutrients
{
    /// <summary>Interface for a nutrient pool.</summary>
    public interface INutrientPool
    {
        /// <summary>Amount of carbon (kg/ha)</summary>
        double[] C { get; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        double[] N { get; }
    }
}