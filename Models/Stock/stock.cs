namespace Models.GrazPlan
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.Surface;
    using StdUnits;

    /// <summary>
    /// #GrazPlan Stock
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
    /// **Priority Score**
    /// 
    /// Finally, each animal group has a user-assigned *priority score* that takes an integer value.
    /// Priority scores are used to control the operation of the **Draft** method. Positive values for
    /// the priority score denote the order in which animals should be moved to the available
    /// paddocks (with a score of 1 denoting that the animals should be moved to the highest-
    /// quality pasture). Animal groups with the same priority score are placed in the same
    /// paddock by a draft event. Animals with a zero or negative priority score are not
    /// drafted.
    /// 
    /// * To set the priority score of an animal group, use the prioritise event.
    /// * To determine the priority score of an animal group, use the priority variable. 
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
    /// 
    /// 
    /// ---
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.HTMLView")]
    [PresenterName("UserInterface.Presenters.GenericPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class Stock : Model
    {
        /// <summary>
        /// The list of user specified forage component names
        /// </summary>
        private List<string> userForages;

        /// <summary>
        /// The list of user specified paddocks
        /// </summary>
        private List<string> userPaddocks;

        /// <summary>
        /// The main stock model
        /// </summary>
        private StockList stockModel;

        /// <summary>
        /// Weather used by the model
        /// </summary>
        private AnimalWeather localWeather;

        /// <summary>
        /// True if at the first step of the run
        /// </summary>
        private bool isFirstStep;

        /// <summary>
        /// The init values for the animal
        /// </summary>
        private AnimalInits[] animalInits;

        /// <summary>
        /// If the paddocks are specified by the user
        /// </summary>
        private bool paddocksGiven;

        /// <summary>
        /// The random seed for the mortality model
        /// </summary>
        private int randSeed = 0;

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
        /// The current time value
        /// </summary>
        private DateTime currentTime;

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
        private Weather locWtr = null;

        /// <summary>
        /// The supplement component
        /// </summary>
        [Link(IsOptional = true)]
        private Supplement suppFeed = null;

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary outputSummary = null;

        #endregion

        /// <summary>
        /// The Stock class constructor
        /// </summary>
        public Stock() : base()
        {
            this.userForages = new List<string>();
            this.userPaddocks = new List<string>();
            this.randFactory = new MyRandom(this.randSeed);       // random number generator
            this.stockModel = new StockList(this);

            Array.Resize(ref this.animalInits, 0);
            this.suppFed = new FoodSupplement();
            this.excretionInfo = new ExcretionInfo();
            this.paddocksGiven = false;
            this.isFirstStep = true;
            this.randSeed = 0;
        }

        #region Initialisation properties ====================================================
        
        /// <summary>
        /// Gets or sets the Seed for the random number generator. Used when computing numbers of animals dying and conceiving from the equations for mortality and conception rates
        /// </summary>
        [Description("Random number seed for mortality and conception rates")]
        [Units("")]
        public int RandSeed
        {
            get { return this.randSeed; }
            set { this.randSeed = value; }
        }

        /// <summary>
        /// An instance that contains all stock genotypes.
        /// </summary>
        public Genotypes Genotypes { get; } = new Genotypes();

        /// <summary>
        /// Gets or sets the initial state of each animal group
        /// </summary>
        public AnimalInits[] Animals
        {
            get
            {
                //AnimalInits[] animal = new AnimalInits[1];
                //StockVars.MakeAnimalValue(this.stockModel, ref animal);
                return this.animalInits;
            }
            set
            {
                this.animalInits = value;
            }
        }

        /// <summary>
        /// Gives access to the list of animals. Needed for unit testing.
        /// </summary>
        public StockList AnimalList { get { return stockModel; } }

        /// <summary>
        /// Gets or sets the manually-specified structure of paddocks and forages 
        /// </summary>
        [Description("Manually-specified structure of paddocks and forages")]
        public PaddockInit[] PaddockList
        {
            get
            {
                PaddockInit[] paddocks = new PaddockInit[1];
                StockVars.MakePaddockList(this.stockModel, ref paddocks);
                return paddocks;
            }

            set
            {
                this.paddocksGiven = value.Length > 1;    // more than the null paddock
                PaddockInfo paddockInfo;
                if (this.paddocksGiven)
                {
                    while (this.stockModel.Paddocks.Count() > 0)
                        this.stockModel.Paddocks.Delete(this.stockModel.Paddocks.Count() - 1);

                    for (int idx = 0; idx < value.Length; idx++)
                    {
                        // TODO: Find the paddock object for this name and store it
                        // if the paddock object is found then add it to Paddocks
                        this.stockModel.Paddocks.Add(idx, value[idx].Name);

                        paddockInfo = this.stockModel.Paddocks.ByIndex(idx);
                        paddockInfo.ExcretionDest = value[idx].Excretion;
                        paddockInfo.UrineDest = value[idx].Urine;
                        paddockInfo.Area = value[idx].Area;
                        paddockInfo.Slope = value[idx].Slope;
                        for (int jdx = 0; jdx < value[idx].Forages.Length; jdx++)
                        {
                            this.userForages.Add(value[idx].Forages[jdx]);    // keep a local list of these for queryInfos later
                            this.userPaddocks.Add(value[idx].Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the livestock enterprises and their management options
        /// </summary>
        [Description("Livestock enterprises and their management options")]
        public EnterpriseInfo[] EnterpriseList
        {
            get
            {
                EnterpriseInfo[] ents = new EnterpriseInfo[this.stockModel.Enterprises.Count];
                for (int i = 0; i < this.stockModel.Enterprises.Count; i++)
                    ents[i] = this.stockModel.Enterprises.byIndex(i);
                return ents;
            }

            set
            {
                if (value != null && this.stockModel.Enterprises != null)
                {
                    while (this.stockModel.Enterprises.Count > 0)
                        this.stockModel.Enterprises.Delete(this.stockModel.Enterprises.Count - 1);
                    for (int i = 0; i < value.Length; i++)
                        this.stockModel.Enterprises.Add(value[i]);
                }
            }
        }

        /// <summary>
        /// Gets or sets the livestock grazing rotations
        /// </summary>
        [Description("Livestock grazing rotations")]
        public GrazingPeriod[] GrazingPeriods
        {
            get
            {
                GrazingPeriod[] periods = new GrazingPeriod[this.stockModel.GrazingPeriods.Count()];
                for (int i = 0; i < this.stockModel.GrazingPeriods.Count(); i++)
                    periods[i] = this.stockModel.GrazingPeriods.ByIndex(i);
                return periods;
            }

            set
            {
                if (value != null && this.stockModel.GrazingPeriods != null)
                {
                    while (this.stockModel.GrazingPeriods.Count() > 0)
                    {
                        this.stockModel.GrazingPeriods.Delete(this.stockModel.GrazingPeriods.Count() - 1);
                    }
                    for (int i = 0; i < value.Length; i++)
                        this.stockModel.GrazingPeriods.Add(value[i]);
                }
            }
        }

        #endregion

        #region Readable properties ====================================================
        /// <summary>
        /// Gets the mass of grazers per unit area
        /// </summary>
        [Description("Mass of grazers per unit area. The value returned depends on the requesting component")]
        [Units("kg/ha")]
        public double Trampling
        {
            get
            {   // TODO: complete the function

                ForageProvider forageProvider;

                // using the component ID
                // return the mass per area for all forages
                forageProvider = this.stockModel.ForagesAll.FindProvider(0);
                return this.stockModel.ReturnMassPerArea(0, forageProvider, "kg/ha"); // by paddock or from forage ref
            }
        }

        /// <summary>
        /// Gets the consumption of supplementary feed by animals
        /// </summary>
        [Description("Consumption of supplementary feed by animals")]
        public SupplementEaten[] SuppEaten
        {
            get
            {
                SupplementEaten[] value = null;
                StockVars.MakeSuppEaten(this.stockModel, ref value);
                return value;
            }
        }

        /// <summary>
        /// Gets the number of animal groups
        /// </summary>
        [Description("Number of animal groups")]
        public int NoGroups
        {
            get
            {
                return this.stockModel.Count();
            }
        }

        // =============== All ============

        /// <summary>
        /// Gets the number of animals in each group
        /// </summary>
        [Description("Number of animals in each group")]
        public int[] Number
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eBoth, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of animals
        /// </summary>
        [Description("Total number of animals")]
        public int NumberAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eBoth, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of animals in each tag group
        /// </summary>
        [Description("Number of animals in each tag group")]
        public int[] NumberTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eBoth, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ============== Young ============

        /// <summary>
        /// Gets the number of unweaned young animals in each group
        /// </summary>
        [Description("Number of unweaned young animals in each group")]
        public int[] NumberYng
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eBoth, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of unweaned young animals
        /// </summary>
        [Description("Number of unweaned young animals")]
        public int NumberYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eBoth, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned young animals in each group
        /// </summary>
        [Description("Number of unweaned young animals in each tag group")]
        public int[] NumberYngTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eBoth, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Female ============

        /// <summary>
        /// Gets the number of female animals in each group
        /// </summary>
        [Description("Number of female animals in each group")]
        public int[] NoFemale
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eFemale, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of female animals
        /// </summary>
        [Description("Total number of female animals")]
        public int NoFemaleAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eFemale, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of female animals in each tag group
        /// </summary>
        [Description("Number of female animals in each tag group")]
        public int[] NoFemaleTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eFemale, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Female Young ============

        /// <summary>
        /// Gets the number of unweaned female animals in each group
        /// </summary>
        [Description("Number of unweaned female animals in each group")]
        public int[] NoFemaleYng
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eFemale, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of unweaned female animals
        /// </summary>
        [Description("Total number of unweaned female animals")]
        public int NoFemaleYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eFemale, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned female animals in each tag group
        /// </summary>
        [Description("Number of unweaned female animals in each tag group")]
        public int[] NoFemaleYngTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eFemale, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Male ============

        /// <summary>
        /// Gets the number of male animals in each group
        /// </summary>
        [Description("Number of male animals in each group")]
        public int[] NoMale
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eMale, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of male animals
        /// </summary>
        [Description("Total number of male animals")]
        public int NoMaleAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eMale, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of male animals in each tag group
        /// </summary>
        [Description("Number of male animals in each tag group")]
        public int[] NoMaleTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eMale, false, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Male Young ============

        /// <summary>
        /// Gets the number of unweaned male animals in each group
        /// </summary>
        [Description("Number of unweaned male animals in each group")]
        public int[] NoMaleYng
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eMale, true, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the total number of unweaned male animals
        /// </summary>
        [Description("Total number of unweaned male animals")]
        public int NoMaleYngAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eMale, true, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned male animals in each tag group
        /// </summary>
        [Description("Number of unweaned male animals in each tag group")]
        public int[] NoMaleYngTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eMale, true, false, true, ref numbers);
                return numbers;
            }
        }

        // ================ Deaths ================

        /// <summary>
        /// Gets the deaths of all non suckling animals
        /// </summary>
        [Description("Number of all deaths")]
        public int DeathsAll
        {
            get
            {
                int[] numbers = new int[1];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eDeaths, false, true, false, ref numbers);
                return numbers[0];
            }
        }

        /// <summary>
        /// Gets the deaths of non suckling animals in each group
        /// </summary>
        [Description("Number of deaths in each group")]
        public int[] Deaths
        {
            get
            {
                int[] numbers = new int[this.stockModel.Count()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eDeaths, false, false, false, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the deaths of non suckling animals in each tag group
        /// </summary>
        [Description("Number of deaths in each tag group")]
        public int[] DeathsTag
        {
            get
            {
                int[] numbers = new int[this.stockModel.HighestTag()];
                StockVars.PopulateNumberValue(this.stockModel, StockVars.CountType.eDeaths, false, false, true, ref numbers);
                return numbers;
            }
        }

        /// <summary>
        /// Gets the sex field of the sheep and cattle initialisation variables
        /// </summary>
        [Description("See the sex field of the sheep and cattle initialisation variables. Returns 'heifer' for cows under two years of age")]
        public string[] Sex
        {
            get
            {
                string[] values = new string[this.stockModel.Count()];
                for (int idx = 0; idx < this.stockModel.Count(); idx++)
                    values[idx] = this.stockModel.SexString((int)idx, false);
                return values;
            }
        }

        // =========== Ages ==================

        /// <summary>
        /// Gets the age of animals by group.
        /// </summary>
        [Description("Age of animals by group")]
        [Units("d")]
        public double[] Age
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of animals total
        /// </summary>
        [Description("Age of animals total")]
        [Units("d")]
        public double AgeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of animals by tag number
        /// </summary>
        [Description("Age of animals by tag number")]
        [Units("d")]
        public double[] AgeTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Ages of young ==================

        /// <summary>
        /// Gets the age of unweaned young animals by group
        /// </summary>
        [Description("Age of unweaned young animals by group")]
        [Units("d")]
        public double[] AgeYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals total
        /// </summary>
        [Description("Age of unweaned young animals total")]
        [Units("d")]
        public double AgeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals by tag number
        /// </summary>
        [Description("Age of unweaned young animals by tag number")]
        [Units("d")]
        public double[] AgeYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Ages months ==================

        /// <summary>
        /// Gets the age of animals, in months by group
        /// </summary>
        [Description("Age of animals, in months by group")]
        public double[] AgeMonths
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE_MONTHS, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of animals, in months total
        /// </summary>
        [Description("Age of animals, in months total")]
        public double AgeMonthsAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE_MONTHS, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of animals, in months by tag number
        /// </summary>
        [Description("Age of animals, in months by tag number")]
        public double[] AgeMonthsTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE_MONTHS, false, false, true, ref values);
                return values;
            }
        }

        // =========== Ages of young in months ==================

        /// <summary>
        /// Gets the age of unweaned young animals, in months by group
        /// </summary>
        [Description("Age of unweaned young animals, in months by group")]
        public double[] AgeMonthsYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE_MONTHS, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals, in months total
        /// </summary>
        [Description("Age of unweaned young animals, in months total")]
        public double AgeMonthsYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE_MONTHS, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the age of unweaned young animals, in months by tag number
        /// </summary>
        [Description("Age of unweaned young animals, in months by tag number")]
        public double[] AgeMonthsYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpAGE_MONTHS, true, false, true, ref values);
                return values;
            }
        }

        // =========== Weight ==================

        /// <summary>
        /// Gets the average live weight by group
        /// </summary>
        [Description("Average live weight by group")]
        [Units("kg")]
        public double[] Weight
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLIVE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the averge live weight total
        /// </summary>
        [Description("Averge live weight total")]
        [Units("kg")]
        public double WeightAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLIVE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the average live weight by tag number
        /// </summary>
        [Description("Average live weight by tag number")]
        [Units("kg")]
        public double[] WeightTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLIVE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Weight of young ==================

        /// <summary>
        /// Gets the average live weight of unweaned young animals by group
        /// </summary>
        [Description("Average live weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] WeightYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLIVE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the average live weight of unweaned young animals total
        /// </summary>
        [Description("Average live weight of unweaned young animals total")]
        [Units("kg")]
        public double WeightYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLIVE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the average live weight of unweaned young animals by tag number
        /// </summary>
        [Description("Average live weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] WeightYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLIVE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Fleece-free, conceptus-free weight ==================

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight by group
        /// </summary>
        [Description("Fleece-free, conceptus-free weight by group")]
        [Units("kg")]
        public double[] BaseWt
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBASE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight total
        /// </summary>
        [Description("Fleece-free, conceptus-free weight total")]
        [Units("kg")]
        public double BaseWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBASE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight by tag number
        /// </summary>
        [Description("Fleece-free, conceptus-free weight by tag number")]
        [Units("kg")]
        public double[] BaseWtTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBASE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Fleece-free, conceptus-free weight young ==================

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight of unweaned young animals by group
        /// </summary>
        [Description("Fleece-free, conceptus-free weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] BaseWtYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBASE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight of unweaned young animals total
        /// </summary>
        [Description("Fleece-free, conceptus-free weight of unweaned young animals total")]
        [Units("kg")]
        public double BaseWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBASE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fleece-free, conceptus-free weight of unweaned young animals by tag number
        /// </summary>
        [Description("Fleece-free, conceptus-free weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] BaseWtYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBASE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score of animals ==================

        /// <summary>
        /// Gets the condition score of animals (1-5 scale) by group
        /// </summary>
        [Description("Condition score of animals (1-5 scale) by group")]
        public double[] CondScore
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCOND_SCORE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the condition score of animals (1-5 scale) total
        /// </summary>
        [Description("Condition score of animals (1-5 scale) total")]
        public double CondScoreAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCOND_SCORE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the condition score of animals (1-5 scale) by tag number
        /// </summary>
        [Description("Condition score of animals (1-5 scale) by tag number")]
        public double[] CondScoreTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCOND_SCORE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score of animals (1-5 scale) of young ==================

        /// <summary>
        /// Gets the condition score of unweaned young animals (1-5 scale) by group
        /// </summary>
        [Description("Condition score of unweaned young animals (1-5 scale) by group")]
        public double[] CondScoreYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCOND_SCORE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the condition score of unweaned young animals (1-5 scale) total
        /// </summary>
        [Description("Condition score of unweaned young animals (1-5 scale) total")]
        public double CondScoreYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCOND_SCORE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the condition score of unweaned young animals (1-5 scale) by tag number
        /// </summary>
        [Description("Condition score of unweaned young animals (1-5 scale) by tag number")]
        public double[] CondScoreYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCOND_SCORE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Maximum previous basal weight ==================

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained by each animal group
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained by each animal group")]
        [Units("kg")]
        public double[] MaxPrevWt
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMAX_PREV_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained total
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained total")]
        [Units("kg")]
        public double MaxPrevWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMAX_PREV_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained by tag number
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained by tag number")]
        [Units("kg")]
        public double[] MaxPrevWtTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMAX_PREV_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Maximum previous basal weight young ==================

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by group
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by group")]
        [Units("kg")]
        public double[] MaxPrevWtYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMAX_PREV_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained unweaned young animals total
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals total")]
        [Units("kg")]
        public double MaxPrevWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMAX_PREV_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by tag number
        /// </summary>
        [Description("Maximum previous basal weight (fleece-free, conceptus-free) attained of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] MaxPrevWtYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMAX_PREV_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current greasy fleece weight ==================

        /// <summary>
        /// Gets the current greasy fleece weight by group
        /// </summary>
        [Description("Current greasy fleece weight by group")]
        [Units("kg")]
        public double[] FleeceWt
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFLEECE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight total
        /// </summary>
        [Description("Current greasy fleece weight total")]
        [Units("kg")]
        public double FleeceWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFLEECE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight by tag number
        /// </summary>
        [Description("Current greasy fleece weight by tag number")]
        [Units("kg")]
        public double[] FleeceWtTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFLEECE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current greasy fleece weight young ==================

        /// <summary>
        /// Gets the current greasy fleece weight of unweaned young animals by group
        /// </summary>
        [Description("Current greasy fleece weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] FleeceWtYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFLEECE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight of unweaned young animals total
        /// </summary>
        [Description("Current greasy fleece weight of unweaned young animals total")]
        [Units("kg")]
        public double FleeceWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFLEECE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current greasy fleece weight of unweaned young animals by tag number
        /// </summary>
        [Description("Current greasy fleece weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] FleeceWtYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFLEECE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current clean fleece weight ==================

        /// <summary>
        /// Gets the current clean fleece weight by group
        /// </summary>
        [Description("Current clean fleece weight by group")]
        [Units("kg")]
        public double[] CFleeceWt
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight total
        /// </summary>
        [Description("Current clean fleece weight total")]
        [Units("kg")]
        public double CFleeceWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight by tag number
        /// </summary>
        [Description("Current clean fleece weight by tag number")]
        [Units("kg")]
        public double[] CFleeceWtTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current clean fleece weight young ==================

        /// <summary>
        /// Gets the current clean fleece weight of unweaned young animals by group
        /// </summary>
        [Description("Current clean fleece weight of unweaned young animals by group")]
        [Units("kg")]
        public double[] CFleeceWtYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_WT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight of unweaned young animals total
        /// </summary>
        [Description("Current clean fleece weight of unweaned young animals total")]
        [Units("kg")]
        public double CFleeceWtYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_WT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current clean fleece weight of unweaned young animals by tag number
        /// </summary>
        [Description("Current clean fleece weight of unweaned young animals by tag number")]
        [Units("kg")]
        public double[] CFleeceWtYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_WT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Current average wool fibre diameter ==================

        /// <summary>
        /// Gets the current average wool fibre diameter by group
        /// </summary>
        [Description("Current average wool fibre diameter by group")]
        [Units("um")]
        public double[] FibreDiam
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFIBRE_DIAM, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter total
        /// </summary>
        [Description("Current average wool fibre diameter total")]
        [Units("um")]
        public double FibreDiamAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFIBRE_DIAM, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter by tag number
        /// </summary>
        [Description("Current average wool fibre diameter by tag number")]
        [Units("um")]
        public double[] FibreDiamTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFIBRE_DIAM, false, false, true, ref values);
                return values;
            }
        }

        // =========== Current average wool fibre diameter young ==================

        /// <summary>
        /// Gets the current average wool fibre diameter of unweaned young animals by group
        /// </summary>
        [Description("Current average wool fibre diameter of unweaned young animals by group")]
        [Units("um")]
        public double[] FibreDiamYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFIBRE_DIAM, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter of unweaned young animals total
        /// </summary>
        [Description("Current average wool fibre diameter of unweaned young animals total")]
        [Units("um")]
        public double FibreDiamYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFIBRE_DIAM, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the current average wool fibre diameter of unweaned young animals by tag number
        /// </summary>
        [Description("Current average wool fibre diameter of unweaned young animals by tag number")]
        [Units("um")]
        public double[] FibreDiamYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpFIBRE_DIAM, true, false, true, ref values);
                return values;
            }
        }

        // =========== If the animals are pregnant, the number of days since conception; zero otherwise ==================

        /// <summary>
        /// Gets the the pregnecy status. If the animals are pregnant, the number of days since conception; zero otherwise, by group
        /// </summary>
        [Description("If the animals are pregnant, the number of days since conception; zero otherwise, by group")]
        [Units("d")]
        public double[] Pregnant
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpPREGNANT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the the pregnecy status. If the animals are pregnant, the number of days since conception; zero otherwise, total
        /// </summary>
        [Description("If the animals are pregnant, the number of days since conception; zero otherwise, total")]
        [Units("d")]
        public double PregnantAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpPREGNANT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the the pregnecy status. If the animals are pregnant, the number of days since conception; zero otherwise, by tag number
        /// </summary>
        [Description("If the animals are pregnant, the number of days since conception; zero otherwise, by tag number")]
        [Units("d")]
        public double[] PregnantTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpPREGNANT, false, false, true, ref values);
                return values;
            }
        }

        // =========== If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise ==================

        /// <summary>
        /// Gets the lactation status. If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by group
        /// </summary>
        [Description("If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by group")]
        [Units("d")]
        public double[] Lactating
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLACTATING, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the lactation status. If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, total
        /// </summary>
        [Description("If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, total")]
        [Units("d")]
        public double LactatingAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLACTATING, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the lactation status. If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by tag number
        /// </summary>
        [Description("If the animals are lactating, the number of days since birth of the lamb or calf; zero otherwise, by tag number")]
        [Units("d")]
        public double[] LactatingTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpLACTATING, false, false, true, ref values);
                return values;
            }
        }

        // =========== Number of foetuses per head ==================

        /// <summary>
        /// Gets the number of foetuses per head by group
        /// </summary>
        [Description("Number of foetuses per head by group")]
        public double[] NoFoetuses
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpNO_FOETUSES, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the number of foetuses per head total
        /// </summary>
        [Description("Number of foetuses per head total")]
        public double NoFoetusesAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpNO_FOETUSES, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the number of foetuses per head by tag number
        /// </summary>
        [Description("Number of foetuses per head by tag number")]
        public double[] NoFoetusesTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpNO_FOETUSES, false, false, true, ref values);
                return values;
            }
        }

        // AddScalarSet(ref   Idx, StockProps.prpNO_SUCKLING, "no_suckling", TTypedValue.TBaseType.ITYPE_DOUBLE, "", false, "Number of unweaned lambs or calves per head", "");
        // =========== Number of unweaned lambs or calves per head ==================

        /// <summary>
        /// Gets the number of unweaned lambs or calves per head by group
        /// </summary>
        [Description("Number of unweaned lambs or calves per head by group")]
        public double[] NoSuckling
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpNO_SUCKLING, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the number of unweaned lambs or calves per head total
        /// </summary>
        [Description("Number of unweaned lambs or calves per head total")]
        public double NoSucklingAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpNO_SUCKLING, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the number of unweaned lambs or calves per head by tag number
        /// </summary>
        [Description("Number of unweaned lambs or calves per head by tag number")]
        public double[] NoSucklingTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpNO_SUCKLING, false, false, true, ref values);
                return values;
            }
        }

        // =========== Condition score at last parturition; zero if lactating=0 ==================

        /// <summary>
        /// Gets the condition score at last parturition; zero if lactating=0, by group
        /// </summary>
        [Description("Condition score at last parturition; zero if lactating=0, by group")]
        public double[] BirthCS
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBIRTH_CS, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the condition score at last parturition; zero if lactating=0, total
        /// </summary>
        [Description("Condition score at last parturition; zero if lactating=0, total")]
        public double BirthCSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBIRTH_CS, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the condition score at last parturition; zero if lactating=0, by tag number
        /// </summary>
        [Description("Condition score at last parturition; zero if lactating=0, by tag number")]
        public double[] BirthCSTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpBIRTH_CS, false, false, true, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the paddock occupied by each animal group
        /// </summary>
        [Description("Paddock occupied by each animal group")]
        public string[] Paddock
        {
            get
            {
                string[] paddocks = new string[this.stockModel.Count()];
                for (int idx = 1; idx <= this.stockModel.Count(); idx++)
                    paddocks[idx - 1] = this.stockModel.GetInPadd((int)idx);
                return paddocks;
            }
        }

        /// <summary>
        /// Gets the tag value assigned to each animal group
        /// </summary>
        [Description("Tag value assigned to each animal group")]
        public int[] TagNo
        {
            get
            {
                int[] tags = new int[this.stockModel.Count()];
                for (int idx = 1; idx <= this.stockModel.Count(); idx++)
                    tags[idx - 1] = this.stockModel.GetTag((int)idx);
                return tags;
            }
        }

        /// <summary>
        /// Gets the priority score assigned to each animal group; used in drafting
        /// </summary>
        [Description("Priority score assigned to each animal group; used in drafting")]
        public int[] Priority
        {
            get
            {
                int[] priorities = new int[this.stockModel.Count()];
                for (int idx = 1; idx <= this.stockModel.Count(); idx++)
                    priorities[idx - 1] = this.stockModel.GetPriority((int)idx);
                return priorities;
            }
        }

        // =========== Dry sheep equivalents, based on potential intake ==================

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake by group
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake by group")]
        public double[] DSE
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDSE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake total
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake total")]
        public double DSEAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDSE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake by tag number
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake by tag number")]
        public double[] DSETag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDSE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Dry sheep equivalents, based on potential intake young ==================

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake of unweaned young animals by group
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake of unweaned young animals by group")]
        public double[] DSEYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDSE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake of unweaned young animals total
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake of unweaned young animals total")]
        public double DSEYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDSE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the dry sheep equivalents, based on potential intake of unweaned young animals by tag number
        /// </summary>
        [Description("Dry sheep equivalents, based on potential intake of unweaned young animals by tag number")]
        public double[] DSEYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDSE, true, false, true, ref values);
                return values;
            }
        }

        // AddScalarSet(ref   Idx, StockProps., "wt_change", TTypedValue.TBaseType.ITYPE_DOUBLE, "kg/d", true, "Rate of change of base weight of each animal group", "");
        // =========== Rate of change of base weight of each animal group ==================

        /// <summary>
        /// Gets the rate of change of base weight of each animal by group
        /// </summary>
        [Description("Rate of change of base weight of each animal by group")]
        [Units("kg/d")]
        public double[] WtChange
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpWT_CHANGE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of each animal total
        /// </summary>
        [Description("Rate of change of base weight of each animal total")]
        [Units("kg/d")]
        public double WtChangeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpWT_CHANGE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of each animal by tag number
        /// </summary>
        [Description("Rate of change of base weight of each animal by tag number")]
        [Units("kg/d")]
        public double[] WtChangeTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpWT_CHANGE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Rate of change of base weight of each animal group young ==================

        /// <summary>
        /// Gets the rate of change of base weight of unweaned young animals by group
        /// </summary>
        [Description("Rate of change of base weight of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] WtChangeYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpWT_CHANGE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of unweaned young animals total
        /// </summary>
        [Description("Rate of change of base weight of unweaned young animals total")]
        [Units("kg/d")]
        public double WtChangeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpWT_CHANGE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the rate of change of base weight of unweaned young animals by tag number
        /// </summary>
        [Description("Rate of change of base weight of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] WtChangeYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpWT_CHANGE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Total intake per head of dry matter and nutrients by each animal group ==================

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients by each animal group
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients by each animal group")]
        public DMPoolHead[] Intake
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients")]
        public DMPoolHead IntakeAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients by tag
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients by tag")]
        public DMPoolHead[] IntakeTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Total intake per head of dry matter and nutrients of unweaned animals by group ==================

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients of unweaned animals by group")]
        public DMPoolHead[] IntakeYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients of unweaned animals
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients of unweaned animals")]
        public DMPoolHead IntakeYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the total intake per head of dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Description("Total intake per head of dry matter and nutrients of unweaned animals by tag")]
        public DMPoolHead[] IntakeYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of pasture dry matter and nutrients by each animal group ==================

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients by each animal group
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients by each animal group")]
        public DMPoolHead[] PastIntake
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_PAST, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients")]
        public DMPoolHead PastIntakeAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_PAST, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients by tag
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients by tag")]
        public DMPoolHead[] PastIntakeTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_PAST, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of pasture dry matter and nutrients of unweaned animals by group ==================

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients of unweaned animals by group")]
        public DMPoolHead[] PastIntakeYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_PAST, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients of unweaned animals
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients of unweaned animals")]
        public DMPoolHead PastIntakeYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_PAST, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of pasture dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Description("Intake per head of pasture dry matter and nutrients of unweaned animals by tag")]
        public DMPoolHead[] PastIntakeYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_PAST, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of supplement dry matter and nutrients by each animal group ==================

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients by each animal group
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients by each animal group")]
        public DMPoolHead[] SuppIntake
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_SUPP, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients")]
        public DMPoolHead SuppIntakeAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_SUPP, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients by tag
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients by tag")]
        public DMPoolHead[] SuppIntakeTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_SUPP, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of supplement dry matter and nutrients of unweaned animals by group ==================

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients of unweaned animals by group
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients of unweaned animals by group")]
        public DMPoolHead[] SuppIntakeYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_SUPP, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients of unweaned animals
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients of unweaned animals")]
        public DMPoolHead SuppIntakeYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_SUPP, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of supplement dry matter and nutrients of unweaned animals by tag
        /// </summary>
        [Description("Intake per head of supplement dry matter and nutrients of unweaned animals by tag")]
        public DMPoolHead[] SuppIntakeYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINTAKE_SUPP, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Intake per head of metabolizable energy ==================

        /// <summary>
        /// Gets the intake per head of metabolizable energy by group
        /// </summary>
        [Description("Intake per head of metabolizable energy by group")]
        [Units("MJ/d")]
        public double[] MEIntake
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpME_INTAKE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy total
        /// </summary>
        [Description("Intake per head of metabolizable energy total")]
        [Units("MJ/d")]
        public double MEIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpME_INTAKE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy by tag number
        /// </summary>
        [Description("Intake per head of metabolizable energy by tag number")]
        [Units("MJ/d")]
        public double[] MEIntakeTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpME_INTAKE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of metabolizable energy of young ==================

        /// <summary>
        /// Gets the intake per head of metabolizable energy of unweaned young animals by group
        /// </summary>
        [Description("Intake per head of metabolizable energy of unweaned young animals by group")]
        [Units("MJ/d")]
        public double[] MEIntakeYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpME_INTAKE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy of unweaned young animals total
        /// </summary>
        [Description("Intake per head of metabolizable energy of unweaned young animals total")]
        [Units("MJ/d")]
        public double MEIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpME_INTAKE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of metabolizable energy of unweaned young animals by tag number
        /// </summary>
        [Description("Intake per head of metabolizable energy of unweaned young animals by tag number")]
        [Units("MJ/d")]
        public double[] MEIntakeYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpME_INTAKE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Crude protein intake per head ==================

        /// <summary>
        /// Gets the crude protein intake per head by group
        /// </summary>
        [Description("Crude protein intake per head by group")]
        [Units("kg/d")]
        public double[] CPIntake
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCPI_INTAKE, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head total
        /// </summary>
        [Description("Crude protein intake per head total")]
        [Units("kg/d")]
        public double CPIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCPI_INTAKE, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head by tag number
        /// </summary>
        [Description("Crude protein intake per head by tag number")]
        [Units("kg/d")]
        public double[] CPIntakeTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCPI_INTAKE, false, false, true, ref values);
                return values;
            }
        }

        // =========== Crude protein intake per head of young ==================

        /// <summary>
        /// Gets the crude protein intake per head of unweaned young animals by group
        /// </summary>
        [Description("Crude protein intake per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] CPIntakeYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCPI_INTAKE, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head of unweaned young animals total
        /// </summary>
        [Description("Crude protein intake per head of unweaned young animals total")]
        [Units("kg/d")]
        public double CPIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCPI_INTAKE, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the crude protein intake per head of unweaned young animals by tag number
        /// </summary>
        [Description("Crude protein intake per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] CPIntakeYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCPI_INTAKE, true, false, true, ref values);
                return values;
            }
        }

        // =========== Growth rate of clean fleece ==================

        /// <summary>
        /// Gets the growth rate of clean fleece by group
        /// </summary>
        [Description("Growth rate of clean fleece by group")]
        [Units("kg/d")]
        public double[] CFleeceGrowth
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_GROWTH, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece total
        /// </summary>
        [Description("Growth rate of clean fleece total")]
        [Units("kg/d")]
        public double CFleeceGrowthAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_GROWTH, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece by tag number
        /// </summary>
        [Description("Growth rate of clean fleece by tag number")]
        [Units("kg/d")]
        public double[] CFleeceGrowthTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_GROWTH, false, false, true, ref values);
                return values;
            }
        }

        // =========== Growth rate of clean fleece of young ==================

        /// <summary>
        /// Gets the growth rate of clean fleece of unweaned young animals by group
        /// </summary>
        [Description("Growth rate of clean fleece of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] CFleeceGrowthYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_GROWTH, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece of unweaned young animals total
        /// </summary>
        [Description("Growth rate of clean fleece of unweaned young animals total")]
        [Units("kg/d")]
        public double CFleeceGrowthYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_GROWTH, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the growth rate of clean fleece of unweaned young animals by tag number
        /// </summary>
        [Description("Growth rate of clean fleece of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] CFleeceGrowthYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCFLEECE_GROWTH, true, false, true, ref values);
                return values;
            }
        }

        // =========== Fibre diameter of the current day's wool growth ==================

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth by group
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth by group")]
        [Units("um")]
        public double[] FibreGrowthDiam
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDAY_FIBRE_DIAM, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth total
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth total")]
        [Units("um")]
        public double FibreGrowthDiamAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDAY_FIBRE_DIAM, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth by tag number
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth by tag number")]
        [Units("um")]
        public double[] FibreGrowthDiamTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDAY_FIBRE_DIAM, false, false, true, ref values);
                return values;
            }
        }

        // =========== Fibre diameter of the current day's wool growth of young ==================

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth of unweaned young animals by group
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth of unweaned young animals by group")]
        [Units("um")]
        public double[] FibreGrowthDiamYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDAY_FIBRE_DIAM, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth of unweaned young animals total
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth of unweaned young animals total")]
        [Units("um")]
        public double FibreGrowthDiamYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDAY_FIBRE_DIAM, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the fibre diameter of the current day's wool growth of unweaned young animals by tag number
        /// </summary>
        [Description("Fibre diameter of the current day's wool growth of unweaned young animals by tag number")]
        [Units("um")]
        public double[] FibreGrowthDiamYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpDAY_FIBRE_DIAM, true, false, true, ref values);
                return values;
            }
        }

        // =========== Weight of milk produced per head, on a 4pc fat-corrected basis ==================

        /// <summary>
        /// Gets the weight of milk produced per head, on a 4pc fat-corrected basis by group
        /// </summary>
        [Description("Weight of milk produced per head, on a 4pc fat-corrected basis by group")]
        [Units("kg/d")]
        public double[] MilkWt
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMILK_WT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the weight of milk produced per head, on a 4pc fat-corrected basis total
        /// </summary>
        [Description("Weight of milk produced per head, on a 4pc fat-corrected basis total")]
        [Units("kg/d")]
        public double MilkWtAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMILK_WT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the weight of milk produced per head, on a 4pc fat-corrected basis by tag number
        /// </summary>
        [Description("Weight of milk produced per head, on a 4pc fat-corrected basis by tag number")]
        [Units("kg/d")]
        public double[] MilkWtTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMILK_WT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Metabolizable energy produced in milk (per head) by each animal group ==================

        /// <summary>
        /// Gets the metabolizable energy produced in milk (per head) by each animal group by group
        /// </summary>
        [Description("Metabolizable energy produced in milk (per head) by each animal group by group")]
        [Units("MJ/d")]
        public double[] MilkME
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMILK_ME, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the metabolizable energy produced in milk (per head) by each animal group total
        /// </summary>
        [Description("Metabolizable energy produced in milk (per head) by each animal group total")]
        [Units("MJ/d")]
        public double MilkMEAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMILK_ME, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the metabolizable energy produced in milk (per head) by each animal group by tag number
        /// </summary>
        [Description("Metabolizable energy produced in milk (per head) by each animal group by tag number")]
        [Units("MJ/d")]
        public double[] MilkMETag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpMILK_ME, false, false, true, ref values);
                return values;
            }
        }

        // =========== Nitrogen retained within the animals, on a per-head basis ==================

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis by group
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis by group")]
        [Units("kg/d")]
        public double[] RetainedN
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_N, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis total
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis total")]
        [Units("kg/d")]
        public double RetainedNAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_N, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis by tag number")]
        [Units("kg/d")]
        public double[] RetainedNTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_N, false, false, true, ref values);
                return values;
            }
        }

        // =========== Nitrogen retained within the animals, on a per-head basis of young ==================

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RetainedNYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_N, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis of unweaned young animals total")]
        [Units("kg/d")]
        public double RetainedNYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_N, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the nitrogen retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Description("Nitrogen retained within the animals, on a per-head basis of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RetainedNYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_N, true, false, true, ref values);
                return values;
            }
        }

        // AddScalarSet(ref   Idx, StockProps., "retained_p", TTypedValue.TBaseType.ITYPE_DOUBLE, "", true, "", "");
        // =========== Phosphorus retained within the animals, on a per-head basis ==================

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis by group
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis by group")]
        [Units("kg/d")]
        public double[] RetainedP
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_P, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis total
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis total")]
        [Units("kg/d")]
        public double RetainedPAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_P, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis by tag number")]
        [Units("kg/d")]
        public double[] RetainedPTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_P, false, false, true, ref values);
                return values;
            }
        }

        // =========== Phosphorus retained within the animals, on a per-head basis of young ==================

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RetainedPYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_P, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis of unweaned young animals total")]
        [Units("kg/d")]
        public double RetainedPYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_P, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the phosphorus retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Description("Phosphorus retained within the animals, on a per-head basis of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RetainedPYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_P, true, false, true, ref values);
                return values;
            }
        }

        // =========== Sulphur retained within the animals, on a per-head basis ==================

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis by group
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis by group")]
        public double[] RetainedS
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_S, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis total
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis total")]
        [Units("kg/d")]
        public double RetainedSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_S, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis by tag number
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis by tag number")]
        [Units("kg/d")]
        public double[] RetainedSTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_S, false, false, true, ref values);
                return values;
            }
        }

        // =========== Sulphur retained within the animals, on a per-head basis of young ==================

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis of unweaned young animals by group
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RetainedSYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_S, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis of unweaned young animals total
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis of unweaned young animals total")]
        [Units("kg/d")]
        public double RetainedSYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_S, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the sulphur retained within the animals, on a per-head basis of unweaned young animals by tag number
        /// </summary>
        [Description("Sulphur retained within the animals, on a per-head basis of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RetainedSYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRETAINED_S, true, false, true, ref values);
                return values;
            }
        }

        // =========== Faecal dry matter and nutrients per head ==================

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head by each animal group
        /// </summary>
        [Description("Faecal dry matter and nutrients per head by each animal group")]
        public DMPoolHead[] Faeces
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpFAECES, false, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head
        /// </summary>
        [Description("Faecal dry matter and nutrients per head")]
        public DMPoolHead FaecesAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpFAECES, false, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head by tag
        /// </summary>
        [Description("Faecal dry matter and nutrients per head by tag")]
        public DMPoolHead[] FaecesTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpFAECES, false, false, true, ref pools);
                return pools;
            }
        }

        // =========== Faecal dry matter and nutrients per head of unweaned animals ==================

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head of unweaned animals by group
        /// </summary>
        [Description("Faecal dry matter and nutrients per head of unweaned animals by group")]
        public DMPoolHead[] FaecesYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpFAECES, true, false, false, ref pools);
                return pools;
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head of unweaned animals
        /// </summary>
        [Description("Faecal dry matter and nutrients per head of unweaned animals")]
        public DMPoolHead FaecesYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpFAECES, true, true, false, ref pools);
                return pools[0];
            }
        }

        /// <summary>
        /// Gets the faecal dry matter and nutrients per head of unweaned animals by tag
        /// </summary>
        [Description("Faecal dry matter and nutrients per head of unweaned animals by tag")]
        public DMPoolHead[] FaecesYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpFAECES, true, false, true, ref pools);
                return pools;
            }
        }

        // =========== Inorganic nutrients excreted in faeces, per head ==================

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head by each animal group
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head by each animal group")]
        public InorgFaeces[] FaecesInorg
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINORG_FAECES, false, false, false, ref pools);
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
        [Description("Inorganic nutrients excreted in faeces, per head")]
        public InorgFaeces FaecesInorgAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINORG_FAECES, false, true, false, ref pools);
                inorgpools[0].N = pools[0].N;
                inorgpools[0].P = pools[0].P;
                inorgpools[0].S = pools[0].S;
                return inorgpools[0];
            }
        }

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head by tag
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head by tag")]
        public InorgFaeces[] FaecesInorgTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINORG_FAECES, false, false, true, ref pools);
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
        [Description("Inorganic nutrients excreted in faeces, per head of unweaned animals by group")]
        public InorgFaeces[] FaecesInorgYng
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.Count()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINORG_FAECES, true, false, false, ref pools);
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
        [Description("Inorganic nutrients excreted in faeces, per head of unweaned animals")]
        public InorgFaeces FaecesInorgYngAll
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[1];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINORG_FAECES, true, true, false, ref pools);
                inorgpools[0].N = pools[0].N;
                inorgpools[0].P = pools[0].P;
                inorgpools[0].S = pools[0].S;
                return inorgpools[0];
            }
        }

        /// <summary>
        /// Gets the inorganic nutrients excreted in faeces, per head of unweaned animals by tag
        /// </summary>
        [Description("Inorganic nutrients excreted in faeces, per head of unweaned animals by tag")]
        public InorgFaeces[] FaecesInorgYngTag
        {
            get
            {
                DMPoolHead[] pools = new DMPoolHead[this.stockModel.HighestTag()];
                InorgFaeces[] inorgpools = new InorgFaeces[pools.Length];
                StockVars.PopulateDMPoolValue(this.stockModel, StockProps.prpINORG_FAECES, true, false, true, ref pools);
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
        [Description("Metabolizable energy use for each animal group")]
        public EnergyUse[] EnergyUse
        {
            get
            {
                EnergyUse[] use = new EnergyUse[this.stockModel.Count()];
                StockVars.MakeEnergyUse(this.stockModel, ref use);
                return use;
            }
        }

        // =========== Output of methane (per head) ==================

        /// <summary>
        /// Gets the output of methane (per head) by group
        /// </summary>
        [Description("Output of methane (per head) by group")]
        [Units("kg/d")]
        public double[] Methane
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCH4_OUTPUT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) total
        /// </summary>
        [Description("Output of methane (per head) total")]
        [Units("kg/d")]
        public double MethaneAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCH4_OUTPUT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) by tag number
        /// </summary>
        [Description("Output of methane (per head) by tag number")]
        [Units("kg/d")]
        public double[] MethaneTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCH4_OUTPUT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Output of methane (per head) of young ==================

        /// <summary>
        /// Gets the output of methane (per head) of unweaned young animals by group
        /// </summary>
        [Description("Output of methane (per head) of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] MethaneYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCH4_OUTPUT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) of unweaned young animals total
        /// </summary>
        [Description("Output of methane (per head) of unweaned young animals total")]
        [Units("kg/d")]
        public double MethaneYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCH4_OUTPUT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the output of methane (per head) of unweaned young animals by tag number
        /// </summary>
        [Description("Output of methane (per head) of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] MethaneYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpCH4_OUTPUT, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary nitrogen output per head ==================

        /// <summary>
        /// Gets the urinary nitrogen output per head by group
        /// </summary>
        [Description("Urinary nitrogen output per head by group")]
        [Units("kg/d")]
        public double[] UrineN
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_N, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head total
        /// </summary>
        [Description("Urinary nitrogen output per head total")]
        [Units("kg/d")]
        public double UrineNAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_N, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head by tag number
        /// </summary>
        [Description("Urinary nitrogen output per head by tag number")]
        [Units("kg/d")]
        public double[] UrineNTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_N, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary nitrogen output per head of young ==================

        /// <summary>
        /// Gets the urinary nitrogen output per head of unweaned young animals by group
        /// </summary>
        [Description("Urinary nitrogen output per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] UrineNYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_N, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head of unweaned young animals total
        /// </summary>
        [Description("Urinary nitrogen output per head of unweaned young animals total")]
        [Units("kg/d")]
        public double UrineNYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_N, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary nitrogen output per head of unweaned young animals by tag number
        /// </summary>
        [Description("Urinary nitrogen output per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] UrineNYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_N, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary phosphorus output per head ==================

        /// <summary>
        /// Gets the urinary phosphorus output per head by group
        /// </summary>
        [Description("Urinary phosphorus output per head by group")]
        [Units("kg/d")]
        public double[] UrineP
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_P, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head total
        /// </summary>
        [Description("Urinary phosphorus output per head total")]
        [Units("kg/d")]
        public double UrinePAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_P, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head by tag number
        /// </summary>
        [Description("Urinary phosphorus output per head by tag number")]
        [Units("kg/d")]
        public double[] UrinePTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_P, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary phosphorus output per head of young ==================

        /// <summary>
        /// Gets the urinary phosphorus output per head of unweaned young animals by group
        /// </summary>
        [Description("Urinary phosphorus output per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] UrinePYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_P, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head of unweaned young animals total
        /// </summary>
        [Description("Urinary phosphorus output per head of unweaned young animals total")]
        [Units("kg/d")]
        public double UrinePYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_P, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary phosphorus output per head of unweaned young animals by tag number
        /// </summary>
        [Description("Urinary phosphorus output per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] UrinePYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_P, true, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary sulphur output per head ==================

        /// <summary>
        /// Gets the urinary sulphur output per head by group
        /// </summary>
        [Description("Urinary sulphur output per head by group")]
        [Units("kg/d")]
        public double[] UrineS
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_S, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head total
        /// </summary>
        [Description("Urinary sulphur output per head total")]
        [Units("kg/d")]
        public double UrineSAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_S, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head by tag number
        /// </summary>
        [Description("Urinary sulphur output per head by tag number")]
        [Units("kg/d")]
        public double[] UrineSTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_S, false, false, true, ref values);
                return values;
            }
        }

        // =========== Urinary sulphur output per head of young ==================

        /// <summary>
        /// Gets the urinary sulphur output per head of unweaned young animals by group
        /// </summary>
        [Description("Urinary sulphur output per head of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] UrineSYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_S, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head of unweaned young animals total
        /// </summary>
        [Description("Urinary sulphur output per head of unweaned young animals total")]
        [Units("kg/d")]
        public double UrineSYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_S, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the urinary sulphur output per head of unweaned young animals by tag number
        /// </summary>
        [Description("Urinary sulphur output per head of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] UrineSYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpURINE_S, true, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of rumen-degradable protein ==================

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein by group
        /// </summary>
        [Description("Intake per head of rumen-degradable protein by group")]
        [Units("kg/d")]
        public double[] RDPIntake
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPI, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein total
        /// </summary>
        [Description("Intake per head of rumen-degradable protein total")]
        [Units("kg/d")]
        public double RDPIntakeAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPI, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein by tag number
        /// </summary>
        [Description("Intake per head of rumen-degradable protein by tag number")]
        [Units("kg/d")]
        public double[] RDPIntakeTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPI, false, false, true, ref values);
                return values;
            }
        }

        // =========== Intake per head of rumen-degradable protein of young ==================

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein of unweaned young animals by group
        /// </summary>
        [Description("Intake per head of rumen-degradable protein of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RDPIntakeYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPI, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein of unweaned young animals total
        /// </summary>
        [Description("Intake per head of rumen-degradable protein of unweaned young animals total")]
        [Units("kg/d")]
        public double RDPIntakeYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPI, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the intake per head of rumen-degradable protein of unweaned young animals by tag number
        /// </summary>
        [Description("Intake per head of rumen-degradable protein of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RDPIntakeYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPI, true, false, true, ref values);
                return values;
            }
        }

        // =========== Requirement per head of rumen-degradable protein ==================

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein by group
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein by group")]
        [Units("kg/d")]
        public double[] RDPReqd
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPR, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein total
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein total")]
        [Units("kg/d")]
        public double RDPReqdAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPR, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein by tag number
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein by tag number")]
        [Units("kg/d")]
        public double[] RDPReqdTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPR, false, false, true, ref values);
                return values;
            }
        }

        // =========== Requirement per head of rumen-degradable protein of young ==================

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein of unweaned young animals by group
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein of unweaned young animals by group")]
        [Units("kg/d")]
        public double[] RDPReqdYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPR, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein of unweaned young animals total
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein of unweaned young animals total")]
        [Units("kg/d")]
        public double RDPReqdYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPR, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the requirement per head of rumen-degradable protein of unweaned young animals by tag number
        /// </summary>
        [Description("Requirement per head of rumen-degradable protein of unweaned young animals by tag number")]
        [Units("kg/d")]
        public double[] RDPReqdYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDPR, true, false, true, ref values);
                return values;
            }
        }

        // =========== Effect of rumen-degradable protein availability on rate of intake  ==================

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by group
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by group")]
        public double[] RDPFactor
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDP_EFFECT, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) total
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) total")]
        public double RDPFactorAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDP_EFFECT, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by tag number
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) by tag number")]
        public double[] RDPFactorTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDP_EFFECT, false, false, true, ref values);
                return values;
            }
        }

        // =========== Effect of rumen-degradable protein availability on rate of intake of young ==================

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by group
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by group")]
        public double[] RDPFactorYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDP_EFFECT, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals total
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals total")]
        public double RDPFactorYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDP_EFFECT, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by tag number
        /// </summary>
        [Description("Effect of rumen-degradable protein availability on rate of intake (1 = no limitation to due lack of RDP) of unweaned young animals by tag number")]
        public double[] RDPFactorYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpRDP_EFFECT, true, false, true, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the list of all paddocks identified by the component. In decreasing order of herbage relative intake (computed for the first group of animals in the list)
        /// </summary>
        [Description("List of all paddocks identified by the component. In decreasing order of herbage relative intake (computed for the first group of animals in the list)")]
        public string[] PaddockRank
        {
            get
            {
                string[] ranks = new string[1];
                StockVars.MakePaddockRank(this.stockModel, ref ranks);
                return ranks;
            }
        }

        // =========== Externally-imposed scaling factor for potential intake ==================

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake. This property is resettable by group
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable by group")]
        public double[] IntakeModifier
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpINTAKE_MOD, false, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake. This property is resettable, total
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, total")]
        public double IntakeModifierAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpINTAKE_MOD, false, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake. This property is resettable by tag number
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable by tag number")]
        public double[] IntakeModifierTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpINTAKE_MOD, false, false, true, ref values);
                return values;
            }
        }

        // =========== Externally-imposed scaling factor for potential intake of young ==================

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by group
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by group")]
        public double[] IntakeModifierYng
        {
            get
            {
                double[] values = new double[this.stockModel.Count()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpINTAKE_MOD, true, false, false, ref values);
                return values;
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals total
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals total")]
        public double IntakeModifierYngAll
        {
            get
            {
                double[] values = new double[1];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpINTAKE_MOD, true, true, false, ref values);
                return values[0];
            }
        }

        /// <summary>
        /// Gets the externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by tag number
        /// </summary>
        [Description("Externally-imposed scaling factor for potential intake. This property is resettable, of unweaned young animals by tag number")]
        public double[] IntakeModifierYngTag
        {
            get
            {
                double[] values = new double[this.stockModel.HighestTag()];
                StockVars.PopulateRealValue(this.stockModel, StockProps.prpINTAKE_MOD, true, false, true, ref values);
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
            var childGenotypes = Apsim.Children(this, typeof(AnimalParamSet)).Cast<AnimalParamSet>().ToList();
            if (childGenotypes != null)
                childGenotypes.ForEach(animalParamSet => Genotypes.Add(animalParamSet));

            if (!this.paddocksGiven)
            {
                // get the paddock areas from the simulation
                foreach (Zone zone in Apsim.FindAll(this, typeof(Zone)))
                {
                    this.stockModel.Paddocks.Add(zone, zone.Name);                          // Add to the Paddocks list
                    this.stockModel.Paddocks.ByObj(zone).Area = zone.Area;

                    PaddockInfo thePadd = this.stockModel.Paddocks.ByObj(zone);

                    // find all the child crop, pasture components that have removable biomass
                    foreach (IPlantDamage crop in Apsim.FindAll(zone, typeof(IPlantDamage)))
                    {
                        this.stockModel.ForagesAll.AddProvider(thePadd, zone.Name, zone.Name + "." + crop.Name, 0, 0, crop);
                    }

                    // locate surfaceOM and soil nutrient model
                    thePadd.AddFaecesObj = (SurfaceOrganicMatter)Apsim.Find(zone, typeof(SurfaceOrganicMatter));
                    thePadd.Soil = (ISoil)Apsim.Find(zone, typeof(ISoil));
                    thePadd.AddUrineObj = (ISolute)Apsim.Find(zone, "Urea");
                }
            }

            // Add all child animal groups to stock.
            for (int idx = 0; idx <= this.animalInits.Length - 1; idx++)                // Only create the initial animal groups 
                this.stockModel.Add(this.animalInits[idx]);                             // after the paddocks have been identified                          

            this.currentTime = this.systemClock.Today;
            int currentDay = this.currentTime.Day + (this.currentTime.Month * 0x100) + (this.currentTime.Year * 0x10000);
            this.stockModel.ManageInternalInit(currentDay, this.localWeather.Latitude);               // init groups
        }

        /// <summary>
        /// New weather data available handler
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Handle the start of day event and get the latitude, time and weather
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if (this.isFirstStep)
            {
                this.localWeather.Latitude = this.locWtr.Latitude;

                this.isFirstStep = false;
            }

            this.GetTimeAndWeather();
        }

        /// <summary>
        /// Handle the end of day event 
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Initialisation step
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("DoStock")]
        private void OnDoStock(object sender, EventArgs e)
        {
            // Weather is retrieved at StartOfDay

            //// for each paddock
            ////FModel.Paddocks.byID(1).fWaterlog = 0.0;    // TODO

            if (!this.paddocksGiven)
            {
                // update the paddock area as this can change during the simulation
                foreach (Zone zone in Apsim.FindAll(this, typeof(Zone)))
                {
                    this.stockModel.Paddocks.ByObj(zone).Area = zone.Area;
                    this.stockModel.Paddocks.ByObj(zone).Slope = zone.Slope;
                }
            }
            this.RequestAvailableToAnimal();  // accesses each forage provider (crop)

            this.stockModel.Paddocks.BeginTimeStep();

            if (this.suppFeed != null)
            {
                SuppToStockType[] availSupp = this.suppFeed.SuppToStock;

                for (int idx = 0; idx < availSupp.Length; idx++)
                {
                    // each paddock
                    this.suppFed.SetSuppAttrs(availSupp[idx]);
                    this.stockModel.PlaceSuppInPadd(availSupp[idx].Paddock, availSupp[idx].Amount, this.suppFed, availSupp[idx].FeedSuppFirst);
                }
            }

            this.localWeather.MeanTemp = 0.5 * (this.localWeather.MaxTemp + this.localWeather.MinTemp);
            this.stockModel.Weather = this.localWeather;

            // Do internal management tasks that are defined for the various
            // enterprises. This includes shearing, buying, selling...
            this.stockModel.ManageInternalTasks(this.localWeather.TheDay);

            this.stockModel.Dynamics();

            ForageProvider forageProvider;

            // Return the amounts of forage removed
            for (int i = 0; i <= this.stockModel.ForagesAll.Count() - 1; i++)
            {
                forageProvider = this.stockModel.ForagesAll.ForageProvider(i);
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
            for (int idx = 0; idx <= this.stockModel.Paddocks.Count() - 1; idx++)
            {
                PaddockInfo paddInfo = this.stockModel.Paddocks.ByIndex(idx);

                if (paddInfo.AddFaecesObj != null)
                {
                    Surface.AddFaecesType faeces = new Surface.AddFaecesType();
                    if (this.PopulateFaeces(paddInfo.PaddID, faeces))
                    {
                        ((SurfaceOrganicMatter)paddInfo.AddFaecesObj).AddFaeces(faeces);
                    }
                }
                if (paddInfo.AddUrineObj != null)
                {
                    AddUrineType urine = new AddUrineType();
                    if (this.PopulateUrine(paddInfo.PaddID, urine))
                    {
                        // We could just add the urea to the top layer, but it's better
                        // to work out the penetration depth, and spread it through those layers.
                        double liquidDepth = urine.VolumePerUrination / urine.AreaPerUrination * 1000.0; // Depth of liquid to be added per urinat, in mm
                        double maxDepth = liquidDepth / 0.05; // basically treats soil as having 5% pore space. This is the depth to which urine will penetrate
                        double[] dlayers = paddInfo.Soil.Thickness;
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
            this.GetTimeAndWeather();
            this.outputSummary.WriteMessage(this, "Adding " + animals.Number.ToString() + ", " + animals.Genotype + " " + animals.Sex);
            this.stockModel.DoStockManagement(this.stockModel, animals, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Buys animals (i.e. they enter the simulation). The purchased animals will form a new animal group that is placed at the end of the list of animal groups.
        /// </summary>
        /// <param name="stock">The stock data</param>
        public void Buy(StockBuy stock)
        {
            this.outputSummary.WriteMessage(this, "Buying " + stock.Number.ToString() + ", " + stock.Age.ToString() + " month old " + stock.Genotype + " " + stock.Sex.ToString() + " ");
            this.stockModel.DoStockManagement(this.stockModel, stock, this.localWeather.TheDay, this.localWeather.Latitude);
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
            this.outputSummary.WriteMessage(this, "Buying " + stock.Number.ToString() + ", " + stock.Age.ToString() + " month old " + stock.Genotype + " " + stock.Sex.ToString() + " ");
            this.stockModel.DoStockManagement(this.stockModel, stock, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Assigns animals to paddocks. The process is as follows:
        /// (a) Animal groups with a positive priority score are removed from their current paddock; groups with a zero or negative priority score remain in their current paddock.
        /// (b) The set of unoccupied non-excluded paddocks is identified and then ranked according the quality of the pasture(the best paddock is that which would give highest DM intake).
        /// (c) The unallocated animal groups are ranked by their priority(lowest values first).
        /// (d) Unallocated animal groups are then assigned to paddocks in rank order(e.g.those with the lowest positive score are placed in the best unoccupied paddock). 
        ///     Animal groups with the same priority score are placed in the same paddock
        /// </summary>
        /// <param name="zonesClosed">Names of paddocks to be excluded from consideration as possible destinations</param>
        public void Draft(string[] zonesClosed)
        {
            StockDraft closedZones = new StockDraft();
            closedZones.Closed = zonesClosed;
            this.RequestAvailableToAnimal();
            this.outputSummary.WriteMessage(this, "Drafting animals. Excluding paddocks: " + string.Join(", ", closedZones.Closed));
            this.stockModel.DoStockManagement(this.stockModel, closedZones, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Removes animals from the simulation.  sell without parameters will remove all sheep in the stock sub-model.
        /// </summary>
        /// <param name="number">Number of animals to sell.</param>
        /// <param name="group">Index number of the animal group from which animals are to be removed. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of animals has been removed.</param>
        public void Sell(double number, int group = 0)
        {
            StockSell selling = new StockSell();
            selling.Group = group;
            selling.Number = Convert.ToInt32(number, CultureInfo.InvariantCulture);
            string msg = "Selling " + number.ToString() + " animals ";
            if (group == 0)
                msg += "from all groups";
            else
                msg += "from group " + group.ToString();
            this.outputSummary.WriteMessage(this, msg);
            this.stockModel.DoStockManagement(this.stockModel, selling, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Removes animals from the simulation by tag number.
        /// </summary>
        /// <param name="number">Number of animals to sell.</param>
        /// <param name="tag">Tag number of the animals from which animals are to be removed. 
        /// Animals are removed starting from the group with the smallest index.</param>
        public void SellTag(int number, int tag)
        {
            StockSellTag selling = new StockSellTag();
            selling.Tag = tag;
            selling.Number = number;
            this.outputSummary.WriteMessage(this, "Selling " + number.ToString() + " animals from tag group " + tag.ToString());
            this.stockModel.DoStockManagement(this.stockModel, selling, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Shears sheep. The event has no effect on cattle
        /// </summary>
        /// <param name="subGroup">Denotes whether the main group of animals, suckling lambs, or both should be shorn. 
        /// Feasible values are the null string (main group), ‘adults’ (main group), ‘lambs’ (suckling lambs), ‘both’ (both).</param>
        /// <param name="group">Index number of the animal group to be shorn. 
        /// A value of zero denotes that all animal groups should be processed.</param>
        public void Shear(string subGroup, int group = 0)
        {
            StockShear shearing = new StockShear();
            shearing.Group = group;
            shearing.SubGroup = subGroup;
            string msg = "Shearing animals ";
            if (group == 0)
                msg += "in all groups";
            else
                msg += "in group " + group.ToString();
            this.outputSummary.WriteMessage(this, msg);
            this.stockModel.DoStockManagement(this.stockModel, shearing, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Changes the paddock to which an animal group is assigned.
        /// </summary>
        /// <param name="paddock">Name of the paddock to which the animal group is to be moved.</param>
        /// <param name="group">Index number of the animal group to be moved.</param>
        public void Move(string paddock, int group)
        {
            StockMove move = new StockMove();
            move.Group = group;
            move.Paddock = paddock;
            this.outputSummary.WriteMessage(this, "Moving animal group " + group.ToString() + " to " + paddock);
            this.stockModel.DoStockManagement(this.stockModel, move, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Move the animals by tag number
        /// </summary>
        /// <param name="paddock">Name of the paddock to which the animals are to be moved.</param>
        /// <param name="tag">The tag number</param>
        public void MoveTag(string paddock, int tag)
        {
            StockMove move = new StockMove();
            move.Paddock = paddock;
            for (int g = 1; g <= this.stockModel.Count(); g++)
            {
                if ((this.stockModel.At(g) != null) && (tag == this.stockModel.GetTag(g)))
                {
                    move.Group = g;
                    this.outputSummary.WriteMessage(this, "Moving " + this.stockModel.At(g).NoAnimals.ToString() + " animals tagged " + tag.ToString() + " to " + paddock);
                    this.stockModel.DoStockManagement(this.stockModel, move, this.localWeather.TheDay, this.localWeather.Latitude);
                }
            }
        }

        /// <summary>
        /// Commences mating of a particular group of animals.  If the animals are not empty females, or if they are too young, has no effect
        /// </summary>
        /// <param name="mateTo">Genotype of the rams or bulls with which the animals are mated. 
        /// Must match the name field of a member of the genotypes property.</param>
        /// <param name="mateDays">Length of the mating period in days.</param>
        /// <param name="group">Index number of the animal group for which mating is to commence. 
        /// A value of zero denotes that all empty females of sufficient age should be mated</param>
        public void Join(string mateTo, int mateDays, int group = 0)
        {
            StockJoin join = new StockJoin();
            join.Group = group;
            join.MateTo = mateTo;
            join.MateDays = mateDays;
            string msg = "Joining animals in ";
            if (group == 0)
                msg += "all groups to " + mateTo;
            else
                msg += "group " + group.ToString() + " to " + mateTo;
            this.outputSummary.WriteMessage(this, msg);
            this.stockModel.DoStockManagement(this.stockModel, join, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Converts ram lambs to wether lambs, or bull calves to steers.  If the animal group(s) denoted by group has no suckling young, has no effect. 
        /// If the number of male lambs or calves in a nominated group is greater than the number to be castrated, the animal group will be split; 
        /// the sub-group with castrated offspring will remain at the original index and the sub-group with offspring that were not castrated will 
        /// be added at the end of the set of animal groups.
        /// </summary>
        /// <param name="number">Number of male lambs or calves to be castrated.</param>
        /// <param name="group">Index number of the animal group, the lambs or calves of which are to be castrated. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of offspring has been castrated.</param>
        public void Castrate(int number, int group = 0)
        {
            StockCastrate castrate = new StockCastrate();
            castrate.Group = group;
            castrate.Number = number;
            string msg = "Castrate " + number.ToString() + " animals ";
            if (group == 0)
                msg += "from all groups";
            else
                msg += "in group " + group.ToString();
            this.outputSummary.WriteMessage(this, msg);
            this.stockModel.DoStockManagement(this.stockModel, castrate, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Weans some or all of the lambs or calves from an animal group. 
        /// The newly weaned animals are added to the end of the list of animal groups, with males and females in separate groups.
        /// </summary>
        /// <param name="sex">The sex to wean.
        /// Feasible values are:
        /// ‘all’       Female and male lambs or calves are to be weaned.
        /// ‘females’   Only female lambs or calves are to be weaned.
        /// ‘males’     Only male lambs or calves are to be weaned</param>
        /// <param name="number">The number of lambs or calves to be weaned</param>
        /// <param name="group">The index number of the animal group from which animals are to be removed. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of lambs or calves has been weaned</param>
        public void Wean(string sex, int number, int group = 0)
        {
            StockWean wean = new StockWean();
            wean.Sex = sex;
            wean.Group = group;
            wean.Number = number;
            string msg = "Weaning " + wean.Number.ToString() + " " + wean.Sex;
            if (wean.Group == 0)
                msg += " from all groups";
            else
                msg += " from group " + wean.Group.ToString();
            this.outputSummary.WriteMessage(this, msg);
            this.stockModel.DoStockManagement(this.stockModel, wean, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Ends lactation in cows that have already had their calves weaned.  The event has no effect on other animals.
        /// If the number of cows in a nominated group is greater than the number to be dried off, the animal group will be split; 
        /// the sub-group that is no longer lactating will remain at the original index and the sub-group that continues lactating will be added at the end of the set of animal groups
        /// </summary>
        /// <param name="number">Number of females for which lactation is to end.</param>
        /// <param name="group">Index number of the animal group for which lactation is to end. 
        /// A value of zero denotes that each animal group should be processed in turn until the nominated number of cows has been dried off.</param>
        public void DryOff(int number, int group = 0)
        {
            StockDryoff dryoff = new StockDryoff();
            dryoff.Group = group;
            dryoff.Number = number;
            string msg = "Drying off " + number.ToString() + " animals ";
            if (group == 0)
                msg += "over all groups";
            else
                msg += "in group " + group.ToString();
            this.outputSummary.WriteMessage(this, msg);
            this.stockModel.DoStockManagement(this.stockModel, dryoff, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Creates new animal groups from all the animal groups.  The new groups are placed at the end of the animal group list. 
        /// This event is for when splits need to occur over all animal groups. Description of split event also applies.
        /// </summary>
        /// <param name="splitall">The split data</param>
        public void SplitAll(StockSplitAll splitall)
        {
            this.outputSummary.WriteMessage(this, "Split all animals by " + splitall.Type + " at " + splitall.Value);
            this.stockModel.DoStockManagement(this.stockModel, splitall, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Creates two or more animal groups from the nominated group.  
        /// One of these groups is placed at the end of the animal group list. 
        /// The new groups remain in the same paddock and keep the same tag value as the original animal group. 
        /// The division may only persist until the beginning of the next do_stock step, when sufficiently similar 
        /// groups of animals are merged.Splitting an animal group is therefore usually carried out as a preliminary to some other management event.
        /// </summary>
        /// <param name="split">The split data</param>
        public void Split(StockSplit split)
        {
            this.outputSummary.WriteMessage(this, "Split animals by " + split.Type + " at " + split.Value);
            this.stockModel.DoStockManagement(this.stockModel, split, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Changes the “tag value” associated with an animal group.  
        /// This value is used to sort animals; it can also be used to group animals for user-defined purposes 
        /// (e.g. to identify animals that are to be managed as a single mob even though they differ physiologically) 
        /// and to keep otherwise similar animal groups distinct from one another.
        /// </summary>
        /// <param name="value">Tag value to be assigned.</param>
        /// <param name="group">Index number of the animal group to be assigned a tag value.</param>
        public void Tag(int value, int group)
        {
            StockTag tag = new StockTag();
            tag.Group = group;
            tag.Value = value;
            this.outputSummary.WriteMessage(this, "Tag animal group " + group.ToString() + " to " + value.ToString());
            this.stockModel.DoStockManagement(this.stockModel, tag, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Sets the "priority" of an animal group for later use in a draft event. It is usual practice to use positive values for priorities.
        /// </summary>
        /// <param name="value">New priority value for the group.</param>
        /// <param name="group">Index number of the animal group for which priority is to be set.</param>
        public void Prioritise(int value, int group)
        {
            StockPrioritise prioritise = new StockPrioritise();
            prioritise.Group = group;
            prioritise.Value = value;
            this.outputSummary.WriteMessage(this, "Prioritise animal group " + group.ToString() + " to " + value.ToString());
            this.stockModel.DoStockManagement(this.stockModel, prioritise, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        /// <summary>
        /// Rearranges the list of animal groups in ascending order of tag value. This event has no parameters.
        /// </summary>
        public void Sort()
        {
            StockSort sortEvent = new StockSort();
            this.outputSummary.WriteMessage(this, "Sort animals");
            this.stockModel.DoStockManagement(this.stockModel, sortEvent, this.localWeather.TheDay, this.localWeather.Latitude);
        }

        #endregion ============================================

        #region Private functions ============================================
        /// <summary>
        /// Get the current time and weather values
        /// </summary>
        private void GetTimeAndWeather()
        {
            this.currentTime = this.systemClock.Today;
            this.localWeather.DayLength = this.locWtr.CalculateDayLength(-6.0);   // civil twighlight
            this.localWeather.TheDay = this.currentTime.Day + (this.currentTime.Month * 0x100) + (this.currentTime.Year * 0x10000);
            this.localWeather.MaxTemp = this.locWtr.MaxT;
            this.localWeather.MinTemp = this.locWtr.MinT;
            this.localWeather.Precipitation = this.locWtr.Rain;
            this.localWeather.WindSpeed = this.locWtr.Wind;
        }

        /// <summary>
        /// Do a request for all the biomasses in every paddock
        /// Note: This could be optimised to not request paddocks that are unstocked (drafting still needs to get the amounts)
        /// </summary>
        private void RequestAvailableToAnimal()
        {
            ForageProvider forageProvider;

            // iterate through all the paddocks and sum the total green and store it in each forage provider
            for (int idx = 0; idx <= this.stockModel.Paddocks.Count() - 1; idx++)
            {
                double pastureGreen = 0;
                PaddockInfo paddInfo = this.stockModel.Paddocks.ByIndex(idx);
                for (int i = 0; i <= this.stockModel.ForagesAll.Count() - 1; i++)
                {
                    forageProvider = this.stockModel.ForagesAll.ForageProvider(i);
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

                for (int i = 0; i <= this.stockModel.ForagesAll.Count() - 1; i++)
                {
                    forageProvider = this.stockModel.ForagesAll.ForageProvider(i);
                    if (string.Compare(forageProvider.OwningPaddock.Name, paddInfo.Name, true) == 0)
                    {
                        forageProvider.PastureGreenDM = pastureGreen;
                    }
                }
            }

            // now update the available forages
            for (int i = 0; i <= this.stockModel.ForagesAll.Count() - 1; i++)
            {
                forageProvider = this.stockModel.ForagesAll.ForageProvider(i);
                if (forageProvider.ForageObj != null)
                {
                    forageProvider.UpdateForages(forageProvider.ForageObj);
                }
            }
        }

        /// <summary>
        /// Populate the AddFaecesType object
        /// </summary>
        /// <param name="paddID">The paddock ID</param>
        /// <param name="faecesValue">The faeces data</param>
        /// <returns>True if the number of defaecations > 0</returns>
        private bool PopulateFaeces(int paddID, Surface.AddFaecesType faecesValue)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;
            bool result = false;

            this.stockModel.ReturnExcretion(paddID, out this.excretionInfo);

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
        /// <param name="paddID">The paddock ID</param>
        /// <param name="urineValue">The urine data</param>
        /// <returns>True if the number of urinations > 0</returns>
        private bool PopulateUrine(int paddID, AddUrineType urineValue)
        {
            int n = (int)GrazType.TOMElement.n;
            int p = (int)GrazType.TOMElement.p;
            int s = (int)GrazType.TOMElement.s;
            bool result = false;

            this.stockModel.ReturnExcretion(paddID, out this.excretionInfo);
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