namespace Models.LifeCycle
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using Models.Functions;

    
    /// <summary>
    /// The general description of a lifestage process. A Lifestage can contain a number of these.
    /// </summary>
    interface ILifeStageProcess 
    {
        void Process(LifeStage host);
        void ProcessCohort(Cohort cohortItem);
    }
}
