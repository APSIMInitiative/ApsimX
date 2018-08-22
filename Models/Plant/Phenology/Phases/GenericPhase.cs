using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// It uses a <i>ThermalTime Target</i> to determine the duration between development <i>Stages</i>.
    ///   <i>ThermalTime</i> is accumulated until the <i>Target</i> is met and remaining <i>ThermalTime</i> is forwarded to the next phase.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GenericPhase : Model, IPhase, IPhaseWithTarget, ICustomDocumentation
    {

        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [ChildLinkByName]
        private IFunction target = null;

        [ChildLinkByName]
        private IFunction progression = null;


        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary> Return a fraction of phase complete. </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target == 0)
                    return 1;
                else
                    return ProgressThroughPhase / Target;
            }
        }

        /// <summary>Units of progress through phase on this time step.</summary>
        [XmlIgnore]
        public double ProgressionForTimeStep { get; set; }

        /// <summary>Accumulated units of pregress through phase.</summary>
        [XmlIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Target that accumulated progression must meet to proceed to the next phase</summary>
        [XmlIgnore]
        public double Target { get { return target.Value(); } }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary> This function increments pregression toward its target and returns true when the target has been met</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            ProgressionForTimeStep = progression.Value() * propOfDayToUse;
            ProgressThroughPhase += ProgressionForTimeStep;

            if (ProgressThroughPhase > Target)
            {
                if (ProgressionForTimeStep > 0.0)
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = (ProgressThroughPhase - Target) / ProgressionForTimeStep;
                    ProgressionForTimeStep *= (1 - propOfDayToUse);
                }
                ProgressThroughPhase = Target;
            }
            
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { ProgressThroughPhase = 0; }
        
        /// <summary>Writes the summary.</summary>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
            writer.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", Target));
        }

        //7. Private method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

                // Describe the start and end stages
                tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + ".  ", indent));

                // get description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }

}
      
      
