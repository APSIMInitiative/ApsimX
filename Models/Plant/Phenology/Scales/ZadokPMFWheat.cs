using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Phen;
using APSIM.Shared.Utilities;
using Models.PMF.Organs;
using System.Xml.Serialization;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary>
    /// # [Name]
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
    ///|       4.3       |         30        |
    ///|       4.9       |         33        |
    ///|       5.0       |         39        |
    ///|       6.0       |         55        |
    ///|       7.0       |         65        |
    ///|       8.0       |         71        |
    ///|       9.0       |         87        |
    ///|      10.0       |         90
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ZadokPMFWheat: Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>
        /// The Structure class
        /// </summary>
        [Link]
        Structure Structure = null;

        [Link]
        private IPlant plant = null;

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
                else if ((Phenology.InPhase("Vegetative") && fracInCurrent <= 0.9)
                    || (!Phenology.InPhase("ReadyForHarvesting")&&Phenology.Stage<5.3))
                {
                    if (Structure.BranchNumber <= 0.0)
                        zadok_stage = 10.0f + Structure.LeafTipsAppeared;
                    else
                        zadok_stage = 20.0f + Structure.BranchNumber;
                    // Try using Yield Prophet approach where Zadok stage during vegetative phase is based on leaf number only
                    zadok_stage = 10.0f + Structure.LeafTipsAppeared;

                }
                else if (!Phenology.InPhase("ReadyForHarvesting"))
                {
                    double[] zadok_code_y = { 30.0, 33, 39.0, 55.0, 65.0, 71.0, 87.0, 90.0};
                    double[] zadok_code_x = { 5.3, 5.9, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0};
                    bool DidInterpolate;
                    zadok_stage = MathUtilities.LinearInterpReal(Phenology.Stage,
                                                               zadok_code_x, zadok_code_y,
                                                               out DidInterpolate);
                }
                return zadok_stage;
            }
        }
    }
}