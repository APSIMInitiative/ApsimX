using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using System.Collections;
using Models.Core;

namespace Models.PMF.Functions
{
    [Description("returns a y value that is interpolated between given XY pairs using cubic Hermite splines")]
    public class SplineInterpolationFunction : Function
    {
        public XYPairs XYPairs { get; set; }

        public string XProperty = "";

        
        public override double Value
        {
            get
            {
                string PropertyName = XProperty;
                string ArraySpec = Utility.String.SplitOffBracketedValue(ref PropertyName, '[', ']');
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
                return Interpolate(XYPairs.X, XYPairs.Y, XValue);

            }
        }

        private double Interpolate(double[] X, double[] Y, double x)
        {
            double[] m = new double[X.Length];
            m[0] = 0.0;
            for (int i = 1; i < X.Length - 1; i++)
            {
                double dlt1 = (Y[i] - Y[i - 1]) / (X[i] - X[i - 1]);
                double dlt2 = (Y[i + 1] - Y[i]) / (X[i + 1] - X[i]);
                m[i] = (dlt1 + dlt2) / 2.0;
            }

            m[X.Length - 1] = 0.0;

            double y = 0.0;

            if (x <= X[0])
                y = Y[0];
            else if (x >= X[X.Length - 1])
                y = Y[X.Length - 1];
            else
            {
                for (int i = 0; i < X.Length - 1; i++)
                {
                    if (x > X[i])
                    {


                        double t = (x - X[i]) / (X[i + 1] - X[i]);
                        double h = X[i + 1] - X[i];
                        double h00 = (1 + 2 * t) * Math.Pow((1 - t), 2.0);
                        double h10 = t * Math.Pow((1 - t), 2.0);
                        double h01 = Math.Pow(t, 2.0) * (3 - 2 * t);
                        double h11 = Math.Pow(t, 2.0) * (t - 1);
                        y = Y[i] * h00 + h * m[i] * h10 + Y[i + 1] * h01 + h * m[i + 1] * h11;
                    }
                }
            }


            return y;

        }
    }

}