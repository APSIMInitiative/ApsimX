using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Groupings
{
    /// <summary>
    /// Generic provider base for filters
    /// </summary>
    public abstract class FilterSummaryBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>
        where TModel : IModel
    {
        /// <summary>
        /// constructor that allows resolver to inject the model at construction time 
        /// </summary>
        /// <param name="model">The model for this provider</param>
        public FilterSummaryBase(TModel model)
        {
            // call base helper to store model
            base.SetModel(model);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterSummaryBase() { }


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
        /// Convert sort to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FilterString(false);
        }

        /// <summary>
        /// Convert filter to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            return FilterString(true);
        }

        /// <summary>
        /// Method to convert a filter into html snippet
        /// </summary>
        /// <returns></returns>
        public virtual string FilterString(bool html)
        {
            return $"UNKNOWN";
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText("filter", FilterString(generator.OutputFormat == DescriptiveSummaryFormat.HTML), styleString: ((CLEMModel.Enabled) ? "" : "disabled"));
        }

        /// <inheritdoc/>
        public override void CreateSummaryClosingBlocks()
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryOpeningBlocks()
        {
        }


    }
}
