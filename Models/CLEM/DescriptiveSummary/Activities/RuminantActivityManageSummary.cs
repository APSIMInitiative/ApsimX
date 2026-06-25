using BruTile.Wms;
using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Manage
    /// </summary>
    public class RuminantActivityManageSummary : RuminantActivitySummaryBase<RuminantActivityManage>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            List<ChildComponentGroup> groups = [];

            var identifiers = ModelTyped.LocateCompanionModels<RuminantGroup>().Select(a => a.Key);
            foreach (var identifier in identifiers)
            {
                groups.Add(new ChildComponentGroup(
                    id: $"activitygroup_{identifier}",
                    model: CLEMModel,
                    childType: typeof(RuminantGroup),
                    missing: "No RuminantGroup component provided",
                    introduction: $"Individuals selected for {identifier} will be further refined by:",
                    borderClass: "childgroupborder filtergroup"
                ));
            }
            groups.Add(new ChildComponentGroup(
                id: "childgroup_specify",
                model: CLEMModel,
                childType: typeof(SpecifyRuminant),
                missing: "No SpecifyRuminant component provided",
                introduction: "The following SpecifyRuminant components will define the individuals to be purchased (Breeding males and females):",
                borderClass: "childgroupborder resourcegroup"
            ));

            return groups;
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            List<string> notBeingMarked = new List<string>();
            var model = ModelTyped;
            if (model is null) return;

            // Safety stop information
            generator.AddBlockWithText($"The simulation will stop if breeders exceed {generator.DisplaySummaryValueSnippet(model.MaxBreedersMultiplierToStop, warnZero: true)} × maximum breeders (max breeders = {generator.DisplaySummaryValueSnippet(model.MaximumBreedersKept, warnZero: true)}) equating to {generator.DisplaySummaryValueSnippet(model.MaximumBreedersKept * model.MaxBreedersMultiplierToStop, warnZero: true)} breeders.");

            // Initial herd adjustment
            if (model.AdjustBreedingFemalesAtStartup || model.AdjustBreedingMalesAtStartup)
            {
                string which = model.AdjustBreedingFemalesAtStartup && model.AdjustBreedingMalesAtStartup ? "females and males" :
                               model.AdjustBreedingFemalesAtStartup ? "females" : "males";
                generator.AddBlockWithText($"At startup this activity will adjust initial cohorts for {which} to achieve the maximum herd.");
            }

            generator.AddBlockWithText($"Breeding females");

            // does controlled mating exist in simulation
            var zone = ModelTyped.Structure.FindParent<Zone>(recurse: true);
            bool cmate = ModelTyped.Structure.FindChild<RuminantActivityControlledMating>(relativeTo: zone, recurse: true) != null;

            string output = "";
            using (generator.OpenBlock("activitycontentlight", id: $"{ModelTyped.Name}_fembreeders"))
            {
                if (ModelTyped.ManageFemaleBreederNumbers && (ModelTyped.PerformFemaleStocking | ModelTyped.PerformFemaleDestocking))
                {
                    double minimumBreedersKept = Math.Min(ModelTyped.MinimumBreedersKept, ModelTyped.MaximumBreedersKept);
                    int maxBreed = Math.Max(ModelTyped.MinimumBreedersKept, ModelTyped.MaximumBreedersKept);
                    output = "The herd will be maintained";
                    if (!ModelTyped.PerformFemaleStocking | (minimumBreedersKept == 0))
                        output += $" using only natural recruitment up to {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumBreedersKept)} breeders";
                    else
                    {
                        if (minimumBreedersKept == maxBreed)
                        {
                            output += $" with breeder purchases and natural recruitment up to {generator.DisplaySummaryValueSnippet(minimumBreedersKept)} breeders";
                        }
                        else
                        {
                            output += $" with breeder purchases up to {generator.DisplaySummaryValueSnippet(minimumBreedersKept)} and only natural recruitment to {generator.DisplaySummaryValueSnippet(maxBreed)} breeders";
                        }
                    }
                    if (!ModelTyped.PerformFemaleDestocking)
                    {
                        output += $" with {generator.DisplayBold("No")} destocking through sales";
                    }
                    generator.AddBlockWithText(output);

                    output = "";
                    if (ModelTyped.PerformFemaleDestocking)
                    {
                        if (ModelTyped.MarkOldBreedersForSale)
                        {
                            output += $"Individuals will be sold when over {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumBreederAge.InDays)} days old";
                            if (ModelTyped.ReturnPregnantMaxAgeToHerd)
                            {
                                output += " unless pregnant and the herd is below the required level";
                            }
                        }
                        else
                        {
                            output += $"Old breeders will {generator.DisplayBold("NOT")} be marked for sale{generator.DisplaySuperScript("*")}";
                            notBeingMarked.Add("Sell old female breeders");
                        }

                        generator.AddBlockWithText(output);
                    }

                    if (ModelTyped.PerformFemaleStocking)
                    {
                        if (ModelTyped.MaximumProportionBreedersPerPurchase < 1 & minimumBreedersKept > 0)
                        {
                            generator.AddBlockWithText($"A maximum of {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumProportionBreedersPerPurchase)} of the Minimum Breeders Kept equal to {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumProportionBreedersPerPurchase * minimumBreedersKept)} can be purchased in a single transaction");
                        }

                        output = $"Purchased breeders will be placed in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreNameBreeders, nullGeneralYards: true)} ";
                        if (ModelTyped.MinimumPastureBeforeRestock > 0)
                        {
                            output += $" with no restocking while pasture is below {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumPastureBeforeRestock)} kg/ha";
                        }
                        generator.AddBlockWithText(output);


                        if (!cmate && ModelTyped.GrazeFoodStoreNameBreeders != "" && ModelTyped.GrazeFoodStoreNameBreeders == ModelTyped.GrazeFoodStoreNameSires)
                        {
                            generator.AddBlockWithText($"Uncontrolled mating will occur as soon as Breeders and Sires are placed in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreNameBreeders, nullGeneralYards: true)}.", "infoBanner warning");
                        }
                    }
                    else
                    {
                        generator.AddBlockWithText($"This activity is {generator.DisplayBold("NOT")} stocking any breeding females");
                    }
                }
                else
                {
                    generator.AddBlockWithText($"This activity is {generator.DisplayBold("NOT")} currently managing breeding females");
                }
            }

            generator.AddBlockWithText($"Breeding males (sires/rams etc)");

            output = "";
            using (generator.OpenBlock("activitycontentlight", id: $"{ModelTyped.Name}_malbreeders"))
            {
                if (ModelTyped.ManageMaleBreederNumbers && (ModelTyped.PerformMaleStocking | ModelTyped.PerformMaleDestocking))
                {
                    if (ModelTyped.MaximumSiresKept == 0)
                    {
                        output = "No breeding sires will be kept";
                    }
                    else if (ModelTyped.MaximumSiresKept < 1)
                    {
                        output = $"The number of breeding males will be determined as {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumSiresKept)} of the maximum female breeder herd. Currently {generator.DisplaySummaryValueSnippet(Convert.ToInt32(Math.Ceiling(ModelTyped.MaximumBreedersKept * ModelTyped.MaximumSiresKept), CultureInfo.InvariantCulture))} individuals";
                    }
                    else
                    {
                        output = $"A maximum of {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumSiresKept)} will be maintained";
                    }
                    generator.AddBlockWithText( output);
                    output = "";
                    if (ModelTyped.PerformMaleDestocking)
                    {
                        if (ModelTyped.MarkOldSiresForSale)
                        {
                            output = $"Individuals will be sold when over {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumSireAge.InDays)} days old";
                        }
                        else
                        {
                            output = $"Old sires will {generator.DisplayBold("NOT")} be marked for sale{generator.DisplaySuperScript("*")}{((ModelTyped.MaximumSiresKept == 0) ? $" as maximum sires kept is {generator.DisplaySummaryValueSnippet(ModelTyped.MaximumSiresKept)}" : "")}";
                            notBeingMarked.Add("Sell old male breeders");
                        }
                        generator.AddBlockWithText( output);
                    }
                    else
                    {
                        generator.AddBlockWithText($"This activity is {generator.DisplayBold("NOT")} destocking any breeding males");
                    }

                    output = "";

                    if (ModelTyped.PerformMaleDestocking)
                    {
                        output = $"Purchased sires will be placed in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreNameSires, nullGeneralYards: true)} ";
                        if (ModelTyped.MinimumPastureBeforeRestock > 0)
                        {
                            output += $" with no restocking while pasture is below {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumPastureBeforeRestock)} kg/ha";
                        }
                        generator.AddBlockWithText(output);
                    }
                    else
                    {
                        generator.AddBlockWithText($"This activity is {generator.DisplayBold("NOT")} stocking any breeding males");
                    }
                }
                else
                {
                    generator.AddBlockWithText($"This activity is {generator.DisplayBold("NOT")} currently managing breeding males");
                }

            }


            generator.AddBlockWithText($"General herd");

            output = "";
            using (generator.OpenBlock("activitycontentlight", id: $"{ModelTyped.Name}_generalherd"))
            {
                if (ModelTyped.GrowOutYoungMales)
                {
                    generator.AddBlockWithText($"Young males will be managed for grow out");

                    if (!ModelTyped.PerformMaleDestocking | !ModelTyped.MarkAgeWeightMalesForSale)
                    {
                        generator.AddBlockWithText($"Grow out males are {generator.DisplayBold("NOT")} marked for sale");
                        notBeingMarked.Add("Sell grow out males reaching age or weight");
                    }
                    else
                    {
                        if (ModelTyped.MaleSellingAge.InDays + ((int)ModelTyped.MaleSellingWeight) > 0)
                        {
                            generator.AddBlockWithText($"Grow out males will be sold when {generator.DisplaySummaryValueSnippet(ModelTyped.MaleSellingAge.InDays)} days old or {generator.DisplaySummaryValueSnippet(ModelTyped.MaleSellingWeight)} kg");
                        }
                        else
                        {
                            generator.AddBlockWithText($"Grow out males will {generator.DisplayBold("NOT")} be marked for sale<sup>*</sup> {((ModelTyped.MaleSellingAge.InDays + ModelTyped.MaleSellingWeight > 0) ? " as no age or weight for sale has been defined" : "")}");
                            notBeingMarked.Add("Sell grow out males reaching age or weight");
                        }
                    }

                    generator.AddBlockWithText($"Grow-out males will be placed in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreNameGrowOutMales, nullGeneralYards: true)}");

                }
                else
                {
                    generator.AddBlockWithText($"Growing out and sale of young males is {generator.DisplayBold("NOT")} being performed by this activity");
                }

                if (ModelTyped.GrowOutYoungFemales)
                {
                    generator.AddBlockWithText($"Young females will be managed as grow out or a breeder reserve {((ModelTyped.GrowOutYoungMales) ? " with the grow out males" : "")}");

                    if (!ModelTyped.PerformFemaleDestocking | !ModelTyped.MarkAgeWeightFemalesForSale)
                    {
                        generator.AddBlockWithText($"Grow out females are {generator.DisplayBold("NOT")} marked for sale");
                        notBeingMarked.Add("Sell grow out females reaching age or weight");
                    }
                    else
                    {
                        if (ModelTyped.FemaleSellingAge.InDays + ModelTyped.FemaleSellingWeight > 0)
                        {
                            generator.AddBlockWithText($"Grow out females will be sold when {generator.DisplaySummaryValueSnippet(ModelTyped.FemaleSellingAge.InDays)} days old or {generator.DisplaySummaryValueSnippet(ModelTyped.FemaleSellingWeight)} kg");
                        }
                        else
                        {
                            generator.AddBlockWithText($"Grow out females will {generator.DisplayBold("NOT")} be marked for sale<sup>*</sup> {((ModelTyped.FemaleSellingAge.InDays + ModelTyped.FemaleSellingWeight > 0) ? " as no age or weight for sale has been defined" : "")}");
                            notBeingMarked.Add("Sell grow out females reaching age or weight");
                        }
                    }

                    generator.AddBlockWithText($"Grow-out females will be placed in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreNameGrowOutFemales, nullGeneralYards: true)}");

                    if (!cmate && ModelTyped.GrazeFoodStoreNameGrowOutFemales != "" & !ModelTyped.CastrateMales && ModelTyped.GrazeFoodStoreNameGrowOutFemales == ModelTyped.GrazeFoodStoreNameGrowOutMales)
                    {
                        generator.AddBlockWithText($"Uncontrolled mating may occur in grow out females and males if allowed to mature before sales as they are placed in {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.GrazeFoodStoreNameGrowOutFemales, nullGeneralYards: true)} if using natural mating.", "infoBanner warning");
                    }
                }
                else
                {
                    generator.AddBlockWithText($"Growing out and sale of young females is {generator.DisplayBold("NOT")} being performed by this activity");
                }


                if (ModelTyped.GrowOutYoungFemales | ModelTyped.GrowOutYoungMales)
                {
                    if (ModelTyped.ContinuousGrowOutSales)
                    {
                        generator.AddBlockWithText("Grow out age/weight sales will be performed in any month where conditions are met");
                    }
                    else
                    {
                        generator.AddBlockWithText("Grow out age/weight sales will only be performed when this activity is due");
                    }
                }

                if (ModelTyped.GrowOutYoungMales & ModelTyped.CastrateMales)
                {
                    generator.AddBlockWithText($"Young males will be castrated (e.g. create steers or bullocks)");
                }

            }

            if (notBeingMarked.Any())
            {
                output = "";
                foreach (var item in notBeingMarked)
                {
                    output += generator.DisplaySummaryValueSnippet(item, spanClass: "entryValue floatLeft warningValue" ); // "highlightdiv");
                }
                generator.AddBlockWithText($"* This activity is not performing all mark for sale tasks. The following tasks can be enabled or handled elsewhere: {output}", "clearfix infoBanner warning");
            }
        }
    }
}