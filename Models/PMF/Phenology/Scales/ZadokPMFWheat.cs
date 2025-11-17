using System;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model calculates a Zadok growth stage value based upon the current phenological growth stage within the model.
    /// The model uses information regarding germination, emergence, leaf appearance and tiller appearance for early growth stages (Zadok stages 0 to 30).
    /// The model then uses simulated phenological growth stages for Zadok stages 30 to 100.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ZadokPMFWheat : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        [Link]
        private Plant plant = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction TillerNumber = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction haunStage = null;

        /// <summary>
        /// Zadok stage numbers for wheat
        /// </summary>
        public static readonly double[] ZADOK_STAGE_NUMBERS = [30.0, 34, 39.0, 55.0, 65.0, 71.0, 87.0, 90.0];

        /// <summary>
        /// Growth stage numbers for wheat
        /// </summary>
        public static readonly double[] GROWTH_STAGE_NUMBERS = [5.0, 5.99, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0];

        private double[] zadokDays = new double[91]; // Use 1 based index (position zero is before sowing). The Zadoks range is 1 to 90.

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("Zadok Stage")]
        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                double zadok_stage = 0.0;
                if (plant != null && !plant.IsAlive)
                    return 0;
                if (Phenology.InPhase("Germinating"))
                    zadok_stage = 5.0f * fracInCurrent;
                else if (Phenology.InPhase("Emerging"))
                    zadok_stage = 5.0f + 5 * fracInCurrent;
                else if (Phenology.Stage < 5.3)
                {
                    zadok_stage = 10.0f + haunStage.Value();
                }
                else if (!Phenology.InPhase("ReadyForHarvesting"))
                {
                    bool DidInterpolate;
                    zadok_stage = MathUtilities.LinearInterpReal(Phenology.Stage,
                                                               GROWTH_STAGE_NUMBERS, ZADOK_STAGE_NUMBERS,
                                                               out DidInterpolate);
                }
                else if (Phenology.InPhase("ReadyForHarvesting"))
                {
                    zadok_stage = 90.0f;
                }

                return zadok_stage;
            }
        }

        // Track the last Zadoks stage that has been recorded
        private int lastRecordedStage = 0;
        /// <summary>
        /// Records the day after sowing when each Zadoks stage (1–99) is first reached.
        /// Uses a progressive approach to avoid redundant looping,
        /// since Zadoks stage increases monotonically.
        /// </summary>
        [EventSubscribe("DoPhenology")]
        private void OnDoPhenology(object sender, EventArgs e)
        {
            int currentStage = (int)Math.Floor(Stage);

            // Progressively check from the last recorded stage onward until the current stage
            // Avoid the long loop from 1 to 99 each time.

            // An example to assign values to zadokDays:
            // if currentStage (Today) = 10, lastRecordedStage (Yesterday) = 5, and DAS = 8
            // zadoksDays[6] = zadoksDays[7] = zadoksDays[8] = zadoksDays[9] = zadoksDays[10] = 8
            // Will skip loop if lastRecordedStage = currentStage
            for (int i = lastRecordedStage + 1; i <= currentStage && i < zadokDays.Length; i++)
            {
                // skip the current one if already recorded for this Zadoks.
                if (zadokDays[i] == 0) 
                {
                    zadokDays[i] = plant.DaysAfterSowing;
                    lastRecordedStage = i;
                }
            }
        }

        /// <summary>
        /// Gets the day (days after sowing) on which a specified Zadoks stage was first reached.
        /// </summary>
        /// <param name="index">
        /// Zadoks stage index (1–99).  
        /// For example, <c>Z(65)</c> returns the day after sowing when stage 65 occurred.
        /// </param>
        /// <returns>
        /// Days after sowing corresponding to the given Zadoks stage,  
        /// or 0 if that stage has not yet been reached.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the range 1–99.
        /// </exception>
        public double StageDAS(int index)
        {
            if (index < 1 || index > zadokDays.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(index), "Valid Zadoks stage range is 1–90.");

            return zadokDays[index]; // Use 1 based index (position zero is before sowing)
        }

    }
}