namespace Models.GrazPlan
{
    using Models.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encapsulates a blend of genotypes that is itself a genotype.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Stock))]
    public class GenotypeCross : Model
    {
        private const double DAYSPERYR = 365.25;

        [Link]
        private Stock stock = null;

        /// <summary>Base rate of mortality in mature animals. Default is 0.0.
        /// [1] = Base rate of mortality in weaners.Default is 0.0.
        /// </summary>
        [Description("Base rate of mortality in mature animals")]
        [Units("/yr")]
        public double MatureDeathRate { get; set; } = double.NaN;
        
        /// <summary>Base rate of mortality in mature animals. Default is 0.0.
        /// [1] = Base rate of mortality in weaners.Default is 0.0.
        /// </summary>
        [Description("Base rate of mortality in weaners")]
        [Units("/yr")]
        public double WeanerDeathRate { get; set; } = double.NaN;

        /// <summary>
        /// Expected rates of conception with 1, 2 and 3 young for mature ewes or cows in average body condition,
        /// over a mating period lasting 2.5 oestrus cycles.Only the first two elements are meaningful for cattle.
        /// </summary>
        [Description("Peak Conception rates (1, 2, 3 young) for mature ewes or cows in average body condition. Only 2 values for cattle.")]
        public double[] Conception { get; set; } = new double[4];

        /// <summary>
        /// Gets or sets the animal type.
        /// </summary>
        [Description("Animal type")]
        [Display(Values = "GetAnimalTypes")]
        public string AnimalType { get; set; }

        /// <summary>
        /// Gets or sets the dam breed name
        /// </summary>
        [Description("Dam breed name")]
        [Display(Values = "GetGenotypeNames")]
        public string DamBreed { get; set; }

        /// <summary>
        /// Gets or sets the sire breed name
        /// </summary>
        [Description("Sire breed name")]
        [Display(Values = "GetGenotypeNames")]
        public string SireBreed { get; set; }

        /// <summary>
        /// Gets or sets the generation
        /// Number of generations of crossing: 0 denotes the pure-bred maternal genotype (in which case SireBreed is
        /// not used), 1 a first cross, 2 a second cross(75% sire:25% dam), etc.
        /// </summary>
        [Description("Number of generations of crossing. 1=first cross, 2=second cross(75% sire:25% dam)")]
        public int Generation { get; set; }

        /// <summary>
        /// Gets or sets the standard reference weight
        /// Breed standard reference weight. The default value depends on DamBreed and SireBreed.
        /// </summary>
        [Description("Standard reference weight")]
        [Units("kg")]
        public double SRW { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the potential fleece weight
        /// Breed reference fleece weight in sheep. The default value depends on DamBreed and SireBreed.
        /// </summary>
        [Description("Potential fleece weight")]
        [Units("kg")]
        public double PotFleeceWt { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the maximum wool fibre diameter
        /// Maximum average wool fibre diameter in sheep. The default depends on DamBreed and SireBreed.
        /// </summary>
        [Description("Maximum wool fibre diameter")]
        [Units("u")]
        public double MaxFibreDiam { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the fleece yield
        /// Clean fleece weight as a proportion of greasy fleece weight in sheep. Default is 0.70.
        /// </summary>
        [Description("Clean fleece weight as a proportion of greasy fleece weight in sheep")]
        [Units("kg/kg")]
        public double FleeceYield { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the peak milk production
        /// Potential maximum milk yield per head, in 4% fat-corrected milk equivalents, in cattle. Default is 20.0.
        /// </summary>
        [Description("Potential maximum milk yield per head, in 4% fat-corrected milk equivalents, in cattle")]
        [Units("kg")]
        public double PeakMilk { get; set; } = double.NaN;

        /// <summary>Get the names of all genotypes for the current animal type.</summary>
        /// <returns></returns>
        public IEnumerable<string> GetAnimalTypes()
        {
            return stock.AllGenotypes.GetAnimalTypes();
        }

        /// <summary>Get the names of all genotypes for the current animal type.</summary>
        /// <returns></returns>
        public IEnumerable<string> GetGenotypeNames()
        {
            return stock.AllGenotypes.GetGenotypeNamesForAnimalType(AnimalType);
        }

        /// <summary>
        /// At the start of the simulation
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            var damProportion = Math.Pow(0.5, Generation);
            var sireProportion = 1 - damProportion;
            var newGenotype = stock.AllGenotypes.CreateGenotypeCross(Name, DamBreed, damProportion, SireBreed, sireProportion);
            if (!double.IsNaN(SRW))
                newGenotype.BreedSRW = SRW;
            if (!double.IsNaN(PotFleeceWt))
                newGenotype.PotentialGFW = PotFleeceWt;
            if (!double.IsNaN(MaxFibreDiam))
                newGenotype.MaxMicrons = MaxFibreDiam;
            if (!double.IsNaN(FleeceYield))
                newGenotype.FleeceYield = FleeceYield;
            if (!double.IsNaN(PeakMilk))
                newGenotype.PotMilkYield = PeakMilk;
            if (Conception != null && Conception.Length == 4 && !double.IsNaN(Conception[0]))
            newGenotype.Conceptions = Conception;
            if (!double.IsNaN(MatureDeathRate))
            {
                if (1.0 - MatureDeathRate < 0)
                    throw new Exception("Power of negative number attempted in setting mature death rate.");

                newGenotype.MortRate[1] = 1.0 - Math.Pow(1.0 - MatureDeathRate, 1.0 / DAYSPERYR);
            }
            if (!double.IsNaN(WeanerDeathRate))
            {
                if (1.0 - WeanerDeathRate < 0)
                    throw new Exception("Power of negative number attempted in setting weaner death rate.");

                newGenotype.MortRate[2] = 1.0 - Math.Pow(1.0 - WeanerDeathRate, 1.0 / DAYSPERYR);
            }
        }
    }
}
