using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Phen;

namespace Models.PMF
{
    /// <summary>
    /// A summeriser model
    /// </summary>
    [Serializable]
    public class Summariser : Model
    {
        /// <summary>The above ground</summary>
        [Link] Biomass AboveGround = null;
        //[Link] Biomass BelowGround = null;
        //[Link] Biomass Total       = null;
        //[Link] Biomass TotalLive   = null;
        //[Link] Biomass TotalDead   = null;

        [Link]
        ISummary Summary = null;
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The leaf</summary>
        [Link(IsOptional = true)]
        Leaf Leaf = null;

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhaseChange">The phase change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            string message = Phenology.CurrentPhase.Start + "\r\n";
            if (Leaf != null)
            {
                message += "  LAI = " + Leaf.LAI.ToString("f2") + " (m^2/m^2)" + "\r\n";
                message += "  Above Ground Biomass = " + AboveGround.Wt.ToString("f2") + " (g/m^2)" + "\r\n";
            }
            Summary.WriteMessage(this, message);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (child is CompositeBiomass)
                {
                }
                else
                    child.Document(tags, headingLevel, indent);
            }
        }
    }

}