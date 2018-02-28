// ----------------------------------------------------------------------
// <copyright file="PowerFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using Models.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// # [Name]
    /// Raises the value of the child to the power of the exponent specified
    /// </summary>
    [Serializable]
    [Description("Raises the value of the child to the power of the exponent specified")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PowerFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];
        
        /// <summary>All child functions</summary>
        [ChildLink]
        private List<IFunction> childFunctions = null;

        /// <summary>The exponent</summary>
        [Description("Exponent")]
        public double Exponent { get; set; }

        /// <summary>constructor</summary>
        public PowerFunction()
        {
            Exponent = 1.0;
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (childFunctions.Count == 1)
                returnValue[0] = Math.Pow(childFunctions[0].Value(), Exponent);
            else if (childFunctions.Count == 2)
            {
                IFunction f = childFunctions[0];
                IFunction p = childFunctions[1];
                returnValue[0] = Math.Pow(f.Value(), p.Value());
            }
            else
                throw new Exception("Invalid number of arguments for Power function");

            return returnValue;
        }
    }
}
