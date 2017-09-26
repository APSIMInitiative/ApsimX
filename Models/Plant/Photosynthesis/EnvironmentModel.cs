using System;
using System.Collections.Generic;
using Utilities;

namespace Models.PMF.Phenology
{
    public class EnvironmentModel
    {
        //Location variables
        protected Angle _latitude;
        protected Angle _longitude;

        //Daily Variables
        protected double _DOY;
        protected double _maxT;
        protected double _minT;
        protected double Sg;
        protected double _vpd;
        protected double _maxRH = -1;
        protected double _minRH;
        protected double _xLag = 1.8;
        protected double _yLag = 2.2;
        protected double _zLag = 1;
        protected double _ATM = 1.013;

        //protected HourlyMet hm;

        public delegate void Notify();

        //[System.Xml.Serialization.XmlIgnore]
        public Notify notify;

        //Timestep variables
        //-------------------------------------------------------------------------
        public EnvironmentModel()
        {
            _latitude = new Angle();
            _longitude = new Angle();

            solarDeclination = new Angle();

            solarElevation = new Angle();

            //hm = new HourlyMet();
        }
        //-------------------------------------------------------------------------
        public EnvironmentModel(double Latitude, double Longitude, int NumberLayers)
            : this()
        {
            _latitude = new Angle(Latitude, AngleType.Deg);

            _longitude = new Angle(Longitude, AngleType.Deg);
        }
        //-------------------------------------------------------------------------
        public EnvironmentModel(double Latitude, int NumberLayers)
            : this()
        {
            _latitude = new Angle(Latitude, AngleType.Deg);

            _longitude = new Angle(90, AngleType.Deg);
        }

        //---------------------------------------------------------------------------
        protected int calcStandardLongitude(double longitude)
        {
            return (int)((longitude / 15) + 0.5) * 15;
        }
        //------------------------------------------------------------------------------------------------
        // Properties
        //------------------------------------------------------------------------------------------------
        [ModelPar("MjNwl", "Longitude", "Le", "", "°")]
        public Angle longitude
        {
            get { return _longitude; }
            set
            {
                _longitude = value;
                //OnPropertyChanged("longitude");
            }
        }

        [ModelPar("hjFzy", "Latitude", "Lat", "", "°")]
        public Angle latitude
        {
            get { return _latitude; }
            set
            {
                _latitude = value;
                //run();
                //OnPropertyChanged("latitude");
                if (notify != null)
                {
                    notify();
                }
            }
        }

        [ModelPar("vmCd9", "Latitude", "Lat", "", "°")]
        public double latitudeD
        {
            get { return _latitude.deg; }
            set
            {
                _latitude = new Angle(value, AngleType.Deg);
                //run();
                //OnPropertyChanged("latitude");
                if (notify != null)
                {
                    notify();
                }
            }
        }

        [ModelPar("AlMfK", "Day of year", "Day", "", "")]
        public double DOY
        {
            get { return _DOY; }
            set
            {
                _DOY = value;
                //run();
                //OnPropertyChanged("DOY");
                if (notify != null)
                {
                    // notify();
                }
            }
        }

        [ModelPar("ArrfK", "Atmospheric pressure", "ATM", "", "")]
        public double ATM
        {
            get { return _ATM; }
            set
            {
                _ATM = value;
            }
        }

        [ModelPar("5hHkF", "Maximum air temperature for the day", "T", "a_max", "°C")]
        public double maxT
        {
            get { return _maxT; }
            set
            {
                _maxT = value;
                //run();
                //OnPropertyChanged("maxT");
                if (notify != null)
                {
                    // notify();
                }
            }
        }

        [ModelPar("lI6iy", "Minimum air temperature for the day", "T", "a_min", "°C")]
        public double minT
        {
            get { return _minT; }
            set
            {
                _minT = value;
                //run();
                //OnPropertyChanged("minT");
                if (notify != null)
                {
                    // notify();
                }
            }
        }

        [ModelPar("RyHP0", "Daily min RH", "RHMin", "", "%")]
        public double minRH
        {
            get { return _minRH; }
            set
            {
                _minRH = value;
                //run();
                //OnPropertyChanged("minT");
                if (notify != null)
                {
                    // notify();
                }
            }
        }

        [ModelPar("CjkRT", "Daily max RH", "RHMax", "", "%")]
        public double maxRH
        {
            get { return _maxRH; }
            set
            {
                _maxRH = value;
                //run();
                //OnPropertyChanged("minT");
                if (notify != null)
                {
                    //notify();
                }
            }
        }

        [ModelPar("vWD3R", "X Lag", "xLag", "", "")]
        public double xLag
        {
            get { return _xLag; }
            set
            {
                _xLag = value;
                //run();
                if (notify != null)
                {
                    //notify();
                }
            }
        }

        [ModelPar("r5C7r", "Y Lag", "yLag", "", "")]
        public double yLag
        {
            get { return _yLag; }
            set
            {
                _yLag = value;
                //run();
                if (notify != null)
                {
                    //notify();
                }
            }
        }

        [ModelPar("JKzC3", "Z Lag", "zLag", "", "")]
        public double zLag
        {
            get { return _zLag; }
            set
            {
                _zLag = value;
                //run();
                if (notify != null)
                {
                    //notify();
                }
            }
        }

        [ModelPar("sdkGj", "Daily VPD", "vpd", "", "kPa")]
        public double vpd
        {
            get { return _vpd; }
            set
            {
                _vpd = value;
                //OnPropertyChanged("vpd");
                if (notify != null)
                {
                    notify();
                }
            }
        }

        [ModelPar("goJzH", "Daily solar radiation reaching Earth's surface", "S", "g", "MJ/m2/day", "", "m2 of ground")]
        public double radn
        {
            get { return Sg; }   
            set
            {
                Sg = value;
                calcSolarGeometry();
                _atmTransmissionRatio = Sg / So;

                calcRatios(Sg / So);
                //run();
                if (notify != null)
                {
                    //    notify();
                }
            }
        }

        [ModelPar("uMZ2C", "Total daily incident solar radiation", "S", "g", "MJ/m2/day", "", "m2 of ground")]
        public double Sg2
        {
            get
            {
                double r = 0;
                for (int i = 6; i <= 18; i++)
                {
                    //r += Ios.value(i) * ratios.value(i) * 3600;
                    r += Ios.value(i) * 3600;
                }
                return r;
            }
            set
            {

            }
        }

        [ModelVar("drKhV", "Solar declination angle", "δ", "", "radian")]
        public Angle solarDeclination { get; set; }

        [ModelVar("EwOhQ", "Solar elevation angle", "β", "", "radian")]
        public Angle solarElevation { get; set; }

        [ModelVar("UHXoS", "Standard longitude of time zone", "Ls", "", "degree")]
        public double standardLongitude { get; set; }

        #region Solar Geometry
        private double _solarConstant = 1360;
        [ModelPar("7eMZa", "Solar constant", "I0", "", "J m-2 s-1")]
        public double solarConstant
        {
            get { return _solarConstant; }
            set
            {
                _solarConstant = value;
                //OnPropertyChanged("solarConstant"); 
            }
        }

        protected double _atmTransmissionRatio = 0.75;
        [ModelPar("AoRO2", "Atmospheric transmission coefficient", "Ratio", "", "")]
        public double atmTransmissionRatio
        {
            get { return _atmTransmissionRatio; }
            set
            {
                _atmTransmissionRatio = value;

                calcSolarGeometry();

                Sg = So * _atmTransmissionRatio;

                calcRatios(_atmTransmissionRatio);

                //run();

                if (notify != null)
                {
                    //     notify();
                }
                //OnPropertyChanged("atmTransmissionCoeff");
            }
        }

        protected double[] _ratios_;
        [ModelPar("a1Pb4", "", "", "", "")]
        public double[] ratios_
        {
            get { return _ratios_; }
            set
            {
                _ratios_ = value;

                calcRatios();

                //run();

                if (notify != null)
                {
                    //          notify();
                }
                //OnPropertyChanged("atmTransmissionCoeff");
            }
        }

        [ModelVar("ob6iX", "Hour angle of sun", "W2", "", "radian")]
        public Angle hourAngle { get; set; }

        [ModelVar("3ywo3", "Sunset hour angle of sun", "W1", "", "radian")]
        public Angle sunsetAngle { get; set; }

        [ModelVar("XidUS", "Day angle", "Γd", "", "")]
        public Angle dayAngle { get; set; }

        [ModelVar("a4REK", "Solar elevation", "α", "", "°", "t")]
        public Angle sunAngle { get; set; }

        [ModelVar("RTS4t", "Solar elevation", "α", "", "°", "t")]
        public double sunAngleD
        {
            get
            {
                return sunAngle.deg;
            }
        }

        [ModelVar("CBMOF", "Zenith angle", "Z", "", "radian")]
        public Angle zenithAngle { get; set; }

        [ModelVar("nEqsb", "Radius vector", "R1", "", "")]
        public double radiusVector { get; set; }

        [ModelVar("t2rN0", "Day length", "L1", "", "h")]
        public double dayLength { get; set; }

        [ModelVar("4lcLb", "", "tfrac", "", "")]
        public double tfrac { get; set; }

        [ModelVar("JfTav", "Daily extra-terrestrial irradiance", "So", "", "MJ/m2/day")]
        public double So { get; set; }

        [ModelVar("eaehB", "Sunrise", "", "", "")]
        public double sunrise { get; set; }

        [ModelVar("0yWWK", "Sunset", "", "", "")]
        public double sunset { get; set; }

        private bool _initilised = false;
        public bool initilised
        {
            get
            {
                return _initilised;
            }
            set
            {
                _initilised = value;
                if (_initilised)
                {
                    run();
                }
            }
        }

        public void calcRatios(double ratio)
        {
            _ratios_ = new double[24];

            for (int i = 0; i < 23; i++)
            {
                _ratios_[i] = ratio;
            }

            calcRatios();
        }


        public void calcRatios()
        {
            double[] times = new double[24];

            for (int i = 0; i < 23; i++)
            {
                times[i] = i;
            }

            ratios = new TableFunction(times, _ratios_);
        }

        //---------------------------------------------------------------------------
        public double getTemp(double time)
        {
            return temps.value(time - 1);
        }
        //---------------------------------------------------------------------------
        public double getVPD(double time)
        {
            return vpds.value(time);
        }
        //---------------------------------------------------------------------------
        Angle calcSolarDeclination(int DOY)
        {
            return new Angle(23.45 * Math.Sin(2 * Math.PI * (284 + DOY) / 365), AngleType.Deg);
        }
        //---------------------------------------------------------------------------
        Angle calcHourAngle(double timeOfDay)
        {
            return new Angle((timeOfDay - 12) * 15, AngleType.Deg);
        }
        //---------------------------------------------------------------------------
        Angle calcDayAngle(int DOY)
        {
            return new Angle(2 * Math.PI * (DOY - 1) / 365, AngleType.Rad);
        }
        //---------------------------------------------------------------------------
        protected double calcDayLength(double latitudeRadians, double solarDecRadians)
        {
            sunsetAngle = new Angle(Math.Acos(-1 * Math.Tan(latitudeRadians) * Math.Tan(solarDecRadians)), AngleType.Rad);
            return (sunsetAngle.deg / 15) * 2;
        }
        //---------------------------------------------------------------------------
        double calcSunAngle(double hour, double latitudeRadians, double solarDecRadians, double dayLength)
        {
            return Math.Asin(Math.Sin(latitudeRadians) * Math.Sin(solarDecRadians) +
                           Math.Cos(latitudeRadians) * Math.Cos(solarDecRadians) * Math.Cos((Math.PI / 12.0) * dayLength * (tfrac - 0.5))); //Daylength can be taken out of this last bit
        }
        //---------------------------------------------------------------------------
        double calcZenithAngle(double latitudeRadians, double solarDeclinationRadians, double hourAngleRadians)
        {
            return Math.Acos(Math.Sin(solarDeclinationRadians) * Math.Sin(latitudeRadians) + Math.Cos(solarDeclinationRadians) * Math.Cos(latitudeRadians) * Math.Cos(hourAngleRadians));
        }
        //---------------------------------------------------------------------------
        double calcDailyExtraTerrestrialRadiation(double latitudeRadians, double sunsetAngleRadians, double solarDecRadians, int DOY) // solar radiation
        {
            radiusVector = 1.0 / Math.Sqrt(1 + (0.033 * Math.Cos(360.0 * DOY / 365.0)));

            return ((24.0 / Math.PI) * (3600.0 * solarConstant / Math.Pow(radiusVector, 2)) * (sunsetAngleRadians * Math.Sin(latitudeRadians) * Math.Sin(solarDecRadians) +
               +Math.Sin(sunsetAngleRadians) * Math.Cos(latitudeRadians) * Math.Cos(solarDecRadians)) / 1000000.0);
        }
        //---------------------------------------------------------------------------
        public void calcSolarGeometry()
        {
            solarDeclination = calcSolarDeclination((int)DOY);

            dayLength = calcDayLength(latitude.rad, solarDeclination.rad);

            dayAngle = calcDayAngle((int)DOY);

            sunrise = 12 - dayLength / 2;

            sunset = 12 + dayLength / 2;

            So = calcDailyExtraTerrestrialRadiation(latitude.rad, sunsetAngle.rad, solarDeclination.rad, (int)DOY);

            //_atmTransmissionRatio = radn / ETRadiation;

        }
        //---------------------------------------------------------------------------
        public void calcSolarGeometryTimestep(double hour)
        {
            tfrac = (hour - sunrise) / dayLength;

            hourAngle = calcHourAngle(hour);

            sunAngle = new Angle(calcSunAngle(hour, latitude.rad, solarDeclination.rad, dayLength), AngleType.Rad);

        }
        //---------------------------------------------------------------------------
        public void run()
        {
            conversionFactor = 2413.0 / 1360.0;

            calcSolarGeometry();

            //_atmTransmissionRatio = _radn / ETRadiation;
            //_atmTransmissionRatio = 0.75;

            Sg = _atmTransmissionRatio * So;

            if (ratios == null)
            {
                calcRatios(Sg / So);
            }
            // calcETRadns();
            calcIncidentRadns();
            calcDiffuseRadns();
            calcDirectRadns();
            calcTemps();
            calcSVPs();
            calcRHs();
            calcVPDs();

            convertRadiationsToPAR();
        }
        //---------------------------------------------------------------------------
        private void convertRadiationsToPAR()
        {
            List<double> io_par = new List<double>();
            List<double> idiff_par = new List<double>();
            List<double> idir_par = new List<double>();
            List<double> time = new List<double>();

            for (int i = 0; i < 24; i++)
            {
                time.Add(i);

                idiff_par.Add(Idiffs.value(i) * 0.5 * 4.25 * 1E6);
                idir_par.Add(Idirs.value(i) * 0.5 * 4.56 * 1E6);

                io_par.Add(idiff_par[i] + idir_par[i]);
            }
            Ios_PAR = new TableFunction(time.ToArray(), io_par.ToArray(), false);
            Idiffs_PAR = new TableFunction(time.ToArray(), idiff_par.ToArray(), false);
            Idirs_PAR = new TableFunction(time.ToArray(), idir_par.ToArray(), false);
        }

        public void run(double time)
        {
            calcSolarGeometry();
            calcSolarGeometryTimestep(time);
            calcIncidentRadiation(time);

            //run();
            // notify();
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Incident Radiation
        protected double _fracDiffuseATM = 0.1725;
        [ModelPar("nU58A", "Fraction of diffuse irradiance in the atmosphere", "", "", "")]
        public double fracDiffuseATM
        {
            get { return _fracDiffuseATM; }
            set
            {
                _fracDiffuseATM = value;
                //OnPropertyChanged("fracDiffuseATM"); 
            }
        }

        [ModelVar("oh5kh", "Total PAR at canopy top ", "I", "o", "μmol PAR/m2/s", "t", "m2 of ground")]
        public double totalIncidentRadiation { get; set; }

        [ModelVar("LuYdw", "Direct PAR at canopy top", "I", "dir", "μmol PAR/m2/s", "t", "m2 of ground")]
        public double directRadiationPAR { get; set; }

        [ModelVar("aqmpo", "Diffuse PAR at canopy top", "I", "dif", "μmol PAR/m2/s", "t", "m2 of ground")]
        public double diffuseRadiationPAR { get; set; }

        [ModelVar("8WKRv", "Total PAR at canopy top", "I", "o", "μmol PAR/m2/s", "t", "m2 of ground")]
        public double totalRadiationPAR { get; set; }

        [ModelVar("4HlHU", "", "", "", "")]
        public double conversionFactor { get; set; }

        //---------------------------------------------------------------------------

        public void calcIncidentRadiation(double hour)
        {
            totalIncidentRadiation = Ios.value(hour);
            totalRadiationPAR = Ios_PAR.value(hour);
            diffuseRadiationPAR = Idiffs_PAR.value(hour);
            directRadiationPAR = Idirs_PAR.value(hour);

            //double S0 = calcExtraTerestrialRadiation2(hour);

            //totalIncidentRadiation = S0 * ratio / conversionFactor * 1E12;
            ////totalIncidentRadiation = calcTotalIncidentRadiation(hour, dayLength, sunrise);
            ////diffuseRadiation = calcDiffuseRadiation(sunAngle.rad) / conversionFactor * 10E12;
            //diffuseRadiation = fracDiffuseATM * solarConstant * Math.Sin(sunAngle.rad) / 1000000 / conversionFactor * 1E12;

            //if (diffuseRadiation > totalIncidentRadiation)
            //{
            //    diffuseRadiation = totalIncidentRadiation;
            //}

            ////totalIncidentRadiation = totalIncidentRadiation / conversionFactor * 10E12;
            //directRadiation = (totalIncidentRadiation - diffuseRadiation);
        }

        //---------------------------------------------------------------------------
        public double calcInstantaneousIncidentRadiation(double hour)
        {

            // return So * ratios.value(hour) * (1 + Math.Sin(2 * Math.PI * (hour - sunrise) / dayLength + 1.5 * Math.PI)) / 
            //     (dayLength * 3600);

            return (So * ratios.value(hour) * Math.PI * Math.Sin(Math.PI * (hour - sunrise) / dayLength)) / (2 * dayLength * 3600);

        }
        //---------------------------------------------------------------------------
        public double calcExtraTerestrialRadiation2(double hour, bool external = false)
        {
            if (external)
            {
                calcSolarGeometryTimestep(hour);
            }

            Angle hourAngle2 = new Angle(0.25 * Math.Abs((hour - 12) * 60) / (180 / Math.PI), AngleType.Rad);

            //zenithAngle = new Angle(calcZenithAngle(latitude.rad, solarDeclination.rad, hourAngle.rad), AngleType.Rad);

            zenithAngle = new Angle(calcZenithAngle(latitude.rad, solarDeclination.rad, hourAngle2.rad), AngleType.Rad);

            return (solarConstant / Math.Pow(radiusVector, 2) * Math.Cos(zenithAngle.rad) / 1000000);
        }
        //---------------------------------------------------------------------------
        // void calcETRadns()
        // {
        //     // calculates SVP at the air temperature
        //     List<double> eTs = new List<double>();
        //     List<double> time = new List<double>();

        //     for (int i = 0; i < 24; i++)
        //     {
        //         time.Add(i);
        //         if ((double)i >= (12.0 - dayLength / 2) && (double)i <= (12.0 + dayLength / 2))
        //         {
        //             //eTs.Add(Math.Max(calcExtraTerestrialRadiation2(i) / conversionFactor * 1E12, 0));
        //             eTs.Add(Math.Max(calcInstantaneousIncidentRadiation(i) , 0));
        //         }
        //         else
        //         {
        //             eTs.Add(0);
        //         }
        //     }
        //     ETs = new TableFunction(time.ToArray(), eTs.ToArray(), false);
        // }
        //---------------------------------------------------------------------------
        void calcIncidentRadns()
        {
            List<double> ios = new List<double>();
            //List<double> ets = new List<double>();

            List<double> time = new List<double>();

            int preDawn = (int)Math.Floor(12 - dayLength / 2.0);
            int postDusk = (int)Math.Ceiling(12 + dayLength / 2.0);

            for (int i = 0; i < 24; i++)
            {
                time.Add(i);

                //ios.Add(ETs.value(i) * ratios.value(i));
                if (i > preDawn && i < postDusk)
                {
                    ios.Add(Math.Max(calcInstantaneousIncidentRadiation(i), 0));
                }
                else
                {
                    ios.Add(0);
                }

                //ets.Add(ios[i] / 0.75);
            }
            Ios = new TableFunction(time.ToArray(), ios.ToArray(), false);
            //ETs = new TableFunction(time.ToArray(), ets.ToArray(), false);
        }
        //---------------------------------------------------------------------------
        void calcDiffuseRadns()
        {
            List<double> diffs = new List<double>();
            List<double> ets = new List<double>();
            List<double> time = new List<double>();

            for (int i = 0; i < 24; i++)
            {
                time.Add(i);
                calcSolarGeometryTimestep(i);

                diffs.Add(Math.Max(fracDiffuseATM * solarConstant * Math.Sin(sunAngle.rad) / 1000000, 0));
                // ets.Add(diffs[i] / fracDiffuseATM);

                if (diffs[i] > Ios.value(i))
                {
                    diffs[i] = Ios.value(i);
                }
            }
            Idiffs = new TableFunction(time.ToArray(), diffs.ToArray(), false);
            // ETs = new TableFunction(time.ToArray(), ets.ToArray(), false);
        }
        //---------------------------------------------------------------------------
        void calcDirectRadns()
        {
            List<double> dirs = new List<double>();
            List<double> time = new List<double>();

            for (int i = 0; i < 24; i++)
            {
                time.Add(i);
                dirs.Add(Ios.value(i) - Idiffs.value(i));
            }
            Idirs = new TableFunction(time.ToArray(), dirs.ToArray(), false);
        }
        //---------------------------------------------------------------------------

        double calcTotalIncidentRadiation(double hour, double dayLength, double sunrise)
        {
            return radn * (1 + Math.Sin(2 * Math.PI * (hour - sunrise) / dayLength + 1.5 * Math.PI)) / (dayLength * 3600);
        }
        //---------------------------------------------------------------------------

        double calcDiffuseRadiation(double sunAngleRadians)
        {
            return Math.Min(Math.Sin(sunAngleRadians) * solarConstant * fracDiffuseATM / 1000000, totalIncidentRadiation);
        }

        //---------------------------------------------------------------------------
        #endregion
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction Ios { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction Idirs { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction Idiffs { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction Ios_PAR { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction Idirs_PAR { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction Idiffs_PAR { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction ETs { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction ratios { get; set; }

        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction temps { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction radns { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction svps { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction vpds { get; set; }
        //[System.Xml.Serialization.XmlIgnore]
        public TableFunction rhs { get; set; }



        public virtual void doUpdate() { }
        //------------------------------------------------------------------------------------------------
        public double calcSVP(double TAir)
        {
            // calculates SVP at the air temperature
            //return 6.1078 * Math.Exp(17.269 * TAir / (237.3 + TAir)) * 0.1;
            return 610.7 * Math.Exp(17.4 * TAir / (239 + TAir)) / 1000;
        }
        //------------------------------------------------------------------------------------------------
        protected void calcSVPs()
        {
            // calculates SVP at the air temperature
            List<double> svp = new List<double>();
            List<double> time = new List<double>();

            for (int i = 0; i < 24; i++)
            {
                svp.Add(calcSVP(temps.yVals[i]));
                time.Add(i);
            }
            svps = new TableFunction(time.ToArray(), svp.ToArray(), false);
        }
        //------------------------------------------------------------------------------------------------
        protected void calcRHs()
        {
            List<double> time = new List<double>();
            List<double> rh = new List<double>();
            // calculate relative humidity
            double wP;
            if (maxRH < 0.0 || minRH < 0.0)
            {
                wP = calcSVP(_minT) / 100 * 1000 * 90;         // svp at Tmin
            }
            else
            {
                wP = (calcSVP(_minT) / 100 * 1000 * maxRH + calcSVP(_maxT) / 100 * 1000 * minRH) / 2.0;
            }

            for (int i = 0; i < 24; i++)
            {
                rh.Add(wP / (10 * svps.yVals[i]));
            }

            rhs = new TableFunction(time.ToArray(), rh.ToArray(), false);

        }
        //------------------------------------------------------------------------------------------------
        protected void calcVPDs()
        {
            List<double> time = new List<double>();
            List<double> vpd = new List<double>();

            double AVP = calcSVP(minT); // Actual vapour pressure

            for (int i = 0; i < 24; i++)
            {
                //AD - Have checked. This formula is equivalent to above
                //vpd.Add((1 - (rhs.yVals[i] / 100)) * svps.yVals[i]);
                vpd.Add(svps.yVals[i] - AVP);
                time.Add(i);
            }

            vpds = new TableFunction(time.ToArray(), vpd.ToArray(), false);
        }
        //------------------------------------------------------------------------------------------------
        public void calcTemps()
        {
            List<double> hours = new List<double>();
            List<double> temperatures = new List<double>();

            Angle aDelt = new Angle(23.45 * Math.Sin(2 * Math.PI * (284 + _DOY) / 365), AngleType.Deg);
            Angle temp2 = new Angle(Math.Acos(-Math.Tan(latitude.rad) * Math.Tan(aDelt.rad)), AngleType.Rad);

            double dayLength = (temp2.deg / 15) * 2;
            double nightLength = (24.0 - dayLength);                     // night hours
            // determine if the hour is during the day or night
            double t_mint = 12.0 - dayLength / 2.0 + zLag;           // corrected dawn - GmcL
                                                                     //time of Tmin ??
            double t_suns = 12.0 + dayLength / 2.0;                  // sundown

            for (int t = 1; t <= 24; t++)
            {
                double hr = t;
                double temperature;

                if (hr >= t_mint && hr < t_suns)         //day
                {
                    double m = 0;
                    m = hr - t_mint;
                    temperature = (maxT - minT) * Math.Sin((Math.PI * m) / (dayLength + 2 * xLag)) + minT;
                }
                else                             // night
                {
                    double n = 0;
                    if (hr > t_suns)
                    {
                        n = hr - t_suns;
                    }
                    if (hr < t_mint)
                    {
                        n = (24.0 - t_suns) + hr;
                    }
                    double m_suns = dayLength - zLag;
                    double T_suns = (maxT - minT) * Math.Sin((Math.PI * m_suns) / (dayLength + 2 * xLag)) + minT;
                    temperature = minT + (T_suns - minT) * Math.Exp(-yLag * n / nightLength);
                }
                hours.Add(hr - 1);
                temperatures.Add(temperature);
            }
            temps = new TableFunction(hours.ToArray(), temperatures.ToArray(), false);
        }
    }
}
