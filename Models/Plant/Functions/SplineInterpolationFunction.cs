// ----------------------------------------------------------------------
// <copyright file="SplineInterpolationFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions
{
    using APSIM.Shared.Utilities;
    using MathNet.Numerics.Interpolation;
    using Models.Core;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// # [Name]
    /// A value is returned via Akima spline interpolation of a given set of XY pairs
    /// </summary>
    [Serializable]
    [Description("A value is returned via Akima spline interpolation of a given set of XY pairs")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SplineInterpolationFunction : BaseFunction
    {
        /// <summary>A locator object</summary>
        [Link]
        private ILocator locator = null;

        /// <summary>Gets the xy pairs.</summary>
        [ChildLink]
        private XYPairs xys = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The spline</summary>
        [NonSerialized]
        private CubicSpline spline = null;

        /// <summary>The x property</summary>
        [Description("XProperty")]
        public string XProperty { get; set; }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (spline == null)
                spline = CubicSpline.InterpolateBoundaries(xys.X, xys.Y, SplineBoundaryCondition.FirstDerivative, 0, SplineBoundaryCondition.FirstDerivative, 0);

            object v = locator.Get(XProperty);
            if (v == null)
                throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
            if (v is IFunction)
                v = (v as IFunction).Values();

            double[] returnValues;
            if (v is double[])
            {
                double[] array = v as double[];
                for (int i = 0; i < array.Length; i++)
                    array[i] = spline.Interpolate(array[i]);
                returnValues = array;
            }
            else if (v is double)
                returnValues =  new double[] { spline.Interpolate((double)v) };

            else
                throw new Exception("Cannot do a spline interpolation on type: " + v.GetType().Name +
                                    " in function: " + Name);
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + StringUtilities.BuildString(returnValues, "F3"));
            return returnValues;
        }
    }
}