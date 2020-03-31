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
    [ValidParent(ParentType = typeof(Phenology))]
    public class NodeNumberPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Structure structure = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction CompletionNodeNumber = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The cohort no at start</summary>
        private double NodeNoAtStart;
        /// <summary>The first</summary>
        private bool First = true;
        /// <summary>The fraction complete yesterday</summary>
        private double FractionCompleteYesterday = 0;

        
        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                double F = (structure.LeafTipsAppeared - NodeNoAtStart) / (CompletionNodeNumber.Value() - NodeNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
        }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            
            if (First)
            {
                NodeNoAtStart = structure.LeafTipsAppeared;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (structure.LeafTipsAppeared >= CompletionNodeNumber.Value())
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001; //assumes we use most of the Tt today to get to specified node number.  Should be calculated as a function of the phyllochron
            }

            return proceedToNextPhase;
        }

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            NodeNoAtStart = 0;
            FractionCompleteYesterday = 0;
            First = true;
        }

        //7. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)  { ResetPhase(); }
        
        
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
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

      
      
