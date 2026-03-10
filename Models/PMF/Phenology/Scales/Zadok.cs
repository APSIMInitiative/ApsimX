using System;
using APSIM.Core;
using Models.Core;
using Models.PMF.Struct;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This model calculates a Zadok growth stage value based upon the current 
    /// phenological growth stage within the model.
    /// 
    /// The model uses information regarding germination, emergence, leaf 
    /// appearance and tiller appearance for early growth stages (Zadok stages 
    /// 0 to 30).
    /// 
    /// The model then uses simulated phenological growth stages for Zadok 
    /// stages 30 to 100.
    ///
    /// By default, the following scale conversion is used, however this can be 
    /// changed using the child ZadokStageMapping function.
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
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Zadok : Model
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        [Link]
        private Plant plant = null;

        /// <summary>
        /// The Structure class
        /// </summary>
        [Link (IsOptional = true)]
        Structure Structure = null;

        /// <summary>
        /// Calculation during vegetive phase
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction VegetativePhaseFunction = null;

        /// <summary>
        /// Zadok and growth stage mapping for winter cereals 
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction ZadokStageMapping = null;

        /// <summary>
        /// Use 1 based index (position zero is before sowing). The Zadoks 
        /// range is 1 to 90.
        /// </summary>
        private double[] zadokDays = new double[91]; 

        /// <summary>Gets the stage.</summary>
        /// <value>The stage.</value>
        [Description("Zadok Stage")]
        public double Stage
        {
            get
            {
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                if (plant != null && !plant.IsAlive)
                    return 0;

                if (Phenology.InPhase("Germinating"))
                    return 5.0f * fracInCurrent;

                if (Phenology.InPhase("Emerging"))
                    return 5.0f + 5 * fracInCurrent;

                double value = VegetativePhaseFunction.Value();
                if (value >= 0)
                    return value;
                
                if (!Phenology.InPhase("ReadyForHarvesting"))
                    return ZadokStageMapping.Value();

                if (Phenology.InPhase("ReadyForHarvesting"))
                    return 90.0f;

                return 0;
            }
        }

        // Track the last Zadoks stage that has been recorded
        private int lastRecordedStage = 0;
        /// <summary>
        /// Records the day after sowing when each Zadoks stage (1–99) is first 
        /// reached.
        /// 
        /// Uses a progressive approach to avoid redundant looping, since 
        /// Zadoks stage increases monotonically.
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
        /// Gets the day (days after sowing) on which a specified Zadoks stage 
        /// was first reached.
        /// </summary>
        /// <param name="index">
        /// Zadoks stage index (1–99).  
        /// For example, <c>Z(65)</c> returns the day after sowing when stage 
        /// 65 occurred.
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

        /// <summary>
        /// Generic function to calculate Zadok stage during the vegetative function
        /// </summary>
        /// <returns>
        /// If less than 90% through Vegetative phase, or Phenelogy stage is less than 4.3, will return calculated 
        /// zadok stage. If outside that timeframe, will return -1.
        /// </returns>
        public double VegetativePhaseCalculation
        {
            get {
                // Try using Yield Prophet approach where Zadok stage during vegetative phase is based on leaf number only
                double fracInCurrent = Phenology.FractionInCurrentPhase;
                if ((Phenology.InPhase("Vegetative") && fracInCurrent <= 0.9) || (Phenology.Stage < 4.3))
                    return 10.0f + Structure.LeafTipsAppeared;
                else
                    return -1;
            }
        }
        

        /// <summary>
        /// Winter Ceral function to calculate Zadok stage during the vegetative function
        /// </summary>
        /// <returns>
        /// If Phenelogy stage is less than 5.3, will return calculated zadok stage. If outside that timeframe, will 
        /// return -1.
        /// </returns>
        public double VegetativePhaseCalculationWheat
        {
            get {
                IFunction haunStage = Phenology.Node.FindChild<IFunction>("HaunStage");
                if (Phenology.Stage < 5.3)
                    return 10.0f + haunStage.Value();
                else
                    return -1;
            }
        }
    }
}