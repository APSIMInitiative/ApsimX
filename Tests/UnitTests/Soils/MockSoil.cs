using System;
using APSIM.Shared.APSoil;
using Models.Core;

namespace UnitTests.Soils
{


    [Serializable]
    class MockSoil : Model
    {
        public double[] CL { get; set; }

        public double Infiltration { get; set; }

        public double[] NH4 { get; set; }

        public double[] NO3 { get; set; }

        public double PotentialRunoff { get; set; }

        public Soil Properties { get; set; }

        public double[] Water { get; set; }
    }
}
