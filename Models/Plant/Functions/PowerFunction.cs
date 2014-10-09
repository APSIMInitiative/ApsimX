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
    public class PowerFunction : Function
    {
        /// <summary>The exponent</summary>
        public double Exponent = 1.0;

        /// <summary>The child functions</summary>
        private List<IModel> ChildFunctions;
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Power function must have only one argument</exception>
        public override double Value
        {
            get
            {
                if (ChildFunctions == null)
                    ChildFunctions = Apsim.Children(this, typeof(Function));

                if (ChildFunctions.Count == 1)
                {
                    Function F = ChildFunctions[0] as Function;
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
