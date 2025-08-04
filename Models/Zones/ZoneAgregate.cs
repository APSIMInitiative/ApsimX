using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;
using Models.Soils.Arbitrator;

namespace Models.Zones
{
    /// <summary>Agregates variables across zones</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [Description("This model agregates variablse across multiple zones for reporting")]
    public class ZoneAgregate : Model, ILocatorDependency
    {
        [NonSerialized] private ILocator locator;

        /// <summary>
        /// Zones in simulation
        /// </summary>
        public List<Zone> Zones;

        /// <summary>
        /// The total area of the zones in the simulation
        /// </summary>
        [Units("m2")]
        public double SimulationArea { get { return ZoneAreas.Sum(); } }

        private List<double> ZoneAreas
        {
            get
            {
                List<double> ret = new List<double>();
                foreach (Zone zone in Zones)
                {
                    ret.Add(zone.Area);
                }
                return ret;
            }
        }

        /// <summary>Potential Evapotranspiration for plant canopy </summary>
         public PlantWaterOrNDelta Eop { get; set; }

        /// <summary> Incident radiation</summary>
        public PlantWaterOrNDelta Ro { get; set; }

        /// <summary> Incident radiation</summary>
        public PlantWaterOrNDelta Ri { get; set; }

        /// <summary> Incident radiation</summary>
        public PlantWaterOrNDelta Rid { get; set; }

        /// <summary> Plant Transpiration </summary>
        public PlantWaterOrNDelta Et { get; set; }

        /// <summary>Irrigation averaged over all zones in simulation</summary>
        public PlantWaterOrNDelta Irrigation { get; set; }

        /// <summary>Irrigation averaged over all zones in simulation</summary>
        public PlantWaterOrNDelta AccumulatedIrrigation { get; set; }

        /// <summary>Nitrogen averaged over all zones in simulation</summary>
        public PlantWaterOrNDelta Nitrogen { get; set; }

        /// <summary>Nitrogen averaged over all zones in simulation</summary>
        public PlantWaterOrNDelta AccumulatedNitrogen { get; set; }

        /// <summary>Radiation intercepted by green leaf over the area of the simulation (tree and understory)</summary>
        public double GreenAreaRadiationInterception { get { return Ri.Amount / Ro.Amount; } }
        /// <summary>Radiation intercepted by dead material over the area of the simulation (tree and understory)</summary>
        public double DeadAreaRadiationInterception { get { return Rid.Amount / Ro.Amount; } }
        /// <summary> The proportion of radiation intercepted by the green leaf on the tree canopy </summary>
        public double FintTreeGreen { get { return Ri.AmountByZone[0] / Ro.Amount; } }
        ///<summary> The proportion of radiation intercepted by the trunk and dead leaf on the tree canopy </summary>
        public double FintTreeDead { get { return Rid.AmountByZone[0] / Ro.Amount; } }
        ///<summary> The proportion of radiation intercepted by the tree canopy </summary>
        public double FintTreeTotal { get { return (Rid.AmountByZone[0]+ Ri.AmountByZone[0]) / Ro.Amount; } }

        /// <summary> The proportion of radiation intercepted by the green leaf of the understory </summary>
        public double FintUnderstoryGreen { get { return Ri.AmountByZone[1] / Ro.Amount; } }
        /// <summary> The proportion of radiation intercepted by the dead leaf of the understory </summary>
        public double FintUnderstoryDead { get { return Rid.AmountByZone[1] / Ro.Amount; } }

        /// <summary>Locator supplied by APSIM kernel.</summary>
        public void SetLocator(ILocator locator) => this.locator = locator;

        PlantWaterOrNDelta UpdateValues(string varName, bool VarPerM2 = false)
        {
            return new PlantWaterOrNDelta(ZoneAreas, amountByZone(varName, VarPerM2));
        }

        private List<double> amountByZone(string varName, bool VarPerM2)
        {
            List<double> ret = new List<double>();
            foreach (Zone z in Zones)
            {
                double areaAdjustment = 1.0;
                if (VarPerM2)
                    areaAdjustment = (double)locator.Get("Area", relativeTo: z) * 10000;
                ret.Add((locator.Get(varName, relativeTo: z) != null)? (double)locator.Get(varName, relativeTo: z) * areaAdjustment : 0.0 );
            }
            return ret;
        }

        [EventSubscribe("DoReportCalculations")]
        private void onDoReportCalculations(object sender, EventArgs e)
        {
            Eop = UpdateValues("[ICanopy].PotentialEP", true);
            Ri = UpdateValues("[Leaf].Canopy.RadiationIntercepted");
            Rid = UpdateValues("[Trunk].EnergyBalance.RadiationInterceptedByDead");
            Et = UpdateValues("[ICanopy].Transpiration", true);
            Ro = UpdateValues("IncidentRadiation");
            Irrigation = UpdateValues("[Irrigation].IrrigationApplied", true);
            Nitrogen = UpdateValues("[Fertiliser].NitrogenApplied", true); //divide N by 10 to make grams
            AccumulatedIrrigation = PlantWaterOrNDelta.Add(AccumulatedIrrigation,Irrigation);
            AccumulatedNitrogen = PlantWaterOrNDelta.Add(AccumulatedNitrogen, Nitrogen);
        }

        /// <summary>Called when simulation starts.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            Zones = new List<Zone>();
            foreach (Zone newZone in this.Parent.FindAllDescendants<Zone>())
                Zones.Add(newZone);
            string message = "For ZoneAgregate to work it requires a multizone simulation with the first zone named \"Row\" and the second zone named \"Alley\"";
            if (Zones[0].Name != "Row")
                throw new Exception(message);
            if (Zones[1].Name != "Alley")
                throw new Exception(message);
            AccumulatedIrrigation = new PlantWaterOrNDelta(ZoneAreas);
            AccumulatedNitrogen = new PlantWaterOrNDelta(ZoneAreas);
        }
    }


}
