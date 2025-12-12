namespace Models.Interfaces
{
    /// <summary>
    /// Interface for models under WaterBalance for different calculations
    /// </summary>
    public interface IWaterCalculation
    {
        ///<summary>Run model calculations</summary>
        void Calculate(double[] swmm);
    }
}
