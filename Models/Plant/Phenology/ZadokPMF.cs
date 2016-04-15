using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using Models.PMF.Phen;
using APSIM.Shared.Utilities;
using Models.PMF.Organs;
using System.Xml.Serialization;

namespace Models.PMF.Phen
{
    /// <summary>
    /// Zadok model
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ZadokPMF: Model
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
                double zadok_stage = 0.0;
                if (Phenology.InPhase("Germinating"))
                    zadok_stage = 5.0f * fracInCurrent;
                else if (Phenology.InPhase("Emerging"))
                    zadok_stage = 5.0f + 5 * fracInCurrent;
                else if (Phenology.InPhase("Vegetative") && fracInCurrent <= 0.9)
                {
                    if (Structure.BranchNumber <= 0.0)
                        zadok_stage = 10.0f + Structure.MainStemNodeNo;
                    else
                        zadok_stage = 20.0f + Structure.BranchNumber;
                }
                else if (!Phenology.InPhase("ReadyForHarvesting"))
                {
                    double[] zadok_code_y = { 30.0, 40.0, 65.0, 71.0, 87.0, 90.0, 100.0 };
                    double[] zadok_code_x = { 3.9, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };
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