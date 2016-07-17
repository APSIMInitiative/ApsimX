using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase extends from the end of the previous phase until the final main-stem leaf has finished expansion.  The duration of this phase is determined by leaf appearance rate and the final main stem node number.  
    /// As such, the model parameterisation of leaf appearance and final leaf number (set in the Structure object) are important for predicting the duration of the crop correctly.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LeafAppearancePhase : Phase
    {
        /// <summary>The leaf</summary>
        [Link]
        Leaf Leaf = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The cohort no at start</summary>
        private double CohortNoAtStart;
        /// <summary>The first</summary>
        private bool First = true;
        /// <summary>The remaining leaves</summary>
        private double RemainingLeaves = 0;
        /// <summary>The fraction complete yesterday</summary>
        private double FractionCompleteYesterday = 0;

        /// <summary>Reset phase</summary>
        public override void ResetPhase()
        {
            base.ResetPhase();
            CohortNoAtStart = 0;
            RemainingLeaves = 0;
            FractionCompleteYesterday = 0;
            First = true;
        }

        
        /// <summary>Do our timestep development</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double DoTimeStep(double PropOfDayToUse)
        {
            base.DoTimeStep(PropOfDayToUse);

            if (First)
            {
                CohortNoAtStart = Leaf.ExpandedCohortNo;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Leaf.ExpandedCohortNo >= (Leaf.InitialisedCohortNo - RemainingLeaves))
                return 0.00001;
            else
                return 0;
        }

        // Return proportion of TT unused
        /// <summary>Adds the tt.</summary>
        /// <param name="PropOfDayToUse">The property of day to use.</param>
        /// <returns></returns>
        public override double AddTT(double PropOfDayToUse)
        {
            base.AddTT(PropOfDayToUse);
            if (First)
            {
                CohortNoAtStart = Leaf.ExpandedCohortNo;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Leaf.ExpandedCohortNo >= (Leaf.InitialisedCohortNo - RemainingLeaves))
                return 0.00001;
            else
                return 0;
        }

        /// <summary>Return a fraction of phase complete.</summary>
        /// <value>The fraction complete.</value>
        [XmlIgnore]
        public override double FractionComplete
        {
            get
            {
                double F = (Structure.LeafTipsAppeared - CohortNoAtStart) / ((Leaf.InitialisedCohortNo - RemainingLeaves) - CohortNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
            set
            {
                throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
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

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            // get description of this class.
            AutoDocumentation.GetClassDescription(this, tags, indent);
        }

    }
}

      
      
