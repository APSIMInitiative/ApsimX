using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using Models.Core;
using Models;
using Models.PMF;
using System.Xml.Serialization;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models
{
    /// <summary>
    /// A new canopy type
    /// </summary>
    [Serializable]
    public class NewCanopyType
    {
        /// <summary>The sender</summary>
        public string sender = "";
        /// <summary>The height</summary>
        public double height;
        /// <summary>The depth</summary>
        public double depth;
        /// <summary>The lai</summary>
        public double lai;
        /// <summary>The lai_tot</summary>
        public double lai_tot;
        /// <summary>The cover</summary>
        public double cover;
        /// <summary>The cover_tot</summary>
        public double cover_tot;
    }

    /// <summary>
    /// 
    /// </summary>
    public class KeyValueArraypair_listType
    {
        /// <summary>The key</summary>
        public string key = "";
        /// <summary>The value</summary>
        public double value;
    }

    /// <summary>
    /// 
    /// </summary>
    public class ChangeGSMaxType
    {
        /// <summary>The component</summary>
        public string component = "";
        /// <summary>The DLT</summary>
        public double dlt;
    }
    /// <summary>
    /// 
    /// </summary>
    public class KeyValueArrayType
    {
        /// <summary>The pair_list</summary>
        public KeyValueArraypair_listType[] pair_list;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="Data">The data.</param>
    public delegate void KeyValueArraypair_listDelegate(KeyValueArraypair_listType Data);


    /// <summary>
    /// A more-or-less direct port of the Fortran MicroMet model
    /// Ported by Eric Zurcher Jun 2011, first to C#, then automatically
    /// to VB via the converter in SharpDevelop.
    /// Ported back to C# by Dean Holzworth
    /// </summary>
    /// <remarks>
    /// <para>
    /// I have generally followed the original division of interface code and "science" code
    /// into different units (formerly MicroMet.for and MicroScience.for)
    /// </para>
    /// <para>
    /// Function routines were changed slightly as part of the conversion process. The "micromet_"
    /// prefixes were dropped, as the functions are now members of a MicroMet class, and that class
    /// membership keeps them distinguishable from other functions. A prefix of "Calc" was added to
    /// some function names (e.g., CalcAverageT) if, after dropping the old "micromet_" prefix, there
    /// was potential confusion between the function name and a variable name.
    /// </para>
    /// <para> The following Fortran routines, originally in MicroScience.for, were NOT converted,
    /// because they were not actively being used:</para>
    /// <para>    micromet_PenmanMonteith (the converted routine below was originally micromet_Penman_Monteith from MicroMet.for)</para>
    /// <para>    micromet_ActualCanopyCond (the routine below was originally micromet_CanopyConductance)</para>
    /// <para>    micromet_FrictionVelocity</para>
    /// <para>    micromet_ZeroPlaneDispl</para>
    /// <para>    micromet_RoughnessLength</para>
    /// <para>    micromet_Radn2SolRad</para>
    /// <para>    micromet_FreeEvapRate</para>
    /// <para>    micromet_AerodynamicConductance (the routine below was originally micromet_AerodynamicConductanceFAO)</para>
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public partial class MicroClimate : Model
    {
        /// <summary>The zone</summary>
        [Link]
        Zone zone = null;

        /// <summary>The clock</summary>
        [Link]
        Clock Clock = null;

        /// <summary>The weather</summary>
        [Link]
        IWeather Weather = null;

        /// <summary>The _albedo</summary>
        private double _albedo = 0;

        /// <summary>Constructor</summary>
        public MicroClimate()
        {
            a_interception = 0.0;
            b_interception = 1.0;
            c_interception = 0.0;
            d_interception = 0.0;
            soil_albedo = 0.23;
        }


        #region "Parameters used to initialise the model"
        #region "Parameters set in the GUI by the user"
        /// <summary>Gets or sets the a_interception.</summary>
        /// <value>The a_interception.</value>
        [Description("a_interception")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm/mm")]
        public double a_interception { get; set; }

        /// <summary>Gets or sets the b_interception.</summary>
        /// <value>The b_interception.</value>
        [Description("b_interception")]
        [Bounds(Lower = 0.0, Upper = 5.0)]
        public double b_interception {get; set;}

        /// <summary>Gets or sets the c_interception.</summary>
        /// <value>The c_interception.</value>
        [Description("c_interception")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        public double c_interception { get; set; }

        /// <summary>Gets or sets the d_interception.</summary>
        /// <value>The d_interception.</value>
        [Description("d_interception")]
        [Bounds(Lower = 0.0, Upper = 20.0)]
        [Units("mm")]
        public double d_interception { get; set; }

        /// <summary>Gets or sets the soil_albedo.</summary>
        /// <value>The soil_albedo.</value>
        [Description("soil albedo")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double soil_albedo { get; set; }

        #endregion

        #region "Parameters not normally settable from the GUI by the user"
        /// <summary>The air_pressure</summary>
        [Bounds(Lower = 900.0, Upper = 1100.0)]
        [Units("hPa")]
        [Description("")]

        public double air_pressure = 1010;
        /// <summary>The soil_emissivity</summary>
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("")]
        [Description("")]

        public double soil_emissivity = 0.96;

        /// <summary>The sun_angle</summary>
        [Bounds(Lower = -20.0, Upper = 20.0)]
        [Units("deg")]
        [Description("")]

        public double sun_angle = 15.0;
        /// <summary>The soil_heat_flux_fraction</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [Description("")]

        public double soil_heat_flux_fraction = 0.4;
        /// <summary>The night_interception_fraction</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [Description("")]

        public double night_interception_fraction = 0.5;
        /// <summary>The windspeed_default</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m/s")]
        [Description("")]

        public double windspeed_default = 3.0;
        /// <summary>The refheight</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m")]
        [Description("")]

        public double refheight = 2.0;
        /// <summary>The albedo</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("")]

        public double albedo = 0.15;
        /// <summary>The emissivity</summary>
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("0-1")]
        [Description("")]

        public double emissivity = 0.96;
        /// <summary>The gsmax</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("m/s")]
        [Description("")]

        public double gsmax = 0.01;
        /// <summary>The R50</summary>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("W/m^2")]
        [Description("")]

        public double r50 = 200;
        #endregion

        #endregion

        #region "Outputs we make available"

        /// <summary>Gets the interception.</summary>
        /// <value>The interception.</value>
        [Units("mm")]
        public double interception
        {
            get
            {
                double totalInterception = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= ComponentData.Count - 1; j++)
                    {
                        totalInterception += ComponentData[j].interception[i];
                    }
                }
                return totalInterception;
            }
        }

        /// <summary>Gets the gc.</summary>
        /// <value>The gc.</value>
        public double gc
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((ComponentData.Count > 0) && (numLayers > 0)) ? ComponentData[0].Gc[0] : 0.0; }
        }

        /// <summary>Gets the ga.</summary>
        /// <value>The ga.</value>
        public double ga
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((ComponentData.Count > 0) && (numLayers > 0)) ? ComponentData[0].Ga[0] : 0.0; }
        }

        /// <summary>Gets the petr.</summary>
        /// <value>The petr.</value>
        public double petr
        {
            get
            {
                double totalPetr = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= ComponentData.Count - 1; j++)
                    {
                        totalPetr += ComponentData[j].PETr[i];
                    }
                }
                return totalPetr;
            }
        }

        /// <summary>Gets the peta.</summary>
        /// <value>The peta.</value>
        public double peta
        {
            get
            {
                double totalPeta = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= ComponentData.Count - 1; j++)
                    {
                        totalPeta += ComponentData[j].PETa[i];
                    }
                }
                return totalPeta;
            }
        }

        /// <summary>Gets the net_radn.</summary>
        /// <value>The net_radn.</value>
        public double net_radn
        {
            get { return radn * (1.0 - _albedo) + netLongWave; }
        }

        /// <summary>Gets the net_rs.</summary>
        /// <value>The net_rs.</value>
        public double net_rs
        {
            get { return radn * (1.0 - _albedo); }
        }

        /// <summary>Gets the net_rl.</summary>
        /// <value>The net_rl.</value>
        public double net_rl
        {
            get { return netLongWave; }
        }

        /// <summary>The soil_heat</summary>
        [XmlIgnore]
        public double soil_heat = 0.0;

        /// <summary>The dryleaffraction</summary>
        [XmlIgnore]
        public double dryleaffraction = 0.0;

        /// <summary>Gets the gsmax_array.</summary>
        /// <value>The gsmax_array.</value>
        public KeyValueArrayType gsmax_array
        {
            get
            {
                KeyValueArrayType _gsmax_array = new KeyValueArrayType();
                Array.Resize<KeyValueArraypair_listType>(ref _gsmax_array.pair_list, ComponentData.Count);
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    _gsmax_array.pair_list[j] = new KeyValueArraypair_listType();
                    _gsmax_array.pair_list[j].key = ComponentData[j].Name;
                    _gsmax_array.pair_list[j].value = ComponentData[j].Gsmax;
                }
                return _gsmax_array;
            }
        }
        #endregion

        #region "Events to which we subscribe, and their handlers"

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            day = Clock.Today.DayOfYear;
            year = Clock.Today.Year;
            radn = Weather.Radn;
            maxt = Weather.MaxT;
            mint = Weather.MinT;
            rain = Weather.Rain;
            vp = Weather.VP;
            wind = Weather.Wind;
        }

        /// <summary>Called when [change gs maximum].</summary>
        /// <param name="ChangeGSMax">The change gs maximum.</param>
        /// <exception cref="System.Exception">Unknown Canopy Component:  + Convert.ToString(ChangeGSMax.component)</exception>
        [EventSubscribe("ChangeGSMax")]
        private void OnChangeGSMax(ChangeGSMaxType ChangeGSMax)
        {
            int senderIdx = FindComponentIndex(ChangeGSMax.component);
            if (senderIdx < 0)
            {
                throw new Exception("Unknown Canopy Component: " + Convert.ToString(ChangeGSMax.component));
            }
            ComponentData[senderIdx].Gsmax += ChangeGSMax.dlt;
        }

        /// <summary>Gets all canopies in simulation</summary>
        private void GetAllCanopies()
        {
            foreach (ICanopy canopy in Apsim.FindAll(this.Parent, typeof(ICanopy)))
            {
                ComponentDataStruct componentData = ComponentData.Find(c => c.Name == canopy.CanopyType);
                if (componentData == null)
                {
                    componentData = CreateNewComonentData(canopy.CanopyType);
                    Clear(componentData);
                }

                componentData.Name = canopy.CanopyType;
                componentData.Type = canopy.CanopyType;
                componentData.Canopy = canopy;
                componentData.LAI = canopy.LAI;
                componentData.LAItot = canopy.LAITotal;
                componentData.CoverGreen = canopy.CoverGreen;
                componentData.CoverTot = canopy.CoverTotal;
                componentData.Height = Math.Round(canopy.Height, 5) / 1000.0; // Round off a bit and convert mm to m
                componentData.Depth = Math.Round(canopy.Depth, 5) / 1000.0;   // Round off a bit and convert mm to m
                componentData.Canopy = canopy;
            }
        }

        /// <summary>Called when [do canopy energy balance].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoEnergyArbitration")]
        private void DoEnergyArbitration(object sender, EventArgs e)
        {
            GetAllCanopies();
            
            MetVariables();
            CanopyCompartments();
            BalanceCanopyEnergy();

            // Loop through all crops and get their potential growth for today.
            foreach (ComponentDataStruct componentData in ComponentData)
            {
                if (componentData.Canopy != null)
                    componentData.Frgr = componentData.Canopy.FRGR;
                else if (componentData.Crop2 != null)
                    componentData.Frgr = componentData.Crop2.CanopyProperties.Frgr;
            }

            CalculateGc();
            CalculateGa();
            CalculateInterception();
            CalculatePM();
            CalculateOmega();

            SendEnergyBalanceEvent();
        }

        /*/// <summary>
        /// Register presence of slurp
        /// </summary>
        [EventSubscribe("StartSlurp")]
        private void OnStartSlurp(object sender, EventArgs e)
        {
            Slurp newSlurp = sender as Slurp;

            int senderIdx = FindComponentIndex(newSlurp.Name);

            // If sender is unknown, add it to the list
            if (senderIdx == -1)
                throw new ApsimXException(FullPath, "Cannot find MicroClimate definition for Slurp");
            ComponentData[senderIdx].Name = newSlurp.Name;
            ComponentData[senderIdx].Type = newSlurp.CropType;
            Clear(ComponentData[senderIdx]);
        } */

        /// <summary>Clears the specified componentdata</summary>
        /// <param name="c">The c.</param>
        private void Clear(ComponentDataStruct c)
        {
            c.CoverGreen = 0;
            c.CoverTot = 0;
            c.Depth = 0;
            c.Frgr = 0;
            c.Height = 0;
            c.K = 0;
            c.Ktot = 0;
            c.LAI = 0;
            c.LAItot = 0;
            Util.ZeroArray(c.layerLAI);
            Util.ZeroArray(c.layerLAItot);
            Util.ZeroArray(c.Ftot);
            Util.ZeroArray(c.Fgreen);
            Util.ZeroArray(c.Rs);
            Util.ZeroArray(c.Rl);
            Util.ZeroArray(c.Rsoil);
            Util.ZeroArray(c.Gc);
            Util.ZeroArray(c.Ga);
            Util.ZeroArray(c.PET);
            Util.ZeroArray(c.PETr);
            Util.ZeroArray(c.PETa);
            Util.ZeroArray(c.Omega);
            Util.ZeroArray(c.interception);
        }

        /// <summary>Called when [loaded].</summary>
        [EventSubscribe("Loaded")]
        private void OnLoaded()
        {
            ComponentData = new List<ComponentDataStruct>();
            foreach (ComponentDataStruct c in ComponentData)
                Clear(c);
            AddCropTypes();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            _albedo = albedo;
            windspeed_checked = false;
            netLongWave = 0;
            sumRs = 0;
            averageT = 0;
            sunshineHours = 0;
            fractionClearSky = 0;
            dayLength = 0;
            dayLengthLight = 0;
            numLayers = 0;
            DeltaZ = new double[-1 + 1];
            layerKtot = new double[-1 + 1];
            layerLAIsum = new double[-1 + 1];
           
        }


        #endregion

        #region "Useful constants"
        // Teten coefficients
        /// <summary>The SVP_ a</summary>
        private const double svp_A = 6.106;
        // Teten coefficients
        /// <summary>The SVP_ b</summary>
        private const double svp_B = 17.27;
        // Teten coefficients
        /// <summary>The SVP_ c</summary>
        private const double svp_C = 237.3;
        // 0 C in Kelvin (g_k)
        /// <summary>The abs_temp</summary>
        private const double abs_temp = 273.16;
        // universal gas constant (J/mol/K)
        /// <summary>The r_gas</summary>
        private const double r_gas = 8.3143;
        // molecular weight water (kg/mol)
        /// <summary>The mwh2o</summary>
        private const double mwh2o = 0.018016;
        // molecular weight air (kg/mol)
        /// <summary>The mwair</summary>
        private const double mwair = 0.02897;
        // molecular fraction of water to air ()
        /// <summary>The molef</summary>
        private const double molef = mwh2o / mwair;
        // Specific heat of air at constant pressure (J/kg/K)
        /// <summary>The cp</summary>
        private const double Cp = 1010.0;
        // Stefan-Boltzman constant
        /// <summary>The stef_boltz</summary>
        private const double stef_boltz = 5.67E-08;
        // constant for cloud effect on longwave radiation
        /// <summary>The c_cloud</summary>
        private const double c_cloud = 0.1;
        // convert degrees to radians
        /// <summary>The deg2 RAD</summary>
        private const double Deg2Rad = Math.PI / 180.0;
        // kg/m3
        /// <summary>The rho w</summary>
        private const double RhoW = 998.0;
        // weights vpd towards vpd at maximum temperature
        /// <summary>The svp_fract</summary>
        private const double svp_fract = 0.66;
        /// <summary>The sun set angle</summary>
        private const double SunSetAngle = 0.0;
        // hours to seconds
        /// <summary>The HR2S</summary>
        private const double hr2s = 60.0 * 60.0;
        #endregion
        #region "Various class variables"

        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class ComponentDataStruct
        {
            /// <summary>The name</summary>
            public string Name;
            /// <summary>The type</summary>
            public string Type;
            /// <summary>The crop</summary>
            public ICrop Crop;
            /// <summary>The crop2</summary>
            public ICrop2 Crop2;

            /// <summary>
            /// The canopy
            /// </summary>
            public ICanopy Canopy;

            /// <summary>The lai</summary>
            [XmlIgnore]
            public double LAI;
            /// <summary>The la itot</summary>
            [XmlIgnore]
            public double LAItot;
            /// <summary>The cover green</summary>
            [XmlIgnore]
            public double CoverGreen;
            /// <summary>The cover tot</summary>
            [XmlIgnore]
            public double CoverTot;
            /// <summary>The ktot</summary>
            [XmlIgnore]
            public double Ktot;
            /// <summary>The k</summary>
            [XmlIgnore]
            public double K;
            /// <summary>The height</summary>
            [XmlIgnore]
            public double Height;
            /// <summary>The depth</summary>
            [XmlIgnore]
            public double Depth;
            /// <summary>Gets or sets the albedo.</summary>
            /// <value>The albedo.</value>
            [XmlIgnore]
            public double Albedo {get; set;}
            /// <summary>Gets or sets the emissivity.</summary>
            /// <value>The emissivity.</value>
            [XmlIgnore]
            public double Emissivity {get; set;}
            /// <summary>Gets or sets the gsmax.</summary>
            /// <value>The gsmax.</value>
            [XmlIgnore]
            public double Gsmax {get; set;}
            /// <summary>Gets or sets the R50.</summary>
            /// <value>The R50.</value>
            [XmlIgnore]
            public double R50 {get; set;}
            /// <summary>The FRGR</summary>
            [XmlIgnore]
            public double Frgr;
            /// <summary>The layer lai</summary>
            [XmlIgnore]
            public double[] layerLAI;
            /// <summary>The layer la itot</summary>
            [XmlIgnore]
            public double[] layerLAItot;
            /// <summary>The ftot</summary>
            [XmlIgnore]
            public double[] Ftot;
            /// <summary>The fgreen</summary>
            [XmlIgnore]
            public double[] Fgreen;
            /// <summary>The rs</summary>
            [XmlIgnore]
            public double[] Rs;
            /// <summary>The rl</summary>
            [XmlIgnore]
            public double[] Rl;
            /// <summary>The rsoil</summary>
            [XmlIgnore]
            public double[] Rsoil;
            /// <summary>The gc</summary>
            [XmlIgnore]
            public double[] Gc;
            /// <summary>The ga</summary>
            [XmlIgnore]
            public double[] Ga;
            /// <summary>The pet</summary>
            [XmlIgnore]
            public double[] PET;
            /// <summary>The pe tr</summary>
            [XmlIgnore]
            public double[] PETr;
            /// <summary>The pe ta</summary>
            [XmlIgnore]
            public double[] PETa;
            /// <summary>The omega</summary>
            [XmlIgnore]
            public double[] Omega;
            /// <summary>The interception</summary>
            [XmlIgnore]
            public double[] interception;
        }

        /// <summary>Adds the crop types.</summary>
        private void AddCropTypes()
        {
            // Could we keep this list in alphabetical order, please
            ComponentDataDefinitions.Clear();
            SetupCropTypes("AgPasture", "Crop");
            SetupCropTypes("bambatsi", "C4grass");
            SetupCropTypes("banksia", "Tree");
            SetupCropTypes("barley", "Crop");
            SetupCropTypes("broccoli", "Crop");
            SetupCropTypes("Browntop", "Grass");
            SetupCropTypes("camaldulensis", "Tree");
            SetupCropTypes("canola", "Crop");
            SetupCropTypes("Carrots4", "Crop");
            SetupCropTypes("chickpea", "Crop");
            SetupCropTypes("Chicory", "Forage");
            SetupCropTypes("Cocksfoot", "Grass");
            SetupCropTypes("crop", "Crop");
            SetupCropTypes("danthonia", "Grass");
            SetupCropTypes("eucalyptus", "Tree");
            SetupCropTypes("fieldpea", "Crop");
            SetupCropTypes("frenchbean", "Crop");
            SetupCropTypes("globulus", "Tree");
            SetupCropTypes("grass", "Grass");
            SetupCropTypes("kale2", "Crop");
            SetupCropTypes("lolium_rigidum", "Crop");
            SetupCropTypes("lucerne", "Crop");
            SetupCropTypes("maize", "Crop");
            SetupCropTypes("MCSP", "Crop");
            SetupCropTypes("nativepasture", "C4Grass");
            SetupCropTypes("oats", "Crop");
            SetupCropTypes("oilmallee", "Tree");
            SetupCropTypes("oilpalm", "Tree");
            SetupCropTypes("Paspalum", "Grass");
            SetupCropTypes("Plantain", "Forage");
            SetupCropTypes("PMFSlurp", "Crop");
            SetupCropTypes("potato", "Potato");
            SetupCropTypes("Kikuyu", "Grass");
            SetupCropTypes("raphanus_raphanistrum", "Crop");
            SetupCropTypes("ryegrass", "Grass");
            SetupCropTypes("saltbush", "Tree");
            SetupCropTypes("SimpleTree", "Tree");
            SetupCropTypes("Slurp", "Crop");
            SetupCropTypes("sorghum", "Crop");
            SetupCropTypes("sugar", "Crop");
            SetupCropTypes("Sward", "Pasture");
            SetupCropTypes("tree", "Tree");
            SetupCropTypes("TallFescue", "Grass");
            SetupCropTypes("understorey", "Crop");
            SetupCropTypes("vine", "Crop");
            SetupCropTypes("weed", "Crop");
            SetupCropTypes("wheat", "Crop");
            SetupCropTypes("Tef", "Crop");
            SetupCropTypes("WheatPMFPrototype", "Crop");
            SetupCropTypes("WhiteClover", "Legume");
            SetupCropTypes("FodderBeet", "Crop");
        }

        /// <summary>Setups the crop types.</summary>
        /// <param name="Name">The name.</param>
        /// <param name="Type">The type.</param>
        private void SetupCropTypes(string Name, string Type)
        {
            ComponentDataStruct CropType = new ComponentDataStruct();
            CropType.Name = Name;

            //Set defalst
            CropType.Albedo = 0.15;
            CropType.Gsmax = 0.01;
            CropType.Emissivity = 0.96;
            CropType.R50 = 200;

            //Override type specific values
            if (Type.Equals("Crop"))
            {
                CropType.Albedo = 0.26;
                CropType.Gsmax=0.011;
            }
            if (Type.Equals("Potato"))
            {
                CropType.Albedo = 0.26;
                CropType.Gsmax = 0.03;
            }
            else if (Type.Equals("Grass"))
            {
                CropType.Albedo = 0.23;
            }
            else if (Type.Equals("C4grass"))
            {
                CropType.Albedo = 0.23;
                CropType.Gsmax = 0.015;
                CropType.R50 = 150;
            }
            else if (Type.Equals("Tree"))
            {
                CropType.Albedo = 0.15;
                CropType.Gsmax = 0.005;
            }
            else if (Type.Equals("Tree2"))
            {
                CropType.Albedo = 0.15;
                CropType.R50 = 100;
            }
            else if (Type.Equals("Pasture") || Type.Equals("Legume") || Type.Equals("Forage"))
            { // added by rcichota when spliting species in agpasture, still setting all parameters the same, will change in the future
                CropType.Albedo = 0.26;
                CropType.Gsmax = 0.011;
            }

            ComponentDataDefinitions.Add(CropType);
        }

        /// <summary>The maxt</summary>
        private double maxt;
        /// <summary>The mint</summary>
        private double mint;
        /// <summary>The radn</summary>
        private double radn;
        /// <summary>The rain</summary>
        private double rain;
        /// <summary>The vp</summary>
        private double vp;
        /// <summary>The wind</summary>
        private double wind;
        /// <summary>The use_external_windspeed</summary>
        private bool use_external_windspeed;

        /// <summary>The windspeed_checked</summary>
        private bool windspeed_checked = false;
        /// <summary>The day</summary>
        private int day;

        /// <summary>The year</summary>
        private int year;
        /// <summary>The net long wave</summary>
        private double netLongWave;
        /// <summary>The sum rs</summary>
        private double sumRs;
        /// <summary>The average t</summary>
        private double averageT;
        /// <summary>The sunshine hours</summary>
        private double sunshineHours;
        /// <summary>The fraction clear sky</summary>
        private double fractionClearSky;
        /// <summary>The day length</summary>
        private double dayLength;
        /// <summary>The day length light</summary>
        private double dayLengthLight;
        /// <summary>The delta z</summary>
        private double[] DeltaZ = new double[-1 + 1];
        /// <summary>The layer ktot</summary>
        private double[] layerKtot = new double[-1 + 1];
        /// <summary>The layer la isum</summary>
        private double[] layerLAIsum = new double[-1 + 1];
        /// <summary>The number layers</summary>
        private int numLayers;

        private List<ComponentDataStruct> ComponentDataDefinitions = new List<ComponentDataStruct>();

        /// <summary>Gets or sets the component data.</summary>
        /// <value>The component data.</value>
        [XmlElement("ComponentData")]
        [XmlIgnore]
        public List<ComponentDataStruct> ComponentData { get; set; }

        #endregion

        /// <summary>Fetches the table value.</summary>
        /// <param name="field">The field.</param>
        /// <param name="compNo">The comp no.</param>
        /// <param name="layerNo">The layer no.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Unknown table element:  + field</exception>
        private double FetchTableValue(string field, int compNo, int layerNo)
        {
            if (field == "LAI")
            {
                return ComponentData[compNo].layerLAI[layerNo];
            }
            else if (field == "Ftot")
            {
                return ComponentData[compNo].Ftot[layerNo];
            }
            else if (field == "Fgreen")
            {
                return ComponentData[compNo].Fgreen[layerNo];
            }
            else if (field == "Rs")
            {
                return ComponentData[compNo].Rs[layerNo];
            }
            else if (field == "Rl")
            {
                return ComponentData[compNo].Rl[layerNo];
            }
            else if (field == "Gc")
            {
                return ComponentData[compNo].Gc[layerNo];
            }
            else if (field == "Ga")
            {
                return ComponentData[compNo].Ga[layerNo];
            }
            else if (field == "PET")
            {
                return ComponentData[compNo].PET[layerNo];
            }
            else if (field == "PETr")
            {
                return ComponentData[compNo].PETr[layerNo];
            }
            else if (field == "PETa")
            {
                return ComponentData[compNo].PETa[layerNo];
            }
            else if (field == "Omega")
            {
                return ComponentData[compNo].Omega[layerNo];
            }
            else
            {
                throw new Exception("Unknown table element: " + field);
            }
        }

        /// <summary>Finds the index of the component.</summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private int FindComponentIndex(string name)
        {
            for (int i = 0; i <= ComponentData.Count - 1; i++)
            {
                if (ComponentData[i].Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }
            // Couldn't find - see if the name is in the ComponentDataDefinitions. If it
            // is then add it as a componentdata that we need to handle.
            for (int i = 0; i <= ComponentDataDefinitions.Count - 1; i++)
            {
                if (ComponentDataDefinitions[i].Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    // found
                    ComponentData.Add(ComponentDataDefinitions[i]);
                    return ComponentData.Count - 1;
                }
            }
            return -1;
        }

        private ComponentDataStruct CreateNewComonentData(string name)
        {
            for (int i = 0; i <= ComponentDataDefinitions.Count - 1; i++)
            {
                if (ComponentDataDefinitions[i].Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    // found
                    ComponentData.Add(ComponentDataDefinitions[i]);
                    return ComponentData[ComponentData.Count - 1];
                }
            }
            throw new Exception("Cannot find a MicroClimate definition for " + name);
        }


        /// <summary>Canopies the compartments.</summary>
        private void CanopyCompartments()
        {
            DefineLayers();
            DivideComponents();
            LightExtinction();
        }

        /// <summary>Mets the variables.</summary>
        private void MetVariables()
        {
            // averageT = (maxt + mint) / 2.0;
            averageT = CalcAverageT(mint, maxt);

            // This is the length of time within the day during which
            //  Evaporation will take place
            dayLength = CalcDayLength(Weather.Latitude, day, sun_angle);

            // This is the length of time within the day during which
            // the sun is above the horizon
            dayLengthLight = CalcDayLength(Weather.Latitude, day, SunSetAngle);

            sunshineHours = CalcSunshineHours(radn, dayLengthLight, Weather.Latitude, day);

            fractionClearSky = MathUtilities.Divide(sunshineHours, dayLengthLight, 0.0);
        }

        /// <summary>Break the combined Canopy into layers</summary>
        private void DefineLayers()
        {
            double[] nodes = new double[2 * ComponentData.Count];
            int numNodes = 1;
            for (int compNo = 0; compNo <= ComponentData.Count - 1; compNo++)
            {
                double height = ComponentData[compNo].Height;
                double canopyBase = height - ComponentData[compNo].Depth;
                if (Array.IndexOf(nodes, height) == -1)
                {
                    nodes[numNodes] = height;
                    numNodes = numNodes + 1;
                }
                if (Array.IndexOf(nodes, canopyBase) == -1)
                {
                    nodes[numNodes] = canopyBase;
                    numNodes = numNodes + 1;
                }
            }
            Array.Resize<double>(ref nodes, numNodes);
            Array.Sort(nodes);
            numLayers = numNodes - 1;
            if (DeltaZ.Length != numLayers)
            {
                // Number of layers has changed; adjust array lengths
                Array.Resize<double>(ref DeltaZ, numLayers);
                Array.Resize<double>(ref layerKtot, numLayers);
                Array.Resize<double>(ref layerLAIsum, numLayers);

                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    Array.Resize<double>(ref ComponentData[j].Ftot, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Fgreen, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Rs, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Rl, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Rsoil, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Gc, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Ga, numLayers);
                    Array.Resize<double>(ref ComponentData[j].PET, numLayers);
                    Array.Resize<double>(ref ComponentData[j].PETr, numLayers);
                    Array.Resize<double>(ref ComponentData[j].PETa, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Omega, numLayers);
                    Array.Resize<double>(ref ComponentData[j].interception, numLayers);
                }
            }
            for (int i = 0; i <= numNodes - 2; i++)
            {
                DeltaZ[i] = nodes[i + 1] - nodes[i];
            }
        }

        /// <summary>Break the components into layers</summary>
        private void DivideComponents()
        {
            double[] Ld = new double[ComponentData.Count];
            for (int j = 0; j <= ComponentData.Count - 1; j++)
            {
                ComponentDataStruct componentData = ComponentData[j];

                componentData.layerLAI = new double[numLayers];
                componentData.layerLAItot = new double[numLayers];
                Ld[j] = MathUtilities.Divide(componentData.LAItot, componentData.Depth, 0.0);
            }
            double top = 0.0;
            double bottom = 0.0;

            for (int i = 0; i <= numLayers - 1; i++)
            {
                bottom = top;
                top = top + DeltaZ[i];
                layerLAIsum[i] = 0.0;

                // Calculate LAI for layer i and component j
                // ===========================================
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    if ((componentData.Height > bottom) && (componentData.Height - componentData.Depth < top))
                    {
                        componentData.layerLAItot[i] = Ld[j] * DeltaZ[i];
                        componentData.layerLAI[i] = componentData.layerLAItot[i] * MathUtilities.Divide(componentData.LAI, componentData.LAItot, 0.0);
                        layerLAIsum[i] += componentData.layerLAItot[i];
                    }
                }

                // Calculate fractional contribution for layer i and component j
                // ====================================================================
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    componentData.Ftot[i] = MathUtilities.Divide(componentData.layerLAItot[i], layerLAIsum[i], 0.0);
                    // Note: Sum of Fgreen will be < 1 as it is green over total
                    componentData.Fgreen[i] = MathUtilities.Divide(componentData.layerLAI[i], layerLAIsum[i], 0.0);
                }
            }
        }

        /// <summary>Calculate light extinction parameters</summary>
        /// <exception cref="System.Exception">Unrealistically high cover value in MicroMet i.e. > -.9999</exception>
        private void LightExtinction()
        {
            // Calculate effective K from LAI and cover
            // =========================================
            for (int j = 0; j <= ComponentData.Count - 1; j++)
            {
                ComponentDataStruct componentData = ComponentData[j];

                if (MathUtilities.FloatsAreEqual(ComponentData[j].CoverGreen, 1.0, 1E-05))
                {
                    throw new Exception("Unrealistically high cover value in MicroMet i.e. > -.9999");
                }

                componentData.K = MathUtilities.Divide(-Math.Log(1.0 - componentData.CoverGreen), componentData.LAI, 0.0);
                componentData.Ktot = MathUtilities.Divide(-Math.Log(1.0 - componentData.CoverTot), componentData.LAItot, 0.0);
            }

            // Calculate extinction for individual layers
            // ============================================
            for (int i = 0; i <= numLayers - 1; i++)
            {
                layerKtot[i] = 0.0;
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    layerKtot[i] += componentData.Ftot[i] * componentData.Ktot;

                }
            }
        }

        /// <summary>Perform the overall Canopy Energy Balance</summary>
        private void BalanceCanopyEnergy()
        {
            ShortWaveRadiation();
            EnergyTerms();
            LongWaveRadiation();
            SoilHeatRadiation();
        }

        /// <summary>Calculate the canopy conductance for system compartments</summary>
        private void CalculateGc()
        {
            double Rin = radn;

            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                double Rflux = Rin * 1000000.0 / (dayLength * hr2s) * (1.0 - _albedo);
                double Rint = 0.0;

                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    componentData.Gc[i] = CanopyConductance(componentData.Gsmax, componentData.R50, componentData.Frgr, componentData.Fgreen[i], layerKtot[i], layerLAIsum[i], Rflux);

                    Rint += componentData.Rs[i];
                }
                // Calculate Rin for the next layer down
                Rin -= Rint;
            }
        }
        /// <summary>Calculate the aerodynamic conductance for system compartments</summary>
        private void CalculateGa()
        {
            double windspeed = windspeed_default;
            if (!windspeed_checked)
            {
                object val = zone.Get("windspeed");
                use_external_windspeed = val != null;
                if (use_external_windspeed)
                    windspeed = (double)val;
                windspeed_checked = true;
            }

            double sumDeltaZ = 0.0;
            double sumLAI = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                sumDeltaZ += DeltaZ[i];
                // top height
                // total lai
                sumLAI += layerLAIsum[i];
            }

            double totalGa = AerodynamicConductanceFAO(windspeed, refheight, sumDeltaZ, sumLAI);

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].Ga[i] = totalGa * MathUtilities.Divide(ComponentData[j].Rs[i], sumRs, 0.0);
                }
            }
        }

        /// <summary>Calculate the interception loss of water from the canopy</summary>
        private void CalculateInterception()
        {
            double sumLAI = 0.0;
            double sumLAItot = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    sumLAI += ComponentData[j].layerLAI[i];
                    sumLAItot += ComponentData[j].layerLAItot[i];
                }
            }

            double totalInterception = a_interception * Math.Pow(rain, b_interception) + c_interception * sumLAItot + d_interception;

            totalInterception = Math.Max(0.0, Math.Min(0.99 * rain, totalInterception));

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].interception[i] = MathUtilities.Divide(ComponentData[j].layerLAI[i], sumLAI, 0.0) * totalInterception;
                }
            }
        }

        /// <summary>Calculate the Penman-Monteith water demand</summary>
        private void CalculatePM()
        {
            // zero a few things, and sum a few others
            double sumRl = 0.0;
            double sumRsoil = 0.0;
            double sumInterception = 0.0;
            double freeEvapGa = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];
                    componentData.PET[i] = 0.0;
                    componentData.PETr[i] = 0.0;
                    componentData.PETa[i] = 0.0;
                    sumRl += componentData.Rl[i];
                    sumRsoil += componentData.Rsoil[i];
                    sumInterception += componentData.interception[i];
                    freeEvapGa += componentData.Ga[i];
                }
            }

            double netRadiation = ((1.0 - _albedo) * sumRs + sumRl + sumRsoil) * 1000000.0;
            // MJ/J
            netRadiation = Math.Max(0.0, netRadiation);
            double freeEvapGc = freeEvapGa * 1000000.0;
            // =infinite surface conductance
            double freeEvap = CalcPenmanMonteith(netRadiation, mint, maxt, vp, air_pressure, dayLength, freeEvapGa, freeEvapGc);

            dryleaffraction = 1.0 - MathUtilities.Divide(sumInterception * (1.0 - night_interception_fraction), freeEvap, 0.0);
            dryleaffraction = Math.Max(0.0, dryleaffraction);

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    netRadiation = 1000000.0 * ((1.0 - _albedo) * componentData.Rs[i] + componentData.Rl[i] + componentData.Rsoil[i]);
                    // MJ/J
                    netRadiation = Math.Max(0.0, netRadiation);

                    if (j == 39) 
                        netRadiation += 0.0;
                    componentData.PETr[i] = CalcPETr(netRadiation * dryleaffraction, mint, maxt, air_pressure, componentData.Ga[i], componentData.Gc[i]);

                    componentData.PETa[i] = CalcPETa(mint, maxt, vp, air_pressure, dayLength * dryleaffraction, componentData.Ga[i], componentData.Gc[i]);

                    componentData.PET[i] = componentData.PETr[i] + componentData.PETa[i];
                }
            }
        }

        /// <summary>Calculate the aerodynamic decoupling for system compartments</summary>
        private void CalculateOmega()
        {
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    componentData.Omega[i] = CalcOmega(mint, maxt, air_pressure, componentData.Ga[i], componentData.Gc[i]);
                }
            }
        }

        /// <summary>Send an energy balance event</summary>
        private void SendEnergyBalanceEvent()
        {
            for (int j = 0; j <= ComponentData.Count - 1; j++)
            {
                ComponentDataStruct componentData = ComponentData[j];

                if (componentData.Canopy != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = Convert.ToSingle(DeltaZ[i]);
                        lightProfile[i].amount = Convert.ToSingle(componentData.Rs[i] * RadnGreenFraction(j));
                        totalPotentialEp += componentData.PET[i];
                        totalInterception += componentData.interception[i];
                    }

                    componentData.Canopy.PotentialEP = totalPotentialEp;
                    componentData.Canopy.LightProfile = lightProfile;
                }
                
                else if (componentData.Crop2 != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = Convert.ToSingle(DeltaZ[i]);
                        lightProfile[i].amount = Convert.ToSingle(componentData.Rs[i] * RadnGreenFraction(j));
                        totalPotentialEp += componentData.PET[i];
                        totalInterception += componentData.interception[i];
                    }

                    componentData.Crop2.demandWater = totalPotentialEp;
                    componentData.Crop2.LightProfile = lightProfile;
                }
            }
        }

    }


}