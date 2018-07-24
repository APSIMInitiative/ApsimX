using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
//using System.ComponentModel;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;


namespace Models.PMF.Phen
{
    /// <summary>
    /// A base model representing a phenological phase。
    /// </summary>
     




    [Serializable]
    [ValidParent(ParentType = typeof(Phenology))]
    abstract public class Phase : Model, ICustomDocumentation
    {
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>The phenology</summary>
        [Link]
        protected Phenology Phenology = null;

        /// <summary>The thermal time</summary>
        //[Link(IsOptional = true)]
        //public IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        /// <summary>The stress</summary>
        [Link(IsOptional = true)]
        public IFunction Stress = null;

        /// <summary>The property of day unused</summary>
        protected double PropOfDayUnused = 0;
        /// <summary>The _ tt for today</summary>
                
        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>
        /// This function increments thermal time accumulated in each phase
        /// and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how
        /// much tt to pass it on the first day.
        /// </summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        virtual public double DoTimeStep(double PropOfDayToUse)
        {
            // Calculate the TT for today and Accumulate.      
            if (ThermalTime != null)
            {
                _TTForToday = ThermalTime.Value() * PropOfDayToUse;
                if (Stress != null)
                {
                    _TTForToday *= Stress.Value();
                }
                TTinPhase += _TTForToday;
            }
            return PropOfDayUnused;
        }

        // Return proportion of TT unused
        /// <summary>Adds the tt.</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        virtual public double AddTT(double PropOfDayToUse)
        {
            TTinPhase += ThermalTime.Value() * PropOfDayToUse;
            return 0;
        }
        /// <summary>Adds the specified DLT_TT.</summary>
        /// <param name="dlt_tt">The DLT_TT.</param>
        virtual public void Add(double dlt_tt) { TTinPhase += dlt_tt; }
        /// <summary>Gets the fraction complete.</summary>
        /// <value>The fraction complete.</value>
        [XmlIgnore]
        abstract public double FractionComplete { get; set; }

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
        internal virtual void WriteSummary(TextWriter writer)
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

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // get description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
