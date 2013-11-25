using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml;

namespace Models.PMF.Functions
{
    [Description("Returns a y value from the specified xy maxrix corresponding to the current value of the Xproperty")]
    public class XYPairs : Function
    {
        public double[] X { get; set; }
        public double[] Y { get; set; }

        public override double Value { get { throw new Exception("Cannot call Value on XYPairs function. Must be indexed."); } }

        public double ValueIndexed(double dX)
        {
            bool DidInterpolate = false;
            return Utility.Math.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
