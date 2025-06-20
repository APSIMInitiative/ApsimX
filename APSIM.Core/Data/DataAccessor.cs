using System.Collections;
using System.Globalization;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// This class provides get/set/conversion (scalar, array, List<>) functions. The caller can:
///     * get a whole Array or List or part thereof,
///     * set a whole Array or List or part thereof,
///     * convert from one data type to another.
/// Specifying a partial Array or List is done via an instance of DataArrayFilter.
/// </summary>
public class DataAccessor
{
    /// <summary>
    /// Get the data from a provider, optionally applying an array filter.
    /// </summary>
    /// <param name="provider">Instance of a data provider.</param>
    /// <param name="arrayFilter">Optional instance of an array filter.</param>
    /// <returns></returns>
    public static object Get(IDataProvider provider, DataArrayFilter arrayFilter = null)
    {
        object data = provider.Data;
        if (arrayFilter == null)
            return data;
        else
            return arrayFilter?.Get(data);
    }

    /// <summary>
    /// Set the value of a data object from a provider, optionally applying an array filter.
    /// </summary>
    /// <param name="provider">Instance of a data provider.</param>
    /// <param name="newValue">The new data to set.</param>
    /// <param name="arrayFilter">Optional instance of an array filter.</param>
    /// <returns></returns>
    public static void Set(IDataProvider provider, object newValue, DataArrayFilter arrayFilter = null)
    {
        if (arrayFilter == null)
            provider.Data = Convert(newValue, provider.Type);
        else
        {
            // Set element of an array.
            newValue = Convert(newValue, GetElementTypeOfIList(provider.Type));
            provider.Data = arrayFilter.Set(provider.Data, newValue);
        }
    }

    /// <summary>
    /// Convert data from one type to another.
    /// </summary>
    /// <param name="data">The data to convert.</param>
    /// <param name="targetType">The type to convert the data to.</param>
    /// <returns>The converted data</returns>
    public static object Convert(object data, Type targetType)
    {
        if (data == null || targetType.IsAssignableFrom(data.GetType()))
            return data;
        if (targetType.IsArray && data is string valueAsSt)
            data = valueAsSt.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        if (IsIList(targetType))
        {
            // Target is an IList. Determine the element type of the array to return.
            Type targetElementType = GetElementTypeOfIList(targetType);

            if (data is IEnumerable valueAsIEnumerable)
            {
                // Both source value and target type are arrays.
                Array values = Array.CreateInstance(targetElementType, valueAsIEnumerable.Count());
                int i = 0;
                foreach (var v in valueAsIEnumerable)
                {
                    object arrayElement = Convert(v, targetElementType);
                    values.SetValue(arrayElement, i);
                    i++;
                }
                return values;
            }
            else
            {
                // Source value is a scalar. Target type is an array.
                Array values = Array.CreateInstance(targetElementType, 1);
                object arrayElement = Convert(data, targetElementType);
                values.SetValue(arrayElement, 0);
                return values;
            }
        }
        else
        {
            // Target is a scalar
            if (data is not string && data is IEnumerable valueAsIEnumerable)
            {
                if (targetType == typeof(string))
                    return StringUtilities.Build(valueAsIEnumerable, ",");
                if (valueAsIEnumerable.Count() == 1)
                {
                    foreach (var v in valueAsIEnumerable)
                        data = v; // will only do this once
                }
                else
                    throw new Exception($"Cannot convert {data.GetType().Name} to {targetType.Name} because there is more than one value in the array.");
            }
            if (targetType == typeof(double))
                return System.Convert.ToDouble(data, CultureInfo.InvariantCulture);
            else if (targetType == typeof(float)) // yuck!
                return System.Convert.ToSingle(data, CultureInfo.InvariantCulture);
            else if (targetType == typeof(int))
                return System.Convert.ToInt32(data, CultureInfo.InvariantCulture);
            else if (targetType == typeof(string))
                return data.ToString();
            else if (targetType == typeof(bool))
                return System.Convert.ToBoolean(data, CultureInfo.InvariantCulture);
            else if (targetType == typeof(DateTime))
                return System.Convert.ToDateTime(data, CultureInfo.InvariantCulture);
            else if (targetType.IsEnum && data is string st)
                return Enum.Parse(targetType, st);
            else
                throw new Exception("Invalid property type: " + targetType.ToString());
        }
    }

    /// <summary>
    /// Helper function to get the element type of an Array or List.
    /// </summary>
    /// <param name="type">The type of the Array or List (e.g. int[] or List<int>)</param>
    /// <returns>The element type e.g. int.</returns>
    public static Type GetElementTypeOfIList(Type type)
    {
        Type elementType;
        if (type.HasElementType)
            elementType = type.GetElementType();
        else
        {
            Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length > 0)
                elementType = genericArguments[0];
            else
                throw new Exception("Unknown type of array");
        }

        return elementType;
    }

    /// <summary>
    /// Is the specified type an Array or List?
    /// </summary>
    /// <param name="type">The type of the Array or List (e.g. int[] or List<int>)</param>
    /// <returns>True if Array or List.</returns>
    internal static bool IsIList(Type type)
    {
        return type.IsArray || type.Name == "List`1";
    }
}