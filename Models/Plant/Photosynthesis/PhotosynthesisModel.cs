using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class PhotosynthesisModel : NotifyablePropertyClass
    {
        /// <summary></summary>
        public enum PhotoPathway
        {     /// <summary></summary>
            C3,
            /// <summary></summary>
            C4
        };
        /// <summary></summary>
        public enum NitrogenModel {
            /// <summary></summary>
            APSIM,
            /// <summary></summary>
            EXPONENTIAL
        };
        /// <summary></summary>
        public enum ElectronTransportModel {
            /// <summary></summary>
            EMPIRICAL,
            /// <summary></summary>
            EXTENDED
        };
        /// <summary></summary>
        public enum ConductanceModel
        {     /// <summary></summary>
            SIMPLE,
            /// <summary></summary>
            DETAILED
        };

        /// <summary></summary>
        public List<string> parameters;
        /// <summary></summary>
        public List<string> variables;
        /// <summary></summary>
        public List<string> sunlitShadedVariables;
        /// <summary></summary>
        public List<string> canopyVariables;

        #region Global
        /// <summary></summary>
        public EnvironmentModel envModel { get; set; }
        /// <summary></summary>
        public LeafCanopy canopy { get; set; }
        /// <summary></summary>
        public SunlitShadedCanopy sunlit { get; set; }
        /// <summary></summary>
        public SunlitShadedCanopy shaded { get; set; }

        /// <summary></summary>
        public SunlitCanopy sunlitAC1;
        /// <summary></summary>
        public SunlitCanopy sunlitAJ;
        /// <summary></summary>
        public ShadedCanopy shadedAC1;
        /// <summary></summary>
        public ShadedCanopy shadedAJ;

        /// <summary></summary>
        public delegate void Notify();
        /// <summary></summary>
        public delegate void NotifyBool(bool flag);
        /// <summary></summary>
        public Notify notifyFinish;
        /// <summary></summary>
        public NotifyBool notifyStart;
        /// <summary></summary>
        public Notify notifyFinishDay;
        /// <summary></summary>
        public Notify notifyNewParSet;


        /// <summary></summary>
        protected double _time = 10.0;
        /// <summary></summary>
        protected double _timeStep = 1;

        /// <summary></summary>
        public bool initialised = false;

        /// <summary></summary>
        public List<double> instants;

        /// <summary></summary>
        public List<double> ass;

        /// <summary></summary>
        public List<double> Ios = new List<double>();
        /// <summary></summary>
        public List<double> Idirs = new List<double>();
        /// <summary></summary>
        public List<double> Idiffs = new List<double>();

        /// <summary></summary>
        [ModelVar("SLAC88", "Time step", "t", "", "")]
        public List<double> SunlitACs { get; set; }
        /// <summary></summary>
        [ModelVar("SLAJ73", "", "", "", "")]
        public List<double> SunlitAJs { get; set; }
        /// <summary></summary>
        [ModelVar("SSAC11", "", "", "", "")]
        public List<double> ShadedACs { get; set; }
        /// <summary></summary>
        [ModelVar("SSAK00", "", "", "", "")]
        public List<double> ShadedAJs { get; set; }

        /// <summary></summary>
        public int minTime = 6;
        /// <summary></summary>
        public int maxTime = 18;

        /// <summary></summary>
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

        /// <summary></summary>
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

        /// <summary></summary>
        [ModelVar("Vs9lY", "Ambient air Temperature", "T", "a", "°C", "t")]
        public double temp
        {
            get { return envModel.getTemp(time); }
        }
        /// <summary></summary>
        [ModelVar("hXaSv", "Vapour pressure deficit", "VPD", "", "kPa", "t")]
        public double VPD
        {
            get { return envModel.getVPD(time); }
        }

        /// <summary></summary>
        [ModelVar("DTXwi", "Total daily intercepted solar radiation", "RAD", "DAY", "MJ/m2/day", "", "m2 ground")]
        public double interceptedRadn { get; set; }

        /// <summary></summary>
        [ModelVar("ewC3T", "Daily canopy solar radiation extinction coefficient", "k", "", "")]
        public double k { get; set; }

        /// <summary></summary>
        [ModelVar("qRXR7", "Daily biomass accumulated by canopy", "BIO", "Total,DAY", "g/m2", "", "m2 ground")]
        public double dailyBiomass { get; set; }

        /// <summary></summary>
        [ModelVar("qR227", "Daily above ground biomass accumulated by canopy", "BIO", "Shoot,DAY", "g/m2", "", "m2 ground")]
        public double dailyBiomassShoot { get; set; }

        /// <summary></summary>
        [ModelVar("DDDR7", "Proportion of above ground biomass to total plant biomass", "P", "Shoot", "", "", "")]
        public double P_ag { get; set; }

        /// <summary></summary>
        [ModelVar("vtoDc", "ASunlitMAx", "A", "", "")]
        public double AShadedMax { get; set; }

        /// <summary></summary>
        [ModelVar("Ti3HI", "ASunlitMAx", "A", "", "")]
        public double ASunlitMax { get; set; }

        /// <summary></summary>
        [ModelVar("8XLI6", "Daily canopy CO2 assimilation", "A", "c_DAY", "mmol/m2/day", "", "m2 ground")]
        public double A { get; set; }

        /// <summary></summary>
        [ModelVar("ThUS8", "Above ground Radiation Use Efficiency for the day", "RUE", "Shoot,DAY", "g/MJ", "", "g biomass")]
        public double RUE { get; set; }

        #region Model switches and delegates
        private PhotoPathway _photoPathway = PhotoPathway.C3;
        /// <summary></summary>
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

        /// <summary></summary>
        public delegate void PhotoPathwayNotifier(PhotoPathway p);
        /// <summary></summary>
        public PhotoPathwayNotifier photoPathwayChanged;

        private NitrogenModel _nitrogenModel = NitrogenModel.APSIM;
        /// <summary></summary>
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
            }
        }

        /// <summary></summary>
        public delegate void NitrogenModelNotifier();
        /// <summary></summary>
        public NitrogenModelNotifier nitrogenModelChanged;

        private ElectronTransportModel _electronTransportModel = ElectronTransportModel.EXTENDED;
        /// <summary></summary>
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

        /// <summary></summary>
        public delegate void ElectronTransportModelNotifier();
        /// <summary></summary>
        public ElectronTransportModelNotifier electronTransportModelChanged;

        private ConductanceModel _conductanceModel = ConductanceModel.DETAILED;
        /// <summary></summary>
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

        /// <summary></summary>
        public delegate void ConductanceModelNotifier();
        /// <summary></summary>
        public ConductanceModelNotifier conductanceModelChanged;
        #endregion

        /// <summary></summary>
        public int count;

        //---------------------------------------------------------------------------
        /// <summary></summary>
        public PhotosynthesisModel()
        {
            SunlitACs = new List<double>();
            SunlitAJs = new List<double>();
            ShadedACs = new List<double>();
            ShadedAJs = new List<double>();
            P_ag = 0.8;

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
        /// <summary></summary>
        public void run()
        {
            if (initialised)
            {
                run(true);
            }
            //runDaily();
        }
        //---------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="swAvail"></param>
        /// <param name="maxHourlyT"></param>
        /// <param name="sunlitPC"></param>
        /// <param name="shadedPC"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendNotification"></param>
        /// <param name="swAvail"></param>
        /// <param name="maxHourlyT"></param>
        /// <param name="sunlitFraction"></param>
        /// <param name="shadedFraction"></param>
        public virtual void run(bool sendNotification, double swAvail = 0, double maxHourlyT = -1, double sunlitFraction = 0, double shadedFraction = 0)
        {
            
        }
#endregion
    }
}
