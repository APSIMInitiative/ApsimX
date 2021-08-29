using System;
using Models.Core;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for models with CLEM Descriptive Summary.
    /// </summary>
    public interface ICLEMDescriptiveSummary
    {
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns>Html formatted description</returns>
        string ModelSummary(bool formatForParentControl);

        /// <summary>
        /// Method to create the full descriptive summary for a model and all ancestors
        /// </summary>
        /// <param name="model">The model providing the summary</param>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <param name="htmlString">Initial string to append to</param>
        /// <param name="markdown2Html">Method to convert markdown memos to html</param>
        /// <returns>Summary description HTML text</returns>
        string GetFullSummary(IModel model, bool formatForParentControl, string htmlString, Func<string, string> markdown2Html = null);

        /// <summary>
        /// Styling to use for HTML summary
        /// </summary>
        HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryClosingTags(bool formatForParentControl);

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryOpeningTags(bool formatForParentControl);

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryInnerClosingTags(bool formatForParentControl);

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryInnerOpeningTags(bool formatForParentControl);

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryInnerOpeningTagsBeforeSummary();

        /// <summary>
        /// Generates the header for description
        /// </summary>
        /// <returns>HTML of header</returns>
        string ModelSummaryNameTypeHeader();

        /// <summary>
        /// Provide the text to place in the model summary header row
        /// </summary>
        /// <returns>header text</returns>
        string ModelSummaryNameTypeHeaderText();
    }
}
