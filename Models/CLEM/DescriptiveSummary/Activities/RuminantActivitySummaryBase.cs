using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Generic provider base for ruminant activities
/// </summary>
public abstract class RuminantActivitySummaryBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <summary>
    /// constructor that allows resolver to inject the model at construction time 
    /// </summary>
    /// <param name="model">The model for this provider</param>
    public RuminantActivitySummaryBase(TModel model)
    {
        // call base helper to store model
        base.SetModel(model);
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public RuminantActivitySummaryBase() { }

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
                model: CLEMModel,
                childType: typeof(RuminantGroup),
                borderClass: "childgroupfilterborder",
                introduction: "Individuals will be selected from the following:"
                )
        ];
    }
}
