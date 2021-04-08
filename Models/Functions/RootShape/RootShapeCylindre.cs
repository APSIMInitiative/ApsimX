using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;
using APSIM.Shared.Utilities;
using Models.PMF.Organs;

namespace Models.Functions.RootShape
{
    /// <summary>
    /// This model calculates the proportion of each soil layer occupided by roots.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Root))]
    public class RootShapeCylindre : Model, IRootShape
    {
        /// <summary>Calculates the root area for a layer of soil</summary>
        public void CalcRootProportionInLayers(ZoneState zone)
        {
            var physical = zone.Soil.FindChild<Soils.IPhysical>();
            zone.RootArea = (zone.RightDist + zone.LeftDist) * zone.Depth / 1e6;
            for (int layer = 0; layer < physical.Thickness.Length; layer++)
            {
                double prop;
                double top = layer == 0 ? 0 : MathUtilities.Sum(physical.Thickness, 0, layer - 1);

                if (zone.Depth < top)
                {
                    prop = 0;
                } 
                else
                {
                    prop = SoilUtilities.ProportionThroughLayer(physical.Thickness, layer, zone.Depth);
                }
                zone.RootProportions[layer] = prop;
            }
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        protected override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // add graph and table.
                //tags.Add(new AutoDocumentation.Paragraph("<i>" + Name + " is calculated as a function of daily min and max temperatures, these are weighted toward VPD at max temperature according to the specified MaximumVPDWeight factor.  A value equal to 1.0 means it will use VPD at max temperature, a value of 0.5 means average VPD.</i>", indent));
                //tags.Add(new AutoDocumentation.Paragraph("<i>MaximumVPDWeight = " + MaximumVPDWeight + "</i>", indent));

                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }
    }
}
