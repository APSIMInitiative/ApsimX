using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SoilWaterBackend
    {

    public enum Surfaces { NormalSurface, PondSurface };


    [Serializable]
    public class Surface
        {

        public Surfaces SurfaceType;

        public double Runoff;
        public double Infiltration;
        public double Eo {get; set;}  //external modules might set this value. 
        public double Eos;
        public double Es;

        public virtual void CalcRunoff(){}
        public virtual void CalcInfiltration(){}
        public virtual void CalcEvaporation() {}


        //nb. Use the methods below rather then just using a SoilObject method to add infiltration or remove water,
        //    because we might want to create Surfaces such as cracking clays that adds infiltration to not just
        //    the top layer but multiple layers. Also these surfaces might want to remove evaporation from 
        //    multiple layers and not just the top layer. They will need logic to decide what fractions of
        //    infiltration or evaporation that they want to remove from which layers.
        public virtual void AddInfiltrationToSoil(ref SoilWaterSoil SoilOject) { }
        public virtual void RemoveEvaporationFromSoil(ref SoilWaterSoil SoilObject) { }

        public virtual void AddBackedUpWaterToSurface(double BackedUp, ref SoilWaterSoil SoilObject) { }


        internal Constants constants;


        //The following below need to be updated each day.
        //they are used as parmeters in the methods above.
        public Clock Clock;
        public MetData Met;
        public double Runon;
        public IrrigData Irrig;
        public CanopyData Canopy;
        public SurfaceCoverData SurfaceCover;
        public SoilWaterSoil SoilObject;


        public double TodaysWaterForRunoff
            {
            get
                {
                //(interception + residueinterception) was created by Hamish Brown. Used in his manager scripts only.

                //! NIH Need to consider if interception losses were already considered in runoff model calibration

                double waterForRunoff = Met.rain + Runon - (Canopy.interception + SurfaceCover.residueinterception);
   
                if (Irrig.irrigation_will_runoff)
                    waterForRunoff = waterForRunoff + Irrig.irrigation;

                return waterForRunoff;
                }
            }


        public double TodaysWaterForSurface
            {
            get
                {
                double waterForSurface = TodaysWaterForRunoff;

                //if irrigations don't runoff and this is surface irrigation (not subsurface)
                if ((!Irrig.irrigation_will_runoff) && (Irrig.irrigation_layer == 1))
                    waterForSurface = TodaysWaterForRunoff + Irrig.irrigation;

                return waterForSurface;
                }

            }



        }




    [Serializable]
    public class SurfaceFactory 
        {

        public Surface GetSurface(SoilWaterSoil SoilObject)
            {
            Surface surface;

            if (SoilObject.max_pond <= 0.0)
                surface = new NormalSurface(SoilObject);
            else
                surface = new PondSurface(SoilObject);

            return surface;
            }

        }




    [Serializable]
    public class NormalSurface : Surface
        {

        //OUTPUTS

        //Runoff
        public double cover_surface_runoff;
        public double cn2_new;


        //Evaporation

        public double sumes1;       //! cumulative soil evaporation in stage 1 (mm)
        public double sumes2;       //! cumulative soil evaporation in stage 2 (mm)
        public double t;            //! time after 2nd-stage soil evaporation begins (d)




        internal NormalRunoff runoff;    //let derived PondSurface see this but not outside world.
        internal NormalEvaporation evap;


        public NormalSurface(SoilWaterSoil SoilObject)
            {
            SurfaceType = Surfaces.NormalSurface;
            base.constants = SoilObject.Constants;
            runoff = new NormalRunoff(SoilObject);     //Soil is needed to initialise the cn2bare, etc. 
            evap = new NormalEvaporation(SoilObject);
            }




        public override void CalcRunoff()
            {
            runoff.CalcCoverForRunoff(base.Canopy, base.SurfaceCover);
            runoff.CalcRunoff(base.TodaysWaterForRunoff, base.SoilObject);

            cover_surface_runoff = runoff.cover_surface_runoff;
            cn2_new = runoff.cn2_new;
            Runoff = runoff.Runoff; 
            }



        public override void CalcInfiltration()
            {
            Infiltration = base.TodaysWaterForSurface - Runoff;
            }



        public override void CalcEvaporation()
            {

            evap.CalcEo_AtmosphericPotential(base.Met, base.Canopy);
            Eo = evap.Eo;

            evap.CalcEos_EoReducedDueToShading(base.Canopy, base.SurfaceCover);
            Eos = evap.Eos;

            evap.CalcEs_RitchieEq_LimitedBySW(base.SoilObject, base.Clock, Infiltration);
            Es = evap.Es;

            }



        public override void AddInfiltrationToSoil(ref SoilWaterSoil SoilObject)
            {
            Layer top = SoilObject.GetTopLayer();
            top.sw_dep = top.sw_dep + Infiltration;
            }

        public override void RemoveEvaporationFromSoil(ref SoilWaterSoil SoilObject)
            {
            Layer top = SoilObject.GetTopLayer();
            top.sw_dep = top.sw_dep - Es;
            }

        public override void AddBackedUpWaterToSurface(double BackedUp, ref SoilWaterSoil SoilObject)
            {
            //If Infiltration was more water that the top layer of soil had empty then the extra water had no choice but to back up. 
            //In this case turn the backed up water into runoff and reduce the infiltration to only what the top layer could take before backing up.
            //The amount the top layer can take must be equal to the infiltration - backedup amount. 

            //nb. What if lateral inflow caused it to back up? We are assuming only source of water into top layer is infiltration from surface.


            //remove backed up amount from the top layer of the soil. (All of the infiltration did not infiltrate)
            Layer top = SoilObject.GetTopLayer();
            top.sw_dep = top.sw_dep - BackedUp;

            //now reduce the infiltration amount by what backed up.
            base.Infiltration = base.Infiltration - BackedUp;

            //turn the proportion of the infiltration that backed up into runoff.
            base.Runoff = base.Runoff + BackedUp;

            }


        public void UpdateTillageCnRedVars(double CumWater, double Reduction)
            {
            runoff.tillageCnCumWater = CumWater;
            runoff.tillageCnRed = Reduction;
            }

        public void ResetCumWaterSinceTillage()
            {
            runoff.cumWaterSinceTillage = 0.0;
            }



        }





    [Serializable]
    public class PondSurface : NormalSurface
        {


        public double pond;
        public double pond_evap;
        




        public PondSurface(SoilWaterSoil SoilObject):base(SoilObject)
            {
            base.SurfaceType = Surfaces.PondSurface;
            pond = 0.0;
            pond_evap = 0.0;
            }




        public override void CalcRunoff()
            {


            base.CalcRunoff();

            pond = pond + base.Runoff;
            base.Runoff = Math.Max((pond - SoilObject.max_pond), 0.0);
            pond = Math.Min(pond, SoilObject.max_pond);

            }



        public override void CalcInfiltration()
            {

            base.CalcInfiltration();

            //infiltrate all of the pond each day. Let the soil work out how much of it backs up again to work out the pond each day.
            base.Infiltration = base.Infiltration + pond;
            pond = 0.0;   

            }



        public override void CalcEvaporation()
            {

            evap.CalcEo_AtmosphericPotential(base.Met, base.Canopy);
            Eo = evap.Eo;

            evap.CalcEos_EoReducedDueToShading(base.Canopy, base.SurfaceCover);
            Eos = evap.Eos;


            //! dsg 270502  check to see if there is any ponding.  If there is, evaporate any potential (g%eos) straight out of it and transfer
            //!             any remaining potential to the soil layer 1, as per usual.  Introduce new term g%pond_evap
            //!             which is the daily evaporation from the pond.

            //TODO: need to figure out what to do about Es and cumulative variable sumes1, sumes2, t etc. Maybe zero and reset each day when pond is present.
            //      Don Gaydon has flagged this as a bug in the current soilwat that he wants fixed.

            if (pond > 0.0)
                {

                //Set the cumulative variables for the NormalEvaporation to zero.
                //they don't make sense when a pond exists.
                base.sumes1 = Double.NaN;
                base.sumes2 = Double.NaN;
                base.t = Double.NaN;


                //If today's Pot Evap less than the Pond
                if (Eos <= pond)
                    {
                    pond = pond - Eos;    //just decrease the pond by the amount of soil evaporation.
                    pond_evap = Eos;      //Pond evaporates at the Pot Evap rate

                    Es = 0.0;      //Pond reports the evaporation using evap_pond not Es. (must be 0.0 not NaN because RemoveEvaporationFromSoil() will crash)
                    }

                //today's Pot Evap was greater than the Pond 
                else
                    {
                    pond_evap = pond;   //evaporate the entire pond.
                    Eos = Eos - pond;   //work out the remaining pot evaporation for the soil to now work out it's Es.
                    pond = 0.0;         //no pond left.
                    
                    //calculate Es using altered Eos.
                    evap.Eos = Eos;
                    base.evap.InitialiseAccumulatingVars(SoilObject); //Reinitialise the Accumulating variables for the normal surface evaporation;
                    evap.CalcEs_RitchieEq_LimitedBySW(base.SoilObject, base.Clock, Infiltration);
                    Es = evap.Es;
                    
                    }

                }
            else
                {
                pond_evap = 0.0;
                pond = 0.0;

                //work out Es as you would for a NormalSurface
                evap.CalcEs_RitchieEq_LimitedBySW(base.SoilObject, base.Clock, Infiltration);
                Es = evap.Es;
                }



            }




        public override void AddBackedUpWaterToSurface(double BackedUp, ref SoilWaterSoil SoilObject)
            {
            //do normal surface add backup to surface
            base.AddBackedUpWaterToSurface(BackedUp, ref SoilObject);

            //Any extra_runoff then it becomes a pond. 
            pond = Math.Min(base.Runoff, SoilObject.max_pond);

            //If there is too much for the pond handle then add the excess to normal runoff.
            base.Runoff = base.Runoff - pond;
            }



        }











    }
