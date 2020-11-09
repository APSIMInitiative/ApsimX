namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
        public void StoreValue()
        {
            object value = locator.Get(variableName);
            if (value is IFunction function)
                value = function.Value();
            else if (value != null && (value.GetType().IsArray || value.GetType().IsClass))
            {
                try
                {
                    value = ReflectionUtilities.Clone(value);
                }
                catch (Exception err)
                {
                    throw new Exception($"Cannot report variable \"{variableName}\": Variable is a non-reportable type: \"{value?.GetType()?.Name}\".", err);
                }
            }

            valuesToAggregate.Add(value);
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
