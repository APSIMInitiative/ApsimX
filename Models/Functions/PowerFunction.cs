using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Raises the value of the child to the power of the exponent specified
    /// </summary>
    [Serializable]
    [Description("Raises the value of the child to the power of the exponent specified")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PowerFunction : Model, IFunction
    {
        /// <summary>constructor</summary>
        public PowerFunction()
        {
            Exponent = 1.0;
        }
        /// <summary>The exponent</summary>
        [Description("Exponent")]
        public double Exponent { get; set; }

        /// <summary>The child functions</summary>
        private List<IFunction> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Power function must have only one argument</exception>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            if (ChildFunctions.Count() == 1)
            {
                IFunction F = ChildFunctions[0];
                return Math.Pow(F.Value(arrayIndex), Exponent);
            }
            else if (ChildFunctions.Count == 2)
            {
                IFunction F = ChildFunctions[0];
                IFunction P = ChildFunctions[1];
                return Math.Pow(F.Value(arrayIndex), P.Value(arrayIndex));
            }
            else
            {

                throw new Exception("Invalid number of arguments for Power function");
            }
        }

    }
}
