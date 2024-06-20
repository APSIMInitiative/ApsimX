using System;
using Models.Core;
using Models.Interfaces;

namespace Models;

/// <summary>
/// Calculate atmospheric potential evaporation
/// </summary>
[ValidParent(ParentType = typeof(MicroClimate))]
[Serializable]
public class AtmosphericPotentialEvaporation : Model, ICalculateEo
{
    [Link]
    private readonly IWeather weather = null;

    /// <summary>Albedo at 100% green crop cover (0-1).</summary>
    private const double maxAlbedo = 0.23;

    /// <summary>Temperature below which eeq decreases (oC).</summary>
    private const double minCritTemp = 5.0;

    /// <summary>Temperature above which eeq increases (oC).</summary>
    private const double maxCritTemp = 35.0;
    
    /// <summary>Calculate the atmospheric potential evaporation rate for a zone.</summary>
    /// <param name="zone">The microclimate zone to calculate eo for.</param>
    public double Calculate(MicroClimateZone zone)
    {

        double coverGreen = 0;
        for (int j = 0; j <= zone.Canopies.Count - 1; j++)
            if (zone.Canopies[j].Canopy != null)
                coverGreen += (1 - coverGreen) * zone.Canopies[j].Canopy.CoverGreen;

        if (zone.SoilWater == null)
            throw new Exception("Cannot calculate atmospheric potential evaporation rate. Missing water balance or surface organic matter models.");

        double residueCover = 0;
        if (zone.SurfaceOM != null)
            residueCover = zone.SurfaceOM.Cover;

        return AtmosphericPotentialEvaporationRate(weather.Radn,
                                                   weather.MaxT,
                                                   weather.MinT,
                                                   zone.SoilWater.Salb,
                                                   residueCover,
                                                   coverGreen);
    }

    /// <summary>
    /// Calculate potential evaporation from soil surface (eos)
    /// </summary>
    /// <param name="radn">Solar radiation (MJ/m2/day)</param>
    /// <param name="maxT">Maximum temperature (oC)</param>
    /// <param name="minT">Minimum temperature (oC)</param>
    /// <param name="surfaceAlbedo">Soil surface albedo</param>
    /// <param name="residueCover">Surface residue cover</param>
    /// <param name="coverGreen">Green canopy cover</param>
    private static double AtmosphericPotentialEvaporationRate(double radn, double maxT, double minT, double surfaceAlbedo, double residueCover, double coverGreen)
    {
        double albedo = maxAlbedo - (maxAlbedo - surfaceAlbedo) * (1.0 - coverGreen);

        // wt_ave_temp is mean temp, weighted towards max.
        double wtMeanTemp = 0.6 * maxT + 0.4 * minT;

        double eeq = radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wtMeanTemp + 29.0);
        // find potential evapotranspiration (pot_eo)
        // from equilibrium evap rate
        return eeq * EeqFac(maxT, minT);
    }   


    /// <summary>
    /// Calculate coefficient for equilibrium evaporation rate
    /// </summary>
    /// <param name="maxT">Maximum temperature (oC)</param>
    /// <param name="minT">Minimum temperature (oC)</param>
    private static double EeqFac(double maxT, double minT)
    {
        if (maxT > maxCritTemp)
        {
            // at very high max temps eo/eeq increases
            // beyond its normal value of 1.1
            return (maxT - maxCritTemp) * 0.05 + 1.1;
        }
        else if (maxT < minCritTemp)
        {
            // at very low max temperatures eo/eeq
            // decreases below its normal value of 1.1
            // note that there is a discontinuity at tmax = 5
            // it would be better at tmax = 6.1, or change the
            // .18 to .188 or change the 20 to 21.1
            return 0.01 * Math.Exp(0.18 * (maxT + 20.0));
        }
        else
        {
            // temperature is in the normal range, eo/eeq = 1.1
            return 1.1;
        }
    }     
}
