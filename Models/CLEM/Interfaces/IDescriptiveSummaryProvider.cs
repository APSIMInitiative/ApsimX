using DocumentFormat.OpenXml.EMMA;
using Models.CLEM.DescriptiveSummary;
using Models.Core;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models.CLEM.Interfaces;

/// <summary>
/// The interface for methods and properties used to generate descriptive summaries for CLEM models.
/// </summary>
public interface IDescriptiveSummaryProvider
{
    /// <summary>
    /// Styling to use for HTML summary
    /// </summary>
    HTMLSummaryStyle SummaryStyle { get; set; }

    /// <summary>
    /// List of parent model types before this 
    /// </summary>
    [JsonIgnore]
    List<string> CurrentAncestorList { get; set; }

    /// <summary>
    /// Determines if this description is below a parent model
    /// </summary>
    bool FormatForParentControl { get; }

    /// <summary>
    /// Determines if this model reports memos in place
    /// </summary>
    DescriptiveSummaryMemoReportingType ReportMemosType { get; set; }

    /// <summary>
    /// Provides the closing blocks for summary
    /// </summary>
    void CreateSummaryClosingBlocks();

    /// <summary>
    /// Provides the closing blocks for summary
    /// </summary>
    void CreateSummaryOpeningBlocks(CLEMModel cm);

    /// <summary>
    /// Provides the closing inner  blocks for summary
    /// </summary>
    void CreateSummaryInnerClosingBlocks();

    /// <summary>
    /// Provides the inner opening blocks for summary
    /// </summary>
    void CreateSummaryInnerOpeningBlocks();

    /// <summary>
    /// Provides any blocks to occur prior to the summary but inside opening blocks
    /// </summary>
    void CreateSummaryInnerOpeningBlocksBeforeSummary();

    /// <summary>
    /// Generates the header blocks for the summary
    /// </summary>
    void GetSummaryNameTypeHeader(CLEMModel cm);

    /// <summary>
    /// Provide the text to place in the model summary header row
    /// </summary>
    string GetSummaryNameTypeHeaderText(CLEMModel cm);

    /// <summary>
    /// Generates a summary description based on the provided model.
    /// </summary>
    /// <param name="model">The model used to generate the summary. Must not be null.</param>
    void BuildSummary(IModel model);

    /// <summary>
    /// Set the descriptive summary generator for the provider to use
    /// </summary>
    void SetGenerator(DescriptiveSummaryGenerator generator);
}


/// <summary>
/// Implement this to provide a custom descriptive summary for a specific model type.
/// </summary>
public interface IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <inheritdoc/>
    void BuildSummary(TModel model); 
}