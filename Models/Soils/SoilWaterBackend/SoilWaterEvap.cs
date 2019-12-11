using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;

namespace Models.Soils.SoilWaterBackend
    {
    [Serializable]
    internal class NormalEvaporation
        {

        //Outputs

        public double Eos;
        public double Es;


        //accumulating variables
        public double sumes1;       //! cumulative soil evaporation in stage 1 (mm)
        public double sumes2;       //! cumulative soil evaporation in stage 2 (mm)
        public double t;            //! time after 2nd-stage soil evaporation begins (d)



        //Inputs

        //from ini file.

        private Constants cons;


        private double salb;           //! bare soil albedo (unitless)

        //same evap for summer and winter   //TODO: move these both into CalcEs_RitchieEq_LimitedBySW()
        private double u= 6.0;       //assigned in CalcEs_RitchieEq_LimitedBySW
        private double cona = 3.0;   //assigned in CalcEs_RitchieEq_LimitedBySW



        //different evap for summer and winter
        //summer

        private double summerCona;
        private double summerU;
        private string summerDate;       //! Date for start of summer evaporation (dd-mmm)

        //winter

        private double winterCona;
        private double winterU;
        private string winterDate;       //! Date for start of winter evaporation (dd-mmm)




        //Zero Ouputs
        public void ZeroOutputs()
            {
            Eos = 0.0;
            Es = 0.0;

            //Don't zero the accumulating variables sumes1, sumes2, t
            }




        //Initialise the Accumulating Variables

        public void InitialiseAccumulatingVars(SoilWaterSoil SoilObject, IClock Clock)
            {
            //reset the accumulated Evap variables (sumes1, sumes2, t) 
            //nb. sumes1 -> is sum of es during stage1
            //used in the SoilWater Init, Reset event



            // soilwat2_soil_property_param()

            //assign u and cona to either sumer or winter values
            // Need to add 12 hours to move from "midnight" to "noon", or this won't work as expected
            if (DateUtilities.WithinDates(winterDate, Clock.Today, summerDate))
                {
                cona = winterCona;
                u = winterU;
                }
            else
                {
                cona = summerCona;
                u = summerU;
                }



            //private void soilwat2_evap_init()
            //    {

            //##################
            //Evap Init   --> soilwat2_evap_init (), soilwat2_ritchie_init()
            //##################   


            //soilwat2_ritchie_init();
            //*+  Mission Statement
            //*       Initialise ritchie evaporation model

            double swr_top;       //! stage 2 evaporation occurs ratio available sw potentially available sw in top layer

            Layer top = SoilObject.GetTopLayer();

            //! set up evaporation stage
            swr_top = MathUtilities.Divide((top.sw_dep - top.ll15_dep), (top.dul_dep - top.ll15_dep), 0.0);
            swr_top = cons.bound(swr_top, 0.0, 1.0);

            //! are we in stage1 or stage2 evap?
            if (swr_top < cons.sw_top_crit)
                {
                //! stage 2 evap
                sumes2 = cons.sumes2_max - (cons.sumes2_max * MathUtilities.Divide(swr_top, cons.sw_top_crit, 0.0));
                sumes1 = u;
                t = MathUtilities.Sqr(MathUtilities.Divide(sumes2, cona, 0.0));
                }
            else
                {
                //! stage 1 evap
                sumes2 = 0.0;
                sumes1 = cons.sumes1_max - (cons.sumes1_max * swr_top);
                t = 0.0;
                }
            }






        //Constructor

        public NormalEvaporation(SoilWaterSoil SoilObject, IClock Clock)
            {

            cons = SoilObject.Constants;

            salb = SoilObject.Salb;

            summerCona = SoilObject.SummerCona;
            summerU = SoilObject.SummerU;
            summerDate = SoilObject.SummerDate;

            winterCona = SoilObject.WinterCona;
            winterU = SoilObject.WinterU;
            winterDate = SoilObject.WinterDate;


            // soilwat2_soil_property_param()

            //u - can either use (one value for summer and winter) or two different values.
            //    (must also take into consideration where they enter two values [one for summer and one for winter] but they make them both the same)


            if ((Double.IsNaN(summerU) || (Double.IsNaN(winterU))))
                {
                throw new Exception("A single value for u OR BOTH values for summeru and winteru must be specified");
                }
            //if they entered two values but they made them the same
            if (summerU == winterU)
                {
                u = summerU;      //u is now no longer null. As if the user had entered a value for u.
                }



            //cona - can either use (one value for summer and winter) or two different values.
            //       (must also take into consideration where they enter two values [one for summer and one for winter] but they make them both the same)

            if ((Double.IsNaN(summerCona)) || (Double.IsNaN(winterCona)))
                {
                throw new Exception("A single value for cona OR BOTH values for summercona and wintercona must be specified");
                }
            //if they entered two values but they made them the same.
            if (summerCona == winterCona)
                {
                cona = summerCona;   //cona is now no longer null. As if the user had entered a value for cona.
                }


            //summer and winter default dates.
            if (summerDate == "not_read")
                {
                summerDate = "1-oct";
                }

            if (winterDate == "not_read")
                {
                winterDate = "1-apr";
                }

            InitialiseAccumulatingVars(SoilObject, Clock);
            }
















        #region Functions

        public void CalcEos_EoReducedDueToShading(double Eo, CanopyData Canopy, SurfaceCoverData SurfaceCover)
            {

            //private void soilwat2_pot_soil_evaporation()
            //    {
            //eos -> ! (output) potential soil evap after modification for crop cover & residue_w


            double eos_canopy_fract;      //! fraction of potential soil evaporation limited by crop canopy (mm)
            double eos_residue_fract;     //! fraction of potential soil evaporation limited by crop residue (mm)


            //! 1. get potential soil water evaporation


            //!---------------------------------------+
            //! reduce Eo to that under plant CANOPY                    <DMS June 95>
            //!---------------------------------------+

            //!  Based on Adams, Arkin & Ritchie (1976) Soil Sci. Soc. Am. J. 40:436-
            //!  Reduction in potential soil evaporation under a canopy is determined
            //!  the "% shade" (ie cover) of the crop canopy - this should include th
            //!  green & dead canopy ie. the total canopy cover (but NOT near/on-grou
            //!  residues).  From fig. 5 & eqn 2.                       <dms June 95>
            //!  Default value for c%canopy_eos_coef = 1.7
            //!              ...minimum reduction (at cover =0.0) is 1.0
            //!              ...maximum reduction (at cover =1.0) is 0.183.


            eos_canopy_fract = Math.Exp(-1 * cons.canopy_eos_coef * Canopy.cover_tot_sum);

            //   !-----------------------------------------------+
            //   ! reduce Eo under canopy to that under mulch            <DMS June 95>
            //   !-----------------------------------------------+

            //   !1a. adjust potential soil evaporation to account for
            //   !    the effects of surface residue (Adams et al, 1975)
            //   !    as used in Perfect
            //   ! BUT taking into account that residue can be a mix of
            //   ! residues from various crop types <dms june 95>

            if (SurfaceCover.surfaceom_cover >= 1.0)
                {
                //! We test for 100% to avoid log function failure.
                //! The algorithm applied here approaches 0 as cover approaches
                //! 100% and so we use zero in this case.
                eos_residue_fract = 0.0;
                }
            else
                {
                //! Calculate coefficient of residue_wt effect on reducing first
                //! stage soil evaporation rate

                //!  estimate 1st stage soil evap reduction power of
                //!    mixed residues from the area of mixed residues.
                //!    [DM. Silburn unpublished data, June 95 ]
                //!    <temporary value - will reproduce Adams et al 75 effect>
                //!     c%A_to_evap_fact = 0.00022 / 0.0005 = 0.44
                eos_residue_fract = Math.Pow((1.0 - SurfaceCover.surfaceom_cover), cons.A_to_evap_fact);
                }

            //! Reduce potential soil evap under canopy to that under residue (mulch)
            Eos = Eo * eos_canopy_fract * eos_residue_fract;
            }









        public void CalcEs_RitchieEq_LimitedBySW(double Eo, SoilWaterSoil SoilObject, IClock Clock, double Infiltration)
            {

            //private void soilwat2_ritchie_evaporation()
            //    {
            //es          -> ! (output) actual evaporation (mm)
            //eos         -> ! (input) potential rate of evaporation (mm/day)
            //avail_sw_top -> ! (input) upper limit of soil evaporation (mm/day)  !sv- now calculated in here, not passed in as a argument.

            //*+  Purpose
            //*          ****** calculate actual evaporation from soil surface (es) ******
            //*          most es takes place in two stages: the constant rate stage
            //*          and the falling rate stage (philip, 1957).  in the constant
            //*          rate stage (stage 1), the soil is sufficiently wet for water
            //*          be transported to the surface at a rate at least equal to the
            //*          evaporation potential (eos).
            //*          in the falling rate stage (stage 2), the surface soil water
            //*          content has decreased below a threshold value, so that es
            //*          depends on the flux of water through the upper layer of soil
            //*          to the evaporating site near the surface.

            //*+  Notes
            //*       This changes globals - sumes1/2 and t.

            Es = 0.0;  //Zero the return value.


            double avail_sw_top;    //! available soil water in top layer for actual soil evaporation (mm)


            //2. get available soil water for evaporation

            Layer top = SoilObject.GetTopLayer();
            avail_sw_top = top.sw_dep - top.air_dry_dep;
            avail_sw_top = cons.bound(avail_sw_top, 0.0, Eo);



            //3. get actual soil water evaporation


            double esoil1;     //! actual soil evap in stage 1
            double esoil2;     //! actual soil evap in stage 2
            double sumes1_max; //! upper limit of sumes1
            double w_inf;      //! infiltration into top layer (mm)



            // Need to add 12 hours to move from "midnight" to "noon", or this won't work as expected
            if (DateUtilities.WithinDates(winterDate, Clock.Today, summerDate))
                {
                cona = winterCona;
                u = winterU;
                }
            else
                {
                cona = summerCona;
                u = summerU;
                }

            sumes1_max = u;
            w_inf = Infiltration;

            //! if infiltration, reset sumes1
            //! reset sumes2 if infil exceeds sumes1      
            if (w_inf > 0.0)
                {
                sumes2 = Math.Max(0.0, (sumes2 - Math.Max(0.0, w_inf - sumes1)));
                sumes1 = Math.Max(0.0, sumes1 - w_inf);

                //! update t (incase sumes2 changed)
                t = MathUtilities.Sqr(MathUtilities.Divide(sumes2, cona, 0.0));
                }
            else
                {
                //! no infiltration, no re-set.
                }

            //! are we in stage1 ?
            if (sumes1 < sumes1_max)
                {
                //! we are in stage1
                //! set esoil1 = potential, or limited by u.
                esoil1 = Math.Min(Eos, sumes1_max - sumes1);

                if ((Eos > esoil1) && (esoil1 < avail_sw_top))
                    {
                    //*           !  eos not satisfied by 1st stage drying,
                    //*           !  & there is evaporative sw excess to air_dry, allowing for esoil1.
                    //*           !  need to calc. some stage 2 drying(esoil2).

                    //*  if g%sumes2.gt.0.0 then esoil2 =f(sqrt(time),p%cona,g%sumes2,g%eos-esoil1).
                    //*  if g%sumes2 is zero, then use ritchie's empirical transition constant (0.6).            

                    if (sumes2 > 0.0)
                        {
                        t = t + 1.0;
                        esoil2 = Math.Min((Eos - esoil1), (cona * Math.Pow(t, 0.5) - sumes2));
                        }
                    else
                        {
                        esoil2 = 0.6 * (Eos - esoil1);
                        }
                    }
                else
                    {
                    //! no deficit (or esoil1.eq.eos_max,) no esoil2 on this day            
                    esoil2 = 0.0;
                    }

                //! check any esoil2 with lower limit of evaporative sw.
                esoil2 = Math.Min(esoil2, avail_sw_top - esoil1);


                //!  update 1st and 2nd stage soil evaporation.     
                sumes1 = sumes1 + esoil1;
                sumes2 = sumes2 + esoil2;
                t = MathUtilities.Sqr(MathUtilities.Divide(sumes2, cona, 0.0));
                }
            else
                {
                //! no 1st stage drying. calc. 2nd stage         
                esoil1 = 0.0;

                t = t + 1.0;
                esoil2 = Math.Min(Eos, (cona * Math.Pow(t, 0.5) - sumes2));

                //! check with lower limit of evaporative sw.
                esoil2 = Math.Min(esoil2, avail_sw_top);

                //!   update 2nd stage soil evaporation.
                sumes2 = sumes2 + esoil2;
                }

            Es = esoil1 + esoil2;

            //! make sure we are within bounds      
            Es = cons.bound(Es, 0.0, Eos);
            Es = cons.bound(Es, 0.0, avail_sw_top);
            }

        #endregion




        }
    }
