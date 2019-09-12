namespace Models.PMF.Interfaces
{
    using Soils.Arbitrator;
    using System.Collections.Generic;

    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IWaterNitrogenUptake
    {
        /// <summary>Calculate the water supply for the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        double[] CalculateWaterSupply(ZoneWaterAndN zone);

        /// <summary>Calculate the nitrogen supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        /// <param name="NO3Supply">The returned NO3 supply</param>
        /// <param name="NH4Supply">The returned NH4 supply</param>
        void CalculateNitrogenSupply(ZoneWaterAndN zone, ref double[] NO3Supply, ref double[] NH4Supply);

        /// <summary>Does the water uptake.</summary>
        /// <param name="amount">The amount - layered mm.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
        void DoWaterUptake(double[] amount, string zoneName);

        /// <summary>Does the Nitrogen uptake.</summary>
        /// <param name="zonesFromSoilArbitrator">List of zones from soil arbitrator</param>
        void DoNitrogenUptake(List<ZoneWaterAndN> zonesFromSoilArbitrator);
    }
}
