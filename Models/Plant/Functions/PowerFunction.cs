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
    using System.Diagnostics;

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
                return new double[] { Math.Pow(childFunctions[0].Value(), Exponent) };
            else if (childFunctions.Count == 2)
            {
                IFunction f = childFunctions[0];
                IFunction p = childFunctions[1];
                double returnValue = Math.Pow(f.Value(), p.Value());
                Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + returnValue);
                return new double[] { returnValue };
            }
            else
                throw new Exception("Invalid number of arguments for Power function");
        }
    }
}
