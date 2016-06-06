
namespace Models.GrazPlan
{
    /// <summary>
    /// Common interface for data passed to stock management events.
    /// </summary>
    public interface IStockEvent
    {

    }

    /// <summary>
    /// The AddStock event
    /// </summary>
    public class TStockAdd: IStockEvent
    {
        /// <summary>
        /// Genotype of the animals to enter the simulation. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string genotype { get; set; }
        /// <summary>
        /// Total number of animals to enter the simulation. 
        /// The animals will be distributed across the age cohorts, taking the genotype-specific death rate into account.
        /// </summary>
        public int number { get; set; }
        /// <summary>
        /// Sex of the animals. Feasible values are as for sheep:sex or cattle:sex, as appropriate.
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// Day of year (1-365) on which all animals are assumed to have been born.
        /// </summary>
        public int birth_day { get; set; }
        /// <summary>
        /// Age in years of the youngest age cohort (their exact age will depend on the current day of year and the value of birth_day).
        /// </summary>
        public int min_years { get; set; }
        /// <summary>
        /// Age in years of the oldest age cohort
        /// </summary>
        public int max_years { get; set; }
        /// <summary>
        /// Average unfasted live weight of the animals across all age cohorts. 
        /// Animals in each age cohort will be given different weights, based on their normal weight for age, such that the overall average weight is that specified by this parameter. 
        /// This parameter may also be set to zero, in which case a default set of live weights will be computed, taking cond_score into account if it is nonzero.
        /// kg
        /// </summary>
        public double mean_weight { get; set; }
        /// <summary>
        /// Average condition score of the animals (assumed to be the same for all age cohorts). 
        /// If a value of zero is given, the default condition score for the weight and age will be used.
        /// </summary>
        public double cond_score { get; set; }
        /// <summary>
        /// Average greasy fleece weight of the animals across all age cohorts. 
        /// Different values will be computed for each age cohort, such that the weighted average fleece weight equals the specified value. 
        /// This parameter may be set to zero, in which case a default set of fleece weights will be computed based on the current day of year and the shear_day parameter. 
        /// Only meaningful in sheep.
        /// kg
        /// </summary>
        public double mean_fleece_wt { get; set; }
        /// <summary>
        /// Day of year on which the animals were last shorn. Only meaningful in sheep.
        /// </summary>
        public int shear_day { get; set; }
        /// <summary>
        /// Genotype of the rams or bulls with which the animals were mated prior to entry. 
        /// Only meaningful if pregnant or lactating is non-zero. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string mated_to { get; set; }
        /// <summary>
        /// Zero denotes no animals are pregnant; 1 or more denotes the time since conception of those animals that are pregnant. 
        /// Only meaningful for females.
        /// d
        /// </summary>
        public int pregnant { get; set; }
        /// <summary>
        /// Average number of foetuses per animal (including barren animals) across all age classes. 
        /// Different pregnancy rates will be computed for each age cohort, such that the weighted average number of foetuses per animal equals the specified value. 
        /// Only meaningful for females.
        /// </summary>
        public double foetuses { get; set; }
        /// <summary>
        /// Zero denotes no animals are lactating; 1 or more denotes the time since parturition in those animals that are lactating. 
        /// Only meaningful for females.
        /// d
        /// </summary>
        public int lactating { get; set; }
        /// <summary>
        /// Average number of suckling offspring per animal (including dry animals) across all age classes. 
        /// Different numbers of offspring will be computed for each age cohort, such that the weighted average number of offspring per animal equals the specified value. 
        /// Only meaningful for females.
        /// </summary>
        public double offspring { get; set; }
        /// <summary>
        /// Average unfasted live weight of any suckling lambs or calves.
        /// kg
        /// </summary>
        public double young_wt { get; set; }
        /// <summary>
        /// Average body condition score of any suckling lambs or calves.
        /// kg
        /// </summary>
        public double young_cond_score { get; set; }
        /// <summary>
        /// Average greasy fleece weight of any suckling lambs.
        /// kg
        /// </summary>
        public double young_fleece_wt { get; set; }
        /// <summary>
        /// Optional tag number to use.
        /// </summary>
        public int usetag { get; set; }
    }

    /// <summary>
    /// Buy stock
    /// </summary>
    public class TStockBuy: IStockEvent
    {
        /// <summary>
        /// Genotype of the animals to be bought. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string genotype { get; set; }
        /// <summary>
        /// Number of animals to be bought.
        /// </summary>
        public int number { get; set; }
        /// <summary>
        /// Sex of the animals. 
        /// Feasible values are as for sheep:sex or cattle:sex, as appropriate,
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// Average age of the animals.
        /// Months
        /// </summary>
        public double age { get; set; }
        /// <summary>
        /// Average unfasted live weight of the animals. 
        /// If a value of zero is given, a default value will be calculated, making use of the cond_score parameter if it is non-zero.
        /// kg
        /// </summary>
        public double weight { get; set; }
        /// <summary>
        /// Average greasy fleece weight of the animals. 
        /// Only meaningful in sheep.
        /// kg
        /// </summary>
        public double fleece_wt { get; set; }
        /// <summary>
        /// Average condition score of the animals. 
        /// If a value of zero is given, the default condition score for the weight and age will be used.
        /// </summary>
        public double cond_score { get; set; }
        /// <summary>
        /// Genotype of the rams or bulls with which the animals were mated prior to entry. 
        /// Only meaningful if pregnant or lactating is non-zero. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string mated_to { get; set; }
        /// <summary>
        /// Zero denotes not pregnant; 1 or more denotes the time since conception. 
        /// Only meaningful for females.
        /// d
        /// </summary>
        public int pregnant { get; set; }
        /// <summary>
        /// Zero denotes not lactating; 1 or more denotes the time since parturition in lactating animals. 
        /// Only meaningful for females.
        /// d
        /// </summary>
        public int lactating { get; set; }
        /// <summary>
        /// Number of foetuses and/or suckling offspring.
        /// </summary>
        public int no_young { get; set; }
        /// <summary>
        /// Average unfasted live weight of any suckling lambs or calves.
        /// kg
        /// </summary>
        public double young_wt { get; set; }
        /// <summary>
        /// Average greasy fleece weight of any suckling lambs.
        /// kg
        /// </summary>
        public double young_fleece_wt { get; set; }
        /// <summary>
        /// Optional tag to use.
        /// </summary>
        public int usetag { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TStockBuy()
        {
            mated_to = string.Empty;
        }
    }

    /// <summary>
    /// Removes animals from the simulation.  sell without parameters will remove all sheep in the stock sub-model
    /// </summary>
    public class TStockSell : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group from which animals are to be removed. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of animals has been removed.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Number of animals to remove.
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Castrate event
    /// </summary>
    public class TStockCastrate : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group, the lambs or calves of which are to be castrated. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of offspring has been castrated.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Number of male lambs or calves to be castrated.
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Dryoff event
    /// </summary>
    public class TStockDryoff : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group for which lactation is to end. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of cows has been dried off.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Number of females for which lactation is to end.
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Removes animals from the simulation by tag number.
    /// </summary>
    public class TStockSellTag : IStockEvent
    {
        /// <summary>
        /// Tag number of the animals from which animals are to be removed. 
        /// Animals are removed starting from the group with the smallest index.
        /// </summary>
        public int tag { get; set; }
        /// <summary>
        /// Number of animals to remove.
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Shears sheep. The event has no effect on cattle.
    /// </summary>
    public class TStockShear : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group to be shorn. 
        /// A value of zero denotes that all animal groups should be processed.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Denotes whether the main group of animals, suckling lambs, or both should be shorn. 
        /// Feasible values are the null string (main group), ‘adults’ (main group), ‘lambs’ (suckling lambs), ‘both’ (both).
        /// </summary>
        public string sub_group { get; set; }

    }

    /// <summary>
    /// Changes the paddock to which an animal group is assigned
    /// </summary>
    public class TStockMove : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group to be moved.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Name of the paddock to which the animal group is to be moved.
        /// </summary>
        public string paddock { get; set; }
    }

    /// <summary>
    /// Stock joining
    /// </summary>
    public class TStockJoin : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group for which mating is to commence. 
        /// A value of zero denotes that all empty females of sufficient age should be mated 
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Genotype of the rams or bulls with which the animals are mated. 
        /// Must match the name field of a member of the genotypes property.
        /// </summary>
        public string mate_to { get; set; }
        /// <summary>
        /// Length of the mating period.
        /// d
        /// </summary>
        public int mate_days { get; set; }

    }

    /// <summary>
    /// Weans some or all of the lambs or calves from an animal group. 
    /// The newly weaned animals are added to the end of the list of animal groups, with males and females in separate groups.
    /// </summary>
    public class TStockWean : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group from which animals are to be removed. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of lambs or calves has been weaned.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Feasible values are:
        /// ‘all’	    Female and male lambs or calves are to be weaned.
        /// ‘females’	Only female lambs or calves are to be weaned.
        /// ‘males’	    Only male lambs or calves are to be weaned
        /// </summary>
        public string sex { get; set; }
        /// <summary>
        /// Number of lambs or calves to be weaned.
        /// </summary>
        public int number { get; set; }
    }

    /// <summary>
    /// Creates new animal groups from all the animal groups.  The new groups are placed at the end of the animal group list. 
    /// This event is for when splits need to occur over all animal groups. Description of split event also applies.
    /// </summary>
    public class TStockSplitAll : IStockEvent
    {
        /// <summary>
        /// Feasible values are:
        /// ‘age’       All animals older than value days are moved to a new group.
        /// ‘weight’    All animals with live weight less than value kg are moved to a new group.
        /// ‘young’     Only animals with suckling offspring are affected.Mothers with different sexes of young are divided, with the group with all male offspring remaining in place.
        ///             For mothers with twins, three groups are created; a group with two male offspring, a group with two female offspring, and a group with one of each.
        /// ‘number’    value animals remain in place and the remainder form a new group
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Threshold age or weight, or the number to be split, depending on the value of type. Ignored if type is ‘young’.
        /// </summary>
        public double value { get; set; }
        /// <summary>
        /// If this value is given then the animals moved into the new animal group will have this tag number. 
        /// </summary>
        public int othertag { get; set; }
    }

    /// <summary>
    /// Creates two or more animal groups from the nominated group.
    /// </summary>
    public class TStockSplit : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group to be split.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Feasible values are:
        /// ‘age’       All animals older than value days are moved to a new group.
        /// ‘weight’    All animals with live weight less than value kg are moved to a new group.
        /// ‘young’     Only animals with suckling offspring are affected.Mothers with different sexes of young are divided, with the group with all male offspring remaining in place.
        ///             For mothers with twins, three groups are created; a group with two male offspring, a group with two female offspring, and a group with one of each.
        /// ‘number’    value animals remain in place and the remainder form a new group
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// Threshold age or weight, or the number to be split, depending on the value of type. Ignored if type is ‘young’.
        /// </summary>
        public double value { get; set; }
        /// <summary>
        /// If this value is given then the animals moved into the new animal group will have this tag number.
        /// </summary>
        public int othertag { get; set; }
    }

    /// <summary>
    /// Changes the “tag value” associated with an animal group.  
    /// This value is used to sort animals; it can also be used to group animals for user-defined purposes 
    /// (e.g. to identify animals that are to be managed as a single mob even though they differ physiologically) 
    /// and to keep otherwise similar animal groups distinct from one another.
    /// </summary>
    public class TStockTag : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group to be assigned a tag value.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// Tag value to be assigned.
        /// </summary>
        public int value { get; set; }
    }

    /// <summary>
    /// Sets the "priority" of an animal group for later use in a draft event. It is usual practice to use positive values for priorities.
    /// </summary>
    public class TStockPrioritise : IStockEvent
    {
        /// <summary>
        /// Index number of the animal group for which priority is to be set.
        /// </summary>
        public int group { get; set; }
        /// <summary>
        /// New priority value for the group.
        /// </summary>
        public int value { get; set; }
    }

    /// <summary>
    /// For the sort event.
    /// </summary>
    public class TStockSort : IStockEvent
    {

    }

    /// <summary>
    /// Draft event
    /// </summary>
    public class TStockDraft : IStockEvent
    {
        /// <summary>
        /// Names of paddocks to be excluded from consideration as possible destinations
        /// </summary>
        public string[] closed { get; set; }
    }

    
}
