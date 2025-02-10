using System;
using System.Linq;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Returns the value at the given index. If the index is outside the array, the last value will be returned.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ArrayFunction : Model, IFunction
    {
        /// <summary>Gets the value.</summary>
        [Description("The values of the array (space seperated)")]
        public string Values { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("The optional units of the array")]
        public string Units { get; set; }

        /// <summary>Double values.</summary>
        public double[] Doubles { get; set; }

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new ApsimXException(this, "ArrayFunction must have an index to return.");

            if (Doubles == null && !string.IsNullOrEmpty(Values))
            {
                Doubles = Values.Split(' ')
                                       .Select(s => Convert.ToDouble(s, System.Globalization.CultureInfo.InvariantCulture))
                                       .ToArray();
                Values = null;
            }

            if (Doubles == null)
                throw new Exception($"Must specify values in ArrayFunction {Name}");

            if (arrayIndex > Doubles.Length - 1)
                return Doubles[Doubles.Length - 1];

            return Doubles[arrayIndex];
        }
    }
}
