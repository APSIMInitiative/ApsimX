using System.Collections;
using System.Data;
using System.Reflection;
using ExCSS;
using Models.Core;

namespace Gtk.Sheet;

/// <summary>
/// A factory for creating a DataProvider from an class instance.
/// </summary>
public class DataProviderFactory
{
    /// <summary>
    /// Convert a model to an ISheetDataProvider so that it can be represented in a grid control.
    /// </summary>
    /// <param name="obj">The class instance.</param>
    /// <param name="callback">A callback allowing the caller to manipulate the list of properties</param>
    /// <returns>A DataProvider instance.</returns>
    public static IDataProvider Create(object obj, Action<List<PropertyMetadata>> callback = null)
    {
        // Discover a list of potential class properties.
        var properties = DiscoverProperties(obj);

        var dataTableProperty = properties.Where(p => p.Property.PropertyType == typeof(DataTable));
        if (dataTableProperty.Any())
            return new DataTableProvider(dataTableProperty.First().Property.GetValue(obj) as DataTable);

        // If the first discovered property is a list of objects.
        var listObjectProperties = properties.Where(p => typeof(IList).IsAssignableFrom(p.Property.PropertyType) &&
                                                         p.Property.PropertyType.GetGenericArguments().Any() &&
                                                         p.Property.PropertyType.GetGenericArguments().First().IsClass);

        if (listObjectProperties.Count() == 1)
            return new ClassWithOneListProperty(listObjectProperties.First().Property, obj);
        else
        {
            callback?.Invoke(properties);

            // Create and return an PropertySheetDataProvider that will represent each
            // property as a column.
            return new ClassWithArrayProperties(properties);
        }
    }

    /// <summary>
    /// Discover all properties in a model that should be shown in a grid control.
    /// </summary>
    /// <param name="tuples">A list of property tuples to process.</param>
    private static List<PropertyMetadata> DiscoverProperties(object obj)
    {
        List<PropertyMetadata> returnProperties = new();

        // Discover a list of potential class properties.
        var properties = obj.GetType()
                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(p => p.GetCustomAttribute<DisplayAttribute>() != null);

        foreach (var property in properties)
        {
            DisplayAttribute displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
            UnitsAttribute unitsAttribute = property.GetCustomAttribute<UnitsAttribute>();
            if (displayAttribute != null)
            {
                string units;
                string[] validUnits = null;
                if (unitsAttribute != null)
                    units = unitsAttribute.ToString();
                else
                {
                    // Look for a dynamic unit property.
                    var unitsProperty = obj.GetType().GetProperty($"{property.Name}Units", BindingFlags.Instance | BindingFlags.Public);
                    units = unitsProperty?.GetValue(obj, null).ToString();

                    if (unitsProperty != null && unitsProperty.PropertyType.IsEnum)
                        validUnits = Enum.GetNames(unitsProperty.PropertyType).ToArray();
                    else
                        validUnits = new string[] { units };
                }

                // Look for a dynamic metadata property.
                var metaDataProperty = obj.GetType().GetProperty($"{property.Name}Metadata", BindingFlags.Instance | BindingFlags.Public);
                
                // Create a new instance of PropertyMetadata and add to the return list of properties.
                returnProperties.Add(new PropertyMetadata(obj, property, metaDataProperty, displayAttribute.DisplayName, units, validUnits, displayAttribute.Format));
            }
        }

        return returnProperties;
    }      
}