using System;

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
        /// <returns></returns>
        string ModelSummary(bool formatForParentControl);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        string GetFullSummary(object model, bool formatForParentControl, string htmlString);

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
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryNameTypeHeader();
    }
}
