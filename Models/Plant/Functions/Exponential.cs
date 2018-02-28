// -----------------------------------------------------------------------
// <copyright file="ExponentialFunction.cs" company="APSIM Initiative">
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
    /// An exponential function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Takes the value of the child as the x value and returns the y value from a exponential of the form y = A + B * exp(x * C)")]
    public class ExponentialFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];
        
        /// <summary>The child functions</summary>
        [ChildLink]
        private List<IModel> childFunctions = null;

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

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (childFunctions.Count == 1)
            {
                IFunction F = childFunctions[0] as IFunction;

                returnValue[0] = A + B * Math.Exp(C * F.Value());
                return returnValue;
            }
            else
            {
                throw new Exception("Exponential function must have only one argument");
            }
        }

    }
}
