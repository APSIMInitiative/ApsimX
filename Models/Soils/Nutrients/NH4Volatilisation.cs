using Models.Core;
using System;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.Soils.Nutrients
{
    /// <summary>
    /// This model estimates the amount of NH3 in the soil and its volatilisation
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NH4Volatilisation : Model
    {
        [Link]
        private readonly IWeather weather = null;

        [Link]
        private readonly ISummary summary = null;

        [Link]
        private readonly ISoilWater waterBalance = null;

        [Link]
        private readonly Irrigation irrigation = null;

        [Link]
        private readonly NFlow hydrolysis = null;

        [Link]
        private readonly NFlow nitrification = null;

        [Link]
        private readonly ISolute nh4 = null;

        [Link]
        private readonly Physical physical = null;

        [Link]
        private readonly Chemical chemical = null;

        [Link]
        private readonly ISoilTemperature soilTemperature = null;

        private double[] initialPH;                    // Initial soil pH
        private int NH3_z;                             // The layer to consider NH3 volatilization
        private double[] cec;                          // Cation exchange capacity (cmol/kg)

        // Parameter variables:
        /// <summary>The depth to which NH3 volatilisation is considered (mm)</summary>
        [Separator("NH3 volatilisation parameters")]
        [Description("Depth to which NH3 volatilisation is considered (mm):")]
        public double Depth_for_NH3 { get; set; }

        /// <summary>The soil buffer capacity factor</summary>
        [Separator("Base soil and pH change parameters")]
        [Description("Soil buffer capacity factor")]
        public double k_BC { get; set; }

        /// <summary>The factor to convert urea hydrolysed into H+ changes</summary>
        [Description("Factor to convert urea hydrolysed into H+ changes")]
        public double k_pH_hyd { get; set; }

        /// <summary>The factor to convert NH4 nitrified into H+ changes</summary>
        [Description("Factor to convert NH4 nitrified into H+ changes")]
        public double k_pH_nit { get; set; }

        /// <summary>The minimum decrease for pH (fraction/day)</summary>
        [Description("Minimum decrease for pH (fraction/day)")]
        public double k_pH_x { get; set; }

        /// <summary>The factor for gas exchange</summary>
        [Separator("Gas exchange parameters")]
        [Description("Factor for soil/atmosphere gas exchange (AFPV/mm):")]
        public double k_AFPV { get; set; }

        /// <summary>The fraction of NH3 volatilisation per soil layer (0-1)</summary>
        [Description("Fractor for volatilisable NH3, per soil layer:")]
        public double[] f_NV { get; set; }

        /// <summary>The additional limits for volatilisation</summary>
        [Separator("Additional limits for volatilisation")]
        [Description("Fraction of total soil NH4 that can be volatilised per day:")]
        public double f_AV { get; set; }

        /// <summary>The critical rainfall for volatilisation</summary>
        [Description("Rain+irrigation below which volatilisation is not limited (mm):")]
        public double CritRain1 { get; set; }

        /// <summary>The critical rainfall for volatilisation</summary>
        [Description("Rain+irrigation above which no volatilisation occur (mm):")]
        public double CritRain2 { get; set; }

        /// <summary>The amount of NH3 in the soil (ppm)</summary>
        [Units("ppm")]
        public double[] NH3ppm { get; set; }

        /// <summary>The amount of NH3 in the soil (kg/ha)</summary>
        [Units("kgN/ha")]
        public double[] NH3 { get; set; }

        /// <summary>The amount of NH3 in the soil in gaseous form (g/ha)</summary>
        [Units("g/ha")]
        public double[] NH3Gas { get; set; }

        /// <summary>The estimated amount of NH3 volatilised (kg/ha/day)</summary>
        [Units("kgN/ha")]
        public double[] EmissionNH3 { get; set; }

        /// <summary>The amount of NH4 in the soil solution (kg/ha)</summary>
        [Units("kgN/ha")]
        public double[] NH4Sol { get; set; }

        /// <summary>The potential gas exchange (Air-filled-pore-spaces/day)</summary>
        [Units("AFPV/day")]
        public double PotGasExchangeNH { get; set; }

        /// <summary>The volume of gas exchanged (L/m2/day)</summary>
        [Units("L/m2/day")]
        public double[] GasExchangeNH { get; set; }

        /// <summary>The air filled pore volume (L/m2)</summary>
        [Units("L/m2")]
        public double[] AirFilledPoreVolume { get; set; }

        /// <summary>The ratio between NH3 and NHx in the soil solution</summary>
        [Units("")]
        public double[] NH3toNHxRatio { get; set; }

        /// <summary>The ratio between NH3 liquide and gas in the soil</summary>
        [Units("x1000")]
        public double[] NH3GtoNH3ARatio { get; set; }

        /// <summary>The equilibrium constant for NH4-NH3</summary>
        [Units("")]
        public double[] pK_NHx { get; set; }

        /// <summary>The equilibrium constant for NH3A-NH3G</summary>
        [Units("")]
        public double[] pG_NH3 { get; set; }

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            initialPH = chemical.PH.Clone() as double[];

            cec = MathUtilities.Multiply_Value(chemical.CEC, 0.01);  // cmol/kg to mol/kg

            // Identify the layer down to which volatilization is considered
            NH3_z = SoilUtilities.LayerIndexOfClosestDepth(physical.Thickness, Depth_for_NH3);

            // verify the bounds of the pH converting factor
            k_pH_hyd = Math.Min(1, Math.Max(0, k_pH_hyd));
            k_pH_nit = Math.Min(1, Math.Max(0, k_pH_nit));
            k_pH_x = Math.Min(1, Math.Max(0, k_pH_x));
            k_BC = Math.Max(0, k_BC);

            for (int z = 0; z < physical.Thickness.Length; z++)
                f_NV[z] = MathUtilities.Bound(f_NV[z], 0, 1);

            // ----------- NH3 volatilization ---------------------------------------------------
            summary.WriteMessage(this, "Volatilization will be calculated", MessageType.Information);
            summary.WriteMessage(this, $"Top {Depth_for_NH3} mm are considered", MessageType.Information);
            summary.WriteMessage(this, "pH variation is mimicked by dlt_urea_hydrol + dlt_nh4_nitrif", MessageType.Information);
        }

        [EventSubscribe("DoManagementCalculations")]
        private void DoManagementCalculations(object sender, EventArgs e)
        {
            double urea_hydrol;                // The amount of urea hydrolised (mol/L)
            double nh4_nitrif;                 // The amount of NH4 nitrified (mol/L)
            double dlt_H;                      // Estimated variation of H+ in the soil (mol/L)
            double delta_pH_Nit;               // Variation in soil pH due to NH4 nitrification
            double delta_pH_x;                 // Minimum decrease in soil pH, to ensure pH returns to base after urea deposition
            double delta_pH_vol;               // Variation in pH due to volatilisation
            double Beta;                       // The soil buffer capacity ()
            double[] delta_nh4 = new double[physical.Thickness.Length];   // The variation in NH4 content due to volatilisation (kg/ha)
            double RainFactor;                 // The rain factor

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

            // Calc the irrigation factor
            if (weather.Rain + irrigation.IrrigationApplied < CritRain1)
                RainFactor = 1.0;
            else if (weather.Rain + irrigation.IrrigationApplied > CritRain2)
                RainFactor = 0.0;
            else
                RainFactor = (weather.Rain + irrigation.IrrigationApplied - CritRain1) / (CritRain2 - CritRain1);

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

            var dlt_urea_hydrol = hydrolysis.Value;
            var dlt_rntrf = nitrification.Value;
            double[] conc_water_nh4 = nh4.ConcInSolution;  // defaults to 0 for SoilWat
            for (int z = 0; z < physical.Thickness.Length; z++)
            {
                // 0-Compute the value of Beta
                Beta = k_BC * physical.BD[z] * cec[z] / waterBalance.SW[z];

                // 1-Compute the consumption of H+ by urea hydrolysis
                urea_hydrol = dlt_urea_hydrol[z] / (physical.Thickness[z] * 10000);   // kg_urea_N/L_soil
                urea_hydrol = urea_hydrol * 1000 / waterBalance.SW[z];                // g_urea_N/L_water
                urea_hydrol = urea_hydrol / 14.00674;                     // mol_urea_N/L_water  - All N pools as expresseed in kg_N, so molecular mass is 14
                dlt_H = -2 * urea_hydrol * k_pH_hyd;                      // mol/L

                double delta_pH_Hyd;
                // 2-Compute changes in soil pH due to urea hydrolysis
                if (dlt_H < 0)
                    delta_pH_Hyd = (14 / (1 + (14 - chemical.PH[z]) / chemical.PH[z] * Math.Pow(10, dlt_H / Beta))) - chemical.PH[z];
                else
                    delta_pH_Hyd = 0;
                chemical.PH[z] = chemical.PH[z] + delta_pH_Hyd;

                // 3-Compute the production of H+ by nitrification and its effect on soil pH
                nh4_nitrif = dlt_rntrf[z] / (physical.Thickness[z] * 10000);          // kg_nh4_N/L_soil
                nh4_nitrif = nh4_nitrif * 1000 / waterBalance.SW[z];                      // g_nh4_N/L_water
                nh4_nitrif = nh4_nitrif / 14.0067;                        // mol_nh4_N/L_water  - All N pools as expresseed in kg_N, so molecular mass is 14
                dlt_H = 2 * nh4_nitrif * k_pH_nit;                        // mol/L
                if (dlt_H > 0)
                    delta_pH_Nit = (14 / (1 + (14 - chemical.PH[z]) / chemical.PH[z] * Math.Pow(10, dlt_H / Beta))) - chemical.PH[z];
                else
                    delta_pH_Nit = 0;

                // 4-Compute soil forced pH change - this is to ensure pH will decline to its base value
                if (chemical.PH[z] > initialPH[z])
                    delta_pH_x = -(chemical.PH[z] - initialPH[z]) * k_pH_x;
                else
                    delta_pH_x = 0;
                chemical.PH[z] = chemical.PH[z] + Math.Min(delta_pH_Nit, delta_pH_x);

                // 5-bound pH between base and 14
                chemical.PH[z] = Math.Min(14, Math.Max(initialPH[z], chemical.PH[z]));


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
                pK_NHx[z] = pK("aq", soilTemperature.AverageSoilTemperature[z] + 273.15);      // Aqueous equilibrium [NH4 <--> NH3]
                pG_NH3[z] = pK("gs", soilTemperature.AverageSoilTemperature[z] + 273.15);      // Gaseous equilibrium [NH3_liquid <--> NH3Gas]

                // 7-Calc the proportion of total NHx that is in the form of NH3
                double NH3toNHx = 1 / (1 + Math.Pow(10, pK_NHx[z] - chemical.PH[z]));
                NH3toNHxRatio[z] = NH3toNHx;

                // 8-Calc the amount of NH3 in aqueous solution (original equation uses mol/L, thus the ratio of molecular mass is needed)
                NH3ppm[z] = conc_water_nh4[z] * NH3toNHx * (17.03 / 18.04);      // ppm (ug/cm3_water)

                // 9-Calc the proportion of total NH3 that is in the gaseous form
                double NH3GtoNH3A = 1 / (1 + Math.Pow(10, pG_NH3[z]));
                NH3GtoNH3ARatio[z] = NH3GtoNH3A * 1000;

                // 10-Calc the amount of NH3 in gaseous form
                NH3Gas[z] = NH3ppm[z] * NH3GtoNH3A;                   // ppm (ug/cm3_air in soil = mg/L)

                // 11-Calc the amount of NH3 in gaseous form and the amount effectivelly lost by volatilization
                PotGasExchangeNH = waterBalance.Eo * k_AFPV;   // air filled pore volumes/day
                AirFilledPoreVolume[z] = (physical.SAT[z] - waterBalance.SW[z]) * physical.Thickness[z];    // L_air/m2
                if (z <= NH3_z)
                {
                    GasExchangeNH[z] = PotGasExchangeNH * AirFilledPoreVolume[z];      // L_air/m2/day
                    GasExchangeNH[z] = GasExchangeNH[z] * RainFactor;
                    if (GasExchangeNH[z] > 0)
                    {
                        EmissionNH3[z] = NH3Gas[z] * GasExchangeNH[z] * f_NV[z];      // mg/m2
                        EmissionNH3[z] = EmissionNH3[z] * 10E+4;                      // mg/ha
                        EmissionNH3[z] = EmissionNH3[z] * 10E-6;                      // kg/ha
                    }
                    else
                        EmissionNH3[z] = 0;
                    // Limit volatilisation to a fraction of NH4 for each layer
                    EmissionNH3[z] = Math.Min(EmissionNH3[z], f_AV * nh4.ppm[z]);
                }
                else
                {
                    GasExchangeNH[z] = 0;
                    EmissionNH3[z] = 0;
                }


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
                double EmissionNH3ised = EmissionNH3[z] / (physical.Thickness[z] * 10000);  // kg_NH3_N/L_soil
                EmissionNH3ised = EmissionNH3ised * 1000 / waterBalance.SW[z];              // g_NH3_N/L_water
                EmissionNH3ised = EmissionNH3ised / 14.0067;                                // mol_NH3_N/L_water  - All N pools as expresseed in kg_N, so molecular mass is 14
                dlt_H = EmissionNH3ised;                                                    // mol/L
                if (dlt_H > 0)
                    delta_pH_vol = (14 / (1 + (14 - chemical.PH[z]) / chemical.PH[z] * Math.Pow(10, dlt_H / Beta))) - chemical.PH[z];
                else
                    delta_pH_vol = 0;

                // 13- Calc new pH and bound it between base and 14
                chemical.PH[z] = chemical.PH[z] + delta_pH_vol;
                chemical.PH[z] = Math.Min(14, Math.Max(initialPH[z], chemical.PH[z]));

                // 14-Transform variables to publishable units
                NH3[z] = NH3ppm[z] * waterBalance.SW[z] / 1000;          // mg/cm3_soil = g/L_soil
                NH3[z] = NH3[z] * physical.Thickness[z] * 10000;         // g/ha_soil
                NH3[z] = NH3[z] / 1000;                                  // kg/ha

                NH3Gas[z] = NH3Gas[z] * (physical.SAT[z] - waterBalance.SW[z]) / 1000; // mg/cm3_soil = g/L_soil
                NH3Gas[z] = NH3Gas[z] * physical.Thickness[z] * 10000;                 // g/ha_soil

                NH4Sol[z] = conc_water_nh4[z] * waterBalance.SW[z] / 1000;             // mg/cm3_soil = g/L_soil
                NH4Sol[z] = NH4Sol[z] * physical.Thickness[z] * 10000;                 // g/ha_soil
                NH4Sol[z] = NH4Sol[z] / 1000;                                          // kg/ha

                // Compute the delta in nh4 amount
                delta_nh4[z] = -EmissionNH3[z];
            }

            // Apply the NH4 delta to the solute
            nh4.AddKgHaDelta(SoluteSetterType.Other, delta_nh4);
        }


        /// <summary>
        /// Calculates the equilibrium constants,
        /// if 'aq' then nh4-NH3 equilibrium in water
        /// if 'gs' then gas-liquid equilibrium for NH3
        /// </summary>
        /// <param name="n_species"></param>
        /// <param name="Temp"></param>
        /// <returns></returns>
        public double pK(string n_species, double Temp)
        {
            double[] a_pK = [0.09018, -1.69];
            double[] b_pK = [2729.92, 1477.7];
            int sp = 0;

            if (n_species == "aq")
                sp = 0;
            else if (n_species == "gs")
                sp = 1;
            return a_pK[sp] + b_pK[sp] / Temp;
        }

    }
}