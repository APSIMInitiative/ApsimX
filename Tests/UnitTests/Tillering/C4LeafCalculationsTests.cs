using Models.PMF;
using NUnit.Framework;

namespace UnitTests.Tillering
{
    [TestFixture]
    public class C4LeafCalculationsTests
    {
        // Buster Defaults.
        [TestCase(3.58, 0.6, 16, 0, 13.18)]
        // Corner cases
        [TestCase(3.58, 0.0, 16, 0, 3.58)]
        [TestCase(0, 0.0, 16, 0, 0)]
        [TestCase(0, 1000, 0, 0, 0)] // * 0
        public void CalculateLargestLeafPosition_CorrectResults(
            double ax0i,
            double ax0s,
            double finalLeafNo,
            int culmNo,
            double expected
        )
        {
            // Arrange && Act
            double result = C4LeafCalculations.CalculateLargestLeafPosition(ax0i, ax0s, finalLeafNo, culmNo);

            // Assert
            Assert.That(expected, Is.EqualTo(result));
        }
    }
}
