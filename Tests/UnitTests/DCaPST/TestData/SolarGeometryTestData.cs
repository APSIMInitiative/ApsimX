using System.Collections.Generic;
using NUnit.Framework;
using Models.DCAPST.Interfaces;
using Moq;

namespace UnitTests.DCaPST
{
    public static class SolarGeometryTestData
    {
        public static IEnumerable<TestCaseData> SunAngleTestCases
        {
            get
            {
                yield return new TestCaseData(1.0, -48.291971830796477);
                yield return new TestCaseData(6.5, 13.12346022737003);
                yield return new TestCaseData(12.7, 79.811435134587384);
                yield return new TestCaseData(22.8, -47.167281573443965);
            }
        }
    }
}
