using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// Attributes of a set of livstock cohorts, used in TStockList.AddStock         
    /// </summary>
    [Serializable]
    public struct CohortsInfo
    {
        /// <summary>
        /// Genotype name
        /// </summary>
        public string Genotype;

        /// <summary>
        /// Total number of animals to enter the simulation. 
        /// The animals will be distributed across the age cohorts, 
        /// taking the genotype-specific death rate into account
        /// </summary>
        public int Number;

        /// <summary>
        /// Reproduction status
        /// </summary>
        public GrazType.ReproType ReproClass;

        /// <summary>
        /// Minimum years of the youngest cohort
        /// </summary>
        public int MinYears;

        /// <summary>
        /// Maximum years of the oldest cohort
        /// </summary>
        public int MaxYears;

        /// <summary>
        /// Age offset
        /// </summary>
        public int AgeOffsetDays;

        /// <summary>
        /// Average unfasted live weight of the animals across all age cohorts
        /// </summary>
        public double MeanLiveWt;

        /// <summary>
        /// Average condition score of the animals 
        /// </summary>
        public double CondScore;

        /// <summary>
        /// Average greasy fleece weight of the animals across all age cohorts
        /// </summary>
        public double MeanGFW;

        /// <summary>
        /// Days since shearing
        /// </summary>
        public int FleeceDays;

        /// <summary>
        /// Genotype of the rams or bulls with which the animals were mated prior to entry
        /// </summary>
        public string MatedTo;

        /// <summary>
        /// Days pregnant
        /// </summary>
        public int DaysPreg;

        /// <summary>
        /// Average number of foetuses per animal (including barren animals) across all age classes
        /// </summary>
        public double Foetuses;

        /// <summary>
        /// The time since parturition in those animals that are lactating
        /// </summary>
        public int DaysLact;

        /// <summary>
        /// Average number of suckling offspring per animal (including dry animals) across all age classes
        /// </summary>
        public double Offspring;

        /// <summary>
        /// Average unfasted live weight of any suckling lambs or calves
        /// </summary>
        public double OffspringWt;

        /// <summary>
        /// Average body condition score of any suckling lambs or calves
        /// </summary>
        public double OffspringCS;

        /// <summary>
        /// Average greasy fleece weight of any suckling lambs
        /// </summary>
        public double LambGFW;
    }
}
