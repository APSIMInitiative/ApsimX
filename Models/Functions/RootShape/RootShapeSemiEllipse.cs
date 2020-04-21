using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.PMF.Organs;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>
    /// This model calculates the proportion of each soil layer occupided by roots.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RootShapeSemiEllipse : Model, IRootShape, ICustomDocumentation
    {
        /// <summary>The Root Angle</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("Degree")]
        private readonly IFunction RootAngle = null;

        /// <summary>Calculates the root area for a layer of soil</summary>
        public void CalcRootProportionInLayers(ZoneState zone)
        {
            for (int layer = 0; layer < zone.soil.Thickness.Length; layer++)
            {
                double prop;
                double top = layer == 0 ? 0 : MathUtilities.Sum(zone.soil.Thickness, 0, layer - 1);
                double bottom = top + zone.soil.Thickness[layer];
                double rootArea;

                if (zone.Depth < top)
                {
                    prop = 0;
                }
                else
                {
                    rootArea = CalcRootAreaSemiEllipse(zone, top, bottom, zone.RightDist);   // Right side
                    rootArea += CalcRootAreaSemiEllipse(zone, top, bottom, zone.LeftDist);   // Left Side

                    double soilArea = (zone.RightDist + zone.LeftDist) * (bottom - top);
                    prop = Math.Max(0.0, MathUtilities.Divide(rootArea, soilArea, 0.0));
                }
                zone.RootProportions[layer] = prop;
            }
        }

        private double DegToRad(double degs)
        {
            return degs * Math.PI / 180.0;
        }

        private double CalcRootAreaSemiEllipse(ZoneState zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            double depth, depthInLayer;

            zone.RootSpread = zone.RootFront * Math.Tan(DegToRad(RootAngle.Value()));   // Semi minor axis

            if (zone.RootFront >= bottom)
            {
                depth = (bottom - top) / 2.0 + top;
                depthInLayer = bottom - top;
            }
            else
            {
                depth = (zone.RootFront - top) / 2.0 + top;
                depthInLayer = zone.RootFront - top;
            }

            double a = Math.Pow(depth - 0.5 * zone.RootFront, 2) / Math.Pow(0.5 * zone.RootFront, 2);
            double xDist = Math.Min(hDist, Math.Sqrt(Math.Pow(zone.RootSpread, 2) * (1 - a)));
            double areaLayer = depthInLayer * xDist;

            return areaLayer;
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // add graph and table.
                //tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " is calculated as a function of daily min and max temperatures, these are weighted toward VPD at max temperature according to the specified MaximumVPDWeight factor.  A value equal to 1.0 means it will use VPD at max temperature, a value of 0.5 means average VPD.</i>", indent));
                //tags.Add(new AutoDocumentation.Paragraph("<i>MaximumVPDWeight = " + MaximumVPDWeight + "</i>", indent));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }
    }
}
