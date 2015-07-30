using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)
    /// </summary>
    [Serializable]
    [Description("Takes the value of the child as the x value and returns the y value from a sigmoid of the form y = Xmax * 1/1+exp(-(x-Xo)/b)")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SigmoidFunction : Model, IFunction
    {
        /// <summary>constructor</summary>
        public SigmoidFunction()
        {
            Xmax = 1.0;
            Xo = 1.0;
            b = 1.0;
        }

        /// <summary>The xmax</summary>
        [Description("Xmax")]
        public double Xmax { get; set; }
        /// <summary>The xo</summary>
        [Description("Xo")]
        public double Xo { get; set; }
        /// <summary>The b</summary>
        [Description("b")]
        public double b { get; set; }
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

                    return Xmax * 1 / (1 + Math.Exp(-(F.Value - Xo) / b));
                }
                else
                {
                    throw new Exception("Sigmoid function must have only one argument");
                }
            }
        }

    }
}
