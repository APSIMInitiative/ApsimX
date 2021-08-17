using NUnit.Framework;
using APSIM.Services.Graphing;
using APSIM.Interop.Graphing;
using OxyPlot;
using LegendOrientation = APSIM.Services.Graphing.LegendOrientation;
using LegendPosition = APSIM.Services.Graphing.LegendPosition;
using MarkerType = APSIM.Services.Graphing.MarkerType;
using System.Drawing;

namespace APSIM.Tests.Graphing.SeriesExporters
{
    /// <summary>
    /// Tests for <see cref="EnumerationExtension"/>, to ensure that apsim
    /// graphing enumerations are converted into the correct oxyplot equivalents.
    /// </summary>
    [TestFixture]
    public class EnumerationExtensionTests
    {
        /// <summary>
        /// Ensure that apsim line types convert to the correct oxyplot line styles.
        /// </summary>
        [Test]
        public void TestLineStyleConversion()
        {
            TestLineStyleConversion(LineType.Dash, LineStyle.Dash);
            TestLineStyleConversion(LineType.DashDot, LineStyle.DashDot);
            TestLineStyleConversion(LineType.Dot, LineStyle.Dot);
            TestLineStyleConversion(LineType.None, LineStyle.None);
            TestLineStyleConversion(LineType.Solid, LineStyle.Solid);
        }

        /// <summary>
        /// Convert the input linetype to an oxyplot linestyle and ensure that
        /// the result matches the expected value.
        /// </summary>
        /// <param name="input">Input line type.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestLineStyleConversion(LineType input, LineStyle expectedOutput)
        {
            Assert.AreEqual(expectedOutput, input.ToOxyPlotLineStyle());
        }

        /// <summary>
        /// Ensure that apsim line thicknesses convert to the correct oxyplot line thickness.
        /// </summary>
        [Test]
        public void TestLineThicknessConversion()
        {
            TestLineThicknessConversion(LineThickness.Normal, 0.5);
            TestLineThicknessConversion(LineThickness.Thin, 0.25);
        }

        /// <summary>
        /// Convert the input thickness to an oxyplot numeric value and ensure that
        /// the result matches the expected value.
        /// </summary>
        /// <param name="input">Input line type.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestLineThicknessConversion(LineThickness input, double expectedOutput)
        {
            Assert.AreEqual(expectedOutput, input.ToOxyPlotThickness());
        }

        /// <summary>
        /// Ensure that apsim marker types convert to the correct oxyplot line thickness.
        /// </summary>
        [Test]
        public void TestMarkerTypeConversion()
        {
            // Series with un-filled markers should have marker colour set to undefined.
            TestMarkerTypeConversion(MarkerType.Circle, OxyPlot.MarkerType.Circle);
            TestMarkerTypeConversion(MarkerType.Cross, OxyPlot.MarkerType.Cross);
            TestMarkerTypeConversion(MarkerType.Diamond, OxyPlot.MarkerType.Diamond);
            TestMarkerTypeConversion(MarkerType.FilledCircle, OxyPlot.MarkerType.Circle);
            TestMarkerTypeConversion(MarkerType.FilledDiamond, OxyPlot.MarkerType.Diamond);
            TestMarkerTypeConversion(MarkerType.FilledSquare, OxyPlot.MarkerType.Square);
            TestMarkerTypeConversion(MarkerType.FilledTriangle, OxyPlot.MarkerType.Triangle);
            TestMarkerTypeConversion(MarkerType.None, OxyPlot.MarkerType.None);
            TestMarkerTypeConversion(MarkerType.Plus, OxyPlot.MarkerType.Plus);
            TestMarkerTypeConversion(MarkerType.Square, OxyPlot.MarkerType.Square);
            TestMarkerTypeConversion(MarkerType.Star, OxyPlot.MarkerType.Star);
            TestMarkerTypeConversion(MarkerType.Triangle, OxyPlot.MarkerType.Triangle);
        }

        /// <summary>
        /// Convert the input marker type to an oxyplot marker type and ensure that
        /// the result matches the expected value.
        /// </summary>
        /// <param name="input">Input marker type.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestMarkerTypeConversion(MarkerType input, OxyPlot.MarkerType expectedOutput)
        {
            Assert.AreEqual(expectedOutput, input.ToOxyPlotMarkerType());
        }

        /// <summary>
        /// Ensure that apsim marker sizes convert to the correct numeric value.
        /// </summary>
        [Test]
        public void TestMarkerSizeConversion()
        {
            TestMarkerSizeConversion(MarkerSize.VerySmall, 3);
            TestMarkerSizeConversion(MarkerSize.Small, 5);
            TestMarkerSizeConversion(MarkerSize.Normal, 7);
            TestMarkerSizeConversion(MarkerSize.Large, 9);
        }

        /// <summary>
        /// Convert the input marker size to a numeric value and ensure that the
        /// result matches the expected value.
        /// </summary>
        /// <param name="input">Input marker size.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestMarkerSizeConversion(MarkerSize input, double expectedOutput)
        {
            Assert.AreEqual(expectedOutput, input.ToOxyPlotMarkerSize());
        }

        /// <summary>
        /// Ensure that apsim legend orientations convert to the correct oxyplot
        /// legend orietnation.
        /// </summary>
        [Test]
        public void TestLegendOrientationConversion()
        {
            TestLegendOrientationConversion(LegendOrientation.Horizontal, OxyPlot.LegendOrientation.Horizontal);
            TestLegendOrientationConversion(LegendOrientation.Vertical, OxyPlot.LegendOrientation.Vertical);
        }

        /// <summary>
        /// Convert the input legend orientation to an oxyplot legend orientation
        /// and ensure that the result matches the expected value.
        /// </summary>
        /// <param name="input">Input legend orientation.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestLegendOrientationConversion(LegendOrientation input, OxyPlot.LegendOrientation expectedOutput)
        {
            Assert.AreEqual(expectedOutput, input.ToOxyPlotLegendOrientation());
        }

        /// <summary>
        /// Ensure that apsim legend positions convert to the correct oxyplot
        /// legend position.
        /// </summary>
        [Test]
        public void TestLegendPositionConversion()
        {
            TestLegendPositionConversion(LegendPosition.TopLeft, OxyPlot.LegendPosition.TopLeft);
            TestLegendPositionConversion(LegendPosition.TopCenter, OxyPlot.LegendPosition.TopCenter);
            TestLegendPositionConversion(LegendPosition.TopRight, OxyPlot.LegendPosition.TopRight);
            TestLegendPositionConversion(LegendPosition.BottomLeft, OxyPlot.LegendPosition.BottomLeft);
            TestLegendPositionConversion(LegendPosition.BottomCenter, OxyPlot.LegendPosition.BottomCenter);
            TestLegendPositionConversion(LegendPosition.BottomRight, OxyPlot.LegendPosition.BottomRight);
            TestLegendPositionConversion(LegendPosition.LeftTop, OxyPlot.LegendPosition.LeftTop);
            TestLegendPositionConversion(LegendPosition.LeftMiddle, OxyPlot.LegendPosition.LeftMiddle);
            TestLegendPositionConversion(LegendPosition.LeftBottom, OxyPlot.LegendPosition.LeftBottom);
            TestLegendPositionConversion(LegendPosition.RightTop, OxyPlot.LegendPosition.RightTop);
            TestLegendPositionConversion(LegendPosition.RightMiddle, OxyPlot.LegendPosition.RightMiddle);
            TestLegendPositionConversion(LegendPosition.RightBottom, OxyPlot.LegendPosition.RightBottom);
        }

        /// <summary>
        /// Convert the input legend position to an oxyplot legend position and ensure
        /// that the result matches the expected value.
        /// </summary>
        /// <param name="input">Input legend position.</param>
        /// <param name="expectedOutput">Expected output.</param>
        private void TestLegendPositionConversion(LegendPosition input, OxyPlot.LegendPosition expectedOutput)
        {
            Assert.AreEqual(expectedOutput, input.ToOxyPlotLegendPosition());
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
            Assert.AreEqual(expectedOutput, input.ToOxyColour());
        }
    }
}
