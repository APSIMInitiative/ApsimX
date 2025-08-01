using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.WaterModel
{

    /// <summary>
    ///Soil evaporation is assumed to take place in two stages: the constant and the falling rate stages.
    ///
    /// In the first stage the soil is sufficiently wet for water to be transported to the surface at a rate
    /// at least equal to the potential evaporation rate. Potential evapotranspiration is calculated using an
    /// equilibrium evaporation concept as modified by Priestly and Taylor(1972).
    ///
    /// Once the water content of the soil has decreased below a threshold value the rate of supply from the soil
    /// will be less than potential evaporation (second stage evaporation). These behaviors are described in SoilWater
    /// through the use of two parameters: U and CONA.
    ///
    /// The parameter U (as from CERES) represents the amount of cumulative evaporation before soil supply decreases
    /// below atmospheric demand. The rate of soil evaporation during the second stage is specified as a function of
    /// time since the end of first stage evaporation. The parameter CONA (from PERFECT) specifies the change in
    /// cumulative second stage evaporation against the square root of time.
    ///
    ///    i.e. Es = CONA t^1/2^
    ///
    /// Water lost by evaporation is removed from the surface layer of the soil profile thus this layer can dry
    /// below the wilting point or lower limit (LL) to a specified air-dry water content (air_dry).
    ///
    /// ![Cumulative Soil Evaporation through time for U = 6 mm and CONA = 3.5.](CurveNumberCover.png)
    ///
    /// For t &lt;=  t~1~
    ///    Es = Eos
    /// For t &gt; t~1~
    ///    Es = U x t + CONA x Sqrt(t-t~1~)
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(WaterBalance))]
    public class EvaporationModel : Model
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance waterBalance = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        private IClock clock = null;

        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;


        /// <summary>cumulative soil evaporation in stage 1 (mm)</summary>
        private double sumes1;

        /// <summary>cumulative soil evaporation in stage 2 (mm)</summary>
        private double sumes2;

        /// <summary>time after 2nd-stage soil evaporation begins (d)</summary>
        public double t;

        /// <summary>Is simulation in summer?</summary>
        private bool isInSummer;

        /// <summary>The value of U yesterday. Used to detect a change in U.</summary>
        private double UYesterday;

        /// <summary>Date for start of summer.</summary>
        private DateTime summerStartDate;

        /// <summary>Date for start of winter.</summary>
        private DateTime winterStartDate;

        /// <summary>Atmospheric potential evaporation (mm)</summary>
        [JsonIgnore]
        public double Eo { get; set; }

        /// <summary>Eo reduced due to shading (mm).</summary>
        [JsonIgnore]
        public double Eos { get; private set; }

        /// <summary>Es - actual evaporation (mm).</summary>
        [JsonIgnore]
        public double Es { get; private set; }

        /// <summary>CONA that was used.</summary>
        public double CONA
        {
            get
            {
                if (IsSummer)
                    return waterBalance.SummerCona;
                else
                    return waterBalance.WinterCona;
            }
        }

        /// <summary>U that was used.</summary>
        public double U
        {
            get
            {
                if (IsSummer)
                    return waterBalance.SummerU;
                else
                    return waterBalance.WinterU;
            }
        }

        /// <summary>Reset the evaporation model.</summary>
        public void Initialise()
        {
            double sw_top_crit = 0.9;
            double sumes1_max = 100;
            double sumes2_max = 25;
            double u = waterBalance.WinterU;
            double cona = waterBalance.WinterCona;
            summerStartDate = DateUtilities.GetDate(waterBalance.SummerDate, 1900).AddDays(1); // AddDays(1) - to reproduce behaviour of DateUtilities.WithinDate
            winterStartDate = DateUtilities.GetDate(waterBalance.WinterDate, 1900);
            var today = clock.Today == DateTime.MinValue ? clock.StartDate : clock.Today;
            isInSummer = !DateUtilities.WithinDates(waterBalance.WinterDate, today, waterBalance.SummerDate);

            if (IsSummer)
            {
                u = waterBalance.SummerU;
                cona = waterBalance.SummerCona;
            }
            UYesterday = u;

            //! set up evaporation stage
            var swr_top = MathUtilities.Divide((waterBalance.Water[0] - soilPhysical.LL15mm[0]),
                                            (soilPhysical.DULmm[0] - soilPhysical.LL15mm[0]),
                                            0.0);
            swr_top = MathUtilities.Constrain(swr_top, 0.0, 1.0);

            //! are we in stage1 or stage2 evap?
            if (swr_top < sw_top_crit)
            {
                //! stage 2 evap
                sumes2 = sumes2_max - (sumes2_max * MathUtilities.Divide(swr_top, sw_top_crit, 0.0));
                sumes1 = u;
                t = MathUtilities.Sqr(MathUtilities.Divide(sumes2, cona, 0.0));
            }
            else
            {
                //! stage 1 evap
                sumes2 = 0.0;
                sumes1 = sumes1_max - (sumes1_max * swr_top);
                t = 0.0;
            }
        }

        /// <summary>Calculate soil evaporation.</summary>
        /// <returns></returns>
        public double Calculate()
        {
            // Done like this to speed up runtime. Using DateUtilities.WithinDates is slow.
            if (clock.Today.Day == summerStartDate.Day && clock.Today.Month == summerStartDate.Month)
                isInSummer = true;
            else if (clock.Today.Day == winterStartDate.Day && clock.Today.Month == winterStartDate.Month)
                isInSummer = false;

            CalcEoReducedDueToResidue();
            CalcEs();
            UYesterday = U;
            return Es;
        }

        /// <summary>Return true if simulation is in summer.</summary>
        private bool IsSummer => isInSummer;

        /// <summary>Calculate potential soil evap after modification for residue weight/cover.</summary>
        public void CalcEoReducedDueToResidue()
        {
            const double A_to_evap_fact = 0.44;

            double eos_residue_fract;     // Fraction of potential soil evaporation limited by crop residue (mm)

            // Adjust potential soil evaporation to account for the effects of surface residue (Adams et al, 1975)
            // as used in `Perfect` BUT taking into account that residue can be a mix of residues from various crop types
            // <dms june 95>

            if (surfaceOrganicMatter.Cover >= 1.0)
            {
                // We test for 100% to avoid log function failure.
                // The algorithm applied here approaches 0 as cover approaches
                // 100% and so we use zero in this case.
                eos_residue_fract = 0.0;
            }
            else
            {
                // Calculate coefficient of residue_wt effect on reducing 1st stage soil evaporation rate
                // Estimate 1st stage soil evap reduction power of mixed residues from the area of mixed residues.
                // [DM. Silburn unpublished data, June 95 ]
                // A_to_evap_fact = 0.00022 / 0.0005 = 0.44 --> To reproduce Adams et al 75 effect
                eos_residue_fract = Math.Pow(1.0 - surfaceOrganicMatter.Cover, A_to_evap_fact);
            }

            // Reduce potential soil evap due to residue (mulch)
            Eos = Eo * eos_residue_fract;
        }

        /// <summary>calculate actual evaporation from soil surface (es)</summary>
        public void CalcEs()
        {

            //es           -> ! (output) actual evaporation (mm)
            //eos          -> ! (input) potential rate of evaporation (mm/day)
            //avail_sw_top -> ! (input) upper limit of soil evaporation (mm/day)  !sv- now calculated in here, not passed in as a argument.

            // Most es takes place in two stages: the constant rate stage
            // and the falling rate stage (philip, 1957).  in the constant
            // rate stage (stage 1), the soil is sufficiently wet for water
            // be transported to the surface at a rate at least equal to the
            // evaporation potential (eos).
            // in the falling rate stage (stage 2), the surface soil water
            // content has decreased below a threshold value, so that es
            // depends on the flux of water through the upper layer of soil
            // to the evaporating site near the surface.

            // This changes globals - sumes1/2 and t.

            Es = 0.0;

            // Calculate available soil water in top layer for actual soil evaporation (mm)
            var airdryMM = soilPhysical.AirDry[0] * soilPhysical.Thickness[0];
            double avail_sw_top = waterBalance.Water[0] - airdryMM;
            avail_sw_top = MathUtilities.Bound(avail_sw_top, 0.0, Eo);

            // Calculate actual soil water evaporation
            double esoil1;     // actual soil evap in stage 1
            double esoil2;     // actual soil evap in stage 2

            // If U has changed (due to summer / winter turn over) and infiltration is zero then reset sumes1 to U to stop
            // artificially entering stage 1 evap. GitHub Issue #8112
            if (UYesterday != U)
            {
                sumes1 = U;
                sumes2 = CONA * Math.Pow(t, 0.5);
            }

            // if infiltration, reset sumes1
            // reset sumes2 if infil exceeds sumes1
            if (waterBalance.Infiltration > 0.0)
            {
                sumes2 = Math.Max(0.0, (sumes2 - Math.Max(0.0, waterBalance.Infiltration - sumes1)));
                sumes1 = Math.Max(0.0, sumes1 - waterBalance.Infiltration);

                // update t (incase sumes2 changed)
                t = MathUtilities.Sqr(MathUtilities.Divide(sumes2, CONA, 0.0));
            }
            else
            {
                // no infiltration, no re-set.
            }

            // are we in stage1 ?
            if (sumes1 < U)
            {
                // we are in stage1
                // set esoil1 = potential, or limited by u.
                esoil1 = Math.Min(Math.Min(Eos, avail_sw_top), U - sumes1);

                if ((Eos > esoil1) && (esoil1 < avail_sw_top))
                {
                    // eos not satisfied by 1st stage drying,
                    // & there is evaporative sw excess to air_dry, allowing for esoil1.
                    // need to calc. some stage 2 drying(esoil2).

                    if (sumes2 > 0.0)
                    {
                        t = t + 1.0;
                        esoil2 = Math.Min((Eos - esoil1), (CONA * Math.Pow(t, 0.5) - sumes2));
                    }
                    else
                        esoil2 = 0.6 * (Eos - esoil1);
                }
                else
                {
                    // no deficit (or esoil1 = eos_max) no esoil2 on this day
                    esoil2 = 0.0;
                }

                // check any esoil2 with lower limit of evaporative sw.
                esoil2 = Math.Min(esoil2, avail_sw_top - esoil1);


                // update 1st and 2nd stage soil evaporation.
                sumes1 = sumes1 + esoil1;
                sumes2 = sumes2 + esoil2;
                t = MathUtilities.Sqr(MathUtilities.Divide(sumes2, CONA, 0.0));
            }
            else
            {
                // no 1st stage drying. calc. 2nd stage
                esoil1 = 0.0;

                t = t + 1.0;
                esoil2 = Math.Min(Eos, CONA * Math.Pow(t, 0.5) - sumes2);

                // check with lower limit of evaporative sw.
                esoil2 = Math.Min(esoil2, avail_sw_top);

                //   update 2nd stage soil evaporation.
                sumes2 = sumes2 + esoil2;
            }

            Es = esoil1 + esoil2;

            // make sure we are within bounds
            Es = MathUtilities.Bound(Es, 0.0, Eos);
            Es = MathUtilities.Bound(Es, 0.0, avail_sw_top);
        }

    }
}
