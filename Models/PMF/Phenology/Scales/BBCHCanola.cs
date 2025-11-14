using System;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model calculates a BBCH growth stage value for Canola based upon the current phenological growth stage within the model.
    /// 
    /// BBCH Scale for Canola Mapping:
    /// - BBCH 0-5: Germinating phase (Stage 1: Sowing to Stage 2: Germination) - Linear progression
    /// - BBCH 5-10: Emerging phase (Stage 2: Germination to Stage 3: Emergence) - Linear progression
    /// - BBCH 10: Emergence (Stage 3: Emergence)
    /// - BBCH 31: Floral Initiation (Stage 4: FloralInitiation)
    /// - BBCH 51: Green Bud (Stage 5: GreenBud)
    /// - BBCH 60: Start Flowering (Stage 6: StartFlowering)
    /// - BBCH 69: End Flowering (Stage 9: EndFlowering)
    /// - BBCH 79: End Pod Development (Stage 10: EndPodDevelopment)
    /// - BBCH 89: End Grain Fill (Stage 11: EndGrainFill)
    /// 
    /// Linear interpolation is used between these key stages (Stages 3-11).
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class BBCHCanola : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The plant</summary>
        [Link]
        private Plant plant = null;

        /// <summary>The Haun stage function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction haunStage = null;

        private double[] bbchDays = new double[90]; // Use 1-based index (position zero is before sowing). BBCH range is 1 to 89.

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("BBCH phenological stages")]
        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                double bbch_stage = 0.0;

                if (plant != null && !plant.IsAlive)
                    return 0;

                // Germinating Phase: BBCH 1 - 5
                if (Phenology.InPhase("Germinating"))
                {
                    bbch_stage = 5.0f * fracInCurrent;
                }
                // Emerging Phase: BBCH 6 - 10
                else if (Phenology.InPhase("Emerging")) 
                {
                    bbch_stage = 5.0f + 5 * fracInCurrent;
                }
                // BBCH 10-89: Emergence to End Grain Fill (Stages 3-11)
                // Linear interpolation between key phenological stages
                else if (Phenology.Stage >= 3.0 && Phenology.Stage <= 11.0)
                {
                    // BBCH stages mapped to phenological stages:
                    // BBCH 10: Stage 3 - Emergence
                    // BBCH 31: Stage 4 - Floral Initiation
                    // BBCH 51: Stage 5 - Green Bud
                    // BBCH 60: Stage 6 - Start Flowering
                    // BBCH 69: Stage 9 - End Flowering
                    // BBCH 79: Stage 10 - End Pod Development
                    // BBCH 89: Stage 11 - End Grain Fill
                    double[] BBCH_code_y = {10, 31, 51, 60, 69, 79, 89 };
                    double[] PMF_Stage = { 3, 4, 5, 6.0, 9, 10, 11 };
                    bool DidInterpolate;
                    bbch_stage = MathUtilities.LinearInterpReal(Phenology.Stage,
                                                                PMF_Stage, BBCH_code_y,
                                                                out DidInterpolate);
                }
                return bbch_stage;
            }
        }

        // Track the last BBCH stage that has been recorded
        private int lastRecordedStage = 0;

        /// <summary>
        /// Records the day after sowing when each BBCH stage (1–89) is first reached.
        /// Uses a progressive approach to avoid redundant looping,
        /// since BBCH stage increases monotonically.
        /// </summary>
        [EventSubscribe("DoPhenology")]
        private void OnDoPhenology(object sender, EventArgs e)
        {
            int currentStage = (int)Math.Floor(Stage);

            // Progressively check from the last recorded stage onward until the current stage
            for (int i = lastRecordedStage + 1; i <= currentStage && i < bbchDays.Length; i++)
            {
                // skip the current one if already recorded for this BBCH stage
                if (bbchDays[i] == 0) 
                {
                    bbchDays[i] = plant.DaysAfterSowing;
                    lastRecordedStage = i;
                }
            }
        }

        /// <summary>
        /// Gets the day (days after sowing) on which a specified BBCH stage was first reached.
        /// </summary>
        /// <param name="index">
        /// BBCH stage index (1–89).  
        /// For example, <c>StageDAS(50)</c> returns the day after sowing when BBCH stage 50 occurred.
        /// </param>
        /// <returns>
        /// Days after sowing corresponding to the given BBCH stage,  
        /// or 0 if that stage has not yet been reached.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the range 1–89.
        /// </exception>
        public double StageDAS(int index)
        {
            if (index < 1 || index > bbchDays.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(index), "Valid BBCH stage range is 1–89.");

            return bbchDays[index];
        }
    }
}