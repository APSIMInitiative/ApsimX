using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.Xml.Serialization;
using Models.PMF.Struct;
using System.IO;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Leaf appearance phenological phase
    /// </summary>
    [Serializable]
    [Description("This phase extends from the end of the previous phase until the Completion Leaf Number is achieved.  The duration of this phase is determined by leaf appearance rate and the completion leaf number target.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class NodeNumberPhase : Model, IPhase
    {
        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The Number of main-stem leave appeared when the phase ends</summary>
        [Link]
        private IFunction CompletionNodeNumber = null;

        /// <summary>The cohort no at start</summary>
        private double NodeNoAtStart;
        /// <summary>The first</summary>
        private bool First = true;
        /// <summary>The fraction complete yesterday</summary>
        private double FractionCompleteYesterday = 0;

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            _TTForToday = 0;
            TTinPhase = 0;
            PropOfDayUnused = 0;
            NodeNoAtStart = 0;
            FractionCompleteYesterday = 0;
            First = true;
        }

        /// <summary>Do our timestep development</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public double DoTimeStep(double PropOfDayToUse)
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

            if (First)
            {
                NodeNoAtStart = Structure.LeafTipsAppeared;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Structure.LeafTipsAppeared >= CompletionNodeNumber.Value())
                return 0.00001;
            else
                return 0;
        }

        // Return proportion of TT unused
        /// <summary>Adds the tt.</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public double AddTT(double PropOfDayToUse)
        {
            TTinPhase += ThermalTime.Value() * PropOfDayToUse;
            if (First)
            {
                NodeNoAtStart = Structure.LeafTipsAppeared;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Structure.LeafTipsAppeared >= CompletionNodeNumber.Value())
                return 0.00001;
            else
                return 0;
        }

        /// <summary>Return a fraction of phase complete.</summary>
        /// <value>The fraction complete.</value>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                double F = (Structure.LeafTipsAppeared - NodeNoAtStart) / (CompletionNodeNumber.Value() - NodeNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
            set
            {
                throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
        }

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
        [Link(IsOptional = true)]
        public IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.

        /// <summary>The stress</summary>
        [Link(IsOptional = true)]
        public IFunction Stress = null;

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

        /// <summary>Adds the specified DLT_TT.</summary>
        /// <param name="dlt_tt">The DLT_TT.</param>
        virtual public void Add(double dlt_tt) { TTinPhase += dlt_tt; }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
        
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

      
      
