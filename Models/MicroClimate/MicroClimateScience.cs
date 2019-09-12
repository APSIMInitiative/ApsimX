using System;
using APSIM.Shared.Utilities;

namespace Models
{
    public partial class MicroClimate
    {
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
            // finally calculate the sunshine hours as the ratio of maximum possible radiation
            return Math.Min(maxSunHrs * weather.Radn / maxRadn, maxSunHrs);
        }

        /// <summary>
        /// Calculate the crop canopy conductance
        /// <param name="cropGsMax">crop-specific maximum stomatal conductance (m/s)</param>
        /// <param name="cropR50">crop-specific SolRad at which stomatal conductance decreases to 50% (W/m2)</param>
        /// <param name="cropLAIfac">crop-specific LAI fraction of total LAI in current layer (0-1)</param>
        /// <param name="layerK">layer-averaged light extinction coeficient (-)</param>
        /// <param name="layerLAI">LAI within the current layer (m2/m2)</param>
        /// <param name="layerSolRad">solar radiation arriving at the top of the current layer(W/m2)</param>
        /// </summary>
        private double CropCanopyConductance(double cropGsMax, double cropR50, double cropLAIfac, double layerK, double layerLAI, double layerSolRad)
        {
            double numerator = layerSolRad + cropR50;
            double denominator = layerSolRad * Math.Exp(-1.0 * layerK * layerLAI) + cropR50;
            double hyperbolic = Math.Max(1.0, MathUtilities.Divide(numerator, denominator, 0.0));
            return Math.Max(0.0001, MathUtilities.Divide(cropGsMax * cropLAIfac, layerK, 0.0) * Math.Log(hyperbolic));
        }

        /// <summary>
        /// Calculate the aerodynamic conductance using FAO approach
        /// </summary>
        private double AerodynamicConductanceFAO(double windSpeed, double refHeight, double topHeight, double LAItot)
        {
            // Calculate site properties
            double d = 0.666 * topHeight;        // zero plane displacement height (m)
            double Zh = topHeight + refHeight;   // height of humidity measurement (m) - assume reference above canopy
            double Zm = topHeight + refHeight;   // height of wind measurement (m)
            double Z0m = 0.123 * topHeight;      // roughness length governing transfer of momentum (m)
            double Z0h = 0.1 * Z0m;              // roughness length governing transfer of heat and vapour (m)
            // Calcuate conductance
            double mterm = 0.0; // momentum term in Ga calculation
            double hterm = 0.0; // heat term in Ga calculation
            if ((Z0m != 0) && (Z0h != 0))
            {
                mterm = MathUtilities.Divide(vonKarman, Math.Log(MathUtilities.Divide(Zm - d, Z0m, 0.0)), 0.0);
                hterm = MathUtilities.Divide(vonKarman, Math.Log(MathUtilities.Divide(Zh - d, Z0h, 0.0)), 0.0);
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
            double denominator = nondQsdT + MathUtilities.Divide(Ga, Gc, 0.0) + 1.0;    // unitless
            double PETr = MathUtilities.Divide(nondQsdT * rn, denominator, 0.0) * 1000.0 / lambda / RhoW;
            double PETa = MathUtilities.Divide(RhoA * specificVPD * Ga, denominator, 0.0) * 1000.0 * (day_length * hr2s) / RhoW;
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
            double denominator = nondQsdT + MathUtilities.Divide(Ga, Gc, 0.0) + 1.0;
            return MathUtilities.Divide(nondQsdT * rn, denominator, 0.0) * 1000.0 / lambda / RhoW;

        }

        /// <summary>
        /// Calculate the aerodynamically-driven term for the Penman-Monteith water demand
        /// </summary>
        private double CalcPETa(double mint, double maxt, double vp, double airPressure, double day_length, double Ga, double Gc)
        {
            double averageT = CalcAverageT(mint, maxt);
            double nondQsdT = CalcNondQsdT(averageT, airPressure);
            double lambda = CalcLambda(averageT);
            double denominator = nondQsdT + MathUtilities.Divide(Ga, Gc, 0.0) + 1.0;
            double RhoA = CalcRhoA(averageT, airPressure);
            double specificVPD = CalcSpecificVPD(vp, mint, maxt, airPressure);
            return MathUtilities.Divide(RhoA * specificVPD * Ga, denominator, 0.0) * 1000.0 * (day_length * hr2s) / RhoW;
        }

        /// <summary>
        /// Calculate the density of air (kg/m3) at a given temperature
        /// </summary>
        private double CalcRhoA(double temperature, double airPressure)
        {
            return MathUtilities.Divide(mwair * airPressure * 100.0, (abs_temp + temperature) * r_gas, 0.0);            // air pressure converted to Pa
        }

        /// <summary>
        /// Calculate the Jarvis and McNaughton decoupling coefficient, omega
        /// </summary>
        private double CalcOmega(double mint, double maxt, double airPressure, double aerodynamicCond, double canopyCond)
        {
            double Non_dQs_dT = CalcNondQsdT((mint + maxt) / 2.0, airPressure);
            return MathUtilities.Divide(Non_dQs_dT + 1.0, Non_dQs_dT + 1.0 + MathUtilities.Divide(aerodynamicCond, canopyCond, 0.0), 0.0);
        }

        /// <summary>
        /// Calculate Non_dQs_dT - the dimensionless valu for 
        /// d(sat spec humidity)/dT ((kg/kg)/K) FROM TETEN FORMULA
        /// </summary>
        private double CalcNondQsdT(double temperature, double airPressure)
        {
            double esat = CalcSVP(temperature);                                        // saturated vapour pressure (mb)
            double desdt = esat * svp_B * svp_C / Math.Pow(svp_C + temperature, 2.0);  // d(sat VP)/dT : (mb/K)
            double dqsdt = (mwh2o / mwair) * desdt / airPressure;                      // d(sat spec hum)/dT : (kg/kg)/K
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
            double VPDmint = Math.Max(0.0, CalcSVP(mint) - vp);  // VPD at minimum temperature
            double VPDmaxt = Math.Max(0.0, CalcSVP(maxt) - vp);  // VPD at maximum temperature
            return svp_fract * VPDmaxt + (1 - svp_fract) * VPDmint;
        }

        /// <summary>
        /// Calculate the lambda (latent heat of vapourisation for water) (J/kg)
        /// </summary>
        private double CalcLambda(double temperature)
        {
            return (2501.0 - 2.38 * temperature) * 1000.0;  // J/kg
        }

        /// <summary>
        /// Calculates interception of short wave by canopy compartments
        /// </summary>
        private void CalculateLayeredShortWaveRadiation(ZoneMicroClimate ZoneMC)
        {
                // Perform Top-Down Light Balance
                // ==============================
                double Rin = weather.Radn;
                double Rint = 0;
                for (int i = ZoneMC.numLayers - 1; i >= 0; i += -1)
                {
                    Rint = Rin * (1.0 - Math.Exp(-ZoneMC.layerKtot[i] * ZoneMC.LAItotsum[i]));
                    for (int j = 0; j <= ZoneMC.Canopies.Count - 1; j++)
                        ZoneMC.Canopies[j].Rs[i] = Rint * MathUtilities.Divide(ZoneMC.Canopies[j].Ftot[i] * ZoneMC.Canopies[j].Ktot, ZoneMC.layerKtot[i], 0.0);
                    Rin -= Rint;
                }
            ZoneMC.SurfaceRs = Rin;
        }

        /// <summary>
        /// Calculate the overall system energy terms
        /// </summary>
        private void CalculateEnergyTerms(ZoneMicroClimate ZoneMC)
        {
            ZoneMC.sumRs = 0.0;
            ZoneMC.Albedo = 0.0;
            ZoneMC.Emissivity = 0.0;

            for (int i = ZoneMC.numLayers - 1; i >= 0; i += -1)
                for (int j = 0; j <= ZoneMC.Canopies.Count - 1; j++)
                {
                    ZoneMC.Albedo += MathUtilities.Divide(ZoneMC.Canopies[j].Rs[i], weather.Radn, 0.0) * ZoneMC.Canopies[j].Canopy.Albedo;
                    ZoneMC.Emissivity += MathUtilities.Divide(ZoneMC.Canopies[j].Rs[i], weather.Radn, 0.0) * CanopyEmissivity;
                    ZoneMC.sumRs += ZoneMC.Canopies[j].Rs[i];
                }

            ZoneMC.Albedo += (1.0 - MathUtilities.Divide(ZoneMC.sumRs, weather.Radn, 0.0)) * soil_albedo;
            ZoneMC.Emissivity += (1.0 - MathUtilities.Divide(ZoneMC.sumRs, weather.Radn, 0.0)) * SoilEmissivity;
        }

        /// <summary>
        /// Calculate Net Long Wave Radiation Balance
        /// </summary>
        private void CalculateLongWaveRadiation(ZoneMicroClimate ZoneMC)
        {
            double sunshineHours = CalcSunshineHours(weather.Radn, dayLengthLight, weather.Latitude, Clock.Today.DayOfYear);
            double fractionClearSky = MathUtilities.Divide(sunshineHours, dayLengthLight, 0.0);
            double averageT = CalcAverageT(weather.MinT, weather.MaxT);
            ZoneMC.NetLongWaveRadiation = LongWave(averageT, fractionClearSky, ZoneMC.Emissivity) * dayLengthEvap * hr2s / 1000000.0;             // W to MJ

            // Long Wave Balance Proportional to Short Wave Balance
            // ====================================================
            for (int i = ZoneMC.numLayers - 1; i >= 0; i += -1)
                for (int j = 0; j <= ZoneMC.Canopies.Count - 1; j++)
                    ZoneMC.Canopies[j].Rl[i] = MathUtilities.Divide(ZoneMC.Canopies[j].Rs[i], weather.Radn, 0.0) * ZoneMC.NetLongWaveRadiation;
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
            //  Notes: Emissivity of the sky comes from Swinbank, W.C. (1963) Longwave radiation from clear skies Quart. J. Roy. Meteorol. Soc. 89, 339-348.
            fracClearSkyRad = Math.Max(0.0, Math.Min(1.0, fracClearSkyRad));
            double emmisSky = 9.37E-06 * Math.Pow(temperature + abs_temp, 2.0);   // emmisivity of the sky
            double cloudEffect = (c_cloud + (1.0 - c_cloud) * fracClearSkyRad);   // cloud effect on net long wave (0-1)
            return cloudEffect * (emmisSky - emmisCanopy) * stef_boltz * Math.Pow(temperature + abs_temp, 4.0);
        }

        /// <summary>
        /// Calculate Radiation loss to soil heating
        /// </summary>
        private void CalculateSoilHeatRadiation(ZoneMicroClimate ZoneMC)
        {
            double radnint = ZoneMC.sumRs;   // Intercepted SW radiation
            ZoneMC.SoilHeatFlux = CalculateSoilHeatFlux(weather.Radn, radnint, SoilHeatFluxFraction);

            // SoilHeat balance Proportional to Short Wave Balance
            // ====================================================
            for (int i = ZoneMC.numLayers - 1; i >= 0; i += -1)
                for (int j = 0; j <= ZoneMC.Canopies.Count - 1; j++)
                    ZoneMC.Canopies[j].Rsoil[i] = MathUtilities.Divide(ZoneMC.Canopies[j].Rs[i], weather.Radn, 0.0) * ZoneMC.SoilHeatFlux;
        }

        /// <summary>
        /// Calculate the daytime soil heat flux
        /// <param name="radn">(INPUT) Incoming Radiation</param>
        /// <param name="radnint">(INPUT) Intercepted incoming radiation</param>
        /// <param name="soilHeatFluxFraction">(INPUT) Fraction of surface radiation absorbed</param>
        /// </summary>
        private double CalculateSoilHeatFlux(double radn, double radnint, double soilHeatFluxFraction)
        {
            return Math.Max(-radn * 0.1, Math.Min(0.0, -soilHeatFluxFraction * (radn - radnint)));
        }

        private double AtmosphericPotentialEvaporationRate(double Radn, double MaxT, double MinT, double Salb, double residue_cover, double _cover_green_sum)
        {
            // ******* calculate potential evaporation from soil surface (eos) ******

            // find equilibrium evap rate as a
            // function of radiation, albedo, and temp.
            double surface_albedo = Salb + (residue_albedo - Salb) * residue_cover;
            // set surface_albedo to soil albedo for backward compatibility with soilwat
            surface_albedo = Salb;

            double albedo = max_albedo - (max_albedo - surface_albedo) * (1.0 - _cover_green_sum);
            // wt_ave_temp is mean temp, weighted towards max.
            double wt_ave_temp = 0.6 * MaxT + 0.4 * MinT;

            double eeq = Radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wt_ave_temp + 29.0);
            // find potential evapotranspiration (pot_eo)
            // from equilibrium evap rate
            return eeq * EeqFac(MaxT,MinT);
        }

        private double EeqFac(double MaxT, double MinT)
        {
            //+  Purpose
            //                 calculate coefficient for equilibrium evaporation rate
            if (MaxT > max_crit_temp)
            {
                // at very high max temps eo/eeq increases
                // beyond its normal value of 1.1
                return (MaxT - max_crit_temp) * 0.05 + 1.1;
            }
            else if (MaxT < min_crit_temp)
            {
                // at very low max temperatures eo/eeq
                // decreases below its normal value of 1.1
                // note that there is a discontinuity at tmax = 5
                // it would be better at tmax = 6.1, or change the
                // .18 to .188 or change the 20 to 21.1
                return 0.01 * Math.Exp(0.18 * (MaxT + 20.0));
            }
            else
            {
                // temperature is in the normal range, eo/eeq = 1.1
                return 1.1;
            }
        }


    }
}