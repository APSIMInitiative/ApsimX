using System;
using APSIM.Core;
using MathNet.Numerics.Interpolation;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// A value is returned via Akima spline interpolation of a given set of XY pairs
    /// </summary>
    [Serializable]
    [Description("A value is returned via Akima spline interpolation of a given set of XY pairs")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SplineInterpolationFunction : Model, IFunction, ILocatorDependency
    {
        private ILocator locator;

        /// <summary>Gets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs XYPairs = null;   // Temperature effect on Growth Interpolation Set

        /// <summary>The x property</summary>
        [Description("XProperty")]
        public string XProperty { get; set; }

        /// <summary>The spline</summary>
        [NonSerialized]
        private CubicSpline spline = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineInterpolationFunction"/> class.
        /// </summary>
        public SplineInterpolationFunction()
        {
        }

        /// <summary>Locator supplied by APSIM kernel.</summary>
        public void SetLocator(ILocator locator) => this.locator = locator;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot find value for  + Name +  XProperty:  + XProperty</exception>
        public double Value(int arrayIndex = -1)
        {
            double XValue = 0;
            try
            {
                object v = locator.GetObject(XProperty)?.Value;
                if (v == null)
                    throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
                if (v is Array && arrayIndex > -1)
                    XValue = Convert.ToDouble((v as Array).GetValue(arrayIndex),
                                              System.Globalization.CultureInfo.InvariantCulture);
                else
                    XValue = Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (IndexOutOfRangeException)
            {
            }

            if (spline == null)
            {
                spline = CubicSpline.InterpolateBoundaries(XYPairs.X, XYPairs.Y, SplineBoundaryCondition.FirstDerivative, 0, SplineBoundaryCondition.FirstDerivative, 0);

            }

            return Interpolate(XValue);
        }

        /// <summary>Interpolates the specified x.</summary>
        /// <param name="x">The x.</param>
        /// <returns></returns>
        private double Interpolate(double x)
        {
            return spline.Interpolate(x);
        }
    }

}