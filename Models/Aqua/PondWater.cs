using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Xml.Serialization;
using System.Runtime.Serialization;
using Models;
using Models.Core;

using Models.Aqua;

//nb. when adding a NEW model you need to add it as a child model in Models.cs (under "Core" folder) (otherwise it just ignores the xml)

namespace Models
    {

    ///<summary>
    /// Aquaculture Pond 
    /// Maintains a balance in the Pond for Water Quantity, Salinity, Temperature, PH, Total Nitrogen (TN), Total Phosphorus (TP), Total Suspended Solutes (TSS).
    /// See "Simulation of temperature and salinity in a fully mixed pond" by Jiacai Gao & Nooel P. Merrick, 2007
    /// Created by Shaun Verrall 15 Jan 2015
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PondWater : Model
        {

        #region Links


        /// <summary>The clock</summary>
        [Link]
        private Clock Clock = null;


        /// <summary>The weather</summary>
        [Link]
        private Weather Weather;


        //[Link]
        //private MicroClimate MicroClim; //added for fr_intc_radn_ , but don't know what the corresponding variable is in MicroClimate.



        /// <summary>The summary</summary>
        [Link]
        private ISummary Summary = null;


        #endregion


        //MODULE CONSTANTS

        #region Constants


        [Description("Pond Surface Area (m^2)")]
        [Units("(m^2)")]
        public double SurfaceArea { get; set; }

        [Description("Maximum Pond Depth (m)")]
        [Units("(m)")]
        public double MaxPondDepth { get; set; }


        #endregion



        //CONSTRUCTOR

        #region Constructor


        public PondWater()
            {
            //Initialise the Optional Params in the XML

            }


        #endregion




        //OUTPUTS


        #region Outputs



        [Units("(mm)")]
        [XmlIgnore]
        public double PondEvap
            {
            get { return pondEvap; }
            }

        [Units("(m)")]
        [XmlIgnore]
        public double PondDepth 
            {
            get { return currentVolume / SurfaceArea; } //TODO: watch out for a divide by zero exception. 
            }


        [XmlIgnore]
        public WaterProperties PondProps
            {
            get 
                {
                //have to create and return a new instance because otherwise, it will just return a reference.
                WaterProperties result = new WaterProperties(pondProps.Temperature, pondProps.Salinity, pondProps.PH, pondProps.N, pondProps.P, pondProps.TSS);
                return result;
                }
            }


        [Units("(oC)")]
        [XmlIgnore]
        public double PondTemp 
            { 
            get { return pondProps.Temperature; } 
            }

        [Units("(kg/m^3)")]
        [XmlIgnore]
        public double Salinity
            {
            get { return pondProps.Salinity; }
            }


        [Units("(kg/m^3)")]
        [XmlIgnore]
        public double PH
            {
            get { return pondProps.PH; }
            }

        [Units("(kg/m^3)")]
        [XmlIgnore]
        public double N
            {
            get { return pondProps.N; }
            }

        [Units("(kg/m^3)")]
        [XmlIgnore]
        public double P
            {
            get { return pondProps.P; }
            }

        [Units("(kg/m^3)")]
        [XmlIgnore]
        public double TSS
            {
            get { return pondProps.TSS; }
            }


        #endregion



        //LOCAL VARIABALES


        #region local variables

        double mm2m = 0.001;  //convert mm to meters
        double m2mm = 1000;    

        double currentVolume = 0;

        WaterProperties pondProps = new WaterProperties(0, 0, 0, 0, 0, 0);

        double pondEvap;

        #endregion




        //MODEL


        #region Add Water to Pond


        private void AddWater(double Volume, WaterProperties addedProps)
            {

            //This logic is valid because people can not overfill their ponds in the real world
            //because the water comes from a river by opening up the gates and letting gravity fill the pond.
            //Therefore you can not overfill it because the water level just becomes equal to the river.
            //Not sure about filling due to pumping the water though.

            //If it overfills due to rain, then because saltwater is more dense than rain water
            //the rainwater just runs straight off without mixing with the pond water. 
            //Therefore no solutes are lost due to this runoff.

            double maxPondVolume = SurfaceArea * MaxPondDepth;

            if (currentVolume + Volume > maxPondVolume)
                {
                Volume = maxPondVolume - currentVolume;
                Summary.WriteWarning(this, "You have overfilled the pond. Reduced the volume added to volume the pond could accept");
                }

            AddWater_TempChange(Volume, addedProps.Temperature);
            AddWater_SalinityChange(Volume, addedProps.Salinity);
            AddWater_PHChange(Volume, addedProps.PH);
            AddWater_NChange(Volume, addedProps.N);
            AddWater_PChange(Volume, addedProps.P);
            AddWater_TSSChange(Volume, addedProps.TSS);
            //Add DOX

            currentVolume = currentVolume + Volume;
            }


        private void AddWater_TempChange(double Volume, double Temp)
            {
            pondProps.Temperature = WeightedAverage(currentVolume, pondProps.Temperature, Volume, Temp);
            //do some boundary checks and warn the user if too high or low.
            }


        private void AddWater_SalinityChange(double Volume, double Salinity)
            {
            pondProps.Salinity = WeightedAverage(currentVolume, pondProps.Salinity, Volume, Salinity);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_PHChange(double Volume, double PH)
            {
            pondProps.PH = WeightedAverage(currentVolume, pondProps.PH, Volume, PH);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_NChange(double Volume, double N)
            {
            pondProps.N = WeightedAverage(currentVolume, pondProps.N, Volume, N);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_PChange(double Volume, double P)
            {
            pondProps.P = WeightedAverage(currentVolume, pondProps.P, Volume, P);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_TSSChange(double Volume, double TSS)
            {
            pondProps.TSS = WeightedAverage(currentVolume, pondProps.TSS, Volume, TSS);
            //do some boundary checks and warn the user if too high or low.
            }


        /// <summary>
        /// Do a weighted average.
        /// Useful when mixing two different volumes of water, and you want to know the resulting concentrations of water solutes. 
        /// </summary>
        private double WeightedAverage(double Volume1, double PerVolume1, double Volume2, double PerVolume2)
            {
            double result;
            result = ((Volume1 * PerVolume1) + (Volume2 * PerVolume2)) / (Volume1 + Volume2);
            return result;
            }


        #endregion



        #region Remove Water from Pond


        /// <summary>
        ///Remove water from the Pond.
        ///nb. There is no change in Temperature, Salinity, etc. as a consequence of removing water.
        ///Only the volume of water is changed. None of the Water's properties are affected.
        /// </summary>
        /// <param name="Volume"></param>
        private void RemoveWater(double Volume)
            {
            if (currentVolume - Volume < 0)
                {
                Volume = currentVolume;
                Summary.WriteWarning(this, "You have tried to remove more water than the pond contained. Reduced the volume removed to current volume of the pond. The pond is now empty.");
                }
            currentVolume = currentVolume - Volume;
            }



        #endregion




        #region Evaporate Water from Pond


        private void EvaporateWater(double Amount_mm)
            {
            double volume = SurfaceArea * (Amount_mm * mm2m);
            if (currentVolume - volume < 0)
                {
                volume = currentVolume;
                Summary.WriteWarning(this, "You have evaporated all the water left in the pond. The pond is now empty.");
                }

            EvaporateWater_SalinityChange(volume);
            EvaporateWater_NChange(volume);
            EvaporateWater_PChange(volume);
            EvaporateWater_TSSChange(volume);

            currentVolume = currentVolume - volume;
            pondEvap = (volume / SurfaceArea) * m2mm; //TODO: watch out for divide by zero error.
            }

        //No Temperature Change due to evaporation -> at least not yet.

        private void EvaporateWater_SalinityChange(double Volume)
            {
            pondProps.Salinity = NewPerVolumeDueToEvaporation(currentVolume, pondProps.Salinity, Volume);
            //do some boundary checks and warn the user if too high or low.
            }

        //No PH Change due to evaporation.

        private void EvaporateWater_NChange(double Volume)
            {
            pondProps.N = NewPerVolumeDueToEvaporation(currentVolume, pondProps.N, Volume);
            //do some boundary checks and warn the user if too high or low.
            }

        private void EvaporateWater_PChange(double Volume)
            {
            pondProps.P = NewPerVolumeDueToEvaporation(currentVolume, pondProps.P, Volume);
            //do some boundary checks and warn the user if too high or low.
            }

        private void EvaporateWater_TSSChange(double Volume)
            {
            pondProps.TSS = NewPerVolumeDueToEvaporation(currentVolume, pondProps.TSS, Volume);
            //do some boundary checks and warn the user if too high or low.
            }


        private double NewPerVolumeDueToEvaporation(double CurrentVolume, double CurrentPerVolume, double VolumeRemoved)
            {
            double currentAmount;
            double newPerVolume;

            currentAmount = currentVolume * CurrentPerVolume;
            newPerVolume = currentAmount / (currentVolume - VolumeRemoved);  //TODO: if the user fully evaporates the pond then the newPerVolume goes to infinity because you have a divide by zero

            return newPerVolume;
            }


        #endregion



        #region Calculate Evaporation


        //public void CalcEo_AtmosphericPotential(MetData Met, CanopyData Canopy)
        //    {
        //    //Get Eo and assign it to the public Eo field for this object.

        //    //private void soilwat2_priestly_taylor()
        //    //   {
        //    double albedo;           //! albedo taking into account plant material

        //    double eeq;              //! equilibrium evaporation rate (mm)
        //    double wt_ave_temp;      //! weighted mean temperature for the day (oC)

        //    //*  ******* calculate potential evaporation from soil surface (eos) ******

        //    //                ! find equilibrium evap rate as a
        //    //                ! function of radiation, albedo, and temp.


        //    albedo = cons.max_albedo - (cons.max_albedo - salb) * (1.0 - Canopy.cover_green_sum);

        //    // ! wt_ave_temp is mean temp, weighted towards max.
        //    wt_ave_temp = (0.60 * Met.maxt) + (0.40 * Met.mint);

        //    eeq = Met.radn * 23.8846 * (0.000204 - 0.000183 * albedo) * (wt_ave_temp + 29.0);

        //    //! find potential evapotranspiration (eo) from equilibrium evap rate
        //    Eo = eeq * soilwat2_eeq_fac(Met);
        //    //    }
        //    }


        //private double soilwat2_eeq_fac(MetData Met)
        //    {
        //    //*+  Mission Statement
        //    //*     Calculate the Equilibrium Evaporation Rate

        //    if (Met.maxt > cons.max_crit_temp)
        //        {
        //        //! at very high max temps eo/eeq increases
        //        //! beyond its normal value of 1.1
        //        return ((Met.maxt - cons.max_crit_temp) * 0.05 + 1.1);
        //        }
        //    else
        //        {
        //        if (Met.maxt < cons.min_crit_temp)
        //            {
        //            //! at very low max temperatures eo/eeq
        //            //! decreases below its normal value of 1.1
        //            //! note that there is a discontinuity at tmax = 5
        //            //! it would be better at tmax = 6.1, or change the
        //            //! .18 to .188 or change the 20 to 21.1
        //            return (0.01 * Math.Exp(0.18 * (Met.maxt + 20.0)));
        //            }
        //        }

        //    return 1.1;  //sv- normal value of eeq fac (eo/eeq)
        //    }





        #endregion



        //EVENT HANDLERS

        #region Clock Event Handlers


        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
            {


            }




        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
            {


            }


        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
            {



            }

        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
            {

            WaterProperties rainProps = new WaterProperties(Weather.Tav, 0, 7.0, 0, 0, 0);
            AddWater(SurfaceArea * (Weather.Rain * mm2m), rainProps);

            EvaporateWater(2);    //Just evaporate 2mm per day since we have not implemented evaporation calculation or read in Eo from met file or Micromet.
            }



        #endregion





        //MANAGER COMMANDS

        #region Manager Commands


        public void Fill(double Volume, WaterProperties WaterProperties)
            {
            AddWater(Volume, WaterProperties);
            }


        public void Fill(double Volume, double WaterTemp, double Salinity, double PH, double N, double P, double TSS)
            {
            WaterProperties addedProps = new WaterProperties(WaterTemp, Salinity, PH, N, P, TSS);
            AddWater(Volume, addedProps);
            }


        public void Empty(double Volume)
            {
            RemoveWater(Volume);
            }

        #endregion


        }


    }
