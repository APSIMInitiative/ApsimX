using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace Models
{
    public partial class MicroClimate
    {

        private double CalcDayLength(double latitude, int day, double sunAngle)
        {
            return Utility.Math.DayLength(day, sunAngle, latitude);
        }

        private double CalcAverageT(double mint, double maxt)
        {
            return 0.75 * maxt + 0.25 * mint;
        }

        private double CalcSunshineHours(double rand, double dayLengthLight, double latitude, double day)
        {
            double maxSunHrs = dayLengthLight;

            double relativeDistance = 1.0 + 0.033 * Math.Cos(0.0172 * day);

            double solarDeclination = 0.409 * Math.Sin(0.0172 * day - 1.39);

            double sunsetAngle = Math.Acos(-Math.Tan(latitude * Deg2Rad) * Math.Tan(solarDeclination));

            double extraTerrestrialRadn = 37.6 * relativeDistance * (sunsetAngle * Math.Sin(latitude * Deg2Rad) * Math.Sin(solarDeclination) + Math.Cos(latitude * Deg2Rad) * Math.Cos(solarDeclination) * Math.Sin(sunsetAngle));

            double maxRadn = 0.75 * extraTerrestrialRadn;

            // finally calculate the sunshine hours as the ratio of
            // maximum possible radiation
            return Math.Min(maxSunHrs * radn / maxRadn, maxSunHrs);
        }

        /// <summary>
        /// Calculate the crop canopy conductance
        /// <param name="cropGsMax">crop-specific maximum stomatal conductance (m/s)</param>
        /// <param name="cropR50">crop-specific SolRad at which stomatal conductance decreases to 50% (W/m2)</param>
        /// <param name="cropRGfac">crop-specific relative growth stress factor (0-1)</param>
        /// <param name="cropLAIfac">crop-specific LAI fraction of total LAI in current layer (0-1)</param>
        /// <param name="layerK">layer-averaged light extinction coeficient (-)</param>
        /// <param name="layerLAI">LAI within the current layer (m2/m2)</param>
        /// <param name="layerSolRad">solar radiation arriving at the top of the current layer(W/m2)</param>
        /// </summary>
        private double CanopyConductance(double cropGsMax, double cropR50, double cropRGfac, double cropLAIfac, double layerK, double layerLAI, double layerSolRad)
        {

            double numerator = layerSolRad + cropR50;
            double denominator = layerSolRad * Math.Exp(-1.0 * layerK * layerLAI) + cropR50;
            double hyperbolic = Utility.Math.Divide(numerator, denominator, 0.0);

            hyperbolic = Math.Max(1.0, hyperbolic);

            return Math.Max(0.0001, Utility.Math.Divide(cropGsMax * cropRGfac * cropLAIfac, layerK, 0.0) * Math.Log(hyperbolic));
        }

        /// <summary>
        /// Calculate the aerodynamic conductance using FAO approach
        /// </summary>
        private double AerodynamicConductanceFAO(double windSpeed, double refHeight, double topHeight, double LAItot)
        {
            const double vonKarman = 0.41;
            double mterm = 0.0;
            // momentum term in Ga calculation
            double hterm = 0.0;
            // heat term in Ga calculation
            // Calculate site properties
            double d = 0.666 * topHeight;
            // zero plane displacement height (m)
            double Zh = topHeight + refHeight;
            // height of humidity measurement (m) - assume reference above canopy
            double Zm = topHeight + refHeight;
            // height of wind measurement (m)
            double Z0m = 0.123 * topHeight;
            // roughness length governing transfer of momentum (m)
            double Z0h = 0.1 * Z0m;
            // roughness length governing transfer of heat and vapour (m)
            // Calcuate conductance

            if ((Z0m != 0) && (Z0h != 0))
            {
                mterm = Utility.Math.Divide(vonKarman, Math.Log(Utility.Math.Divide(Zm - d, Z0m, 0.0)), 0.0);
                hterm = Utility.Math.Divide(vonKarman, Math.Log(Utility.Math.Divide(Zh - d, Z0h, 0.0)), 0.0);
            }

            return Math.Max(0.001, windSpeed * mterm * hterm);
        }

        /// <summary>
        /// Calculate the Penman-Monteith water demand
        /// </summary>
        private double CalcPenmanMonteith(double rn, double mint, double maxt, double vp, double airPressure, double day_length, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double RhoA = CalcRhoA(averageT, airPressure);
            double lambda = CalcLambda(averageT);

            double specificVPD = CalcSpecificVPD(vp, mint, maxt, airPressure);
            double denominator = nondQsdT + Utility.Math.Divide(Ga, Gc, 0.0) + 1.0;
            // unitless

            double PETr = Utility.Math.Divide(nondQsdT * rn, denominator, 0.0) * 1000.0 / lambda / RhoW;

            double PETa = Utility.Math.Divide(RhoA * specificVPD * Ga, denominator, 0.0) * 1000.0 * (day_length * hr2s) / RhoW;

            return PETr + PETa;
        }

        /// <summary>
        /// Calculate the radiation-driven term for the Penman-Monteith water demand
        /// </summary>
        private double CalcPETr(double rn, double mint, double maxt, double airPressure, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double lambda = CalcLambda(averageT);
            double denominator = nondQsdT + Utility.Math.Divide(Ga, Gc, 0.0) + 1.0;

            return Utility.Math.Divide(nondQsdT * rn, denominator, 0.0) * 1000.0 / lambda / RhoW;

        }

        /// <summary>
        /// Calculate the aerodynamically-driven term for the Penman-Monteith water demand
        /// </summary>
        private double CalcPETa(double mint, double maxt, double vp, double airPressure, double day_length, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double lambda = CalcLambda(averageT);
            double denominator = nondQsdT + Utility.Math.Divide(Ga, Gc, 0.0) + 1.0;

            double RhoA = CalcRhoA(averageT, airPressure);

            double specificVPD = CalcSpecificVPD(vp, mint, maxt, airPressure);

            return Utility.Math.Divide(RhoA * specificVPD * Ga, denominator, 0.0) * 1000.0 * (day_length * hr2s) / RhoW;

        }

        /// <summary>
        /// Calculate the density of air (kg/m3) at a given temperature
        /// </summary>
        private double CalcRhoA(double temperature, double airPressure)
        {
            // air pressure converted to Pa
            return Utility.Math.Divide(mwair * airPressure * 100.0, (abs_temp + temperature) * r_gas, 0.0);
        }

        /// <summary>
        /// Calculate the Jarvis & McNaughton decoupling coefficient, omega
        /// </summary>
        private double CalcOmega(double mint, double maxt, double airPressure, double aerodynamicCond, double canopyCond)
        {
            double Non_dQs_dT = CalcNondQsdT((mint + maxt) / 2.0, airPressure);
            return Utility.Math.Divide(Non_dQs_dT + 1.0, Non_dQs_dT + 1.0 + Utility.Math.Divide(aerodynamicCond, canopyCond, 0.0), 0.0);
        }

        /// <summary>
        /// Calculate Non_dQs_dT - the dimensionless valu for 
        /// d(sat spec humidity)/dT ((kg/kg)/K) FROM TETEN FORMULA
        /// </summary>
        private double CalcNondQsdT(double temperature, double airPressure)
        {
            double esat = CalcSVP(temperature);
            // saturated vapour pressure (mb)
            double desdt = esat * svp_B * svp_C / Math.Pow(svp_C + temperature, 2.0);
            // d(sat VP)/dT : (mb/K)
            double dqsdt = (mwh2o / mwair) * desdt / airPressure;
            // d(sat spec hum)/dT : (kg/kg)/K
            return CalcLambda(temperature) / Cp * dqsdt;
        }

        /// <summary>
        /// Calculate the saturated vapour pressure for a given temperature
        /// </summary>
        private double CalcSVP(double temperature)
        {
            return svp_A * Math.Exp(svp_B * temperature / (temperature + svp_C));
        }

        /// <summary>
        /// Calculate the vapour pressure deficit
        /// <param name="vp">(INPUT) vapour pressure (hPa = mbar)</param>
        /// <param name="mint">(INPUT) minimum temperature (oC)</param>
        /// <param name="maxt">(INPUT) maximum temperature (oC)</param>
        /// <param name="airPressure">(INPUT) Air pressure (hPa)</param>
        /// </summary>
        private double CalcSpecificVPD(double vp, double mint, double maxt, double airPressure)
        {
            double VPD = CalcVPD(vp, mint, maxt);
            return CalcSpecificHumidity(VPD, airPressure);
        }

        /// <summary>
        /// Calculate specific humidity from vapour pressure
        /// <param name="vp">vapour pressure (hPa = mbar)</param>
        /// <param name="airPressure">air pressure (hPa)</param>
        /// </summary>
        private double CalcSpecificHumidity(double vp, double airPressure)
        {
            return (mwh2o / mwair) * vp / airPressure;
        }

        /// <summary>
        /// Calculate the vapour pressure deficit
        /// <param name="vp">(INPUT) vapour pressure (hPa = mbar)</param>
        /// <param name="mint">(INPUT) minimum temperature (oC)</param>
        /// <param name="maxt">(INPUT) maximum temperature (oC)</param>
        /// </summary>
        private double CalcVPD(double vp, double mint, double maxt)
        {
            double VPDmint = Math.Max(0.0, CalcSVP(mint) - vp);
            // VPD at minimum temperature
            double VPDmaxt = Math.Max(0.0, CalcSVP(maxt) - vp);
            // VPD at maximum temperature
            return svp_fract * VPDmaxt + (1 - svp_fract) * VPDmint;
        }

        /// <summary>
        /// Calculate the lambda (latent heat of vapourisation for water) (J/kg)
        /// </summary>
        private double CalcLambda(double temperature)
        {
            return (2501.0 - 2.38 * temperature) * 1000.0;
            // J/kg
        }
        /// <summary>
        /// Calculates interception of short wave by canopy compartments
        /// </summary>
        private void ShortWaveRadiation()
        {
            // Perform Top-Down Light Balance
            // ==============================
            double Rin = radn;
            double Rint = 0;
            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                Rint = Rin * (1.0 - Math.Exp(-layerKtot[i] * layerLAIsum[i]));

                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].Rs[i] = Rint * Utility.Math.Divide(ComponentData[j].Ftot[i] * ComponentData[j].Ktot, layerKtot[i], 0.0);
                }
                Rin -= Rint;
            }
        }

        /// <summary>
        /// Calculate the overall system energy terms
        /// </summary>
        private void EnergyTerms()
        {
            sumRs = 0.0;
            _albedo = 0.0;
            emissivity = 0.0;

            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    _albedo += Utility.Math.Divide(ComponentData[j].Rs[i], radn, 0.0) * ComponentData[j].Albedo;
                    emissivity += Utility.Math.Divide(ComponentData[j].Rs[i], radn, 0.0) * ComponentData[j].Emissivity;
                    sumRs += ComponentData[j].Rs[i];
                }
            }

            _albedo += (1.0 - Utility.Math.Divide(sumRs, radn, 0.0)) * soil_albedo;
            emissivity += (1.0 - Utility.Math.Divide(sumRs, radn, 0.0)) * soil_emissivity;
        }

        /// <summary>
        /// Calculate Net Long Wave Radiation Balance
        /// </summary>
        private void LongWaveRadiation()
        {
            netLongWave = LongWave(averageT, fractionClearSky, emissivity) * dayLength * hr2s / 1000000.0;
            // W to MJ
            // Long Wave Balance Proportional to Short Wave Balance
            // ====================================================
            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].Rl[i] = Utility.Math.Divide(ComponentData[j].Rs[i], radn, 0.0) * netLongWave;
                }
            }
        }

        /// <summary>
        /// Calculate the net longwave radiation 'in' (W/m2)
        /// <param name="temperature">temperature  (oC)</param>
        /// <param name="fracClearSkyRad">R/Ro, SunshineHrs/DayLength (0-1)</param>
        /// <param name="emmisCanopy">canopy emmissivity</param>
        /// <returns>net longwave radiation 'in' (W/m2)</returns>
        /// </summary>
        private double LongWave(double temperature, double fracClearSkyRad, double emmisCanopy)
        {
            //  Notes
            //   Emissivity of the sky comes from Swinbank, W.C. (1963).
            //   Longwave radiation from clear skies Quart. J. Roy. Meteorol.
            //   Soc. 89, 339-348.

            //  Changes
            //      291098 - NIH Adapted from Grandis module
            //       050799 - VOS Changed sign so that is net INWARDS longwave
            //       060799 - VOS Changed arguments from sunhine hours and daylength
            //          to FracClearSkyRadn for compatability with variable timestep

            double emmisSky = 0;
            // emmisivity of the sky
            double cloudEffect = 0;
            // cloud effect on net long wave (0-1)
            emmisSky = 9.37E-06 * Math.Pow(temperature + abs_temp, 2.0);

            // assume constant value for now
            // emmisSky = 0.80

            fracClearSkyRad = Math.Max(0.0, Math.Min(1.0, fracClearSkyRad));
            cloudEffect = (c_cloud + (1.0 - c_cloud) * fracClearSkyRad);

            // remove cloud effect for now
            // cloud_effect = 1.0

            return cloudEffect * (emmisSky - emmisCanopy) * stef_boltz * Math.Pow(temperature + abs_temp, 4.0);

            // Try Monteith approach
            // return -(107. - 0.3 * temperature);
        }

        /// <summary>
        /// Calculate Radiation loss to soil heating
        /// </summary>
        private void SoilHeatRadiation()
        {
            double radnint = 0;
            // Intercepted SW radiation
            radnint = sumRs;
            soil_heat = SoilHeatFlux(radn, radnint, soil_heat_flux_fraction);

            // soil_heat = -0.1 * ((1.0 - albedo) * radn * netLongWave;

            // SoilHeat balance Proportional to Short Wave Balance
            // ====================================================
            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].Rsoil[i] = Utility.Math.Divide(ComponentData[j].Rs[i], radn, 0.0) * soil_heat;
                }
            }
        }

        /// <summary>
        /// Calculate the daytime soil heat flux
        /// <param name="radn">(INPUT) Incoming Radiation</param>
        /// <param name="radnint">(INPUT) Intercepted incoming radiation</param>
        /// <param name="soilHeatFluxFraction">(INPUT) Fraction of surface radiation absorbed</param>
        /// </summary>
        private double SoilHeatFlux(double radn, double radnint, double soilHeatFluxFraction)
        {
            return Math.Max(-radn * 0.1, Math.Min(0.0, -soilHeatFluxFraction * (radn - radnint)));
        }

        /// <summary>
        /// Calculate the proportion of light intercepted by a given component that corresponds to green leaf
        /// </summary>
        private double RadnGreenFraction(int j)
        {
            double klGreen = -Math.Log(1.0 - ComponentData[j].CoverGreen);
            double klTot = -Math.Log(1.0 - ComponentData[j].CoverTot);
            return Utility.Math.Divide(klGreen, klTot, 0.0);
        }

    }


}