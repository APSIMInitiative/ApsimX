using NUnit.Framework;
using APSIM.Shared.Graphing;
using APSIM.Interop.Graphing;
using OxyPlot;
using LegendOrientation = APSIM.Shared.Graphing.LegendOrientation;
using LegendPosition = APSIM.Shared.Graphing.LegendPosition;
using MarkerType = APSIM.Shared.Graphing.MarkerType;
using OxyAxisPosition = OxyPlot.Axes.AxisPosition;
using System.Drawing;

namespace UnitTests.Graphing.SeriesExporters
{
    /// <summary>
    /// Tests for <see cref="EnumerationExtensions"/>, to ensure that apsim
    /// graphing enumerations are converted into the correct oxyplot equivalents.
    /// </summary>
    [TestFixture]
    public class EnumerationExtensionTests
    {
        /// <summary>
        /// Convert the input linetype to an oxyplot linestyle and ensure that
        /// the result matches the expected value.
        /// </summary>
        /// <param name="input">Input line type.</param>
        /// <param name="expectedOutput">Expected output.</param>
        [TestCase(LineType.Dash, LineStyle.Dash)]
        [TestCase(LineType.DashDot, LineStyle.DashDot)]
        [TestCase(LineType.Dot, LineStyle.Dot)]
        [TestCase(LineType.None, LineStyle.None)]
        [TestCase(LineType.Solid, LineStyle.Solid)]
        public void TestLineStyleConversion(LineType input, LineStyle expectedOutput)
        {
            Assert.That(input.ToOxyPlotLineStyle(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Convert the input thickness to an oxyplot numeric value and ensure that
        /// the result matches the expected value.
        /// </summary>
        /// <param name="input">Input line type.</param>
        /// <param name="expectedOutput">Expected output.</param>
        [TestCase(LineThickness.Normal, 0.5)]
        [TestCase(LineThickness.Thin, 0.25)]
        public void TestLineThicknessConversion(LineThickness input, double expectedOutput)
        {
            Assert.That(input.ToOxyPlotThickness(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Convert the input marker type to an oxyplot marker type and ensure that
        /// the result matches the expected value.
        /// </summary>
        /// <param name="input">Input marker type.</param>
        /// <param name="expectedOutput">Expected output.</param>
        [TestCase(MarkerType.Circle, OxyPlot.MarkerType.Circle)]
        [TestCase(MarkerType.Cross, OxyPlot.MarkerType.Cross)]
        [TestCase(MarkerType.Diamond, OxyPlot.MarkerType.Diamond)]
        [TestCase(MarkerType.FilledCircle, OxyPlot.MarkerType.Circle)]
        [TestCase(MarkerType.FilledDiamond, OxyPlot.MarkerType.Diamond)]
        [TestCase(MarkerType.FilledSquare, OxyPlot.MarkerType.Square)]
        [TestCase(MarkerType.FilledTriangle, OxyPlot.MarkerType.Triangle)]
        [TestCase(MarkerType.None, OxyPlot.MarkerType.None)]
        [TestCase(MarkerType.Plus, OxyPlot.MarkerType.Plus)]
        [TestCase(MarkerType.Square, OxyPlot.MarkerType.Square)]
        [TestCase(MarkerType.Star, OxyPlot.MarkerType.Star)]
        [TestCase(MarkerType.Triangle, OxyPlot.MarkerType.Triangle)]
        public void TestMarkerTypeConversion(MarkerType input, OxyPlot.MarkerType expectedOutput)
        {
            Assert.That(input.ToOxyPlotMarkerType(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Convert the input marker size to a numeric value and ensure that the
        /// result matches the expected value.
        /// </summary>
        /// <param name="input">Input marker size.</param>
        /// <param name="expectedOutput">Expected output.</param>
        [TestCase(MarkerSize.VerySmall, 3)]
        [TestCase(MarkerSize.Small, 5)]
        [TestCase(MarkerSize.Normal, 7)]
        [TestCase(MarkerSize.Large, 9)]
        public void TestMarkerSizeConversion(MarkerSize input, double expectedOutput)
        {
            Assert.That(input.ToOxyPlotMarkerSize(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Convert the input legend orientation to an oxyplot legend orientation
        /// and ensure that the result matches the expected value.
        /// </summary>
        /// <param name="input">Input legend orientation.</param>
        /// <param name="expectedOutput">Expected output.</param>
        [TestCase(LegendOrientation.Horizontal, OxyPlot.Legends.LegendOrientation.Horizontal)]
        [TestCase(LegendOrientation.Vertical, OxyPlot.Legends.LegendOrientation.Vertical)]
        public void TestLegendOrientationConversion(LegendOrientation input, OxyPlot.Legends.LegendOrientation expectedOutput)
        {
            Assert.That(input.ToOxyPlotLegendOrientation(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Convert the input legend position to an oxyplot legend position and ensure
        /// that the result matches the expected value.
        /// </summary>
        /// <param name="input">Input legend position.</param>
        /// <param name="expectedOutput">Expected output.</param>
        [TestCase(LegendPosition.TopLeft, OxyPlot.Legends.LegendPosition.TopLeft)]
        [TestCase(LegendPosition.TopCenter, OxyPlot.Legends.LegendPosition.TopCenter)]
        [TestCase(LegendPosition.TopRight, OxyPlot.Legends.LegendPosition.TopRight)]
        [TestCase(LegendPosition.BottomLeft, OxyPlot.Legends.LegendPosition.BottomLeft)]
        [TestCase(LegendPosition.BottomCenter, OxyPlot.Legends.LegendPosition.BottomCenter)]
        [TestCase(LegendPosition.BottomRight, OxyPlot.Legends.LegendPosition.BottomRight)]
        [TestCase(LegendPosition.LeftTop, OxyPlot.Legends.LegendPosition.LeftTop)]
        [TestCase(LegendPosition.LeftMiddle, OxyPlot.Legends.LegendPosition.LeftMiddle)]
        [TestCase(LegendPosition.LeftBottom, OxyPlot.Legends.LegendPosition.LeftBottom)]
        [TestCase(LegendPosition.RightTop, OxyPlot.Legends.LegendPosition.RightTop)]
        [TestCase(LegendPosition.RightMiddle, OxyPlot.Legends.LegendPosition.RightMiddle)]
        [TestCase(LegendPosition.RightBottom, OxyPlot.Legends.LegendPosition.RightBottom)]
        public void TestLegendPositionConversion(LegendPosition input, OxyPlot.Legends.LegendPosition expectedOutput)
        {
            Assert.That(input.ToOxyPlotLegendPosition(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Ensure that <see cref="EnumerationExtensions.ToOxyAxisPosition(AxisPosition)"/>
        /// works correctly for all possible input values.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="expectedOutput"></param>
        [TestCase(AxisPosition.Bottom, OxyAxisPosition.Bottom)]
        [TestCase(AxisPosition.Left, OxyAxisPosition.Left)]
        [TestCase(AxisPosition.Top, OxyAxisPosition.Top)]
        [TestCase(AxisPosition.Right, OxyAxisPosition.Right)]
        public void TestAxisPositionConversion(AxisPosition position, OxyAxisPosition expectedOutput)
        {
            Assert.That(position.ToOxyAxisPosition(), Is.EqualTo(expectedOutput));
        }

        /// <summary>
        /// Ensure that System.Drawing.Color values are converted to the correct
        /// oxyplot colour.
        /// </summary>
        [Test]
        public void TestColourConversion()
        {
            TestColourConversion(Color.Black, OxyColors.Black);
            TestColourConversion(Color.White, OxyColors.White);
            TestColourConversion(Color.Red, OxyColors.Red);
            TestColourConversion(Color.Green, OxyColors.Green);
            TestColourConversion(Color.Blue, OxyColors.Blue);
        }

        /// <summary>
        /// Convert the input colour to an oxyplot colour and ensure that the
        /// result matches the expected value.
        /// </summary>
        /// <param name="input">Input colour.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestColourConversion(Color input, OxyColor expectedOutput)
        {
            Assert.That(input.ToOxyColour(), Is.EqualTo(expectedOutput));
        }
    }
}
