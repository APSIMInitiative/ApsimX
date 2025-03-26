using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model calculates a Zadok growth stage value based upon the current phenological growth stage within the model. 
    /// The model uses information regarding germination, emergence, leaf appearance and tiller appearance for early growth stages (Zadok stages 0 to 30).
    /// The model then uses simulated phenological growth stages for Zadok stages 30 to 100.
    /// 
    ///|Growth Phase     |Description                                   |
    ///|-----------------|:---------------------------------------------|
    ///|Germinating      |ZadokStage = 5 x FractionThroughPhase         |
    ///|Emerging         |ZadokStage = 5 + 5 x FractionThroughPhase     |
    ///|Vegetative       |ZadokStage = 10 + Structure.LeafTipsAppeared  |
    ///|Reproductive     |ZadokStage is interpolated from values of     |
    ///|                 |stage number using the following table.       |
    ///
    ///|   Growth Stage  |   ZadokStage      |
    ///|-----------------|:------------------|
    ///|       3.9       |         30        |
    ///|       4.9       |         33        |
    ///|       5.0       |         39        |
    ///|       6.0       |         65        |
    ///|       7.0       |         71        |
    ///|       8.0       |         87        |
    ///|       9.0       |         90        |
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ZadokPMF : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>
        /// The Structure class
        /// </summary>
        [Link]
        Structure Structure = null;

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("Zadok Stage")]
        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                if (Phenology.InPhase(""))
                {
                    return 0;
                }
                else if (Phenology.InPhase("Germinating"))
                {
                    return 5.0f * fracInCurrent;
                }
                else if (Phenology.InPhase("Emerging"))
                {
                    return 5.0f + 5 * fracInCurrent;
                }
                else if ((Phenology.InPhase("Vegetative") && fracInCurrent <= 0.9) || (!Phenology.InPhase("ReadyForHarvesting") && Phenology.Stage < 4.3))
                {
                    // Try using Yield Prophet approach where Zadok stage during vegetative phase is based on leaf number only
                    return 10.0f + Structure.LeafTipsAppeared;
                }
                else if (!Phenology.InPhase("ReadyForHarvesting"))
                {
                    double[] zadok_code_y = { 30.0, 33, 39.0, 65.0, 71.0, 87.0, 90.0 };
                    double[] zadok_code_x = { 4.3, 4.9, 5.0, 6.0, 7.0, 8.0, 9.0 };
                    return MathUtilities.LinearInterpReal(Phenology.Stage, zadok_code_x, zadok_code_y, out bool DidInterpolate);
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}