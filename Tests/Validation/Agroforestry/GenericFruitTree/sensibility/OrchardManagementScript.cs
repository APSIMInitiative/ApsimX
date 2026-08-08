using System;
using Models;
using Models.Core;
using Models.Agroforestry;
using Models.Soils;

namespace UserCode
{
    [Serializable]
    public class OrchardManagementScript : Model
    {
        [Link] private Clock Clock = null!;
        [Link] private ISummary Summary = null!;
        [Link(ByName = true)] private GenericFruitTree FruitTree = null!;
        [Link] private Irrigation Irrigation = null!;
        [Link] private Fertiliser Fertiliser = null!;

        [Description("Annual pruning fraction of live leaf biomass")]
        public double PruningLeafFraction { get; set; } = 0.10;
        [Description("Annual pruning fraction for canopy structure and light interception")]
        public double PruningStructuralFraction { get; set; } = 0.10;
        [Description("Late-February thinning fraction of live fruit biomass")]
        public double ThinningFraction { get; set; } = 0.15;
        [Description("Irrigation trigger as root-zone PAW fraction")]
        public double IrrigationPAWTrigger { get; set; } = 0.45;
        [Description("Irrigation amount when triggered (mm)")]
        public double IrrigationAmount { get; set; } = 25.0;
        [Description("Minimum days between irrigation events")]
        public int MinimumIrrigationInterval { get; set; } = 10;
        [Description("Annual nitrogen application (kg N/ha)")]
        public double AnnualNitrogen { get; set; } = 40.0;

        public double ReservePoolCapacityRatioOutput { get; private set; }
        public double ReserveCumulativeSurplusDMOutput { get; private set; }
        public double ReserveCumulativeDeficitDMOutput { get; private set; }

        private DateTime lastIrrigation = DateTime.MinValue;
        private int lastPruneYear = int.MinValue;
        private int lastThinYear = int.MinValue;
        private int lastFertiliseYear = int.MinValue;

        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            ReservePoolCapacityRatioOutput = 0.0;
            ReserveCumulativeSurplusDMOutput = 0.0;
            ReserveCumulativeDeficitDMOutput = 0.0;
            lastIrrigation = DateTime.MinValue;
            lastPruneYear = int.MinValue;
            lastThinYear = int.MinValue;
            lastFertiliseYear = int.MinValue;
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if (Clock.Today.DayOfYear == 210 && lastPruneYear != Clock.Today.Year)
            {
                FruitTree.PruningLeafFraction = PruningLeafFraction;
                FruitTree.PruningStructuralFraction = PruningStructuralFraction;
                new Events(this).Publish("Prune", new object[] { this, EventArgs.Empty });
                Summary.WriteMessage(this, $"Pruned {PruningLeafFraction:P0} of live leaf biomass and {PruningStructuralFraction:P0} of canopy structure.", MessageType.Information);
                lastPruneYear = Clock.Today.Year;
            }

            if (Clock.Today.DayOfYear == 260 && lastFertiliseYear != Clock.Today.Year)
            {
                Fertiliser.Apply(amount: AnnualNitrogen, type: "NO3N");
                lastFertiliseYear = Clock.Today.Year;
            }

            if (Clock.Today.DayOfYear == 55 && lastThinYear != Clock.Today.Year)
            {
                FruitTree.ThinningFraction = ThinningFraction;
                new Events(this).Publish("Thin", new object[] { this, EventArgs.Empty });
                Summary.WriteMessage(this, $"Thinned {ThinningFraction:P0} of live fruit biomass.", MessageType.Information);
                lastThinYear = Clock.Today.Year;
            }

            if ((Clock.Today - lastIrrigation).TotalDays >= MinimumIrrigationInterval
                && FruitTree.RootZonePAWFractionOutput < IrrigationPAWTrigger)
            {
                Irrigation.Apply(IrrigationAmount);
                lastIrrigation = Clock.Today;
            }
        }

        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            double capacity = Math.Max(0.0, FruitTree.ReserveCapacityDMOutput);
            double pool = Math.Max(0.0, FruitTree.ReservePoolDMOutput);
            ReservePoolCapacityRatioOutput = capacity > 0.0 ? pool / capacity : 0.0;

            double supply = Math.Max(0.0, FruitTree.ReserveSupplyDMOutput);
            double demand = Math.Max(0.0, FruitTree.ReserveCriticalDemandDMOutput);
            ReserveCumulativeSurplusDMOutput += Math.Max(0.0, supply - demand);
            ReserveCumulativeDeficitDMOutput += Math.Max(0.0, demand - supply);
        }
    }
}
