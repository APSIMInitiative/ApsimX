using Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        /// <returns>Html formatted description</returns>
        string ModelSummary();

        /// <summary>
        /// Method to create the full descriptive summary for a model and all ancestors
        /// </summary>
        /// <param name="model">The model providing the summary</param>
        /// <param name="parentControlList">history of parent controls for description style</param>
        /// <param name="htmlString">Initial string to append to</param>
        /// <param name="markdown2Html">Method to convert markdown memos to html</param>
        /// <returns>Summary description HTML text</returns>
        string GetFullSummary(IModel model, List<string> parentControlList, string htmlString, Func<string, string> markdown2Html = null);

        /// <summary>
        /// Styling to use for HTML summary
        /// </summary>
        HTMLSummaryStyle ModelSummaryStyle { get; set; }

        /// <summary>
        /// List of parent model types before this 
        /// </summary>
        [JsonIgnore]
        List<string> CurrentAncestorList { get; set; }

        /// <summary>
        /// Determines if this discription is below a parent model
        /// </summary>
        bool FormatForParentControl { get; }

        /// <summary>
        /// Determines if this model reports memos in place
        /// </summary>
        DescriptiveSummaryMemoReportingType ReportMemosType { get; set; }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryClosingTags();

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryOpeningTags();

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryInnerClosingTags();

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        string ModelSummaryInnerOpeningTags();

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
