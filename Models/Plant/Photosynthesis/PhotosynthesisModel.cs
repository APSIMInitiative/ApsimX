using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.PMF.Phenology
{
    public class PhotosynthesisModel : NotifyablePropertyClass
    {
        public enum PhotoPathway { C3, C4 };
        public enum NitrogenModel { APSIM, EXPONENTIAL };
        public enum ElectronTransportModel { EMPIRICAL, EXTENDED };
        public enum ConductanceModel { SIMPLE, DETAILED };

        public List<string> parameters;
        public List<string> variables;
        public List<string> sunlitShadedVariables;
        public List<string> canopyVariables;

        #region Global
        public EnvironmentModel envModel { get; set; }
        public LeafCanopy canopy { get; set; }
        public SunlitShadedCanopy sunlit { get; set; }
        public SunlitShadedCanopy shaded { get; set; }

        public SunlitCanopy sunlitAC1;
        public SunlitCanopy sunlitAJ;
        public ShadedCanopy shadedAC1;
        public ShadedCanopy shadedAJ;

        public delegate void Notify();
        public delegate void NotifyBool(bool flag);
        //[System.Xml.Serialization.XmlIgnore]
        public Notify notifyFinish;
        //[System.Xml.Serialization.XmlIgnore]
        public NotifyBool notifyStart;
        //[System.Xml.Serialization.XmlIgnore]
        public Notify notifyFinishDay;
        //[System.Xml.Serialization.XmlIgnore]
        public Notify notifyNewParSet;


        protected double _time = 10.0;
        protected double _timeStep = 1;

        //[System.Xml.Serialization.XmlIgnore]
        public bool initialised = false;

        //[System.Xml.Serialization.XmlIgnore]
        public List<double> instants;

        //[System.Xml.Serialization.XmlIgnore]
        public List<double> ass;

        public List<double> Ios = new List<double>();
        public List<double> Idirs = new List<double>();
        public List<double> Idiffs = new List<double>();

        [ModelVar("SLAC88", "Time step", "t", "", "")]
        public List<double> SunlitACs { get; set; } = new List<double>();
        [ModelVar("SLAJ73", "", "", "", "")]
        public List<double> SunlitAJs { get; set; } = new List<double>();
        [ModelVar("SSAC11", "", "", "", "")]
        public List<double> ShadedACs { get; set; } = new List<double>();
        [ModelVar("SSAK00", "", "", "", "")]
        public List<double> ShadedAJs { get; set; } = new List<double>();

        // [ModelVar("CCs324", "", "", "", "")]
        // public List<double> Ccs { get; set; } = new List<double>();

        public int minTime = 6;
        public int maxTime = 18;

        [ModelVar("4UytK", "Time step", "t", "", "")]
        public double timeStep
        {
            get { return _timeStep; }
            set
            {
                _timeStep = value;
                OnPropertyChanged("timeStep");
                //this.runDaily();
            }
        }

        [ModelVar("xEdBc", "Time of day", "t", "", "")]
        public double time
        {
            get { return _time; }
            set
            {
                _time = value;
                //OnPropertyChanged("time");
                //this.run();
            }
        }

        [ModelVar("Vs9lY", "Ambient air Temperature", "T", "a", "°C", "t")]
        //[System.Xml.Serialization.XmlIgnore]
        public double temp
        {
            get { return envModel.getTemp(time); }
        }
        //[System.Xml.Serialization.XmlIgnore]
        [ModelVar("hXaSv", "Vapour pressure deficit", "VPD", "", "kPa", "t")]
        public double VPD
        {
            get { return envModel.getVPD(time); }
        }

        [ModelVar("DTXwi", "Total daily intercepted solar radiation", "RAD", "DAY", "MJ/m2/day", "", "m2 ground")]
        public double interceptedRadn { get; set; }

        [ModelVar("ewC3T", "Daily canopy solar radiation extinction coefficient", "k", "", "")]
        public double k { get; set; }

        [ModelVar("qRXR7", "Daily biomass accumulated by canopy", "BIO", "Total,DAY", "g/m2", "", "m2 ground")]
        public double dailyBiomass { get; set; }

        [ModelVar("qR227", "Daily above ground biomass accumulated by canopy", "BIO", "Shoot,DAY", "g/m2", "", "m2 ground")]
        public double dailyBiomassShoot { get; set; }

        [ModelVar("DDDR7", "Proportion of above ground biomass to total plant biomass", "P", "Shoot", "", "", "")]
        public double P_ag { get; set; } = 0.8;

        [ModelVar("vtoDc", "ASunlitMAx", "A", "", "")]
        public double AShadedMax { get; set; }

        [ModelVar("Ti3HI", "ASunlitMAx", "A", "", "")]
        public double ASunlitMax { get; set; }

        [ModelVar("8XLI6", "Daily canopy CO2 assimilation", "A", "c_DAY", "mmol/m2/day", "", "m2 ground")]
        public double A { get; set; }

        [ModelVar("ThUS8", "Above ground Radiation Use Efficiency for the day", "RUE", "Shoot,DAY", "g/MJ", "", "g biomass")]
        public double RUE { get; set; }

        #region Model switches and delegates
        private PhotoPathway _photoPathway = PhotoPathway.C3;
        [ModelPar("0gDem", "Photosynthetic pathway", "", "", "")]
        public PhotoPathway photoPathway
        {
            get
            {
                return _photoPathway;
            }
            set
            {
                _photoPathway = value;
                if (photoPathwayChanged != null)
                {
                    photoPathwayChanged(_photoPathway);
                }
                if (initialised)
                {
                    //this.runDaily();
                }
            }
        }

        public delegate void PhotoPathwayNotifier(PhotoPathway p);
        //[System.Xml.Serialization.XmlIgnore]
        public PhotoPathwayNotifier photoPathwayChanged;

        private NitrogenModel _nitrogenModel = NitrogenModel.APSIM;
        [ModelPar("3GxZk", "Nitrogen Model", "", "", "")]
        public NitrogenModel nitrogenModel
        {
            get
            {
                return _nitrogenModel;
            }
            set
            {
                _nitrogenModel = value;
                if (nitrogenModelChanged != null)
                {
                    nitrogenModelChanged();
                }
                if (initialised)
                {
                    //this.runDaily();
                }
            }
        }

        public delegate void NitrogenModelNotifier();
        //[System.Xml.Serialization.XmlIgnore]
        public NitrogenModelNotifier nitrogenModelChanged;

        private ElectronTransportModel _electronTransportModel = ElectronTransportModel.EXTENDED;
        //[ModelPar("Ndx07", "Electron Transport Model", "", "")]
        public ElectronTransportModel electronTransportModel
        {
            get
            {
                return _electronTransportModel;
            }
            set
            {
                _electronTransportModel = value;
                if (electronTransportModelChanged != null)
                {
                    electronTransportModelChanged();
                }
                if (initialised)
                {
                    //this.runDaily();
                }
            }
        }

        public delegate void ElectronTransportModelNotifier();
        //[System.Xml.Serialization.XmlIgnore]
        public ElectronTransportModelNotifier electronTransportModelChanged;

        private ConductanceModel _conductanceModel = ConductanceModel.DETAILED;
        //[ModelPar("pvSkJ", "Photosynthetic pathway", "P", "")]
        public ConductanceModel conductanceModel
        {
            get
            {
                return _conductanceModel;
            }
            set
            {
                _conductanceModel = value;
                if (conductanceModelChanged != null)
                {
                    conductanceModelChanged();
                }
                if (initialised)
                {
                    //this.runDaily();
                }
            }
        }

        public delegate void ConductanceModelNotifier();
        //[System.Xml.Serialization.XmlIgnore]
        public ConductanceModelNotifier conductanceModelChanged;
        #endregion

        public int count;
        //---------------------------------------------------------------------------
        //public void runDaily()
        //{

        //    if (!initialised)
        //    {
        //        return;
        //    }

        //    if (notifyStart != null)
        //    {
        //        notifyStart(true);
        //    }

        //    double modelTime = this.time;
        //    instants = new List<double>();
        //    ass = new List<double>();

        //    Ios = new List<double>();
        //    Idiffs = new List<double>();
        //    Idirs = new List<double>();

        //    SunlitACs = new List<double>();
        //    SunlitAJs = new List<double>();
        //    ShadedACs = new List<double>();
        //    ShadedAJs = new List<double>();

        //    dailyBiomass = 0;
        //    interceptedRadn = 0;

        //    AShadedMax = 0;
        //    ASunlitMax = 0;

        //    envModel.run();

        //    for (double time = minTime; time <= maxTime; time += timeStep)
        //    {
        //        this.time = time;
        //        instants.Add(canopy.Ac.Sum());
        //        ass.Add(canopy.biomassC.Sum() * timeStep);

        //        Idiffs.Add(envModel.diffuseRadiationPAR);
        //        Idirs.Add(envModel.directRadiationPAR);
        //        Ios.Add(envModel.diffuseRadiationPAR + envModel.directRadiationPAR);

        //        dailyBiomass += canopy.totalBiomassC;

        //        double propIntRadn = canopy.propnInterceptedRadns.Sum();
        //        //double propIntRadn = canopy.propnInterceptedRadns[0];

        //        double interceptedRadnTimestep = envModel.totalIncidentRadiation * propIntRadn * timeStep * 3600;
        //        interceptedRadn += interceptedRadnTimestep;

        //        //double RUEtimeStep = canopy.totalBiomassC / interceptedRadnTimestep;
        //        double RUEtimeStep = canopy.biomassC[0] / interceptedRadnTimestep;

        //        PhotosynthesisModel PM = this;

        //        if (ASunlitMax < sunlit.A.Max())
        //        {
        //            ASunlitMax = sunlit.A.Max();
        //        }

        //        if (AShadedMax < shaded.A.Max())
        //        {
        //            AShadedMax = shaded.A.Max();
        //        }
        //    }

        //    dailyBiomassShoot = dailyBiomass * P_ag;

        //    RUE = dailyBiomassShoot / interceptedRadn;
        //    k = -Math.Log(1 - interceptedRadn / envModel.radn) / canopy.LAI;

        //    A = instants.Sum() / 1000;

        //    this.time = modelTime;

        //    if (notifyFinish != null)
        //    {
        //        notifyFinish();
        //    }

        //    run(true);

        //    if (notifyFinishDay != null)
        //    {
        //        notifyFinishDay();
        //    }
        //}
        //---------------------------------------------------------------------------
        public PhotosynthesisModel()
        {
            envModel = new EnvironmentModel();
            canopy = new LeafCanopy();
            canopy.nLayers = 1;
            //sunlit = new SunlitCanopy();
            //shaded = new ShadedCanopy();

            //canopy.notifyChanged += runDaily;
            photoPathwayChanged += canopy.photoPathwayChanged;

            //envModel.notify += runDaily;
            parameters = new List<string>();
            variables = new List<string>();
            sunlitShadedVariables = new List<string>();
            canopyVariables = new List<string>();

            //canopy.layerNumberChanged += sunlit.initArrays;
            //canopy.layerNumberChanged += shaded.initArrays;
        }
        //---------------------------------------------------------------------------
        public void run()
        {
            if (initialised)
            {
                run(true);
            }
            //runDaily();
        }
        //---------------------------------------------------------------------------
        public virtual void run(double time, double swAvail, double maxHourlyT = -1, double sunlitPC = 0, double shadedPC = 0)
        {
            this.time = time;
            if (maxHourlyT == -1)
            {
                run(false, swAvail);
            }
            else
            {
                run(false, swAvail, maxHourlyT, sunlitPC, shadedPC);
            }
        }

        //---------------------------------------------------------------------------
        public virtual void run(bool sendNotification, double swAvail = 0, double maxHourlyT = -1, double sunlitFraction = 0, double shadedFraction = 0)
        {
            
        }

        //--------------------------------------------------------------------------
        //public void calcDailyAChartData(PhotoLayerSolver pls, LeafCanopy canopy, SunlitShadedCanopy sunlit, SunlitShadedCanopy shaded)
        //{
        //    SunlitACs = new List<double>();
        //    SunlitAJs = new List<double>();
        //    ShadedACs = new List<double>();
        //    ShadedAJs = new List<double>();

        //    //C3 incrementals
        //    int start = 0;
        //    int finish = 600;
        //    int step = 5;

        //    if (_photoPathway == PhotoPathway.C4)
        //    {
        //        start = 0;
        //        finish = 12000;
        //        step = 100;
        //    }

        //    // Layer 1 only

        //    for (int i = start; i <= finish; i += step)
        //    {
        //        double sunlitAc = pls.calcAc(i, canopy, sunlit, 0);
        //        double sunlitAj = pls.calcAj(i, canopy, sunlit, 0);
        //        double shadedAc = pls.calcAc(i, canopy, shaded, 0);
        //        double shadedAj = pls.calcAj(i, canopy, shaded, 0);

        //        sunlitAc = double.IsNaN(sunlitAc) ? 0 : Math.Max(0, sunlitAc);
        //        sunlitAj = double.IsNaN(sunlitAj) ? 0 : Math.Max(0, sunlitAj);
        //        shadedAc = double.IsNaN(shadedAc) ? 0 : Math.Max(0, shadedAc);
        //        shadedAj = double.IsNaN(shadedAj) ? 0 : Math.Max(0, shadedAj);

        //        SunlitACs.Add(sunlitAc);
        //        SunlitAJs.Add(sunlitAj);
        //        ShadedACs.Add(shadedAc);
        //        ShadedAJs.Add(shadedAj);
        //    }
        //}
        ////---------------------------------------------------------------------------
        //public void runDaily(double timeStep)
        //{
        //    double _time = time;
        //    double dailyBiomass = 0;
        //    double interceptedRadn = 0;
        //    double RUE;

        //    for (double i = 0; i < 24; i += timeStep)
        //    {
        //        time = i;
        //        run(false);
        //        dailyBiomass += canopy.totalBiomassC;

        //        double propIntRadn = canopy.propnInterceptedRadns.Sum();
        //        double interceptedRadnTimestep = envModel.totalIncidentRadiation * envModel.conversionFactor / 10E12;
        //        interceptedRadn += interceptedRadnTimestep * propIntRadn * timeStep * 3600;

        //        double RUEtimeStep = canopy.totalBiomassC / interceptedRadnTimestep;
        //    }

        //    RUE = dailyBiomass / interceptedRadn;

        //    time = _time;
        //}
        #endregion
    }
}
