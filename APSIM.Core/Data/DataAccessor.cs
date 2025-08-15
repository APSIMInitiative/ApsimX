namespace APSIM.Core;

/// <summary>
/// This class provides get/set/conversion (scalar, array, List<>) functions. The caller can:
///     * get a whole Array or List or part thereof,
///     * set a whole Array or List or part thereof,
///     * convert from one data type to another.
/// Specifying a partial Array or List is done via an instance of DataArrayFilter.
/// </summary>
internal class DataAccessor
{
    /// <summary>
    /// Get the data from a provider, optionally applying an array filter.
    /// </summary>
    /// <param name="provider">Instance of a data provider.</param>
    /// <param name="arrayFilter">Optional instance of an array filter.</param>
    /// <returns></returns>
    internal static object Get(IDataProvider provider, DataArrayFilter arrayFilter = null)
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
    internal static void Set(IDataProvider provider, object newValue, DataArrayFilter arrayFilter = null)
    {
        if (arrayFilter == null)
            provider.Data = ApsimConvert.ToType(newValue, provider.Type);
        else
        {
            // Set element of an array.
            newValue = ApsimConvert.ToType(newValue, GetElementTypeOfIList(provider.Type));
            provider.Data = arrayFilter.Set(provider.Data, newValue);
        }
    }

    /// <summary>
    /// Helper function to get the element type of an Array or List.
    /// </summary>
    /// <param name="type">The type of the Array or List (e.g. int[] or List<int>)</param>
    /// <returns>The element type e.g. int.</returns>
    internal static Type GetElementTypeOfIList(Type type)
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

}