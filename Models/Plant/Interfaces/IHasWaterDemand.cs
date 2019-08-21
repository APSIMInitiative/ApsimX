namespace Models.PMF.Interfaces
{
    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IHasWaterDemand
    {
        /// <summary>Gets or sets the water demand.</summary>
        double CalculateWaterDemand();

        /// <summary>Sets the organs water allocation.</summary>
        double WaterAllocation { get; set; }
    }  
}
