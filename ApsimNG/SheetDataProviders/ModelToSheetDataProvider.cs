using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.Soils;
using Gtk.Sheet;

namespace UserInterface.Views;

/// <summary>
/// Create a sheet data provider from a model instance.
/// </summary>
public class ModelToSheetDataProvider
{
    /// <summary>
    /// Convert a model to an ISheetDataProvider so that it can be represented in a grid control.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>An ISheetDataProvider instance.</returns>
    public static IDataProvider ToSheetDataProvider(object model)
    {
        return DataProviderFactory.Create(model, (properties) =>
        {
            if (model is Physical physical)
                ProcessPhysicalProperties(properties);
            else if (model is Chemical chemical)
                ProcessChemicalProperties(properties);
            else if (model is Solute solute)
                ProcessSoluteProperties(properties);
        });
    }

    /// <summary>
    /// Process the properties to go onto the physical grid.
    /// </summary>
    /// <param name="properties">The physical properties</param>
    private static void ProcessPhysicalProperties(List<PropertyMetadata> properties)
    {
        // Add the SoilCrop properties to the end of the physical properties list.
        Physical physical = properties.First().Obj as Physical;
        foreach (var soilCrop in physical.FindAllChildren<SoilCrop>())
        {
            string plantName = soilCrop.Name.Replace("Soil", string.Empty);

            DataProviderFactory.Create(soilCrop, (soilCropProperties) =>
            {
                foreach (var soilCropProperty in soilCropProperties.Where(sc => sc.Alias != "Depth"))
                {
                    if (soilCropProperty.Alias == "PAWC")
                        soilCropProperty.Units = $"{soilCrop.PAWCmm.Sum():0.0} mm";
                    soilCropProperty.Alias = $"{plantName} {soilCropProperty.Alias}";
                    properties.Add(soilCropProperty);
                }
            });
        }
    }

    /// <summary>
    /// Process the properties to go onto the chemical grid.
    /// </summary>
    /// <param name="properties">The properties</param>
    private static void ProcessChemicalProperties(List<PropertyMetadata> properties)
    {
        // Insert the solute properties after the initial Depth property.
        var chemical = properties.First().Obj as Chemical;

        int insertIndex = 1;  // Assumes depth is the first column.
        foreach (var solute in Chemical.GetStandardisedSolutes(chemical))
        {
            DataProviderFactory.Create(solute, (soluteProperties) =>
            {
                var initialValues = soluteProperties.Find(p => p.Alias == "InitialValues");
                if (initialValues != null)
                {
                    initialValues.Alias = solute.Name;
                    initialValues.Metadata = Enumerable.Repeat(SheetCellState.ReadOnly, solute.Thickness.Length).ToList();
                    properties.Insert(insertIndex, initialValues);
                    insertIndex++;
                }
            });
        }
    }

    /// <summary>
    /// Process the properties to go onto the solute grid.
    /// </summary>
    /// <param name="properties">The properties</param>
    private static void ProcessSoluteProperties(List<PropertyMetadata> properties)
    {
        // Remove Exco and FIP if SWIM is NOT present.
        var solute = properties.First().Obj as Solute;
        bool swimPresent = solute.FindInScope<Swim3>() != null || solute.Parent is Models.Factorial.Factor;
        if (!swimPresent)
            properties.RemoveAll(p => p.Alias == "Exco" || p.Alias == "FIP");
    }
}