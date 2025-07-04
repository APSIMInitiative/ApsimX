using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using Newtonsoft.Json;

namespace Models.PMF
{
    ///<summary>
    /// Interface between soil arbitrator and Plant model instance
    /// All supplies, demands and uptakes passed to and from the Soil Arbitrator are in liters for water and kg for N.  
    /// This is because the Soil Arbitrator work independently of area.
    /// This interface needs to adjust uptakes back to the appropriate units to send them back to plan.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    public class RootUptakesArbitrator : Model, IUptake
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The constant to convert kg/ha to g/m2</summary>
        protected const double kgha2gsm = 0.1;
        /// <summary>The constant to convert ha to m2</summary>
        protected const double ha2sm = 10000;

        private bool initialWaterEstimate = true;
        private bool initialNitrogenEstimate = true;

        /// <summary>The list of organs</summary>
        protected List<IWaterNitrogenUptake> uptakingOrgans = new List<IWaterNitrogenUptake>();

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> waterDemandingOrgans = new List<IHasWaterDemand>();

        private BiomassArbitrator biomassArbitrator { get; set; }

        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>The Live status of the crop</summary>
        [JsonIgnore]
        public bool IsAlive { get { return plant.IsAlive; } }

        /// <summary>Water demand from canopy of this plant model instance.  
        /// Value in liters</summary>
        [JsonIgnore]
        [Units("liters")]
        public double WaterDemand { get; protected set; }

        /// <summary>Water supply from the root network of this plant model instance.
        /// Supply assumes no competition from other plants in same zone.  Soil Arbitrator works out how much is actaully taken up 
        /// This supply is set the first time GetWaterUptakeEstimates is called each day as the initial soil state is passed in.
        /// Value in liters</summary>
        [JsonIgnore]
        [Units("liters")]
        public double WaterSupply { get; protected set; }

        /// <summary>Water taken up by the root network of this plant model instance.
        /// For single plant simulations it Will be the lessor of WaterDemand and WaterSupply 
        /// For multi plant simulations it may be less that WaterSupply as SoilArbitrator could have assigned some of the potentil supply to uptake in competting plants
        /// Value in liters</summary>
        [JsonIgnore]
        [Units("liters")]
        public double WaterUptake { get; protected set; }

        /// <summary>Nitrogen demand from this plant model instance.  
        /// Value in kg</summary>
        [JsonIgnore]
        [Units("kg")]
        public double NitrogenDemand { get; protected set; }

        /// <summary>Nitrogen supply from the root network of this plant model instance.
        /// Supply assumes no competition from other plants in same zone.  Soil Arbitrator works out how much is actaully taken up 
        /// Value in kg</summary>
        [JsonIgnore]
        [Units("kg")]
        public double NitrogenSupply { get; protected set; }

        /// <summary>Nitrogen taken up by the root network of this plant model instance.
        /// For single plant simulations it Will be the lessor of NitrogenDemand and NitrogenSupply 
        /// For multi plant simulations it may be less that NitrogenSupply as SoilArbitrator could have assigned some of the potentil supply to uptake in competting plants
        /// Value in kg</summary>
        [JsonIgnore]
        [Units("kg")]
        public double NitrogenUptake { get; protected set; }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<IHasWaterDemand> OrgansToDemandWater = new List<IHasWaterDemand>();
            foreach (Model hwd in plant.FindAllInScope<IHasWaterDemand>())
                OrgansToDemandWater.Add(hwd as IHasWaterDemand);
            waterDemandingOrgans = OrgansToDemandWater;

            List<IWaterNitrogenUptake> OrgansToUptakeWaterAndN = new List<IWaterNitrogenUptake>();
            foreach (Model wnu in plant.FindAllInScope<IWaterNitrogenUptake>())
                OrgansToUptakeWaterAndN.Add(wnu as IWaterNitrogenUptake);
            uptakingOrgans = OrgansToUptakeWaterAndN;

            biomassArbitrator = plant.FindChild<BiomassArbitrator>();
        }

        /// <summary>Things the plant model does at the start of each day</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfDay")]
        virtual protected void OnStartOfDay(object sender, EventArgs e)
        {
            initialNitrogenEstimate = true;
            initialWaterEstimate = true;
        }

        /// <summary>
        /// Calculate the potential sw uptake for today.  All values in liters
        /// This is called multiple times each day by the soil arbitrator with different soil state conditions varied depending 
        /// on competition from other plant instances.
        /// The first call for the day is with the initial soil state and represents the situation of there is only one plant 
        /// with roots in the zone.
        /// </summary>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsAlive)
            {
                if (initialWaterEstimate)
                {
                    // Calculate total water demand.
                    WaterDemand = 0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                    foreach (IHasWaterDemand WD in waterDemandingOrgans)
                        WaterDemand += WD.CalculateWaterDemand();
                }

                // Get all water supplies.
                //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
                double waterSupplyCurrentSoilState = 0;
                List<double[]> zoneSuppliesByLayer = new List<double[]>();
                List<double> zoneSuppliesProfileSum = new List<double>();
                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    foreach (IWaterNitrogenUptake u in uptakingOrgans)
                    {
                        double[] organSupply = u.CalculateWaterSupply(zone);
                        if (organSupply != null)
                        {
                            zoneSuppliesByLayer.Add(organSupply);
                            zoneSuppliesProfileSum.Add(organSupply.Sum());
                            zones.Add(zone);
                            waterSupplyCurrentSoilState += MathUtilities.Sum(organSupply);
                        }
                    }
                }
                if (initialWaterEstimate)
                {
                    WaterSupply = waterSupplyCurrentSoilState;
                    foreach (IWaterNitrogenUptake u in uptakingOrgans)
                    {
                        //u.WaterUptakeSupply.AmountByZone = zoneSuppliesProfileSum.ToArray();
                    }
                }
                initialWaterEstimate = false;

                // Calculate demand / supply ratio.
                double fractionSupplied = 0;
                if (waterSupplyCurrentSoilState > 0)
                    fractionSupplied = Math.Min(1.0, WaterDemand / waterSupplyCurrentSoilState);

                // Apply demand supply ratio to each zone and create a ZoneWaterAndN structure
                // to return to caller.
                List<ZoneWaterAndN> ZWNs = new List<ZoneWaterAndN>();
                for (int i = 0; i < zoneSuppliesByLayer.Count; i++)
                {
                    // Just send uptake from my zone
                    ZoneWaterAndN uptake = new ZoneWaterAndN(zones[i]);
                    uptake.Water = MathUtilities.Multiply_Value(zoneSuppliesByLayer[i], fractionSupplied);
                    uptake.NO3N = new double[uptake.Water.Length];
                    uptake.NH4N = new double[uptake.Water.Length];
                    ZWNs.Add(uptake);
                }
                return ZWNs;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// Values passed in are in liters.  This converts them to mm to send to soil
        /// </summary>
        public virtual void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {
            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
            {
                waterSupply += MathUtilities.Sum(Z.Water);
            }

            // Calculate the fraction of water demand that has been given to us.
            double fraction = 1;
            if (WaterDemand > 0)
                fraction = Math.Min(1.0, waterSupply / WaterDemand);

            // Proportionally allocate supply across organs.
            WaterUptake = 0.0;

            foreach (IHasWaterDemand WD in waterDemandingOrgans)
            {
                double organDemand = WD.CalculateWaterDemand();
                double allocation = fraction * organDemand;
                WD.WaterAllocation = allocation;
                WaterUptake += allocation;
            }

            // Give the water uptake for each zone to Root so that it can perform the uptake
            // i.e. Root will pass the uptake to the soil water balance.
            foreach (ZoneWaterAndN Z in zones)
                foreach (IWaterNitrogenUptake u in uptakingOrgans)
                {
                    double[] waterMM = new double[Z.Water.Length];
                    for (int i = 0; i < Z.Water.Length; i++)
                    {
                        waterMM[i] = Z.Water[i] / (Z.Zone.Area * 10000); //Multiply water allocation in liters by the area of the zone in m2 to convert to liters 
                    }
                    u.DoWaterUptake(waterMM, Z.Zone.Name);
                }

            List<double> uptakebyzone = new List<double>();
            foreach (IWaterNitrogenUptake u in uptakingOrgans)
            {
                foreach (ZoneWaterAndN Z in zones)
                {
                    uptakebyzone.Add(Z.Water.Sum());
                }
                //u.WaterTakenUp.AmountByZone = uptakebyzone.ToArray();
            }
        }

        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public virtual List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsEmerged)
            {
                if (initialNitrogenEstimate) //This is called multiple times per day with different soil states.  The first call is with the initial value that represents plant supply in the absence of competition
                {
                    NitrogenDemand = (biomassArbitrator.Nitrogen.TotalPlantDemand - biomassArbitrator.Nitrogen.TotalPlantDemandsAllocated) / 1000; //NOTE: This is in kg, not g, to arbitrate N demands for spatial simulations.
                    if (NitrogenDemand < 0) NitrogenDemand = 0;  //NSupply should be zero if Reallocation can meet all demand (including small rounding errors which can make this -ve)
                }

                double nitrogenSupplyCurrentSoilState = 0;//NOTE: This is in kg, not g, to arbitrate N demands for spatial simulations.

                
                foreach (Organ o in biomassArbitrator.PlantOrgans)
                    o.Nitrogen.Supplies.Uptake = 0;  //Fix me.  Organs should zero themselves, not from here.

                List<double> zoneSuppliesProfileSum = new List<double>();

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    ZoneWaterAndN PlantUptakeSupply_kg = new ZoneWaterAndN(zone);

                    PlantUptakeSupply_kg.NO3N = new double[zone.NO3N.Length];
                    PlantUptakeSupply_kg.NH4N = new double[zone.NH4N.Length];
                    
                    //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                    foreach (Organ o in biomassArbitrator.PlantOrgans)
                    {
                        if (o.WaterNitrogenUptakeObject != null)
                        {
                            double[] organNO3Supply_kg = new double[zone.NO3N.Length];
                            double[] organNH4Supply_kg = new double[zone.NH4N.Length];
                            o.WaterNitrogenUptakeObject.CalculateNitrogenSupply(zone, ref organNO3Supply_kg, ref organNH4Supply_kg);
                            PlantUptakeSupply_kg.NO3N = MathUtilities.Add(PlantUptakeSupply_kg.NO3N, organNO3Supply_kg); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                            PlantUptakeSupply_kg.NH4N = MathUtilities.Add(PlantUptakeSupply_kg.NH4N, organNH4Supply_kg);
                            double organSupply_kg = organNH4Supply_kg.Sum() + organNO3Supply_kg.Sum();
                            o.Nitrogen.Supplies.Uptake += organSupply_kg * 1000; //Uptake supply in g so organSupply (in ka/ha) convert to grams/ha 
                            nitrogenSupplyCurrentSoilState += organSupply_kg;
                        }
                    }
                    zones.Add(PlantUptakeSupply_kg);
                    zoneSuppliesProfileSum.Add(PlantUptakeSupply_kg.NO3N.Sum() + PlantUptakeSupply_kg.NH4N.Sum());
                }

                if (initialNitrogenEstimate)
                {
                    foreach (IWaterNitrogenUptake u in uptakingOrgans)
                    {
                        //u.NitrogenUptakeSupply.AmountByZone = zoneSuppliesProfileSum.ToArray();
                    }
                    NitrogenSupply = nitrogenSupplyCurrentSoilState;
                }
                initialNitrogenEstimate = false;

                if (nitrogenSupplyCurrentSoilState > NitrogenDemand)
                {
                    //Reduce the PotentialUptakes that we pass to the soil arbitrator
                    double ratio = Math.Min(1.0, NitrogenDemand / nitrogenSupplyCurrentSoilState);
                    foreach (ZoneWaterAndN UptakeDemands in zones)
                    {
                        UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, ratio);
                        UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, ratio);
                    }
                }
                return zones;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (plant.IsEmerged)
            {
                // Calculate the total no3 and nh4 across all zones.
                double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                foreach (ZoneWaterAndN Z in zones)
                    NSupply += (Z.NO3N.Sum() + Z.NH4N.Sum());  

                NitrogenUptake = NSupply;
                //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
                //NUptakeSupply units should be g
                biomassArbitrator.AllocateNUptake(NSupply * 1000); //Allocation to plant in g so allocation from soil arbitrator (in ka/ha) convert to grams/ha 

                List<double> uptakebyzone = new List<double>();
                foreach (IWaterNitrogenUptake u in uptakingOrgans)
                {  //Fix me.  This needs to be modified to account for multiple uptakeing organs
                    u.DoNitrogenUptake(zones);
                    foreach (ZoneWaterAndN Z in zones)
                    {
                        uptakebyzone.Add((Z.NH4N.Sum()+Z.NO3N.Sum())); //Allocation to plant in g so allocation from soil arbitrator (in ka/ha) convert to grams/ha 
                    }
                    //u.NitrogenTakenUp.AmountByZone = uptakebyzone.ToArray();
                }
            }
        }
    }
}
