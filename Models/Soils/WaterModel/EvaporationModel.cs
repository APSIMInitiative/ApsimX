namespace Models.WaterModel
{
    using APSIM.Shared.Utilities;
    using Core;
    using Interfaces;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Models.Soils;

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
    /// ![Alt Text](CurveNumberCover.png) 
    /// Figure: Cumulative Soil Evaporation through time for U = 6 mm and CONA = 3.5.
    ///
    /// For t &lt;=  t~1~
    ///    Es = Eos
    /// For t &gt; t~1~
    ///    Es = U x t + CONA x Sqrt(t-t~1~)
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ProfileView")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
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
        private IWeather weather = null;

        [Link]
        private List<ICanopy> canopies = null;

        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;


        /// <summary>cumulative soil evaporation in stage 1 (mm)</summary>
        private double sumes1;

        /// <summary>cumulative soil evaporation in stage 2 (mm)</summary>
        private double sumes2;

        /// <summary>time after 2nd-stage soil evaporation begins (d)</summary>
        public double t;

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
            if (IsSummer)
            {
                u = waterBalance.SummerU;
                cona = waterBalance.SummerCona;
            }

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
            //CalcEo();  // Use EO from MicroClimate
            CalcEoReducedDueToShading();
            CalcEs();
            return Es;
        }

        /// <summary>Return true if simulation is in summer.</summary>
        private bool IsSummer
        {
            get
            {
                return !DateUtilities.WithinDates(waterBalance.WinterDate, clock.Today, waterBalance.SummerDate);
            }
        }

        /// <summary>Calculate the Eo (atmospheric potential)</summary>
        private void CalcEo()
        {
            const double max_albedo = 0.23;

            double coverGreenSum = canopies.Sum(c => c.CoverGreen);
            double albedo = max_albedo - (max_albedo - waterBalance.Salb) * (1.0 - coverGreenSum);

            // weighted mean temperature for the day (oC)
            double wt_ave_temp = (0.60 * weather.MaxT) + (0.40 * weather.MinT);

            // equilibrium evaporation rate (mm)
            double eeq = weather.Radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wt_ave_temp + 29.0);

            // find potential evapotranspiration (eo) from equilibrium evap rate
            Eo = eeq * soilwat2_eeq_fac();
        }

        /// <summary>Calculate the Equilibrium Evaporation Rate</summary>
        private double soilwat2_eeq_fac()
        {
            const double maxCriticalTemperature = 35.0;
            const double minCriticalTemperature = 5.0;
            if (weather.MaxT > maxCriticalTemperature)
            {
                //! at very high max temps eo/eeq increases
                //! beyond its normal value of 1.1
                return ((weather.MaxT - maxCriticalTemperature) * 0.05 + 1.1);
            }
            else
            {
                if (weather.MaxT < minCriticalTemperature)
                {
                    //! at very low max temperatures eo/eeq
                    //! decreases below its normal value of 1.1
                    //! note that there is a discontinuity at tmax = 5
                    //! it would be better at tmax = 6.1, or change the
                    //! .18 to .188 or change the 20 to 21.1
                    return (0.01 * Math.Exp(0.18 * (weather.MaxT + 20.0)));
                }
            }

            return 1.1;
        }

        /// <summary>Calculate potential soil evap after modification for crop cover and residue weight.</summary>
        public void CalcEoReducedDueToShading()
        {
            const double canopy_eos_coef = 1.7;
            const double A_to_evap_fact = 0.44;

            double eos_residue_fract;     //! fraction of potential soil evaporation limited by crop residue (mm)

            double coverTotalSum = 0.0;
            for (int i = 0; i < canopies.Count; i++)
                coverTotalSum = 1.0 - (1.0 - coverTotalSum) * (1.0 - canopies[i].CoverTotal);

            // Based on Adams, Arkin & Ritchie (1976) Soil Sci. Soc. Am. J. 40:436-
            // Reduction in potential soil evaporation under a canopy is determined
            // the "% shade" (ie cover) of the crop canopy - this should include th
            // green & dead canopy ie. the total canopy cover (but NOT near/on-grou
            // residues).  From fig. 5 & eqn 2.                       <dms June 95>
            // Default value for c%canopy_eos_coef = 1.7
            //             ...minimum reduction (at cover =0.0) is 1.0
            //             ...maximum reduction (at cover =1.0) is 0.183.

            // fraction of potential soil evaporation limited by crop canopy (mm)
            double eos_canopy_fract = Math.Exp(-1 * canopy_eos_coef * coverTotalSum);

            // 1a. adjust potential soil evaporation to account for
            //    the effects of surface residue (Adams et al, 1975)
            //    as used in Perfect
            // BUT taking into account that residue can be a mix of
            // residues from various crop types <dms june 95>

            if (surfaceOrganicMatter.Cover >= 1.0)
            {
                // We test for 100% to avoid log function failure.
                // The algorithm applied here approaches 0 as cover approaches
                // 100% and so we use zero in this case.
                eos_residue_fract = 0.0;
            }
            else
            {
                // Calculate coefficient of residue_wt effect on reducing first
                // stage soil evaporation rate

                //  estimate 1st stage soil evap reduction power of
                //    mixed residues from the area of mixed residues.
                //    [DM. Silburn unpublished data, June 95 ]
                //    <temporary value - will reproduce Adams et al 75 effect>
                //     c%A_to_evap_fact = 0.00022 / 0.0005 = 0.44
                eos_residue_fract = Math.Pow((1.0 - surfaceOrganicMatter.Cover), A_to_evap_fact);
            }

            // Reduce potential soil evap under canopy to that under residue (mulch)
            Eos = Eo * eos_canopy_fract * eos_residue_fract;
        }

        /// <summary>calculate actual evaporation from soil surface (es)</summary>
        public void CalcEs()
        {

            //es          -> ! (output) actual evaporation (mm)
            //eos         -> ! (input) potential rate of evaporation (mm/day)
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
                esoil1 = Math.Min(Eos, U - sumes1);

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
                esoil2 = Math.Min(Eos, (CONA * Math.Pow(t, 0.5) - sumes2));

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
