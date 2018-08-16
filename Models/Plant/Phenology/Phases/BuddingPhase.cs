using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary> Predicts the date of bud emerging date of pereniel crops </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class BuddingPhase : Model, IPhase, IPhaseWithTarget, ICustomDocumentation
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private IFunction target = null;

        [Link]
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

        /// <summary> This function increments thermal time accumulated in each phase and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and howmuch tt to pass it on the first day.</summary>
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
        public virtual void ResetPhase()  { ProgressThroughPhase = 0; }
        
        /// <summary> Write Summary  /// </summary>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
            if (target != null)
                writer.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", target.Value()));
        }

        //7. Private methode
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


