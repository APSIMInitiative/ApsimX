using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary base class for CLEM models
/// </summary>
public class DescriptiveSummaryBase : IDescriptiveSummaryProvider
{
    private DescriptiveSummaryGenerator generator;
    private readonly List<string> openingBlocks = [];
    private readonly List<string> innerBlocks = [];

    /// <summary>
    /// Set the current string builder for the provider to use
    /// </summary>
    /// <param name="generator"></param>
    public void SetGenerator(DescriptiveSummaryGenerator generator)
    {
        this.generator = generator;
    }

    /// <summary>
    /// Provide the current string builder for the provider to use
    /// </summary>
    /// <returns></returns>
    public DescriptiveSummaryGenerator Generator
    {
        get
        {
            if (generator == null)
                throw new InvalidOperationException("DescriptiveSummaryGenerator has not been set for this provider.");
            return generator;
        }
    }

    /// <inheritdoc/>
    public HTMLSummaryStyle SummaryStyle { get; set; } = HTMLSummaryStyle.Default;

    /// <inheritdoc/>
    public List<string> CurrentAncestorList { get; set; } = [];

    /// <inheritdoc/>
    public bool FormatForParentControl => CurrentAncestorList?.Count > 0;

    /// <inheritdoc/>
    public DescriptiveSummaryMemoReportingType ReportMemosType { get; set; } = DescriptiveSummaryMemoReportingType.InPlace;

    /// <inheritdoc/>
    public virtual void BuildSummary(IModel model)
    {
        generator.AddBlockWithText("activityentry", $"No details for [{model.GetType().Name}].");
    }

    /// <inheritdoc/>
    public void CreateSummaryClosingBlocks()
    {
        foreach (string block in openingBlocks.ToArray().Reverse())
        {
            generator.CloseMostRecentBlock(id: block);
        }
        openingBlocks.Clear();
    }

    /// <inheritdoc/>
    public void CreateSummaryInnerClosingBlocks()
    {
        foreach (string block in innerBlocks.ToArray().Reverse())
        {
            generator.CloseMostRecentBlock(id: block);
        }
        innerBlocks.Clear();
    }

    /// <inheritdoc/>
    public void CreateSummaryInnerOpeningBlocks()
    {
    }

    /// <inheritdoc/>
    public void CreateSummaryInnerOpeningBlocksBeforeSummary()
    {
    }

    /// <inheritdoc/>
    public void GetSummaryNameTypeHeader(CLEMModel cm)
    {
        generator.AddBlockWithText("namediv", $"{GetSummaryNameTypeHeaderText(cm)} {((!cm.Enabled) ? " - DISABLED!" : "")}");
        generator.AddLineBreak();
        generator.AddBlockWithText("typediv", cm.GetType().Name);

        if (cm.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
        {
            //string tooltip = "";
            string divText = "";

            switch ((cm as CLEMActivityBase).OnPartialResourcesAvailableAction)
            {
                case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                    //tooltip = "Error and Stop on insufficient resources";
                    divText = "Stop";
                    break;
                case OnPartialResourcesAvailableActionTypes.SkipActivity:
                    divText = "Skip";
                    break;
                case OnPartialResourcesAvailableActionTypes.UseAvailableResources:
                    divText = "Partial";
                    break;
                case OnPartialResourcesAvailableActionTypes.UseAvailableWithImplications:
                    divText = "Impact";
                    break;
                default:
                    break;
            }

            generator.AddBlockWithText("partialdiv", divText);

            if (cm is CLEMActivityBase cmab)
            {
                string transCat = CLEMActivityBase.UpdateTransactionCategory(cmab, cm.Structure);
                if (transCat != "")
                {
                    generator.AddBlockWithText("partialdiv", $"tag: {transCat}");
                }
            }

        }
    }

    /// <inheritdoc/>
    public string GetSummaryNameTypeHeaderText(CLEMModel cm)
    {
        return cm.Name; 
    }

    /// <inheritdoc/>
    public virtual void CreateSummaryOpeningBlocks(CLEMModel cm)
    {
        string overall = "activity";
        string extra = "";

        if (cm.ModelSummaryStyle == HTMLSummaryStyle.Default)
        {
            if (cm is Relationship || this.GetType().IsSubclassOf(typeof(Relationship)))
                cm.ModelSummaryStyle = HTMLSummaryStyle.Default;
            else if (cm.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                cm.ModelSummaryStyle = HTMLSummaryStyle.Resource;
            else if (typeof(IResourceType).IsAssignableFrom(cm.GetType()))
                cm.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
            else if (cm.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                cm.ModelSummaryStyle = HTMLSummaryStyle.Activity;
        }

        switch (cm.ModelSummaryStyle)
        {
            case HTMLSummaryStyle.Default:
                overall = "default";
                break;
            case HTMLSummaryStyle.Resource:
                overall = "resource";
                break;
            case HTMLSummaryStyle.SubResource:
                overall = "resource";
                extra = "light";
                break;
            case HTMLSummaryStyle.SubResourceLevel2:
                overall = "resource";
                extra = "dark";
                break;
            case HTMLSummaryStyle.Activity:
                break;
            case HTMLSummaryStyle.SubActivity:
                extra = "light";
                break;
            case HTMLSummaryStyle.Helper:
                break;
            case HTMLSummaryStyle.SubActivityLevel2:
                extra = "dark";
                break;
            case HTMLSummaryStyle.FileReader:
                overall = "file";
                break;
            case HTMLSummaryStyle.Filter:
                overall = "filter";
                break;
            default:
                break;
        }

        generator.OpenBlock($"holder{((extra == "") ? "main" : "sub")} {overall}", styleString: $"opacity: {cm.SummaryOpacity(FormatForParentControl)};", id: $"{cm.Name}_opening");
        openingBlocks.Add($"{cm.Name}_opening");
        using (generator.OpenBlock($"clearfix {overall}banner{extra}"))
        {
            GetSummaryNameTypeHeader(cm);
        }
        generator.OpenBlock($"{overall}content{((extra != "") ? extra : "")}", id: $"{cm.Name}_content");
        openingBlocks.Add($"{cm.Name}_content");
    }
}
