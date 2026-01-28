using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Trucking
    /// </summary>
    public class RuminantTruckingSummary : RuminantActivitySummaryBase<RuminantTrucking>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            return
            [
                new ChildComponentGroup(
                id: "loadrelationship",
                model: CLEMModel,
                childType: typeof(Relationship),
                missing: "",
                introduction: "Relationship defining the individual live weight to load unit size",
                borderClass: "childgroupborder activitygroup"
                ),
                new ChildComponentGroup(
                id: "ruminantgroups",
                model: CLEMModel,
                childType: typeof(RuminantGroup),
                missing: "No individual specified",
                introduction: "Individuals to be trucked",
                borderClass: "childgroupborder filtergroup"
                ),
                new ChildComponentGroup(
                id: "truckfees",
                model: CLEMModel,
                childType: typeof(ActivityFee),
                missing: "",
                introduction: "Fees associated with trucking",
                borderClass: "childgroupborder activitygroup"
                ),
                new ChildComponentGroup(
                id: "trucklabour",
                model: CLEMModel,
                childType: typeof(LabourRequirement),
                missing: "",
                introduction: "Labour required for trucking",
                borderClass: "childgroupborder filtergroup"
                ),
                new ChildComponentGroup(
                id: "truckemissions",
                model: CLEMModel,
                childType: typeof(GreenhouseGasActivityEmission),
                missing: "",
                introduction: "Trucking emissions",
                borderClass: "childgroupborder activitygroup"
                )
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText($"It is {generator.DisplaySummaryValueSnippet(ModelTyped.DistanceToMarket)} km to market");

            generator.AddBlockWithText($"Each load unit (pod/deck) holds {generator.DisplaySummaryValueSnippet(ModelTyped.NumberPerLoadUnit, warnZero: true)} head (of specified individuals)");

            string output = "Each truck ";
            if (ModelTyped.MinimumLoadUnitsPerTruck > 0)
            {
                output = $" requires a minimum of {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumLoadUnitsPerTruck, warnZero: true)} load units and ";
            }
            output += $"has a maximum of {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumLoadUnitsPerTruck, warnZero: true)} load units permitted";
            if (ModelTyped.MinimumLoadUnitsBeforeTransporting > 0)
            {
                output += $" and requires at least {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumLoadUnitsBeforeTransporting, warnZero: true)} load units before transporting.";
            }
            generator.AddBlockWithText(output);

            output = "";
            if (ModelTyped.LoadUnitsPerTrailer.Length > 1)
            {
                output = $"Trailers from first to last hold ";
            }
            else
            {
                output = $"The trailer holds ";
            }

            output += $"{generator.DisplaySummaryValueSnippet<double>(ModelTyped.LoadUnitsPerTrailer, warnZero: true)} load units";

            if (ModelTyped.MinimumLoadUnitsBeforeAddTrailer.Max() > 0)
            {
                output += $" and requires {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumLoadUnitsBeforeAddTrailer, warnZero: true)} load units before adding each trailer";
            }
            generator.AddBlockWithText(output);

            output = "";

            output += $"Each truck has a Tare Mass (with average fuel) of {generator.DisplaySummaryValueSnippet(ModelTyped.TruckTareMass.ToString("0.##"), warnZero: true)} kg ";
            output += $"with an Aggregate Trailer Mass {generator.DisplaySummaryValueSnippet<double>(ModelTyped.AggregateTrailerMass, warnZero: true)} (kg)";
            if (ModelTyped.AggregateTrailerMass.Length > 1)
            {
                output += $" from first to last trailer";
            }
            generator.AddBlockWithText(output);
        }
    }
}