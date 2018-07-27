using System;
using System.Collections.Generic;
using Models.Core;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.Functions;
using Models.PMF.Struct;
using System.IO;

namespace Models.PMF.Phen
{
    /// <summary> It continues until the final main-stem leaf has finished expansion.  The duration of this phase is determined by leaf appearance rate (Structure.Phyllochron) and the number of leaves produced on the mainstem (Structure.FinalLeafNumber). As such, the model parameterisation of leaf appearance and final leaf number (set in the Structure model) are important for predicting the duration of the crop correctly.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LeafAppearancePhase : Model, IPhase, ICustomDocumentation
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Leaf Leaf = null;

        [Link]
        Structure Structure = null;

        [Link]
        private IFunction ThermalTime = null;  //FIXME this should be called something to represent rate of progress as it is sometimes used to represent other things that are not thermal time.


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

        /// <summary>Gets the tt for today.</summary>
        public double TTForToday { get { return ThermalTime.Value(); } }
        
        /// <summary>Gets the t tin phase.</summary>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        /// <summary>Return a fraction of phase complete.</summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                double F = 0;
                F = (Leaf.ExpandedCohortNo + Leaf.NextExpandingLeafProportion - LeafNoAtStart) / TargetLeafForCompletion;
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
            set
            {
                throw new Exception("Not possible to set phenology into " + this + " phase (at least not at the moment because there is no code to do it");
            }
        }

        //6. Public methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public double DoTimeStep(double PropOfDayToUse)
        {
            if (First)
            {
                LeafNoAtStart = Leaf.ExpandedCohortNo + Leaf.NextExpandingLeafProportion;
                TargetLeafForCompletion = Structure.FinalLeafNumber.Value()  - LeafNoAtStart;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (Leaf.ExpandedCohortNo >= (Leaf.InitialisedCohortNo))
                    return 0.00001;
                else
                    return 0;
        }
                
        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            TTinPhase = 0;
            LeafNoAtStart = 0;
            FractionCompleteYesterday = 0;
            TargetLeafForCompletion = 0;
            First = true;
        }
        
        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer)
        { writer.WriteLine("      " + Name); }

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
            }
        }
    }
}

      
      
