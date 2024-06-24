using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Surface;
using Newtonsoft.Json;

namespace Models.PMF
{

    ///<summary> This is a temporary class that will be refactored so the generic biomass/arbutration functionality can be seperatured 
    ///from the root specific functionality which will then be extracted so root can be represented with the Organ class
    ///</summary>
    [Serializable]
    [Description("Root Class")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IOrgan))]
    public class RootNetwork : Model, IWaterNitrogenUptake
    {

        ///1. Links
        ///--------------------------------------------------------------------------------------------------

        /// <summary>The plant</summary>
        [Link]
        protected Plant parentPlant = null;

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Ancestor)]
        public Organ parentOrgan = null;

        /// <summary>The RootShape model</summary> 
        [Link(Type = LinkType.Child, ByName = false)]
        public IRootShape RootShape = null;

        /// <summary>Link to the KNO3 link</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d/ppm")]
        private IFunction kno3 = null;

        /// <summary>Link to the KNH4 link</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d/ppm")]
        private IFunction knh4 = null;

        /// <summary>Soil water factor for N Uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nUptakeSWFactor = null;

        /// <summary>Gets or sets the specific root length</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("m/g")]
        private IFunction specificRootLength = null;

        /// <summary>The nitrogen root calc switch</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction RootFrontCalcSwitch = null;

        /// <summary>The root front velocity</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("mm/d")]
        private IFunction rootFrontVelocity = null;

        /// <summary>Link to the KNO3 link</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        public IFunction RootDepthStressFactor = null;

        /// <summary>The maximum daily N uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("kg N/ha/d")]
        private IFunction maxDailyNUptake = null;

        /// <summary>The kl modifier</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        private IFunction klModifier = null;

        /// <summary>The Maximum Root Depth</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("mm")]
        private IFunction maximumRootDepth = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        private SoilCrop soilCrop;


        /// <summary>The kgha2gsm</summary>
        protected const double kgha2gsm = 0.1;

        /// <summary>Returns true if the KL modifier due to root damage is active or not.</summary>
        private bool IsKLModiferDueToDamageActive { get; set; } = false;

        /// <summary>Gets the KL modifier due to root damage (0-1).</summary>
        private double KLModiferDueToDamage(int layerIndex)
        {
            var threshold = 0.01;
            if (!IsKLModiferDueToDamageActive)
                return 1;
            else if (LengthDensity[layerIndex] < 0)
                return 0;
            else if (LengthDensity[layerIndex] >= threshold)
                return 1;
            else
                return (1 / threshold) * LengthDensity[layerIndex];
        }


        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------
        /// <summary>Constructor</summary>
        public RootNetwork()
        {
            Zones = new List<NetworkZoneState>();
            ZoneNamesToGrowRootsIn = new List<string>();
            ZoneRootDepths = new List<double>();
            ZoneInitialDM = new List<NutrientPoolFunctions>();
        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------
        /// <summary>Gets or sets the root length modifier due to root damage (0-1).</summary>
        [JsonIgnore]
        public double RootLengthDensityModifierDueToDamage { get; set; } = 1.0;

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return false; } }

        /// <summary>A list of other zone names to grow roots in</summary>
        public List<string> ZoneNamesToGrowRootsIn { get; set; }

        /// <summary>The root depths for each addition zone.</summary>
        [JsonIgnore]
        public List<double> ZoneRootDepths { get; set; }

        /// <summary>The live weights for each addition zone.</summary>
        [JsonIgnore]
        public List<NutrientPoolFunctions> ZoneInitialDM { get; set; }

        /*
        // <summary>Live Biomass in each soil layer</summary>
        [JsonIgnore]
        public List<OrganNutrientStates> LayerLive { get { return PlantZone.LayerLive; } }

        /// <summary>Dead Biomass in each soil layer</summary>
        [JsonIgnore]
        public List<OrganNutrientStates> LayerDead { get { return PlantZone.LayerDead; } }*/


        /// <summary>A list of all zones to grow roots in</summary>
        [JsonIgnore]
        public List<NetworkZoneState> Zones { get; set; }

        /// <summary>The zone where the plant is growing</summary>
        [JsonIgnore]
        public NetworkZoneState PlantZone { get; set; }

        /// <summary>Gets the root length density.</summary>
        [Units("mm/mm3")]
        [JsonIgnore]
        public double[] LengthDensity
        {
            get
            {
                if (PlantZone == null)    // Can be null in autodoc
                    return new double[0];
                double[] value;
                value = new double[PlantZone.Physical.Thickness.Length];
                double SRL = specificRootLength.Value();
                for (int i = 0; i < PlantZone.Physical.Thickness.Length; i++)
                    value[i] = PlantZone.LayerLive[i].Wt * RootLengthDensityModifierDueToDamage * SRL * 1000 / 1000000 / PlantZone.Physical.Thickness[i];
                return value;
            }
        }

        ///<Summary>The amount of N taken up after arbitration</Summary>
        [Units("g/m2")]
        [JsonIgnore]
        public double NTakenUp { get; set; }

        ///<Summary>The speed of root descent</Summary>
        [JsonIgnore]
        public double RootFrontVelocity { get; set; }

        ///<Summary>The deepest roots will get</Summary>
        [JsonIgnore]
        public double MaximumRootDepth { get; set; }

        /// <summary>Root depth.</summary>
        [JsonIgnore]
        [Units("mm")]
        public double Depth { 
            get { return PlantZone.Depth; }
            set { PlantZone.Depth = value; }
        }

        /// <summary>Root length.</summary>
        [JsonIgnore]
        public double Length { get { return PlantZone.RootLength; } }

        /// <summary>Root Area</summary>
        [JsonIgnore]
        public double Area { get { return PlantZone.RootArea; } }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("mm")]
        public double WaterUptake
        {
            get
            {
                double uptake = 0;
                foreach (NetworkZoneState zone in Zones)
                    uptake = uptake + MathUtilities.Sum(zone.WaterUptake);
                return -uptake;
            }
        }

        /// <summary>Gets or sets the N uptake.</summary>
        [Units("kg/ha")]
        public double NUptake
        {
            get
            {
                double uptake = 0;
                foreach (NetworkZoneState zone in Zones)
                    uptake += MathUtilities.Sum(zone.NitUptake);
                return uptake;
            }
        }

        /// <summary>Gets or sets the mid points of each layer</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] LayerMidPointDepth { get; private set; }

        /// <summary>Gets or sets relative water content for a soil layer (ie fraction between LL15 and DUL)</summary>
        [JsonIgnore]
        [Units("0-1")]
        public double[] RWC { get; private set; }

        /// <summary>Returns the Fraction of Available Soil Water for the root system (across zones and depths in zones)</summary>
        [Units("unitless")]
        [JsonIgnore]
        public double FASW
        {
            get
            {
                double fasw = 0;
                double TotalArea = 0;

                foreach (NetworkZoneState Z in Zones)
                {
                    Zone zone = this.FindInScope(Z.Name) as Zone;
                    var soilPhysical = Z.Soil.FindChild<IPhysical>();
                    var waterBalance = Z.Soil.FindChild<ISoilWater>();
                    var soilCrop = Z.Soil.FindDescendant<SoilCrop>(parentPlant.Name + "Soil");
                    double[] paw = APSIM.Shared.APSoil.APSoilUtilities.CalcPAWC(soilPhysical.Thickness, soilCrop.LL, waterBalance.SW, soilCrop.XF);
                    double[] pawmm = MathUtilities.Multiply(paw, soilPhysical.Thickness);
                    double[] pawc = APSIM.Shared.APSoil.APSoilUtilities.CalcPAWC(soilPhysical.Thickness, soilCrop.LL, soilPhysical.DUL, soilCrop.XF);
                    double[] pawcmm = MathUtilities.Multiply(pawc, soilPhysical.Thickness);
                    TotalArea += zone.Area;

                    fasw += MathUtilities.Sum(pawmm) / MathUtilities.Sum(pawcmm) * zone.Area;
                }
                fasw = fasw / TotalArea;
                return fasw;
            }
        }

        /// <summary>Gets a factor to account for root zone Water tension weighted for root mass.</summary>
        [Units("0-1")]
        [JsonIgnore]
        public double WaterTensionFactor
        {
            get
            {
                if (PlantZone == null)
                    return 0;

                double MeanWTF = 0;

                double liveWt = parentOrgan.Live.Wt;
                if (liveWt > 0)
                    foreach (NetworkZoneState Z in Zones)
                    {
                        var soilPhysical = Z.Soil.FindChild<IPhysical>();
                        var waterBalance = Z.Soil.FindChild<ISoilWater>();
                        double[] paw = waterBalance.PAW;
                        double[] pawc = soilPhysical.PAWC;
                        int i = 0;
                        foreach (OrganNutrientsState l in Z.LayerLive)
                        {
                            if (pawc[i] > 0)
                            {
                                MeanWTF += l.Wt / liveWt * MathUtilities.Bound(2 * paw[i] / pawc[i], 0, 1);
                            }
                            i += 1;
                        }
                    }

                return MeanWTF;
            }
        }

        /// <summary>Gets a factor to account for root zone Water tension weighted for root mass.</summary>
        [Units("0-1")]
        [JsonIgnore]
        public double PlantWaterPotentialFactor
        {
            get
            {
                if (PlantZone == null)
                    return 0;

                double MeanWTF = 0;

                double liveWt = parentOrgan.Live.Weight.Total;
                if (liveWt > 0)
                    foreach (NetworkZoneState Z in Zones)
                    {
                        var soilPhysical = Z.Soil.FindChild<IPhysical>();
                        var waterBalance = Z.Soil.FindChild<ISoilWater>();

                        double[] paw = waterBalance.PAW;
                        double[] pawc = soilPhysical.PAWC;
                        int i = 0;
                        foreach (OrganNutrientsState l in Z.LayerLive)
                        {
                            MeanWTF += l.Wt / liveWt * MathUtilities.Bound(paw[i] / pawc[i], 0, 1);
                            i += 1;
                        }
                    }
                return MeanWTF;
            }
        }

        /// <summary>Gets or sets the water supply.</summary>
        /// <param name="zone">The zone.</param>
        public double[] CalculateWaterSupply(ZoneWaterAndN zone)
        {
            NetworkZoneState myZone = Zones.Find(z => z.Name == zone.Zone.Name);
            if (myZone == null)
                return null;

            var currentLayer = SoilUtilities.LayerIndexOfDepth(PlantZone.Physical.Thickness, Depth);

            var soilCrop = myZone.Soil.FindDescendant<SoilCrop>(parentPlant.Name + "Soil");
            if (soilCrop == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {parentPlant.Name + "Soil"}");

            if (RootFrontCalcSwitch?.Value() >= 1.0)
            {
                double[] kl = soilCrop.KL;
                double[] ll = soilCrop.LL;

                double[] supply = new double[myZone.Physical.Thickness.Length];

                LayerMidPointDepth = myZone.Physical.DepthMidPoints;
                for (int layer = 0; layer <= currentLayer; layer++)
                {
                    double available = zone.Water[layer] - ll[layer] * myZone.Physical.Thickness[layer] * myZone.LLModifier[layer];

                    supply[layer] = Math.Max(0.0, kl[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) *
                        available * myZone.RootProportions[layer]);
                }

                return supply;
            }
            else
            {
                double[] kl = soilCrop.KL;
                double[] ll = soilCrop.LL;

                double[] supply = new double[myZone.Physical.Thickness.Length];
                LayerMidPointDepth = myZone.Physical.DepthMidPoints;
                for (int layer = 0; layer < myZone.Physical.Thickness.Length; layer++)
                {
                    if (layer <= SoilUtilities.LayerIndexOfDepth(myZone.Physical.Thickness, myZone.Depth))
                    {
                        double available = zone.Water[layer] - ll[layer] * myZone.Physical.Thickness[layer] * myZone.LLModifier[layer];

                        supply[layer] = Math.Max(0.0, kl[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) *
                        available * myZone.RootProportions[layer]);
                    }
                }
                return supply;
            }
        }

        /// <summary>Computes root total water supply.</summary>
        public double TotalExtractableWater()
        {

            double[] LL = soilCrop.LL;
            double[] KL = soilCrop.KL;
            double[] SWmm = PlantZone.WaterBalance.SWmm;
            double[] DZ = PlantZone.Physical.Thickness;

            double supply = 0;
            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= SoilUtilities.LayerIndexOfDepth(PlantZone.Physical.Thickness, Depth))
                {
                    double available = Math.Max(SWmm[layer] - LL[layer] * DZ[layer] * PlantZone.LLModifier[layer], 0);

                    supply += Math.Max(0.0, KL[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) *
                            available * PlantZone.RootProportions[layer]);
                }
            }
            return supply;
        }


        /// <summary>Plant Avaliable water supply used by sorghum.</summary>
        /// <summary>It adds an extra layer proportion calc to extractableWater calc.</summary>
        public double PlantAvailableWaterSupply()
        {
            double[] LL = soilCrop.LL;
            double[] KL = soilCrop.KL;
            double[] SWmm = PlantZone.WaterBalance.SWmm;
            double[] DZ = PlantZone.Physical.Thickness;
            double[] available = new double[PlantZone.Physical.Thickness.Length];
            double[] supply = new double[PlantZone.Physical.Thickness.Length];

            var currentLayer = SoilUtilities.LayerIndexOfDepth(PlantZone.Physical.Thickness, Depth);
            var layertop = MathUtilities.Sum(PlantZone.Physical.Thickness, 0, Math.Max(0, currentLayer - 1));
            var layerBottom = MathUtilities.Sum(PlantZone.Physical.Thickness, 0, currentLayer);
            var layerProportion = Math.Min(MathUtilities.Divide(Depth - layertop, layerBottom - layertop, 0.0), 1.0);

            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= currentLayer)
                {
                    available[layer] = Math.Max(0.0, SWmm[layer] - LL[layer] * DZ[layer] * PlantZone.LLModifier[layer]);
                }
            }
            available[currentLayer] *= layerProportion;

            double supplyTotal = 0;
            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= currentLayer)
                {
                    supply[layer] = Math.Max(0.0, available[layer] * KL[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) *
                        PlantZone.RootProportions[layer]);

                    supplyTotal += supply[layer];
                }
            }
            return supplyTotal;
        }

        ///6. Public methods
        /// --------------------------------------------------------------------------------------------------

        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
        public void DoWaterUptake(double[] Amount, string zoneName)
        {
            NetworkZoneState zone = Zones.Find(z => z.Name == zoneName);
            if (zone == null)
                throw new Exception("Cannot find a zone called " + zoneName);

            zone.WaterUptake = MathUtilities.Multiply_Value(Amount, -1.0);
            zone.WaterBalance.RemoveWater(Amount);
        }

        /// <summary>Does the Nitrogen uptake.</summary>
        /// <param name="zonesFromSoilArbitrator">List of zones from soil arbitrator</param>
        public void DoNitrogenUptake(List<ZoneWaterAndN> zonesFromSoilArbitrator)
        {
            foreach (ZoneWaterAndN thisZone in zonesFromSoilArbitrator)
            {
                NetworkZoneState zone = Zones.Find(z => z.Name == thisZone.Zone.Name);
                if (zone != null)
                {
                    zone.NO3.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(zone.NO3.kgha, thisZone.NO3N));
                    zone.NH4.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(zone.NH4.kgha, thisZone.NH4N));

                    zone.NitUptake = MathUtilities.Multiply_Value(MathUtilities.Add(thisZone.NO3N, thisZone.NH4N), -1);
                }
            }
        }

        /// <summary>Gets the nitrogen supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        /// <param name="NO3Supply">The returned NO3 supply</param>
        /// <param name="NH4Supply">The returned NH4 supply</param>
        public void CalculateNitrogenSupply(ZoneWaterAndN zone, ref double[] NO3Supply, ref double[] NH4Supply)
        {
            NetworkZoneState myZone = Zones.Find(z => z.Name == zone.Zone.Name);
            if (myZone != null)
            {
                if (RWC == null || RWC.Length != myZone.Physical.Thickness.Length)
                    RWC = new double[myZone.Physical.Thickness.Length];

                double NO3Uptake = 0;
                double NH4Uptake = 0;

                double[] thickness = myZone.Physical.Thickness;
                double[] water = myZone.WaterBalance.SWmm;
                double[] ll15mm = myZone.Physical.LL15mm;
                double[] dulmm = myZone.Physical.DULmm;
                double[] bd = myZone.Physical.BD;

                double accuDepth = 0;
                if (RootFrontCalcSwitch?.Value() >= 1.0)
                {
                    if (myZone.MassFlow == null || myZone.MassFlow.Length != myZone.Physical.Thickness.Length)
                        myZone.MassFlow = new double[myZone.Physical.Thickness.Length];
                    if (myZone.Diffusion == null || myZone.Diffusion.Length != myZone.Physical.Thickness.Length)
                        myZone.Diffusion = new double[myZone.Physical.Thickness.Length];

                    var currentLayer = SoilUtilities.LayerIndexOfDepth(myZone.Physical.Thickness, myZone.Depth);
                    for (int layer = 0; layer <= currentLayer; layer++)
                    {
                        var swdep = water[layer]; //mm
                        var flow = myZone.WaterUptake[layer];
                        var yest_swdep = swdep - flow;
                        //NO3N is in kg/ha - old sorghum used g/m^2
                        var no3conc = zone.NO3N[layer] * kgha2gsm / yest_swdep; //to equal old sorghum
                        var no3massFlow = no3conc * (-flow);
                        myZone.MassFlow[layer] = no3massFlow;

                        //diffusion
                        var swAvailFrac = RWC[layer] = (water[layer] - ll15mm[layer]) / (dulmm[layer] - ll15mm[layer]);
                        //old sorghum stores N03 in g/ms not kg/ha
                        var no3Diffusion = MathUtilities.Bound(swAvailFrac, 0.0, 1.0) * (zone.NO3N[layer] * kgha2gsm);
                        myZone.Diffusion[layer] = no3Diffusion * myZone.RootProportions[layer];

                        //NH4Supply[layer] = no3massFlow;
                        //onyl 2 fields passed in for returning data. 
                        //actual uptake needs to distinguish between massflow and diffusion
                        //sorghum calcs don't use nh4 - so using that temporarily
                    }
                }
                else
                {
                    double maxNUptake = maxDailyNUptake.Value();
                    for (int layer = 0; layer < thickness.Length; layer++)
                    {
                        accuDepth += thickness[layer];
                        if (myZone.LayerLive[layer].Wt > 0)
                        {
                            double factorRootDepth = Math.Max(0, Math.Min(1, 1 - (accuDepth - Depth) / thickness[layer]));
                            RWC[layer] = (water[layer] - ll15mm[layer]) / (dulmm[layer] - ll15mm[layer]);
                            RWC[layer] = Math.Max(0.0, Math.Min(RWC[layer], 1.0));
                            double SWAF = nUptakeSWFactor.Value(layer);

                            double kno3 = this.kno3.Value(layer);
                            double NO3ppm = zone.NO3N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                            NO3Supply[layer] = Math.Min(zone.NO3N[layer] * kno3 * NO3ppm * SWAF * factorRootDepth, (maxNUptake - NO3Uptake));
                            NO3Uptake += NO3Supply[layer];

                            double knh4 = this.knh4.Value(layer);
                            double NH4ppm = zone.NH4N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                            NH4Supply[layer] = Math.Min(zone.NH4N[layer] * knh4 * NH4ppm * SWAF * factorRootDepth, (maxNUptake - NH4Uptake));
                            NH4Uptake += NH4Supply[layer];
                        }
                    }
                }
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Cannot find a soil crop parameterisation for  + Name</exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Zones = new List<NetworkZoneState>();

            Soil soil = this.FindInScope<Soil>();
            if (soil == null)
                throw new Exception("Cannot find soil");
            PlantZone = new NetworkZoneState(parentPlant, soil);

            soilCrop = soil.FindDescendant<SoilCrop>(parentPlant.Name + "Soil");
            if (soilCrop == null)
                throw new Exception("Cannot find a soil crop parameterisation for " + parentPlant.Name);
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PostPhenology")]
        protected void OnPostPhenology(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                RootFrontVelocity = rootFrontVelocity.Value();
                MaximumRootDepth = maximumRootDepth.Value();
                foreach (NetworkZoneState z in Zones)
                {
                    z.CalculateRAw();
                    z.CalculateRelativeBiomassProportions();
                }
            }
        }

        /// <summary>
        /// Set the initial biomass
        /// </summary>
        public void InitailiseNetwork(OrganNutrientsState Initial)
        {
            Clear();
            RootFrontVelocity = rootFrontVelocity.Value();
            MaximumRootDepth = maximumRootDepth.Value();

            InitialiseZones();
            foreach (NetworkZoneState Z in Zones)
            {
                Z.LayerLive[0] = Initial;
            }
        }

        /// <summary>
        /// Method to take biomass pratitioned to roots and partition between zones and layers
        /// </summary>
        /// <param name="reAllocated"></param>
        /// <param name="reTranslocated"></param>
        /// <param name="allocated"></param>
        /// <param name="senesced"></param>
        /// <param name="detached"></param>
        /// <param name="liveRemoved"></param>
        /// <param name="deadRemoved"></param>
        public void PartitionBiomassThroughSoil(OrganNutrientsState reAllocated, OrganNutrientsState reTranslocated,
                                             OrganNutrientsState allocated, OrganNutrientsState senesced,
                                             OrganNutrientsState detached,
                                             OrganNutrientsState liveRemoved, OrganNutrientsState deadRemoved)
        {
            double TotalRAw = 0;
            foreach (NetworkZoneState Z in Zones)
                TotalRAw += Z.RAw.Sum();

            if (parentPlant.IsAlive)
            {
                double checkTotalWt = 0;
                double checkTotalN = 0;
                foreach (NetworkZoneState Z in Zones)
                {
                    FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[Z.LayerLive.Length];
                    for (int layer = 0; layer < Z.Physical.Thickness.Length; layer++)
                    {
                        Z.LayerLive[layer] = OrganNutrientsState.Subtract(Z.LayerLive[layer], OrganNutrientsState.Multiply(liveRemoved, Z.LayerLiveProportion[layer], parentOrgan.Cconc), parentOrgan.Cconc);
                        Z.LayerLive[layer] = OrganNutrientsState.Subtract(Z.LayerLive[layer], OrganNutrientsState.Multiply(reAllocated, Z.LayerLiveProportion[layer], parentOrgan.Cconc), parentOrgan.Cconc);
                        Z.LayerLive[layer] = OrganNutrientsState.Subtract(Z.LayerLive[layer], OrganNutrientsState.Multiply(reTranslocated, Z.LayerLiveProportion[layer], parentOrgan.Cconc), parentOrgan.Cconc);
                        Z.LayerLive[layer] = OrganNutrientsState.Subtract(Z.LayerLive[layer], OrganNutrientsState.Multiply(senesced, Z.LayerLiveProportion[layer], parentOrgan.Cconc), parentOrgan.Cconc);

                        double fracAlloc = MathUtilities.Divide(Z.RAw[layer], TotalRAw, 0);
                        Z.LayerLive[layer] = OrganNutrientsState.Add(Z.LayerLive[layer], OrganNutrientsState.Multiply(allocated, fracAlloc, parentOrgan.Cconc), parentOrgan.Cconc);

                        Z.LayerDead[layer] = OrganNutrientsState.Add(Z.LayerDead[layer], OrganNutrientsState.Multiply(senesced, Z.LayerDeadProportion[layer], parentOrgan.Cconc), parentOrgan.Cconc);
                        OrganNutrientsState detachedToday = OrganNutrientsState.Multiply(detached, Z.LayerDeadProportion[layer], parentOrgan.Cconc);
                        Z.LayerDead[layer] = OrganNutrientsState.Subtract(Z.LayerDead[layer], detachedToday, parentOrgan.Cconc);
                        Z.LayerDead[layer] = OrganNutrientsState.Subtract(Z.LayerDead[layer], OrganNutrientsState.Multiply(deadRemoved, Z.LayerDeadProportion[layer], parentOrgan.Cconc), parentOrgan.Cconc);
                        checkTotalWt += (Z.LayerLive[layer].Wt + Z.LayerDead[layer].Wt);
                        checkTotalN += (Z.LayerLive[layer].N + Z.LayerDead[layer].N);

                        FOMType fom = new FOMType();
                        fom.amount = (float)(detachedToday.Wt * 10);
                        fom.N = (float)(detachedToday.N * 10);
                        fom.C = (float)(0.40 * detachedToday.Wt * 10);
                        fom.P = 0.0;
                        fom.AshAlk = 0.0;

                        FOMLayerLayerType Layer = new FOMLayerLayerType();
                        Layer.FOM = fom;
                        Layer.CNR = 0.0;
                        Layer.LabileP = 0.0;
                        FOMLayers[layer] = Layer;
                    }
                    FOMLayerType FomLayer = new FOMLayerType();
                    FomLayer.Type = parentPlant.PlantType;
                    FomLayer.Layer = FOMLayers;
                    Z.nutrient.DoIncorpFOM(FomLayer);
                }
               if (Math.Abs(checkTotalWt - parentOrgan.Wt)> 2e-12)
                        throw new Exception("C Mass balance error in root profile partitioning");
                if (Math.Abs(checkTotalN - parentOrgan.N) > 2e-12)
                    throw new Exception("C Mass balance error in root profile partitioning");
            }
            //}
            //}
        }


        /// <summary>grow roots in each zone.</summary>
        public void GrowRootDepth()
        {
            foreach (NetworkZoneState z in Zones)
                z.GrowRootDepth();
        }
        /// <summary>Initialise all zones.</summary>
        private void InitialiseZones()
        {
            PlantZone.Initialize(parentPlant.SowingData.Depth);
            Zones.Add(PlantZone);
            if (ZoneRootDepths.Count != ZoneNamesToGrowRootsIn.Count ||
                ZoneRootDepths.Count != ZoneInitialDM.Count)
                throw new Exception("The root zone variables (ZoneRootDepths, ZoneNamesToGrowRootsIn, ZoneInitialDM) need to have the same number of values");

            for (int i = 0; i < ZoneNamesToGrowRootsIn.Count; i++)
            {
                Zone zone = this.FindInScope(ZoneNamesToGrowRootsIn[i]) as Zone;
                if (zone != null)
                {
                    Soil soil = zone.FindInScope<Soil>();
                    if (soil == null)
                        throw new Exception("Cannot find soil in zone: " + zone.Name);
                    NetworkZoneState newZone = new NetworkZoneState(parentPlant, soil);
                    newZone.Initialize(parentPlant.SowingData.Depth);
                    Zones.Add(newZone);
                }
            }
        }

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {

            PlantZone.Clear();
            Zones.Clear();
        }

    }
}





