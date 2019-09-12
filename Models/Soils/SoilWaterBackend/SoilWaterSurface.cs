using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.SoilWaterBackend
    {

    /// <summary>
    /// Surfaces enumeration
    /// </summary>
    public enum Surfaces 
    {
        /// <summary>
        /// The normal surface
        /// </summary>
        NormalSurface,

        /// <summary>
        /// The pond surface
        /// </summary>
        PondSurface 
    };


    /// <summary>
    /// A surface class.
    /// </summary>
    [Serializable]
    public class Surface
        {

        /// <summary>
        /// The surface type
        /// </summary>
        public Surfaces SurfaceType;

        /// <summary>
        /// The runoff
        /// </summary>
        public double Runoff;
        /// <summary>
        /// The infiltration
        /// </summary>
        public double Infiltration;
        /// <summary>
        /// Gets or sets the eo.
        /// </summary>
        /// <value>
        /// The eo.
        /// </value>
        public double Eo {get; set;}  //external modules might set this value. 
        /// <summary>
        /// The eos
        /// </summary>
        public double Eos;
        /// <summary>
        /// The es
        /// </summary>
        public double Es;

        /// <summary>
        /// Calculates the runoff.
        /// </summary>
        public virtual void CalcRunoff(){}
        /// <summary>
        /// Calculates the infiltration.
        /// </summary>
        public virtual void CalcInfiltration(){}
        /// <summary>
        /// Calculates the evaporation.
        /// </summary>
        public virtual void CalcEvaporation(double Eo) {}


        //nb. Use the methods below rather then just using a SoilObject method to add infiltration or remove water,
        //    because we might want to create Surfaces such as cracking clays that adds infiltration to not just
        //    the top layer but multiple layers. Also these surfaces might want to remove evaporation from 
        //    multiple layers and not just the top layer. They will need logic to decide what fractions of
        //    infiltration or evaporation that they want to remove from which layers.
        /// <summary>
        /// Adds the infiltration to soil.
        /// </summary>
        /// <param name="SoilOject">The soil oject.</param>
        public virtual void AddInfiltrationToSoil(ref SoilWaterSoil SoilOject) { }
        /// <summary>
        /// Removes the evaporation from soil.
        /// </summary>
        /// <param name="SoilObject">The soil object.</param>
        public virtual void RemoveEvaporationFromSoil(ref SoilWaterSoil SoilObject) { }

        /// <summary>
        /// Adds the backed up water to surface.
        /// </summary>
        /// <param name="BackedUp">The backed up.</param>
        /// <param name="SoilObject">The soil object.</param>
        public virtual void AddBackedUpWaterToSurface(double BackedUp, ref SoilWaterSoil SoilObject) { }


        /// <summary>
        /// The constants
        /// </summary>
        internal Constants constants;


        //The following below need to be updated each day.
        //they are used as parmeters in the methods above.
        /// <summary>
        /// The clock
        /// </summary>
        public IClock Clock;
        /// <summary>
        /// The met
        /// </summary>
        public MetData Met;
        /// <summary>
        /// The runon
        /// </summary>
        public double Runon;
        /// <summary>
        /// The irrig
        /// </summary>
        public List<IrrigData> Irrig;
        /// <summary>
        /// The canopy
        /// </summary>
        public CanopyData Canopy;
        /// <summary>
        /// The surface cover
        /// </summary>
        public SurfaceCoverData SurfaceCover;
        /// <summary>
        /// The soil object
        /// </summary>
        public SoilWaterSoil SoilObject;


        /// <summary>
        /// Gets the todays water for runoff.
        /// </summary>
        /// <value>
        /// The todays water for runoff.
        /// </value>
        public double TodaysWaterForRunoff
            {
            get
                {
                //(interception + residueinterception) was created by Hamish Brown. Used in his manager scripts only.

                //! NIH Need to consider if interception losses were already considered in runoff model calibration

                double waterForRunoff = Canopy.PotentialInfiltration + Runon;

                foreach (IrrigData irrData in Irrig)
                {
                    if (irrData.willRunoff)
                        waterForRunoff += irrData.amount;
                }
                return waterForRunoff;
                }
            }


        /// <summary>
        /// Gets the todays water for infiltration.
        /// </summary>
        /// <value>
        /// The todays water for infiltration.
        /// </value>
        public double TodaysWaterForInfiltration
            {
            get
                {
                double waterForInfiltration = TodaysWaterForRunoff;

                //if irrigation was not included in TodaysWaterForRunoff (because will_runoff was false)
                //and this is surface irrigation (not subsurface) then it needs to be included in the TodaysWaterForInfiltration.
                foreach (IrrigData irrData in Irrig)
                {
                    if ((!irrData.willRunoff) && (irrData.isSubSurface == false))
                        waterForInfiltration += irrData.amount;
                }
                return waterForInfiltration;
                }

            }



        }




    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SurfaceFactory 
        {

            /// <summary>
            /// Gets the surface.
            /// </summary>
            /// <param name="SoilObject">The soil object.</param>
            /// <param name="Clock">The clock.</param>
            /// <returns></returns>
        public Surface GetSurface(SoilWaterSoil SoilObject, IClock Clock)
            {
            Surface surface;

            if (SoilObject.max_pond <= 0.0)
                surface = new NormalSurface(SoilObject, Clock);
            else
                surface = new PondSurface(SoilObject, Clock);

            return surface;
            }

        }




    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class NormalSurface : Surface
        {

        //OUTPUTS

        //Runoff
            /// <summary>
            /// The cover_surface_runoff
            /// </summary>
        public double cover_surface_runoff;
        /// <summary>
        /// The cn2_new
        /// </summary>
        public double cn2_new;


        //Evaporation

        /// <summary>
        /// The sumes1
        /// </summary>
        public double sumes1;       //! cumulative soil evaporation in stage 1 (mm)
        /// <summary>
        /// The sumes2
        /// </summary>
        public double sumes2;       //! cumulative soil evaporation in stage 2 (mm)
        /// <summary>
        /// The t
        /// </summary>
        public double t;            //! time after 2nd-stage soil evaporation begins (d)




        /// <summary>
        /// The runoff
        /// </summary>
        internal NormalRunoff runoff;    //let derived PondSurface see this but not outside world.
        /// <summary>
        /// The evap
        /// </summary>
        internal NormalEvaporation evap;


        /// <summary>
        /// Initializes a new instance of the <see cref="NormalSurface"/> class.
        /// </summary>
        /// <param name="SoilObject">The soil object.</param>
        /// <param name="Clock">The clock.</param>
        public NormalSurface(SoilWaterSoil SoilObject, IClock Clock)
            {
            SurfaceType = Surfaces.NormalSurface;
            base.constants = SoilObject.Constants;
            runoff = new NormalRunoff(SoilObject);     //Soil is needed to initialise the cn2bare, etc. 
            evap = new NormalEvaporation(SoilObject, Clock);
            }




        /// <summary>
        /// Calculates the runoff.
        /// </summary>
        public override void CalcRunoff()
            {
            runoff.CalcCoverForRunoff(base.Canopy, base.SurfaceCover);
            runoff.CalcRunoff(base.TodaysWaterForRunoff, base.SoilObject);

            cover_surface_runoff = runoff.cover_surface_runoff;
            cn2_new = runoff.cn2_new;
            Runoff = runoff.Runoff; 
            }



        /// <summary>
        /// Calculates the infiltration.
        /// </summary>
        public override void CalcInfiltration()
            {
            Infiltration = base.TodaysWaterForInfiltration - Runoff;  //remove the runoff because it did not infiltrate.
            }



        /// <summary>
        /// Calculates the evaporation.
        /// </summary>
        public override void CalcEvaporation(double Eo)
            {

            evap.CalcEos_EoReducedDueToShading(Eo, base.Canopy, base.SurfaceCover);
            Eos = evap.Eos;

            evap.CalcEs_RitchieEq_LimitedBySW(Eo, base.SoilObject, base.Clock, Infiltration);
            Es = evap.Es;
            t = evap.t;
            }



        /// <summary>
        /// Adds the infiltration to soil.
        /// </summary>
        /// <param name="SoilObject">The soil object.</param>
        public override void AddInfiltrationToSoil(ref SoilWaterSoil SoilObject)
            {
            Layer top = SoilObject.GetTopLayer();
            top.sw_dep = top.sw_dep + Infiltration;
            }

        /// <summary>
        /// Removes the evaporation from soil.
        /// </summary>
        /// <param name="SoilObject">The soil object.</param>
        public override void RemoveEvaporationFromSoil(ref SoilWaterSoil SoilObject)
            {
            Layer top = SoilObject.GetTopLayer();
            top.sw_dep = top.sw_dep - Es;
            }

        /// <summary>
        /// Adds the backed up water to surface.
        /// </summary>
        /// <param name="BackedUp">The backed up.</param>
        /// <param name="SoilObject">The soil object.</param>
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


        /// <summary>
        /// Updates the tillage cn red vars.
        /// </summary>
        /// <param name="CumWater">The cum water.</param>
        /// <param name="Reduction">The reduction.</param>
        public void UpdateTillageCnRedVars(double CumWater, double Reduction)
            {
            runoff.tillageCnCumWater = CumWater;
            runoff.tillageCnRed = Reduction;
            }

        /// <summary>
        /// Resets the cum water since tillage.
        /// </summary>
        public void ResetCumWaterSinceTillage()
            {
            runoff.cumWaterSinceTillage = 0.0;
            }



        }





    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PondSurface : NormalSurface
        {


            /// <summary>
            /// The pond
            /// </summary>
        public double pond;
        /// <summary>
        /// The pond_evap
        /// </summary>
        public double pond_evap;





        /// <summary>
        /// Initializes a new instance of the <see cref="PondSurface"/> class.
        /// </summary>
        /// <param name="SoilObject">The soil object.</param>
        /// <param name="Clock">The clock.</param>
        public PondSurface(SoilWaterSoil SoilObject, IClock Clock):base(SoilObject, Clock)
            {
            base.SurfaceType = Surfaces.PondSurface;
            pond = 0.0;
            pond_evap = 0.0;
            }




        /// <summary>
        /// Calculates the runoff.
        /// </summary>
        public override void CalcRunoff()
            {


            base.CalcRunoff();  //do NormalSurface runoff

            pond = pond + base.Runoff;
            base.Runoff = Math.Max((pond - SoilObject.max_pond), 0.0);
            pond = Math.Min(pond, SoilObject.max_pond);

            }



        /// <summary>
        /// Calculates the infiltration.
        /// </summary>
        public override void CalcInfiltration()
            {

            base.CalcInfiltration();  //do NormalSurface infiltration

            //infiltrate all of the pond each day. Let the soil work out how much of it backs up again to work out the pond each day.
            base.Infiltration = base.Infiltration + pond;
            pond = 0.0;   

            }



        /// <summary>
        /// Calculates the evaporation.
        /// </summary>
        public override void CalcEvaporation(double Eo)
            {

            evap.CalcEos_EoReducedDueToShading(Eo, base.Canopy, base.SurfaceCover);
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
                    evap.InitialiseAccumulatingVars(base.SoilObject, base.Clock); //Reinitialise the Accumulating variables for the normal surface evaporation;
                    evap.CalcEs_RitchieEq_LimitedBySW(Eo, base.SoilObject, base.Clock, Infiltration);
                    Es = evap.Es;
                    t = evap.t;
                    }

                }
            else
                {
                pond_evap = 0.0;
                pond = 0.0;

                //work out Es as you would for a NormalSurface
                evap.CalcEs_RitchieEq_LimitedBySW(Eo, base.SoilObject, base.Clock, Infiltration);
                Es = evap.Es;
                t = evap.t;
                }



            }




        /// <summary>
        /// Adds the backed up water to surface.
        /// </summary>
        /// <param name="BackedUp">The backed up.</param>
        /// <param name="SoilObject">The soil object.</param>
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
