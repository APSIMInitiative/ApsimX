using System;
using APSIM.Core;
using Newtonsoft.Json;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;
using Models.Functions;

namespace Models.Agroforestry
{
    /// <summary>
    /// Extends BasialBuds so it also resets itself on the tree’s
    /// DormancyStage and initializes on BudBreakStage.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(GenericFruitTree))]
    public class BudOrgan : Models.PMF.Organs.BasialBuds
    {
        [Link(Type = LinkType.Ancestor)] private GenericFruitTree tree = null!;
        [Link] private ISummary Summary = null!;

        // Allow cultivar/management to set buds per plant at bud-break
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction BudsPerTreeAtBudBreak = null!;

        // What was sown (buds per plant); captured at sow and used as fallback at bud-break
        [JsonIgnore] private double sownBudsPerPlant = 1.0;

        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters s)
        {
            // s.Population is plants/m^2; s.BudNumber is buds/plant
            sownBudsPerPlant = Math.Max(0.0, s.BudNumber);
            double population = Math.Max(0.0, s.Population);
            NodeNumber = sownBudsPerPlant * population; // buds/m^2
            Summary?.WriteMessage(this,
                $"[Bud] Sow: buds/plant={sownBudsPerPlant:F2}, pop={population:F2} -> NodeNumber={NodeNumber:F2}",
                MessageType.Diagnostic);
        }

        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e) => NodeNumber = 0.0;

        /// <summary>Respond to phase changes for dormancy/bud-break.</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType args)
        {
            if (sender != tree) return; // ignore other plants

            bool hasDormancy = !string.IsNullOrWhiteSpace(tree.DormancyStage);
            bool hasBudBreak = !string.IsNullOrWhiteSpace(tree.BudBreakStage);
            string stage = args.StageName?.Trim() ?? string.Empty;

            if (hasDormancy && stage.Equals(tree.DormancyStage, StringComparison.OrdinalIgnoreCase))
                DormancyStart();
            else if (hasBudBreak && stage.Equals(tree.BudBreakStage, StringComparison.OrdinalIgnoreCase))
                BudBreakStart();
        }

        private void DormancyStart()
        {
            NodeNumber = 0.0;
            Summary?.WriteMessage(this, "[Bud] Dormancy start -> NodeNumber reset to 0", MessageType.Diagnostic);
        }

        private void BudBreakStart()
        {
            // Prefer the optional function; fall back to what was sown
            double budsPerPlant = BudsPerTreeAtBudBreak?.Value() ?? sownBudsPerPlant;
            double population = Math.Max(0.0, tree.Population);
            NodeNumber = Math.Max(0.0, budsPerPlant) * population; // buds/m^2
            Summary?.WriteMessage(this,
                $"[Bud] Bud-break: buds/plant={(BudsPerTreeAtBudBreak != null ? "func" : "sown")}={budsPerPlant:F2}, pop={population:F2} -> NodeNumber={NodeNumber:F2}",
                MessageType.Diagnostic);
        }
    }
}
