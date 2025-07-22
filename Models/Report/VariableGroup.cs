using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.Core;
using Models.Functions;
using Models.Soils;

namespace Models
{

    /// <summary>
    ///
    /// </summary>
    public class VariableGroup
    {
        /// <summary>An instance of a locator service.</summary>
        private readonly ILocator locator;

        /// <summary>The values for each report event (e.g. daily) for a group.</summary>
        private readonly List<object> valuesToAggregate = new List<object>();

        /// <summary>The full name of the variable we are retrieving from APSIM.</summary>
        private readonly string variableName;

        /// <summary>The aggregation (e.g. sum) to apply to the values in each group.</summary>
        private readonly string aggregationFunction;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="locatorInstance">An instance of a locator service</param>
        /// <param name="valueOfGroupBy">The full name of the group by variable.</param>
        /// <param name="varName">The full name of the variable we are retrieving from APSIM.</param>
        /// <param name="aggFunction">The aggregation (e.g. sum) to apply to the values in each group.</param>
        public VariableGroup(ILocator locatorInstance, object valueOfGroupBy,
                                string varName, string aggFunction)
        {
            locator = locatorInstance;
            GroupByValue = valueOfGroupBy;
            variableName = varName;
            aggregationFunction = aggFunction;

        }

        /// <summary>The value full name of the group by variable.</summary>
        public object GroupByValue { get; }

        /// <summary>Stores a value into the values array.</summary>
        /// <param name="physical">An instance of a physical node.</param>///
        /// <param name="startDepth">Start depth e.g. 100</param>
        /// <param name="endDepth">End depth e.g. 200</param>
        public void StoreValue(IPhysical physical, int startDepth, int endDepth)
        {
            object value = locator.Get(variableName, LocatorFlags.IncludeReportVars);
            if (value is IFunction function)
                value = function.Value();
            else if (value != null && (value.GetType().IsArray || value.GetType().IsClass))
            {
                try
                {
                    if (startDepth != 0 && endDepth != 0)
                    {
                        // do a soil depth aggregation.
                        if (value is IList<double> values)
                            value = AggregateSoilVariable(values, physical, startDepth, endDepth);
                        else
                            throw new Exception($"Cannot use a soil aggregation on a non array. Variable: {variableName}");
                    }
                    else
                        value = ReflectionUtilities.Clone(value);
                }
                catch (Exception err)
                {
                    throw new Exception($"Cannot report variable \"{variableName}\": Variable is a non-reportable type: \"{value?.GetType()?.Name}\".", err);
                }
            }

            valuesToAggregate.Add(value);
        }

        /// <summary>
        /// User has specified a soil array specifier e.g. 200mm:400mm. Aggregate the array into
        /// a single value by mapping the values into the correct layer structure.
        /// </summary>
        /// <param name="values">The values to aggregate</param>
        /// <param name="physical">An instance of a physical node.</param>
        /// <param name="startDepth">Start depth e.g. 100</param>
        /// <param name="endDepth">End depth e.g. 200</param>
        private object AggregateSoilVariable(IList<double> values, IPhysical physical, int startDepth, int endDepth)
        {
            // Create an soil profile layer structure based on start/end depth. This will be used
            // to map values into.
            double[] toThickness = [startDepth, endDepth - startDepth];

            return SoilUtilities.MapMass(values.ToArray(), physical.Thickness, toThickness)
                                .Last(); // the last element will be the layer we want.
        }

        /// <summary>Retrieve the current value to be stored in the report.</summary>
        public object GetValue()
        {
            if (!string.IsNullOrEmpty(aggregationFunction))
                return ApplyAggregation();
            else if (valuesToAggregate.Count == 0)
                throw new Exception($"In report, cannot find a value to return for variable {variableName}");
            else
                return valuesToAggregate.Last();
        }

        /// <summary>Clear the values.</summary>
        public void Clear()
        {
            valuesToAggregate.Clear();
        }

        /// <summary>Apply the aggregation function if necessary to the list of values we have stored.</summary>
        private object ApplyAggregation()
        {
            if (aggregationFunction == "value")
            {
                if (valuesToAggregate.Count == 0)
                    return null;
                return valuesToAggregate.Last();
            }

            double result = double.NaN;
            if (this.valuesToAggregate.Count > 0 && this.aggregationFunction != null)
            {
                if (this.aggregationFunction.Equals("sum", StringComparison.CurrentCultureIgnoreCase))
                    if (this.valuesToAggregate[0].GetType() == typeof(double))
                        result = MathUtilities.Sum(this.valuesToAggregate.Cast<double>());
                    else if (this.valuesToAggregate[0].GetType() == typeof(int))
                        result = MathUtilities.Sum(this.valuesToAggregate.Cast<int>());
                    else
                        throw new Exception("Unable to use sum function for variable of type " + this.valuesToAggregate[0].GetType().ToString());
                else if (this.aggregationFunction.Equals("prod", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Prod(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("mean", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Average(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("min", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Min(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("max", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.Max(this.valuesToAggregate);
                else if (this.aggregationFunction.Equals("first", StringComparison.CurrentCultureIgnoreCase))
                    result = Convert.ToDouble(this.valuesToAggregate.First(), System.Globalization.CultureInfo.InvariantCulture);
                else if (this.aggregationFunction.Equals("last", StringComparison.CurrentCultureIgnoreCase))
                    result = Convert.ToDouble(this.valuesToAggregate.Last(), System.Globalization.CultureInfo.InvariantCulture);
                else if (this.aggregationFunction.Equals("diff", StringComparison.CurrentCultureIgnoreCase))
                    result = Convert.ToDouble(this.valuesToAggregate.Last(), System.Globalization.CultureInfo.InvariantCulture) -
                                    Convert.ToDouble(this.valuesToAggregate.First(), System.Globalization.CultureInfo.InvariantCulture);
                else if (this.aggregationFunction.Equals("stddev", StringComparison.CurrentCultureIgnoreCase))
                    result = MathUtilities.SampleStandardDeviation(this.valuesToAggregate.Cast<double>());
            }
            return result;
        }
    }

}
