using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using System.Xml.Serialization;
using Models.Functions;
using System.IO;

namespace Models.PMF.Phen
{
    /// <summary></summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class EmergingPhase : Model, IPhase, ICustomDocumentation
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        [Link]
        Phenology phenology = null;

        /// <summary>Gets or sets the shoot lag.</summary>
        /// <value>The shoot lag.</value>
        [Units("oCd")]
       // [XmlIgnore]
        [Description("ShootLag")]
        public double ShootLag { get; set; }
        /// <summary>Gets or sets the shoot rate.</summary>
        /// <value>The shoot rate</value>
        [Units("oCd/mm")]
       // [XmlIgnore]
        [Description("ShootRate")]
        public double ShootRate { get; set; }

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>The thermal time</summary>
        [Link(IsOptional = true)]
        public IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        /// <summary>Gets the fraction complete.</summary>
        /// <value>The fraction complete.</value>
        [XmlIgnore]
        public double FractionComplete { get; set; }

        /// <summary>Number of days from sowing to end of this phase.</summary>
        [XmlIgnore]
        public int DaysFromSowingToEndPhase { get; set; }

        /// <summary>The property of day unused</summary>
        protected double PropOfDayUnused = 0;
        
        /// <summary>The _ tt for today</summary>
        protected double _TTForToday = 0;

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

        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>The stress</summary>
        [Link(IsOptional = true)]
        public IFunction Stress = null;

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

            if (DaysFromSowingToEndPhase > 0)
            {
                if (phenology.DaysAfterSowing >= DaysFromSowingToEndPhase)
                    PropOfDayUnused = 1.0;
                else
                    PropOfDayUnused = 0.0;
            }
            else if (TTinPhase > Target)
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

            if (PropOfDayUnused > 0)
            {
                Plant.SendEmergingEvent();
                phenology.Emerged = true;
            }

            return PropOfDayUnused;
        }

        /// <summary>Return the target to caller. Can be overridden by derived classes.</summary>
        private double CalcTarget()
        {
            double retVAl = 0;
            if (Plant != null)
                retVAl = ShootLag + Plant.SowingData.Depth * ShootRate;
            return retVAl;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            _TTForToday = 0;
            TTinPhase = 0;
            PropOfDayUnused = 0;
        }

        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
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

                tags.Add(new AutoDocumentation.Paragraph("This phase simulates time to emergence as a function of sowing depth."
                    + " The <i>ThermalTime Target</i> from Sowing to Emergence is given by:<br>"
                    + "&nbsp;&nbsp;&nbsp;&nbsp;*Target = SowingDepth x ShootRate + ShootLag*<br>"
                    + "Where:<br>"
                    + "&nbsp;&nbsp;&nbsp;&nbsp;*ShootRate* = " + ShootRate + " (deg day/mm),<br>"
                    + "&nbsp;&nbsp;&nbsp;&nbsp;*ShootLag* = " + ShootLag + " (deg day), <br>"
                    + "and *SowingDepth* (mm) is sent from the manager with the sowing event.", indent));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                tags.Add(new AutoDocumentation.Paragraph("Progress toward emergence is driven by Thermal time accumulation where thermal time is calculated as:", indent));
                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }


}