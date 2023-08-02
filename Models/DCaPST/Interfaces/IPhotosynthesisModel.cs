namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents a model that simulates daily photosynthesis
    /// </summary>
    public interface IPhotosynthesisModel
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lai"></param>
        /// <param name="sln"></param>
        /// <param name="soilWater"></param>
        /// <param name="RootShootRatio"></param>
        void DailyRun(double lai, double sln, double soilWater, double RootShootRatio);
    }
}
