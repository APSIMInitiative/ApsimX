using System;
using System.Collections.Generic;
using Models.Core;
using Models.Functions;
using System.IO;
using System.Xml.Serialization;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary> Predicts the date of bud emerging date of pereniel crops </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BuddingPhase : Model, IPhase, ICustomDocumentation
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Plant Plant = null;

        [Link]
        Phenology phenology = null;

        [Link]
        Structure structure = null;

        [Link]
        private IFunction FractionOfBudBurst = null;

        [Link]
        private IFunction Target = null;

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
        
        /// <summary> Return a fraction of phase complete. </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target.Value() == 0)
                    return 1;
                else
                    return TTinPhase / Target.Value();
            }
            set
            {
                if (phenology != null)
                {
                    TTinPhase = Target.Value() * value;
                    phenology.AccumulatedEmergedTT += TTinPhase;
                    phenology.AccumulatedTT += TTinPhase;
                }
            }
        }

        /// <summary>Gets the tt for today.</summary>
        [XmlIgnore]
        public double TTForToday { get { return ThermalTime.Value(); } }

        /// <summary>Gets the t tin phase.</summary>
        [XmlIgnore]
        public double TTinPhase { get; set; }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        
        /// <summary> This function increments thermal time accumulated in each phase and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and howmuch tt to pass it on the first day.</summary>
        public double DoTimeStep(double PropOfDayToUse)
        {
            double tTForToday = ThermalTime.Value() * PropOfDayToUse;
            TTinPhase += tTForToday;

            // Get the Target TT
            structure.PrimaryBudNo = Plant.SowingData.BudNumber;

            double PropOfDayUnused = 0;
            if (TTinPhase > Target.Value())
            {
                if (tTForToday > 0.0)
                {
                    double PropOfValueUnused = (TTinPhase - Target.Value()) / ThermalTime.Value();
                    PropOfDayUnused = PropOfValueUnused * PropOfDayToUse;
                }
                else
                    PropOfDayUnused = 1.0;
                TTinPhase = Target.Value();
            }

            if (PropOfDayUnused > 0.0)
            {
                double BudNumberBurst = Plant.SowingData.BudNumber * FractionOfBudBurst.Value();

                structure.PrimaryBudNo = BudNumberBurst;
                structure.TotalStemPopn = structure.MainStemPopn;
                Plant.SendEmergingEvent();
                phenology.Emerged = true;
            }

            return PropOfDayUnused;
        }
        
        /// <summary> Write Summary  /// </summary>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
            if (Target != null)
                writer.WriteLine(string.Format("         Target                    = {0,8:F0} (dd)", Target.Value()));
        }
        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        { TTinPhase = 0; }

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

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}


