using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Raises the value of the child to the power of the exponent specified
    /// </summary>
    [Serializable]
    [Description("Raises the value of the child to the power of the exponent specified")]
    [ViewName("UserInterface.Views.GridView")]
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
        private List<IModel> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Power function must have only one argument</exception>
        public double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(IFunction));

                if (ChildFunctions.Count == 1)
                {
                    IFunction F = ChildFunctions[0] as IFunction;
                    return Math.Pow(F.Value, Exponent);
                }
                else
                {
                    throw new Exception("Power function must have only one argument");
                }
            }
        }

    }
}
