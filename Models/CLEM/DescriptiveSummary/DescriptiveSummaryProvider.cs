using DocumentFormat.OpenXml.Wordprocessing;
using Mapsui.Providers.Wfs.Utilities;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Provides an abstract base class for generating descriptive summaries of models,  including support for creating and
/// managing summary blocks and formatting options.
/// </summary>
/// <remarks>This class defines the core functionality for building descriptive summaries,  including methods for
/// opening and closing summary blocks, generating headers,  and formatting summaries based on the provided model. It is
/// designed to be  extended by derived classes to customize the summary generation process.</remarks>
public abstract class DescriptiveSummaryProvider : IDescriptiveSummaryProvider
{
    /// <inheritdoc/>
    protected DescriptiveSummaryGenerator generator = null!;
    /// <inheritdoc/>
    protected readonly List<string> openingBlocks = new List<string>();
    /// <inheritdoc/>
    protected readonly List<string> innerBlocks = new List<string>();

    /// <summary>
    /// The model instance the provider is currently summarising. Set by the resolver or when the
    /// interface entry points are invoked.
    /// </summary>
    public IModel Model { get; set; }

    /// <summary>
    /// Convenience accessor when the model is a CLEMModel.
    /// </summary>
    public CLEMModel CLEMModel => Model as CLEMModel;

    /// <summary>
    /// Allow external code (or resolver) to set the model explicitly.
    /// </summary>
    /// <param name="model">Model instance</param>
    public virtual void SetModel(IModel model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        CurrentAncestorList ??= new List<string>();
    }

    /// <summary>
    /// Method to set the current summary generator for the provider to use.
    /// </summary>
    /// <param name="generator">Descriptive summary generator to use</param>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual void SetGenerator(DescriptiveSummaryGenerator generator)
    {
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        openingBlocks.Clear();
        innerBlocks.Clear();
        CurrentAncestorList ??= new List<string>();
    }

    /// <inheritdoc/>
    protected DescriptiveSummaryGenerator Generator
    {
        get
        {
            if (generator == null) throw new InvalidOperationException("DescriptiveSummaryGenerator has not been set for this provider.");
            return generator;
        }
    }

    /// <inheritdoc/>
    public HTMLSummaryStyle SummaryStyle { get; set; } = HTMLSummaryStyle.Default;
    /// <inheritdoc/>
    public List<string> CurrentAncestorList { get; set; } = new();

    /// <inheritdoc/>
    public int NestedLevel { get; set; } = 0;

    /// <inheritdoc/>
    public bool FormatForParentControl => NestedLevel > 1; // generator.IndentIndex > 0; //CurrentAncestorList?.Count > 0;
    /// <inheritdoc/>
    public DescriptiveSummaryMemoReportingType ReportMemosType { get; set; } = DescriptiveSummaryMemoReportingType.InPlace;

    /// <inheritdoc/>
    public virtual void BuildSummary()
    {
        if (Model is ResourceBaseWithTransactions)
            Generator.AddBlockWithText("activityentry", $"No details for {CLEMModel.DisplaySummaryResourceTypeSnippet(Model.GetType().Name)}.");
        else
            Generator.AddBlockWithText("activityentry", $"No details for {Model.GetType().Name}.");
    }

    /// <inheritdoc/>
    public virtual void CreateSummaryClosingBlocks()
    {
        foreach (string block in openingBlocks.ToArray().Reverse())
            Generator.CloseMostRecentBlock(id: block);
        openingBlocks.Clear();
    }

    /// <inheritdoc/>
    public virtual void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        foreach (string block in innerBlocks.ToArray().Reverse())
            Generator.CloseMostRecentBlock(id: block);
        innerBlocks.Clear();
    }

    /// <inheritdoc/>
    public virtual void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
    }

    /// <inheritdoc/>
    public virtual void CreateSummaryInnerOpeningBlocksBeforeSummary() 
    {
        var cm = CLEMModel;
        if (cm is null) return;

        if (cm.GetType().IsSubclassOf(typeof(CLEMResourceTypeBase)))
        {
            // add units when completed
            string units = (cm as IResourceType).Units;
            if (units != "NA")
            {
                Generator.AddBlockWithText("activityentry", $"This resource is measured in {CLEMModel.DisplaySummaryValueSnippet(units)}");
            }

        }

        if (this.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
        {
            if (cm.Children.Count == 0)
            {
                Generator.AddBlockWithText("activityentry", $"Empty");
            }
        }
    }

    /// <inheritdoc/>
    public virtual void GetSummaryNameTypeHeader(bool disabled = false)
    {
        // copy your header logic here (unchanged except using Generator instead of generator)
        Generator.AddBlockWithText("namediv", $"{GetSummaryNameTypeHeaderText()} {((disabled) ? " - DISABLED!" : "")}");
        Generator.AddLineBreak();
        Generator.AddBlockWithText("typediv", Model.GetType().Name);

        var cm = CLEMModel;
        if (cm is null) return;

        if (Model is CLEMActivityBase cmab)
        {
            //string tooltip = "";
            string divText = "";

            switch (cmab.OnPartialResourcesAvailableAction)
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

            Generator.AddBlockWithText("partialdiv", divText);

            string transCat = CLEMActivityBase.UpdateTransactionCategory(cmab, cm.Structure);
            if (transCat != "")
            {
                Generator.AddBlockWithText("partialdiv", $"tag: {transCat}");
            }
        }
    }

    /// <inheritdoc/>
    public virtual string GetSummaryNameTypeHeaderText()
    {
        return Model is null ? "" : $"{Model.Name}";
    }

    /// <inheritdoc/>
    public virtual void CreateSummaryOpeningBlocks()
    {
        string overall = "activity";
        string extra = "";

        if (SummaryStyle == HTMLSummaryStyle.Default)
        {
            if (Model is Relationship || this.GetType().IsSubclassOf(typeof(Relationship)))
                SummaryStyle = HTMLSummaryStyle.Default;
            else if (Model.GetType().IsSubclassOf(typeof(ResourceBaseWithTransactions)))
                SummaryStyle = HTMLSummaryStyle.Resource;
            else if (typeof(IResourceType).IsAssignableFrom(Model.GetType()))
                SummaryStyle = HTMLSummaryStyle.SubResource;
            else if (typeof(ISubParameters).IsAssignableFrom(Model.GetType()))
                SummaryStyle = HTMLSummaryStyle.SubResource;
            else if (typeof(IConceptionModel).IsAssignableFrom(Model.GetType()))
                SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
            else if (Model.GetType().IsSubclassOf(typeof(CLEMActivityBase)))
                SummaryStyle = HTMLSummaryStyle.Activity;
        }

        switch (SummaryStyle)
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

        bool firstDisabledBlock = !Model.Enabled && generator.CurrentlyDisabled == false;

        generator.OpenBlock($"holder{((extra == "") ? "main" : "sub")} {overall} {(firstDisabledBlock ? "disabledcomponent" : "")}", id: $"{Model.Name}_opening", disabled: !Model.Enabled);
        openingBlocks.Add($"{Model.Name}_opening");
        using (generator.OpenBlock($"clearfix {overall}banner{extra}"))
        {
            GetSummaryNameTypeHeader(firstDisabledBlock);
        }
        generator.OpenBlock($"{overall}content{((extra != "") ? extra : "")}", id: $"{Model.Name}_content");
        openingBlocks.Add($"{Model.Name}_content");
    }

    /// <summary>
    /// Provide a list of child types to include or ignore from summary for the given model
    /// </summary>
    /// <returns>List of child model groups to handle</returns>
    public virtual List<ChildComponentGroup> GetChildrenInSummary()
    {
        return [];
    }

    /// <summary>
    /// Provide a list of child types to include or ignore from summary for the given model
    /// </summary>
    /// <returns>Full list of child model groups to handle</returns>
    public virtual IEnumerable<ChildComponentGroup> HandleChildrenInSummary()
    {
        var modelsToSummarise = GetChildrenInSummary();

        // Build a materialized set of all models already included in modelsToSummarise
        var uniqueModels = new HashSet<IModel>(
            modelsToSummarise
                .SelectMany(entry => entry.SelectedModels ?? [])
            );

        Model.Node.FindChildren<IModel>();
        
        //// Find all child models and only add those not already present in uniqueModels
        //var remainingChildren = Model.Structure.FindChildren<IModel>()
        //                          .Where(child => !uniqueModels.Contains(child))
        //                          .ToList();

        // Find all child models and only add those not already present in uniqueModels
        var remainingChildren = Model.Node.FindChildren<IModel>()
                                  .Where(child => !uniqueModels.Contains(child))
                                  .ToList();


        if (remainingChildren.Count > 0)
            modelsToSummarise.Add(new ChildComponentGroup("others", remainingChildren));

        return modelsToSummarise;
    }

    /// <summary>
    /// Returns the opacity value for this component in the summary display
    /// </summary>
    public double SummaryOpacity()
    {
        return ((!Model.Enabled & (!FormatForParentControl | (FormatForParentControl & Model.Parent.Enabled))) ? 0.4 : 1.0);
    }
}