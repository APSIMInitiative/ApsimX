using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;

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
        private IPlant plant = null;

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

    }
}