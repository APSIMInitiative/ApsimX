using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using MathNet.Numerics.Interpolation;

using System.Collections;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("returns a y value that is interpolated between given XY pairs using Akima splines")]
    public class SplineInterpolationFunction : Function
    {
        public XYPairs XYPairs { get; set; }

        public string XProperty = "";

        private CubicSpline spline = null;
        private string PropertyName;
        private string ArraySpec;

        public SplineInterpolationFunction()
        {
            PropertyName = XProperty;
            ArraySpec = Utility.String.SplitOffBracketedValue(ref PropertyName, '[', ']');
        }
        
        public override double Value
        {
            get
            {
                double XValue = 0;
                try
                {
                    object v = Util.GetVariable(XProperty, this);
                    if (v == null)
                        throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
                    XValue = Convert.ToDouble(v);
                }
                catch (IndexOutOfRangeException)
                {
                }

                if (spline == null)
                    spline = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkima(XYPairs.X, XYPairs.Y);

                return Interpolate(XValue);
            }
        }

        private double Interpolate(double x)
        {
            return spline.Interpolate(x);
        }
    }

}