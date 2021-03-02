using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// [Name] = [FixedValue] ([Units])
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Constant : Model, IFunction
    {
        /// <summary>Gets the value.</summary>
        [Description("The value of the constant")]
        public double FixedValue { get; set; }

        /// <summary>Gets the optional units</summary>
        [Description("The optional units of the constant")]
        public string Units { get; set; }

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            return FixedValue;
        }
    }
}