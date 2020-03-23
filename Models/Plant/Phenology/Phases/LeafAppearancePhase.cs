using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Struct;
using System.IO;
using APSIM.Shared;
using APSIM.Shared.Utilities;

namespace Models.PMF.Phen
{
    /// <summary> It continues until the final main-stem leaf has finished expansion.  The duration of this phase is determined by leaf appearance rate (Structure.Phyllochron) and the number of leaves produced on the mainstem (Structure.FinalLeafNumber). As such, the model parameterisation of leaf appearance and final leaf number (set in the Structure model) are important for predicting the duration of the crop correctly.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class LeafAppearancePhase : Model, IPhase, ICustomDocumentation
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Leaf leaf = null;

        [Link]
        Structure structure = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        private double LeafNoAtStart;
        private bool First = true;
        private double FractionCompleteYesterday = 0;
        private double TargetLeafForCompletion = 0;

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------
        
        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                double F = 0;
                F = (leaf.ExpandedCohortNo + leaf.NextExpandingLeafProportion - LeafNoAtStart) / TargetLeafForCompletion;
                F = MathUtilities.Bound(F,0,1);
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
        }


        //6. Public method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
                        
            if (First)
            {
                LeafNoAtStart = leaf.ExpandedCohortNo + leaf.NextExpandingLeafProportion;
                TargetLeafForCompletion = structure.finalLeafNumber.Value() - LeafNoAtStart;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (leaf.ExpandedCohortNo >= (leaf.InitialisedCohortNo))
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;  //assumes we use most of the Tt today to get to final leaf.  Should be calculated as a function of the phyllochron
            }
            
            return proceedToNextPhase;
        }
                
        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            LeafNoAtStart = 0;
            FractionCompleteYesterday = 0;
            TargetLeafForCompletion = 0;
            First = true;
        }
        
        //7. Private methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { ResetPhase(); }

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
            }
        }
    }
}

      
      
