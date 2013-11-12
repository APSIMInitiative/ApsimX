using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml;

namespace Models.Plant.Functions
{
    [Description("Returns a y value from the specified xy maxrix corresponding to the current value of the Xproperty")]
    public class XYPairs : Function
    {

        public string[] XY;
        public double[] X;
        public double[] Y;

        public void DoInitialisation()
        {
            X = new double[XY.Length];
            Y = new double[XY.Length];
            for (int i = 0; i < XY.Length; i++)
            {
                string[] XYBits = XY[i].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (XYBits.Length != 2)
                    throw new Exception("Invalid XY coordinate for function. Value: " + XY[i]);
                X[i] = Convert.ToDouble(XYBits[0]);
                Y[i] = Convert.ToDouble(XYBits[1]);
            }
        }
        public override double Value { get { throw new Exception("Cannot call Value on XYPairs function. Must be indexed."); } }

        public double ValueIndexed(double dX)
        {
            if (X == null)
                DoInitialisation();
            bool DidInterpolate = false;
            return Utility.Math.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
