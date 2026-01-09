using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Generic provider base. Default CreateSummary calls GetSummary(TModel).
    /// Providers can override CreateSummary or GetSummary as needed.
    /// </summary>
    public abstract class DescriptiveSummaryProviderBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
        where TModel : IModel
    {
        /// <summary>
        /// Override the non-generic entry so virtual dispatch from DescriptiveSummaryBase/IDescriptiveSummaryProvider will reach this implementation and then forward to the typed overload.
        /// </summary>
        /// <param name="model">Model for which to generate summary</param>
        /// <returns></returns>
        public override void BuildSummary(IModel model)
        {
            if (model is TModel typed)
            {
                BuildSummary(typed);
                return;
            }

            // If the model is not the expected type, fall back to base behaviour.
            base.BuildSummary(model);
        }

        /// <summary>
        /// Typed virtual method for concrete providers to override. 
        /// </summary>
        /// <param name="model">Model for which to generate summary</param>
        /// <returns></returns>
        public virtual void BuildSummary(TModel model)
        {
            // Default behaviour: fall back to the non-generic base implementation.
            base.BuildSummary(model);
        }
    }
}