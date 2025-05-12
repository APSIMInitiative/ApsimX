using Models.Core;
using System;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using System.Collections.Generic;
using APSIM.Numerics;

namespace Models.Soils.Nutrients
{
    /// <summary>
    /// This model estimates the amount of NH3 in the soil and its volatilisation
    /// </summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NH3Volatilisation : Model
    {

        ///////////////////////////////////////////////////////////////////////////
        // Links
        ///////////////////////////////////////////////////////////////////////////
        [Link]
        private readonly IWeather weather = null;

        [Link]
        private readonly ISummary summary = null;

        [Link]
        private readonly ISoilWater waterBalance = null;

        [Link]
        private readonly Irrigation irrigation = null;

        [Link(ByName=true)]
        private readonly NFlow hydrolysis = null;

        [Link(ByName = true)]
        private readonly NFlow nitrification = null;

        [Link(ByName = true)]
        private readonly ISolute nh4 = null;

        [Link]
        private readonly Physical physical = null;

        [Link]
        private readonly Chemical chemical = null;

        [Link]
        private readonly ISoilTemperature soilTemperature = null;

        ///////////////////////////////////////////////////////////////////////////
        // Private fields
        ///////////////////////////////////////////////////////////////////////////

        private double[] initialPH;                    // Initial soil pH
        private double[] cec;                          // Cation exchange capacity (cmol/kg)
        private readonly Dictionary<string, (double a, double b)> pK = new()
        {
            //       a_pK,     b_pK
            { "aq", (0.09018, 2729.92) },
            { "gs", (-1.69, 1477.7) }
        };

        ///////////////////////////////////////////////////////////////////////////
        // User accessible parameters
        ///////////////////////////////////////////////////////////////////////////

        /// <summary>Depth in soil to which all NH3 gas is suseptable to volatilisation (mm)</summary>
        [Separator("NH3 volatilisation parameters")]
        [Description("Depth in soil to which all NH3 gas is suseptable to volatilisation (mm):")]
        public double DepthEmissable1 { get; set; } = 30.0;

        /// <summary>Depth in soil below which no NH3 gas is suseptable to volatilisation (mm)</summary>
        [Description("Depth in soil below which no NH3 gas is suseptable to volatilisation (mm):")]
        public double DepthEmissable2 { get; set; } = 100.0;

        /// <summary>The soil buffer capacity factor</summary>
        [Separator("Base soil and pH change parameters")]
        [Description("Soil buffer capacity factor")]
        public double k_BC { get; set; } = 0.03;

        /// <summary>The factor to convert urea hydrolysed into H+ changes</summary>
        [Description("Factor to convert urea hydrolysed into H+ changes")]
        public double k_pH_hyd { get; set; } = 1.0;

        /// <summary>The factor to convert NH4 nitrified into H+ changes</summary>
        [Description("Factor to convert NH4 nitrified into H+ changes")]
        public double k_pH_nit { get; set; } = 1.0;

        /// <summary>The minimum decrease for pH (fraction/day)</summary>
        [Description("Minimum decrease for pH (fraction/day)")]
        public double k_pH_x { get; set; } = 0.2;

        /// <summary>The factor for gas exchange</summary>
        [Separator("Gas exchange parameters")]
        [Description("Factor for soil/atmosphere gas exchange (AFPV/mm):")]
        public double k_AFPV { get; set; } = 300;

        /// <summary>The additional limits for volatilisation</summary>
        [Separator("Additional limits for volatilisation")]
        [Description("Fraction of total soil NH4 that can be volatilised per day:")]
        public double f_AV { get; set; } = 0.5;

        /// <summary>The critical rainfall for volatilisation</summary>
        [Description("Rain+irrigation below which volatilisation is not limited (mm):")]
        public double CritRain1 { get; set; } = 3;

        /// <summary>The critical rainfall for volatilisation</summary>
        [Description("Rain+irrigation above which no volatilisation occur (mm):")]
        public double CritRain2 { get; set; } = 10;

        ///////////////////////////////////////////////////////////////////////////
        // Outputs
        ///////////////////////////////////////////////////////////////////////////

        /// <summary>Dynamic pH within the volatilisation model</summary>
        public double[] pH { get; private set; }

        /// <summary>Emissability, fraction subject to loss, of NH3 gas by soil layer (0-1)</summary>
        public double[] EmissabilityNH3 { get; private set; }

        /// <summary>The amount of NH3 in the soil (ppm)</summary>
        [Units("ppm")]
        public double[] NH3ppm { get; private set; }

        /// <summary>The amount of NH3 in the soil (kg/ha)</summary>
        [Units("kgN/ha")]
        public double[] NH3 { get; private set; }

        /// <summary>The amount of NH3 in the soil in gaseous form (g/ha)</summary>
        [Units("g/ha")]
        public double[] NH3Gas { get; private set; }

        /// <summary>The estimated amount of NH3 volatilised (kg/ha/day)</summary>
        [Units("kgN/ha")]
        public double[] EmissionNH3 { get; private set; }

        /// <summary>The amount of NH4 in the soil solution (kg/ha)</summary>
        [Units("kgN/ha")]
        public double[] NH4Sol { get; private set; }

        /// <summary>The potential gas exchange (Air-filled-pore-spaces/day)</summary>
        [Units("AFPV/day")]
        public double PotGasExchangeNH { get; private set; }

        /// <summary>The volume of gas exchanged (L/m2/day)</summary>
        [Units("L/m2/day")]
        public double[] GasExchangeNH { get; private set; }

        /// <summary>The air filled pore volume (L/m2)</summary>
        [Units("L/m2")]
        public double[] AirFilledPoreVolume { get; private set; }

        /// <summary>The ratio between NH3 and NHx in the soil solution</summary>
        [Units("")]
        public double[] NH3toNHxRatio { get; private set; }

        /// <summary>The ratio between NH3 liquide and gas in the soil</summary>
        [Units("x1000")]
        public double[] NH3GtoNH3ARatio { get; private set; }

        /// <summary>The equilibrium constant for NH4-NH3</summary>
        [Units("")]
        public double[] pK_NHx { get; private set; }

        /// <summary>The equilibrium constant for NH3A-NH3G</summary>
        [Units("")]
        public double[] pG_NH3 { get; private set; }

        /// <summary>The rain factor</summary>
        [Units("")]
        public double RainFactor { get; private set; }

        ///////////////////////////////////////////////////////////////////////////
        // Methods (functions)
        ///////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Called at the start of the simulation
        /// </summary>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            NH3ppm = new double[physical.Thickness.Length];
            NH4Sol = new double[physical.Thickness.Length];
            NH3 = new double[physical.Thickness.Length];
            NH3Gas = new double[physical.Thickness.Length];
            EmissionNH3 = new double[physical.Thickness.Length];
            GasExchangeNH = new double[physical.Thickness.Length];
            AirFilledPoreVolume = new double[physical.Thickness.Length];
            NH3toNHxRatio = new double[physical.Thickness.Length];
            NH3GtoNH3ARatio = new double[physical.Thickness.Length];
            pK_NHx = new double[physical.Thickness.Length];
            pG_NH3 = new double[physical.Thickness.Length];

            initialPH = chemical.PH.Clone() as double[];
            pH = chemical.PH.Clone() as double[];
            EmissabilityNH3 = new double[physical.Thickness.Length];
            cec = MathUtilities.Multiply_Value(chemical.CEC, 0.01);  // cmol/kg to mol/kg

            // A ramp function describing the suseptability of NH3(g) to emission from the soil surface
            double[] midPoints = SoilUtilities.ToMidPoints(physical.Thickness);
            for (int i = 0; i < physical.Thickness.Length; i++)
            {
                if (midPoints[i] <= DepthEmissable1)
                    EmissabilityNH3[i] = 1.0;
                else if (midPoints[i] <= DepthEmissable2)
                    EmissabilityNH3[i] = 1.0 - (midPoints[i] - DepthEmissable1) /(DepthEmissable2 - DepthEmissable1);
                else
                    EmissabilityNH3[i] = 0.0;
            }

            // verify the bounds of the pH converting factor
            k_pH_hyd = Math.Min(1, Math.Max(0, k_pH_hyd));
            k_pH_nit = Math.Min(1, Math.Max(0, k_pH_nit));
            k_pH_x = Math.Min(1, Math.Max(0, k_pH_x));
            k_BC = Math.Max(0, k_BC);


            // ----------- NH3 volatilization ---------------------------------------------------
            summary.WriteMessage(this, "Volatilization will be calculated", MessageType.Information);
            summary.WriteMessage(this, "pH variation is mimicked by dlt_urea_hydrol + dlt_nh4_nitrif", MessageType.Information);
        }


        /// <summary>
        /// Called to perform daily management calculations
        /// </summary>
        [EventSubscribe("DoManagementCalculations")]
        private void DoManagementCalculations(object sender, EventArgs e)
        {
            // Calc the irrigation factor
            if (weather.Rain + irrigation.IrrigationApplied < CritRain1)
                RainFactor = 1.0;
            else if (weather.Rain + irrigation.IrrigationApplied > CritRain2)
                RainFactor = 0.0;
            else
                RainFactor = 1.0 - (weather.Rain + irrigation.IrrigationApplied - CritRain1) / (CritRain2 - CritRain1);  // this was the wrong way around (and does not agree with the diagram below)

            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // NOTES:
            // The RainFactor accounts for the downwards transport of NH3 gas when water from rain or irrigation is being drained through the soil.
            // Below RainCrit1, precipitation has no effect, above RainCrit2 volatilisation is zero.  A linear relationship is used between
            // these two limits:
            //
            //        |
            //   F   1+                /-----------
            // R a    |               /:
            // a c    |              / :
            // i t    |             /  :
            // n o    |            /   :
            //   r    |           /    :
            //        |          /     :
            //        |         /      :
            //       0+========+-------+--------------> Rain + Irrigation
            //        |      Rain    Rain
            //               Crit1   Crit2
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            // Volatilisation needs thin layers to be sensible. Map layers in and out of this method.

            for (int z = 0; z < physical.Thickness.Length; z++)
            {
                // 0-Compute the value of Beta
                double Beta = k_BC * physical.BD[z] * cec[z] / waterBalance.SW[z];   // VOS - Beta varies by depth but is only used within this layer loop

                // 1-Compute the consumption of H+ by urea hydrolysis
                double ureaHydrol = hydrolysis.Value[z] / (physical.Thickness[z] * 10000);   // kg_urea_N/L_soil
                ureaHydrol = ureaHydrol * 1000 / waterBalance.SW[z];                   // g_urea_N/L_water
                ureaHydrol = ureaHydrol / 14.00674;                             // mol_urea_N/L_water  - All N pools as expresseed in kg_N, so molecular mass is 14
                double dlt_H = -2 * ureaHydrol * k_pH_hyd;                      // Estimated variation of H+ in the soil (mol/L)

                double delta_pH_Hyd;
                // 2-Compute changes in soil pH due to urea hydrolysis
                if (dlt_H < 0)
                    delta_pH_Hyd = (14 / (1 + (14 - pH[z]) / pH[z] * Math.Pow(10, dlt_H / Beta))) - pH[z];
                else
                    delta_pH_Hyd = 0;
                pH[z] = pH[z] + delta_pH_Hyd;

                // 3-Compute the production of H+ by nitrification and its effect on soil pH
                double nh4_nitrif = nitrification.Value[z] / (physical.Thickness[z] * 10000);          // kg_nh4_N/L_soil
                nh4_nitrif = nh4_nitrif * 1000 / waterBalance.SW[z];                      // g_nh4_N/L_water
                nh4_nitrif = nh4_nitrif / 14.0067;                        // mol_nh4_N/L_water  - All N pools as expresseed in kg_N, so molecular mass is 14
                dlt_H = 2 * nh4_nitrif * k_pH_nit;                        // mol/L

                double delta_pH_Nit;               // Variation in soil pH due to NH4 nitrification
                if (dlt_H > 0)
                    delta_pH_Nit = (14 / (1 + (14 - pH[z]) / pH[z] * Math.Pow(10, dlt_H / Beta))) - pH[z];
                else
                    delta_pH_Nit = 0;
                // VOS why is pH not updated here? Held over for the forced change?

                // 4-Compute soil forced pH change - this is to ensure pH will decline to its base value
                double delta_pH_x;                 // Minimum decrease in soil pH, to ensure pH returns to base after urea deposition
                if (pH[z] > initialPH[z])
                    delta_pH_x = -(pH[z] - initialPH[z]) * k_pH_x;
                else
                    delta_pH_x = 0;
                pH[z] = pH[z] + Math.Min(delta_pH_Nit, delta_pH_x);

                // 5-bound pH between base and 14
                pH[z] = Math.Min(14, Math.Max(initialPH[z], pH[z]));


                // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // NOTES:
                // The changes in pH are due to the consumption of H+ as urea is hydrolysed or the production of H+ by nitrification,
                // in theory for each mol of N hydrolised two mols of H+ are consumed, while two mols are produced by nitrification;
                // The k_pH_nit ad k_pH_hyd factors allow to control the extent that H+ is affected by hydrolysis and nitrification,
                // when set to zero there's no effect, while setting it to one causes full theoretical effect
                // The parameter Beta describes the soil buffer capacity, which is a function of CEC, k_BC is used for adjusting the relationship.
                // k_pH_x stablish a minimum rate for pH decay, it mimicks soil buffering and other reactions that bring pH down
                // after urea/urine deposition. Used when nitrification is not enough to bring the pH down.
                // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


                // 6-compute the NH3 equilibrium factors
                pK_NHx[z] = pK["aq"].a + pK["aq"].b / (soilTemperature.AverageSoilTemperature[z] + 273.15);   // Aqueous equilibrium [NH4 <--> NH3]
                pG_NH3[z] = pK["gs"].a + pK["gs"].b / (soilTemperature.AverageSoilTemperature[z] + 273.15);   // Gaseous equilibrium [NH3_liquid <--> NH3Gas]

                // 7-Calc the proportion of total NHx that is in the form of NH3
                double NH3toNHx = 1 / (1 + Math.Pow(10, pK_NHx[z] - pH[z]));
                NH3toNHxRatio[z] = NH3toNHx;

                // 8-Calc the amount of NH3 in aqueous solution (original equation uses mol/L, thus the ratio of molecular mass is needed)
                NH3ppm[z] = nh4.ConcInSolution[z] * NH3toNHx * (17.03 / 18.04);      // ppm (ug/cm3_water)

                // 9-Calc the proportion of total NH3 that is in the gaseous form
                double NH3GtoNH3A = 1 / (1 + Math.Pow(10, pG_NH3[z]));
                NH3GtoNH3ARatio[z] = NH3GtoNH3A * 1000;

                // 10-Calc the amount of NH3 in gaseous form
                NH3Gas[z] = NH3ppm[z] * NH3GtoNH3A;                   // ppm (ug/cm3_air in soil = mg/L)

                // 11-Calc the amount of NH3 in gaseous form and the amount effectivelly lost by volatilization
                PotGasExchangeNH = waterBalance.Eo * k_AFPV;   // air filled pore volumes/day
                AirFilledPoreVolume[z] = (physical.SAT[z] - waterBalance.SW[z]) * physical.Thickness[z];    // L_air/m2

                GasExchangeNH[z] = PotGasExchangeNH * AirFilledPoreVolume[z];      // L_air/m2/day
                GasExchangeNH[z] = GasExchangeNH[z] * RainFactor;
                if (GasExchangeNH[z] > 0)
                {
                    EmissionNH3[z] = NH3Gas[z] * GasExchangeNH[z] * EmissabilityNH3[z];      // mg/m2
                    EmissionNH3[z] = EmissionNH3[z] * 10E+4;                      // mg/ha
                    EmissionNH3[z] = EmissionNH3[z] * 10E-6;                      // kg/ha
                }
                else
                    EmissionNH3[z] = 0;
                   // Limit volatilisation to a fraction of NH4 for each layer

                EmissionNH3[z] = Math.Min(EmissionNH3[z], f_AV * nh4.kgha[z]);   // why is this a fraction of nh4 in ppm? It shouldn't be (wasn't in Classic). CHanged to kgha

                // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // NOTES:
                // To estimate the amount of NH3 volatilised during the day, it is assumed that the amount of NH3 in gaseous form in the
                // beginning of the day represents well the proportion of NHx 'volatilisable'. It is assumed also that the potential evaporation
                // is a good estimator for the gas exchange between soil and atmosphere (this makes volatilisation sensible to temperature and
                // wind).  By scaling the gas exchange to the air filled pore space in the soil, we get volatilisation also sensible to water
                // content, which mimicks the decrease in gas transport when the soil is wet.  Finally a factor (f_NV) is used to limit the
                // contribution of each soil layer to the overall volatilisation (deep layers would volatilise less because NH3 needs to move
                // transport to the surface.
                // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


                // 12-Compute soil pH change due to volatilisation
                double emissionNH3ised = EmissionNH3[z] / (physical.Thickness[z] * 10000);  // kg_NH3_N/L_soil
                emissionNH3ised = emissionNH3ised * 1000 / waterBalance.SW[z];              // g_NH3_N/L_water
                emissionNH3ised = emissionNH3ised / 14.0067;                                // mol_NH3_N/L_water  - All N pools as expresseed in kg_N, so molecular mass is 14
                dlt_H = emissionNH3ised;                                                    // mol/L
                double delta_pH_vol;               // Variation in pH due to volatilisation
                if (dlt_H > 0)
                    delta_pH_vol = (14 / (1 + (14 - pH[z]) / pH[z] * Math.Pow(10, dlt_H / Beta))) - pH[z];
                else
                    delta_pH_vol = 0;

                // 13- Calc new pH and bound it between base and 14
                pH[z] = pH[z] + delta_pH_vol;
                pH[z] = Math.Min(14, Math.Max(initialPH[z], pH[z]));

                // 14-Transform variables to publishable units
                NH3[z] = NH3ppm[z] * waterBalance.SW[z] / 1000;          // mg/cm3_soil = g/L_soil
                NH3[z] = NH3[z] * physical.Thickness[z] * 10000;         // g/ha_soil
                NH3[z] = NH3[z] / 1000;                                  // kg/ha

                NH3Gas[z] = NH3Gas[z] * (physical.SAT[z] - waterBalance.SW[z]) / 1000; // mg/cm3_soil = g/L_soil
                NH3Gas[z] = NH3Gas[z] * physical.Thickness[z] * 10000;                 // g/ha_soil

                NH4Sol[z] = nh4.ConcInSolution[z] * waterBalance.SW[z] / 1000;             // mg/cm3_soil = g/L_soil
                NH4Sol[z] = NH4Sol[z] * physical.Thickness[z] * 10000;                 // g/ha_soil
                NH4Sol[z] = NH4Sol[z] / 1000;                                          // kg/ha
            }

            // Apply the NH4 delta to the solute
            nh4.AddKgHaDelta(SoluteSetterType.Other, MathUtilities.Multiply_Value(EmissionNH3, -1.0));
        }
    }
}