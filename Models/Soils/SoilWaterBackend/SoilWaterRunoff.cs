using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SoilWaterBackend
    {
    [Serializable]
    internal class NormalRunoff
        {

        //Outputs

        /// Effective total cover (0-1)
        /// residue cover + cover from any crops (tall or short)
        public double cover_surface_runoff;

        public double cn2_new;               //! New cn2  after modification for crop cover & residue cover
        public double cn_red_cov;            //reduction of cn2_bare due to cover               //sv- I added this, I think it would be helpful.
        public double cn_red_till;           //reduction of cn2_bare due to tillage event       //sv- I added this, I think it would be helpful.

        public double Runoff;



        //Inputs

        //from ini file.

        private Constants cons;


        private double _cn2_bare;


        //Reduction in cn2_bare due to cover (so when it is not bare)

        private double coverCnCov;  //cn_cov;
        private double coverCnRed;  //cn_red;






        #region Tillage


        //Reduction in cn2_bare due to a tillage of the soil occuring.
     
        public double cumWaterSinceTillage;   //tillage_rain_sum //! running total of cumulative rainfall since last tillage event. Used for tillage CN reduction (mm)
        public double tillageCnCumWater;  //tillage_cn_rain //! cumulative rainfall below which tillage reduces CN (mm) //can either come from the manager module orh the sim file
        public double tillageCnRed;     //! reduction in CN due to tillage ()   //can either come from the manager module or from the sim file






        private void ShouldIStopTillageCNReduction()
            {
            //private void soilwat2_tillage_addrain(double Rain, double Runon, double TotalInterception)
            //{

            //The reduction in the runoff as a result of doing a tillage (tillage_cn_red) ceases after a set amount of rainfall (tillage_cn_rain).
            //This function works out the accumulated rainfall since last tillage event, and turns off the reduction if it is over the amount of rain specified.
            //This  soilwat2_tillage_addrain() is only called in soilwat2_runoff() 

            //sv- The Runoff is altered after a tillage event occurs.
            //sv- This code calculates how much it should be altered based on the accumulated rainfall since the last tillage event. 
            //sv- The zeroing of the tillage_rain_sum occurs in the tillage event.

            //*+  Mission Statement
            //*      Accumulate rainfall for tillage cn reduction 


            //sv- NB. not just rain. Accumulate any type of water that may runoff (Runon, Irrigation etc.)

            string message;      //! message string



            if ((tillageCnCumWater > 0.0) && (cumWaterSinceTillage > tillageCnCumWater))
                {
                //! This tillage has lost all effect on cn. CN reduction
                //!  due to tillage is off until the next tillage operation.
                tillageCnCumWater = 0.0;   //sv- why do we need to zero this?
                tillageCnRed = 0.0;

                message = "Tillage CN reduction finished";
                cons.IssueWarning(message);

                }

            }

        #endregion








        //Constructor

        public NormalRunoff(SoilWaterSoil SoilObject)
            {
            cons = SoilObject.Constants;

            _cn2_bare = SoilObject.cn2_bare;
            coverCnRed = SoilObject.cn_red;
            coverCnCov = SoilObject.cn_cov;


            //soilwat2_soil_property_param()

            if (coverCnRed >= _cn2_bare)
                {
                coverCnRed = _cn2_bare - 0.00009;
                }

            }







        #region Functions




        public void CalcCoverForRunoff(CanopyData Canopy, SurfaceCoverData SurfaceCover)
            {
            //private void soilwat2_cover_surface_runoff()
            //    {

            //This does NOT calculate runoff. It calculates an effective cover that is used for runoff.
            //In the process event this is called before the soilwat2_runoff.

            //*+  Purpose
            //*       calculate the effective runoff cover

            //*+  Assumptions
            //*       Assumes that if canopy height is negative it is missing.

            //*+  Mission Statement
            //*     Calculate the Effective Runoff surface Cover

            double canopyfact;             //! canopy factor (0-1)
            int crop;                   //! crop number
            double effective_crop_cover;   //! effective crop cover (0-1)
            double cover_surface_crop;     //! efective total cover (0-1)

            //! cover cn response from perfect   - ML  & dms 7-7-95
            //! nb. perfect assumed crop canopy was 1/2 effect of mulch
            //! This allows the taller canopies to have less effect on runoff
            //! and the cover close to ground to have full effect (jngh)

            //! weight effectiveness of crop canopies
            //!    0 (no effect) to 1 (full effect)

            cover_surface_crop = 0.0;
            for (crop = 0; crop < Canopy.NumberOfCrops; crop++)
                {
                if (Canopy.canopy_height[crop] >= 0.0)
                    {
                    bool bDidInterpolate;
                    canopyfact = Utility.Math.LinearInterpReal(Canopy.canopy_height[crop], cons.canopy_fact_height, cons.canopy_fact, out bDidInterpolate);
                    }
                else
                    {
                    canopyfact = cons.canopy_fact_default;
                    }

                effective_crop_cover = Canopy.cover_tot[crop] * canopyfact;
                cover_surface_crop = add_cover(cover_surface_crop, effective_crop_cover);
                }

            //! add cover known to affect runoff
            //!    ie residue with canopy shading residue         
            cover_surface_runoff = add_cover(cover_surface_crop, SurfaceCover.surfaceom_cover);
            }



        private double add_cover(double cover1, double cover2)
            {
            //!+ Sub-Program Arguments
            //   real       cover1                ! (INPUT) first cover to combine (0-1)
            //   real       cover2                ! (INPUT) second cover to combine (0-1)

            //!+ Purpose
            //!     Combines two covers

            //!+  Definition
            //!     "cover1" and "cover2" are numbers between 0 and 1 which
            //!     indicate what fraction of sunlight is intercepted by the
            //!     foliage of plants.  This function returns a number between
            //!     0 and 1 indicating the fraction of sunlight intercepted
            //!     when "cover1" is combined with "cover2", i.e. both sets of
            //!     plants are present.

            //!+  Mission Statement
            //!     cover as a result of %1 and %2

            double bare;     //! bare proportion (0-1)

            bare = (1.0 - cover1) * (1.0 - cover2);
            return (1.0 - bare);

            }












        public void CalcRunoff(double WaterForRunoff, SoilWaterSoil SoilObject)
            {
            //private void soilwat2_runoff(double Rain, double Runon, double TotalInterception, ref double Runoff)
            //{

            Runoff = 0.0;  //zero the return parameter

            if (WaterForRunoff > 0.0)
                {

                CalcRunoff_USDA_SoilConservationService(WaterForRunoff, SoilObject);

                cumWaterSinceTillage = cumWaterSinceTillage + WaterForRunoff;
                ShouldIStopTillageCNReduction();  //! NB. this needs to be done _after_ cn calculation.
                }
            }





        private void CalcRunoff_USDA_SoilConservationService(double WaterForRunoff, SoilWaterSoil SoilObject)
            {
            //private void soilwat2_scs_runoff(double Rain, double Runon, double TotalInterception, ref double Runoff)
            //    {
            //cn2_new (output)
            //Runoff  (output)

            double cn;                                 //! scs curve number
            double cn1;                                //! curve no. for dry soil (antecedant) moisture
            double cn3;                                //! curve no. for wet soil (antecedant) moisture
            double cnpd;                               //! cn proportional in dry range (dul to ll15)

            double s;                                  //! potential max retention (surface ponding + infiltration)
            double xpb;                                //! intermedite variable for deriving runof
            double[] runoff_wf;                        //! weighting factor for depth for each layer
            double dul_fraction;                       // if between (0-1) not above dul, if (1-infinity) above dul 


            double cover_fract;                        //! proportion of maximum cover effect on runoff (0-1)
            double cover_reduction = 0.0;

            double tillage_fract;
            double tillage_reduction = 0.0;            //! reduction in cn due to tillage


            runoff_wf = new double[SoilObject.num_layers];

            soilwat2_runoff_depth_factor(SoilObject, ref runoff_wf);

            cnpd = 0.0;
            foreach (Layer lyr in SoilObject)
                {
                dul_fraction = Utility.Math.Divide((lyr.sw_dep - lyr.ll15_dep), (lyr.dul_dep - lyr.ll15_dep), 0.0);
                cnpd = cnpd + dul_fraction * runoff_wf[lyr.number-1]; //zero based array.
                }
            cnpd = cons.bound(cnpd, 0.0, 1.0);


            //reduce cn2 for the day due to the cover effect
            //nb. cover_surface_runoff should really be a parameter to this function
            cover_fract = Utility.Math.Divide(cover_surface_runoff, coverCnCov, 0.0);
            cover_fract = cons.bound(cover_fract, 0.0, 1.0);
            cover_reduction = coverCnRed * cover_fract;
            cn2_new = _cn2_bare - cover_reduction;


            //tillage reduction on cn
            //nb. tillage_cn_red, tillage_cn_rain, and tillage_rain_sum, should really be parameters to this function
            if (tillageCnCumWater > 0.0)
                {
                //We minus 1 because we want the opposite fraction. 
                //Tillage Reduction is biggest (CnRed value) straight after Tillage and gets smaller and becomes 0 when reaches CumWater.
                //unlike the Cover Reduction, where the reduction starts out smallest (0) and gets bigger and becomes (CnRed value) when you hit CnCover.
                tillage_fract = Utility.Math.Divide(cumWaterSinceTillage, tillageCnCumWater, 0.0) - 1.0; 
                tillage_reduction = tillageCnRed * tillage_fract;
                cn2_new = cn2_new + tillage_reduction;
                }
            else
                {
                //nothing
                }


            //! cut off response to cover at high covers if p%cn_red < 100.
            cn2_new = cons.bound(cn2_new, 0.0, 100.0);

            cn1 = Utility.Math.Divide(cn2_new, (2.334 - 0.01334 * cn2_new), 0.0);
            cn3 = Utility.Math.Divide(cn2_new, (0.4036 + 0.005964 * cn2_new), 0.0);
            cn = cn1 + (cn3 - cn1) * cnpd;

            // ! curve number will be decided from scs curve number table ??dms
            s = 254.0 * (Utility.Math.Divide(100.0, cn, 1000000.0) - 1.0);
            xpb = WaterForRunoff - 0.2 * s;
            xpb = Math.Max(xpb, 0.0);

            //assign the output variable
            Runoff = Utility.Math.Divide(xpb * xpb, (WaterForRunoff + 0.8 * s), 0.0);

            //sv- I added these output variables
            cn_red_cov = cover_reduction;
            cn_red_till = tillage_reduction;


            //bound check the ouput variable
            cons.bound_check_real_var(Runoff, 0.0, WaterForRunoff, "runoff");
            }








        private void soilwat2_runoff_depth_factor(SoilWaterSoil SoilObject, ref double[] runoff_wf)
            {

            //runoff_wf -> ! (OUTPUT) weighting factor for runoff

            //*+  Purpose
            //*      Calculate the weighting factor hydraulic effectiveness used
            //*      to weight the effect of soil moisture on runoff.

            //*+  Mission Statement
            //*      Calculate soil moisture effect on runoff      

  
            double cum_depth;                 //! cumulative depth (mm)
            double hydrol_effective_depth_local;    //! hydrologically effective depth for runoff (mm) - for when erosion turned on  
            int hydrol_effective_layer;    //! layer number that the effective depth occurs in ()

            double scale_fact;                //! scaling factor for wf function to sum to 1
            double wf_tot;                    //! total of wf ()
            double wx;                        //! depth weighting factor for current total depth. intermediate variable for deriving wf (total wfs to current layer)
            double xx;                        //! intermediate variable for deriving wf total wfs to previous layer

            xx = 0.0;
            cum_depth = 0.0;
            wf_tot = 0.0;


            //! check if hydro_effective_depth applies for eroded profile.
            hydrol_effective_depth_local = Math.Min(cons.hydrol_effective_depth, SoilObject.DepthTotal);

            scale_fact = 1.0 / (1.0 - Math.Exp(-4.16));
            hydrol_effective_layer = SoilObject.FindLayerNo(hydrol_effective_depth_local);

            foreach (Layer lyr in SoilObject.TopToX(hydrol_effective_layer))
                {
                cum_depth = cum_depth + lyr.dlayer;
                cum_depth = Math.Min(cum_depth, hydrol_effective_depth_local);

                //! assume water content to c%hydrol_effective_depth affects runoff
                //! sum of wf should = 1 - may need to be bounded? <dms 7-7-95>
                wx = scale_fact * (1.0 - Math.Exp(-4.16 * Utility.Math.Divide(cum_depth, hydrol_effective_depth_local, 0.0)));
                runoff_wf[lyr.number-1] = wx - xx;  //zero based array
                xx = wx;

                wf_tot = wf_tot + runoff_wf[lyr.number-1]; //zero based array.
                }

            cons.bound_check_real_var(wf_tot, 0.9999, 1.0001, "wf_tot");
            }

        #endregion













        }
    }
