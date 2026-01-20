using BruTile.Wms;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Generic provider base. Default CreateSummary calls GetSummary(TModel).
/// Providers can override CreateSummary or GetSummary as needed.
/// </summary>
public abstract class RuminantParametersSummaryBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <summary>
    /// constructor that allows resolver to inject the model at construction time 
    /// </summary>
    /// <param name="model">The model for this provider</param>
    public RuminantParametersSummaryBase(TModel model)
    {
        // call base helper to store model
        base.SetModel(model);
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public RuminantParametersSummaryBase() { }

    /// <summary>
    /// Strongly-typed accessor for the model that the provider is summarising.
    /// Performs a checked cast and throws a clear exception if the stored Model is not the expected type.
    /// </summary>
    protected TModel ModelTyped
    {
        get
        {
            if (Model is TModel typed)
                return typed;

            string actual = Model?.GetType().FullName ?? "null";
            throw new InvalidOperationException($"DescriptiveSummaryProviderBase<{typeof(TModel).FullName}>: stored Model is not of the expected type. Actual: {actual}");
        }
    }

    /// <summary>
    /// Method to get the summary parameters for a ruminant parameter type
    /// </summary>
    /// <returns>The list of parameters with component name and category for display</returns>
    public virtual List<(string ComponentName, string Category, string Value)> GetSummaryParameters()
    {
        var model = ModelTyped;
        if (model is null) return new List<(string, string, string)>();
        var summary = new List<(string, string, string)>();
        return summary;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        if (!FormatForParentControl)
            return;

        foreach (var param in GetSummaryParameters().OrderBy(a => a.Category))
        {
            generator.AddSummaryParameterSnippet(param.Category, param.Value);
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
    {
        if (!FormatForParentControl)
        {
            string PropertyType = typeof(TModel).Name.Replace("RuminantParameters", "");
            Generator.AddBlockWithText("detailsnote", $"{PropertyType} parameters used by multiple activities and growth components.");
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
