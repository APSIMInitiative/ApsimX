using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.PMF.Organs;

namespace Models.Functions
{
    /// <summary>
    /// This Function calculates a mean daily VPD from Max and Min weighted toward Max according to the specified MaximumVPDWeight factor.  
    /// This is then passed into the XY matrix as the x property and the function returns the y value
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class RootShape : Model, IFunction, ICustomDocumentation
    {
        #region Class Data Members

        /// <summary>The maximum temperature weighting</summary>
        [Description("The shape of root system (0: cylindre, 1: semi-ellipse, 2: semi-circle (RootAngle=45), 3: semi-circle (Sorghum and Maize)")]
        public double Type { get; set; } = 0;

        /// <summary>The maximum temperature weighting</summary>
        [Description("Root angle")]
        [Units("Degree")]
        public double RootAngle { get; set; } = 45;

        #endregion

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return arrayIndex;
        }

        /// <summary>Calculates the root area for a layer of soil</summary>
        public double CalcRootArea(ZoneState zone, int layer, double top, double bottom, double hDist)
        {
            if (Type == 0)
            {
                return zone.soil.ProportionThroughLayer(layer, zone.Depth) * hDist * (bottom - top);
            }
            else if (Type == 1)
            {
                return CalcRootAreaSemiEllipse(zone, top, bottom, hDist);
            }
            else if (Type == 2)
            {
                return CalcRootAreaSemiCircle(zone, top, bottom, hDist);
            }
            else if (Type == 3)
            {
                return CalcRootAreaSemiCircleMaize(zone, top, bottom, hDist);
            }
            else
            {
                throw new Exception("The root shape is not specified!");
            }
        }

        double DegToRad(double degs)
        {
            return degs * Math.PI / 180.0;
        }

        double CalcRootAreaSemiEllipse(ZoneState zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            double depth, depthInLayer;

            zone.RootSpread = zone.RootFront * Math.Tan(DegToRad(RootAngle));   // Semi minor axis

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

        double CalcRootAreaSemiCircle(ZoneState zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            double depth, depthInLayer;

            zone.RootSpread = zone.RootFront * Math.Tan(DegToRad(RootAngle));   // Semi minor axis

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

            // Ben (2020.04.19): The first line does not take into account the coordinate of the centre of the circl, which is (0, 0.5*zone.RootFront).
            // double xDist = Math.Min(hDist, zone.RootSpread * Math.Sqrt(1 - (Math.Pow(depth, 2) / Math.Pow(zone.RootFront, 2))));
            double xDist = Math.Min(hDist, zone.RootSpread * Math.Sqrt(1 - (Math.Pow(depth - 0.5 * zone.RootFront, 2) / Math.Pow(0.5 * zone.RootFront, 2))));
            double areaLayer = depthInLayer * xDist;

            return areaLayer;
        }

        double CalcRootAreaSemiCircleMaize(ZoneState zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            // get the area occupied by roots in a semi-circular section between top and bottom
            double SDepth, areaLayer;

            // intersection of roots and Section
            if (zone.RootFront <= hDist)
                SDepth = 0.0;
            else
                SDepth = Math.Sqrt(Math.Pow(zone.RootFront, 2) - Math.Pow(hDist, 2));

            // Rectangle - SDepth past bottom of this area
            if (SDepth >= bottom)
                areaLayer = (bottom - top) * hDist;
            else               // roots Past top
            {
                double Theta = 2 * Math.Acos(MathUtilities.Divide(Math.Max(top, SDepth), zone.RootFront, 0));
                double topArea = (Math.Pow(zone.RootFront, 2) / 2.0 * (Theta - Math.Sin(Theta))) / 2.0;

                // bottom down
                double bottomArea = 0;
                if (zone.RootFront > bottom)
                {
                    Theta = 2 * Math.Acos(bottom / zone.RootFront);
                    bottomArea = (Math.Pow(zone.RootFront, 2) / 2.0 * (Theta - Math.Sin(Theta))) / 2.0;
                }
                // rectangle
                if (SDepth > top) topArea += (SDepth - top) * hDist;
                areaLayer = topArea - bottomArea;
            }
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
