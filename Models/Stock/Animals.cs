namespace Models.GrazPlan
{
    using Models.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Information required to initialise a single animal group
    /// The YoungWt and YoungGFW fields may be set to MISSING, in which case    
    /// TStockList will estimate defaults.                                       
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Stock))]
    public class Animals : Model
    {
        [Link]
        private Stock stock = null;

        // ------------------ Properties for type of animal ------------------

        /// <summary>The type of animal.</summary>
        [Separator("Type of animals in group")]

        [Description("Animal type")]
        [Display(Values = "GetAnimalTypes")]
        [Units("-")]
        public string AnimalType { get; set; }

        /// <summary>Genotype of this group of animals.</summary>
        [Description("Genotype of this group of animals.")]
        [Display(Values = "GetGenotypeNames")]
        [Units("-")]
        public string Genotype { get; set; }

        // ------------------ Properties for adult animals ------------------

        /// <summary>Number of animals.</summary>
        [Separator("Adult animals")]

        [Description("Number of animals")]
        [Units("-")]
        public int Number { get; set; }

        /// <summary>Reproductive status of animals</summary>
        [Description("Reproductive status of animals")]
        [Units("-")]
        public GrazType.ReproType Sex { get; set; }

        /// <summary>Age (days)</summary>
        [Description("Age of animals")]
        [Units("days")]
        public int AgeDays { get; set; }

        /// <summary>Unfasted live weight of the animals.</summary>
        [Description("Unfasted live weight of the animals")]
        [Units("kg")]
        public double Weight { get; set; }

        /// <summary>Highest weight recorded to date.</summary>
        [Description("Highest weight recorded to date")]
        [Units("kg")]
        public double MaxPrevWt { get; set; }

        /// <summary>Greasy fleece weight of the animals.</summary>
        [Description("Greasy fleece weight of the animals")]
        [Units("kg")]
        [Display(EnabledCallback = "IsSheepSelected")]
        public double FleeceWt { get; set; }

        /// <summary>Average wool fibre diameter of the animals.</summary>
        [Description("Average wool fibre diameter of the animals")]
        [Units("um")]
        [Display(EnabledCallback = "IsSheepSelected")]
        public double FibreDiam { get; set; }

        /// <summary>Genotype of the bulls/rams to which pregnant or lactating animals were mated.</summary>
        [Description("Genotype of the bulls/rams to which pregnant or lactating animals were mated")]
        [Display(Values = "GetGenotypeNames")]
        [Units("-")]
        public string MatedTo { get; set; }

        /// <summary>Days lactating. 1 or more denotes the time since parturition.</summary>
        [Description("Days lactating. 1 or more denotes the time since parturition")]
        [Units("d")]
        public int Lactating { get; set; }

        /// <summary>Days pregnant. Zero denotes not pregnant.</summary>
        [Description("Days pregnant. Zero denotes not pregnant")]
        [Units("d")]
        public int Pregnant { get; set; }

        /// <summary>Number of foetuses or suckling lambs.</summary>
        [Description("Number of foetuses or suckling lambs")]
        [Display(EnabledCallback = "ArePregnant")]
        [Units("-")]
        public int NumFoetuses { get; set; }

        /// <summary>Paddock occupied by the animals.</summary>
        [Description("Paddock occupied by the animals")]
        [Display(Values = "GetFieldNames")]
        [Units("-")]
        public string Paddock { get; set; }

        /// <summary>Initial tag value for the animal group.</summary>
        [Description("Initial tag value for the animal group")]
        [Units("-")]
        public int Tag { get; set; }

        // ------------------ Properties for young animals ------------------

        /// <summary>Number of suckling young.</summary>

        [Separator("Young animals")]

        [Description("Number of suckling young")]
        [Units("-")]
        public int NumSuckling { get; set; }

        /// <summary>Unfasted live weight of suckling calves/lambs.</summary>
        [Description("Unfasted live weight of suckling calves/lambs")]
        [Units("kg")]
        [Display(EnabledCallback = "AreYoungPresent")]
        public double YoungWt { get; set; }

        /// <summary>Birth Condition score.</summary>
        [Description("Birth Condition score")]
        [Display(EnabledCallback = "AreYoungPresent")]
        [Units("-")]
        public double BirthCS { get; set; }

        /// <summary>Greasy fleece weight of suckling lambs.</summary>
        [Description("Greasy fleece weight of suckling lambs")]
        [Units("kg")]
        [Display(EnabledCallback = "AreYoungPresent,IsSheepSelected")]
        public double YoungGFW { get; set; }

        // ------------------ Properties for user interface ------------------

        /// <summary>Get the names of all genotypes for the current animal type.</summary>
        public IEnumerable<string> GetAnimalTypes()
        {
            return stock.Genotypes.All.Select(genotype => genotype.AnimalType).Distinct();
        }

        /// <summary>Get the names of all genotypes for the current animal type.</summary>
        public IEnumerable<string> GetGenotypeNames()
        {
            return stock.Genotypes.All.Where(genotype => genotype.AnimalType == AnimalType)
                                      .Select(genotype => genotype.Name);
        }

        /// <summary>Is the animal type sheep?</summary>
        [Units("-")]
        public bool IsSheepSelected {  get { return AnimalType == "Sheep"; } }

        /// <summary>Is the animal type sheep?</summary>
        [Units("-")]
        public bool AreYoungPresent { get { return NumSuckling > 0; } }

        /// <summary>Are the animals pregnant?</summary>
        [Units("-")]
        public bool ArePregnant { get { return Pregnant > 0; } }

        /// <summary>Get the names of all fields.</summary>
        public IEnumerable<string> GetFieldNames()
        {
            return this.FindAllInScope<Zone>().Select(zone => zone.Name);
        }

        // ------------------ Events subscribed to ------------------

        /// <summary>Invoked at the start of the simulation.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            stock.StockModel.Add(this);
        }
    }
}
