namespace UnitTests
{
    using Models;
    using Models.Core;
    using Models.Soils;
    using System;

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
