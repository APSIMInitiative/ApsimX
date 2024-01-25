﻿using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;

namespace Models.GrazPlan
{

    /// <summary>
    /// An instance of this class creates a genotype cross and adds it to the list of 
    /// available crosses.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Stock))]
    public class GenotypeCross : Model
    {
        private const double DAYSPERYR = 365.25;

        [Link]
        private Stock stock = null;

        /// <summary>Gets or sets the animal type.</summary>
        [Description("Animal type")]
        [Display(Values = "GetAnimalTypes")]
        public string AnimalType { get; set; }

        /// <summary>The pure bred breed name.</summary>
        [Description("Purebred breed name")]
        [Display(Values = "GetGenotypeNames", EnabledCallback = "PurebredEnabled")]
        public string PureBredBreed { get; set; }

        /// <summary>The dam breed name.</summary>
        [Description("Dam breed name for crosses")]
        [Display(Values = "GetGenotypeNames", EnabledCallback = "CrossEnabled")]
        public string DamBreed { get; set; }

        /// <summary>The sire breed name.</summary>
        [Description("Sire breed name for crosses")]
        [Display(Values = "GetGenotypeNames", EnabledCallback = "CrossEnabled")]
        public string SireBreed { get; set; }

        /// <summary>Base rate of mortality in mature animals. Default is 0.0.</summary>
        [Description("Base rate of mortality in mature animals")]
        [Units("/yr")]
        public double MatureDeathRate { get; set; } = double.NaN;

        /// <summary>Base rate of mortality in weaners. Default is 0.0.</summary>
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

        /// <summary>Is the pure bred drop down enabled?</summary>
        public bool PurebredEnabled
        {
            get
            {
                return string.IsNullOrEmpty(DamBreed) && string.IsNullOrEmpty(SireBreed);
            }
        }

        /// <summary>Are the cross drop downs enabled?</summary>
        public bool CrossEnabled
        {
            get
            {
                return string.IsNullOrEmpty(PureBredBreed);
            }
        }

        /// <summary>Get the names of all genotypes for the current animal type.</summary>
        public IEnumerable<string> GetAnimalTypes()
        {
            if (AnimalType == null)
                DetermineAnimalType();
            return stock.Genotypes.All.Select(genotype => genotype.AnimalType).Distinct();
        }

        /// <summary>Get the names of all genotypes for the current animal type.</summary>
        public IEnumerable<string> GetGenotypeNames()
        {
            if (AnimalType == null)
                DetermineAnimalType();
            return stock.Genotypes.All.Where(genotype => genotype.AnimalType == AnimalType)
                                      .Select(genotype => genotype.Name);
        }

        /// <summary>the animal type from the breed names.</summary>
        private void DetermineAnimalType()
        {
            var genotypeName = PureBredBreed;
            if (string.IsNullOrEmpty(genotypeName))
                genotypeName = DamBreed;
            if (!string.IsNullOrEmpty(genotypeName))
            {
                var genotype = stock.Genotypes.All.FirstOrDefault(g => g.Name == genotypeName);
                if (genotype != null)
                    AnimalType = genotype.AnimalType;
            }
        }

        /// <summary>
        /// At the start of the simulation
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            Genotype newGenotype;
            if (!string.IsNullOrEmpty(PureBredBreed))
            {
                newGenotype = stock.Genotypes.Get(PureBredBreed);
                newGenotype.Name = Name;
                stock.Genotypes.Add(newGenotype);
            }
            else if (string.IsNullOrEmpty(DamBreed) || string.IsNullOrEmpty(SireBreed))
                throw new Exception("You must specify a either a pure bred breed name or a dam and sire breed name when creating genotype crosses.");
            else
            {
                var damProportion = Math.Pow(0.5, Generation);
                var sireProportion = 1 - damProportion;
                newGenotype = CreateGenotypeCross(Name, DamBreed, damProportion, SireBreed, sireProportion);
            }
            newGenotype.InitialiseWithParams(SRW, PotFleeceWt, MaxFibreDiam, FleeceYield, PeakMilk, Conception, MatureDeathRate, WeanerDeathRate);
        }

        /// <summary>
        /// Create a genotype cross.                                      
        /// </summary>
        /// <param name="nameOfNewGenotype">Name of new genotype. Can be null.</param>
        /// <param name="damBreedName">Dam breed name.</param>
        /// <param name="damProportion">Proportion dam.</param>
        /// <param name="sireBreedName">Sire breed name.</param>
        /// <param name="sireProportion">Proportion sire.</param>
        private Genotype CreateGenotypeCross(string nameOfNewGenotype,
                                                       string damBreedName, double damProportion,
                                                       string sireBreedName, double sireProportion)
        {
            if (damProportion + sireProportion != 1)
                throw new Exception("When creating a cross breed the total proportions must be equal to one.");

            var damBreed = stock.Genotypes.Get(damBreedName);
            damBreed.Initialise();

            var sireBreed = stock.Genotypes.Get(sireBreedName);
            sireBreed.Initialise();

            Genotype newGenotype = new Genotype(nameOfNewGenotype, damBreed, sireBreed, damProportion, sireProportion);

            // Add the new genotype into our list.
            stock.Genotypes.Add(newGenotype);

            return newGenotype;
        }
    }
}
