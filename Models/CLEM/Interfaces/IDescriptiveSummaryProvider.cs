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
    /// Provides the nested level of the associated component
    /// </summary>
    int NestedLevel { get; set; }

    /// <summary>
    /// A switch to determine if child models are contained in the bounds of model border
    /// </summary>
    public bool WrapChildren { get; set; }

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
    void CreateSummaryOpeningBlocks();

    /// <summary>
    /// Provides the closing inner  blocks for summary
    /// </summary>
    void CreateSummaryInnerClosingBlocks(ChildComponentGroup group);

    /// <summary>
    /// Provides the inner opening blocks for summary
    /// </summary>
    void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group);

    /// <summary>
    /// Provides any blocks to occur prior to the summary but inside opening blocks
    /// </summary>
    void CreateSummaryInnerOpeningBlocksBeforeSummary();

    /// <summary>
    /// Generates the header blocks for the summary
    /// </summary>
    void GetSummaryNameTypeHeader(bool disabled);

    /// <summary>
    /// Provide the text to place in the model summary header row
    /// </summary>
    string GetSummaryNameTypeHeaderText();

    /// <summary>
    /// Generates a summary description based on the provided model.
    /// </summary>
    void BuildSummary();

    /// <summary>
    /// Set the descriptive summary generator for the provider to use
    /// </summary>
    void SetGenerator(DescriptiveSummaryGenerator generator);

    /// <summary>
    /// Provide the grouped lists of all Children components to summarise
    /// </summary>
    /// <returns>A IEnumerable of the models, include, border class name, intro text and missing text for each type of component reported.</returns>
    List<ChildComponentGroup> GetChildrenInSummary();

    /// <summary>
    /// Update the list of child types to include or ignore from summary for the given model
    /// </summary>
    IEnumerable<ChildComponentGroup> HandleChildrenInSummary();

    /// <summary>
    /// Determine the opacity level to apply for summary display based on enabled state
    /// </summary>
    /// <returns>Opacity level for html classes</returns>
    double SummaryOpacity();

    /// <summary>
    /// The model that this provider is summarising.
    /// </summary>
    IModel Model { get; set; }
}


/// <summary>
/// Implement this to provide a custom descriptive summary for a specific model type.
/// </summary>
public interface IDescriptiveSummaryProvider<TModel>
    where TModel : IModel
{
    /// <inheritdoc/>
    void BuildSummary();
}

/// <summary>
/// Implement this to provide a custom descriptive summary for a specific model type.
/// </summary>
public interface IRuminantParameterSummaryProvider
{
    /// <summary>
    /// Method to get the summary parameters for a ruminant parameter type
    /// </summary>
    /// <returns>The list of parameters with component name and category for display</returns>
    List<(string componentName, string propertyName, string category, string description, string value)> GetSummaryParameters();

    /// <summary>
    /// Determines if the parameter component is needed in the simulation
    /// </summary>
    /// <returns>True if the component is needed, false otherwise</returns>
    bool IsNeeded();
}