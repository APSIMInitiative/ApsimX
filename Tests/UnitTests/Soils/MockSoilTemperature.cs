using Models.Core;
using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Soils
{
    internal class MockSoilTemperature : Model, ISoilTemperature
    {
        public double[] Value { get; set; }
    }
}
