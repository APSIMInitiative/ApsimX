using System.Collections.Generic;
using NUnit.Framework;

namespace UnitTests.DCaPST
{
    public static class RadiationTestData
    {
        public static IEnumerable<TestCaseData> HourlyRadiationTestCases
        {
            get
            {
                yield return new TestCaseData(-2.3, -0.66955090392859185);
                yield return new TestCaseData(24.7, -0.86629147258044892);
            }
        }

        public static IEnumerable<TestCaseData> IncidentRadiationTestCases
        {
            get
            {
                yield return new TestCaseData(0.0, 0.0, -0.88957017994999932);
                yield return new TestCaseData(6, 6.4422088216489629E-05, 0.111379441989282);
                yield return new TestCaseData(12, 0.00055556786827918008, 1.5283606861799228);
                yield return new TestCaseData(18, 6.44220882164897E-05, 0.1113794419892816);
                yield return new TestCaseData(24, 0.0, -0.88957017994999932);
            }
        }

        public static IEnumerable<TestCaseData> DiffuseRadiationTestCases
        {
            get
            {
                yield return new TestCaseData(0.0, 0.0, -0.88957017994999932);
                yield return new TestCaseData(6, 2.60756259519616E-05, 0.111379441989282);
                yield return new TestCaseData(12, 0.00023438879978105451, 1.5283606861799228);
                yield return new TestCaseData(18, 2.6075625951961505E-05, 0.1113794419892816);
                yield return new TestCaseData(24, 0.0, -0.88957017994999932);
            }
        }

        public static IEnumerable<TestCaseData> DirectRadiationTestCases
        {
            get
            {
                yield return new TestCaseData(0.0, 0.0, -0.88957017994999932);
                yield return new TestCaseData(6, 3.8346462264528029E-05, 0.111379441989282);
                yield return new TestCaseData(12, 0.00032117906849812557, 1.5283606861799228);
                yield return new TestCaseData(18, 3.8346462264528192E-05, 0.1113794419892816);
                yield return new TestCaseData(24, 0.0, -0.88957017994999932);
            }
        }

        public static IEnumerable<TestCaseData> DiffuseRadiationParTestCases
        {
            get
            {
                yield return new TestCaseData(0.0, 0.0, -0.88957017994999932);
                yield return new TestCaseData(6, 55.4107051479184, 0.111379441989282);
                yield return new TestCaseData(12, 498.07619953474079, 1.5283606861799228);
                yield return new TestCaseData(18, 55.4107051479182, 0.1113794419892816);
                yield return new TestCaseData(24, 0.0, -0.88957017994999932);
            }
        }

        public static IEnumerable<TestCaseData> DirectRadiationParTestCases
        {
            get
            {
                yield return new TestCaseData(0.0, 0.0, -0.88957017994999932);
                yield return new TestCaseData(6, 87.4299339631239, 0.111379441989282);
                yield return new TestCaseData(12, 732.28827617572631, 1.5283606861799228);
                yield return new TestCaseData(18, 87.42993396312427, 0.1113794419892816);
                yield return new TestCaseData(24, 0.0, -0.88957017994999932);
            }
        }
    }
}
