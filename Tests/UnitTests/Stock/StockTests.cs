namespace UnitTests.Stock
{
    using Models.Core;
    using Models.GrazPlan;
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class StockTests
    {
        /// <summary>Make sure parameters with all values and some values missing work.</summary>
        [Test]
        public void TestReadingPRM()
        {
            var xml = "<parameters name=\"standard\" version=\"2.0\">" +
                      "  <par name=\"editor\">Andrew Moore</par>" +
                      "  <par name=\"edited\">30 Jan 2013</par>" +
                      "  <par name=\"dairy\">false</par>" +
                      "  <par name=\"c-srs-\">1.2,1.4</par>" +
                      "  <par name=\"c-i-\">,1.7,,,,25.0,22.0,,,,,0.15,,0.002,0.5,1.0,0.01,20.0,3.0,1.5</par>" +
                      "  <par name=\"c-w-\">1.1,,</par>" +
                      "  <set name=\"small ruminants\">" +
                      "     <par name=\"c-w-\">,0.004,</par>" +
                      "     <set name=\"sheep\">" +
                      "        <par name=\"c-w-0\">0.999</par>" +
                      "     </set>" +
                      "  </set>" +
                      "</parameters>";
            var genotypes = new Genotypes();
            genotypes.ReadPRM(xml);
            var animalParamSet = genotypes.Get("sheep").Parameters;

            Assert.AreEqual("Andrew Moore", animalParamSet.sEditor);
            Assert.AreEqual("30 Jan 2013", animalParamSet.sEditDate);
            Assert.IsFalse(animalParamSet.bDairyBreed);
            Assert.AreEqual(new double[] { 1.2, 1.4 }, animalParamSet.SRWScalars);
            Assert.AreEqual(new double[] { 0, 0, 1.7, 0, 0, 0, 25.0, 22.0, 0, 0, 0, 0, 0.15, 0, 0.002, 0.5, 1.0, 0.01, 20, 3, 1.5, 0 }, animalParamSet.IntakeC);
            Assert.AreEqual(new double[] { 0.999, 1.1, 0.004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, animalParamSet.WoolC);
        }

        /// <summary>Test that a genotype can be extracted from the stock resource file.</summary>
        [Test]
        public void GetStandardGenotype()
        {
            var genotypes = new Genotypes();
            var friesian = genotypes.Get("Friesian").Parameters;
            Assert.AreEqual(550,  friesian.BreedSRW, 550);
            Assert.AreEqual(0.05, friesian.SelfWeanPropn);
            Assert.IsTrue(friesian.bDairyBreed);
            Assert.AreEqual(new double[] { 0.0, 0.85, 0.577, 0.9 },        friesian.IntakeLactC);
            Assert.AreEqual(new double[] { 0.0, 0.0115, 0.27, 0.4, 1.1 }, friesian.GrowthC);
        }

        /// <summary>Ensure that a user supplied genotype overrides a standard one.</summary>
        [Test]
        public void EnsureUserGenotypeOverridesStandardGenotype()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var friesian = genotypes.Get("Friesian").Parameters;

            // Clone the genotype and change it.
            friesian = Apsim.Clone(friesian) as AnimalParamSet;
            friesian.BreedSRW = 1;

            // Give it to the genotypes instance as a user genotype.
            genotypes.Add(friesian);

            // Now ask for friesian again. This time it should return the user genotype, not the standard one.
            friesian = genotypes.Get("Friesian").Parameters;

            Assert.AreEqual(1, friesian.BreedSRW);
        }

        /// <summary>Ensure there are no dot characters in genotype names.</summary>
        [Test]
        public void EnsureNoDotsInGenotypeNames()
        {
            var genotypes = new Genotypes();
            foreach (var genotypeName in genotypes.All.Select(genotype => genotype.Name))
                Assert.IsFalse(genotypeName.Contains("."));
        }

        /// <summary>Ensure we can get a list of all animal types represented in the genotypes.</summary>
        [Test]
        public void GetAllAnimalTypes()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var animalTypes = genotypes.All.Select(genotype=>genotype.AnimalType).Distinct();

            Assert.AreEqual(animalTypes.ToArray(), new string[] { "", "sheep", "cattle" });
        }

        /// <summary>Ensure we can get a list of all genotype names for an animal type.</summary>
        [Test]
        public void GetGenotypeNamesForAnimalType()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var genotypeNames = genotypes.All.Where(genotype => genotype.AnimalType == "cattle")
                                             .Select(genotype => genotype.Name);

            Assert.AreEqual(genotypeNames.ToArray(), new string[] { "cattle", "beef breeds", "british beef", "Hereford", "Angus",
                                                                  "Beef Shorthorn", "South Devon", "Ujimqin Cattle", "Ujimqin x Angus (1st cross)",                                                                  "Ujimqin x Angus (2nd cross)", "indicus", "Brahman", "european",
                                                                  "Charolais", "Simmental", "Limousin", "Chianina", "British x Brahman",
                                                                  "British x Charolais", "Ujimqin x Charolais (1st cross)",
                                                                  "Ujimqin x Charolais (2nd cross)", "british-friesian crosses",
                                                                  "British x Friesian", "British x Holstein", "charolais-friesian crosses",
                                                                  "Charolais x Friesian", "Charolais x Holstein", "dairy breeds", "Jersey",
                                                                  "Ayrshire", "Guernsey", "Dairy Shorthorn", "Brown Swiss", "Friesian",
                                                                  "Holstein"});

        }

        /// <summary>Ensure we can create an animal cross genotype.</summary>
        [Test]
        public void CreateAnimalCross()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var animalParamSet = genotypes.CreateGenotypeCross("NewGenotype", "Friesian", 0.5, "Jersey", 0.5);

            // Make sure we can retrieve the new genotype.
            Assert.IsNotNull(genotypes.Get("NewGenotype"));

            Assert.AreEqual("Andrew Moore", animalParamSet.sEditor);
            Assert.AreEqual("30 Jan 2013", animalParamSet.sEditDate);
            Assert.AreEqual("NewGenotype", animalParamSet.Name);
            Assert.IsTrue(animalParamSet.bDairyBreed);
            Assert.AreEqual(new double[] { 1.2, 1.4 }, animalParamSet.SRWScalars);
            Assert.AreEqual(new double[] { 0, 0.025, 1.7, 0.22, 60, 0.02, 25, 22, 81, 1.7, 0.6, 0.05, 0.15, 0.005, 0.002, 0.5, 1.0, 0.01, 20, 3, 1.5, 0.7 }, animalParamSet.IntakeC);
            Assert.AreEqual(new double[] { 0, 285, 2.2, 1.77, 0.33, 1.8, 2.42, 1.16, 4.11, 343.5, 0.0164, 0.134, 6.22, 0.747 }, animalParamSet.PregC);
        }

        /// <summary>Ensure we can create and initialise an animal cross as user would in GUI.</summary>
        [Test]
        public void CreateAnimalCrossFromGUI()
        {
            // Get a friesian genotype.
            var stock = new Stock();
            var genotypeCross = new GenotypeCross()
            {
                Name = "NZFriesianCross",
                DamBreed = "Friesian",
                SireBreed = "Jersey",
                MatureDeathRate = 0.2,
                SRW = 550,
                PeakMilk = 35,
                FleeceYield = 1,        // a dairy cow that has a fleece! :)
                Conception = new double[] { 100, 0, 0, 0 }
            };
            Utilities.InjectLink(genotypeCross, "stock", stock);

            // Invoke start of simulation event. This should create a genotype cross.
            Utilities.CallEvent(genotypeCross, "StartOfSimulation");

            // Get the cross.
            var animalParamSet = stock.Genotypes.Get("NZFriesianCross").Parameters;

            Assert.AreEqual("NZFriesianCross", animalParamSet.Name);
            Assert.IsTrue(animalParamSet.bDairyBreed);
            Assert.AreEqual(550, animalParamSet.BreedSRW);
            Assert.AreEqual(35, animalParamSet.PeakMilk);
            Assert.AreEqual(new double[] { 0, 0 }, animalParamSet.ConceiveSigs[0]);
            Assert.AreEqual(new double[] { 10, 5.89 }, animalParamSet.ConceiveSigs[1]);
            Assert.AreEqual(new double[] { 10, 5.89 }, animalParamSet.ConceiveSigs[2]);
            Assert.AreEqual(new double[] { 0, 0 }, animalParamSet.ConceiveSigs[3]);
            Assert.AreEqual(1, animalParamSet.FleeceYield);
            Assert.AreEqual(new double[] { 0, 0.00061074716558540132, 5.53E-05 }, animalParamSet.MortRate);
        }

        /// <summary>Ensure we can add an animal group to STOCK.</summary>
        //[Test]
        //public void AddAnimalGroupToStock()
        //{
        //    // Get a friesian genotype.
        //    var stock = new Stock
        //    {
        //        Children = new List<Model>()
        //        {
        //            new Clock(),
        //            new Weather(),
        //            new MockSummary(),
        //            new Zone()
        //            {
        //                Name = "Field1",
        //                Area = 100
        //            },
        //            new AnimalGroup()
        //            {
        //                MeanAge = 100,
        //                GenotypeName = "Jersey",
        //                InitialMaxPrevWeight = 300,
        //                InitialNumberOfAnimals = 50,
        //                PaddockName = "Field1",
        //                ReproStatus = GrazType.ReproType.Empty,
        //                InitialLiveWeight = 290,
        //                MatedToGenotypeName = "Friesian"
        //            }
        //        }
        //    };
        //    Utilities.ResolveLinks(stock);

        //    // Invoke start of simulation event. This should add the animal group to stock.
        //    Utilities.CallEvent(stock, "StartOfSimulation");

        //    // Get the animal group
        //    var animalGroup = stock.AnimalList.At(1);

        //    Assert.AreEqual(100, animalGroup.MeanAge);
        //    Assert.AreEqual(GrazType.AgeType.Weaner ,animalGroup.AgeClass);
        //    Assert.AreEqual(GrazType.AnimalType.Cattle, animalGroup.Animal);
        //    Assert.AreEqual(0, animalGroup.AnimalsPerHa);  // I would not expect zero here.
        //    Assert.AreEqual(2.791845743237555, animalGroup.BirthCondition);
        //    Assert.AreEqual(290, animalGroup.BaseWeight);
        //    Assert.AreEqual(2.791845743237555, animalGroup.BodyCondition);
        //    Assert.AreEqual("Jersey", animalGroup.Breed);
        //    Assert.AreEqual(0, animalGroup.ConceptusWeight);
        //    Assert.AreEqual(0, animalGroup.DrySheepEquivs);
        //    Assert.AreEqual(50, animalGroup.FemaleNo);
        //    Assert.AreEqual(290, animalGroup.FemaleWeight);
        //    Assert.AreEqual("Jersey", animalGroup.Genotype.Name);
        //    Assert.AreEqual(1, animalGroup.IntakeModifier);
        //    Assert.AreEqual(0, animalGroup.Lactation);
        //    Assert.AreEqual(290, animalGroup.LiveWeight);
        //    Assert.AreEqual(0, animalGroup.MaleNo);
        //    Assert.AreEqual(0, animalGroup.MaleWeight);
        //    Assert.AreEqual("Friesian", animalGroup.MatedTo.Name);
        //    Assert.AreEqual(0, animalGroup.MaxMilkYield);
        //    Assert.AreEqual(300, animalGroup.MaxPrevWeight);
        //    Assert.IsNull(animalGroup.MotherGroup);
        //    Assert.AreEqual(50, animalGroup.NoAnimals);
        //    Assert.AreEqual(0, animalGroup.NoFoetuses);
        //    Assert.AreEqual(0, animalGroup.NoOffspring);
        //    Assert.AreEqual(0, animalGroup.PaddSteep);
        //    Assert.AreEqual(-26.97964272287771, animalGroup.PotIntake);
        //    Assert.AreEqual(0.25968483457802222, animalGroup.RelativeSize);
        //    Assert.AreEqual(GrazType.ReproType.Empty, animalGroup.ReproState);
        //    Assert.AreEqual(400, animalGroup.StdReferenceWt);
        //    Assert.AreEqual(0, animalGroup.SupptFW_Intake);
        //    Assert.IsFalse(animalGroup.UreaWarning);
        //    Assert.AreEqual(0, animalGroup.WaterLogging);
        //    Assert.IsNotNull(animalGroup.Weather);
        //    Assert.AreEqual(0, animalGroup.WeightChange);
        //    Assert.IsNull(animalGroup.Young);
        //}
    }
}
