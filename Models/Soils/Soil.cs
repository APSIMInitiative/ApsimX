namespace Models.Soils
{
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Models.Soils.Standardiser;
    using System;
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

        /// <summary>
        /// Calculate and return SW relative to the Water node thicknesses.
        /// Although there are no references in C# code to this property, it is
        /// used in the initial water chart in the GUI.
        /// </summary>
        [XmlIgnore]
        public double[] SWAtWaterThickness
        {
            get
            {
                InitialWater initialWater = Apsim.Child(this, typeof(InitialWater)) as InitialWater;

                if (initialWater != null)
                    return initialWater.SW(waterNode.Thickness, waterNode.LL15, waterNode.DUL, null);
                else
                {
                    foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    {
                        if (MathUtilities.ValuesInArray(Sample.SW))
                        {
                            if (waterNode != null)
                                return SWMapped(Sample.SWVolumetric, Sample.Thickness, waterNode.Thickness);
                            else
                                return Map(Sample.SWVolumetric, Sample.Thickness, Weirdo.Thickness);
                        }
                    }
                }
                return null;
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

        /// <summary>A list of crop names. Never returns null.</summary>
        [XmlIgnore]
        public string[] CropNames
        {
            get
            {
                if (waterNode != null)
                    return waterNode.CropNames;
                else
                    return new string[0];
            }
        }

        /// <summary>Return a specific crop to caller. Will throw if crop doesn't exist.</summary>
        /// <param name="CropName">Name of the crop.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Soil could not find crop:  + CropName</exception>
        public ISoilCrop Crop(string CropName) 
        {
            if (!CropName.EndsWith("Soil"))
                CropName += "Soil";

            if (waterNode != null)
            {
                ISoilCrop MeasuredCrop = waterNode.Crop(CropName);
                if (MeasuredCrop != null)
                {
                    if (MeasuredCrop is SoilCrop && 
                        CropName.Equals("Wheat", StringComparison.InvariantCultureIgnoreCase))
                        ModifyKLForSubSoilConstraints(MeasuredCrop as SoilCrop);
                    return MeasuredCrop;
                }

                throw new Exception("Soil could not find crop: " + CropName);
            }
            else return null;
        }

        /// <summary>Standard thicknesses</summary>
        readonly double[] StandardThickness = new double[] { 100, 100, 200, 200, 200, 200, 200 };
        /// <summary>Standard Kls</summary>
        readonly double[] StandardKL = new double[] { 0.06, 0.06, 0.04, 0.04, 0.04, 0.04, 0.02 };

        /// <summary>
        /// Modify the KL values for subsoil constraints.
        /// </summary>
        /// <remarks>
        /// From:
        /// Hochman, Z., Dang, Y.P., Schwenke, G.D., Dalgliesh, N.P., Routley, R., McDonald, M., 
        ///     Daniells, I.G., Manning, W., Poulton, P.L., 2007. 
        ///     Simulating the effects of saline and sodic subsoils on wheat crops 
        ///     growing on Vertosols. Australian Journal of Agricultural Research 58, 802–810. doi:10.1071/ar06365
        /// </remarks>
        /// <param name="measuredCrop"></param>
        private void ModifyKLForSubSoilConstraints(SoilCrop measuredCrop)
        {
            double[] cl = Cl;
            if (MathUtilities.ValuesInArray(cl))
            {
                measuredCrop.KL = Layers.MapConcentration(StandardKL, StandardThickness, Thickness, StandardKL.Last());
                for (int i = 0; i < Thickness.Length; i++)
                    measuredCrop.KL[i] *= Math.Min(1.0, 4.0 * Math.Exp(-0.005 * cl[i]));
            }
            else
            {
                double[] esp = ESP;
                if (MathUtilities.ValuesInArray(esp))
                {
                    measuredCrop.KL = Layers.MapConcentration(StandardKL, StandardThickness, Thickness, StandardKL.Last());
                    for (int i = 0; i < Thickness.Length; i++)
                        measuredCrop.KL[i] *= Math.Min(1.0, 10.0 * Math.Exp(-0.15 * esp[i]));
                }
                else
                {
                    double[] ec = EC;
                    if (MathUtilities.ValuesInArray(ec))
                    {
                        measuredCrop.KL = Layers.MapConcentration(StandardKL, StandardThickness, Thickness, StandardKL.Last());
                        for (int i = 0; i < Thickness.Length; i++)
                            measuredCrop.KL[i] *= Math.Min(1.0, 3.0 * Math.Exp(-1.3 * ec[i]));
                    }
                }
            }
        }

        /// <summary>Return the plant available water CAPACITY at water node thickness. Units: mm/mm</summary>
        /// <param name="CropName">Name of the crop.</param>
        /// <returns></returns>
        public double[] PAWCCropAtWaterThickness(string CropName)
        {
            return CalcPAWC(waterNode.Thickness,
                            LLMapped(CropName, waterNode.Thickness),
                            DULMapped(waterNode.Thickness),
                            XFMapped(CropName, waterNode.Thickness));
        }

        #endregion

        #region Soil organic matter
        /// <summary>Organic carbon. Units: %</summary>
        /// <value>The oc.</value>
        [Units("%")]
        public double[] OC
        {
            get
            {
                double[] SoilOC = SoilOrganicMatter.OCTotal;
                double[] SoilOCThickness = SoilOrganicMatter.Thickness;

                // Try and find a sample with OC in it.
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    if (MathUtilities.ValuesInArray(Sample.OC) && 
                        OverlaySampleOnTo(Sample.OCTotal, Sample.Thickness, ref SoilOC, ref SoilOCThickness))
                        break;
                if (SoilOC == null)
                    return null;
                return Map(SoilOC, SoilOCThickness, Thickness,
                           MapType.Concentration, SoilOC.Last());
            }
        }

        /// <summary>Organic nitrogen. Units: %</summary>
        /// <value>The on.</value>
        [Units("%")]
        public double[] ON
        {
            get
            {
                double[] SoilOC = SoilOrganicMatter.OCTotal;
                double[] SoilOCThickness = SoilOrganicMatter.Thickness;

                // Try and find a sample with OC in it.
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    if (MathUtilities.ValuesInArray(Sample.OC) &&
                        OverlaySampleOnTo(Sample.OCTotal, Sample.Thickness, ref SoilOC, ref SoilOCThickness))
                        break;
                if (SoilOC == null)
                    return null;
                double[] SoilON = MathUtilities.Divide(SoilOC, SoilOrganicMatter.SoilCN);

                return Map(SoilON, SoilOCThickness, Thickness,
                           MapType.Concentration, SoilON.Last());
            }
        }

        /// <summary>FBiom. Units: 0-1</summary>
        /// <value>The f biom.</value>
        [Units("0-1")]
        public double[] FBiom
        {
            get
            {
                if (SoilOrganicMatter.FBiom == null) return null;
                return Map(SoilOrganicMatter.FBiom, SoilOrganicMatter.Thickness, Thickness,
                           MapType.Concentration, LastValue(SoilOrganicMatter.FBiom));
            }
        }

        /// <summary>FInert. Units: 0-1</summary>
        /// <value>The f inert.</value>
        [Units("0-1")]
        public double[] FInert
        {
            get
            {
                if (SoilOrganicMatter.FInert == null) return null;
                return Map(SoilOrganicMatter.FInert, SoilOrganicMatter.Thickness, Thickness,
                           MapType.Concentration, LastValue(SoilOrganicMatter.FInert));
            }
        }

        /// <summary>Initial Root Wt</summary>
        /// <value>Initial Root Wt</value>
        [Units("kg/ha")]
        public double[] InitialRootWt
        {
            get
            {
                if (SoilOrganicMatter.RootWt == null) return null;
                return Map(SoilOrganicMatter.RootWt, SoilOrganicMatter.Thickness, Thickness,
                           MapType.Mass, LastValue(SoilOrganicMatter.RootWt));
            }
        }

        /// <summary>Initial Root Wt</summary>
        /// <value>Initial Root Wt</value>
        [Units("kg/ha")]
        public double[] InitialSoilCNR
        {
            get
            {
                return MathUtilities.Divide(OC, ON);
            }
        }

        #endregion

        #region Analysis
        /// <summary>Rocks. Units: %</summary>
        /// <value>The rocks.</value>
        [Units("0-1")]
        public double[] Rocks 
        { 
            get 
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                return Map(analysis.Rocks, analysis.Thickness, Thickness, MapType.Concentration);
            } 
        }

        /// <summary>Particle size sand</summary>
        /// <value>The particle size sand.</value>
        public double[] ParticleSizeSand
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                if (analysis.ParticleSizeSand == null) return null;
                return Map(analysis.ParticleSizeSand, analysis.Thickness, Thickness,
                           MapType.Concentration, LastValue(analysis.ParticleSizeSand));
            }
        }

        /// <summary>Particle size silt</summary>
        /// <value>The particle size silt.</value>
        public double[] ParticleSizeSilt
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                if (analysis.ParticleSizeSilt == null) return null;
                return Map(analysis.ParticleSizeSilt, analysis.Thickness, Thickness,
                           MapType.Concentration, LastValue(analysis.ParticleSizeSilt));
            }
        }

        /// <summary>Particle size silt</summary>
        /// <value>The particle size clay.</value>
        public double[] ParticleSizeClay
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                
                if (analysis.ParticleSizeClay == null) return null;
                return Map(analysis.ParticleSizeClay, analysis.Thickness, Thickness,
                           MapType.Concentration, LastValue(analysis.ParticleSizeClay));
            }
        }
        #endregion

        #region Sample

        /// <summary>Find the specified sample. Will throw if not found.</summary>
        /// <param name="SampleName">Name of the sample.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot find soil sample named:  + SampleName</exception>
        public Sample FindSample(string SampleName)
        {
            foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                if (Sample.Name.Equals(SampleName, StringComparison.CurrentCultureIgnoreCase))
                    return Sample;
            throw new Exception("Cannot find soil sample named: " + SampleName);
        }

        /// <summary>Nitrate (ppm).</summary>
        [Units("ppm")]
        public double[] InitialNO3N
        {
            get
            {
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                {
                    if (MathUtilities.ValuesInArray(Sample.NO3ppm))
                    {
                        double[] Values = Sample.NO3ppm;
                        double[] Thicknesses = Sample.Thickness;                
                        return Map(Values, Thicknesses, Thickness, MapType.Concentration, 1.0);
                    }
                }
                return null;
            }
        }

        /// <summary>Gets the temperature of each layer</summary>
        public double[] Temperature { get { return temperatureModel.Value; } }

        /// <summary>Ammonia (ppm).</summary>
        [Units("ppm")]
        public double[] InitialNH4N
        {
            get
            {
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                {
                    if (MathUtilities.ValuesInArray(Sample.NH4))
                    {
                        double[] Values = Sample.NH4ppm;
                        double[] Thicknesses = Sample.Thickness;                
                        return Map(Values, Thicknesses, Thickness, MapType.Concentration, 0.2);
                    }
                }
                return null;
            }
        }

        /// <summary>Cloride from either a sample or from Analysis. Units: mg/kg</summary>
        [Units("mg/kg")]
        public double[] Cl
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                
                double[] Values = analysis.CL;
                double[] Thicknesses = analysis.Thickness;

                // Try and find a sample with CL in it.
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    if (OverlaySampleOnTo(Sample.CL, Sample.Thickness, ref Values, ref Thicknesses))
                        break;
                if (Values != null)
                    return Map(Values, Thicknesses, Thickness,
                               MapType.Concentration, 0.0);
                return Values;
            }
        }

        /// <summary>ESP from either a sample or from Analysis. Units: %</summary>
        [Units("%")]
        public double[] ESP
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                
                double[] Values = analysis.ESP;
                double[] Thicknesses = analysis.Thickness;

                // Try and find a sample with ESP in it.
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    if (OverlaySampleOnTo(Sample.ESP, Sample.Thickness, ref Values, ref Thicknesses))
                        break;
                if (Values != null)
                    return Map(Values, Thicknesses, Thickness,
                               MapType.Concentration, LastValue(Values));
                return Values;
            }
        }

        /// <summary>EC from either a sample or from Analysis. Units: 1:5 dS/m</summary>
        public double[] EC
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                
                double[] Values = analysis.EC;
                double[] Thicknesses = analysis.Thickness;

                // Try and find a sample with EC in it.
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    if (OverlaySampleOnTo(Sample.EC, Sample.Thickness, ref Values, ref Thicknesses))
                        break;
                if (Values != null)
                    return Map(Values, Thicknesses, Thickness,
                               MapType.Concentration, LastValue(Values));
                return null;
            }
        }

        /// <summary>PH from either a sample or from Analysis. Units: 1:5 Water</summary>
        public double[] PH
        {
            get
            {
                Analysis analysis = Apsim.Child(this, typeof(Analysis)) as Analysis;
                
                double[] Values = analysis.PHWater;
                double[] Thicknesses = analysis.Thickness;

                // Try and find a sample with PH in it.
                foreach (Sample Sample in Apsim.Children(this, typeof(Sample)))
                    if (MathUtilities.ValuesInArray(Sample.PH) && 
                        OverlaySampleOnTo(Sample.PHWater, Sample.Thickness, ref Values, ref Thicknesses))
                        break;
                if (Values != null)
                    return Map(Values, Thicknesses, Thickness,
                               MapType.Concentration, LastValue(Values));
                return null;
            }
        }

        #endregion

        #region Phosphorus
        /// <summary>LabileP at standard thickness</summary>
        /// <value>The labile p.</value>
        public double[] LabileP
        {
            get
            {
                Phosphorus phosphorus = Apsim.Child(this, typeof(Phosphorus)) as Phosphorus;
                if (phosphorus == null)
                    return null;
                return Map(phosphorus.LabileP, phosphorus.Thickness, Thickness, MapType.Concentration);
            }
        }

        /// <summary>Sorption at standard thickness</summary>
        /// <value>The sorption.</value>
        public double[] Sorption
        {
            get
            {
                Phosphorus phosphorus = Apsim.Child(this, typeof(Phosphorus)) as Phosphorus;
                
                if (phosphorus == null)
                    return null;
                return Map(phosphorus.Sorption, phosphorus.Thickness, Thickness, MapType.Concentration);
            }
        }

        /// <summary>BandedP at standard thickness</summary>
        /// <value>The banded p.</value>
        public double[] BandedP
        {
            get
            {
                Phosphorus phosphorus = Apsim.Child(this, typeof(Phosphorus)) as Phosphorus;
                
                if (phosphorus == null)
                    return null;
                return Map(phosphorus.BandedP, phosphorus.Thickness, Thickness, MapType.Concentration);
            }
        }

        /// <summary>RockP at standard thickness</summary>
        /// <value>The rock p.</value>
        public double[] RockP
        {
            get
            {
                Phosphorus phosphorus = Apsim.Child(this, typeof(Phosphorus)) as Phosphorus;
                
                if (phosphorus == null)
                    return null;
                return Map(phosphorus.RockP, phosphorus.Thickness, Thickness, MapType.Concentration);
            }
        }


        #endregion

        #region Mapping
        /// <summary>Bulk density - mapped to the specified layer structure. Units: mm/mm</summary>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        internal double[] BDMapped(double[] ToThickness)
        {
            if(waterNode != null)
            return Map(waterNode.BD, waterNode.Thickness, ToThickness, MapType.Concentration, waterNode.BD.Last());
            else
                return Map(Weirdo.BD, Weirdo.Thickness, ToThickness, MapType.Concentration, Weirdo.BD.Last());
        }

        /// <summary>AirDry - mapped to the specified layer structure. Units: mm/mm</summary>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public double[] AirDryMapped(double[] ToThickness)
        {
            return Map(waterNode.AirDry, waterNode.Thickness, ToThickness, MapType.Concentration, waterNode.AirDry.Last());
        }

        /// <summary>Lower limit 15 bar - mapped to the specified layer structure. Units: mm/mm</summary>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public double[] LL15Mapped(double[] ToThickness)
        {
            return Map(waterNode.LL15, waterNode.Thickness, ToThickness, MapType.Concentration, waterNode.LL15.Last());
        }

        /// <summary>Drained upper limit - mapped to the specified layer structure. Units: mm/mm</summary>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        public double[] DULMapped(double[] ToThickness)
        {
            return Map(waterNode.DUL, waterNode.Thickness, ToThickness, MapType.Concentration, waterNode.DUL.Last());
        }

        /// <summary>SW - mapped to the specified layer structure. Units: mm/mm</summary>
        /// <param name="Values">The values.</param>
        /// <param name="Thicknesses">The thicknesses.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot find crop lower limit or LL15 in soil</exception>
        public double[] SWMapped(double[] Values, double[] Thicknesses, double[] ToThickness)
        {
            if (Thicknesses == ToThickness)
                return Values;

            // Get the last item in the sw array and its indx.
            double LastSW = LastValue(Values);
            double LastThickness = LastValue(Thicknesses);
            int LastIndex = Values.Length - 1;

            Array.Resize(ref Thicknesses, Thicknesses.Length + 3); // Increase array size by 3.
            Array.Resize(ref Values, Values.Length + 3);

            Thicknesses[LastIndex + 1] = LastThickness;
            Thicknesses[LastIndex + 2] = LastThickness;
            Thicknesses[LastIndex + 3] = 3000;

            Values[LastIndex + 1] = LastSW * 0.8;
            Values[LastIndex + 2] = LastSW * 0.4;
            Values[LastIndex + 3] = 0.0; // This will be constrained below to crop LL below.

            // Get the first crop ll or ll15.
            double[] LowerBound = null;
            if (waterNode.Crops.Count > 0)
                LowerBound = LLMapped(waterNode.Crops[0].Name, Thicknesses);

            if (LowerBound == null)
                LowerBound = LL15Mapped(Thicknesses);
            if (LowerBound == null)
                throw new Exception("Cannot find crop lower limit or LL15 in soil");

            // Make sure all SW values below LastIndex don't go below CLL.
            for (int i = LastIndex + 1; i < Thicknesses.Length; i++)
                if (i < Values.Length && i < LowerBound.Length)
                    Values[i] = Math.Max(Values[i], LowerBound[i]);

            return Map(Values, Thicknesses, ToThickness, MapType.Concentration);
        }
        /// <summary>
        /// The lower limit to water extraction for each layer
        /// </summary>
        /// <param name="CropName"></param>
        /// <returns></returns>
        public double[] LL(string CropName)
        {
            return LLMapped(CropName, Thickness);
        }
        /// <summary>Crop lower limit mapped. Units: mm/mm</summary>
        /// <param name="CropName">Name of the crop.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>
        internal double[] LLMapped(string CropName, double[] ToThickness)
        {
            SoilCrop SoilCrop = Crop(CropName) as SoilCrop;
            if (SoilCrop == null)
                return null;

            if (MathUtilities.AreEqual(waterNode.Thickness, ToThickness))
                return SoilCrop.LL;
            double[] Values = Map(SoilCrop.LL, waterNode.Thickness, ToThickness, MapType.Concentration, LastValue(SoilCrop.LL));
            if (Values == null) return null;
            double[] AirDry = AirDryMapped(ToThickness);
            double[] DUL = DULMapped(ToThickness);
            if (AirDry == null || DUL == null)
                return null;
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = Math.Max(Values[i], AirDry[i]);
                Values[i] = Math.Min(Values[i], DUL[i]);
            }
            return Values;
        }

        /// <summary>
        /// The extension resistance to crop root growth from the soil
        /// </summary>
        /// <param name="CropName"></param>
        /// <returns></returns>
        public double[] XF(string CropName)
        {
            return XFMapped(CropName, Thickness);
        }
        /// <summary>Crop XF mapped. Units: 0-1</summary>
        /// <param name="CropName">Name of the crop.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <returns></returns>

        internal double[] XFMapped(string CropName, double[] ToThickness)
        {
            if (Weirdo != null)
            {
                return Weirdo.MappedXF;
            }
            else
            {
                SoilCrop SoilCrop = Crop(CropName) as SoilCrop;
                if (MathUtilities.AreEqual(waterNode.Thickness, ToThickness))
                    return SoilCrop.XF;
                return Map(SoilCrop.XF, waterNode.Thickness, ToThickness, MapType.Concentration, LastValue(SoilCrop.XF));
            }
        }
        /// <summary>
        /// The potential water extraction rate constant for each layer
        /// </summary>
        /// <param name="CropName"></param>
        /// <returns></returns>
        public double[] KL(string CropName)
        {
            return KLMapped(CropName, Thickness);
        }
        internal double[] KLMapped (string CropName, double[] ToThickness)
        {
            SoilCrop SoilCrop = Crop(CropName) as SoilCrop;
            //if (CropName.Equals("Wheat", StringComparison.InvariantCultureIgnoreCase))
            //    ModifyKLForSubSoilConstraints(SoilCrop);
            if (MathUtilities.AreEqual(waterNode.Thickness, ToThickness))
                return SoilCrop.KL;
            return Map(SoilCrop.KL, waterNode.Thickness, ToThickness, MapType.Concentration, LastValue(SoilCrop.KL));
        }
        /// <summary>different methods for mapping soil variables </summary>
        public enum MapType
        {
            /// <summary>How heavy things are</summary>
            Mass,
            /// <summary>The concentration of things</summary>
            Concentration,
            /// <summary>Using bulk density</summary>
            UseBD
        }
        /// <summary>Map soil variables from one layer structure to another.</summary>
        /// <param name="FValues">The f values.</param>
        /// <param name="FThickness">The f thickness.</param>
        /// <param name="ToThickness">To thickness.</param>
        /// <param name="MapType">Type of the map.</param>
        /// <param name="DefaultValueForBelowProfile">The default value for below profile.</param>
        /// <returns></returns>
        public double[] Map(double[] FValues, double[] FThickness,
                             double[] ToThickness, MapType MapType = MapType.Concentration,
                             double DefaultValueForBelowProfile = double.NaN)
        {
            if (FValues == null || FThickness == null || FValues.Length != FThickness.Length)
                return null;

            if (MathUtilities.AreEqual(FThickness, ToThickness))
                return FValues;

            double[] FromThickness = (double[]) FThickness.Clone();
            double[] FromValues = (double[])FValues.Clone();

            if (FromValues == null)
                return null;

            // remove missing layers.
            for (int i = 0; i < FromValues.Length; i++)
            {
                if (double.IsNaN(FromValues[i]) || double.IsNaN(FromThickness[i]))
                {
                    FromValues[i] = double.NaN;
                    FromThickness[i] = double.NaN;
                }
            }
            FromValues = MathUtilities.RemoveMissingValuesFromBottom(FromValues);
            FromThickness = MathUtilities.RemoveMissingValuesFromBottom(FromThickness);

            if (MathUtilities.AreEqual(FromThickness, ToThickness))
                return FromValues;

            if (FromValues.Length != FromThickness.Length)
                return null;

            // Add the default value if it was specified.
            if (!double.IsNaN(DefaultValueForBelowProfile))
            {
                Array.Resize(ref FromThickness, FromThickness.Length + 1);
                Array.Resize(ref FromValues, FromValues.Length + 1);
                FromThickness[FromThickness.Length - 1] = 3000;  // to push to profile deep.
                FromValues[FromValues.Length - 1] = DefaultValueForBelowProfile;
            }

            // If necessary convert FromValues to a mass.
            if (MapType == Soil.MapType.Concentration)
                FromValues = MathUtilities.Multiply(FromValues, FromThickness);
            else if (MapType == Soil.MapType.UseBD)
            {
                double[] BD = waterNode.BD;
                for (int Layer = 0; Layer < FromValues.Length; Layer++)
                    FromValues[Layer] = FromValues[Layer] * BD[Layer] * FromThickness[Layer] / 100;
            }

            // Remapping is achieved by first constructing a map of
            // cumulative mass vs depth
            // The new values of mass per layer can be linearly
            // interpolated back from this shape taking into account
            // the rescaling of the profile.

            double[] CumDepth = new double[FromValues.Length + 1];
            double[] CumMass = new double[FromValues.Length + 1];
            CumDepth[0] = 0.0;
            CumMass[0] = 0.0;
            for (int Layer = 0; Layer < FromThickness.Length; Layer++)
            {
                CumDepth[Layer + 1] = CumDepth[Layer] + FromThickness[Layer];
                CumMass[Layer + 1] = CumMass[Layer] + FromValues[Layer];
            }

            //look up new mass from interpolation pairs
            double[] ToMass = new double[ToThickness.Length];
            for (int Layer = 1; Layer <= ToThickness.Length; Layer++)
            {
                double LayerBottom = MathUtilities.Sum(ToThickness, 0, Layer, 0.0);
                double LayerTop = LayerBottom - ToThickness[Layer - 1];
                bool DidInterpolate;
                double CumMassTop = MathUtilities.LinearInterpReal(LayerTop, CumDepth,
                    CumMass, out DidInterpolate);
                double CumMassBottom = MathUtilities.LinearInterpReal(LayerBottom, CumDepth,
                    CumMass, out DidInterpolate);
                ToMass[Layer - 1] = CumMassBottom - CumMassTop;
            }

            // If necessary convert FromValues back into their former units.
            if (MapType == Soil.MapType.Concentration)
                ToMass = MathUtilities.Divide(ToMass, ToThickness);
            else if (MapType == Soil.MapType.UseBD)
            {
                double[] BD = BDMapped(ToThickness);
                for (int Layer = 0; Layer < FromValues.Length; Layer++)
                    ToMass[Layer] = ToMass[Layer] * 100.0 / BD[Layer] / ToThickness[Layer];
            }

            for (int i = 0; i < ToMass.Length; i++)
                if (double.IsNaN(ToMass[i]))
                    ToMass[i] = 0.0;
            return ToMass;
        }


        // <param name="units">The units of the associated field or property</param>

        /// <summary>Overlay sample values onto soil values.</summary>
        /// <param name="SampleValues">The sample values.</param>
        /// <param name="SampleThickness">The sample thickness.</param>
        /// <param name="SoilValues">The soil values.</param>
        /// <param name="SoilThickness">The soil thickness.</param>
        /// <returns></returns>
        private static bool OverlaySampleOnTo(double[] SampleValues, double[] SampleThickness,
                                               ref double[] SoilValues, ref double[] SoilThickness)
        {
            if (MathUtilities.ValuesInArray(SampleValues))
            {
                double[] Values = (double[])SampleValues.Clone();
                double[] Thicknesses = (double[])SampleThickness.Clone();
                InFillValues(ref Values, ref Thicknesses, SoilValues, SoilThickness);
                SoilValues = Values;
                SoilThickness = Thicknesses;
                return true;
            }
            return false;
        }


        /// <summary>Takes values from SoilValues and puts them at the bottom of SampleValues.</summary>
        /// <param name="SampleValues">The sample values.</param>
        /// <param name="SampleThickness">The sample thickness.</param>
        /// <param name="SoilValues">The soil values.</param>
        /// <param name="SoilThickness">The soil thickness.</param>
        private static void InFillValues(ref double[] SampleValues, ref double[] SampleThickness,
                                         double[] SoilValues, double[] SoilThickness)
        {
            //-------------------------------------------------------------------------
            //  e.g. IF             SoilThickness  Values   SampleThickness	SampleValues
            //                           0-100		2         0-100				10
            //                         100-250	    3	     100-600			11
            //                         250-500		4		
            //                         500-750		5
            //                         750-900		6
            //						  900-1200		7
            //                        1200-1500		8
            //                        1500-1800		9
            //
            // will produce:		SampleThickness	        Values
            //						     0-100				  10
            //						   100-600				  11
            //						   600-750				   5
            //						   750-900				   6
            //						   900-1200				   7
            //						  1200-1500				   8
            //						  1500-1800				   9
            //
            //-------------------------------------------------------------------------
            if (SoilValues == null || SoilThickness == null) return;

            // remove missing layers.
            for (int i = 0; i < SampleValues.Length; i++)
            {
                if (double.IsNaN(SampleValues[i]) || double.IsNaN(SampleThickness[i]))
                {
                    SampleValues[i] = double.NaN;
                    SampleThickness[i] = double.NaN;
                }
            }
            SampleValues = MathUtilities.RemoveMissingValuesFromBottom(SampleValues);
            SampleThickness = MathUtilities.RemoveMissingValuesFromBottom(SampleThickness);

            double CumSampleDepth = MathUtilities.Sum(SampleThickness);

            //Work out if we need to create a dummy layer so that the sample depths line up 
            //with the soil depths
            double CumSoilDepth = 0.0;
            for (int SoilLayer = 0; SoilLayer < SoilThickness.Length; SoilLayer++)
            {
                CumSoilDepth += SoilThickness[SoilLayer];
                if (CumSoilDepth > CumSampleDepth)
                {
                    Array.Resize(ref SampleThickness, SampleThickness.Length + 1);
                    Array.Resize(ref SampleValues, SampleValues.Length + 1);
                    int i = SampleThickness.Length - 1;
                    SampleThickness[i] = CumSoilDepth - CumSampleDepth;
                    if (SoilValues[SoilLayer] == MathUtilities.MissingValue)
                        SampleValues[i] = 0.0;
                    else
                        SampleValues[i] = SoilValues[SoilLayer];
                    CumSampleDepth = CumSoilDepth;
                }
            }
        }

        #endregion

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

        /// <summary>Codes to meta data.</summary>
        /// <param name="Codes">The codes.</param>
        /// <returns></returns>
        static public string[] CodeToMetaData(string[] Codes)
        {
            string[] Metadata = new string[Codes.Length];
            for (int i = 0; i < Codes.Length; i++)
                if (Codes[i] == "FM")
                    Metadata[i] = "Field measured and checked for sensibility";
                else if (Codes[i] == "C_grav")
                    Metadata[i] = "Calculated from gravimetric moisture when profile wet but drained";
                else if (Codes[i] == "E")
                    Metadata[i] = "Estimated based on local knowledge";
                else if (Codes[i] == "U")
                    Metadata[i] = "Unknown source or quality of data";
                else if (Codes[i] == "LM")
                    Metadata[i] = "Laboratory measured";
                else if (Codes[i] == "V")
                    Metadata[i] = "Volumetric measurement";
                else if (Codes[i] == "M")
                    Metadata[i] = "Mass measured";
                else if (Codes[i] == "C_bd")
                    Metadata[i] = "Calculated from measured, estimated or calculated BD";
                else if (Codes[i] == "C_pt")
                    Metadata[i] = "Developed using a pedo-transfer function";
                else
                    Metadata[i] = Codes[i];
            return Metadata;
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

        /// <summary>Return the last value that isn't a missing value.</summary>
        /// <param name="Values">The values.</param>
        /// <returns></returns>
        private double LastValue(double[] Values)
        {
            if (Values == null) return double.NaN;
            for (int i = Values.Length - 1; i >= 0; i--)
                if (!double.IsNaN(Values[i]))
                    return Values[i];
            return double.NaN;
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

            foreach (string Crop in CropNames)
            {
                SoilCrop soilCrop = this.Crop(Crop) as SoilCrop;
                if (soilCrop != null)
                {
                    double[] LL = soilCrop.LL;
                    double[] KL = soilCrop.KL;
                    double[] XF = soilCrop.XF;

                    if (!MathUtilities.ValuesInArray(LL) ||
                        !MathUtilities.ValuesInArray(KL) ||
                        !MathUtilities.ValuesInArray(XF))
                        Msg += "Values for LL, KL or XF are missing for crop " + Crop + "\r\n";

                    else
                    {
                        for (int layer = 0; layer != waterNode.Thickness.Length; layer++)
                        {
                            int RealLayerNumber = layer + 1;

                            if (KL[layer] == MathUtilities.MissingValue)
                                Msg += Crop + " KL value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.GreaterThan(KL[layer], 1, 3))
                                Msg += Crop + " KL value of " + KL[layer].ToString("f3")
                                         + " in layer " + RealLayerNumber.ToString() + " is greater than 1"
                                         + "\r\n";

                            if (XF[layer] == MathUtilities.MissingValue)
                                Msg += Crop + " XF value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.GreaterThan(XF[layer], 1, 3))
                                Msg += Crop + " XF value of " + XF[layer].ToString("f3")
                                         + " in layer " + RealLayerNumber.ToString() + " is greater than 1"
                                         + "\r\n";

                            if (LL[layer] == MathUtilities.MissingValue)
                                Msg += Crop + " LL value missing"
                                         + " in layer " + RealLayerNumber.ToString() + "\r\n";

                            else if (MathUtilities.LessThan(LL[layer], waterNode.AirDry[layer], 3))
                                Msg += Crop + " LL of " + LL[layer].ToString("f3")
                                             + " in layer " + RealLayerNumber.ToString() + " is below air dry value of " + waterNode.AirDry[layer].ToString("f3")
                                           + "\r\n";

                            else if (MathUtilities.GreaterThan(LL[layer], waterNode.DUL[layer], 3))
                                Msg += Crop + " LL of " + LL[layer].ToString("f3")
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

            if (OC.Length == 0)
                throw new Exception("Cannot find OC values in soil");

            for (int layer = 0; layer != waterNode.Thickness.Length; layer++)
            {
                int RealLayerNumber = layer + 1;
                if (OC[layer] == MathUtilities.MissingValue)
                    Msg += "OC value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(OC[layer], 0.01, 3))
                    Msg += "OC value of " + OC[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is less than 0.01"
                                  + "\r\n";

                if (PH[layer] == MathUtilities.MissingValue)
                    Msg += "PH value missing"
                             + " in layer " + RealLayerNumber.ToString() + "\r\n";

                else if (MathUtilities.LessThan(PH[layer], 3.5, 3))
                    Msg += "PH value of " + PH[layer].ToString("f3")
                                  + " in layer " + RealLayerNumber.ToString() + " is less than 3.5"
                                  + "\r\n";
                else if (MathUtilities.GreaterThan(PH[layer], 11, 3))
                    Msg += "PH value of " + PH[layer].ToString("f3")
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

                if (!MathUtilities.ValuesInArray(InitialNO3N))
                    Msg += "No starting NO3 values found.\r\n";
                if (!MathUtilities.ValuesInArray(InitialNH4N))
                    Msg += "No starting NH4 values found.\r\n";


            }


            return Msg;
        }

        #endregion

    }
}
