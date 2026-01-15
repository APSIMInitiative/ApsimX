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
public abstract class GroupSummaryBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <summary>
    /// constructor that allows resolver to inject the model at construction time 
    /// </summary>
    /// <param name="model">The model for this provider</param>
    public GroupSummaryBase(TModel model)
    {
        // call base helper to store model
        base.SetModel(model);
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public GroupSummaryBase() { }

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

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "default",
                models: ModelTyped.Node.FindChildren<IModel>().Where(a => a.GetType() != typeof(Memo)),
                missing: "",
                borderClass: "filterborder clearfix"
                )
        ];
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
        if (ModelTyped is IActivityCompanionModel acm && (acm.Identifier ?? "") != "")
        {
            name += $" - applies to {acm.Identifier}";
        }
        generator.AddBlockWithText("filtername", name, disabled: !Model.Enabled);
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
    }


    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        generator.OpenBlock("filterborder clearfix", "", id: "groupitems");
        if (group.SelectedModels.Any() == false)
        {
            generator.AddBlockWithText("filter", "All individuals");
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        generator.CloseMostRecentBlock("groupitems");
    }

}
