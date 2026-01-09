using Models.CLEM.Interfaces;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Generic descriptive summary provider base class
/// </summary>
/// <typeparam name="TModel"></typeparam>
public class DescriptiveSummaryProvider<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <inheritdoc/>
    public virtual void BuildSummary(TModel model)
    {
        base.BuildSummary(model); // default fallback
    }

    /// <inheritdoc/>
    public override void BuildSummary(IModel model)
    {
        if (model is TModel tm) BuildSummary(tm);
        else base.BuildSummary(model);
    }


    /// <inheritdoc/>
    public virtual void CreateSummaryOpeningBlocks(TModel model)
    {
        base.CreateSummaryOpeningBlocks(model as CLEMModel); // default fallback
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks(CLEMModel model)
    {
        if (model is TModel tm) CreateSummaryOpeningBlocks(tm);
        else base.CreateSummaryOpeningBlocks(model as CLEMModel);
    }

}