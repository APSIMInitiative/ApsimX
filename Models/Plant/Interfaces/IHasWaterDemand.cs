// -----------------------------------------------------------------------
// <copyright file="IHasWaterDemand.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IHasWaterDemand
    {
        /// <summary>Gets or sets the water demand.</summary>
        double CalculateWaterDemand();

        /// <summary>Sets the organs water allocation.</summary>
        double WaterAllocation { get; set; }

        /// <summary> Flag to test is Microclimate is setting PotentialEP value </summary>
        bool MicroClimatePresent {get; set;}
    }  
}
