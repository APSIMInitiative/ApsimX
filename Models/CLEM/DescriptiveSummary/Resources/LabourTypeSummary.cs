using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Resources;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for LabourType (sub-resource)
    /// </summary>
    public class LabourTypeSummary : DescriptiveSummaryProviderBase<LabourType>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            if (FormatForParentControl)
            {
                generator.AddTableRow(new List<(string, bool)>()
                {
                    (model.Name, false),
                    (CLEMModel.DisplaySummaryValueSnippet(model.Sex.ToString()), false),
                    (CLEMModel.DisplaySummaryValueSnippet(model.InitialAge, warnZero:true), false),
                    (CLEMModel.DisplaySummaryValueSnippet(model.Individuals, warnZero:true), false),
                    ("", model.IsHired)
                }, model.Enabled);
            }
            else
            {
                if (model.Individuals == 0)
                {
                    generator.AddBlockWithText("errorbanner", "No individuals are specified for this labour type");
                }
                else
                {
                    string number = $"{CLEMModel.DisplaySummaryValueSnippet(model.Individuals, warnZero: true)} x {CLEMModel.DisplaySummaryValueSnippet(model.InitialAge, warnZero: true)} year old {CLEMModel.DisplaySummaryValueSnippet(model.Sex)}";
                    if (model.IsHired)
                    {
                        number += " as hired labour";
                    }
                    generator.AddBlockWithText("activityentry", number);
                }

                if (model.Individuals > 1)
                {
                    generator.AddBlockWithText("warningbanner", $"You will be unable to identify these individuals with <span class=\"setvalue\">Name</span> but need to use the Attribute with tag <span class=\"setvalue\">Group</span> and value <span class=\"setvalue\">{model.Name}</span></div>");
                }
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryOpeningBlocks()
        {
            if (!FormatForParentControl)
                base.CreateSummaryOpeningBlocks();
        }

        /// <inheritdoc/>
        public override void CreateSummaryClosingBlocks()
        {
            if (!FormatForParentControl)
                base.CreateSummaryClosingBlocks();
        }

    }
}