using System;
using System.Collections.Generic;
using Models.Core;
using System.Xml.Serialization;
using Models.Functions;
using System.IO;

namespace Models.PMF.Phen
{
    /// <summary>Describe the phenological development through the emerging phase.</summary>
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

        // 2. Public properties
        //-----------------------------------------------------------------------------------------------------------------
    
        /// <summary>Gets or sets the lag for shoot development.</summary>
        [Units("oCd")]
        [Description("ShootLag")]
        public double ShootLag { get; set; }

        /// <summary>Gets or sets the shoot growth rate.</summary>
        [Units("oCd/mm")]
        [Description("ShootRate")]
        public double ShootRate { get; set; }

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Models.Core.Description("End")]
        public string End { get; set; }

        /// <summary>Fraction of phase that is complete (0-1).</summary>
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

        /// <summary>Thermal time target to end this phase.</summary>
        [XmlIgnore]
        public double Target { get; set; } 

        /// <summary>Thermal time for this time-step.</summary>
        public double TTForTimeStep { get; set; }

        /// <summary>Accumulated units of thermal time as progress through phase.</summary>
        [XmlIgnore]
        public double ProgressThroughPhase { get; set; }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Computes the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
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
        
        // 4. Private method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            Target = ShootLag + data.Depth * ShootRate;
        }
       
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading
                tags.Add(new AutoDocumentation.Heading(Name + " Phase", headingLevel));

                // write description of this class
                tags.Add(new AutoDocumentation.Paragraph("This phase goes from " + Start + " to " + End + " and simulates time to "
                    + "emergence as a function of sowing depth.  The <i>ThermalTime Target</i> for ending this phase is given by:<br>"
                    + "&nbsp;&nbsp;&nbsp;&nbsp;*Target = SowingDepth x ShootRate + ShootLag*<br>"
                    + "Where:<br>"
                    + "&nbsp;&nbsp;&nbsp;&nbsp;*ShootRate* = " + ShootRate + " (deg day/mm),<br>"
                    + "&nbsp;&nbsp;&nbsp;&nbsp;*ShootLag* = " + ShootLag + " (deg day), <br>"
                    + "and *SowingDepth* (mm) is sent from the manager with the sowing event.", indent));

                // write memos
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                // write intro to children
                tags.Add(new AutoDocumentation.Paragraph("Progress toward emergence is driven by Thermal time accumulation, where thermal time is calculated as:", indent));

                // write children
                foreach (IModel child in Apsim.Children(this, typeof(IFunction)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
