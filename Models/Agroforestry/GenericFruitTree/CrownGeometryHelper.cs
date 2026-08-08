using System;
using System.Collections.Generic;

namespace Models.Agroforestry
{
    /// <summary>
    /// Class for computing crown geometry properties such as projected area, volume,
    /// and layer segments with support for variable density distributions and shape options.
    /// </summary>
    public static class CrownGeometryHelper
    {
        /// <summary>
        /// Supported crown shapes for geometry calculations.
        /// </summary>
        public enum CrownShape
        {
            /// <summary>
            /// Half ellipsoidal crown shape.
            /// </summary>
            Ellipsoid,
            /// <summary>
            /// Conical crown shape.
            /// </summary>
            Cone,
            /// <summary>
            /// Paraboloid crown shape.
            /// </summary>
            Paraboloid
        }

        /// <summary>
        /// Pre-defined density distributions for leaf area along crown height.
        /// </summary>
        public enum DensityDistribution
        {
            /// <summary>
            /// Uniform distribution (equal leaf density in all layers).
            /// </summary>
            Uniform,
            /// <summary>
            /// Bottom-heavy distribution (higher density near the base).
            /// </summary>
            BottomHeavy,
            /// <summary>
            /// Top-heavy distribution (higher density near the top).
            /// </summary>
            TopHeavy,
            /// <summary>
            /// Mid-peak distribution (highest density in the middle layers).
            /// </summary>
            MidPeak
        }

        /// <summary>
        /// Simple width/depth dimensions of the crown.
        /// </summary>
        public struct CrownDimensions
        {
            /// <summary>
            /// Crown width (horizontal axis).
            /// </summary>
            public double Width;
            /// <summary>
            /// Crown depth (perpendicular horizontal axis).
            /// </summary>
            public double Depth;
        }

        /// <summary>
        /// Represents a single horizontal layer (segment) of the crown.
        /// </summary>
        public class LayerSegment
        {
            /// <summary>
            /// Lower height boundary of the layer (m).
            /// </summary>
            public double Bottom { get; set; }
            /// <summary>
            /// Upper height boundary of the layer (m).
            /// </summary>
            public double Top { get; set; }
            /// <summary>
            /// Projected area of this layer at its midpoint (m^2).
            /// </summary>
            public double ProjectedArea { get; set; }
            /// <summary>
            /// Volume of this layer segment (m^3).
            /// </summary>
            public double Volume { get; set; }
            /// <summary>
            /// Extinction coefficient (m^2/m^2) for this layer.
            /// </summary>
            public double ExtinctionCoefficient { get; set; }
        }

        /// <summary>
        /// Compute crown width and depth from tree height using given ratios.
        /// </summary>
        public static CrownDimensions ComputeCrownDimensions(double height, double widthHeightRatio, double depthHeightRatio)
        {
            return new CrownDimensions
            {
                Width = height * widthHeightRatio,
                Depth = height * depthHeightRatio
            };
        }

        /// <summary>
        /// Calculate projected (ground) area of the crown. Footprint is identical for all shapes.
        /// </summary>
        public static double CalculateProjectedArea(double width, double depth)
        {
            double a = width / 2.0;
            double b = depth / 2.0;
            return Math.PI * a * b;
        }

        /// <summary>
        /// Calculate crown volume for different geometric approximations.
        /// </summary>
        public static double CalculateVolume(double width, double depth, double height, CrownShape shape = CrownShape.Ellipsoid)
        {
            double a = width / 2.0;
            double b = depth / 2.0;
            double c = height;
            switch (shape)
            {
                case CrownShape.Ellipsoid:
                    return (2.0 / 3.0) * Math.PI * a * b * c;
                case CrownShape.Cone:
                    return (1.0 / 3.0) * Math.PI * a * b * c;
                case CrownShape.Paraboloid:
                    return 0.5 * Math.PI * a * b * c;
                default:
                    throw new ArgumentException("Unsupported crown shape");
            }
        }

        /// <summary>
        /// Compute normalized layer fractions based on a pre-defined density distribution.
        /// </summary>
        /// <param name="layerCount">Number of layers.</param>
        /// <param name="distributionType">Selected density distribution type.</param>
        /// <returns>Array of normalized fractions summing to 1.</returns>
        public static double[] ComputeLayerFractions(int layerCount, DensityDistribution distributionType)
        {
            if (layerCount < 1)
                throw new ArgumentOutOfRangeException(nameof(layerCount), "Layer count must be at least one.");

            var distFunc = GetDistributionFunction(distributionType);
            var fractions = new double[layerCount];
            double total = 0.0;
            for (int i = 0; i < layerCount; i++)
            {
                double midRelZ = (i + 0.5) / layerCount;
                fractions[i] = distFunc(midRelZ);
                total += fractions[i];
            }
            for (int i = 0; i < layerCount; i++)
                fractions[i] /= total;
            return fractions;
        }

        /// <summary>
        /// Divide the crown into horizontal segments, computing for each:
        /// bottom/top heights, projected area at midpoint, and volume of the slice.
        /// Supports variable thickness via pre-defined density distributions.
        /// </summary>
        /// <param name="height">Total crown height.</param>
        /// <param name="width">Crown width at base.</param>
        /// <param name="depth">Crown depth at base.</param>
        /// <param name="layerCount">Number of layers.</param>
        /// <param name="shape">Geometric shape approximation.</param>
        /// <param name="distributionType">Density distribution scheme for layer thickness.</param>
        /// <param name="extinctionCoefficient">Extinction coefficient (m^2/m^2) to apply uniformly across all crown layers.</param>
        /// <returns>List of layer segments with area and volume.</returns>
        public static List<LayerSegment> GetLayerSegments(
            double height,
            double width,
            double depth,
            int layerCount,
            CrownShape shape,
            DensityDistribution distributionType,
            double extinctionCoefficient)
        {
            if (height <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            if (layerCount < 1)
                throw new ArgumentOutOfRangeException(nameof(layerCount), "Layer count must be at least one.");

            // Build normalized thickness fractions
            var fractions = ComputeLayerFractions(layerCount, distributionType);
            var segments = new List<LayerSegment>(layerCount);
            double z = 0.0;

            for (int i = 0; i < layerCount; i++)
            {
                double h = fractions[i] * height;
                double bot = z;
                double top = z + h;

                // mid‐slice relative height for scaling radius
                double midRel = ((bot + top) / 2) / height;
                double rf = GetRadiusFactor(midRel, shape);
                double a = (width / 2) * rf;
                double b = (depth / 2) * rf;
                double area = Math.PI * a * b;

                segments.Add(new LayerSegment
                {
                    Bottom = bot,
                    Top = top,
                    ProjectedArea = area,
                    Volume = area * h,
                    ExtinctionCoefficient = extinctionCoefficient
                });

                z += h;
            }

            return segments;
        }

        /// <summary>
        /// Factory for density distribution functions.
        /// </summary>
        private static Func<double, double> GetDistributionFunction(DensityDistribution type)
        {
            switch (type)
            {
                case DensityDistribution.Uniform:
                    return z => 1.0;
                case DensityDistribution.BottomHeavy:
                    return z => 1.0 - z;
                case DensityDistribution.TopHeavy:
                    return z => z;
                case DensityDistribution.MidPeak:
                    return z => 4.0 * z * (1.0 - z);
                default:
                    return z => 1.0;
            }
        }

        /// <summary>
        /// Compute the local radius factor at a relative height (0=base,1=top) for each shape.
        /// </summary>
        private static double GetRadiusFactor(double relZ, CrownShape shape)
        {
            switch (shape)
            {
                case CrownShape.Ellipsoid:
                    return Math.Sqrt(1.0 - relZ * relZ);
                case CrownShape.Cone:
                    return 1.0 - relZ;
                case CrownShape.Paraboloid:
                    return Math.Sqrt(1.0 - relZ);
                default:
                    return 1.0;
            }
        }
    }
}
