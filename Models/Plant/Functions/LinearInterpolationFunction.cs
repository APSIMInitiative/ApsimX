using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using System.Collections;
using Models.Core;

namespace Models.Plant.Functions
{
    [Description("returns a y value that corresponds to the position of the value of XProperty in the specified xy matrix")]
    public class LinearInterpolationFunction : Function
    {
        public XYPairs XYPairs { get; set; }

        public string XProperty = "";

        
        public override double FunctionValue
        {
            get
            {
                string PropertyName = XProperty;
                string ArraySpec;
                bool ArrayFound = PropertyName.Contains("[");
                if (ArrayFound)
                    ArraySpec = Utility.String.SplitOffBracketedValue(ref PropertyName, '[', ']');
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

                object v = this.Get(XProperty);
                if (v == null)
                    throw new Exception("Cannot find value for " + Name + " XProperty: " + XProperty);
                if (v is Array)
                {
                    Array A = v as Array;
                    double[] ReturnValues = new double[A.Length];
                    for (int i = 0; i < A.Length; i++)
                    {
                        double XValue = Convert.ToDouble(A.GetValue(i));
                        ReturnValues[i] = XYPairs.ValueIndexed(XValue);
                    }
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