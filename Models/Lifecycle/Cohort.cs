using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Lifecycle
{
    /// <summary>
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
        /// Developmental level (which lifestage)
        /// </summary>
        public double PhenoAge { get; set; }

        /// <summary>
        /// Days of existence
        /// </summary>
        public double ChronoAge { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Count { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Lifestage OwningStage;

        /// <summary>
        /// 
        /// </summary>
        public Cohort()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        public Cohort(Lifestage owner)
        {
            OwningStage = owner;
        }

        /// <summary>
        /// 
        /// </summary>
        public void AgeCohort()
        {
            PhenoAge++;
            ChronoAge++;
        }
    }
}
