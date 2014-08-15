using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using System.Collections;
using Models.Core;

namespace Models.PMF.Functions
{
    [Serializable]
    [Description("returns a y value that corresponds to the position of the value of XProperty in the specified xy matrix")]
    public class LinearInterpolationFunction : Function
    {
        private bool YsAreAllTheSame = false;
        public XYPairs XYPairs { get; set; }
        public string XProperty = "";

        public override void OnLoaded()
        {
            if (XYPairs != null)
            {
                for (int i = 1; i < XYPairs.Y.Length; i++)
                    if (XYPairs.Y[i] != XYPairs.Y[i - 1])
                    {
                        YsAreAllTheSame = false;
                        return;
                    }

                // If we get this far then the Y values must all be the same.
                YsAreAllTheSame = XYPairs.Y.Length > 0;
            }
        }

        public override double Value
        {
            get
            {
                // Shortcut exit when the Y values are all the same. Runs quicker.
                if (YsAreAllTheSame)
                    return XYPairs.Y[0];

                string PropertyName = XProperty;
                object v = Util.GetVariable(PropertyName, this);
                if (v == null)
                    throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
                double XValue = (double) v;
                return XYPairs.ValueIndexed(XValue);
            }
        }

        public double ValueForX(double XValue)
        {
            return XYPairs.ValueIndexed(XValue);
        }

        public override double[] Values
        {
            get
            {
                string PropertyName = XProperty;

                double[] v = (double[])this.Get(XProperty);
                if (v == null)
                    throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
                if (v is Array)
                {
                    double[] ReturnValues = new double[v.Length];
                    for (int i = 0; i < v.Length; i++)
                        ReturnValues[i] = XYPairs.ValueIndexed(v[i]);
                    return ReturnValues;
                }
                else
                {
                    double XValue = Convert.ToDouble(v);
                    return new double[1] { XYPairs.ValueIndexed(XValue) };
                }
            }
        }

    }

}