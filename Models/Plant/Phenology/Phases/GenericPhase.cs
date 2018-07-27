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
    /// 
    /// </summary>
    /// \param Target The thermal time target in this phase.
    /// <remarks>
    /// Generic phase increments daily thermal time accumulated in this phase
    /// to met the \p Target.
    /// The remainder thermal time will pass into the first day of 
    /// next phase by \ref Models.PMF.Phen.Phenology "Phenology" 
    /// function if the phase target is met today.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class GenericPhase : Model, IPhase, ICustomDocumentation
    {
        [Link(IsOptional=true)]
        private IFunction Target = null;

        [Link]
        Phenology phenology = null;

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>The thermal time</summary>
        [Link(IsOptional = true)]
        public IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        /// <summary>The stress</summary>
        [Link(IsOptional = true)]
        public IFunction Stress = null;

        /// <summary>The _ tt for today</summary>
        protected double _TTForToday = 0;

        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>Adds the specified DLT_TT.</summary>
        public void Add(double dlt_tt) { TTinPhase += dlt_tt; }

        /// <summary>The property of day unused</summary>
        protected double PropOfDayUnused = 0;

        /// <summary>
        /// This function increments thermal time accumulated in each phase 
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        public double DoTimeStep(double PropOfDayToUse)
        {

            if (ThermalTime != null)
            {
                _TTForToday = ThermalTime.Value() * PropOfDayToUse;
                if (Stress != null)
                {
                    _TTForToday *= Stress.Value();
                }
                TTinPhase += _TTForToday;
            }

            // Get the Target TT
            double Target = CalcTarget();

            if (TTinPhase > Target)
            {
                double LeftOverValue = TTinPhase - Target;
                if (_TTForToday > 0.0)
                {
                    double PropOfValueUnused = LeftOverValue / ThermalTime.Value();
                    PropOfDayUnused = PropOfValueUnused * PropOfDayToUse;
                }
                else
                    PropOfDayUnused = 1.0;
                TTinPhase = Target;
            }

            return PropOfDayUnused;
        }

        /// <summary>
        /// Return the target to caller. Can be overridden by derived classes.
        /// </summary>
        private double CalcTarget()
        {
            double retVAL = 0;
            if (phenology != null)
            {
                if (Target == null)
                    throw new Exception("Cannot find target for phase: " + Name);
                retVAL = Target.Value();
            }
            return retVAL;
        }
        /// <summary>Return proportion of TT unused</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public double AddTT(double PropOfDayToUse)
        {
            TTinPhase += ThermalTime.Value() * PropOfDayToUse;
            double AmountUnusedTT = TTinPhase - CalcTarget();
            if (AmountUnusedTT > 0)
                return AmountUnusedTT / ThermalTime.Value();
            return 0;
        }
        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                if (CalcTarget() == 0)
                    return 1;
                else
                    return TTinPhase / CalcTarget();
            }
            set
            {
                if (phenology != null)
                {
                    TTinPhase = CalcTarget() * value;
                    phenology.AccumulatedEmergedTT += TTinPhase;
                    phenology.AccumulatedTT += TTinPhase;
                }
            }
        }

        /// <summary>Gets the tt for today.</summary>
        /// <value>The tt for today.</value>
        public double TTForToday
        {
            get
            {
                if (ThermalTime == null)
                    return 0;
                return ThermalTime.Value();
            }
        }

        /// <summary>Writes the summary.</summary>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
            if (Target != null)
                writer.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", Target.Value()));
        }
        
        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            _TTForToday = 0;
            TTinPhase = 0;
            PropOfDayUnused = 0;
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
                tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

                // Describe the start and end stages
                tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + ".  ", indent));

                // get description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                if (Stress != null)
                    tags.Add(new AutoDocumentation.Paragraph("Development is slowed in this phase by multiplying <i>ThermalTime</i> by the value of the <i>Stress</i> function.", indent));

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
      
      
