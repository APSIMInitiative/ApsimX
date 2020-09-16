namespace Models.Soils
{
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
    public class Soil : Model
    {
        /// <summary>The child physical model.</summary>
        private IPhysical physical;

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

        /// <summary>Gets or sets the year of sampling.</summary>
        [Summary]
        [Description("Year of sampling")]
        public string YearOfSampling { get; set; }

        /// <summary>Gets or sets the data source.</summary>
        [Summary]
        [Description("Data source")]
        public string DataSource { get; set; }

        /// <summary>Gets or sets the comments.</summary>
        [Summary]
        [Description("Comments")]
        public string Comments { get; set; }

        /// <summary>Gets the soil water.</summary>
        [JsonIgnore] public ISoilWater SoilWater { get; private set; }

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
        public void FindChildren()
        {
            physical = this.FindChild<IPhysical>();

            SoilWater = this.FindInScope<ISoilWater>();
            if (physical == null)
                throw new Exception($"{Name}: Unable to find Physical");
        }

        #region Water

        /// <summary>Gets or sets the soil water for each layer (mm)</summary>
        [Units("mm")]
        [JsonIgnore]
        public double[] Water
        {
            get
            {
                return SoilWater.SWmm;
            }
        }

        /// <summary>Return drained upper limit at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [JsonIgnore]
        public double[] DUL { get { return physical.DUL; } }

        /// <summary>Return drained upper limit at standard thickness. Units: mm</summary>
        [Units("mm/mm")]
        public double[] DULmm
        {
            get
            {
                return MathUtilities.Multiply(DUL, physical.Thickness);
            }
        }

        /// <summary>Return saturation at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [JsonIgnore]
        public double[] SAT { get { return physical.SAT; } }
       
        /// <summary>Return saturation at standard thickness. Units: mm</summary>
        [Units("mm/mm")]
        public double[] SATmm
        {
            get
            {
                return MathUtilities.Multiply(SAT, physical.Thickness);
            }
        }

        /// <summary>KS at standard thickness. Units: mm/mm</summary>
        [Units("mm/mm")]
        [JsonIgnore]
        public double[] KS { get { return physical.KS; } }

        /// <summary>Plant available water CAPACITY (DUL-LL15).</summary>
        [Units("mm/mm")]
        public double[] PAWC
        {
            get
            {
                return CalcPAWC(physical.Thickness,
                                physical.LL15,
                                physical.DUL,
                                null);
            }
        }

        /// <summary>Plant available water CAPACITY (DUL-LL15).</summary>
        [Units("mm")]
        [Display(Format = "N0", ShowTotal = true)]
        public double[] PAWCmm
        {
            get
            {
                return MathUtilities.Multiply(PAWC, physical.Thickness);
            }
        }

        /// <summary>Plant available water (SW-LL15).</summary>
        [Units("mm/mm")]
        public double[] PAW
        {
            get
            {
                return CalcPAWC(physical.Thickness,
                                physical.LL15,
                                SoilWater.SW,
                                null);
            }
        }

        /// <summary>Plant available water (SW-LL15).</summary>
        [Units("mm")]
        public double[] PAWmm
        {
            get
            {
                return MathUtilities.Multiply(PAW, physical.Thickness);
            }
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

        #endregion

        /// <summary>Calculates the layer index for a specified depth.</summary>
        /// <param name="depth">The depth to search for.</param>
        /// <returns>The layer index or throws on error.</returns>
        public int LayerIndexOfDepth(double depth)
        {
            double CumDepth = 0;
            double[] thickness = physical.Thickness; // use local for efficiency reasons
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
        public double ProportionThroughLayer(int layerIndex, double depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            for (int i = 0; i <= layerIndex; i++)
                depth_to_layer_bottom += physical.Thickness[i];

            double depth_to_layer_top = depth_to_layer_bottom - physical.Thickness[layerIndex];
            double depth_to_root = Math.Min(depth_to_layer_bottom, depth);
            double depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / physical.Thickness[layerIndex];
        }

        /// <summary>Calculate conversion factor from kg/ha to ppm (mg/kg)</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public double[] kgha2ppm(double[] values)
        {
            if (values == null)
                return null;

            double[] ppm = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                ppm[i] = values[i] * (100.0 / (physical.BD[i] * physical.Thickness[i]));
            return ppm;
        }

        /// <summary>Calculate conversion factor from ppm to kg/ha</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public double[] ppm2kgha(double[] values)
        {
            if (values == null)
                return null;

            double[] kgha = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                kgha[i] = values[i] * (physical.BD[i] * physical.Thickness[i] / 100);
            return kgha;
        }
    }
}