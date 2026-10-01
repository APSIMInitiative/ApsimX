using Models.CLEM.Activities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Breed
    /// </summary>
    public class RuminantActivityBreedSummary : RuminantActivitySummaryBase<RuminantActivityBreed>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            return
            [
                new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(RuminantActivityControlledMating),
                missing: "This simulation uses natural (uncontrolled) mating that will occur when males and females of breeding condition are located together.",
                introduction: "Mating uses the following approach",
                borderClass: "childgroupborder activitygroup"
                )
            ];
        }


        /// <inheritdoc/>
        public override void BuildSummary()
        {
            if (ModelTyped.InferStartupPregnancy)
            {
                generator.AddBlockWithText("Pregnancy status of breeders from matings prior to simulation start will be predicted.");
            }
            else
            {
                generator.AddBlockWithText("No pregnancy of breeders from matings prior to simulation start is inferred.");
            }
        }
    }
}