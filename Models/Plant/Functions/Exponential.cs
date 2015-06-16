using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// An exponential function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Takes the value of the child as the x value and returns the y value from a exponential of the form y = A * B * exp(x * C)")]
    public class ExponentialFunction : Model, IFunction
    {
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
        private List<IModel> ChildFunctions;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Sigmoid function must have only one argument</exception>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                if (ChildFunctions.Count == 1)
                {
                    IFunction F = ChildFunctions[0] as IFunction;

                    return A + B * Math.Exp(C * F.Value);
                }
                else
                {
                    throw new Exception("Sigmoid function must have only one argument");
                }
            }
        }

    }
}
