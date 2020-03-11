using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// A special phase that jumps to another phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GotoPhase : Model, IPhase, ICustomDocumentation
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private Phenology phenology = null;


        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>The phase name to goto</summary>
        [Description("PhaseNameToGoto")]
        public string PhaseNameToGoto { get; set; }

        /// <summary>Gets the fraction complete.</summary>
        [XmlIgnore]
        public double FractionComplete { get;}

        /// <summary>Thermal time target</summary>
        [XmlIgnore]
        public double Target { get; set; }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Should not be called in this class</summary>
        public bool DoTimeStep(ref double PropOfDayToUse)
        {
            PropOfDayToUse = 0;
            phenology.SetToStage((double)phenology.IndexFromPhaseName(PhaseNameToGoto)+1);
            return false;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase() {}

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

                // Describe the start and end stages
                tags.Add(new AutoDocumentation.Paragraph("This is a special phase, at " + Start + " the phenology is reset to the " + PhaseNameToGoto + " phase.  ", indent));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }
    }
}
