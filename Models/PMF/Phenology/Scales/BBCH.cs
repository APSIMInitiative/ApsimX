using System;
using APSIM.Core;
using APSIM.Numerics;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Organs;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model calculates a BBCH growth stage value based upon the current phenological growth stage within the model.
    /// The model uses information regarding germination, emergence and leaf appearance for early growth stages (BBCH stages 0 to 39).
    ///
    ///|BeginStage |Growth Phase     |Description                             |
    ///|-----------|-----------------|:---------------------------------------|
    ///|1          |Germinating      |BBCH = 5 x FractionThroughPhase         |
    ///|2          |Emerging         |BBCH = 5 + 5 x FractionThroughPhase     |
    ///|3          |Juvenile         |BBCH = 10 + (Leaf.AppearedCohortNo - 1) |
    ///|4          |PhotoSensitive   |BBCH = 10 + (Leaf.AppearedCohortNo - 1) |
    ///|5          |LeafAppearance   |BBCH = 30 + LeavesAppearedInPhase       |
    ///
    /// BBCHSTages 11-19 assume the dropy leaf method of measuring leaf appearance and to translate this to a model variable we assum droopy leaves are one fewer than the number of tips visiable
    /// WE assume that the begining of stem extension (BBCH 30) corresponds to the floral initiation stage in the model (Stage 5).  Scores between 31 and 39 depend on the number of nodes visiable.
    /// The model does not simulate nodes explicitly so we assume that node apparance occurs at the same rate as leaf appearance an add the number of leaves that have appeared in the LeafAppearance Phase to give an estimate of BBCH score
    /// BBCH stage 50 occurs when the tastle is just visiable.  This is assumed to occur at the same time as the appearance of the tip of the flag leaf.  This occurs toward the end of the leaf appearance phase
    /// While still in the leaf apperance phase after flag leaf tip appearance BBCH score is calculated as:
    /// BBCH = 50 + 5 * FractionFlagLeafExpansion
    /// This assumes the tassel will be half emerged when the flaf leaf is full expanded
    /// The model then uses simulated phenological growth stages for BBCH stages 55 to 99.
    ///
    ///|Stage   |APSIM Name              |BBCH translation
    ///|--------|------------------------|-------------------------------------|
    ///|6.0     |FlagLeafFullyExpanded   |55 - Mid Tassel Emergence
    ///|7.0     |Flowering               |65 - Mid flowering
    ///|8.0     |StartGrainFill          |70 - Begining of Grain development
    ///|9.0     |EndGrainFill            |87 - Physiological maturity
    ///|10      |Maturity                |99 - Harvest Product
    ///
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class BBCH : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>
        /// The Leaf class
        /// </summary>
        [Link(IsOptional = true)]
        Leaf Leaf = null;

        /// <summary>
        /// The Structure class
        /// </summary>
        [Link(IsOptional = true)]
        Structure Structure = null;

        /// <summary>
        /// Calculation during vegetive phase
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction BBCHCalculationFunction = null;

        private bool StemExtensionInitialised = false;
        private bool TasselVisiable = false;
        private int ligulesAtStartStemExtension = 0;
        private double FractionInPhaseAtBBCH50 = 0;

        // Track the last BBCH stage that has been recorded
        private int lastRecordedStage = 0;

        // Use 1-based index (position zero is before sowing). BBCH range is 1 to 89.
        private double[] bbchDays = new double[91];

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("BBCH phenoligical stages")]

        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;

                if (Plant != null && !Plant.IsAlive)
                    return 0;

                if (Phenology.InPhase("Germinating"))
                    return 5.0f * fracInCurrent;

                if (Phenology.InPhase("Emerging"))
                    return 5.0f + 5 * fracInCurrent;
                
                double value = BBCHCalculationFunction.Value();
                if (value >= 0)
                    return value;
                
                return 0;
            }
        }

        /// <summary>
        /// Records the day after sowing when each BBCH stage (1–90) is first reached.
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
                    bbchDays[i] = Plant.DaysAfterSowing;
                    lastRecordedStage = i;
                }
            }
        }

        /// <summary>
        /// Gets the day (days after sowing) on which a specified BBCH stage was first reached.
        /// </summary>
        /// <param name="index">
        /// BBCH stage index (1–90).  
        /// For example, <c>StageDAS(50)</c> returns the day after sowing when BBCH stage 50 occurred.
        /// </param>
        /// <returns>
        /// Days after sowing corresponding to the given BBCH stage,  
        /// or 0 if that stage has not yet been reached.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="index"/> is outside the range 1–90.
        /// </exception>
        public double StageDAS(int index)
        {
            if (index < 1 || index > bbchDays.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(index), "Valid BBCH stage range is 1–90.");

            return bbchDays[index];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// If Phenelogy Phase is Juvenile or beyond, will return calculated BBCH stage. If outside that timeframe, 
        /// will return -1.
        /// </returns>
        public double BBCHCalculation
        {
            get {
                Leaf leaf = Leaf as Leaf;
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                if (Phenology.InPhase("Juvenile") || Phenology.InPhase("PhotoSensitive"))
                    return Math.Min(19.0, 10.0f + Math.Max(0, leaf.AppearedCohortNo - 1));

                if (Phenology.InPhase("LeafAppearance") && (leaf.AppearedCohortNo <= Structure.finalLeafNumber.Value()))
                {
                    if (StemExtensionInitialised == false)
                    {
                        ligulesAtStartStemExtension = leaf.ExpandedCohortNo;
                        StemExtensionInitialised = true;
                    }
                    return 30.0f + Math.Min(9, leaf.ExpandedCohortNo - ligulesAtStartStemExtension);
                }
                
                if (Phenology.InPhase("LeafAppearance") && leaf.AppearedCohortNo >= Structure.finalLeafNumber.Value() && fracInCurrent < 1)
                {
                    if (TasselVisiable == false)
                    {
                        FractionInPhaseAtBBCH50 = fracInCurrent;
                        TasselVisiable = true;
                    }
                    if (fracInCurrent == 0.0)//Falt in FractionCurrent meaning it returns a zero when phase is complete rather than a one
                        fracInCurrent = 1.0;
                    return 50.0f + 5 * (fracInCurrent - FractionInPhaseAtBBCH50) / (1 - FractionInPhaseAtBBCH50);
                }
                else
                {
                    double[] BBCH_code_y = { 55, 65, 70, 87, 99.0 };
                    double[] PMF_Stage = { 6.0, 7.0, 8.0, 9.0, 10.0 };
                    return MathUtilities.LinearInterpReal(Phenology.Stage, PMF_Stage, BBCH_code_y, out bool DidInterpolate);
                }
            }
        }

        /// <summary>
        /// BBCH stages mapped to phenological stages:
        /// BBCH 10: Stage 3 - Emergence
        /// BBCH 30: Stage 4 - Floral Initiation
        /// BBCH 51: Stage 5 - Green Bud
        /// BBCH 60: Stage 6 - Start Flowering
        /// BBCH 65: Stage 6.99 - 50% of flowers on main raceme open, older petals falling
        /// BBCH 70: Stage 7 - Pods begin to form after flowering
        /// BBCH 75: Stage 7.99 - 50% of pods to form after flowering
        /// BBCH 80: Stage 8 - Seeds begin to accumulate dry matter (grain filling starts)
        /// BBCH 79: Stage 10 - End Pod Development // Not implemented
        /// BBCH 87: Stage 11 - End Grain Fill
        /// BBCH 90: Stage 12 - Maturity
        /// </summary>
        /// <returns>
        /// If Phenelogy stage is more than 3.0 and less than 13.0, will return calculated BBCH stage. If outside that 
        /// timeframe, will return -1.
        /// </returns>
        public double BBCHCalculationCanola
        {
            get {
                if (Phenology.Stage >= 3.0 && Phenology.Stage <= 13.0)
                {
                    
                    double[] BBCH_code_y = {10, 30, 51, 60, 65, 70, 75, 80, 87, 90 };
                    double[] PMF_Stage = { 3, 4, 5, 6.0, 6.99, 7, 7.99, 8, 11, 12 };
                    return MathUtilities.LinearInterpReal(Phenology.Stage, PMF_Stage, BBCH_code_y, out bool didInterpolate);
                }
                else
                    return -1;
            }
        }
    }
}