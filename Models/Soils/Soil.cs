namespace Models.Soils
{
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Models.Soils.Standardiser;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// The soil class encapsulates a soil characterisation and 0 or more soil samples.
    /// the methods in this class that return double[] always return using the
    /// "Standard layer structure" i.e. the layer structure as defined by the Water child object.
    /// method. Mapping will occur to achieve this if necessary.
    /// To obtain the "raw", unmapped, values use the child classes e.g. SoilWater, Analysis and Sample.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Zones.CircularZone))]
    [ValidParent(ParentType = typeof(Zones.RectangularZone))]
    public class Soil : Model, ISoil
    {
        /// <summary>Gets the water.</summary>
        private Water waterNode;


        /// <summary>
        /// The multipore water model.  An alternativie soil water model that is not yet fully functional
        /// </summary>
        /// 
        [XmlIgnore]
        public WEIRDO Weirdo;
        /// <summary>A reference to the layer structure node or null if not present.</summary>
        private LayerStructure structure;

        /// <summary>Gets or sets the record number.</summary>
        [Summary]
        [Description("Record number")]
        public int RecordNumber { get; set; }

        /// <summary>Gets or sets the asc order.</summary>
        [Summary]
        [Description("Australian Soil Classification Order")]
        public string ASCOrder { get; set; }

        /// <summary>Gets or sets the asc sub order.</summary>
        [Summary]
        [Description("Australian Soil Classification Sub-Order")]
        public string ASCSubOrder { get; set; }

        /// <summary>Gets or sets the type of the soil.</summary>
        [Summary]
        [Description("Soil texture or other descriptor")]
        public string SoilType { get; set; }

        /// <summary>Gets or sets the name of the local.</summary>
        [Summary]
        [Description("Local name")]
        public string LocalName { get; set; }

        /// <summary>Gets or sets the site.</summary>
        [Summary]
        [Description("Site")]
        public string Site { get; set; }

        /// <summary>Gets or sets the nearest town.</summary>
        [Summary]
        [Description("Nearest town")]
        public string NearestTown { get; set; }

        /// <summary>Gets or sets the region.</summary>
        [Summary]
        [Description("Region")]
        public string Region { get; set; }

        /// <summary>Gets or sets the state.</summary>
        [Summary]
        [Description("State")]
        public string State { get; set; }

        /// <summary>Gets or sets the country.</summary>
        [Summary]
        [Description("Country")]
        public string Country { get; set; }

        /// <summary>Gets or sets the natural vegetation.</summary>
        [Summary]
        [Description("Natural vegetation")]
        public string NaturalVegetation { get; set; }

        /// <summary>Gets or sets the apsoil number.</summary>
        [Summary]
        [Description("APSoil number")]
        public string ApsoilNumber { get; set; }

        /// <summary>Gets or sets the latitude.</summary>
        [Summary]
        [Description("Latitude (WGS84)")]
        public double Latitude { get; set; }

        /// <summary>Gets or sets the longitude.</summary>
        [Summary]
        [Description("Longitude (WGS84)")]
        public double Longitude { get; set; }

        /// <summary>Gets or sets the location accuracy.</summary>
        [Summary]
        [Description("Location accuracy")]
        public string LocationAccuracy { get; set; }

        /// <summary>Gets or sets the data source.</summary>
        [Summary]
        [Description("Data source")]
        public string DataSource { get; set; }

        /// <summary>Gets or sets the comments.</summary>
        [Summary]
        [Description("Comments")]
        public string Comments { get; set; }

        /// <summary>Gets the soil water.</summary>
        [XmlIgnore] public ISoilWater SoilWater { get; private set; }

        /// <summary>Gets the soil organic matter.</summary>
        [XmlIgnore] public SoilOrganicMatter SoilOrganicMatter { get; private set; }

        /// <summary>Gets the soil nitrogen.</summary>
        private ISoilTemperature temperatureModel;

        /// <summary>Gets the initial conditions node.</summary>
        [XmlIgnore] 
        public Sample Initial { get; private set; }

        /// <summary>Called when model has been created.</summary>
        public override void OnCreated()
        {
            FindChildren();
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            FindChildren();
        }

        /// <summary>Find our children.</summary>
        private void FindChildren()
        {
            waterNode = Apsim.Child(this, typeof(Water)) as Water;
            Weirdo = Apsim.Child(this, typeof(WEIRDO)) as WEIRDO;
            structure = Apsim.Child(this, typeof(LayerStructure)) as LayerStructure; 
            SoilWater = Apsim.Child(this, typeof(ISoilWater)) as ISoilWater;
            SoilOrganicMatter = Apsim.Child(this, typeof(SoilOrganicMatter)) as SoilOrganicMatter;
            temperatureModel = Apsim.Child(this, typeof(ISoilTemperature)) as ISoilTemperature;
            Initial = Children.Find(child => child is Sample) as Sample;
            }

        /// <summary>
        /// Water node of soil.
        /// </summary>
        public Water WaterNode { get { return waterNode; } }

        #region Water
        /// <summary>The layering used to parameterise the water node</summary>
        public double[] WaterNodeThickness
        {
            get
            {
                return waterNode.Thickness;
            }
        }


        /// <summary>Return the soil layer thicknesses (mm)</summary>
        [Units("mm")]
        [XmlIgnore]
        public double[] Thickness 
        {
            get
            {
                if (waterNode != null)
                    return waterNode.Thickness;
                else if (Weirdo != null)
                    return Weirdo.Thickness;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.Thickness = value;
                else if (Weirdo != null)
                    Weirdo.Thickness = value;
                else
                    throw new Exception("Cannot set thickness. No water model found");
            }
        }

        /// <summary>Gets the depth mid points (mm).</summary>
        [Units("mm")]
        public double[] DepthMidPoints { get { return Soil.ToMidPoints(Thickness); } }

        /// <summary>Gets the depths (mm) of each layer.</summary>
        [Units("mm")]
        [Description("Depth")]
        public string[] Depth
        {
            get
            {
                return Soil.ToDepthStrings(Thickness);
            }
        }

        /// <summary>Bulk density at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [XmlIgnore]
        public double[] BD
        {
            get
            {
                if (waterNode != null)
                    return waterNode.BD;
                else if (Weirdo != null)
                    return Weirdo.BD;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.BD = value;
                else if (Weirdo != null)
                    Weirdo.BD = value;
                else
                    throw new Exception("Cannot set BD. No water model found");
            }
        }

        /// <summary>Soil water at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        public double[] InitialWaterVolumetric
        {
            get
            {
                var sample = Apsim.Child(this, typeof(Sample)) as Sample;
                return sample?.SW;
            }
        }

        /// <summary>Gets or sets the soil water for each layer (mm)</summary>
        [Units("mm")]
        [XmlIgnore]
        public double[] Water
        {
            get
            {
                return SoilWater.SWmm;
            }
        }

        /// <summary>Return AirDry at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        public double[] AirDry
        {
            get
            {
                if (waterNode != null)
                    return waterNode.AirDry;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.AirDry = value;
            }
        }


        /// <summary>Return lower limit at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [XmlIgnore]
        public double[] LL15
        {
            get
            {
                if (waterNode != null)
                    return waterNode.LL15;
                else if (Weirdo != null)
                    return Weirdo.LL15;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.LL15 = value;
                else if (Weirdo != null)
                    Weirdo.LL15 = value;
                else
                    throw new Exception("Cannot set LL15. No water model found");
            }
        }

        /// <summary>Return lower limit limit at standard thickness. Units: mm</summary>
        [Units("mm/mm")]
        public double[] LL15mm
        {
            get
            {
                return MathUtilities.Multiply(LL15, Thickness);
            }
        }

        /// <summary>Return drained upper limit at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [XmlIgnore]
        public double[] DUL
        {
            get
            {
                if (waterNode != null)
                    return waterNode.DUL;
                else if (Weirdo != null)
                    return Weirdo.DUL;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.DUL = value;
                else if (Weirdo != null)
                    Weirdo.DUL = value;
                else
                    throw new Exception("Cannot set DUL. No water model found");
            }
        }

        /// <summary>Return drained upper limit at standard thickness. Units: mm</summary>
        [Units("mm/mm")]
        public double[] DULmm
        {
            get
            {
                return MathUtilities.Multiply(DUL, Thickness);
            }
        }

        /// <summary>Return saturation at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [XmlIgnore]
        public double[] SAT
        {
            get
            {
                if (waterNode != null)
                    return waterNode.SAT;
                else if (Weirdo != null)
                    return Weirdo.SAT;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.SAT = value;
                else if (Weirdo != null)
                    Weirdo.SAT = value;
                else
                    throw new Exception("Cannot set SAT. No water model found");
            }
        }

        /// <summary>Return saturation at standard thickness. Units: mm</summary>
        [Units("mm/mm")]
        public double[] SATmm
        {
            get
            {
                return MathUtilities.Multiply(SAT, Thickness);
            }
        }

        /// <summary>KS at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [XmlIgnore]
        public double[] KS
        {
            get
            {
                if (waterNode != null)
                    return waterNode.KS;
                else if (Weirdo != null)
                    return Weirdo.Ksat;
                else
                    return null;
            }
            set
            {
                if (waterNode != null)
                    waterNode.KS = value;
                else if (Weirdo != null)
                    Weirdo.Ksat = value;
                else
                    throw new Exception("Cannot set KS. No water model found");
            }
        }

        /// <summary>SWCON at standard thickness. Units: 0-1</summary>
        [Units("0-1")]
        internal double[] SWCON 
        { 
            get 
            {
                if (SoilWater == null || !(SoilWater is SoilWater)) return null;
                return (SoilWater as SoilWater).SWCON;
            }
        }

        /// <summary>
        /// KLAT at standard thickness. Units: 0-1
        /// </summary>
        [Units("0-1")]
        internal double[] KLAT
            {
            get
                {
                if (SoilWater == null) return null;
                return (SoilWater as SoilWater).KLAT;
            }
        }



        /// <summary>Return the plant available water CAPACITY at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        public double[] PAWC
        {
            get
            {
                return CalcPAWC(Thickness,
                                LL15,
                                DUL,
                                null);
            }
        }

        /// <summary>Gets unavailable water at standard thickness. Units:mm</summary>
        [Description("Unavailable LL15")]
        [Units("mm")]
        [Display(Format = "N0", ShowTotal = true)]
        public double[] Unavailablemm
        {
            get
            {
                return MathUtilities.Multiply(LL15, Thickness);
            }
        }

        /// <summary>Gets available water at standard thickness (SW-LL15). Units:mm</summary>
        [Description("Available SW-LL15")]
        [Units("mm")]
        [Display(Format = "N0", ShowTotal = true)]
        public double[] PAWmmInitial
        {
            get
            {
                return MathUtilities.Multiply(CalcPAWC(Thickness,
                                                      LL15,
                                                      InitialWaterVolumetric,
                                                      null),
                                             Thickness);
            }
        }

        /// <summary>
        /// Gets the maximum plant available water CAPACITY at standard thickness (DUL-LL15). Units: mm
        /// </summary>
        [Description("Max. available\r\nPAWC DUL-LL15")]
        [Units("mm")]
        [Display(Format = "N0", ShowTotal = true)]
        public double[] PAWCmm
        {
            get
            {
                return MathUtilities.Multiply(PAWC, Thickness);
            }
        }

        /// <summary>Gets the drainable water at standard thickness (SAT-DUL). Units: mm</summary>
        [Description("Drainable\r\nPAWC SAT-DUL")]
        [Units("mm")]
        [Display(Format = "N0", ShowTotal = true)]
        public double[] Drainablemm
        {
            get
            {
                return MathUtilities.Multiply(MathUtilities.Subtract(SAT, DUL), Thickness);
            }
        }

        /// <summary>Plant available water at standard thickness. Units:mm/mm</summary>
        [Units("mm/mm")]
        public double[] PAW
        {
            get
            {
                return CalcPAWC(Thickness,
                                LL15,
                                SoilWater.SW,
                                null);
            }
        }

        /// <summary>Plant available water at standard thickness. Units:mm</summary>
        [Units("mm")]
        public double[] PAWmm
        {
            get
            {
                return MathUtilities.Multiply(PAW, Thickness);
            }
        }

        /// <summary>Plant available water at standard thickness. Units:mm/mm</summary>
        [Units("mm/mm")]
        public double[] PAWInitial
        {
            get
            {
                return CalcPAWC(Thickness,
                                LL15,
                                InitialWaterVolumetric,
                                null);
            }
        }

        /// <summary>Return the plant available water CAPACITY at water node thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        public double[] PAWCAtWaterThickness
        {
            get
            {
                return CalcPAWC(waterNode.Thickness,
                                waterNode.LL15,
                                waterNode.DUL,
                                null);
            }
        }

        /// <summary>Return the plant available water CAPACITY at water node thickenss. Units: mm</summary>
        [Units("mm")]
        public double[] PAWCmmAtWaterThickness
        {
            get
            {
                return MathUtilities.Multiply(PAWCAtWaterThickness, waterNode.Thickness);
            }
        }

        #endregion

        #region Crops

        /// <summary>Return a list of soil-crop parameterisations.</summary>
        public List<SoilCrop> Crops
        {
            get
            {
                return waterNode?.Children.Cast<SoilCrop>().ToList();
            }
        }

        /// <summary>Return a specific crop to caller. Will throw if crop doesn't exist.</summary>
        /// <param name="cropName">Name of the crop.</param>
        public SoilCrop Crop(string cropName) 
        {
            cropName = cropName + "Soil";
            var foundCrop = Crops?.Find(crop => crop.Name.Equals(cropName, StringComparison.InvariantCultureIgnoreCase));
            if (foundCrop == null)
                throw new Exception("Cannot find a soil-crop parameterisation for " + cropName);
            return foundCrop;
        }

        #endregion

        #region Soil organic matter

        /// <summary>FBiom. Units: 0-1</summary>
        [Units("0-1")]
        public double[] FBiom { get { return SoilOrganicMatter.FBiom; } }

        /// <summary>FInert. Units: 0-1</summary>
        [Units("0-1")]
        public double[] FInert { get { return SoilOrganicMatter.FInert; } }

        /// <summary>Initial Root Wt</summary>
        [Units("kg/ha")]
        public double[] InitialRootWt { get { return SoilOrganicMatter.RootWt; } }

        /// <summary>Initial soil CN ratio</summary>
        [Units("kg/ha")]
        public double[] SoilCN { get { return SoilOrganicMatter.SoilCN; } }

        /// <summary>Initial Root Wt</summary>
        [Units("kg/ha")]
        public double[] InitialSoilCNR { get { return MathUtilities.Divide(Initial.OC, Initial.ON); } }

        #endregion

        /// <summary>Gets the temperature of each layer</summary>
        public double[] Temperature { get { return temperatureModel.Value; } }

        #region Utility
        /// <summary>Convert an array of thickness (mm) to depth strings (cm)</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns></returns>
        static public string[] ToDepthStrings(double[] Thickness)
        {
            if (Thickness == null)
                return null;
            string[] Strings = new string[Thickness.Length];
            double DepthSoFar = 0;
            for (int i = 0; i != Thickness.Length; i++)
            {
                if (Thickness[i] == MathUtilities.MissingValue)
                    Strings[i] = "";
                else
                {
                    double ThisThickness = Thickness[i] / 10; // to cm
                    double TopOfLayer = DepthSoFar;
                    double BottomOfLayer = DepthSoFar + ThisThickness;
                    Strings[i] = TopOfLayer.ToString() + "-" + BottomOfLayer.ToString();
                    DepthSoFar = BottomOfLayer;
                }
            }
            return Strings;
        }
        /// <summary>
        /// Convert an array of depth strings (cm) to thickness (mm) e.g.
        /// "0-10", "10-30"
        /// To
        /// 100, 200
        /// </summary>
        /// <param name="DepthStrings">The depth strings.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Invalid layer string:  + DepthStrings[i] +
        ///                                   . String must be of the form: 10-30</exception>
        static public double[] ToThickness(string[] DepthStrings)
        {
            double[] Thickness = new double[DepthStrings.Length];
            for (int i = 0; i != DepthStrings.Length; i++)
            {
                if (DepthStrings[i] == "")
                    Thickness[i] = MathUtilities.MissingValue;
                else
                {
                    int PosDash = DepthStrings[i].IndexOf('-');
                    if (PosDash == -1)
                        throw new Exception("Invalid layer string: " + DepthStrings[i] +
                                  ". String must be of the form: 10-30");
                    double TopOfLayer;
                    double BottomOfLayer;

                    if (!Double.TryParse(DepthStrings[i].Substring(0, PosDash), out TopOfLayer))
                        throw new Exception("Invalid string for layer top: '" + DepthStrings[i].Substring(0, PosDash) + "'");
                    if (!Double.TryParse(DepthStrings[i].Substring(PosDash + 1), out BottomOfLayer))
                        throw new Exception("Invalid string for layer bottom: '" + DepthStrings[i].Substring(PosDash + 1) + "'");
                    Thickness[i] = (BottomOfLayer - TopOfLayer) * 10;
                }
            }
            return Thickness;
        }
        /// <summary>To the mid points.</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns></returns>
        static public double[] ToMidPoints(double[] Thickness)
        {
            //-------------------------------------------------------------------------
            // Return cumulative thickness midpoints for each layer - mm
            //-------------------------------------------------------------------------
            double[] CumThickness = ToCumThickness(Thickness);
            double[] MidPoints = new double[CumThickness.Length];
            for (int Layer = 0; Layer != CumThickness.Length; Layer++)
            {
                if (Layer == 0)
                    MidPoints[Layer] = CumThickness[Layer] / 2.0;
                else
                    MidPoints[Layer] = (CumThickness[Layer] + CumThickness[Layer - 1]) / 2.0;
            }
            return MidPoints;
        }
        /// <summary>To the cum thickness.</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <returns></returns>
        static public double[] ToCumThickness(double[] Thickness)
        {
            // ------------------------------------------------
            // Return cumulative thickness for each layer - mm
            // ------------------------------------------------
            double[] CumThickness = new double[Thickness.Length];
            if (Thickness.Length > 0)
            {
                CumThickness[0] = Thickness[0];
                for (int Layer = 1; Layer != Thickness.Length; Layer++)
                    CumThickness[Layer] = Thickness[Layer] + CumThickness[Layer - 1];
            }
            return CumThickness;
        }

        /// <summary>Layers the index.</summary>
        /// <param name="depth">The depth.</param>
        /// <param name="thickness">Layer thicknesses</param>
        public static int LayerIndexOfDepth(double depth, double[] thickness)
        {
            double CumDepth = 0;
            for (int i = 0; i < thickness.Length; i++)
            {
                CumDepth = CumDepth + thickness[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }

        /// <summary>Returns the proportion that 'depth' is through the layer.</summary>
        /// <param name="layerIndex">The layer index</param>
        /// <param name="depth">The depth</param>
        /// <param name="thickness">Layer thicknesses</param>
        public static double ProportionThroughLayer(int layerIndex, double depth, double[] thickness)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            for (int i = 0; i <= layerIndex; i++)
                depth_to_layer_bottom += thickness[i];

            double depth_to_layer_top = depth_to_layer_bottom - thickness[layerIndex];
            double depth_to_root = Math.Min(depth_to_layer_bottom, depth);
            double depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / thickness[layerIndex];
        }

        /// <summary>
        /// Plant available water for the specified crop. Will throw if crop not found. Units: mm/mm
        /// </summary>
        /// <param name="Thickness">The thickness.</param>
        /// <param name="LL">The ll.</param>
        /// <param name="DUL">The dul.</param>
        /// <param name="XF">The xf.</param>
        /// <returns></returns>
        public static double[] CalcPAWC(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            double[] PAWC = new double[Thickness.Length];
            if (LL == null || DUL == null)
                return PAWC;
            if (Thickness.Length != DUL.Length || Thickness.Length != LL.Length)
                throw new ApsimXException(null, "Number of soil layers in SoilWater is different to number of layers in SoilWater.Crop");

            for (int layer = 0; layer != Thickness.Length; layer++)
                if (DUL[layer] == MathUtilities.MissingValue ||
                    LL[layer] == MathUtilities.MissingValue)
                    PAWC[layer] = 0;
                else
                    PAWC[layer] = Math.Max(DUL[layer] - LL[layer], 0.0);

            bool ZeroXFFound = false;
            for (int layer = 0; layer != Thickness.Length; layer++)
                if (ZeroXFFound || XF != null && XF[layer] == 0)
                {
                    ZeroXFFound = true;
                    PAWC[layer] = 0;
                }
            return PAWC;
        }

        /// <summary>
        /// Calculate conversion factor from kg/ha to ppm (mg/kg)
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public double[] kgha2ppm(double[] values)
        {
            double[] ppm = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                ppm[i] = values[i] * MathUtilities.Divide(100.0, BD[i] * Thickness[i], 0.0);
            return ppm;
        }
        /// <summary>
        /// Calculate conversion factor from ppm to kg/ha
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public double[] ppm2kgha(double[] values)
        {
            double[] kgha = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                kgha[i] = values[i] * MathUtilities.Divide(BD[i] * Thickness[i], 100, 0.0);
            return kgha;
        }
        #endregion

        #region Checking
        /// <summary>
        /// Checks validity of soil water parameters
        /// This is a port of the soilwat2_check_profile routine. Returns a blank string if
        /// no errors were found.
        /// </summary>
        /// <param name="IgnoreStartingWaterN">if set to <c>true</c> [ignore starting water n].</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot find OC values in soil</exception>
        public string Check(bool IgnoreStartingWaterN)
        {
            const double min_sw = 0.0;
            const double specific_bd = 2.65; // (g/cc)
            string Msg = "";

            // Check the summer / winter dates.
            if (SoilWater is SoilWater)
            {
                DateTime Temp;
                if (!DateTime.TryParse((SoilWater as SoilWater).SummerDate, out Temp))
                    Msg += "Invalid summer date of: " + (SoilWater as SoilWater).SummerDate + "\r\n";
                if (!DateTime.TryParse((SoilWater as SoilWater).WinterDate, out Temp))
                    Msg += "Invalid winter date of: " + (SoilWater as SoilWater).WinterDate + "\r\n";
            }

            foreach (var soilCrop in Crops)
            {
                if (soilCrop != null)
                {
                    double[] LL = soilCrop.LL;
                    double[] KL = soilCrop.KL;
                    double[] XF = soilCrop.XF;

                    if (!MathUtilities.ValuesInArray(LL) ||
                        !MathUtilities.ValuesInArray(KL) ||
                        !MathUtilities.ValuesInArray(XF))
                        Msg += "Values for LL, KL or XF are missing for crop " + soilCrop.Name + "\r\n";

                    else
                    {
                        for (int layer = 0; layer != waterNode.Thickness.Length; layer++)
                        {
                            int RealLayerNumber = layer + 1;

                            if (KL[layer] == MathUtilities.MissingValue)
                                Msg += soilCrop.Name + " KL value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.GreaterThan(KL[layer], 1, 3))
                                Msg += soilCrop.Name + " KL value of " + KL[layer].ToString("f3")
                                         + " in layer " + RealLayerNumber.ToString() + " is greater than 1"
                                         + "\r\n";

                            if (XF[layer] == MathUtilities.MissingValue)
                                Msg += soilCrop.Name + " XF value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.GreaterThan(XF[layer], 1, 3))
                                Msg += soilCrop.Name + " XF value of " + XF[layer].ToString("f3")
                                         + " in layer " + RealLayerNumber.ToString() + " is greater than 1"
                                         + "\r\n";

                            if (LL[layer] == MathUtilities.MissingValue)
                                Msg += soilCrop.Name + " LL value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.LessThan(LL[layer], waterNode.AirDry[layer], 3))
                                Msg += soilCrop.Name + " LL of " + LL[layer].ToString("f3")
                                             + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + waterNode.AirDry[layer].ToString("f3")
                                           + "\r\n";

                            else if (MathUtilities.GreaterThan(LL[layer], waterNode.DUL[layer], 3))
                                Msg += soilCrop.Name + " LL of " + LL[layer].ToString("f3")
                                             + " in layer " + RealLayerNumber.ToString() + " is above drained upper limit of " + waterNode.DUL[layer].ToString("f3")
                                           + "\r\n";
                        }
                    }
                }
            }

            // Check other profile variables.
            for (int layer = 0; layer != waterNode.Thickness.Length; layer++)
            {
                double max_sw = MathUtilities.Round(1.0 - waterNode.BD[layer] / specific_bd, 3);
                int RealLayerNumber = layer + 1;

                if (waterNode.AirDry[layer] == MathUtilities.MissingValue)
                    Msg += " Air dry value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(waterNode.AirDry[layer], min_sw, 3))
                    Msg += " Air dry lower limit of " + waterNode.AirDry[layer].ToString("f3")
                                       + " in layer " + RealLayerNumber.ToString() + " is below acceptable value of " + min_sw.ToString("f3")
                               + "\r\n";

                if (waterNode.LL15[layer] == MathUtilities.MissingValue)
                    Msg += "15 bar lower limit value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(waterNode.LL15[layer], waterNode.AirDry[layer], 3))
                    Msg += "15 bar lower limit of " + waterNode.LL15[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + waterNode.AirDry[layer].ToString("f3")
                               + "\r\n";

                if (waterNode.DUL[layer] == MathUtilities.MissingValue)
                    Msg += "Drained upper limit value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(waterNode.DUL[layer], waterNode.LL15[layer], 3))
                    Msg += "Drained upper limit of " + waterNode.DUL[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is at or below lower limit of " + waterNode.LL15[layer].ToString("f3")
                               + "\r\n";

                if (waterNode.SAT[layer] == MathUtilities.MissingValue)
                    Msg += "Saturation value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(waterNode.SAT[layer], waterNode.DUL[layer], 3))
                    Msg += "Saturation of " + waterNode.SAT[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is at or below drained upper limit of " + waterNode.DUL[layer].ToString("f3")
                               + "\r\n";

                else if (MathUtilities.GreaterThan(waterNode.SAT[layer], max_sw, 3))
                {
                    double max_bd = (1.0 - waterNode.SAT[layer]) * specific_bd;
                    Msg += "Saturation of " + waterNode.SAT[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is above acceptable value of  " + max_sw.ToString("f3")
                               + ". You must adjust bulk density to below " + max_bd.ToString("f3")
                               + " OR saturation to below " + max_sw.ToString("f3")
                               + "\r\n";
                }

                if (waterNode.BD[layer] == MathUtilities.MissingValue)
                    Msg += "BD value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.GreaterThan(waterNode.BD[layer], 2.65, 3))
                    Msg += "BD value of " + waterNode.BD[layer].ToString("f3")
                                 + " in layer " + RealLayerNumber.ToString() + " is greater than the theoretical maximum of 2.65"
                               + "\r\n";
            }

            if (Initial.OC.Length == 0)
                throw new Exception("Cannot find OC values in soil");

            for (int layer = 0; layer != waterNode.Thickness.Length; layer++)
            {
                int RealLayerNumber = layer + 1;
                if (Initial.OC[layer] == MathUtilities.MissingValue)
                    Msg += "OC value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(Initial.OC[layer], 0.01, 3))
                    Msg += "OC value of " + Initial.OC[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is less than 0.01"
                                  + "\r\n";

                if (Initial.PH[layer] == MathUtilities.MissingValue)
                    Msg += "PH value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(Initial.PH[layer], 3.5, 3))
                    Msg += "PH value of " + Initial.PH[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is less than 3.5"
                                  + "\r\n";
                else if (MathUtilities.GreaterThan(Initial.PH[layer], 11, 3))
                    Msg += "PH value of " + Initial.PH[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is greater than 11"
                                  + "\r\n";
            }

            if (!IgnoreStartingWaterN)
            {
                if (!MathUtilities.ValuesInArray(InitialWaterVolumetric))
                    Msg += "No starting soil water values found.\r\n";
                else
                    for (int layer = 0; layer != waterNode.Thickness.Length; layer++)
                    {
                        int RealLayerNumber = layer + 1;

                        if (InitialWaterVolumetric[layer] == MathUtilities.MissingValue)
                            Msg += "Soil water value missing"
                                        + " in layer " + RealLayerNumber.ToString() + "\r\n";

                        else if (MathUtilities.GreaterThan(InitialWaterVolumetric[layer], waterNode.SAT[layer], 3))
                            Msg += "Soil water of " + InitialWaterVolumetric[layer].ToString("f3")
                                            + " in layer " + RealLayerNumber.ToString() + " is above saturation of " + waterNode.SAT[layer].ToString("f3")
                                            + "\r\n";

                        else if (MathUtilities.LessThan(InitialWaterVolumetric[layer], waterNode.AirDry[layer], 3))
                            Msg += "Soil water of " + InitialWaterVolumetric[layer].ToString("f3")
                                            + " in layer " + RealLayerNumber.ToString() + " is below air-dry value of " + waterNode.AirDry[layer].ToString("f3")
                                            + "\r\n";
                    }

                if (!MathUtilities.ValuesInArray(Initial.NO3))
                    Msg += "No starting NO3 values found.\r\n";
                if (!MathUtilities.ValuesInArray(Initial.NH4))
                    Msg += "No starting NH4 values found.\r\n";


            }


            return Msg;
        }

        #endregion

    }
}