using Models.Soils;
using Models.Core;
using System;
using Models.Functions;
using System.Linq;
using Models.Soils.Nutrients;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Interface for root zone objects to talk to root shape model
    /// </summary>
    public interface IRootGeometryData
    {
        /// <summary>The soil in this zone</summary>
        Soil Soil { get; set; }

       /// <summary>The parent plant</summary>
        Plant plant { get; set; }

        /// <summary>Gets or sets the depth.</summary>
        [Units("mm")]
        double Depth { get; set; }

        /// <summary>Gets the RootFront</summary>
        double RootLength { get; }

        /// <summary>Gets the RootFront</summary>
        double RootFront { get; set; }
        /// <summary>Gets the RootFront</summary>
        double RootSpread { get; set; }
        /// <summary>Gets the RootFront</summary>
        double LeftDist { get; set; }
        /// <summary>Gets the RootFront</summary>
        double RightDist { get; set; }

        /// <summary>Gets the RootProportions</summary>
        double[] RootProportions { get; set; }

        /// <summary>Gets the LLModifier for leaf angles != RootAngleBase</summary>
        double[] LLModifier { get; set; }

        /// <summary>Soil area occipied by roots</summary>
        [Units("m2")]
        double RootArea { get; set; }
    }
}
