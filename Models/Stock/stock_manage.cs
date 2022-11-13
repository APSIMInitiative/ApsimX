namespace Models.GrazPlan
{
    using Models.Core;

    /// <summary>
    /// Used by the Add() method 
    /// </summary>
    public class StockAdd
    {
        /// <summary>
        /// Gets or sets the genotype of the animals to enter the simulation. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        [Units("-")]
        public string Genotype { get; set; }

        /// <summary>
        /// Gets or sets the total number of animals to enter the simulation. 
        /// The animals will be distributed across the age cohorts, taking the genotype-specific death rate into account.
        /// </summary>
        [Units("-")]
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the sex of the animals. Feasible values are as for sheep:sex or cattle:sex, as appropriate.
        /// </summary>
        [Units("-")]
        public ReproductiveType Sex { get; set; }

        /// <summary>
        /// Gets or sets the day of year (1-365) on which all animals are assumed to have been born.
        /// </summary>
        [Units("d")]
        public int BirthDay { get; set; }

        /// <summary>
        /// Gets or sets the age in years of the youngest age cohort (their exact age will depend on the current day of year and the value of birth_day).
        /// </summary>
        [Units("years")]
        public int MinYears { get; set; }

        /// <summary>
        /// Gets or sets the age in years of the oldest age cohort
        /// </summary>
        [Units("years")]
        public int MaxYears { get; set; }

        /// <summary>
        /// Gets or sets the average unfasted live weight of the animals across all age cohorts. 
        /// Animals in each age cohort will be given different weights, based on their normal weight for age, such that the overall average weight is that specified by this parameter. 
        /// This parameter may also be set to zero, in which case a default set of live weights will be computed, taking cond_score into account if it is nonzero.
        /// kg
        /// </summary>
        [Units("kg")]
        public double MeanWeight { get; set; }

        /// <summary>
        /// Gets or sets the average condition score of the animals (assumed to be the same for all age cohorts). 
        /// If a value of zero is given, the default condition score for the weight and age will be used.
        /// </summary>
        [Units("-")]
        public double CondScore { get; set; }

        /// <summary>
        /// Gets or sets the average greasy fleece weight of the animals across all age cohorts. 
        /// Different values will be computed for each age cohort, such that the weighted average fleece weight equals the specified value. 
        /// This parameter may be set to zero, in which case a default set of fleece weights will be computed based on the current day of year and the shear_day parameter. 
        /// Only meaningful in sheep.
        /// kg
        /// </summary>
        [Units("kg")]
        public double MeanFleeceWt { get; set; }

        /// <summary>
        /// Gets or sets the day of year on which the animals were last shorn. Only meaningful in sheep.
        /// </summary>
        [Units("-")]
        public int ShearDay { get; set; }

        /// <summary>
        /// Gets or sets the genotype of the rams or bulls with which the animals were mated prior to entry. 
        /// Only meaningful if pregnant or lactating is non-zero. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        [Units("-")]
        public string MatedTo { get; set; }

        /// <summary>
        /// Gets or sets the pregnancy status. Zero denotes no animals are pregnant; 1 or more denotes the time since conception of those animals that are pregnant. 
        /// Only meaningful for females.
        /// </summary>
        [Units("d")]
        public int Pregnant { get; set; }

        /// <summary>
        /// Gets or sets the average number of foetuses per animal (including barren animals) across all age classes. 
        /// Different pregnancy rates will be computed for each age cohort, such that the weighted average number of foetuses per animal equals the specified value. 
        /// Only meaningful for females.
        /// </summary>
        [Units("-")]
        public double Foetuses { get; set; }

        /// <summary>
        /// Gets or sets the lactation status. Zero denotes no animals are lactating; 1 or more denotes the time since parturition in those animals that are lactating. 
        /// Only meaningful for females.
        /// d
        /// </summary>
        [Units("d")]
        public int Lactating { get; set; }

        /// <summary>
        /// Gets or sets the average number of suckling offspring per animal (including dry animals) across all age classes. 
        /// Different numbers of offspring will be computed for each age cohort, such that the weighted average number of offspring per animal equals the specified value. 
        /// Only meaningful for females.
        /// </summary>
        [Units("-")]
        public double Offspring { get; set; }

        /// <summary>
        /// Gets or sets the average unfasted live weight of any suckling lambs or calves.
        /// </summary>
        [Units("kg")]
        public double YoungWt { get; set; }

        /// <summary>
        /// Gets or sets the average body condition score of any suckling lambs or calves.
        /// </summary>
        [Units("-")]
        public double YoungCondScore { get; set; }

        /// <summary>
        /// Gets or sets the average greasy fleece weight of any suckling lambs.
        /// </summary>
        [Units("kg")]
        public double YoungFleeceWt { get; set; }

        /// <summary>
        /// Gets or sets the optional tag number to use.
        /// </summary>
        [Units("-")]
        public int UseTag { get; set; }
    }

    /// <summary>
    /// Used by the Buy() method
    /// </summary>
    public class StockBuy
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public StockBuy()
        {
            this.MatedTo = string.Empty;
        }

        /// <summary>
        /// Gets or sets the genotype of the animals to be bought. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        [Units("-")]
        public string Genotype { get; set; }

        /// <summary>
        /// Gets or sets the number of animals to be bought.
        /// </summary>
        [Units("-")]
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the sex of the animals. 
        /// Feasible values are as for sheep:sex or cattle:sex, as appropriate,
        /// </summary>
        [Units("-")]
        public ReproductiveType Sex { get; set; }

        /// <summary>
        /// Gets or sets the average age of the animals.
        /// </summary>
        [Units("months")]
        public double Age { get; set; }

        /// <summary>
        /// Gets or sets the average unfasted live weight of the animals. 
        /// If a value of zero is given, a default value will be calculated, making use of the cond_score parameter if it is non-zero.
        /// </summary>
        [Units("kg")]
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the average greasy fleece weight of the animals. 
        /// Only meaningful in sheep.
        /// </summary>
        [Units("kg")]
        public double FleeceWt { get; set; }

        /// <summary>
        /// Gets or sets the average condition score of the animals. 
        /// If a value of zero is given, the default condition score for the weight and age will be used.
        /// </summary>
        [Units("-")]
        public double CondScore { get; set; }

        /// <summary>
        /// Gets or sets the genotype of the rams or bulls with which the animals were mated prior to entry. 
        /// Only meaningful if pregnant or lactating is non-zero. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        [Units("-")]
        public string MatedTo { get; set; }

        /// <summary>
        /// Gets or sets the pregnancy status. Zero denotes not pregnant; 1 or more denotes the time since conception. 
        /// Only meaningful for females.
        /// </summary>
        [Units("d")]
        public int Pregnant { get; set; }

        /// <summary>
        /// Gets or sets the latation status. Zero denotes not lactating; 1 or more denotes the time since parturition in lactating animals. 
        /// Only meaningful for females.
        /// </summary>
        [Units("d")]
        public int Lactating { get; set; }

        /// <summary>
        /// Gets or sets the number of foetuses and/or suckling offspring.
        /// </summary>
        [Units("-")]
        public int NumYoung { get; set; }

        /// <summary>
        /// Gets or sets the average unfasted live weight of any suckling lambs or calves.
        /// </summary>
        [Units("kg")]
        public double YoungWt { get; set; }

        /// <summary>
        /// Gets or sets the average greasy fleece weight of any suckling lambs.
        /// </summary>
        [Units("kg")]
        public double YoungFleeceWt { get; set; }

        /// <summary>
        /// Gets or sets the optional tag to use.
        /// </summary>
        [Units("-")]
        public int UseTag { get; set; }
    }
}