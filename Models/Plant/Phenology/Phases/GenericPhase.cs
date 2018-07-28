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
    public class GenericPhase : Model, IPhase, ICustomDocumentation
    {

        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Phenology phenology = null;

        [Link]
        private IFunction target = null;

        [Link]
        private IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Gets the t tin phase.</summary>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary> Return a fraction of phase complete. </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target == 0)
                    return 1;
                else
                    return TTinPhase / Target;
            }
            set
            {
                if (phenology != null)
                {
                    TTinPhase = Target * value;
                    phenology.AccumulatedEmergedTT += TTinPhase;
                    phenology.AccumulatedTT += TTinPhase;
                }
            }
        }

        /// <summary>Thermal time target.</summary>
        [XmlIgnore]
        public double Target { get { return target.Value(); } }

        /// <summary>Gets the tt for today.</summary>
        [XmlIgnore]
        public double TTForTimeStep { get; set; }


        //6. Public methode
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// This function increments thermal time accumulated in each phase and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how much tt to pass it on the first day.
        /// </summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            TTForTimeStep = ThermalTime.Value() * propOfDayToUse;
            TTinPhase += TTForTimeStep;
            
            if (TTinPhase > Target)
            {
                if (TTForTimeStep > 0.0)
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = (TTinPhase - Target) / TTForTimeStep;
                }
                TTinPhase = Target;
            }

            return proceedToNextPhase;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { TTinPhase = 0; }
        
        /// <summary>Writes the summary.</summary>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
            writer.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", Target));
        }

        //7. Private method
        //-----------------------------------------------------------------------------------------------------------------

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
      
      
