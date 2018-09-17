using System;
using System.Collections.Generic;
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
    [ValidParent(ParentType = typeof(Phenology))]
    public class EmergingPhase : Model, IPhase, IPhaseWithTarget, ICustomDocumentation
    {

        // 1. Links
        //----------------------------------------------------------------------------------------------------------------
       
        [Link]
        Phenology phenology = null;

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the shoot lag.</summary>
        [Units("oCd")]
        [Description("ShootLag")]
        public double ShootLag { get; set; }

        /// <summary>Gets or sets the shoot rate.</summary>
        [Units("oCd/mm")]
        [Description("ShootRate")]
        public double ShootRate { get; set; }

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary> Return a fraction of phase complete. </summary>
        [XmlIgnore]
        public double FractionComplete
        {
            get
            {
                if (Target == 0)
                    return 1;
                else
                    return ProgressThroughPhase / Target;
            }
        }

        /// <summary>Thermal time target.</summary>
        [XmlIgnore]
        public double Target { get; set; } 

        /// <summary>Gets the tt for today.</summary>
        public double TTForTimeStep { get; set; } 

        /// <summary>Gets the t tin phase.</summary>
        /// <value>The t tin phase.</value>
        [XmlIgnore]
        public double ProgressThroughPhase { get; set; }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary> This function increments thermal time accumulated in each phase and returns a non-zero value if the phase target is met today so
        /// the phenology class knows to progress to the next phase and how much tt to pass it on the first day. </summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            TTForTimeStep = phenology.thermalTime.Value() * propOfDayToUse;
            ProgressThroughPhase += TTForTimeStep;

            if (ProgressThroughPhase > Target)
            {
                if (TTForTimeStep > 0.0)
                {
                    proceedToNextPhase = true;
                    propOfDayToUse = (ProgressThroughPhase - Target) / TTForTimeStep;
                    TTForTimeStep *= (1 - propOfDayToUse);
                }
                ProgressThroughPhase = Target;
            }
            
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            ProgressThroughPhase = 0;
            Target = 0;
        }
        
        /// <summary>Writes the summary.</summary>
        /// <param name="writer">The writer.</param>
        public void WriteSummary(TextWriter writer)
        {
            writer.WriteLine("      " + Name);
        }

        
        //7. Private methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
        Target = ShootLag + data.Depth * ShootRate;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        { ResetPhase(); }
       
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
                tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + " and simulates time to emergence as a function of sowing depth."
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