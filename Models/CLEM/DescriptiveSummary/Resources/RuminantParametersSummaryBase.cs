using BruTile.Wms;
using ExCSS;
using Models.CLEM.Interfaces;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Generic provider base. Default CreateSummary calls GetSummary(TModel).
/// Providers can override CreateSummary or GetSummary as needed.
/// </summary>
public abstract class RuminantParametersSummaryBase<TModel> : DescriptiveSummaryProvider, IDescriptiveSummaryProvider<TModel>, IRuminantParameterSummaryProvider
    where TModel : IModel
{
    /// <summary>
    /// constructor that allows resolver to inject the model at construction time 
    /// </summary>
    /// <param name="model">The model for this provider</param>
    public RuminantParametersSummaryBase(TModel model)
    {
        // call base helper to store model
        base.SetModel(model);
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public RuminantParametersSummaryBase() { }

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
    /// Method to get the summary parameters for a ruminant parameter type
    /// </summary>
    /// <returns>The list of parameters with component name and category for display</returns>
    public virtual List<(string componentName, string propertyName, string category, string description, string value)> GetSummaryParameters()
    {
        var summaryParams = GetParametersByAttributeDetails();

        var summaryToRemove = SummaryParametersToRemove();
        summaryParams.RemoveAll(a => summaryToRemove.Any(b => b == a.propertyName));

        var customSummaryParams = GetCustomSummaryParameters();
        summaryParams.RemoveAll(a => customSummaryParams.Any(b => b.propertyName == a.propertyName));
        summaryParams.InsertRange(0, customSummaryParams);

        return summaryParams;
    }

    /// <summary>
    /// Method to get the summary parameters for a ruminant parameter type
    /// </summary>
    /// <returns>The list of parameters with component name and category for display</returns>
    public virtual List<string> SummaryParametersToRemove()
    {
        return [];
    }

    /// <summary>
    /// Method to get the summary parameters for a ruminant parameter type
    /// </summary>
    /// <returns>The list of parameters with component name and category for display</returns>
    public virtual List<(string componentName, string propertyName, string category, string description, string value)> GetCustomSummaryParameters()
    {
        return GetParametersByAttributeDetails();
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        if (!FormatForParentControl)
            return;

        foreach (var param in GetSummaryParameters().OrderBy(a => a.category))
        {
            generator.AddSummaryParameterSnippet(param.category, param.value);
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
    {
        if (!FormatForParentControl)
        {
            string PropertyType = typeof(TModel).Name.Replace("RuminantParameters", "");
            Generator.AddBlockWithText("detailsnote", $"{PropertyType} parameters used by multiple activities and growth components.");
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryOpeningBlocks();
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryClosingBlocks();
    }

    /// <summary>
    /// Get parameters to display based on Description and Category attributes of the property.
    /// </summary>
    /// <returns>A list of summary parameters with description, subcategory, and value</returns>
    private List<(string componentName, string propertyName, string category, string description, string value)> GetParametersByAttributeDetails()
    {
        var model = ModelTyped;
        if (model is null)
            return [];

        var summary = new List<(string, string, string, string, string)>();
        string subToMatch = typeof(TModel).Name.Replace("RuminantParameters", "", StringComparison.OrdinalIgnoreCase);

        var properties = model.GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var prop in properties)
        {
            DescriptionAttribute desAtt = prop.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault() as DescriptionAttribute;
            if (desAtt is null)
                continue;

            string desc = desAtt.ToString();

            CategoryAttribute catAtt = prop.GetCustomAttributes(typeof(CategoryAttribute), true).FirstOrDefault() as CategoryAttribute;
            List<string> validCategories = ["Summary"];
            bool correctLevel = validCategories.Any(a => catAtt.Category.Contains(a)); // catAtt.Category.Contains("Farm");
            if (!correctLevel) continue;

            string subcat = catAtt.Subcategory;

            // Get property value and format it
            object valueObj = null;
            try
            {
                valueObj = prop.GetValue(model);
            }
            catch
            {
                valueObj = null;
            }

            string valueStr;
            if (valueObj == null)
            {
                valueStr = "Not set";
            }
            else if (valueObj is string s)
            {
                valueStr = s;
            }
            else if (valueObj is System.Collections.IEnumerable enumerable && !(valueObj is System.Collections.IDictionary))
            {
                var parts = new List<string>();
                foreach (var item in enumerable)
                    parts.Add(item?.ToString() ?? "null");
                valueStr = string.Join(", ", parts);
            }
            else
            {
                valueStr = valueObj.ToString();
            }

            summary.Add((ModelTyped.GetType().Name.Replace("RuminantParameters", ""), prop.Name, catAtt.Subcategory, desc, generator.DisplaySummaryValueSnippet(valueStr, warnZero: true, errorNotSet: true)));
        }
        return summary;
    }
}
