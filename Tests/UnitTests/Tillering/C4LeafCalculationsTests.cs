using Models.PMF;
using NUnit.Framework;

namespace UnitTests.Tillering
{
    [TestFixture]
    public class C4LeafCalculationsTests
    {
        // Buster Defaults.
        [TestCase(3.58, 0.6, 16, 13.18)]
        public void CalculateLargestLeafPosition_CorrectResults(
            double ax0i, 
            double ax0s, 
            double finalLeafNo, 
            double expected
        )
        {
            // Arrange && Act
            double result = C4LeafCalculations.CalculateLargestLeafPosition(ax0i, ax0s, finalLeafNo);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
