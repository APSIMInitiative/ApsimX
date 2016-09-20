using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Lifecycle
{
    /// <summary>
    /// 
    /// </summary>
    public class Cohort
    {
        /// <summary>
        /// 
        /// </summary>
        public int Ident;

        /// <summary>
        /// Developmental level (which lifestage)
        /// </summary>
        public int PhenoAge;

        /// <summary>
        /// Days of existence
        /// </summary>
        public int ChronoAge;

        /// <summary>
        /// 
        /// </summary>
        public double Count;

        /// <summary>
        /// 
        /// </summary>
        private Lifestage OwningStage;

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
