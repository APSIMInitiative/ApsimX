using Models.Core;
using System;

namespace UnitTests
{

    [Serializable]
    class MockModel : Model
    {
        public double Amount { get; set; }

        public double A { get; set; }

        public double B { get; set; }

        public double X { get; set; }

        public double[] Y { get; set; }
        
        public double[] Z { get; set; }
    }
}
