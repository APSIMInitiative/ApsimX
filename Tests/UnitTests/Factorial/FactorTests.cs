using System;
using Models.Core;
using Models.Factorial;
using NUnit.Framework;

namespace UnitTests.Factorial
{
    [TestFixture]
    public class FactorTests
    {
        /// <summary>
        /// Ensure that an unbounded "step" factor causes an exception. This
        /// reproduces bug #7019.
        /// 
        /// https://github.com/APSIMInitiative/ApsimX/issues/7019
        /// </summary>
        /// <remarks>
        /// This previously resulted in an infinite loop, so I've set ta 1s
        /// timeout on the test method.
        /// </remarks>
        [TestCase("x = 0 to 10 step -1")]
        [TestCase("x = 0 to -10 step 1")]
        [CancelAfter(1_000)]
        public void TestUnboundedStep(string spec)
        {
            Factor factor = new Factor();
            factor.Specification = spec;
            Assert.Throws<InvalidOperationException>(() => factor.GetCompositeFactors());
        }
    }
}
