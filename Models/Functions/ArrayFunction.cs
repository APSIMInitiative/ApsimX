using System;
using System.Collections.Generic;
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

        private List<double> str2dbl = new List<double>();

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new ApsimXException(this, "ArrayFunction must have an index to return.");

            if (str2dbl.Count == 0)
            {
                string[] split = Values.Split(' ');
                foreach (string s in split)
                    try
                    {
                        str2dbl.Add(Convert.ToDouble(s, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch (Exception)
                    {
                        throw new ApsimXException(this, "ArrayFunction: Could not convert " + s + " to a number.");
                    }
            }

            if (arrayIndex > str2dbl.Count - 1)
                return str2dbl[str2dbl.Count - 1];

            return str2dbl[arrayIndex];
        }
    }
}
