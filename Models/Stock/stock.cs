namespace Models.GrazPlan
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Surface;
    using StdUnits;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// # Stock
    /// The STOCK component encapsulates the GRAZPLAN animal biology model, as described in [FREER1997].
    /// 
    /// [The GrazPlan animal model technical description](https://grazplan.csiro.au/wp-content/uploads/2007/08/TechPaperMay12.pdf)
    /// 
    /// Animals may be of different genotypes. In particular, sheep and cattle may be represented within a single STOCK instance.
    /// 
    /// Usually a single STOCK module is added to an AusFarm simulation, at the top level in the
    /// module hierarchy.
    /// 
    /// In a grazing system, however, there may be a variety of different classes of livestock. Animals
    /// may be of different genotypes (including both sheep and cattle); may be males, females or
    /// castrates; are likely to have a range of different ages; and females may be pregnant and/or
    /// lactating. The set of classes of livestock can change over time as animals enter or leave the
    /// system, are mated, give birth or are weaned. Further, animals that are otherwise similar may be
    /// placed in different paddocks, where their growth rates may differ.
    /// 
    /// ![Alt Text](StockGroupsExample.png)
    /// 
    /// **Figure [FigureNumber]:**  The list of animal groups at a particular time during a hypothetical simulation containing a
    /// STOCK module. Group 1 is distinct from the others because it has a different genotype and sex. Groups 2
    /// and 3 are distinct because they are in different age classes (yearling vs mature). Groups 2 and 4 are
    /// distinct because they are in different reproductive states (pregnant vs lactating). Note how the unweaned
    /// lambs are associated with their mothers.
    /// 
    /// In the STOCK component, this complexity is handled by representing the set of animals in a
    /// simulated system as a list of animal groups (Figure 2.1). The members of each animal group
    /// have the same genotype and age class, but may have a range of ages (for example, an animal
    /// group containing mature animals may include four-year-old, five-year-old and six-year-old
    /// stock). The members of each animal group also have the same stage of pregnancy and/or
    /// lactation; the same number of suckling offspring; and occupy the same paddock.
    /// 
    /// The set of animal groups changes as animals enter and leave the simulation, and as
    /// physiological events such as maturation, mating, birth or weaning take place. Animal groups
    /// that become sufficiently similar are merged into a single group. The state of any unweaned
    /// lambs or calves is stored alongside that of their mothers; at weaning, the male and female
    /// weaners are transferred into two new animal groups within the main list.
    /// 
    /// In addition to the biological state variables that describe the animals, each animal group has
    /// four attributes that are of particular interest when writing management scripts.
    /// 
    /// **Index**
    /// 
    /// Each animal group has a unique, internally-assigned integer index, starting at 1.
    /// Because the set of groups present in a component instance is dynamic, the index
    /// number associated with a particular group of animals can – and usually does – change
    /// over time. This dynamic numbering scheme has consequences for the way that animals
    /// of a particular kind must be located when writing management scripts.
    /// 
    /// **Paddock**
    /// 
    /// Each animal group is also assigned a paddock. The forage and supplementary feed
    /// available to a group of animals are determined by the paddock it occupies. Paddocks are
    /// referred to by name in the STOCK component:
    /// 
    /// * To set the paddock occupied by an animal group, use the **Move** event.
    /// * To determine the paddock occupied by an animal group, use the **Paddock** variable.
    /// 
    /// It is the user’s responsibility to ensure that paddock names correspond to PADDOCK
    /// modules or other sources of necessary driving variables.
    /// 
    /// **Tag Value**
    /// 
    /// Each animal group also has a user-assigned tag value that takes an integer value. Tag
    /// values have two purposes:
    /// 
    /// * They can be used to manage distinct groups of animals in a common fashion. For
    /// example, all lactating ewes might be assigned the same tag value, and then all
    /// animals with this tag value might undergo the same supplementary feeding regime.
    /// * If tag values are assigned sequentially (starting at 1), they can be used to generate
    /// summary variables. For example, **WeightTag[1]** gives the average live weight
    /// of all animals in groups with a tag value of 1.
    /// 
    /// Note that animal groups with different tag values are never merged, even if they are
    /// otherwise similar.
    /// 
    /// * To set the tag value of an animal group, use the **Tag** method.
    /// * To determine the tag value of an animal group, use the **TagNo** variable.
    /// 
    ///  **Merging groups of similar animals**
    ///  
    /// Animal groups that become sufficiently similar are merged into a single group.
    /// Animals are similar if all these are the same:
    /// 
    /// * Occupy the same paddock
    /// * Reproduction status (Castrated, Male, Empty, Early Preg,  Late Preg)
    /// * Number of foetuses
    /// * Mating cycle (day in the mating cycle)
    /// * Days to mating (Days left in joining period)
    /// * Pregnancy (Days since conception)
    /// * Lactation status (Days since parturition (if lactating)) – within 7 days
    /// * Has (not) young
    /// * If young exist, their reproductive status must be the same
    /// * Implants (hormone implants)
    /// * Mean age (if the animals are less than one year old )
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.MarkdownView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Stock : Model
    {
        /// <summary>
        /// The list of user specified forage component names
        /// </summary>
        private readonly List<string> userForages;

        /// <summary>
        /// The list of user specified paddocks
        /// </summary>
        private readonly List<string> userPaddocks;

        /// <summary>
        /// The random number host
        /// </summary>
        public MyRandom randFactory;

        /// <summary>
        /// The supplement used
        /// </summary>
        private FoodSupplement suppFed;

        /// <summary>
        /// The excretion info
        /// </summary>
        private ExcretionInfo excretionInfo;

        /// <summary>
        /// Used to show it is unset
        /// </summary>
        internal const int UNKNOWN = -1;

        #region Class links
        /// <summary>
        /// The simulation clock
        /// </summary>
        [Link]
        private Clock systemClock = null;

        /// <summary>
        /// The simulation weather component
        /// </summary>
        [Link]
        private IWeather locWtr = null;

        /// <summary>
        /// The supplement component
        /// </summary>
        [Link(IsOptional = true)]
        private Supplement suppFeed = null;

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary outputSummary = null;

        [Link]
        private List<Zone> paddocks = null;

        #endregion

        /// <summary>
        /// The Stock class constructor
        /// </summary>
        public Stock() : base()
        {
            this.userForages = new List<string>();
            this.userPaddocks = new List<string>();
            this.randFactory = new MyRandom(this.RandSeed);       // random number generator

            this.suppFed = new FoodSupplement();
            this.excretionInfo = new ExcretionInfo();
        }

        #region Initialisation properties ====================================================

        /// <summary>
        /// The seed for the random number generator. Used when computing numbers of animals dying and conceiving from the equations for mortality and conception rates.
        /// </summary>
        public int RandSeed { get; set; } = 0;

        /// <summary>
        /// An instance that contains all stock genotypes.
        /// </summary>
        public Genotypes Genotypes { get; } = new Genotypes();

        /// <summary>
        /// Gives access to the list of animals. Needed for unit testing.
        /// </summary>
        public StockList StockModel { get; private set; }

        /// <summary>List of animal groups.</summary>
        public IList<AnimalGroup> AnimalGroups { get { return StockModel.Animals.Skip(1).ToList(); } }

        /// <summary>Return animal groups that have a specific tag number.</summary>
        /// <param name="tag">Tag number of animal groups to return.</param>
        public IEnumerable<AnimalGroup> ByTag(int tag) { return AnimalGroups.Where(animalGroup => animalGroup.Tag == tag); }

        #endregion

        #region Readable properties ====================================================
        /// <summary>Mass of grazers per unit area</summary>
        [Units("kg/ha")]
        public double Trampling
        {
            get
            {   // TODO: complete the function

                ForageProvider forageProvider;

                // using the component ID
                // return the mass per area for all forages
                forageProvider = this.StockModel.ForagesAll.FindProvider(0);
                return this.StockModel.ReturnMassPerArea(StockModel.Paddocks[0], forageProvider, "kg/ha"); // by paddock or from forage ref
            }
        }

        /// <summary>
        /// Gets the consumption of supplementary feed by animals
        /// </summary>
        [Units("-")]
        public SupplementEaten[] SuppEaten
        {
            get
            {
                SupplementEaten[] value = null;
                StockVars.MakeSuppEaten(this.StockModel, ref value);
                return value;
            }
        }

        /// <summary>
        /// Gets the number of animal groups
        /// </summary>
        [Units("-")]
        public int NoGroups
        {
            get
            {
                return this.StockModel.Count();
            }
        }

        // =============== All ============

        /// <summary>
        /// Gets the number of animals in each group
        /// </summary>
        [Units("-")]
        public int[] Number
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eBoth, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of animals
        /// </summary>
        [Units("-")]
        public int NumberAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eBoth, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of animals in each tag group
        /// </summary>
        [Units("-")]
        public int[] NumberTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eBoth, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ============== Young ============

        /// <summary>
        /// Gets the number of unweaned young animals in each group
        /// </summary>
        [Units("-")]
        public int[] NumberYng
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eBoth, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of unweaned young animals
        /// </summary>
        [Units("-")]
        public int NumberYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eBoth, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned young animals in each group
        /// </summary>
        [Units("-")]
        public int[] NumberYngTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eBoth, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Female ============

        /// <summary>
        /// Gets the number of female animals in each group
        /// </summary>
        [Units("-")]
        public int[] NoFemale
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eFemale, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of female animals
        /// </summary>
        [Units("-")]
        public int NoFemaleAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eFemale, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of female animals in each tag group
        /// </summary>
        [Units("-")]
        public int[] NoFemaleTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eFemale, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Female Young ============

        /// <summary>
        /// Gets the number of unweaned female animals in each group
        /// </summary>
        [Units("-")]
        public int[] NoFemaleYng
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eFemale, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of unweaned female animals
        /// </summary>
        [Units("-")]
        public int NoFemaleYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eFemale, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned female animals in each tag group
        /// </summary>
        [Units("-")]
        public int[] NoFemaleYngTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eFemale, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Male ============

        /// <summary>
        /// Gets the number of male animals in each group
        /// </summary>
        [Units("-")]
        public int[] NoMale
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eMale, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of male animals
        /// </summary>
        [Units("-")]
        public int NoMaleAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eMale, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of male animals in each tag group
        /// </summary>
        [Units("-")]
        public int[] NoMaleTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eMale, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Male Young ============

        /// <summary>
        /// Gets the number of unweaned male animals in each group
        /// </summary>
        [Units("-")]
        public int[] NoMaleYng
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eMale, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of unweaned male animals
        /// </summary>
        [Units("-")]
        public int NoMaleYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eMale, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned male animals in each tag group
        /// </summary>
        [Units("-")]
        public int[] NoMaleYngTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eMale, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Deaths ================

        /// <summary>
        /// Gets the deaths of all non suckling animals
        /// </summary>
        [Units("-")]
        public int DeathsAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eDeaths, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the deaths of non suckling animals in each group
        /// </summary>
        [Units("-")]
        public int[] Deaths
        {
            get
            {
                int[] numbers = new int[this.StockModel.Count()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eDeaths, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the deaths of non suckling animals in each tag group
        /// </summary>
        [Units("-")]
        public int[] DeathsTag
        {
            get
            {
                int[] numbers = new int[this.StockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.StockModel, StockVars.CountType.eDeaths, false, false, true, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the sex field of the sheep and cattle initialisation variables. [wether | ram | steer | bull | ewe | heifer | cow]
        /// </summary>
        [Units("-")]
        public string[] Sex
        {
            get
            {
                string[] values = new string[this.StockModel.Count()];
                for (int idx = 0; idx < this.StockModel.Count(); idx++)
                    values[idx] = this.StockModel.SexString((int)idx, false);
                return values;
            }
        }

        // =========== Ages ==================

        /// <summary>
        /// Gets the age of animals by group.
        /// </summary>
        [Units("d")]
        public double[] Age
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of animals total
        /// </summary>
        [Units("d")]
        public double AgeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of animals by tag number
        /// </summary>
        [Units("d")]
        public double[] AgeTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Ages of young ==================

        /// <summary>
        /// Gets the age of unweaned young animals by group
        /// </summary>
        [Units("d")]
        public double[] AgeYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals total
        /// </summary>
        [Units("d")]
        public double AgeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals by tag number
        /// </summary>
        [Units("d")]
        public double[] AgeYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Ages months ==================

        /// <summary>
        /// Gets the age of animals, in months by group
        /// </summary>
        [Units("month")]
        public double[] AgeMonths
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE_MONTHS, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of animals, in months total
        /// </summary>
        [Units("month")]
        public double AgeMonthsAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE_MONTHS, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of animals, in months by tag number
        /// </summary>
        [Units("month")]
        public double[] AgeMonthsTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE_MONTHS, false, false, true, ref values);
                return values;
            }
        }

        // =========== Ages of young in months ==================

        /// <summary>
        /// Gets the age of unweaned young animals, in months by group
        /// </summary>
        [Units("month")]
        public double[] AgeMonthsYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE_MONTHS, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals, in months total
        /// </summary>
        [Units("month")]
        public double AgeMonthsYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE_MONTHS, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals, in months by tag number
        /// </summary>
        [Units("month")]
        public double[] AgeMonthsYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpAGE_MONTHS, true, false, true, ref values);
                return values;
            }
        }

        // =========== Weight ==================

        /// <summary>
        /// Gets the average live weight by group
        /// </summary>
        [Units("kg")]
        public double[] Weight
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLIVE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the averge live weight total
        /// </summary>
        [Units("kg")]
        public double WeightAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLIVE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the average live weight by tag number
        /// </summary>
        [Units("kg")]
        public double[] WeightTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLIVE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Weight of young ==================

        /// <summary>
        /// Gets the average live weight of unweaned young animals by group
        /// </summary>
        [Units("kg")]
        public double[] WeightYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLIVE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the average live weight of unweaned young animals total
        /// </summary>
        [Units("kg")]
        public double WeightYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLIVE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the average live weight of unweaned young animals by tag number
        /// </summary>
        [Units("kg")]
        public double[] WeightYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLIVE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Fleece-free, conceptus-free weight ==================

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight by group
        /// </summary>
        [Units("kg")]
        public double[] BaseWt
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBASE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight total
        /// </summary>
        [Units("kg")]
        public double BaseWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBASE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight by tag number
        /// </summary>
        [Units("kg")]
        public double[] BaseWtTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBASE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Fleece-free, conceptus-free weight young ==================

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight of unweaned young animals by group
        /// </summary>
        [Units("kg")]
        public double[] BaseWtYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBASE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight of unweaned young animals total
        /// </summary>
        [Units("kg")]
        public double BaseWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBASE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight of unweaned young animals by tag number
        /// </summary>
        [Units("kg")]
        public double[] BaseWtYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBASE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score of animals ==================

        /// <summary>
        /// Gets the condition score of animals (1-5 scale) by group
        /// </summary>
        [Units("-")]
        public double[] CondScore
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCOND_SCORE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the condition score of animals (1-5 scale) total
        /// </summary>
        [Units("-")]
        public double CondScoreAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCOND_SCORE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the condition score of animals (1-5 scale) by tag number
        /// </summary>
        [Units("-")]
        public double[] CondScoreTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCOND_SCORE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score of animals (1-5 scale) of young ==================

        /// <summary>
        /// Gets the condition score of unweaned young animals (1-5 scale) by group
        /// </summary>
        [Units("-")]
        public double[] CondScoreYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCOND_SCORE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the condition score of unweaned young animals (1-5 scale) total
        /// </summary>
        [Units("-")]
        public double CondScoreYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCOND_SCORE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the condition score of unweaned young animals (1-5 scale) by tag number
        /// </summary>
        [Units("-")]
        public double[] CondScoreYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCOND_SCORE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Maximum previous basal weight ==================

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained by each animal group
        /// </summary>
        [Units("kg")]
        public double[] MaxPrevWt
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMAX_PREV_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained total
        /// </summary>
        [Units("kg")]
        public double MaxPrevWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMAX_PREV_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained by tag number
        /// </summary>
        [Units("kg")]
        public double[] MaxPrevWtTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMAX_PREV_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Maximum previous basal weight young ==================

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by group
        /// </summary>
        [Units("kg")]
        public double[] MaxPrevWtYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMAX_PREV_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained unweaned young animals total
        /// </summary>
        [Units("kg")]
        public double MaxPrevWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMAX_PREV_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by tag number
        /// </summary>
        [Units("kg")]
        public double[] MaxPrevWtYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMAX_PREV_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current greasy fleece weight ==================

        /// <summary>
        /// Gets the current greasy fleece weight by group
        /// </summary>
        [Units("kg")]
        public double[] FleeceWt
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFLEECE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight total
        /// </summary>
        [Units("kg")]
        public double FleeceWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFLEECE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight by tag number
        /// </summary>
        [Units("kg")]
        public double[] FleeceWtTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFLEECE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current greasy fleece weight young ==================

        /// <summary>
        /// Gets the current greasy fleece weight of unweaned young animals by group
        /// </summary>
        [Units("kg")]
        public double[] FleeceWtYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFLEECE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight of unweaned young animals total
        /// </summary>
        [Units("kg")]
        public double FleeceWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFLEECE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight of unweaned young animals by tag number
        /// </summary>
        [Units("kg")]
        public double[] FleeceWtYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFLEECE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current clean fleece weight ==================

        /// <summary>
        /// Gets the current clean fleece weight by group
        /// </summary>
        [Units("kg")]
        public double[] CFleeceWt
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight total
        /// </summary>
        [Units("kg")]
        public double CFleeceWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight by tag number
        /// </summary>
        [Units("kg")]
        public double[] CFleeceWtTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current clean fleece weight young ==================

        /// <summary>
        /// Gets the current clean fleece weight of unweaned young animals by group
        /// </summary>
        [Units("kg")]
        public double[] CFleeceWtYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight of unweaned young animals total
        /// </summary>
        [Units("kg")]
        public double CFleeceWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight of unweaned young animals by tag number
        /// </summary>
        [Units("kg")]
        public double[] CFleeceWtYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current average wool fibre diameter ==================

        /// <summary>
        /// Gets the current average wool fibre diameter by group
        /// </summary>
        [Units("um")]
        public double[] FibreDiam
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFIBRE_DIAM, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter total
        /// </summary>
        [Units("um")]
        public double FibreDiamAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFIBRE_DIAM, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter by tag number
        /// </summary>
        [Units("um")]
        public double[] FibreDiamTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFIBRE_DIAM, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current average wool fibre diameter young ==================

        /// <summary>
        /// Gets the current average wool fibre diameter of unweaned young animals by group
        /// </summary>
        [Units("um")]
        public double[] FibreDiamYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFIBRE_DIAM, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter of unweaned young animals total
        /// </summary>
        [Units("um")]
        public double FibreDiamYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFIBRE_DIAM, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter of unweaned young animals by tag number
        /// </summary>
        [Units("um")]
        public double[] FibreDiamYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpFIBRE_DIAM, true, false, true, ref values);
                return values;
            }
        }

        // =========== If the animals are pregnant, the number of days since conception; zero otherwise ==================

        /// <summary>
        /// Gets the the pregnecy status. If the animals are pregnant, the number of days since conception; zero otherwise, by group
        /// </summary>
        [Units("d")]
        public double[] Pregnant
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpPREGNANT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the the pregnecy status. If the animals are pregnant, the number of days since conception; zero otherwise, total
        /// </summary>
        [Units("d")]
        public double PregnantAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpPREGNANT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the the pregnecy status. If the animals are pregnant, the number of days since conception; zero otherwise, by tag number
        /// </summary>
        [Units("d")]
        public double[] PregnantTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpPREGNANT, false, false, true, ref values);
                return values;
            }
        }

        // =========== If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise ==================

        /// <summary>
        /// Gets the lactation status. If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by group
        /// </summary>
        [Units("d")]
        public double[] Lactating
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLACTATING, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the lactation status. If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, total
        /// </summary>
        [Units("d")]
        public double LactatingAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLACTATING, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the lactation status. If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by tag number
        /// </summary>
        [Units("d")]
        public double[] LactatingTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpLACTATING, false, false, true, ref values);
                return values;
            }
        }

        // =========== Number of foetuses per head ==================

        /// <summary>
        /// Gets the number of foetuses per head by group
        /// </summary>
        [Units("-")]
        public double[] NoFoetuses
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpNO_FOETUSES, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the number of foetuses per head total
        /// </summary>
        [Units("-")]
        public double NoFoetusesAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpNO_FOETUSES, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the number of foetuses per head by tag number
        /// </summary>
        [Units("-")]
        public double[] NoFoetusesTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpNO_FOETUSES, false, false, true, ref values);
                return values;
            }
        }

        // AddScalarSet(ref   Idx, StockProps.prpNO_SUCKLING, "no_suckling", TTypedValue.TBaseType.ITYPE_DOUBLE, "", false, "Number of unweaned lambs or calves per head", "");
        // =========== Number of unweaned lambs or calves per head ==================

        /// <summary>
        /// Gets the number of unweaned lambs or calves per head by group
        /// </summary>
        [Units("-")]
        public double[] NoSuckling
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpNO_SUCKLING, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the number of unweaned lambs or calves per head total
        /// </summary>
        [Units("-")]
        public double NoSucklingAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpNO_SUCKLING, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned lambs or calves per head by tag number
        /// </summary>
        [Units("-")]
        public double[] NoSucklingTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpNO_SUCKLING, false, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score at last parturition; zero if lactating=0 ==================

        /// <summary>
        /// Gets the condition score at last parturition; zero if lactating=0, by group
        /// </summary>
        [Units("-")]
        public double[] BirthCS
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBIRTH_CS, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the condition score at last parturition; zero if lactating=0, total
        /// </summary>
        [Units("-")]
        public double BirthCSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBIRTH_CS, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the condition score at last parturition; zero if lactating=0, by tag number
        /// </summary>
        [Units("-")]
        public double[] BirthCSTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpBIRTH_CS, false, false, true, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the paddock occupied by each animal group
        /// </summary>
        [Units("-")]
        public string[] Paddock { get { return StockModel.Paddocks.Skip(1).Select(p => p.Name).ToArray(); } }

        /// <summary>
        /// Gets the tag value assigned to each animal group
        /// </summary>
        [Units("-")]
        public int[] TagNo { get { return StockModel.Animals.Skip(1).Select(p => p.Tag).ToArray(); } }

        // =========== Dry sheep equivalents, based on potential intake ==================

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake by group
        /// </summary>
        [Units("-")]
        public double[] DSE
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDSE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake total
        /// </summary>
        [Units("-")]
        public double DSEAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDSE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake by tag number
        /// </summary>
        [Units("-")]
        public double[] DSETag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDSE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Dry sheep equivalents, based on potential intake young ==================

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake of unweaned young animals by group
        /// </summary>
        [Units("-")]
        public double[] DSEYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDSE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake of unweaned young animals total
        /// </summary>
        [Units("-")]
        public double DSEYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDSE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake of unweaned young animals by tag number
        /// </summary>
        [Units("-")]
        public double[] DSEYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDSE, true, false, true, ref values);
                return values;
            }
        }

        // AddScalarSet(ref   Idx, StockProps., "wt_change", TTypedValue.TBaseType.ITYPE_DOUBLE, "kg/d", true, "Rate of change of base weight of each animal group", "");
        // =========== Rate of change of base weight of each animal group ==================

        /// <summary>
        /// Gets the rate of change of base weight of each animal by group
        /// </summary>
        [Units("kg/d")]
        public double[] WtChange
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpWT_CHANGE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of each animal total
        /// </summary>
        [Units("kg/d")]
        public double WtChangeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpWT_CHANGE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of each animal by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] WtChangeTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpWT_CHANGE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Rate of change of base weight of each animal group young ==================

        /// <summary>
        /// Gets the rate of change of base weight of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] WtChangeYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpWT_CHANGE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double WtChangeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpWT_CHANGE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] WtChangeYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpWT_CHANGE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Total intake per head of dry matter and nutrients by each animal group ==================

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients by each animal group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] Intake
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients
        /// </summary>
        [Units("-")]
        public DMPoolHead IntakeAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] IntakeTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Total intake per head of dry matter and nutrients of unweaned animals by group ==================

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] IntakeYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients of unweaned animals
        /// </summary>
        [Units("-")]
        public DMPoolHead IntakeYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] IntakeYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of pasture dry matter and nutrients by each animal group ==================

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients by each animal group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] PastIntake
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_PAST, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients
        /// </summary>
        [Units("-")]
        public DMPoolHead PastIntakeAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_PAST, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] PastIntakeTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_PAST, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of pasture dry matter and nutrients of unweaned animals by group ==================

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] PastIntakeYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_PAST, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients of unweaned animals
        /// </summary>
        [Units("-")]
        public DMPoolHead PastIntakeYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_PAST, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] PastIntakeYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_PAST, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of supplement dry matter and nutrients by each animal group ==================

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients by each animal group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] SuppIntake
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_SUPP, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients
        /// </summary>
        [Units("-")]
        public DMPoolHead SuppIntakeAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_SUPP, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] SuppIntakeTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_SUPP, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of supplement dry matter and nutrients of unweaned animals by group ==================

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] SuppIntakeYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_SUPP, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients of unweaned animals
        /// </summary>
        [Units("-")]
        public DMPoolHead SuppIntakeYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_SUPP, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] SuppIntakeYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINTAKE_SUPP, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of metabolizable energy ==================

        /// <summary>
        /// Gets the intake per head of metabolizable energy by group
        /// </summary>
        [Units("MJ/d")]
        public double[] MEIntake
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpME_INTAKE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy total
        /// </summary>
        [Units("MJ/d")]
        public double MEIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpME_INTAKE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy by tag number
        /// </summary>
        [Units("MJ/d")]
        public double[] MEIntakeTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpME_INTAKE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of metabolizable energy of young ==================

        /// <summary>
        /// Gets the intake per head of metabolizable energy of unweaned young animals by group
        /// </summary>
        [Units("MJ/d")]
        public double[] MEIntakeYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpME_INTAKE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy of unweaned young animals total
        /// </summary>
        [Units("MJ/d")]
        public double MEIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpME_INTAKE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy of unweaned young animals by tag number
        /// </summary>
        [Units("MJ/d")]
        public double[] MEIntakeYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpME_INTAKE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Crude protein intake per head ==================

        /// <summary>
        /// Gets the crude protein intake per head by group
        /// </summary>
        [Units("kg/d")]
        public double[] CPIntake
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCPI_INTAKE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head total
        /// </summary>
        [Units("kg/d")]
        public double CPIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCPI_INTAKE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] CPIntakeTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCPI_INTAKE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Crude protein intake per head of young ==================

        /// <summary>
        /// Gets the crude protein intake per head of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] CPIntakeYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCPI_INTAKE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double CPIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCPI_INTAKE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] CPIntakeYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCPI_INTAKE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Growth rate of clean fleece ==================

        /// <summary>
        /// Gets the growth rate of clean fleece by group
        /// </summary>
        [Units("kg/d")]
        public double[] CFleeceGrowth
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_GROWTH, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece total
        /// </summary>
        [Units("kg/d")]
        public double CFleeceGrowthAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_GROWTH, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] CFleeceGrowthTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_GROWTH, false, false, true, ref values);
                return values;
            }
        }

        // =========== Growth rate of clean fleece of young ==================

        /// <summary>
        /// Gets the growth rate of clean fleece of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] CFleeceGrowthYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_GROWTH, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double CFleeceGrowthYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_GROWTH, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] CFleeceGrowthYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCFLEECE_GROWTH, true, false, true, ref values);
                return values;
            }
        }

        // =========== Fibre diameter of the current day's wool growth ==================

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth by group
        /// </summary>
        [Units("um")]
        public double[] FibreGrowthDiam
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDAY_FIBRE_DIAM, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth total
        /// </summary>
        [Units("um")]
        public double FibreGrowthDiamAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDAY_FIBRE_DIAM, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth by tag number
        /// </summary>
        [Units("um")]
        public double[] FibreGrowthDiamTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDAY_FIBRE_DIAM, false, false, true, ref values);
                return values;
            }
        }

        // =========== Fibre diameter of the current day's wool growth of young ==================

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth of unweaned young animals by group
        /// </summary>
        [Units("um")]
        public double[] FibreGrowthDiamYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDAY_FIBRE_DIAM, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth of unweaned young animals total
        /// </summary>
        [Units("um")]
        public double FibreGrowthDiamYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDAY_FIBRE_DIAM, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth of unweaned young animals by tag number
        /// </summary>
        [Units("um")]
        public double[] FibreGrowthDiamYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpDAY_FIBRE_DIAM, true, false, true, ref values);
                return values;
            }
        }

        // =========== Weight of milk produced per head, on a 4pc fat-corrected basis ==================

        /// <summary>
        /// Gets the weight of milk produced per head, on a 4pc fat-corrected basis by group
        /// </summary>
        [Units("kg/d")]
        public double[] MilkWt
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMILK_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the weight of milk produced per head, on a 4pc fat-corrected basis total
        /// </summary>
        [Units("kg/d")]
        public double MilkWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMILK_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the weight of milk produced per head, on a 4pc fat-corrected basis by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] MilkWtTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMILK_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Metabolizable energy produced in milk (per head) by each animal group ==================

        /// <summary>
        /// Gets the metabolizable energy produced in milk (per head) by each animal group by group
        /// </summary>
        [Units("MJ/d")]
        public double[] MilkME
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMILK_ME, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the metabolizable energy produced in milk (per head) by each animal group total
        /// </summary>
        [Units("MJ/d")]
        public double MilkMEAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMILK_ME, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the metabolizable energy produced in milk (per head) by each animal group by tag number
        /// </summary>
        [Units("MJ/d")]
        public double[] MilkMETag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpMILK_ME, false, false, true, ref values);
                return values;
            }
        }

        // =========== Nitrogen retained within the animals, on a per-head basis ==================

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis by group
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedN
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_N, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis total
        /// </summary>
        [Units("kg/d")]
        public double RetainedNAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_N, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedNTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_N, false, false, true, ref values);
                return values;
            }
        }

        // =========== Nitrogen retained within the animals, on a per-head basis of young ==================

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedNYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_N, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double RetainedNYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_N, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedNYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_N, true, false, true, ref values);
                return values;
            }
        }

        // AddScalarSet(ref   Idx, StockProps., "retained_p", TTypedValue.TBaseType.ITYPE_DOUBLE, "", true, "", "");
        // =========== Phosphorus retained within the animals, on a per-head basis ==================

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis by group
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedP
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_P, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis total
        /// </summary>
        [Units("kg/d")]
        public double RetainedPAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_P, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedPTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_P, false, false, true, ref values);
                return values;
            }
        }

        // =========== Phosphorus retained within the animals, on a per-head basis of young ==================

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedPYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_P, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double RetainedPYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_P, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedPYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_P, true, false, true, ref values);
                return values;
            }
        }

        // =========== Sulphur retained within the animals, on a per-head basis ==================

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis by group
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedS
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_S, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis total
        /// </summary>
        [Units("kg/d")]
        public double RetainedSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_S, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedSTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_S, false, false, true, ref values);
                return values;
            }
        }

        // =========== Sulphur retained within the animals, on a per-head basis of young ==================

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedSYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_S, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double RetainedSYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_S, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RetainedSYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRETAINED_S, true, false, true, ref values);
                return values;
            }
        }

        // =========== Faecal dry matter and nutrients per head ==================

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head by each animal group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] Faeces
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpFAECES, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head
        /// </summary>
        [Units("-")]
        public DMPoolHead FaecesAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpFAECES, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] FaecesTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpFAECES, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Faecal dry matter and nutrients per head of unweaned animals ==================

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head of unweaned animals by group
        /// </summary>
        [Units("-")]
        public DMPoolHead[] FaecesYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpFAECES, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head of unweaned animals
        /// </summary>
        [Units("-")]
        public DMPoolHead FaecesYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpFAECES, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head of unweaned animals by tag
        /// </summary>
        [Units("-")]
        public DMPoolHead[] FaecesYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpFAECES, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Inorganic nutrients excreted in faeces, per head ==================

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head by each animal group
        /// </summary>
        [Units("-")]
        public InorgFaeces[] FaecesInorg
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINORG_FAECES, false, false, false, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].N = pools[i].N;
                    inorgpools[i].P = pools[i].P;
                    inorgpools[i].S = pools[i].S;
                }
                return inorgpools;
            }
        }

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head
        /// </summary>
        [Units("-")]
        public InorgFaeces FaecesInorgAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINORG_FAECES, false, true, false, ref pools);
                inorgpools[0].N = pools[0].N;
                inorgpools[0].P = pools[0].P;
                inorgpools[0].S = pools[0].S;
                return inorgpools[0];
            }
        }

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head by tag
        /// </summary>
        [Units("-")]
        public InorgFaeces[] FaecesInorgTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINORG_FAECES, false, false, true, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].N = pools[i].N;
                    inorgpools[i].P = pools[i].P;
                    inorgpools[i].S = pools[i].S;
                }
                return inorgpools;
            }
        }

        // =========== Inorganic nutrients excreted in faeces, per head of unweaned animals ==================

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head of unweaned animals by group
        /// </summary>
        [Units("-")]
        public InorgFaeces[] FaecesInorgYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.Count()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINORG_FAECES, true, false, false, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].N = pools[i].N;
                    inorgpools[i].P = pools[i].P;
                    inorgpools[i].S = pools[i].S;
                }
                return inorgpools;
            }
        }

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head of unweaned animals
        /// </summary>
        [Units("-")]
        public InorgFaeces FaecesInorgYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINORG_FAECES, true, true, false, ref pools);
                inorgpools[0].N = pools[0].N;
                inorgpools[0].P = pools[0].P;
                inorgpools[0].S = pools[0].S;
                return inorgpools[0];
            }
        }

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head of unweaned animals by tag
        /// </summary>
        [Units("-")]
        public InorgFaeces[] FaecesInorgYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.StockModel.HighestTag()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.StockModel, StockProps.prpINORG_FAECES, true, false, true, ref pools);
                for (int i = 0; i < pools.Length; i++)
                {
                    inorgpools[i].N = pools[i].N;
                    inorgpools[i].P = pools[i].P;
                    inorgpools[i].S = pools[i].S;
                }
                return inorgpools;
            }
        }

        /// <summary>
        /// Gets the metabolizable energy use for each animal group
        /// </summary>
        [Units("-")]
        public EnergyUse[] EnergyUse
        {
            get
            {
                EnergyUse[] use = new EnergyUse[this.StockModel.Count()];
                StockVars.MakeEnergyUse(this.StockModel, ref use);
                return use;
            }
        }

        // =========== Output of methane (per head) ==================

        /// <summary>
        /// Gets the output of methane (per head) by group
        /// </summary>
        [Units("kg/d")]
        public double[] Methane
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCH4_OUTPUT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) total
        /// </summary>
        [Units("kg/d")]
        public double MethaneAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCH4_OUTPUT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] MethaneTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCH4_OUTPUT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Output of methane (per head) of young ==================

        /// <summary>
        /// Gets the output of methane (per head) of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] MethaneYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCH4_OUTPUT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double MethaneYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCH4_OUTPUT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] MethaneYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpCH4_OUTPUT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary nitrogen output per head ==================

        /// <summary>
        /// Gets the urinary nitrogen output per head by group
        /// </summary>
        [Units("kg/d")]
        public double[] UrineN
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_N, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head total
        /// </summary>
        [Units("kg/d")]
        public double UrineNAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_N, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] UrineNTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_N, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary nitrogen output per head of young ==================

        /// <summary>
        /// Gets the urinary nitrogen output per head of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] UrineNYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_N, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double UrineNYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_N, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] UrineNYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_N, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary phosphorus output per head ==================

        /// <summary>
        /// Gets the urinary phosphorus output per head by group
        /// </summary>
        [Units("kg/d")]
        public double[] UrineP
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_P, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head total
        /// </summary>
        [Units("kg/d")]
        public double UrinePAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_P, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] UrinePTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_P, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary phosphorus output per head of young ==================

        /// <summary>
        /// Gets the urinary phosphorus output per head of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] UrinePYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_P, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double UrinePYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_P, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] UrinePYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_P, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary sulphur output per head ==================

        /// <summary>
        /// Gets the urinary sulphur output per head by group
        /// </summary>
        [Units("kg/d")]
        public double[] UrineS
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_S, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head total
        /// </summary>
        [Units("kg/d")]
        public double UrineSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_S, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] UrineSTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_S, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary sulphur output per head of young ==================

        /// <summary>
        /// Gets the urinary sulphur output per head of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] UrineSYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_S, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double UrineSYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_S, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] UrineSYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpURINE_S, true, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of rumen-degradable protein ==================

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein by group
        /// </summary>
        [Units("kg/d")]
        public double[] RDPIntake
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPI, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein total
        /// </summary>
        [Units("kg/d")]
        public double RDPIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPI, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RDPIntakeTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPI, false, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of rumen-degradable protein of young ==================

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] RDPIntakeYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPI, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double RDPIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPI, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RDPIntakeYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPI, true, false, true, ref values);
                return values;
            }
        }

        // =========== Requirement per head of rumen-degradable protein ==================

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein by group
        /// </summary>
        [Units("kg/d")]
        public double[] RDPReqd
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPR, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein total
        /// </summary>
        [Units("kg/d")]
        public double RDPReqdAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPR, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RDPReqdTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPR, false, false, true, ref values);
                return values;
            }
        }

        // =========== Requirement per head of rumen-degradable protein of young ==================

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein of unweaned young animals by group
        /// </summary>
        [Units("kg/d")]
        public double[] RDPReqdYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPR, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein of unweaned young animals total
        /// </summary>
        [Units("kg/d")]
        public double RDPReqdYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPR, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein of unweaned young animals by tag number
        /// </summary>
        [Units("kg/d")]
        public double[] RDPReqdYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDPR, true, false, true, ref values);
                return values;
            }
        }

        // =========== Effect of rumen-degradable protein availability on rate of intake  ==================

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by group
        /// </summary>
        [Units("0-1")]
        public double[] RDPFactor
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDP_EFFECT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) total
        /// </summary>
        [Units("0-1")]
        public double RDPFactorAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDP_EFFECT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by tag number
        /// </summary>
        [Units("0-1")]
        public double[] RDPFactorTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDP_EFFECT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Effect of rumen-degradable protein availability on rate of intake of young ==================

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by group
        /// </summary>
        [Units("0-1")]
        public double[] RDPFactorYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDP_EFFECT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals total
        /// </summary>
        [Units("0-1")]
        public double RDPFactorYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDP_EFFECT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by tag number
        /// </summary>
        [Units("0-1")]
        public double[] RDPFactorYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpRDP_EFFECT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Externally-imposed scaling factor for potential intake ==================

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake (0-1.0). This property is resettable by group
        /// </summary>
        [Units("-")]
        public double[] IntakeModifier
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpINTAKE_MOD, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake (0-1.0). This property is resettable, total
        /// </summary>
        [Units("-")]
        public double IntakeModifierAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpINTAKE_MOD, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake (0-1.0). This property is resettable by tag number
        /// </summary>
        [Units("-")]
        public double[] IntakeModifierTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpINTAKE_MOD, false, false, true, ref values);
                return values;
            }
        }

        // =========== Externally-imposed scaling factor for potential intake of young ==================

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake (0-1.0). This property is resettable, of unweaned young animals by group
        /// </summary>
        [Units("-")]
        public double[] IntakeModifierYng
        {
            get
            {
                double[] values = new double[this.StockModel.Count()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpINTAKE_MOD, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake (0-1.0). This property is resettable, of unweaned young animals total
        /// </summary>
        [Units("-")]
        public double IntakeModifierYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpINTAKE_MOD, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake (0-1.0). This property is resettable, of unweaned young animals by tag number
        /// </summary>
        [Units("-")]
        public double[] IntakeModifierYngTag
        {
            get
            {
                double[] values = new double[this.StockModel.HighestTag()];
                StockVars.PopulateRealValue(this.StockModel, StockProps.prpINTAKE_MOD, true, false, true, ref values);
                return values;
            }
        }

        #endregion readable properties

        #region Subscribed events ====================================================

        /// <summary>
        /// At the start of the simulation, initialise all the paddocks and forages and nitrogen returns.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            randFactory.Initialise(RandSeed);
            StockModel = new StockList(this, systemClock, locWtr, paddocks);

            var childGenotypes = this.FindAllChildren<Genotype>().Cast<Genotype>().ToList();
            if (childGenotypes != null)
                childGenotypes.ForEach(animalParamSet => Genotypes.Add(animalParamSet));

            int currentDay = systemClock.Today.Day + (systemClock.Today.Month * 0x100) + (systemClock.Today.Year * 0x10000);
        }

        /// <summary>
        /// Initialisation step
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("DoStock")]
        private void OnDoStock(object sender, EventArgs e)
        {
            //// for each paddock
            ////FModel.Paddocks.byID(1).fWaterlog = 0.0;    // TODO

            this.RequestAvailableToAnimal();  // accesses each forage provider (crop)

            foreach (var paddock in StockModel.Paddocks)
            {
                paddock.ClearSupplement();
                paddock.ZeroRemoval();
            }

            if (this.suppFeed != null)
            {
                SuppToStockType[] availSupp = this.suppFeed.SuppToStock;

                for (int idx = 0; idx < availSupp.Length; idx++)
                {
                    // each paddock
                    this.suppFed.SetSuppAttrs(availSupp[idx]);
                    this.StockModel.PlaceSuppInPadd(availSupp[idx].Paddock, availSupp[idx].Amount, this.suppFed, availSupp[idx].FeedSuppFirst);
                }
            }

            // Do internal management tasks that are defined for the various
            // enterprises. This includes shearing, buying, selling...
            int currentDay = this.systemClock.Today.Day + (this.systemClock.Today.Month * 0x100) + (this.systemClock.Today.Year * 0x10000);

            this.StockModel.Dynamics();

            ForageProvider forageProvider;

            // Return the amounts of forage removed
            for (int i = 0; i <= this.StockModel.ForagesAll.Count() - 1; i++)
            {
                forageProvider = this.StockModel.ForagesAll.ForageProvider(i);
                if (forageProvider.ForageObj != null)
                {
                    // if there is forage removed from this forage object/crop/pasture
                    if (forageProvider.SomethingRemoved())
                    {
                        forageProvider.RemoveHerbageFromPlant();
                    }
                }
                else
                    throw new ApsimXException(this, "No destination for forage removal");
            }

            // if destinations for the surface om and nutrients are known then
            // send the values to the components
            for (int idx = 0; idx <= this.StockModel.Paddocks.Count() - 1; idx++)
            {
                PaddockInfo paddInfo = this.StockModel.Paddocks[idx];

                if (paddInfo.AddFaecesObj != null)
                {
                    Surface.AddFaecesType faeces = new Surface.AddFaecesType();
                    if (this.PopulateFaeces(paddInfo, faeces))
                    {
                        ((SurfaceOrganicMatter)paddInfo.AddFaecesObj).AddFaeces(faeces);
                    }
                }
                if (paddInfo.AddUrineObj != null)
                {
                    AddUrineType urine = new AddUrineType();
                    if (this.PopulateUrine(paddInfo, urine))
                    {
                        // We could just add the urea to the top layer, but it's better
                        // to work out the penetration depth, and spread it through those layers.
                        double liquidDepth = urine.VolumePerUrination / urine.AreaPerUrination * 1000.0; // Depth of liquid to be added per urinat, in mm
                        double maxDepth = liquidDepth / 0.05; // basically treats soil as having 5% pore space. This is the depth to which urine will penetrate
                        double[] dlayers = paddInfo.SoilLayerThickness;
                        int nLayers = dlayers.Length;
                        double cumDepth = 0.0;
                        double[] ureaAdded = new double[nLayers];
                        for (int iLayer = 0; iLayer < nLayers; iLayer++)
                        {
                            double layerFrac = Math.Min(1.0, MathUtilities.Divide(maxDepth - cumDepth, dlayers[iLayer], 0.0));
                            ureaAdded[iLayer] = layerFrac > 0.0 ? urine.Urea * layerFrac * dlayers[iLayer] / maxDepth : 0.0;
                            cumDepth += dlayers[iLayer];
                        }
                        ((ISolute)paddInfo.AddUrineObj).AddKgHaDelta(SoluteSetterType.Other, ureaAdded);
                    }
                }
            }
        }
        #endregion

        #region Management methods ============================================
        // ............................................................................
        // Management methods                                                         
        // ............................................................................

        /// <summary>
        /// Causes a set of related age cohorts of animals to enter the simulation. 
        /// Each age cohort may contain animals that are pregnant and/or lactating, in which case distributions of numbers of foetuses and/or suckling offspring are computed automatically. 
        /// This event is primarily intended to simplify the initialisation of flocks and herds in simulations.
        /// </summary>
        /// <param name="animals">The animal data</param>
        public void Add(StockAdd animals)
        {
            outputSummary.WriteMessage(this, "Adding " + animals.Number.ToString() + ", " + animals.Genotype + " " + animals.Sex);
            StockModel.Add(animals);
        }

        /// <summary>
        /// Buys animals (i.e. they enter the simulation). The purchased animals will form a new animal group that is placed at the end of the list of animal groups.
        /// </summary>
        /// <param name="stock">The stock data</param>
        public void Buy(StockBuy stock)
        {
            outputSummary.WriteMessage(this, "Buying " + stock.Number.ToString() + ", " + stock.Age.ToString() + " month old " + stock.Genotype + " " + stock.Sex.ToString() + " ");
            StockModel.Buy(stock);
        }

        /// <summary>
        /// Buys animals (i.e. they enter the simulation). The purchased animals will form a new animal group that is placed at the end of the list of animal groups.
        /// </summary>
        /// <param name="genotype">The genotype</param>
        /// <param name="number">The number of animals</param>
        /// <param name="sex">The sex of animals</param>
        /// <param name="age">The age of animals (months)</param>
        /// <param name="weight">The weight of animals (kg)</param>
        /// <param name="fleeceWeight">The fleece weight of animals (kg)</param>
        public void Buy(string genotype, double number, ReproductiveType sex, double age, double weight, double fleeceWeight)
        {
            StockBuy stock = new StockBuy();
            stock.Genotype = genotype;
            stock.Number = Convert.ToInt32(number, CultureInfo.InvariantCulture);
            stock.Sex = sex;
            stock.Age = age;
            stock.Weight = weight;
            stock.FleeceWt = fleeceWeight;
            outputSummary.WriteMessage(this, "Buying " + stock.Number.ToString() + ", " + stock.Age.ToString() + " month old " + stock.Genotype + " " + stock.Sex.ToString() + " ");
            StockModel.Buy(stock);
        }

        /// <summary>
        /// Remove the specified number of animals (not including unweaned lambs/calves).
        /// </summary>
        /// <param name="number">The number of animals to remove.</param>
        /// <param name="group">The animal group to remove animals from. Null denotes all groups.</param>
        /// <returns>The number of animals sold.</returns>
        public int Sell(int number, AnimalGroup group = null)
        {
            int numSold = 0;
            if (group == null)
            {
                foreach (var g in AnimalGroups)
                {
                    int numToSellFromThisGroup = Math.Min(number, g.NoAnimals);
                    g.NoAnimals -= numToSellFromThisGroup;
                    number -= numToSellFromThisGroup;
                    numSold += numToSellFromThisGroup;
                }
            }
            else
            {
                numSold = Math.Min(number, group.NoAnimals);
                group.NoAnimals -= numSold;
            }
            outputSummary.WriteMessage(this, $"Sold {number} animals");
            return numSold;
        }

        /// <summary>
        /// Remove the specified number of animals (not including unweaned lambs/calves)
        /// Will iterate through the groups specified, removing as many animals from each
        /// until the specified number has been reached. If groups is null, will iterate
        /// through all animal groups.
        /// </summary>
        /// <param name="number">The number of animals to remove.</param>
        /// <param name="groups">The animal group to remove animals from. Null denotes all groups.</param>
        /// <returns>The number of animals sold.</returns>
        public int Sell(int number, IEnumerable<AnimalGroup> groups)
        {
            int numSold = 0;
            foreach (var g in groups)
            {
                int numToSellFromThisGroup = Math.Min(number, g.NoAnimals);
                g.NoAnimals -= numToSellFromThisGroup;
                number -= numToSellFromThisGroup;
                numSold += numToSellFromThisGroup;
            }
            outputSummary.WriteMessage(this, $"Sold {numSold} animals");
            return numSold;
        }

        /// <summary>
        /// Shears sheep. The event has no effect on cattle.
        /// </summary>
        /// <param name="shearAdults">Shear adults?</param>
        /// <param name="shearYoung">Shear lambs?</param>
        /// <param name="group">The group to shear. null = all groups</param>
        /// <returns>cfw</returns>
        public double Shear(bool shearAdults, bool shearYoung, AnimalGroup group = null)
        {
            this.outputSummary.WriteMessage(this, "Shearing animals");
            double totalCFW = 0;
            if (group == null)
            {
                foreach (var g in AnimalGroups)
                    totalCFW += g.Shear(shearAdults, shearYoung);
            }
            else
                totalCFW = group.Shear(shearAdults, shearYoung);

            return totalCFW;
        }

        /// <summary>Moves animals to a specified paddock.</summary>
        /// <param name="paddockName">Name of the paddock to which the animal group is to be moved.</param>
        /// <param name="group">The animal group to move.</param>
        public void Move(string paddockName, AnimalGroup group = null)
        {
            this.outputSummary.WriteMessage(this, $"Moving animals to paddock {paddockName}");
            var paddockToMoveTo = StockModel.Paddocks.Find(p => p.Name.Equals(paddockName, StringComparison.InvariantCultureIgnoreCase));
            if (paddockToMoveTo == null)
                throw new Exception($"Stock: attempt to place animals in non-existent paddock: {paddockName}");
            if (group == null)
            {
                foreach (var g in AnimalGroups)
                    g.PaddOccupied = paddockToMoveTo;
            }
            else
                group.PaddOccupied = paddockToMoveTo;
        }

        /// <summary>
        /// Commences mating of a particular group of animals.  If the animals are not empty females, or if they are too young, has no effect
        /// </summary>
        /// <param name="mateTo">Genotype of the rams or bulls with which the animals are mated. 
        /// Must match the name field of a member of the genotypes property.</param>
        /// <param name="mateDays">Length of the mating period in days.</param>
        /// <param name="group">The animal group to mate. null denotes that all empty females of sufficient age should be mated.</param>
        public void Join(string mateTo, int mateDays, AnimalGroup group = null)
        {
            outputSummary.WriteMessage(this, $"Joining animals to {mateTo}");

            if (group == null)
            {
                foreach (var g in AnimalGroups)
                    g.Join(Genotypes.Get(mateTo), mateDays);
            }
            else
                group.Join(Genotypes.Get(mateTo), mateDays);
        }

        /// <summary>
        /// Converts ram lambs to wether lambs, or bull calves to steers.  If the animal group(s) denoted by group has no suckling young, has no effect. 
        /// If the number of male lambs or calves in a nominated group is greater than the number to be castrated, the animal group will be split; 
        /// the sub-group with castrated offspring will remain at the original index and the sub-group with offspring that were not castrated will 
        /// be added at the end of the set of animal groups.
        /// </summary>
        /// <param name="number">Number of male lambs or calves to be castrated.</param>
        /// <param name="group">The animal group to castrate. null denotes that each animal group should be processed in turn until the nominated number of offspring has been castrated.</param>
        public void Castrate(int number, AnimalGroup group = null)
        {
            outputSummary.WriteMessage(this, $"Castrate {number} animals");
            if (group == null)
            {
                foreach (var g in AnimalGroups)
                {
                    if (g.Young != null && g.Young.MaleNo > 0 && number > 0)
                    {
                        var numToCastrateFromThisGroup = Math.Min(number, g.Young.MaleNo);
                        if (numToCastrateFromThisGroup < g.Young.MaleNo)
                            StockModel.Split(g, Convert.ToInt32(Math.Round((double)number / numToCastrateFromThisGroup * g.NoAnimals), CultureInfo.InvariantCulture));  // TODO: check this conversion
                        g.Young.Castrate();
                        number -= numToCastrateFromThisGroup;
                    }
                }
            }
            else
            {
                var numToCastrateFromThisGroup = Math.Min(number, group.Young.MaleNo);
                if (numToCastrateFromThisGroup < group.Young.MaleNo)
                    StockModel.Split(group, Convert.ToInt32(Math.Round((double)number / numToCastrateFromThisGroup * group.NoAnimals), CultureInfo.InvariantCulture));  // TODO: check this conversion
                group.Young.Castrate();
            }
        }

        /// <summary>
        /// Weans some or all of the lambs or calves from an animal group. 
        /// The newly weaned animals are added to the end of the list of animal groups, with males and females in separate groups.
        /// </summary>
        /// <param name="number">The number of lambs or calves to be weaned.</param>
        /// <param name="weanMales">Wean the male animals?</param>
        /// <param name="weanFemales">Wean the female animals?</param>
        /// <param name="group">The animal group to wean. null denotes that each animal group should be processed in turn until the nominated number of lambs or calves has been weaned.</param>
        public void Wean(int number, bool weanMales, bool weanFemales, AnimalGroup group = null)
        {
            var msg = "Weaning";
            if (weanMales && weanFemales)
                msg += " males and females";
            else if (weanMales)
                msg += " males";
            else
                msg += " females";
            outputSummary.WriteMessage(this, msg);

            if (group == null)
            {
                foreach (var g in AnimalGroups)
                    number -= StockModel.Wean(g, number, weanFemales, weanMales);
            }
            else
                StockModel.Wean(group, number, weanFemales, weanMales);
        }

        /// <summary>
        /// Ends lactation in cows that have already had their calves weaned.  The event has no effect on other animals.
        /// If the number of cows in a nominated group is greater than the number to be dried off, the animal group will be split; 
        /// the sub-group that is no longer lactating will remain at the original index and the sub-group that continues lactating will be added at the end of the set of animal groups
        /// </summary>
        /// <param name="number">Number of females for which lactation is to end.</param>
        /// <param name="group">The animal group for which lactation is to end. Null denotes that each animal group should be processed in turn until the nominated number of cows has been dried off.</param>
        public void DryOff(int number, AnimalGroup group = null)
        {
            outputSummary.WriteMessage(this, $"Drying off {number} animals.");
            if (group == null)
                StockModel.DryOff(AnimalGroups, number);
            else
                StockModel.DryOff(new AnimalGroup[] { group }, number);
        }

        /// <summary>
        /// Split animal group by age
        /// </summary>
        /// <param name="age">Age in days</param>
        /// <param name="group">The animal group to split.</param>
        /// <returns>The new animal groups that were created.</returns>
        public IEnumerable<AnimalGroup> SplitByAge(int age, AnimalGroup group = null)
        {
            outputSummary.WriteMessage(this, "Split animals by age.");
            if (group == null)
                return StockModel.SplitByAge(age, AnimalGroups);
            else
                return StockModel.SplitByAge(age, new AnimalGroup[] { group });
        }

        /// <summary>
        /// Split animal group by weight
        /// </summary>
        /// <param name="weight">Weight to split on (kg/animal)</param>
        /// <param name="group">The animal group to split.</param>
        /// <returns>The new animal groups that were created.</returns>
        public IEnumerable<AnimalGroup> SplitByWeight(double weight, AnimalGroup group = null)
        {
            outputSummary.WriteMessage(this, "Split animals by weight.");
            if (group == null)
                return StockModel.SplitByWeight(weight, AnimalGroups);
            else
                return StockModel.SplitByWeight(weight, new AnimalGroup[] { group });
        }

        /// <summary>
        /// Split animal group by young.
        /// </summary>
        /// <param name="group">The animal group to split.</param>
        /// <returns>The new animal groups that were created.</returns>
        public IEnumerable<AnimalGroup> SplitByYoung(AnimalGroup group = null)
        {
            outputSummary.WriteMessage(this, "Split young animals off.");
            if (group == null)
                return StockModel.SplitByYoung(AnimalGroups);
            else
                return StockModel.SplitByYoung(new AnimalGroup[] { group });
        }

        /// <summary>
        /// Rearranges the list of animal groups in ascending order of tag value.
        /// </summary>
        public void Sort()
        {
            outputSummary.WriteMessage(this, "Sort animals by tag");
            StockModel.Sort();
        }

        #endregion ============================================

        #region Private functions ============================================

        /// <summary>
        /// Do a request for all the biomasses in every paddock
        /// Note: This could be optimised to not request paddocks that are unstocked (drafting still needs to get the amounts)
        /// </summary>
        private void RequestAvailableToAnimal()
        {
            ForageProvider forageProvider;

            // iterate through all the paddocks and sum the total green and store it in each forage provider
            for (int idx = 0; idx <= this.StockModel.Paddocks.Count() - 1; idx++)
            {
                double pastureGreen = 0;
                PaddockInfo paddInfo = this.StockModel.Paddocks[idx];
                for (int i = 0; i <= this.StockModel.ForagesAll.Count() - 1; i++)
                {
                    forageProvider = this.StockModel.ForagesAll.ForageProvider(i);
                    if (string.Compare(forageProvider.OwningPaddock.Name, paddInfo.Name, true) == 0)
                    {
                        if (forageProvider.ForageObj != null)
                        {
                            foreach (IOrganDamage biomass in forageProvider.ForageObj.Organs)
                            {
                                if (biomass.IsAboveGround)
                                {
                                    if (biomass.Live.Wt > 0)
                                    {
                                        pastureGreen += biomass.Live.Wt;   // g/m^2
                                    }
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i <= this.StockModel.ForagesAll.Count() - 1; i++)
                {
                    forageProvider = this.StockModel.ForagesAll.ForageProvider(i);
                    if (string.Compare(forageProvider.OwningPaddock.Name, paddInfo.Name, true) == 0)
                    {
                        forageProvider.PastureGreenDM = pastureGreen;
                    }
                }
            }

            // now update the available forages
            for (int i = 0; i <= this.StockModel.ForagesAll.Count() - 1; i++)
            {
                forageProvider = this.StockModel.ForagesAll.ForageProvider(i);
                if (forageProvider.ForageObj != null)
                {
                    forageProvider.UpdateForages(forageProvider.ForageObj);
                }
            }
        }

        /// <summary>
        /// Populate the AddFaecesType object
        /// </summary>
        /// <param name="paddock">The paddock</param>
        /// <param name="faecesValue">The faeces data</param>
        /// <returns>True if the number of defaecations > 0</returns>
        private bool PopulateFaeces(PaddockInfo paddock, Surface.AddFaecesType faecesValue)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;
            bool result = false;

            this.StockModel.ReturnExcretion(paddock, out this.excretionInfo);

            if (this.excretionInfo.Defaecations > 0)
            {
                faecesValue.Defaecations = this.excretionInfo.Defaecations;
                faecesValue.VolumePerDefaecation = this.excretionInfo.DefaecationVolume;
                faecesValue.AreaPerDefaecation = this.excretionInfo.DefaecationArea;
                faecesValue.Eccentricity = this.excretionInfo.DefaecationEccentricity;
                faecesValue.OMWeight = this.excretionInfo.OrgFaeces.DM;
                faecesValue.OMN = this.excretionInfo.OrgFaeces.Nu[n];
                faecesValue.OMP = this.excretionInfo.OrgFaeces.Nu[p];
                faecesValue.OMS = this.excretionInfo.OrgFaeces.Nu[s];
                faecesValue.OMAshAlk = this.excretionInfo.OrgFaeces.AshAlk;
                faecesValue.NO3N = this.excretionInfo.InOrgFaeces.Nu[n] * this.excretionInfo.FaecalNO3Propn;
                faecesValue.NH4N = this.excretionInfo.InOrgFaeces.Nu[n] * (1.0 - this.excretionInfo.FaecalNO3Propn);
                faecesValue.POXP = this.excretionInfo.InOrgFaeces.Nu[p];
                faecesValue.SO4S = this.excretionInfo.InOrgFaeces.Nu[s];
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Copy the urine info into the AddUrineType
        /// </summary>
        /// <param name="paddock">The paddock</param>
        /// <param name="urineValue">The urine data</param>
        /// <returns>True if the number of urinations > 0</returns>
        private bool PopulateUrine(PaddockInfo paddock, AddUrineType urineValue)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;
            bool result = false;

            this.StockModel.ReturnExcretion(paddock, out this.excretionInfo);
            if (this.excretionInfo.Urinations > 0)
            {
                urineValue.Urinations = this.excretionInfo.Urinations;
                urineValue.VolumePerUrination = this.excretionInfo.UrinationVolume;
                urineValue.AreaPerUrination = this.excretionInfo.UrinationArea;
                urineValue.Eccentricity = this.excretionInfo.dUrinationEccentricity;
                urineValue.Urea = this.excretionInfo.Urine.Nu[n];
                urineValue.POX = this.excretionInfo.Urine.Nu[p];
                urineValue.SO4 = this.excretionInfo.Urine.Nu[s];
                urineValue.AshAlk = this.excretionInfo.Urine.AshAlk;
                result = true;
            }
            return result;
        }

        #endregion

    }
}