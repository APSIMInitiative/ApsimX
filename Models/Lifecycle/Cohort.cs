namespace Models.LifeCycle
{
    using System;

    /// <summary>
    /// # [Name]
    /// A class that holds the status of a group of individules (cohort) of the same developmental stage
    /// </summary>

    [Serializable]
    public class Cohort
    {
        /// <summary>Number of individules in this cohort.</summary>
        public double Population { get; set; }

        /// <summary>Days since this cohort was initiated</summary>
        public double ChronologicalAge { get; set; }

        /// <summary>The maturity of the cohort (0-1)</summary>
        public double PhysiologicalAge { get; set; }
                
        /// <summary>Number of Mortalities from this cohort today.</summary>
        public double Mortalities { get; set; }

        /// <summary>Number of progeny created by this cohort</summary>
        public double Progeny { get; set; }

        /// <summary>Number of migrants leaving this cohort</summary>
        public double Migrants { get; set; }

        /// <summary>The LifeCyclePhase this cohort belongs to.</summary>
        public LifeCyclePhase BelongsToPhase;

        /// <summary> Construct and store reference to owner.</summary>
        /// <param name="belongsTo"></param>
        public Cohort(LifeCyclePhase belongsTo)
        {
            BelongsToPhase = belongsTo;
        }

    }
}
