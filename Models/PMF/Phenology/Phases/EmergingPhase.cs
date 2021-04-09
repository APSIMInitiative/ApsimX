using System;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using Models.Core;
using Newtonsoft.Json;
using Models.Functions;
using APSIM.Shared.Utilities;
using System.IO;
using System.Text;

namespace Models.PMF.Phen
{
    /// <summary>
    /// # [Name] Phase
    /// The [Name] phase goes from [Start] stage to [End] stage and simulates time to 
    /// emergence as a function of sowing depth.  
    /// The <i>ThermalTime Target</i> for this phase is given by
    /// <br>Target = SowingDepth x ShootRate + ShootLag</br>
    /// Where: ShootRate = + ShootRate + ShootLag \n 
    /// and SowingDepth (mm) is sent from the manager with the sowing event;
    /// Progress toward emergence is driven by Thermal time accumulation from Phenology.Thermaltime
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class EmergingPhase : Model, IPhase, IPhaseWithTarget
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        Phenology phenology = null;

        [Link]
        Clock clock = null;

        [Link]
        Plant plant = null;

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
        [JsonIgnore]
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
        [JsonIgnore]
        public double Target { get; set; } 

        /// <summary>Thermal time for this time-step.</summary>
        public double TTForTimeStep { get; set; }

        /// <summary>Accumulated units of thermal time as progress through phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>
        /// Date for emergence to occur.  null by default so model is used
        /// </summary>
        [JsonIgnore]
        public string EmergenceDate { get; set; }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Computes the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;
            TTForTimeStep = phenology.thermalTime.Value() * propOfDayToUse;
            if (EmergenceDate != null)
            {
                Target = (DateUtilities.GetDate(EmergenceDate, clock.Today) - plant.SowingDate).TotalDays;
                ProgressThroughPhase += 1;
                if (DateUtilities.DatesEqual(EmergenceDate, clock.Today))
                {
                    proceedToNextPhase = true;
                }
            }
            else {
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
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public virtual void ResetPhase()
        {
            ProgressThroughPhase = 0;
            Target = 0;
            EmergenceDate = null;
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
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            Target = ShootLag + data.Depth * ShootRate;
        }
       
        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="indent">Indentation level.</param>
        /// <param name="headingLevel">Heading level.</param>
        public override IEnumerable<ITag> Document(int indent, int headingLevel)
        {
            // add a heading
            yield return new Heading($"{Name} Phase", indent, headingLevel);

            // Write description of this class.
            StringBuilder paragraph = new StringBuilder();
            paragraph.AppendLine($"This phase goes from {Start} to {End} and simulates time to emergence as a function of sowing depth. The *ThermalTime Target* for ending this phase is given by:");
            paragraph.AppendLine("    *Target = SowingDepth x ShootRate + ShootLag*");
            paragraph.AppendLine("Where:");
            paragraph.AppendLine($"    *ShootRate* = {ShootRate} (deg day/mm),");
            paragraph.AppendLine($"    *ShootLag* = {ShootLag} (deg day), ");
            paragraph.AppendLine($"and *SowingDepth* (mm) is sent from the manager with the sowing event.");
            yield return new Paragraph(paragraph.ToString(), indent);

            // write intro to children
            // ?
            yield return new Paragraph("Progress toward emergence is driven by Thermal time accumulation, where thermal time is calculated as:", indent);
        }
    }
}
