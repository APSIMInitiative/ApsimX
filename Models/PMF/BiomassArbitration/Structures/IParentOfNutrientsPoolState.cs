namespace Models.PMF
{
    /// <summary>
    /// An interface that any class that has a NutrientPoolState child must implement
    /// </summary>
    public interface IParentOfNutrientsPoolState
    {
        /// <summary>Update own properties and tell parent class to update its properties that are derived from this</summary>
        void UpdateProperties();
    }
}

