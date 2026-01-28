using DocumentFormat.OpenXml.Vml.Spreadsheet;
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
public abstract class TimerSummaryBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <summary>
    /// constructor that allows resolver to inject the model at construction time 
    /// </summary>
    /// <param name="model">The model for this provider</param>
    public TimerSummaryBase(TModel model)
    {
        // call base helper to store model
        base.SetModel(model);
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public TimerSummaryBase() { }

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

    /// <inheritdoc/>
    public override void BuildSummary()
    {

    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        string name = "";
        if (!Model.Name.Contains(GetType().Name.Split('.').Last()))
        {
            name = Model.Name;
        }
        generator.AddBlockWithText(name, "childTitle filter", disabled: !Model.Enabled);
        generator.OpenBlock("childgroupborder filteritems clearfix", "", id: "timerdetails");
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        generator.CloseMostRecentBlock("timerdetails");
    }


    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        // close after buildSummary to allow memos etc to be reported below.
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
    }

}
