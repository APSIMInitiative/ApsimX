namespace Models.Utilities;

using System;
using System.Reflection;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;

/// <summary>
/// Provides unit get/set functions for c# properties.
/// </summary>
public static class AttributeUtilities
{
    /// <summary>
    /// Get the units of the property
    /// </summary>
    /// <param name="property">The property to get the units for.</param>
    /// <param name="model">The model instance</param>
    public static string GetUnits(this PropertyInfo property, IModel model)
    {
        string unitString = null;
        UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
        PropertyInfo unitsInfo = model?.GetType().GetProperty(property.Name + "Units");
        if (unitsAttribute != null)
        {
            unitString = unitsAttribute.ToString();
        }
        else if (unitsInfo != null)
        {
            object val = unitsInfo.GetValue(model, null);
            unitString = val.ToString();
        }
        return unitString;
    }

    /// <summary>
    /// Set the units of the property
    /// </summary>
    /// <param name="property">The property to set the units in.</param>
    /// <param name="model">The model instance</param>
    /// <param name="newUnits">New units</param>
    public static void SetUnits(this PropertyInfo property, IModel model, string newUnits)
    {
        PropertyInfo unitsInfo = model.GetType().GetProperty(property.Name + "Units");
        MethodInfo unitsSet = model.GetType().GetMethod(property.Name + "UnitsSet");
        if (unitsSet != null)
        {
            unitsSet.Invoke(model, new object[] { Enum.Parse(unitsInfo.PropertyType, newUnits) });
        }
        else if (unitsInfo != null)
        {
            unitsInfo.SetValue(model, Enum.Parse(unitsInfo.PropertyType, newUnits), null);
        }
    }

    /// <summary>
    /// Get the units of the property
    /// </summary>
    /// <param name="composite">The property to get the units for.</param>
    public static string GetUnitsLabel(this VariableComposite composite)
    {
        if (composite != null)
        {
            // Get units from property
            string unitString = null;
            UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(composite.Property, typeof(UnitsAttribute), false) as UnitsAttribute;
            PropertyInfo unitsInfo = composite.Object.GetType().GetProperty(composite.Name + "Units");
            if (unitsAttribute != null)
            {
                unitString = unitsAttribute.ToString();
            }
            else if (unitsInfo != null)
            {
                object val = unitsInfo.GetValue(composite.Object, null);
                if (unitsInfo != null && unitsInfo.PropertyType.BaseType == typeof(Enum))
                    unitString = GetEnumDescription(val as Enum);
                else
                    unitString = val.ToString();
            }
            if (unitString != null)
                return "(" + unitString + ")";
        }
        return null;
    }

    /// <summary>
    /// Looks for a description string associated with an enumerated value
    /// Adapted from http://blog.spontaneouspublicity.com/associating-strings-with-enums-in-c
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetEnumDescription(Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());

        DescriptionAttribute[] attributes =
        (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0)
            return attributes[0].ToString();
        else
            return value.ToString();
    }


    /// <summary>
    /// Gets the display format for this property e.g. 'N3'. Can return null if not present.
    /// </summary>
    public static string GetFormat(this PropertyInfo property)
    {
        DisplayAttribute displayFormatAttribute = ReflectionUtilities.GetAttribute(property, typeof(DisplayAttribute), false) as DisplayAttribute;
        if (displayFormatAttribute != null && displayFormatAttribute.Format != null)
        {
            return displayFormatAttribute.Format;
        }

        return string.Empty;
    }
}