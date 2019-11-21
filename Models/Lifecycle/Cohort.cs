// -----------------------------------------------------------------------
// <copyright file="Cohort.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace Models.LifeCycle
{
    using System;

    /// <summary>
    /// # [Name]
    /// A generic cohort item that exists in a LifeStage within a LifeCycle
    /// 
    ///|Property          .|Type    .|Units  .|Description              .| 
    ///|---|---|---|:---|
    ///|PhenoAge     |double   |time steps  |Developmental level (within a LifeStage)    |
    ///|ChronoAge    |double   |time steps  |Period of existence since start of egg(?) stage |
    ///|PhysiologicalAge |double |0-1 |The fraction of maturity for the cohort |
    ///|Count        |double |  |Count of organisms in this cohort |
    ///|Fecundity    |double |  |The fecundity for the time step |
    ///|Mortality    |double |  |The mortality for the time step |
    ///|OwningStage  |LifeStage|  |The LifeStage that owns this cohort |
    ///
    /// </summary>
    [Serializable]
    public class Cohort
    {
        /// <summary>
        /// 
        /// </summary>
        public int Ident;

        /// <summary>
        /// Developmental level (within a lifestage). In timesteps.
        /// </summary>
        public double PhenoAge { get; set; }

        /// <summary>
        /// Period of existence since start of egg(?) stage. In timesteps.
        /// </summary>
        public double ChronoAge { get; set; }

        /// <summary>
        /// The fraction of maturity for the cohort. 0-1
        /// </summary>
        public double PhysiologicalAge { get; set; }

        /// <summary>
        /// Count of creatures/spores in this cohort.
        /// </summary>
        public double Count { get; set; }

        /// <summary>
        /// The fecundity for the timestep.
        /// </summary>
        public double Fecundity = -1.0;

        /// <summary>
        /// Mortality for the timestep.
        /// </summary>
        public double Mortality { get; set; }

        /// <summary>
        /// The Lifestage that owns this cohort.
        /// </summary>
        public LifeStage OwningStage;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Cohort()
        {

        }

        /// <summary>
        /// Construct and store reference to owner.
        /// </summary>
        /// <param name="owner"></param>
        public Cohort(LifeStage owner)
        {
            OwningStage = owner;
        }

        /// <summary>
        /// Increment the timestep age of this cohort.
        /// </summary>
        public void AgeCohort()
        {
            PhenoAge++;
            ChronoAge++;
        }
    }
}
