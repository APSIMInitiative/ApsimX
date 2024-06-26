using System.Collections.Generic;
using System.Reflection;
using Models.Core;

namespace UserInterface.Views;

/// <summary>
/// 
/// </summary>
public class ModelToSheetDataProvider
{

    /// <summary>
    /// Convert a model to an ISheetDataProvider so that it can be represented in a grid control.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>An ISheetDataProvider instance.</returns>
    public static ISheetDataProvider ToSheetDataProvider(IModel model)
    {
        // Discover all public properties
        var properties = DiscoverProperties(model);

        // Create and return an PropertySheetDataProvider that will represent each
        // property as a column.
        return new PropertySheetDataProvider(properties);
    }

    /// <summary>
    /// Discover all properties in a model that should be shown in a grid control.
    /// </summary>
    /// <param name="model">The model to scan.</param>
    /// <returns>A list of discovered properties.</returns>
    private static List<PropertyMetadata> DiscoverProperties(IModel model)
    {
        List<PropertyMetadata> properties = new();

        foreach (var property in model.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (property.PropertyType.IsArray)
            {
                DisplayAttribute displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
                UnitsAttribute unitsAttribute = property.GetCustomAttribute<UnitsAttribute>();
                if (displayAttribute != null)
                {
                    string units = null;
                    if (unitsAttribute != null)
                        units = unitsAttribute.ToString();
                    else
                    {
                        // Look for a dynamic unit property.
                        var unitsProperty = model.GetType().GetProperty($"{property.Name}Units", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        units = unitsProperty?.GetValue(model, null).ToString();
                    }

                    // Look for a dynamic metadata property.
                    var metaDataProperty = model.GetType().GetProperty($"{property.Name}Metadata", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var metadata = metaDataProperty?.GetValue(model, null) as string[];
                    
                    // Create a new instance of PropertyMetadata and add to the return list of properties.
                    properties.Add(new PropertyMetadata(model, property, displayAttribute.DisplayName, units, displayAttribute.Format, metadata));
                }
            }
        }

        return properties;
    }
}