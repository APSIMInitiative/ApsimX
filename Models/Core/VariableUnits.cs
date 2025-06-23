using System;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace Models.Core;

/// <summary>
/// Provides unit get/set functions for c# properties.
/// </summary>
public static class VariablePropertyExtensions
{
    /// <summary>
    /// Get the units of the property
    /// </summary>
    /// <param name="property">The property to get the units for.</param>
    public static string GetUnits(this VariableProperty property)
    {
        string unitString = null;
        UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property.PropertyInfo, typeof(UnitsAttribute), false) as UnitsAttribute;
        PropertyInfo unitsInfo = property.Object?.GetType().GetProperty(property.Name + "Units");
        if (unitsAttribute != null)
        {
            unitString = unitsAttribute.ToString();
        }
        else if (unitsInfo != null)
        {
            object val = unitsInfo.GetValue(property.Object, null);
            unitString = val.ToString();
        }
        return unitString;
    }

    /// <summary>
    /// Set the units of the property
    /// </summary>
    /// <param name="property">The property to get the units for.</param>
    /// <param name="newUnits">New units</param>
    public static void SetUnits(this VariableProperty property, string newUnits)
    {
        PropertyInfo unitsInfo = property.Object.GetType().GetProperty(property.Name + "Units");
        MethodInfo unitsSet = property.Object.GetType().GetMethod(property.Name + "UnitsSet");
        if (unitsSet != null)
        {
            unitsSet.Invoke(property.Object, new object[] { Enum.Parse(unitsInfo.PropertyType, newUnits) });
        }
        else if (unitsInfo != null)
        {
            unitsInfo.SetValue(property.Object, Enum.Parse(unitsInfo.PropertyType, newUnits), null);
        }
    }

    /// <summary>
    /// Get the units of the property
    /// </summary>
    /// <param name="property">The property to get the units for.</param>
    public static string GetUnitsLabel(this VariableProperty property)
    {
        if (property != null)
        {
            // Get units from property
            string unitString = null;
            UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property.PropertyInfo, typeof(UnitsAttribute), false) as UnitsAttribute;
            PropertyInfo unitsInfo = property.Object.GetType().GetProperty(property.Name + "Units");
            if (unitsAttribute != null)
            {
                unitString = unitsAttribute.ToString();
            }
            else if (unitsInfo != null)
            {
                object val = unitsInfo.GetValue(property.Object, null);
                if (unitsInfo != null && unitsInfo.PropertyType.BaseType == typeof(Enum))
                    unitString = VariableProperty.GetEnumDescription(val as Enum);
                else
                    unitString = val.ToString();
            }
            if (unitString != null)
                return "(" + unitString + ")";
        }
        return null;
    }
}