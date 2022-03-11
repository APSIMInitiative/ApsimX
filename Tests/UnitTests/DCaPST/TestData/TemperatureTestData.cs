using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;

namespace UnitTests.DCaPST
{
    public static class TemperatureTestData
    {
        public static IEnumerable<TestCaseData> InvalidTimeTestCases
        {
            get
            {
                yield return new TestCaseData(-2.2);
                yield return new TestCaseData(27.6);
            }
        }

        public static IEnumerable<TestCaseData> ValidTimeTestCases
        {
            get
            {
                yield return new TestCaseData(0, 19.060093267303721);
                yield return new TestCaseData(4.9, 17.152696283098191);
                yield return new TestCaseData(9.5, 22.427634641305584);
                yield return new TestCaseData(16.4, 27.451348375778267);
                yield return new TestCaseData(21.1, 21.453623342308138);
                yield return new TestCaseData(24, 19.060093267303721);
            }
        }
    }
}
