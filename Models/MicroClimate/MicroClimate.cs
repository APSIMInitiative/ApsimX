using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Climate;
using Models.Core;
using Models.Interfaces;
using Models.Zones;

namespace Models
{

    /// <summary>
    /// The module MICROMET, described here, has been developed to allow the calculation of
    /// potential transpiration for multiple competing canopies that can be either layered or intermingled.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class MicroClimate : Model, IScopeDependency
    {
        private IScope scope;

        /// <summary>Scope supplied by APSIM.core.</summary>
        public void SetScope(IScope scope) => this.scope = scope;

        /// <summary>The clock</summary>
        [Link]
        private IClock clock = null;

        /// <summary>The weather</summary>
        [Link]
        private IWeather weather = null;

        /// <summary>The soil water model</summary>
        [Link]
        private ISoilWater soilWater = null;

        [Link]
        private ICalculateEo eoCalculator = null;

        /// <summary>The sun set angle (degrees)</summary>
        private const double sunSetAngle = 0.0;

        /// <summary>The sun angle for net positive radiation (degrees)</summary>
        private const double sunAngleNetPositiveRadiation = 15;

        /// <summary>List of uptakes</summary>
        private List<MicroClimateZone> microClimatesZones;

        /// <summary>This is the length of time within the day during which evaporation will take place</summary>
        private double dayLengthEvap;

        /// <summary>This is the length of time within the day during which the sun is above the horizon</summary>
        private double dayLengthLight;

        /// <summary>Gets or sets the a_interception.</summary>
        [Description("Multiplier on rainfall to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm/mm")]
        public double a_interception { get; set; }

        /// <summary>Gets or sets the b_interception.</summary>
        [Description("Power on rainfall to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 5.0)]
        [Units("-")]
        public double b_interception { get; set; }

        /// <summary>Gets or sets the c_interception.</summary>
        [Description("Multiplier on LAI to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        public double c_interception { get; set; }

        /// <summary>Gets or sets the d_interception.</summary>
        [Description("Constant value to add to calculate interception losses")]
        [Bounds(Lower = 0.0, Upper = 20.0)]
        [Units("mm")]
        public double d_interception { get; set; }

        /// <summary>Fraction of solar radiation reaching the soil surface that results in soil heating</summary>
        [Description("Fraction of solar radiation reaching the soil surface that results in soil heating")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("MJ/MJ")]
        public double SoilHeatFluxFraction { get; set; }

        /// <summary>The minimum height difference between canopies for a new layer to be created (m).</summary>
        [Description("The minimum height difference between canopies for a new layer to be created (m)")]
        [Units("m")]
        public double MinimumHeightDiffForNewLayer { get; set; } = 0.0;

        /// <summary>Height of the tallest canopy.</summary>
        [Units("mm")]
        public double CanopyHeight => microClimatesZones.Max(m =>
                                      {
                                          if (m.Canopies.Count == 0 )
                                              return 0;
                                          else
                                              return m.Canopies.Max(c => c.Canopy.Height);
                                      });

        /// <summary>The fraction of intercepted rainfall that evaporates at night</summary>
        [Description("The fraction of intercepted rainfall that evaporates at night")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double NightInterceptionFraction { get; set; }

        /// <summary>Height of the weather instruments</summary>
        [Description("Height of the weather instruments")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m")]
        public double ReferenceHeight { get; set; }

        /// <summary>Shortwave radiation reaching the surface (ie above the residue layer) (MJ/m2)</summary>
        [Description("Shortwave radiation reaching the surface (ie above the residue layer) (MJ/m2)")]
        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("MJ/m2")]
        public double[] SurfaceRS
        {
            get
            {
                double[] values = new double[microClimatesZones.Count];
                for (int i = 0; i < microClimatesZones.Count; i++)
                    values[i] = microClimatesZones[i].SurfaceRs;

                return values;
            }
        }

        /// <summary>Gets the amount of precipitation intercepted by the canopy (mm).</summary>
        [Description("Total intercepted precipitation")]
        [Units("mm")]
        public double PrecipitationInterception
        {
            get { return microClimatesZones[0].PrecipitationInterception; }
        }

        /// <summary>Gets the amount of radiation intercepted by the canopy (MJ/m2).</summary>
        [Description("Total intercepted radiation")]
        [Units("MJ/m^2")]
        public double RadiationInterception
        {
            get { return microClimatesZones == null ? 0 : microClimatesZones[0].RadiationInterception; }
        }

        /// <summary>Gets the amount of radiation intercepted by the green elements of canopy (MJ/m2).</summary>
        [Units("MJ/m^2")]
        public double RadiationInterceptionOnGreen
        {
            get { return microClimatesZones == null ? 0 : microClimatesZones[0].RadiationInterceptionOnGreen; }
        }

        /// <summary>Gets the total Penman-Monteith potential evapotranspiration (MJ/m2).</summary>
        [Description("Total Penman-Monteith potential evapotranspiration")]
        [Units("MJ/m^2")]
        public double PetTotal
        {
            get { return PetRadiationTerm + PetAerodynamicTerm; }
        }

        /// <summary>Gets the radiation term of for the Penman-Monteith PET (mm).</summary>
        [Description("Radiation term of for the Penman-Monteith PET")]
        [Units("mm")]
        public double PetRadiationTerm
        {
            get { return microClimatesZones[0].petr; }
        }

        /// <summary>Gets the aerodynamic term of for the Penman-Monteith PET (mm).</summary>
        [Description("Aerodynamic term of for the Penman-Monteith PET")]
        [Units("mm")]
        public double PetAerodynamicTerm
        {
            get { return microClimatesZones[0].peta; }
        }

        /// <summary>Gets the fraction of the daytime in which the leaves are dry (0-1).</summary>
        [Description("Fraction of the daytime in which the leaves are dry")]
        [Units("-")]
        public double DryLeafTimeFraction
        {
            get { return microClimatesZones[0].DryLeafFraction; }
        }

        /// <summary>Gets the total net radiation, long and short waves (MJ/m2).</summary>
        [Description("Net radiation, long and short waves")]
        [Units("MJ/m^2")]
        public double NetRadiation
        {
            get { return NetShortWaveRadiation + NetLongWaveRadiation; }
        }

        /// <summary>Gets the net short wave radiation (MJ/m2).</summary>
        [Description("Net short wave radiation")]
        [Units("MJ/m^2")]
        public double NetShortWaveRadiation
        {
            get { return weather.Radn * (1.0 - microClimatesZones[0].Albedo); }
        }

        /// <summary>Gets the net long wave radiation (MJ/m2).</summary>
        [Description("Net long wave radiation")]
        [Units("MJ/m^2")]
        public double NetLongWaveRadiation
        {
            get { return microClimatesZones[0].NetLongWaveRadiation; }
        }

        /// <summary>Gets the flux of heat into the soil (MJ/m2).</summary>
        [Description("Soil heat flux")]
        [Units("MJ/m^2")]
        public double SoilHeatFlux
        {
            get { return microClimatesZones[0].SoilHeatFlux; }
        }

        /// <summary>Gets the total plant cover (0-1).</summary>
        [Description("Total canopy cover")]
        [Units("-")]
        public double CanopyCover
        {
            get { return microClimatesZones[0].CanopyCover; }
        }

        /// <summary>The number of canopy layers.</summary>
        [Description("Number of canopy layers")]
        public int NumLayers
        {
            get { return microClimatesZones[0].DeltaZ.Length; }
        }

        /// <summary>Called when simulation starts.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            if (ReferenceHeight < 1 || ReferenceHeight > 10)
                throw new Exception($"Error in microclimate: reference height must be between 1 and 10. Actual value is {ReferenceHeight}");
            microClimatesZones = new List<MicroClimateZone>();
            foreach (Zone newZone in this.Parent.FindAllDescendants<Zone>())
                microClimatesZones.Add(new MicroClimateZone(clock, newZone, scope, MinimumHeightDiffForNewLayer));
            if (microClimatesZones.Count == 0)
                microClimatesZones.Add(new MicroClimateZone(clock, this.Parent as Zone, scope, MinimumHeightDiffForNewLayer));
        }

        /// <summary>Called when the canopy energy balance needs to be calculated.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoEnergyArbitration")]
        private void DoEnergyArbitration(object sender, EventArgs e)
        {
            microClimatesZones.ForEach(zone => zone.DailyInitialise(weather));

            dayLengthLight = MathUtilities.DayLength(clock.Today.DayOfYear, sunSetAngle, weather.Latitude);
            dayLengthEvap = MathUtilities.DayLength(clock.Today.DayOfYear, sunAngleNetPositiveRadiation, weather.Latitude);
            // VOS - a temporary kludge to get this running for high latitudes. MicroMet is due for a clean up soon so reconsider then.
            dayLengthEvap = Math.Max(dayLengthEvap, (dayLengthLight * 2.0 / 3.0));

            string canopyType = "BroadAcre";
            foreach (MicroClimateZone ZoneMC in microClimatesZones)
            {
                ZoneMC.IncomingRs = weather.Radn;
                ZoneMC.DoCanopyCompartments();
                if ((ZoneMC.Zone.CanopyType != "BroadAcre")&&(ZoneMC.Zone.CanopyType != null))
                {
                    canopyType = ZoneMC.Zone.CanopyType;
                }
            }

            if (canopyType == "BroadAcre")
            {
                foreach (MicroClimateZone ZoneMC in microClimatesZones)
                {
                    CalculateLayeredShortWaveRadiation(ZoneMC, weather.Radn);
                }
            }
            else
            {
                MicroClimateZone tallest = new MicroClimateZone(new RectangularZone((microClimatesZones[0].Zone as RectangularZone).Length, 0));
                MicroClimateZone shortest = new MicroClimateZone(new RectangularZone((microClimatesZones[0].Zone as RectangularZone).Length, 0));
                if (microClimatesZones[0].DeltaZ.Sum() > microClimatesZones[1].DeltaZ.Sum())
                {
                    tallest = microClimatesZones[0];
                    shortest = microClimatesZones[1];
                }
                else
                {
                    tallest = microClimatesZones[1];
                    shortest = microClimatesZones[0];
                }

                if (canopyType == "TreeRow")
                    DoTreeRowCropShortWaveRadiation(ref tallest, ref shortest);
                if (canopyType == "CropRow")
                    DoStripCropShortWaveRadiation(ref tallest, ref shortest);
                if (canopyType == "VineRow")
                    DoVineStripShortWaveRadiation(ref tallest, ref shortest);
            }

            // Light distribution is now complete so calculate remaining micromet equations
            foreach (var zoneMC in microClimatesZones)
            {
                zoneMC.CalculateEnergyTerms(soilWater.Salb);
                zoneMC.CalculateLongWaveRadiation(dayLengthLight, dayLengthEvap);
                zoneMC.CalculateSoilHeatRadiation(SoilHeatFluxFraction);
                zoneMC.CalculateGc(dayLengthEvap);
                zoneMC.CalculateGa(ReferenceHeight);
                zoneMC.CalculateInterception(a_interception, b_interception, c_interception, d_interception);
                zoneMC.CalculatePM(dayLengthEvap, NightInterceptionFraction);
                zoneMC.CalculateOmega();
                zoneMC.SetCanopyEnergyTerms();
                zoneMC.SoilWater.Eo = eoCalculator.Calculate(zoneMC);
                //zoneMC.SoilWater.CoverTotal = 1-MathUtilities.Divide((zoneMC.SurfaceRs),zoneMC.IncomingRs,0);
            }
        }

        /// <summary>
        /// This model is for tree crops where there is no vertical overlap of the shortest and tallest canopy but the tallest canopy can overlap the shortest horzontally
        /// </summary>
        /// <param name="treeZone"></param>
        /// <param name="alleyZone"></param>
        private void DoTreeRowCropShortWaveRadiation(ref MicroClimateZone treeZone, ref MicroClimateZone alleyZone)
        {
            if (DateUtilities.DatesAreEqual("02/01/2008",clock.Today))
            {

            }

            double TreeCanopyHeight = treeZone.DeltaZ.Sum();
            double TreeZoneWidth = (treeZone.Zone as Zones.RectangularZone).Width;
            double AlleyZoneWidth = (alleyZone.Zone as Zones.RectangularZone).Width;
            double SimulationWidth = TreeZoneWidth + AlleyZoneWidth;
            double TreeZoneLength = (treeZone.Zone as Zones.RectangularZone).Length;
            double AlleyZoneLength = (alleyZone.Zone as Zones.RectangularZone).Length;

            if (TreeZoneLength != AlleyZoneLength)
                throw new Exception("tree zone radiation interception requires zone and alley lengths to be the same.");

            double TreeZoneArea = (treeZone.Zone as Zones.RectangularZone).Area * 10000;
            double AlleyZoneArea = (alleyZone.Zone as Zones.RectangularZone).Area * 10000;
            double SimulatoinArea = TreeZoneArea + AlleyZoneArea;
            treeZone.IncomingRs = TreeZoneArea * weather.Radn; //Overwrite base value with area adjusted value
            alleyZone.IncomingRs = AlleyZoneArea * weather.Radn; //Overwrite base value with area adjusted value

            if (treeZone.DeltaZ.Sum() > 0 || alleyZone.DeltaZ.Sum() > 0)               // Don't perform calculations if both layers are empty
            {


                double TreeCanopyDepth = 0;
                double TreeCanopyWidth = 0;
                foreach (MicroClimateCanopy c in treeZone.Canopies)
                    if (c.Canopy.Depth < c.Canopy.Height)
                    {
                        if (TreeCanopyDepth > 0.0)
                            throw new Exception("Can't have two tree canopies");
                        else
                        {
                            TreeCanopyDepth = c.Canopy.Depth / 1000;
                            TreeCanopyWidth = c.Canopy.Width / 1000;
                        }
                    }
                if (AlleyZoneArea > 0)
                    TreeCanopyWidth = Math.Min(TreeCanopyWidth, TreeZoneWidth + AlleyZoneWidth);
                double TreeCanopyArea = TreeCanopyWidth * Math.Min(TreeCanopyWidth, TreeZoneLength); //Cap width of the canopy in the length dirrection to the inter row spacing (which sets Tree zone length) so the canopy widght can't exceed the inter row spacing
                double TreeCanopyBaseHeight = TreeCanopyHeight - TreeCanopyDepth;
                double AlleyCropCanopyHeight = alleyZone.DeltaZ.Sum();
                if ((AlleyCropCanopyHeight > TreeCanopyBaseHeight) & (treeZone.DeltaZ.Length > 1))
                    throw (new Exception("Height of the alley canopy must not exceed the base height of the tree canopy"));
                double TreeZoneCanopyOverlap = Math.Min(TreeCanopyWidth - TreeZoneWidth, AlleyZoneWidth);
                double TreeCanopyGap = AlleyZoneWidth - TreeZoneCanopyOverlap;
                double TreeCanopyLAI = treeZone.LAItotsum.Sum(); // LAI of trees in M2 of leaf area per m2 canopy area.  I.E. the area doesn't count the gaps between canopy rows
                double CropCanopyLAI = alleyZone.LAItotsum.Sum();
                double Kt = treeZone.layerKtot[treeZone.layerKtot.Length - 1];
                double Ka = 0;
                if (alleyZone.layerKtot.Length != 0)
                    Ka = alleyZone.layerKtot[0];                           // Extinction Coefficient of alley crop

                double FpassingTreeBB = 0;
                if (TreeCanopyGap > 0)
                    FpassingTreeBB = (Math.Sqrt(Math.Pow(TreeCanopyDepth, 2) + Math.Pow(TreeCanopyGap, 2)) - TreeCanopyDepth) / TreeCanopyGap;  //Fraction of radiation making it to the base of the tree canopy gap relative to the radiation incident to the area of the gap at the top of the tree canopy.  I.E the amount of radiation the side of the tree canopy intercepts .
                double FtransTree = Math.Exp(-Kt * TreeCanopyLAI);// Fraction of radiation transmitted through the tree canopy.  I.E the proportion of radiation not passing through the canopy.  Excludes the radiation passing through gaps
                double FpassingCropBB = (Math.Sqrt(Math.Pow(AlleyCropCanopyHeight, 2) + Math.Pow(TreeZoneWidth, 2)) - AlleyCropCanopyHeight) / TreeZoneWidth;  //Fraction of radiation making it to the base of the row relative to the amount incident to the area of the row at the top of the crop canopy.   I.E the amount of radiation the side of the crop canopy intercepts .
                double FTransCrop = Math.Exp(-Ka * CropCanopyLAI); // Fraction of radiation transmitted through the alley canopy
                double FradCrop = 1 - FTransCrop; //Fraction of radiation reaching the alley surface that is intercepted by the understory crop

                //First tree canopy intercepts radiation
                double IncidentRadn = Math.Max(SimulatoinArea, TreeCanopyArea) * weather.Radn;
                double TreeCanopyTopRadInt = IncidentRadn * TreeCanopyArea / SimulatoinArea * (1 - FtransTree); //Radiation that strikes the top of the tree canpy and is intercepted
                double TreeCanopySideRadInt = IncidentRadn * TreeCanopyGap / SimulationWidth * (1-FpassingTreeBB); //Radiation that is in the gap at the top of the canopy but is intercepted by the sides of the canopy in the gap.
                double TreeCanopyRadInt = TreeCanopyTopRadInt + TreeCanopySideRadInt; //Radiation (MJ) intercepted by the tree canopy
                for (int j = 0; j <= treeZone.Canopies.Count - 1; j++)
                {
                    treeZone.Canopies[j].Rs[1] = TreeCanopyRadInt * treeZone.Canopies[j].Canopy.CoverTotal;
                    treeZone.Canopies[j].AreaM2 = TreeCanopyArea;
                }
                double RadnRemaining = IncidentRadn - TreeCanopyRadInt;

                //The we partition transmitted radiation between the row and alley understory
                //Understory soil in row zone gets its share based on relative area
                double RowZoneUnderStorySoilRad = RadnRemaining * TreeZoneWidth/SimulationWidth*FpassingCropBB;
                treeZone.SurfaceRs = RowZoneUnderStorySoilRad;
                RadnRemaining -= RowZoneUnderStorySoilRad;

                //Then do top down radiation partitioning in the alley with the remaining radiation
                CalculateLayeredShortWaveRadiation(alleyZone, RadnRemaining);

            }
            else
            {
                treeZone.SurfaceRs = treeZone.IncomingRs;
                CalculateLayeredShortWaveRadiation(alleyZone, alleyZone.IncomingRs);
            }
        }

        /// <summary>
        /// This model is for strip crops where there is verticle overlap of the shortest and tallest crops canopy but no horizontal overlap
        /// </summary>
        /// <param name="tallest"></param>
        /// <param name="shortest"></param>
        private void DoStripCropShortWaveRadiation(ref MicroClimateZone tallest, ref MicroClimateZone shortest)
        {
            if (tallest.DeltaZ.Sum() > 0)  // Don't perform calculations if layers are empty
            {
                double Ht = tallest.DeltaZ.Sum();                // Height of tallest strip
                double Hs = shortest.DeltaZ.Sum();               // Height of shortest strip
                double Wt = (tallest.Zone as Zones.RectangularZone).Width;    // Width of tallest strip
                double Ws = (shortest.Zone as Zones.RectangularZone).Width;   // Width of shortest strip
                double Ft = Wt / (Wt + Ws);                                   // Fraction of space in tallest strip
                double Fs = Ws / (Wt + Ws);                                   // Fraction of space in the shortest strip
                double LAIt = tallest.LAItotsum.Sum();           // LAI of tallest strip
                double LAIs = shortest.LAItotsum.Sum();          // LAI of shortest strip
                if (LAIs > 0)
                { }
                double Kt = 0;                                                // Extinction Coefficient of the tallest strip
                if (tallest.Canopies.Count > 0)                                 // If it exists...
                    Kt = tallest.Canopies[0].Ktot;
                double Ks = 0;                                                // Extinction Coefficient of the shortest strip
                if (shortest.Canopies.Count > 0)                                // If it exists...
                    Ks = shortest.Canopies[0].Ktot;
                double Httop = Ht - Hs;                                       // Height of the top layer in tallest strip (ie distance from top of shortest to top of tallest)
                double LAIttop = Httop / Ht * LAIt;                           // LAI of the top layer of the tallest strip (ie LAI in tallest strip above height of shortest strip)
                double LAItbot = LAIt - LAIttop;                              // LAI of the bottom layer of the tallest strip (ie LAI in tallest strip below height of the shortest strip)
                double LAIttophomo = Ft * LAIttop;                            // LAI of top layer of tallest strip if spread homogeneously across all of the space
                double Ftblack = (Math.Sqrt(Math.Pow(Httop, 2) + Math.Pow(Wt, 2)) - Httop) / Wt;  // View factor for top layer of tallest strip
                double Fsblack = (Math.Sqrt(Math.Pow(Httop, 2) + Math.Pow(Ws, 2)) - Httop) / Ws;  // View factor for top layer of shortest strip
                double Tt = Ft * (Ftblack * Math.Exp(-Kt * LAIttop)
                          + Ft * (1 - Ftblack) * Math.Exp(-Kt * LAIttophomo))
                          + Fs * Ft * (1 - Fsblack) * Math.Exp(-Kt * LAIttophomo);  //  Transmission of light to bottom of top layer in tallest strip
                double Ts = Fs * (Fsblack + Fs * (1 - Fsblack) * Math.Exp(-Kt * LAIttophomo))
                          + Ft * Fs * ((1 - Ftblack) * Math.Exp(-Kt * LAIttophomo));           //  Transmission of light to bottom of top layer in shortest strip
                double Intttop = 1 - Tt - Ts;                                 // Interception by the top layer of the tallest strip (ie light intercepted in tallest strip above height of shortest strip)
                double Inttbot = (Tt * (1 - Math.Exp(-Kt * LAItbot)));        // Interception by the bottom layer of the tallest strip
                double Soilt = (Tt * (Math.Exp(-Kt * LAItbot)));              // Transmission to the soil below tallest strip
                double Ints = Ts * (1 - Math.Exp(-Ks * LAIs));                // Interception by the shortest strip
                double Soils = Ts * (Math.Exp(-Ks * LAIs));                   // Transmission to the soil below shortest strip
                double EnergyBalanceCheck = Intttop + Inttbot + Soilt + Ints + Soils;  // Sum of all light fractions (should equal 1)
                if (Math.Abs(1 - EnergyBalanceCheck) > 0.001)
                    throw (new Exception("Energy Balance not maintained in strip crop light interception model"));

                if (tallest.Canopies.Count > 0)
                    tallest.Canopies[0].Rs[0] = weather.Radn * (Intttop + Inttbot) / Ft;
                tallest.SurfaceRs = weather.Radn * Soilt / Ft;
                //CalculateLayeredShortWaveRadiation(tallest, weather.Radn * (Intttop + Inttbot) / Ft);

                if (shortest.Canopies.Count > 0 && shortest.Canopies[0].Rs != null)
                    if (shortest.Canopies[0].Rs.Length > 0)
                        shortest.Canopies[0].Rs[0] = weather.Radn * Ints / Fs;
                shortest.SurfaceRs = weather.Radn * Soils / Fs;
                //CalculateLayeredShortWaveRadiation(shortest, weather.Radn * Ints / Fs);
            }
            else
            {
                //tallest.Canopies[0].Rs[0] =0;
                tallest.SurfaceRs = weather.Radn;
                //shortest.Canopies[0].Rs[0] = 0;
                shortest.SurfaceRs = weather.Radn;
            }


        }

        /// <summary>
        /// This model is for strip crops where there is no verticle overlap of the shortest and tallest crops canopy but no horizontal overlap
        /// </summary>
        /// <param name="vine"></param>
        /// <param name="alley"></param>
        private void DoVineStripShortWaveRadiation(ref MicroClimateZone vine, ref MicroClimateZone alley)
        {
            if (vine.DeltaZ.Sum() > 0)  // Don't perform calculations if layers are empty
            {
                double Ht = vine.DeltaZ.Sum();                // Height of tree canopy

                double CDt = vine.Canopies[0].Canopy.Depth / 1000;         // Depth of tree canopy
                double CBHt = Ht - CDt;                                    // Base hight of the tree canopy
                double Ha = alley.DeltaZ.Sum();               // Height of alley canopy
                //if ((Ha > CBHt) & (vine.DeltaZ.Length > 1))
                //    throw (new Exception("Height of the alley canopy must not exceed the base height of the tree canopy"));

                double Wt = (vine.Zone as Zones.RectangularZone).Width;    // Width of tree zone
                double Wa = (alley.Zone as Zones.RectangularZone).Width;   // Width of alley zone
                double CWt = Math.Max(vine.Canopies[0].Canopy.Width,10) / 1000;         // Width of the tree canopy

                double WaOp = Wa + Wt - CWt;                               // Width of the open alley zone between tree canopies
                double Ft = CWt / (Wt + Wa);                              // Fraction of space in tree canopy
                double Fs = WaOp / (Wt + Wa);                             // Fraction of open space in the alley row


                double LAIt = vine.LAItotsum.Sum() * Wt / CWt;  // adjusting the LAI of tallest strip based on new width
                double LAIs = alley.LAItotsum.Sum() * Wa / WaOp; // adjusting the LAI of shortest strip based on new width

                double Kt = 0;                                               // Extinction Coefficient of the tallest strip
                if (vine.Canopies.Count > 0 & LAIt > 0)                                 // If it exists...
                    Kt = vine.Canopies[0].Ktot;
                double Ka = 0;                                                // Extinction Coefficient of the shortest strip
                if (alley.Canopies.Count > 0 & LAIs > 0)                                 // If it exists...
                    Ka = alley.Canopies[0].Ktot;

                double Httop = Ht - Ha;                                       // distance from top of shortest to top of tallest
                double LAIthomo = Ft * LAIt;                                // LAI of top layer of tallest strip if spread homogeneously across all of the space

                double fhomo = 1 - Math.Exp(-Kt * LAIthomo);
                double fcompr = (1 - Math.Exp(-Kt * LAIt)) * CWt / WaOp;

                double IPblackt = (Math.Sqrt(Math.Pow(CDt, 2) + Math.Pow(WaOp, 2)) - CDt) / WaOp;

                double IRblackt = (Math.Sqrt(Math.Pow(CDt, 2) + Math.Pow(CWt, 2)) - CDt) / CWt;  // View factor for top layer of tallest strip

                double SPt = IPblackt + (1 - IPblackt) * Math.Exp(-Kt * LAIthomo);
                double SRt = IRblackt * Math.Exp(-Kt * LAIt) +
                            (1 - IRblackt) * Math.Exp(-Kt * LAIthomo);   // Transmission of light to bottom of top layer in tallest strip
                double W = 0;
                if (vine.Canopies.Count > 0 & LAIt > 0 & Kt > 0)                                 // If it exists...
                    W = (SPt - SRt) / (1 - Math.Exp(-Kt * LAIt));

                double ftop = fhomo * (1 - W) + fcompr * W;              // light interception by the vine row

                //use different height and LAI distribution for calculating the light penetrate to the interrow
                //this is for accounting the effect of trunk, cordon, and post that shading on the interrow

                double IPblackb = (Math.Sqrt(Math.Pow(Httop, 2) + Math.Pow(WaOp, 2)) - Httop) / WaOp; //bottom part

                double IRblackb = (Math.Sqrt(Math.Pow(Httop, 2) + Math.Pow(CWt, 2)) - Httop) / CWt;  // View factor for top layer of tallest strip

                double SPb = IPblackb + (1 - IPblackb) * Math.Exp(-Kt * LAIthomo);
                double SRb = IRblackb * Math.Exp(-Kt * LAIt) +
                            (1 - IRblackb) * Math.Exp(-Kt * LAIthomo);  //  Transmission of light to bottom of top layer in tallest strip
                //double Wb = (SPt - SRt) / (1 - Math.Exp(-Kt * LAIt));

                //double fb = fhomo * (1 - W) + fcompr * W;  //light interception by the vine row

                double Soilt = SRb * Ft;                   // Transmission to the soil below tallest strip

                double Intttop = ftop;   // Interception by the top layer of the tallest strip (ie light intercepted in tallest strip above height of shortest strip)

                double Ints = SPb * (1 - Math.Exp(-Ka * LAIs)) * Fs;               // Interception by the shortest strip
                double Soils = SPb * (Math.Exp(-Ka * LAIs)) * Fs;                  // Transmission to the soil below shortest strip

                Ft = (Wt) / (Wt + Wa);  // Scaling back to zone ground area works
                Fs = (Wa) / (Wt + Wa);  // Scaling back to zone ground area works

                // Perform Top-Down Light Balance for tree zone
                // ==============================
                double Rint = 0;
                double Rin = weather.Radn * Intttop / Ft;
                for (int i = vine.numLayers - 1; i >= 0; i += -1)
                {
                    if (double.IsNaN(Rint))
                        throw new Exception("Bad Radiation Value in Light partitioning");
                    Rint = Rin;
                    for (int j = 0; j <= vine.Canopies.Count - 1; j++)
                        vine.Canopies[j].Rs[i] = Rint * MathUtilities.Divide(vine.Canopies[j].Ftot[i] * vine.Canopies[j].Ktot, vine.layerKtot[i], 0.0);
                    Rin -= Rint;
                }
                vine.SurfaceRs = weather.Radn * Soilt / Ft;

                // Perform Top-Down Light Balance for alley zone
                // ==============================
                Rint = 0;
                Rin = weather.Radn * Ints / Fs;
                for (int i = alley.numLayers - 1; i >= 0; i += -1)
                {
                    if (double.IsNaN(Rint))
                        throw new Exception("Bad Radiation Value in Light partitioning");
                    Rint = Rin;
                    for (int j = 0; j <= alley.Canopies.Count - 1; j++)
                        alley.Canopies[j].Rs[i] = Rint * MathUtilities.Divide(alley.Canopies[j].Ftot[i] * alley.Canopies[j].Ktot, alley.layerKtot[i], 0.0);
                    Rin -= Rint;
                }
                alley.SurfaceRs = weather.Radn * Soils / Fs;

            }
            else
            {
                //tallest.Canopies[0].Rs[0] =0;
                vine.SurfaceRs = weather.Radn;
                //shortest.Canopies[0].Rs[0] = 0;
                alley.SurfaceRs = weather.Radn;
            }

        }

        /// <summary>
        /// Calculates interception of short wave by canopy compartments
        /// </summary>
        private void CalculateLayeredShortWaveRadiation(MicroClimateZone ZoneMC, double Rin)
        {
            // Perform Top-Down Light Balance
            // ==============================
            double Rint = 0;
            for (int i = ZoneMC.numLayers - 1; i >= 0; i += -1)
            {
                if (double.IsNaN(Rint))
                    throw new Exception("Bad Radiation Value in Light partitioning");
                if (ZoneMC.LAItotsum[i] > 0)
                { }
                Rint = Rin * (1.0 - Math.Exp(-ZoneMC.layerKtot[i] * ZoneMC.LAItotsum[i]));
                for (int j = 0; j <= ZoneMC.Canopies.Count - 1; j++)
                    ZoneMC.Canopies[j].Rs[i] = Rint * MathUtilities.Divide(ZoneMC.Canopies[j].Ftot[i] * ZoneMC.Canopies[j].Ktot, ZoneMC.layerKtot[i], 0.0);
                Rin -= Rint;
            }
            ZoneMC.SurfaceRs = Rin;
        }
    }
}