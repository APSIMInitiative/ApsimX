using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Documentation.Extensions;
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
    public class RootNetwork : Model, IWaterNitrogenUptake, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        ///1. Links
        ///--------------------------------------------------------------------------------------------------

        /// <summary>The plant</summary>
        [Link]
        protected Plant parentPlant = null;

        /// <summary>The plant</summary>
        [Link(Type = LinkType.Ancestor)]
        public Organ parentOrgan = null;

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

        /// <summary>The maximum daily N uptake in kg N/ha/d</summary>
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

        /// <summary>A list of all zones to grow roots in</summary>
        [JsonIgnore]
        public List<NetworkZoneState> Zones { get; set; }

        /// <summary>The zone where the plant is growing</summary>
        [JsonIgnore]
        public NetworkZoneState PlantZone { get; set; }

        /// <summary>The amount of root weight in each zone </summary>
        public double[] WtByLayerZone1
        {
            get
            {
                NetworkZoneState z = Zones[0];
                double[] ret = new double[z.LayerLive.Length];
                for (int i = 0; i < z.LayerLive.Length; i++)
                        ret[i] = z.LayerLive[i].Wt;
                return ret;
            }
        }

        /// <summary>The amount of root weight in each zone </summary>
        public double[] WtByLayerZone2
        {
            get
            {
                if (Zones.Count == 1)
                    return new double[0];
                else
                {
                    NetworkZoneState z = Zones[1];
                    double[] ret = new double[z.LayerLive.Length];
                    for (int i = 0; i < z.LayerLive.Length; i++)
                        ret[i] = z.LayerLive[i].Wt;
                    return ret;
                }
            }
        }

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

        /// <summary>
        /// The kl being used daily in each layer
        /// </summary>
        public double[] klByLayer { get; set; }

        /// <summary>Gets the nitrogen uptake.</summary>
        [Units("mm")]
        [JsonIgnore]
        public double[] NUptakeLayered
        {
            get
            {
                if (Zones == null || Zones.Count == 0)
                    return Array.Empty<double>();
                if (Zones.Count > 1)
                    throw new Exception(this.Name + " Can't report layered Nuptake for multiple zones as they may not have the same size or number of layers");
                double[] uptake = new double[Zones[0].Physical.Thickness.Length];
                if (Zones[0].NitUptake != null)
                    uptake = Zones[0].NitUptake;
                return MathUtilities.Multiply_Value(uptake, -1); // convert to positive values.
            }
        }

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

        /// <summary>Water uptake allocated to the root network by the soil arbitrator</summary>
        public PlantWaterOrNDelta WaterTakenUp { get; set; }

        /// <summary>Nitrogen uptake allocated to this plant by the soil arbitrator</summary>
        public PlantWaterOrNDelta NitrogenTakenUp { get; set; }

        /// <summary>Water supplied by root network to soil arbitrator for this plant instance</summary>
        public PlantWaterOrNDelta WaterUptakeSupply { get; set; }

        /// <summary>Nitrogen supplied by the root network to the soil arbitrator for this plant instance</summary>
        public PlantWaterOrNDelta NitrogenUptakeSupply { get; set; }

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
                    Zone zone = Structure.Find<Zone>(Z.Name);
                    var soilPhysical = Structure.FindChild<IPhysical>(relativeTo: Z.Soil);
                    var waterBalance = Structure.FindChild<ISoilWater>(relativeTo: Z.Soil);
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
                        var soilPhysical = Structure.FindChild<IPhysical>(relativeTo: Z.Soil);
                        var waterBalance = Structure.FindChild<ISoilWater>(relativeTo: Z.Soil);
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
                        var soilPhysical = Structure.FindChild<IPhysical>(relativeTo: Z.Soil);
                        var waterBalance = Structure.FindChild<ISoilWater>(relativeTo: Z.Soil);

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

        /// <summary>Gets water supply.</summary>
        /// Returns a value in l so must adjust for zone size
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

            double[] ll = soilCrop.LL;

            double[] supply = new double[myZone.Physical.Thickness.Length];
            LayerMidPointDepth = myZone.Physical.DepthMidPoints;
            for (int layer = 0; layer < myZone.Physical.Thickness.Length; layer++)
            {
                if (layer <= SoilUtilities.LayerIndexOfDepth(myZone.Physical.Thickness, myZone.Depth))
                {
                    double available = zone.Water[layer] - ll[layer] * myZone.Physical.Thickness[layer];

                    supply[layer] = Math.Max(0.0, klByLayer[layer] *  available * myZone.RootProportions[layer]);

                    //supply[layer] *= zone.Zone.Area * 10000; // Calculation above is in mm (l/m2).  To convert to m2 multiply by zone area (which has to be multiplied by 10000 to convert from ha to m2.
                }
            }
            return supply;
        }

        /// <summary>Computes root total water supply.</summary>
        public double TotalExtractableWater()
        {
            double[] LL = soilCrop.LL;
            double[] SWmm = PlantZone.WaterBalance.SWmm;
            double[] DZ = PlantZone.Physical.Thickness;

            double supply = 0;
            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= SoilUtilities.LayerIndexOfDepth(PlantZone.Physical.Thickness, Depth))
                {
                    double available = Math.Max(SWmm[layer] - LL[layer] * DZ[layer] * PlantZone.LLModifier[layer], 0);

                    supply += Math.Max(0.0, klByLayer[layer] * available * PlantZone.RootProportions[layer]);
                }
            }
            return supply;
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
            List<double> zoneNuptakes = new List<double>(zonesFromSoilArbitrator.Count);
            foreach (ZoneWaterAndN thisZone in zonesFromSoilArbitrator)
            {

                NetworkZoneState zone = Zones.Find(z => z.Name == thisZone.Zone.Name);
                if (zone != null)
                {
                    //NO3 and NH4 pased in zonesFromSoilArbitrator are in kg.  Need to convert to kg/ha to set soil uptake
                    double[] thisZoneNO3kgpha = new double[thisZone.NO3N.Count()];
                    double[] thisZoneNH4kgpha = new double[thisZone.NO3N.Count()];
                    for (int i = 0; i < thisZone.NO3N.Count(); i++)
                    {
                        thisZoneNO3kgpha[i] = MathUtilities.Divide(thisZone.NO3N[i], thisZone.Zone.Area, 0);
                        thisZoneNH4kgpha[i] = MathUtilities.Divide(thisZone.NH4N[i], thisZone.Zone.Area, 0);
                    }

                    zone.NO3.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(zone.NO3.kgha, thisZoneNO3kgpha));
                    zone.NH4.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(zone.NH4.kgha, thisZoneNH4kgpha));

                    zone.NitUptake = MathUtilities.Multiply_Value(MathUtilities.Add(thisZone.NO3N, thisZone.NH4N), -1);
                    zoneNuptakes.Add(thisZone.NO3N.Sum()+thisZone.NH4N.Sum());
                }
            }
            NitrogenTakenUp.AmountByZone = zoneNuptakes.ToArray();
        }

        /// <summary>Gets the nitrogen supply (kg) from the specified zone for the current plant instance.</summary>
        /// <param name="zone">The zone.</param>
        /// <param name="NO3Supply_kg">The returned NO3 supply</param>
        /// <param name="NH4Supply_kg">The returned NH4 supply</param>
        public void CalculateNitrogenSupply(ZoneWaterAndN zone, ref double[] NO3Supply_kg, ref double[] NH4Supply_kg)
        {
            NetworkZoneState myZone = Zones.Find(z => z.Name == zone.Zone.Name);
            if (myZone != null)
            {
                if (RWC == null || RWC.Length != myZone.Physical.Thickness.Length)
                    RWC = new double[myZone.Physical.Thickness.Length];



                double[] thickness = myZone.Physical.Thickness;
                double[] water = myZone.WaterBalance.SWmm;
                double[] ll15mm = myZone.Physical.LL15mm;
                double[] dulmm = myZone.Physical.DULmm;
                double[] bd = myZone.Physical.BD;

                double accuDepth = 0;
                double NO3Supply_kgpha = 0;
                double NH4Supply_kgpha = 0;

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
                        double maxNO3uptake = maxNUptake - NO3Supply_kgpha - NH4Supply_kgpha;
                        double NO3Supply_kgpha_layer = Math.Min(zone.NO3N[layer] * kno3 * NO3ppm * SWAF * factorRootDepth, maxNO3uptake);
                        NO3Supply_kgpha += NO3Supply_kgpha_layer;
                        NO3Supply_kg[layer] = NO3Supply_kgpha_layer * myZone.Area;

                        double knh4 = this.knh4.Value(layer);
                        double NH4ppm = zone.NH4N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                        double maxNH4Uptake = maxNUptake - NH4Supply_kgpha - NO3Supply_kgpha;
                        double NH4Supply_kgpha_layer = Math.Min(zone.NH4N[layer] * knh4 * NH4ppm * SWAF * factorRootDepth, maxNH4Uptake);
                        NH4Supply_kgpha += NH4Supply_kgpha_layer;
                        NH4Supply_kg[layer] = NH4Supply_kgpha_layer * myZone.Area;
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

            Soil soil = Structure.Find<Soil>();
            if (soil == null)
                throw new Exception("Cannot find soil");
            PlantZone = new NetworkZoneState(parentPlant, soil, Structure);
            ZoneNamesToGrowRootsIn.Add(PlantZone.Name);

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
                    z.CalculateRelativeLiveBiomassProportions();
                    z.CalculateRelativeDeadBiomassProportions();
                    z.CalculateRootProportionThroughLayer();
                }

                double[] KL = soilCrop.KL;
                for (int layer = 0; layer < Zones[0].Physical.Thickness.Length; layer++)
                {
                    klByLayer[layer] = KL[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer);
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

            double TotalArea = 0;

            foreach (NetworkZoneState Z in Zones)
            {
                TotalArea += Z.Area;
            }

            PartitionBiomassThroughSoil(new OrganNutrientsState(), new OrganNutrientsState(),
                                             Initial, new OrganNutrientsState(),
                                             new OrganNutrientsState(),
                                             new OrganNutrientsState(), new OrganNutrientsState());
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
            double TotalArea = 0;
            foreach (NetworkZoneState Z in Zones)
            {
                TotalArea += Z.Area;
            }

            if (parentPlant.IsAlive)
            {
                double checkTotalWt = 0;
                double checkTotalN = 0;

                foreach (NetworkZoneState z in Zones)
                {
                    double RZA = z.Area / TotalArea;
                    TotalRAw = z.RAw.Sum();
                    FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[z.LayerLive.Length];
                    for (int layer = 0; layer < z.Physical.Thickness.Length; layer++)
                    {
                        z.LayerLive[layer] -= (liveRemoved * RZA * z.LayerLiveProportion[layer]);
                        z.LayerLive[layer] -= (reAllocated * RZA * z.LayerLiveProportion[layer]);
                        z.LayerLive[layer] -= (reTranslocated * RZA * z.LayerLiveProportion[layer]);
                        z.LayerLive[layer] -= (senesced * RZA * z.LayerLiveProportion[layer]);
                        double fracAlloc = MathUtilities.Divide(z.RAw[layer], TotalRAw, 0);
                        z.LayerLive[layer] += (allocated * RZA * fracAlloc);

                        z.LayerDead[layer] += (senesced * RZA * z.LayerLiveProportion[layer]);
                        OrganNutrientsState detachedToday = detached * RZA * z.LayerDeadProportion[layer];
                        z.LayerDead[layer] -= detachedToday;
                        z.LayerDead[layer] -= (deadRemoved * RZA * z.LayerDeadProportion[layer]);
                        checkTotalWt += (z.LayerLive[layer].Wt + z.LayerDead[layer].Wt);
                        checkTotalN += (z.LayerLive[layer].N + z.LayerDead[layer].N);

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
                    z.nutrient.DoIncorpFOM(FomLayer);
                }
               if (!MathUtilities.FloatsAreEqual(checkTotalWt, parentOrgan.Wt, Math.Max(checkTotalWt * 1e-10,1e-12)))
                        throw new Exception("C Mass balance error in root profile partitioning");
               if (!MathUtilities.FloatsAreEqual(checkTotalN, parentOrgan.N, Math.Max(checkTotalN * 1e-10,1e-12)))
                        throw new Exception("C Mass balance error in root profile partitioning");
            }
        }

        /// <summary>Sets root biomass to zero and passes existing biomass to soil </summary>
        public void endRoots()
        {
            if (parentPlant.IsAlive)
            {
                foreach (NetworkZoneState z in Zones)
                {
                    FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[z.LayerLive.Length];
                    for (int layer = 0; layer < z.Physical.Thickness.Length; layer++)
                    {
                        OrganNutrientsState detachedToday = z.LayerLive[layer] + z.LayerDead[layer];
                        z.LayerDead[layer] = new OrganNutrientsState(parentOrgan.Cconc);
                        z.LayerLive[layer] = new OrganNutrientsState(parentOrgan.Cconc);

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
                    z.nutrient.DoIncorpFOM(FomLayer);
                }
            }
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
            List<double> zoneAreas = new List<double>();
            foreach (string z in ZoneNamesToGrowRootsIn)
            {
                Zone zone = Structure.Find<Zone>(z);
                if (zone != null)
                {
                    Soil soil = Structure.Find<Soil>(relativeTo: zone);
                    if (soil == null)
                        throw new Exception("Cannot find soil in zone: " + zone.Name);
                    NetworkZoneState newZone = null;
                    if (z == PlantZone.Name)
                        newZone = PlantZone;
                    else
                        newZone = new NetworkZoneState(parentPlant, soil, Structure);
                    newZone.Initialize(parentPlant.SowingData.Depth);
                    Zones.Add(newZone);
                    zoneAreas.Add(newZone.Area);
                }
            }

            klByLayer = new double[Zones[0].Physical.Thickness.Length];

            WaterUptakeSupply = new PlantWaterOrNDelta(zoneAreas);
            NitrogenUptakeSupply = new PlantWaterOrNDelta(zoneAreas);
            WaterTakenUp = new PlantWaterOrNDelta(zoneAreas);
            NitrogenTakenUp = new PlantWaterOrNDelta(zoneAreas);
        }

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {

            PlantZone.Clear();
            Zones.Clear();
        }

    }




}





