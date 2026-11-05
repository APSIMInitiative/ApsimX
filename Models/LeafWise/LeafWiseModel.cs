using APSIM.Core;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Struct;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Display = Models.Core.DisplayAttribute;

namespace Models.LeafWise
{
    /// <summary>
    /// Calculates the length, width, and area of individual C4 leaves.
    /// Add this model to a zone to opt a crop into the LeafWise calculation.
    /// Reference: https://doi.org/10.1093/aob/mcaf328
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(typeof(Zone))]
    public class LeafWiseModel : Model, IStructureDependency
    {
        private readonly SortedDictionary<int, double> mainCulmWidths = new();
        private readonly SortedDictionary<int, double> mainCulmLengths = new();

        /// <summary>Structure instance supplied by APSIM.Core.</summary>
        [field: NonSerialized]
        public IStructure Structure { get; set; }

        /// <summary>The crop for which LeafWise will be used.</summary>
        [Description("The crop for which LeafWise will run")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetPlantNames))]
        public string CropName { get; set; }

        /// <summary>
        /// Maximum rate parameter for leaf width. This is the original LeafWise
        /// <c>cm</c> coefficient (called <c>CMw</c> in the prototype C# code).
        /// </summary>
        [Description("Leaf width maximum rate")]
        [Units("mm")]
        [Range(0.0, double.MaxValue)]
        public double MaximumWidthRate { get; set; } = 11.7214951252813;

        /// <summary>Calculated widths of leaves on the main culm, in leaf-number order.</summary>
        [JsonIgnore]
        public double[] LeafWidthsMain => [.. mainCulmWidths.Values];

        /// <summary>Calculated lengths of leaves on the main culm, in leaf-number order.</summary>
        [JsonIgnore]
        public double[] LeafLengthsMain => [.. mainCulmLengths.Values];

        /// <summary>Returns true when this model should replace leaf area for the supplied plant.</summary>
        public bool AppliesTo(Plant plant)
        {
            return Enabled && plant != null && string.Equals(CropName, plant.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Calculates an individual leaf area (mm2).</summary>
        public double CalculateIndividualLeafArea(double leafNumber, Culm culm)
        {
            return CalculateIndividualLeafArea(leafNumber, culm, leafNumber);
        }

        /// <summary>Calculates an individual leaf area (mm2) and records it against the unadjusted leaf number.</summary>
        public double CalculateIndividualLeafArea(double leafNumber, Culm culm, double reportedLeafNumber)
        {
            double length = CalculateLeafDimension(LeafDimension.Length, leafNumber, culm.FinalLeafNo);
            double width = CalculateLeafDimension(LeafDimension.Width, leafNumber, culm.FinalLeafNo);

            if (culm.CulmNo == 0 && Math.Abs(reportedLeafNumber - Math.Round(reportedLeafNumber)) < 1e-9)
            {
                int leaf = (int)Math.Round(reportedLeafNumber);
                mainCulmLengths[leaf] = length;
                mainCulmWidths[leaf] = width;
            }

            double shapeFactor = leafNumber < culm.FinalLeafNo ? 0.71 : 0.635;
            return Math.Max(length * width * shapeFactor, 0.0);
        }

        /// <summary>Calculates leaf length or width (mm).</summary>
        public double CalculateLeafDimension(LeafDimension dimension, double leafNumber, double finalLeafNumber)
        {
            Parameters parameters = dimension == LeafDimension.Length
                ? Parameters.Length
                : Parameters.Width(MaximumWidthRate);

            double transition = finalLeafNumber < parameters.TransitionBreakpoint
                ? parameters.TransitionIntercept + parameters.TransitionSlope * finalLeafNumber
                : parameters.TransitionIntercept + parameters.TransitionSlope * parameters.TransitionBreakpoint
                    + parameters.TransitionTailSlope * (finalLeafNumber - parameters.TransitionBreakpoint);

            double decline = (Math.Min(finalLeafNumber, parameters.DeclineBreakpoint) * parameters.DeclineSlope)
                             + parameters.DeclineIntercept;
            double maximum = Logistic(parameters, transition);
            double upperLevel = maximum + parameters.UpperLevelOffset;

            if (leafNumber < transition)
                return Logistic(parameters, leafNumber);

            return (maximum * upperLevel) /
                   (maximum + (upperLevel - maximum) * Math.Exp(decline * (leafNumber - transition)));
        }

        /// <summary>Clear reported values at sowing.</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            if (AppliesTo(data.Plant))
            {
                mainCulmWidths.Clear();
                mainCulmLengths.Clear();
            }
        }

        private static double Logistic(Parameters parameters, double leafNumber)
        {
            return (parameters.MaximumRate / parameters.Rate) *
                   Math.Log(1 + Math.Exp(parameters.Rate * (leafNumber - parameters.BaseLeafNumber - 1)))
                   + parameters.FirstLeafSize;
        }

        private IEnumerable<string> GetPlantNames()
        {
            return Structure?.FindAll<IPlant>()
                .Select(plant => plant.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct() ?? [];
        }

        /// <summary>A leaf dimension calculated by LeafWise.</summary>
        public enum LeafDimension
        {
            /// <summary>Leaf length.</summary>
            Length,
            /// <summary>Leaf width.</summary>
            Width
        }

        // Names used here map to the original LeafWise/R coefficients as follows:
        // TransitionIntercept = xs.a; TransitionSlope = xs.b;
        // TransitionBreakpoint = xs.bp; TransitionTailSlope = xs.c;
        // BaseLeafNumber = tb; FirstLeafSize = L1; Rate = rm;
        // MaximumRate = cm; UpperLevelOffset = dsl;
        // DeclineIntercept = dr.a; DeclineSlope = dr.b;
        // DeclineBreakpoint = dr.xs.
        private readonly record struct Parameters(
            double TransitionIntercept,
            double TransitionSlope,
            double TransitionBreakpoint,
            double TransitionTailSlope,
            double BaseLeafNumber,
            double FirstLeafSize,
            double Rate,
            double MaximumRate,
            double UpperLevelOffset,
            double DeclineIntercept,
            double DeclineSlope,
            double DeclineBreakpoint)
        {
            internal static Parameters Length => new(
                4.63675634607185, 0.464274995216371, 20.5, 0.264019424636425,
                1.92263965763877, 17.5892507288144, 1.56561064388568, 70.5000773569451,
                3.49990943010694, 2.62071562339733, -0.0827519027915431, 27.0238146959851);

            internal static Parameters Width(double maximumWidthRate) => new(
                7.31219363021202, 0.250153450025153, 20.5, 0.202106303769515,
                4.03721892732686, 7.05775847039688, 0.614111400681646, maximumWidthRate,
                0.182013403559016, 1.78334828389108, -0.0477300157216497, 28.9990446426163);
        }
    }
}
