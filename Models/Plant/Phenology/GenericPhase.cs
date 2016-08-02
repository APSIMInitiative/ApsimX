using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using System.IO;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// It uses a <i>ThermalTime Target</i> to determine the duration between development <i>Stages</i>.
    /// <i>ThermalTime</i> is accumulated until the <i>Target</i> is met and remaining <i>ThermalTime</i> is forwarded to the next phase.
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
    public class GenericPhase : Phase
    {
        [Link(IsOptional=true)]
        private IFunction Target = null;

        /// <summary>
        /// This function increments thermal time accumulated in each phase 
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        public override double DoTimeStep(double PropOfDayToUse)
        {

            base.DoTimeStep(PropOfDayToUse);

            // Get the Target TT
            double Target = CalcTarget();


            if (TTinPhase > Target)
            {
                double LeftOverValue = TTinPhase - Target;
                if (_TTForToday > 0.0)
                {
                    double PropOfValueUnused = LeftOverValue / ThermalTime.Value;
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
        public virtual double CalcTarget()
        {
            double retVAL = 0;
            if (Phenology != null)
            {
                if (Target == null)
                    throw new Exception("Cannot find target for phase: " + Name);
                retVAL = Target.Value;
            }
            return retVAL;
        }
        /// <summary>Return proportion of TT unused</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double AddTT(double PropOfDayToUse)
        {
            TTinPhase += ThermalTime.Value * PropOfDayToUse;
            double AmountUnusedTT = TTinPhase - CalcTarget();
            if (AmountUnusedTT > 0)
                return AmountUnusedTT / ThermalTime.Value;
            return 0;
        }
        /// <summary>
        /// Return a fraction of phase complete.
        /// </summary>
        [XmlIgnore]
        public override double FractionComplete
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
                if (Phenology != null)
                {
                    TTinPhase = CalcTarget() * value;
                    Phenology.AccumulatedEmergedTT += TTinPhase;
                    Phenology.AccumulatedTT += TTinPhase;
                }
            }
        }

        internal override void WriteSummary(TextWriter writer)
        {
            base.WriteSummary(writer);
            if (Target != null)
                writer.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", Target.Value));
        }
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

            // Describe the start and end stages
            tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + ".  ", indent));

            // get description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);

            if (Stress != null)
                tags.Add(new AutoDocumentation.Paragraph("Development is slowed in this phase by multiplying <i>ThermalTime</i> by the value of the <i>Stress</i> function.", indent));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // write children.
            foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                child.Document(tags, -1, indent);
        }
    }

}
      
      
