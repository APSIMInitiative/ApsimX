using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Core;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// An exponential function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Takes the value of the child as the x value and returns the y value from a exponential of the form y = A + B * exp(x * C)")]
    public class ExponentialFunction : Model, IFunction, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>ExponentialFunction Constructor</summary>
        public ExponentialFunction()
        {
            A = 1.0;
            B = 1.0;
            C = 1.0;
        }
        /// <summary>a</summary>
        [Description("A")]
        public double A { get; set; }
        /// <summary>The b</summary>
        [Description("B")]
        public double B { get; set; }
        /// <summary>The c</summary>
        [Description("C")]
        public double C { get; set; }
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Sigmoid function must have only one argument</exception>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = Structure.FindChildren<IFunction>().ToList();

            if (ChildFunctions.Count() == 1)
            {
                IFunction F = ChildFunctions.First() as IFunction;

                return A + B * Math.Exp(C * F.Value(arrayIndex));
            }
            else
            {
                throw new Exception("Exponential function must have only one argument");
            }
        }

    }
}
