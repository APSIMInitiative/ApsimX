using System;
using Models.Core;
using Models.Climate;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;

namespace Models.Aqua
    {

    ///<summary>
    /// Aquaculture Pond Water.
    /// Maintains a water balance in the Pond.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PondWater : Model
        {
        //nb. when adding a NEW model you need to add it as a child model in Models.cs (under "Core" folder) (otherwise it just ignores the xml)

        //See "Simulation of temperature and salinity in a fully mixed pond" by Jiacai Gao and Nooel P. Merrick, 2007

        #region Links


        ///// <summary>The clock</summary>
        //[Link]
        //private IClock Clock = null;


        /// <summary>The weather</summary>
        [Link]
        private Weather Weather = null;


        //[Link]
        //private MicroClimate MicroClim; //added for fr_intc_radn_ , but don't know what the corresponding variable is in MicroClimate.



        /// <summary>The summary</summary>
        [Link]
        private ISummary Summary = null;


        #endregion


        //MODULE CONSTANTS

        #region Constants

        /// <summary>
        /// Suface Area of the Pond (m^2)
        /// </summary>
        [Description("Pond Surface Area (m^2)")]
        [Units("(m^2)")]
        public double SurfaceArea { get; set; }

        /// <summary>
        /// Maximum Pond Depth (m)
        /// </summary>
        [Description("Maximum Pond Depth (m)")]
        [Units("(m)")]
        public double MaxPondDepth { get; set; }

        /// <summary>
        /// Kpan - Coefficient applied to PanEvap to give PondEvap
        /// </summary>
        [Description("Kpan - Coefficient applied to PanEvap to give PondEvap")]
        [Units("()")]
        public double Kpan { get; set; }


        #endregion



        //CONSTRUCTOR

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public PondWater()
            {
            //Initialise the Optional Params in the XML

            }


        #endregion




        //OUTPUTS


        #region Outputs


        /// <summary>
        /// Evaporation from the Pond (mm)
        /// </summary>
        [Units("(mm)")]
        [JsonIgnore]
        public double PondEvap
            {
            get { return pondEvap; }
            }

        /// <summary>
        /// Current Depth Water in the Pond (m)
        /// </summary>
        [Units("(m)")]
        [JsonIgnore]
        public double PondDepth
            {
            get { return pondVolume / SurfaceArea; } //TODO: watch out for a divide by zero exception.
            }


        /// <summary>
        /// Current Properties of any given volume/amount of water in the Pond.
        /// Used when mixing water together or evaporating water.
        /// </summary>
        [JsonIgnore]
        public WaterProperties PondProps
            {
            get
                {
                //have to create and return a new instance because otherwise, it will just return a reference.
                WaterProperties result = new WaterProperties(pondProps.Temperature, pondProps.Salinity, pondProps.PH, pondProps.N, pondProps.P, pondProps.TSS);
                return result;
                }
            }


        /// <summary>
        /// Temperature of the water in the Pond (oC)
        /// </summary>
        [Units("(oC)")]
        [JsonIgnore]
        public double PondTemp
            {
            get { return pondProps.Temperature; }
            }

        /// <summary>
        /// Salinity of the water in the Pond (kg/m^3)
        /// </summary>
        [Units("(kg/m^3)")]
        [JsonIgnore]
        public double Salinity
            {
            get { return pondProps.Salinity; }
            }


        /// <summary>
        /// PH of the water in the Pond
        /// </summary>
        [Units("()")]
        [JsonIgnore]
        public double PH
            {
            get { return pondProps.PH; }
            }


        /// <summary>
        /// Nitrogen in the water in the Pond (kg/m^3)
        /// </summary>
        [Units("(kg/m^3)")]
        [JsonIgnore]
        public double N
            {
            get { return pondProps.N; }
            }

        /// <summary>
        /// Phosphorus in the water in the Pond (kg/m^3)
        /// </summary>
        [Units("(kg/m^3)")]
        [JsonIgnore]
        public double P
            {
            get { return pondProps.P; }
            }

        /// <summary>
        /// Total Suspended Soild in the water in the Pond (kg/m^3)
        /// </summary>
        [Units("(kg/m^3)")]
        [JsonIgnore]
        public double TSS
            {
            get { return pondProps.TSS; }
            }


        #endregion



        //LOCAL VARIABALES


        #region local variables

        private double mm2m = 0.001;  //convert mm to meters
        private double m2mm = 1000;
        double mm2kg = 1; // mm (per unit area) of water converted to kg (per unit area) of water. (1mm/m^2 = 1 kg/m^2)(1000mm/^m2 = 1000kg/m^2 [1m/m^2 = 1 tonne/m^2])

        private double pondVolume = 0;

        private WaterProperties pondProps = new WaterProperties(0, 0, 0, 0, 0, 0);

        private double pondEvap;

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

            if (pondVolume + Volume > maxPondVolume)
                {
                Volume = maxPondVolume - pondVolume;
                Summary.WriteMessage(this, "You have overfilled the pond. Reduced the volume added to volume the pond could accept", MessageType.Warning);
                }

            AddWater_TempChange(Volume, addedProps.Temperature);
            AddWater_SalinityChange(Volume, addedProps.Salinity);
            AddWater_PHChange(Volume, addedProps.PH);
            AddWater_NChange(Volume, addedProps.N);
            AddWater_PChange(Volume, addedProps.P);
            AddWater_TSSChange(Volume, addedProps.TSS);
            //Add DOX

            pondVolume = pondVolume + Volume;
            }


        private void AddWater_TempChange(double Volume, double Temp)
            {
            pondProps.Temperature = WeightedAverage(pondVolume, pondProps.Temperature, Volume, Temp);
            //do some boundary checks and warn the user if too high or low.
            }


        private void AddWater_SalinityChange(double Volume, double Salinity)
            {
            pondProps.Salinity = WeightedAverage(pondVolume, pondProps.Salinity, Volume, Salinity);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_PHChange(double Volume, double PH)
            {
            pondProps.PH = WeightedAverage(pondVolume, pondProps.PH, Volume, PH);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_NChange(double Volume, double N)
            {
            pondProps.N = WeightedAverage(pondVolume, pondProps.N, Volume, N);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_PChange(double Volume, double P)
            {
            pondProps.P = WeightedAverage(pondVolume, pondProps.P, Volume, P);
            //do some boundary checks and warn the user if too high or low.
            }

        private void AddWater_TSSChange(double Volume, double TSS)
            {
            pondProps.TSS = WeightedAverage(pondVolume, pondProps.TSS, Volume, TSS);
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
            if (pondVolume - Volume < 0)
                {
                Volume = pondVolume;
                Summary.WriteMessage(this, "You have tried to remove more water than the pond contained. Reduced the volume removed to current volume of the pond. The pond is now empty.", MessageType.Warning);
                pondProps.ZeroProperties();   //when you empty the pond by removing water (eg. via pumping) the properties are removed with the water. (unlike emptying via evaporation)
                }

            pondVolume = pondVolume - Volume;
            }



        #endregion



        #region Evaporate Water from Pond


        private void EvaporateWater(double EvapAmount_mm)
            {

            double evapVolume;
            evapVolume = SurfaceArea * (EvapAmount_mm * mm2m);

            if (pondVolume - evapVolume <= 0.001)
                {
                Summary.WriteMessage(this, "You have evaporated all the water left in the pond. The pond is now empty."
                + Environment.NewLine + "We left 0.001 of a cubic meter of water in entire pond to store concentrated Pond Properties in.", MessageType.Warning);
                evapVolume = (pondVolume - 0.001);  //don't evaporate all the water. Leave a small amount so you don't get divide by zero.
                }

            if (evapVolume > 0.0)
                {
                //Change the Pond Properties to concentrate them.
                Concentrate_Salinity(evapVolume);
                Concentrate_N(evapVolume);
                Concentrate_P(evapVolume);
                Concentrate_TSS(evapVolume);
                }

            pondVolume = pondVolume - evapVolume;
            pondEvap = (evapVolume / SurfaceArea) * m2mm;

            }

        //No Temperature Change due to evaporation -> at least not yet.
        private void EvaporateWater_TempChange()
            {
            }

        private void Concentrate_Salinity(double EvapVolume)
            {
            pondProps.Salinity = Concentrate(pondVolume, pondProps.Salinity, EvapVolume);
            //do some boundary checks and warn the user if too high or low.
            }

        //No PH Change due to evaporation.

        private void Concentrate_N(double EvapVolume)
            {
            pondProps.N = Concentrate(pondVolume, pondProps.N, EvapVolume);
            //do some boundary checks and warn the user if too high or low.
            }

        private void Concentrate_P(double EvapVolume)
            {
            pondProps.P = Concentrate(pondVolume, pondProps.P, EvapVolume);
            //do some boundary checks and warn the user if too high or low.
            }

        private void Concentrate_TSS(double EvapVolume)
            {
            pondProps.TSS = Concentrate(pondVolume, pondProps.TSS, EvapVolume);
            //do some boundary checks and warn the user if too high or low.
            }


        private double Concentrate(double CurrentVolume, double CurrentPerVolume, double EvapVolume)
            {
            double currentAmount;
            double newPerVolume;

            currentAmount = CurrentVolume * CurrentPerVolume;
            newPerVolume = currentAmount / (CurrentVolume - EvapVolume);

            return newPerVolume;
            }


        #endregion



        #region Calculate Evaporation


        private double CalcEvap()
            {

            //Check if Pan Evaporation is in the Weather File.
            if (double.IsNaN(Weather.PanEvap))
                {
                //SILO Apsim format comes with "evap" column by default. https://www.longpaddock.qld.gov.au/silo/data/samples/datadrill/sampleapsim.html
                string errorMsg = "PondWater module requires 'evap' column in the weather file (Pan Evaporation).";
                throw new ApsimXException(this, errorMsg);
                }

            return (Kpan * Weather.PanEvap); //returns mm
            }



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




        #region Calculate Heat Change

        /// <summary>
        /// Fraction of sunlight reflected from the water surface.
        /// </summary>
        /// <param name="Today">DateTime for today</param>
        /// <param name="Latitude">(Not used at present but should use it to calculate Solar Altitude Angle)</param>
        /// <param name="WindSpeed">(m/s)</param>
        /// <returns>(0-1)</returns>
        private double ReflectionWaterSurface(DateTime Today, double Latitude, double WindSpeed)
            {
            double angleSolarAltitude;  //aka. lambda (units are in Radians)

            //TODO: convert latitude into Solar Altitude Angle.
            //for now hard code summer and winder values for northern australia.

            bool isWinter = DateUtilities.WithinDates("01Apr", Today, "01Nov");

            if (isWinter)
                angleSolarAltitude = 0.873;
            else
                angleSolarAltitude = Math.PI / 2; //90 degrees

            return (2.2 * Math.Pow((180 * (angleSolarAltitude/Math.PI)), -0.97)) * (1-(0.08* WindSpeed));

            }


        /// <summary>
        /// Heat Flux due to incomming solar radiation penetrating the surface of the water
        /// </summary>
        /// <param name="Heat_S_Radn">Solar Radiation (taken from APSIM met file) (MJ/m^2/day)</param>
        /// <param name="Reflection">Fraction Reflected from the water surface (0-1)</param>
        /// <returns>(kJ/m^2/day)</returns>
        private double HeatIn_SN_SolarRadiation(double Heat_S_Radn, double Reflection)
            {
            double MJ2kJ = 1000;
            return Heat_S_Radn * (1 - Reflection) * MJ2kJ;
            }


        /// <summary>
        /// Heat Flux due to incomming atmospheric radiation
        /// (Long-wave, sensible heat transfer from the atmospheric air)
        /// This still occurs at night unlike the solar radiation.
        /// </summary>
        /// <param name="MaxT">Maximum Temperature of the Atmosphere (oC)</param>
        /// <param name="MinT">Minimum Temperature of the Atmosphere (oC)</param>
        /// <returns>(kJ/m^2/hr)</returns>
        private double HeatIn_AT_AtmosphericLongWaveRadn(double MaxT, double MinT)
            {
            double r = 0.03; //reflectance of the water to long wave radiation (unitless)
            double sigma = 2.07 * Math.Pow(10, -7); //Stefan Boltzman constant (kJ/m^2/hr/K^4)
            double avgTemp_K = ((MaxT+MinT)/2) + 273.15;  //average temperature converted from oC to Kelvin

            //Emittance of the atmosphere.
            //Emittance is the ratio of emissivity of a particular body to the the emissivity of a of a perfect black body.
            double emittanceAtmosphere; //ratio (dimensionless) (between 0 and 1)
            emittanceAtmosphere = 0.398 * Math.Pow(10, -5) * Math.Pow(avgTemp_K, 2.148);

            //Stephan Boltzmans Law decribes the power radiated from a perfect black body.
            //(The choice of units for Stephan Boltzmans constant gives the units)
            double stephanBoltzmansLaw = sigma * Math.Pow(avgTemp_K, 4);  //kJ/m^2/hr

            //TODO: Don't we need to multiply by 24 to convert the units from hourly to daily.
            return (1 - r) * emittanceAtmosphere * stephanBoltzmansLaw;
            }


        /// <summary>
        /// Heat Flux radiated from the water surface to the atmosphere.
        /// </summary>
        /// <param name="PondTemp">Temperature of the Pond Water (oC)</param>
        /// <returns>(kJ/m^2/hr)</returns>
        private double HeatOut_WS_FromWaterSurface(double PondTemp)
            {
            double sigma = 2.07 * Math.Pow(10, -7); //Stefan Boltzman constant (kJ/m^2/hr/K^4)
            double pondTemp_K = PondTemp + 273.15;  //Pond Water Temp converted from oC to Kelvin

            //Emittance of the Pond Water Surface.
            //Emittance is the ratio of emissivity of a particular body to the the emissivity of a of a perfect black body.
            double emittanceWaterSurface = 0.97; //ratio (dimensionless) (between 0 and 1)

            //Stephan Boltzmans Law decribes the power radiated from a perfect black body.
            //(The choice of units for Stephan Boltzmans constant gives the units)
            double stephanBoltzmansLaw = sigma * Math.Pow(pondTemp_K, 4);  //kJ/m^2/hr

            //TODO: Don't we need to multiply by 24 to convert the units from hourly to daily.
            return emittanceWaterSurface * stephanBoltzmansLaw;
            }

        /// <summary>
        /// Heat Flux caused by evaporation from the pond.
        /// When water evaporates it takes away the latent heat required to go from liquid to vapour.
        /// </summary>
        /// <param name="PondTemp">Temperature of the Pond Water (oC)</param>
        /// <param name="MinT">Minimum Temperature of the Atmosphere (oC)</param>
        /// <param name="WindSpeed">Wind Speed (m/s)</param>
        /// <returns>(kJ/day)</returns>
        private double HeatOut_E_EvaporationLatentHeat(double PondTemp, double MinT, double WindSpeed)
            {
            double N = 5.0593; //constant (kJ/m^2/km/mmHg)

            double satVP; //saturated vapour pressure
            double pondVP; //vapour pressure above the pond surface;

            satVP = 25.374 * Math.Exp(17.62 - (5271 / PondTemp));
            pondVP = 610.78 * Math.Exp(17.2694 * ((MinT - 2) / (MinT + 235.3)));
            double VPdeficit = satVP - pondVP;

            return N * WindSpeed * VPdeficit;
            }


        /// <summary>
        /// Heat Flux caused by evaporation from the pond.
        /// When water evaporates it takes away the latent heat required to go from liquid to vapour.
        /// </summary>
        /// <param name="Evaporation_mm">Amount of water evaporated from the pond (mm)</param>
        /// <returns>(kJ/m^2/day)</returns>
        private double HeatOut_E_EvaporationLatentHeat(double Evaporation_mm)
            {
            double latentHeatVaporization = 2258.0;  //Specific Latent Heat of Vaporization for water (kJ/kg) at 100oC

            double Evaporation_kg = Evaporation_mm * mm2kg; //actually mm2kg = 1, but nice to be explicit about the conversion.

            return latentHeatVaporization * Evaporation_kg;
            }



        /// <summary>
        /// Heat Flux caused by transfer of heat from the atmosphere to the pond AND vice versa.
        /// Basically this is via the Conduction of Heat when two bodies are in contact.
        /// </summary>
        /// <param name="PondTemp">Temperature of the Pond Water (oC)</param>
        /// <param name="MaxT">Maximum Temperature of the Atmosphere (oC)</param>
        /// <param name="MinT">Minimum Temperature of the Atmosphere (oC)</param>
        /// <param name="WindSpeed">(m/s)</param>
        /// <returns>(kJ/day)</returns>
        private double HeatFlux_C_SensibleHeatTransferBetweenAirAndPond(double PondTemp, double MaxT, double MinT, double WindSpeed)
            {
            double airTemp = (MaxT + MinT) / 2;

            double tempGradient = PondTemp - airTemp;

            return 1.5701 * WindSpeed * tempGradient;
            }


        /// <summary>
        /// Heat from solar radiation that passed through the pond and reached the bottom.
        /// This has to be removed from what entered the pond to give the net amount absorbed in the pond.
        /// </summary>
        /// <param name="HeatIn_SN">Radiation that entered the pond (kJ/m^2/day)</param>
        /// <param name="Reflection">Fraction of light reflected from the surface of the pond (0-1)</param>
        /// <param name="SDD">Secchi Disk Depth (m)</param>
        /// <param name="PondDepth">Current Depth of the Pond Water (oC)</param>
        /// <returns>(kJ/day)</returns>
        private double HeatOut_SNZ_SolarRadiationReachingBottomOfPond(double HeatIn_SN, double Reflection, double SDD, double PondDepth)
            {
            double fractAborbedAtSurface = 0.03;  //fraction of the light absorbed at the surface.
            double lightExtinctionCoeff = 1.7 / SDD;

            double fractReachingBtm = (1 - Reflection) * (1 - fractAborbedAtSurface) * Math.Exp(-lightExtinctionCoeff * PondDepth);

            return HeatIn_SN * fractReachingBtm;
            }



        /// <summary>
        /// Heat Flux caused by transfer of heat from pond water and bottom of the pond.
        /// Basically this is via the Conduction of Heat when two bodies are in contact.
        /// </summary>
        /// <param name="PondTemp">Temperature of the Pond Water (oC)</param>
        /// <param name="SedimentTemp">Temperature of the Sediment at the bottom of the pond (oC)</param>
        /// <param name="PondDepth">Current Depth of the Pond Water (oC)</param>
        /// <returns>(kJ/day)</returns>
        private double HeatFlux_SED_HeatTransferBetweenWaterAndBottomOfPond(double PondTemp, double SedimentTemp, double PondDepth)
            {
            double Ksed = 2.53; //thermal conductivity coefficient for the Sediment. (kJ/m/hr/oC)
            double deltaZ = PondDepth;  //distance between centres of the volume elements (m)

            double tempGradient = PondTemp - SedimentTemp;

            return Ksed * (tempGradient / deltaZ);
            }


        /// <summary>
        /// Heat Lost from the Sediment to the Ground Water
        /// Basically this is via the Conduction of Heat when two bodies are in contact.
        /// </summary>
        /// <param name="SedimentTemp">Temperature of the Sediment at the bottom of the pond (oC)</param>
        /// <returns>(kJ/day)</returns>
        private double HeatOut_GW_HeatLostFromSedimentToGroundWater(double SedimentTemp)
            {
            double Ke = 2.53;  //thermal conductivity coefficient for the Earth. (kJ/m/hr/oC)  //nb. using same value for Sediment (Ksed)
            double groundWaterTemp = 20;   //Temperature of the Ground Water (oC)
            double deltaZ = 5;    //distance betweeen centres of sediment and GW volumes (m)

            double tempGradient = SedimentTemp - groundWaterTemp;

            return Ke * (tempGradient / deltaZ);
            }


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

            //Add Today's Rain
            double rainVolume = SurfaceArea * (Weather.Rain * mm2m);
            WaterProperties rainProps = new WaterProperties(Weather.Tav, 0, 7.0, 0, 0, 0);
            AddWater(rainVolume, rainProps);

            //Remove Today's Evaporation
            double evapAmount_mm;
            evapAmount_mm = CalcEvap();
            EvaporateWater(evapAmount_mm);
            }



        #endregion





        //MANAGER COMMANDS

        #region Manager Commands

        /// <summary>
        /// Fill the Pond with a given volume of water.
        /// Must specifiy the properties of the water you are adding as well.
        /// </summary>
        /// <param name="Volume">Volume of water to add (m^3)</param>
        /// <param name="WaterProperties">Properties of the water you are adding</param>
        public void Fill(double Volume, WaterProperties WaterProperties)
            {
            AddWater(Volume, WaterProperties);
            }

        /// <summary>
        /// Fill the Pond with a given volume of water.
        /// Must specifiy the properties of the water you are adding as well.
        /// </summary>
        /// <param name="Volume">Volume of water to add (m^3)</param>
        /// <param name="WaterTemp">Temperature of the water (oC)</param>
        /// <param name="Salinity">Salinity (kg/m^3)</param>
        /// <param name="PH">PH</param>
        /// <param name="N">Nitrogen (kg/m^3)</param>
        /// <param name="P">Phosporus (kg/m^3)</param>
        /// <param name="TSS">Total Suspended Solids (kg/m^3)</param>
        public void Fill(double Volume, double WaterTemp, double Salinity, double PH, double N, double P, double TSS)
            {
            WaterProperties addedProps = new WaterProperties(WaterTemp, Salinity, PH, N, P, TSS);
            AddWater(Volume, addedProps);
            }


        /// <summary>
        /// Remove a given volume of water from the Pond.
        /// </summary>
        /// <param name="Volume">Volume of water to remove from the pond (m^3)</param>
        public void Empty(double Volume)
            {
            RemoveWater(Volume);
            }

        #endregion


        }


    }
