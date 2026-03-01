using System.Collections;
using APSIM.Shared.Utilities;

namespace APSIM.Core;

/// <summary>
/// This class filters Array or List instances using a string specifier.
/// The specifier can be:
///     2     - a 1 based index. Returns a single value.
///     2:4   - a 1 based start:end index. Returns multiple values - no aggregation.
///     :4    - a 1 based end index. Start assumed to be start of array. Returns multiple values - no aggregation.
///     2:    - a 1 based start index. End assumed to be end of array. Returns multiple values - no aggregation.
///     150mm - a depth (mm). Returns a single value for the layer as specified by the depth.
///     150mm:200mm - a start and end depth (mm). Maps the values in to the specified depth range and returns a single mapped value.
/// </summary>
internal class DataArrayFilter
{
    /// <summary>Lower array index.</summary>
    private int startIndex;

    /// <summary>Upper array index.</summary>
    private int endIndex;

    /// <summary>Are startIndex and endIndex depths (mm)?</summary>
    private bool indexesAreDepths;

    /// <summary>Node this instance of array filter is relative to.</summary>
    private double[] thickness;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="specifier">The specifier e.g. 2:4</param>
    /// <param name="thickness">Soil profile thicknesses (mm)</param>
    public DataArrayFilter(string specifier, double[] thickness)
    {
        this.thickness = thickness;
        ProcessArraySpecifier(specifier);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="specifier">The specifier e.g. 2:4</param>
    /// <param name="thickness">Node this array is relative to.</param>
    public DataArrayFilter(string specifier, Node relativeTo = null)
    {
        ProcessArraySpecifier(specifier);
        if (indexesAreDepths)
            thickness = relativeTo.FindInScope<ILayerStructure>()?.Thickness
                ?? throw new Exception("Cannot use a MM array specifier because no layer thickness can be found");
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
            if (indexesAreDepths)
            {
                if (array is IList<double> doubleArray)

                    return AggregateSoilVariable(doubleArray, thickness, startIndex, endIndex);
                else
                    throw new Exception("Can only use MM array indexing on a double array");
            }
            else
            {
                int constrainedEndIndex = Math.Min(endIndex, array.Count - 1);

                // If one value is to be returned then return a scalar, otherwise return an array.
                if (startIndex == constrainedEndIndex)
                    return array[startIndex];
                else
                {
                    int numElements = constrainedEndIndex - startIndex + 1;
                    Array newArray = Array.CreateInstance(DataAccessor.GetElementTypeOfIList(array.GetType()), numElements);
                    for (int i = startIndex; i <= constrainedEndIndex; i++)
                        newArray.SetValue(array[i], i - startIndex);
                    return newArray;
                }
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

        if (indexesAreDepths)
            throw new Exception($"Cannot set the value of a variable that uses MM indexing.");

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
            List<string> tokens = specifier.Split(":", StringSplitOptions.RemoveEmptyEntries).ToList();

            if (specifier.StartsWith(":"))
                tokens.Insert(0, "1");
            if (specifier.EndsWith(":"))
                tokens.Add(int.MaxValue.ToString());

            // Can be either a number or a range e.g. 1:3
            if (tokens.Count < 1 || tokens.Count > 2)
                throw new Exception($"Invalid array specifier: {specifier}");

            (int index, bool isMM) = SpecifierToInt(tokens.First());
            startIndex = index;
            indexesAreDepths = isMM;

            if (tokens.Count == 1)
                endIndex = startIndex;
            else
            {
                (index, isMM) = SpecifierToInt(tokens.Last());
                endIndex = index;
                if (isMM != indexesAreDepths)
                    throw new Exception($"Cannot mix integer array indexing and MM indexing in same variable: {specifier}");
            }

            // Convert from 1 base to 0 based indexes.
            if (!indexesAreDepths)
            {
                startIndex = startIndex - 1;
                endIndex = endIndex == int.MaxValue ? endIndex : endIndex - 1;

                // Check startIndex. Can't check endIndex because we don't know the size of the data array yet.
                if (startIndex == -1)
                    throw new Exception($"Array indexing in APSIM (report) is one based. Cannot have an index of zero. Array specifier: {specifier}");
            }
        }
    }

    /// <summary>
    /// Convert an array specifier (e.g. 1 or 100mm) to an integer.
    /// </summary>
    /// <param name="specifier">The array specifier</param>
    /// <returns>The integer.</returns>
    private static (int index, bool isMM) SpecifierToInt(string specifier)
    {
        if (!string.IsNullOrEmpty(specifier))
        {
            // look for an integer first (array index).
            if (int.TryParse(specifier, out int i))
                return (i, false);

            // look for an depth specifier e.g. (100mm)
            if (specifier.EndsWith("mm"))
            {
                string depthString = specifier[..^2];
                if (int.TryParse(depthString, out int depth))
                    return (depth, true);
            }

            throw new Exception($"Invalid array specifier: {specifier}");
        }
        else
            return (-1, false);
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

    /// <summary>
    /// User has specified a soil array specifier e.g. 200mm:400mm. Aggregate the array into
    /// a single value by mapping the values into the correct layer structure.
    /// </summary>
    /// <param name="values">The values to aggregate</param>
    /// <param name="physical">An instance of a physical node.</param>
    /// <param name="startDepth">Start depth e.g. 100</param>
    /// <param name="endDepth">End depth e.g. 200</param>
    private object AggregateSoilVariable(IList<double> values, double[] thickness, int startDepth, int endDepth)
    {
        if (startDepth == endDepth)
        {
            int i = SoilUtilities.LayerIndexOfDepth(thickness, startDepth);
            return values[i];
        }
        else
        {
            // Create an soil profile layer structure based on start/end depth. This will be used
            // to map values into.
            double[] toThickness = [startDepth, endDepth - startDepth];

            return SoilUtilities.MapMass(values.ToArray(), thickness, toThickness)
                                .Last(); // the last element will be the layer we want.
        }
    }
}
