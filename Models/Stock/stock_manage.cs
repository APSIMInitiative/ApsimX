namespace Models.GrazPlan
{
    using Models.Core;

    /// <summary>
    /// Common interface for data passed to stock management events.
    /// </summary>
    public interface IStockEvent
    {
    }

    /// <summary>
    /// Used by the Add() method 
    /// </summary>
    public class StockAdd : IStockEvent
    {
        /// <summary>
        /// Gets or sets the genotype of the animals to enter the simulation. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string Genotype { get; set; }

        /// <summary>
        /// Gets or sets the total number of animals to enter the simulation. 
        /// The animals will be distributed across the age cohorts, taking the genotype-specific death rate into account.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the sex of the animals. Feasible values are as for sheep:sex or cattle:sex, as appropriate.
        /// </summary>
        public ReproductiveType Sex { get; set; }

        /// <summary>
        /// Gets or sets the day of year (1-365) on which all animals are assumed to have been born.
        /// </summary>
        [Units("d")]
        public int BirthDay { get; set; }

        /// <summary>
        /// Gets or sets the age in years of the youngest age cohort (their exact age will depend on the current day of year and the value of birth_day).
        /// </summary>
        public int MinYears { get; set; }

        /// <summary>
        /// Gets or sets the age in years of the oldest age cohort
        /// </summary>
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
        public int ShearDay { get; set; }

        /// <summary>
        /// Gets or sets the genotype of the rams or bulls with which the animals were mated prior to entry. 
        /// Only meaningful if pregnant or lactating is non-zero. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
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
        public double Offspring { get; set; }

        /// <summary>
        /// Gets or sets the average unfasted live weight of any suckling lambs or calves.
        /// </summary>
        [Units("kg")]
        public double YoungWt { get; set; }

        /// <summary>
        /// Gets or sets the average body condition score of any suckling lambs or calves.
        /// </summary>
        [Units("kg")]
        public double YoungCondScore { get; set; }

        /// <summary>
        /// Gets or sets the average greasy fleece weight of any suckling lambs.
        /// </summary>
        [Units("kg")]
        public double YoungFleeceWt { get; set; }

        /// <summary>
        /// Gets or sets the optional tag number to use.
        /// </summary>
        public int UseTag { get; set; }
    }

    /// <summary>
    /// Used by the Buy() method
    /// </summary>
    public class StockBuy : IStockEvent
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
        public string Genotype { get; set; }

        /// <summary>
        /// Gets or sets the number of animals to be bought.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Gets or sets the sex of the animals. 
        /// Feasible values are as for sheep:sex or cattle:sex, as appropriate,
        /// </summary>
        public ReproductiveType Sex { get; set; }

        /// <summary>
        /// Gets or sets the average age of the animals.
        /// </summary>
        [Units("Months")]
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
        public double CondScore { get; set; }

        /// <summary>
        /// Gets or sets the genotype of the rams or bulls with which the animals were mated prior to entry. 
        /// Only meaningful if pregnant or lactating is non-zero. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
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
        public int UseTag { get; set; }
    }

    /// <summary>
    /// Removes animals from the simulation.  sell without parameters will remove all sheep in the stock sub-model
    /// </summary>
    public class StockSell : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group from which animals are to be removed. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of animals has been removed.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the number of animals to remove.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// Castrate event
    /// </summary>
    public class StockCastrate : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group, the lambs or calves of which are to be castrated. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of offspring has been castrated.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the number of male lambs or calves to be castrated.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// Dryoff event
    /// </summary>
    public class StockDryoff : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group for which lactation is to end. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of cows has been dried off.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the number of females for which lactation is to end.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// Removes animals from the simulation by tag number.
    /// </summary>
    public class StockSellTag : IStockEvent
    {
        /// <summary>
        /// Gets or sets the tag number of the animals from which animals are to be removed. 
        /// Animals are removed starting from the group with the smallest index.
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets or sets the number of animals to remove.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// Shears sheep. The event has no effect on cattle.
    /// </summary>
    public class StockShear : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group to be shorn. 
        /// A value of zero denotes that all animal groups should be processed.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the subgroup. Denotes whether the main group of animals, suckling lambs, or both should be shorn. 
        /// Feasible values are the null string (main group), ‘adults’ (main group), ‘lambs’ (suckling lambs), ‘both’ (both).
        /// </summary>
        public string SubGroup { get; set; }
    }

    /// <summary>
    /// Changes the paddock to which an animal group is assigned
    /// </summary>
    public class StockMove : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group to be moved.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the name of the paddock to which the animal group is to be moved.
        /// </summary>
        public string Paddock { get; set; }
    }

    /// <summary>
    /// Stock joining
    /// </summary>
    public class StockJoin : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group for which mating is to commence. 
        /// A value of zero denotes that all empty females of sufficient age should be mated 
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the genotype of the rams or bulls with which the animals are mated. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string MateTo { get; set; }

        /// <summary>
        /// Gets or sets the length of the mating period.
        /// </summary>
        [Units("d")]
        public int MateDays { get; set; }
    }

    /// <summary>
    /// Weans some or all of the lambs or calves from an animal group. 
    /// The newly weaned animals are added to the end of the list of animal groups, with males and females in separate groups.
    /// </summary>
    public class StockWean : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group from which animals are to be removed. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of lambs or calves has been weaned.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the sex to wean.
        /// Feasible values are:
        /// ‘all’       Female and male lambs or calves are to be weaned.
        /// ‘females’   Only female lambs or calves are to be weaned.
        /// ‘males’     Only male lambs or calves are to be weaned
        /// </summary>
        public string Sex { get; set; }

        /// <summary>
        /// Gets or sets the number of lambs or calves to be weaned.
        /// </summary>
        public int Number { get; set; }
    }

    /// <summary>
    /// Creates new animal groups from all the animal groups.  The new groups are placed at the end of the animal group list. 
    /// This event is for when splits need to occur over all animal groups. Description of split event also applies.
    /// </summary>
    public class StockSplitAll : IStockEvent
    {
        /// <summary>
        /// Gets or sets the type of animal to split.
        /// Feasible values are:
        /// ‘age’       All animals older than value days are moved to a new group.
        /// ‘weight’    All animals with live weight less than value kg are moved to a new group.
        /// ‘young’     Only animals with suckling offspring are affected.Mothers with different sexes of young are divided, with the group with all male offspring remaining in place.
        ///             For mothers with twins, three groups are created; a group with two male offspring, a group with two female offspring, and a group with one of each.
        /// ‘number’    value animals remain in place and the remainder form a new group
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the threshold age or weight, or the number to be split, depending on the value of type. Ignored if type is ‘young’.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the tag number. If this value is given then the animals moved into the new animal group will have this tag number. 
        /// </summary>
        public int OtherTag { get; set; }
    }

    /// <summary>
    /// Creates two or more animal groups from the nominated group.
    /// </summary>
    public class StockSplit : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group to be split.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the type of animal.
        /// Feasible values are:
        /// ‘age’       All animals older than value days are moved to a new group.
        /// ‘weight’    All animals with live weight less than value kg are moved to a new group.
        /// ‘young’     Only animals with suckling offspring are affected.Mothers with different sexes of young are divided, with the group with all male offspring remaining in place.
        ///             For mothers with twins, three groups are created; a group with two male offspring, a group with two female offspring, and a group with one of each.
        /// ‘number’    value animals remain in place and the remainder form a new group
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the threshold age or weight, or the number to be split, depending on the value of type. Ignored if type is ‘young’.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the tag number. If this value is given then the animals moved into the new animal group will have this tag number.
        /// </summary>
        public int OtherTag { get; set; }
    }

    /// <summary>
    /// Changes the “tag value” associated with an animal group.  
    /// This value is used to sort animals; it can also be used to group animals for user-defined purposes 
    /// (e.g. to identify animals that are to be managed as a single mob even though they differ physiologically) 
    /// and to keep otherwise similar animal groups distinct from one another.
    /// </summary>
    public class StockTag : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group to be assigned a tag value.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the tag value to be assigned.
        /// </summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// Sets the "priority" of an animal group for later use in a draft event. It is usual practice to use positive values for priorities.
    /// </summary>
    public class StockPrioritise : IStockEvent
    {
        /// <summary>
        /// Gets or sets the index number of the animal group for which priority is to be set.
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Gets or sets the new priority value for the group.
        /// </summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// For the sort event.
    /// </summary>
    public class StockSort : IStockEvent
    {
    }

    /// <summary>
    /// Draft event
    /// </summary>
    public class StockDraft : IStockEvent
    {
        /// <summary>
        /// Gets or sets the names of paddocks to be excluded from consideration as possible destinations
        /// </summary>
        public string[] Closed { get; set; }
    }
}
