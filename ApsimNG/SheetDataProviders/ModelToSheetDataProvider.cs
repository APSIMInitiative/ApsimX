using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Models.Core;
using Models.Soils;
using Gtk.Sheet;
using System;

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
        List<PropertyMetadata> properties = new();
        if (model is Physical physical)
            properties = DiscoverPhysicalProperties(physical);
        else if (model is Chemical chemical)
            properties = DiscoverChemicalProperties(chemical);
        else if (model is Solute solute)
            properties = DiscoverSoluteProperties(solute);
        else
            properties = DiscoverProperties(model);

        // Create and return an PropertySheetDataProvider that will represent each
        // property as a column.
        return new PropertySheetDataProvider(properties);
    }

    /// <summary>
    /// Discover the properties to go onto the physical grid.
    /// </summary>
    /// <param name="physical">The physical model</param>
    private static List<PropertyMetadata> DiscoverPhysicalProperties(Physical physical)
    {
        // The strategy is to discover the properties in Physical and then add the SoilCrop
        // properties to the end of the physical properties list.
        List<PropertyMetadata> properties = DiscoverProperties(physical);
        foreach (var soilCrop in physical.FindAllChildren<SoilCrop>())
        {
            string plantName = soilCrop.Name.Replace("Soil", string.Empty);
            foreach (var soilCropProperty in DiscoverProperties(soilCrop).Where(sc => sc.Alias != "Depth"))
            {
                if (soilCropProperty.Alias == "PAWC")
                    soilCropProperty.Units = $"{soilCrop.PAWCmm.Sum():0.0} mm";
                soilCropProperty.Alias = $"{plantName} {soilCropProperty.Alias}";
                properties.Add(soilCropProperty);
            }
        }
        return properties;
    }

    /// <summary>
    /// Discover the properties to go onto the chemical grid.
    /// </summary>
    /// <param name="chemical">The chemical model</param>
    private static List<PropertyMetadata> DiscoverChemicalProperties(Chemical chemical)
    {
        // The strategy is to discover the properties in Chemical and then insert the solute
        // properties after the initial Depth property.
        List<PropertyMetadata> properties = DiscoverProperties(chemical);

        int insertIndex = 1;  // Assumes depth is the first column.
        foreach (var solute in Chemical.GetStandardisedSolutes(chemical))
        {
            var soluteProperties = DiscoverProperties(solute);
            var initialValues = soluteProperties.Find(p => p.Alias == "InitialValues");
            if (initialValues != null)
            {
                initialValues.Alias = solute.Name;
                initialValues.Metadata = Enumerable.Repeat(SheetDataProviderCellState.ReadOnly, solute.Thickness.Length).ToList();
                properties.Insert(insertIndex, initialValues);
                insertIndex++;
            }
        }
        return properties;
    }

    /// <summary>
    /// Discover the properties to go onto the solute grid.
    /// </summary>
    /// <param name="solute">The solute model</param>
    private static List<PropertyMetadata> DiscoverSoluteProperties(Solute solute)
    {
        // The strategy is to discover the properties in Solute and then remove 
        // Exco and FIP if SWIM is NOT present.
        List<PropertyMetadata> properties = DiscoverProperties(solute);
        bool swimPresent = solute.FindInScope<Swim3>() != null || solute.Parent is Models.Factorial.Factor;
        if (!swimPresent)
            properties.RemoveAll(p => p.Alias == "Exco" || p.Alias == "FIP");
        return properties;
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
                    string[] validUnits = null;
                    if (unitsAttribute != null)
                        units = unitsAttribute.ToString();
                    else
                    {
                        // Look for a dynamic unit property.
                        var unitsProperty = model.GetType().GetProperty($"{property.Name}Units", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        units = unitsProperty?.GetValue(model, null).ToString();

                        if (unitsProperty != null && unitsProperty.PropertyType.IsEnum)
                            validUnits = Enum.GetNames(unitsProperty.PropertyType).ToArray();
                        else
                            validUnits = new string[] { units };
                    }

                    // Look for a dynamic metadata property.
                    var metaDataProperty = model.GetType().GetProperty($"{property.Name}Metadata", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    
                    // Create a new instance of PropertyMetadata and add to the return list of properties.
                    properties.Add(new PropertyMetadata(model, property, metaDataProperty, displayAttribute.DisplayName, units, validUnits, displayAttribute.Format));
                }
            }
        }

        return properties;
    }
}