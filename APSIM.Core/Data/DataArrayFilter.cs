using System.Collections;

namespace APSIM.Core;

/// <summary>
/// This class filters Array or List instances using a string specifier.
/// The specifier can be:
///     2     - a 1 based index. Returns a single value.
///     2:4   - a 1 based start:end index. Returns multiple values - no aggregation.
///     :4    - a 1 based end index. Start assumed to be start of array. Returns multiple values - no aggregation.
///     2:    - a 1 based start index. End assumed to be end of array. Returns multiple values - no aggregation.
/// </summary>
internal class DataArrayFilter
{
    /// <summary>Lower array index.</summary>
    private int startIndex;

    /// <summary>Upper array index.</summary>
    private int endIndex;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="specifier">The specifier e.g. 2:4</param>
    public DataArrayFilter(string specifier)
    {
        ProcessArraySpecifier(specifier);
    }

    /// <summary>
    /// Apply the filter to a data object and return the resulting array or scalar.
    /// </summary>
    /// <param name="data">The data to apply the filter to.</param>
    public object Get(object data)
    {
        ThrowIfScalarOrNull(data);

        IList array = data as IList;
        if (array.Count != 0)
        {
            int constrainedEndIndex = Math.Min(endIndex, array.Count - 1);

            // If one value is to be returned then return a scalar, otherwise return an array.
            if (startIndex == constrainedEndIndex)
                return array[startIndex];
            else
            {
                Array newArray = Array.CreateInstance(DataAccessor.GetElementTypeOfIList(array.GetType()), length: constrainedEndIndex - startIndex + 1);
                for (int i = startIndex; i <= constrainedEndIndex; i++)
                    newArray.SetValue(array[i], i - startIndex);
                return newArray;
            }
        }
        return data;
    }

    /// <summary>
    /// Apply the filter, setting the data object (or an element thereof) to a new value.
    /// </summary>
    /// <param name="data">The data to apply the filter to.</param>
    public object Set(object data, object newValue)
    {
        ThrowIfScalarOrNull(data);

        IList array = data as IList;
        int constrainedEndIndex = Math.Min(endIndex, array.Count - 1);
        if (startIndex >= 0 && constrainedEndIndex < array.Count)
        {
            for (int i = startIndex; i <= constrainedEndIndex; i++)
                array[i] = newValue;
        }
        else
            throw new Exception($"Index {startIndex} is out of bounds. Cannot set array.");

        return data;
    }

    /// <summary>
    /// Process a specifier into integer start:end indexes.
    /// </summary>
    /// <param name="specifier">The array specifier.</param>
    private void ProcessArraySpecifier(string specifier)
    {
        if (specifier != null)
        {
            // Can be either a number or a range e.g. 1:3
            int posColon = specifier.IndexOf(':');
            if (posColon == -1)
            {
                startIndex = SpecifierToInt(specifier);
                endIndex = startIndex;
            }
            else
            {
                string start = specifier.Substring(0, posColon);
                if (!string.IsNullOrEmpty(start))
                    startIndex = SpecifierToInt(start);
                else
                    startIndex = 1;

                string end = specifier.Substring(posColon + 1);
                if (!string.IsNullOrEmpty(end))
                    endIndex = SpecifierToInt(end);
                else
                    endIndex = int.MaxValue;
            }

            // Convert from 1 base to 0 based indexes.
            startIndex--;
            endIndex = endIndex == int.MaxValue ? endIndex : endIndex-1;

            // Check startIndex. Can't check endIndex because we don't know the size of the data array yet.
            if (startIndex == -1)
                throw new Exception($"Array indexing in APSIM (report) is one based. Cannot have an index of zero. Array specifier: {specifier}");
        }
    }

    /// <summary>
    /// Convert an array specifier (e.g. 1 or 100mm) to an integer.
    /// </summary>
    /// <param name="specifier">The array specifier</param>
    /// <returns>The integer.</returns>
    private static int SpecifierToInt(string specifier)
    {
        if (!string.IsNullOrEmpty(specifier))
        {
            // look for an integer first (array index).
            if (int.TryParse(specifier, out int i))
                return i;

            throw new Exception($"Invalid array specifier: {specifier}");
        }
        else
            return -1;
    }

    /// <summary>
    /// Throw an exception if data is a scalar or null.
    /// </summary>
    /// <param name="data">The data instance</param>
    private void ThrowIfScalarOrNull(object data)
    {
        if (data == null || data is not IList)
            throw new Exception("Array index on a scalar is not valid");
    }

}