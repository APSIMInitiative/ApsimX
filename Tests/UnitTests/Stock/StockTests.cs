namespace UnitTests.Stock
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Climate;
    using Models.Core;
    using Models.GrazPlan;
    using Models.Soils;
    using Models.StockManagement;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
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
            var animalParamSet = genotypes.Get("sheep");

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
            var friesian = genotypes.Get("Friesian");
            Assert.AreEqual(550,  friesian.BreedSRW, 550);
            Assert.AreEqual(0.05, friesian.SelfWeanPropn);
            Assert.IsTrue(friesian.bDairyBreed);
            Assert.AreEqual(new double[] { 0.85, 0.577, 0.9, 0.0 },       friesian.IntakeLactC);
            Assert.AreEqual(new double[] { 0.0, 0.0115, 0.27, 0.4, 1.1 }, friesian.GrowthC);
        }

        /// <summary>Ensure that a user supplied genotype overrides a standard one.</summary>
        [Test]
        public void EnsureUserGenotypeOverridesStandardGenotype()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var friesian = genotypes.Get("Friesian");

            // Clone the genotype and change it.
            friesian = Apsim.Clone(friesian) as Genotype;
            friesian.InitialiseWithParams(srw: 1);

            // Give it to the genotypes instance as a user genotype.
            genotypes.Add(friesian);

            // Now ask for friesian again. This time it should return the user genotype, not the standard one.
            friesian = genotypes.Get("Friesian");

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

            Assert.AreEqual(animalTypes.ToArray(), new string[] { "Cattle", "Goats", "Sheep" });
        }

        /// <summary>Ensure we can get a list of all genotype names for an animal type.</summary>
        [Test]
        public void GetGenotypeNamesForAnimalType()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var genotypeNames = genotypes.All.Where(genotype => genotype.AnimalType == "Cattle")
                                             .Select(genotype => genotype.Name);

            Assert.AreEqual(genotypeNames.ToArray().OrderBy(x => x), new string[] { "Angus", "Beef Shorthorn", "Hereford", "South Devon", "Ayrshire", "Brown Swiss",
                                                                    "Dairy Shorthorn", "Friesian", "Guernsey", "Holstein",  "Jersey",
                                                                    "British x Brahman", "British x Charolais", "British x Friesian", "British x Holstein",
                                                                    "Charolais x Friesian", "Charolais x Holstein", "Charolais", "Chianina",
                                                                    "Limousin", "Simmental", "Brahman", "Ujimqin Cattle",
                                                                    "Ujimqin x Angus (1st cross)", "Ujimqin x Angus (2nd cross)",
                                                                    "Ujimqin x Charolais (1st cross)", "Ujimqin x Charolais (2nd cross)"}.OrderBy(x => x));

        }

        /// <summary>Ensure we can create an animal cross genotype.</summary>
        [Test]
        public void CreateAnimalCross()
        {
            var stock = new Stock();
            var genotypeCross = new GenotypeCross()
            { 
                Name = "NewGenotype",
                DamBreed = "Friesian",
                Generation = 1,
                SireBreed = "Jersey",
            };

            // Inject the stock link into genotype cross.
            Utilities.InjectLink(genotypeCross, "stock", stock);

            // Call the StartOfSimulation event in genotype cross.
            Utilities.CallEvent(genotypeCross, "StartOfSimulation");

            // Get a friesian genotype.
            var animalParamSet = stock.Genotypes.Get("NewGenotype");

            // Make sure we can retrieve the new genotype.
            Assert.IsNotNull(animalParamSet);

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
            var animalParamSet = stock.Genotypes.Get("NZFriesianCross");

            Assert.AreEqual("NZFriesianCross", animalParamSet.Name);
            Assert.IsTrue(animalParamSet.bDairyBreed);
            Assert.AreEqual(550, animalParamSet.BreedSRW);
            Assert.AreEqual(27.5, animalParamSet.PeakMilk);
            Assert.AreEqual(new double[] { 0, 0 }, animalParamSet.ConceiveSigs[0]);
            Assert.AreEqual(new double[] { 10, 5.89 }, animalParamSet.ConceiveSigs[1]);
            Assert.AreEqual(new double[] { 10, 5.89 }, animalParamSet.ConceiveSigs[2]);
            Assert.AreEqual(new double[] { 0, 0 }, animalParamSet.ConceiveSigs[3]);
            Assert.AreEqual(1, animalParamSet.FleeceYield);
            Assert.AreEqual(new double[] { 0, 0.00061074716558540132, 5.53E-05 }, animalParamSet.MortRate);
        }

        /// <summary>Ensure a user can add an animal group to STOCK.</summary>
        [Test]
        public void AddAnimalGroupByDroppingOntoStock()
        {
            // Get a friesian genotype.
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock(),
                    new Weather(),
                    new MockSummary(),
                    new Zone()
                    {
                        Name = "Field1",
                        Area = 100
                    },
                    new Soil()
                    { 
                        Children = new List<IModel>()
                        { 
                            new Physical()
                            {
                                Thickness = new double[] { 100, 100, 100 }
                            }
                        }
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Zone",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290
                    }
                }
            };

            Utilities.ResolveLinks(stock);

            // Invoke start of simulation event. This should add the animal group to stock.
            var animals = stock.Children[5] as Animals;
            Utilities.CallEvent(stock, "StartOfSimulation");
            Utilities.CallEvent(animals, "StartOfSimulation");

            // Get the animal group
            var animalGroup = stock.StockModel.Animals[1];

            Assert.AreEqual(100, animalGroup.AgeDays);
            Assert.AreEqual(GrazType.AgeType.Weaner, animalGroup.AgeClass);
            Assert.AreEqual(GrazType.AnimalType.Cattle, animalGroup.Animal);
            Assert.AreEqual(0, animalGroup.AnimalsPerHa);  // I would not expect zero here.
            Assert.AreEqual(2.791845743237555, animalGroup.BirthCondition);
            Assert.AreEqual(290, animalGroup.BaseWeight);
            Assert.AreEqual(2.791845743237555, animalGroup.BodyCondition);
            Assert.AreEqual(0, animalGroup.ConceptusWeight);
            Assert.AreEqual(0, animalGroup.DrySheepEquivs);
            Assert.AreEqual(50, animalGroup.FemaleNo);
            Assert.AreEqual(290, animalGroup.FemaleWeight);
            Assert.AreEqual("Jersey", animalGroup.Genotype.Name);
            Assert.AreEqual(1, animalGroup.IntakeModifier);
            Assert.AreEqual(0, animalGroup.Lactation);
            Assert.AreEqual(290, animalGroup.LiveWeight);
            Assert.AreEqual(0, animalGroup.MaleNo);
            Assert.AreEqual(0, animalGroup.MaleWeight);
            Assert.AreEqual("Friesian", animalGroup.MatedTo.Name);
            Assert.AreEqual(0, animalGroup.MaxMilkYield);
            Assert.AreEqual(300, animalGroup.MaxPrevWeight);
            Assert.AreEqual(50, animalGroup.NoAnimals);
            Assert.AreEqual(0, animalGroup.NoFoetuses);
            Assert.AreEqual(0, animalGroup.NoOffspring);
            Assert.AreEqual(1, animalGroup.PaddSteep);
            Assert.AreEqual(-26.97964272287771, animalGroup.PotIntake);
            Assert.AreEqual(0.25968483457802222, animalGroup.RelativeSize);
            Assert.AreEqual(GrazType.ReproType.Empty, animalGroup.ReproState);
            Assert.AreEqual(400, animalGroup.StdReferenceWt);
            Assert.AreEqual(0, animalGroup.WaterLogging);
            Assert.AreEqual(0, animalGroup.WeightChange);
            Assert.IsNull(animalGroup.Young);
        }

        /// <summary>Ensure a user can add a fixed grazing component to a simulation.</summary>
        [Test]
        public void FixedGrazingEffectsOnStock()
        {
            // Get a friesian genotype.
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(1980, 1, 1),
                        EndDate = new DateTime(1980, 1, 2)
                    },
                    new ControlledEnvironment(),
                    new MockSummary(),
                    new Zone()
                    {
                        Name = "Field1",
                        Area = 100
                    },
                    new Zone()
                    {
                        Name = "Field2",
                        Area = 50
                    },
                    new Soil()
                    {
                        Children = new List<IModel>()
                        {
                            new Physical() { Thickness = new double[] { 100, 100, 100 } }
                        }
                    },
                    new Animals()
                    {
                        Name = "MyGroup1",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290,
                        Tag = 1
                    },
                    new Draft()
                    {
                        StartDate = "1-Jan",
                        EndDate = " 10-Jan",
                        TypeOfDraft = Draft.DraftType.Fixed,
                        TagNumbers = new int[] { 1 },
                        PaddockNames = new string[] { "Field2" }
                    }
                }
            };

            Utilities.ResolveLinks(stock);

            var animals = stock.Children[6] as Animals;
            var grazing = stock.Children[7] as Draft;

            // Invoke start of simulation event. This should add the animal group to stock.
            Utilities.CallEvent(stock, "StartOfSimulation");
            Utilities.CallEvent(animals, "StartOfSimulation");

            // Day 1
            Utilities.CallEvent(stock, "DoStock");
            Utilities.CallEvent(grazing, "DoManagement");

            // The animal group should have moved to field 2.
            var animalGroup = stock.StockModel.Animals[1];

            Assert.AreEqual("Field2", animalGroup.PaddOccupied.Name);
        }

        /// <summary>Ensure a user can add a draft (flexible) grazing component to a simulation.</summary>
        [Test]
        public void DraftGrazingEffectsOnStock()
        {
            // Get a friesian genotype.
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(1980, 1, 1),
                        EndDate = new DateTime(1980, 1, 2)
                    },
                    new ControlledEnvironment(),
                    new MockSummary(),
                    new Zone()
                    {
                        Name = "Field1",
                        Area = 100,
                        Children = new List<IModel>() { new MockForage(300) }
                    },
                    new Zone()
                    {
                        Name = "Field2",
                        Area = 50,
                        Children = new List<IModel>() { new MockForage(100) }
                    },
                    new Zone()
                    {
                        Name = "Field3",
                        Area = 150,
                        Children = new List<IModel>() { new MockForage(500) }
                    },
                    new Zone()
                    {
                        Name = "Field4",
                        Area = 75,
                        Children = new List<IModel>() { new MockForage(500) }
                    },                    new Soil()
                    {
                        Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } }
                    },
                    new Animals()
                    {
                        Name = "MyGroup1",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290,
                        Tag = 1,
                    },
                    new Animals()
                    {
                        Name = "MyGroup2",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Friesian",
                        MatedTo = "Friesian",
                        Paddock = "Field2",
                        Number = 50,
                        AgeDays = 100,
                        Weight = 500,
                        Tag = 2,
                    },
                    new Animals()
                    {
                        Name = "MyGroup3",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Friesian",
                        MatedTo = "Friesian",
                        Paddock = "Field2",
                        Number = 50,
                        AgeDays = 100,
                        Weight = 500,
                        Tag = 3,
                    },
                    new Draft()
                    {
                        StartDate = "1-Jan",
                        EndDate = " 10-Jan",
                        TypeOfDraft = Draft.DraftType.Flexible,
                        CheckEvery = 1,
                        TagNumberPriority1 = new int[] { 2 },
                        TagNumberPriority2 = new int[] { 3 },
                        PaddockNames = new string[] { "Field2", "Field3", "Field4" }
                    }
                }
            };

            Utilities.ResolveLinks(stock);

            var animals1 = stock.Children[8] as Animals;
            var animals2 = stock.Children[9] as Animals;
            var animals3 = stock.Children[10] as Animals;
            var grazing = stock.Children[11] as Draft;

            // Invoke start of simulation event. This should add the animal group to stock.
            Utilities.CallEvent(stock, "StartOfSimulation");
            Utilities.CallEvent(animals1, "StartOfSimulation");
            Utilities.CallEvent(animals2, "StartOfSimulation");
            Utilities.CallEvent(animals3, "StartOfSimulation");

            // Day 1
            Utilities.CallEvent(stock, "DoStock");
            Utilities.CallEvent(grazing, "DoManagement");

            // The animal group 2 should have moved to field 3 because group 2 is the highest priority and field 3
            // has the most forage available.
            var animalGroup1 = stock.StockModel.Animals[1];
            var animalGroup2 = stock.StockModel.Animals[2];
            var animalGroup3 = stock.StockModel.Animals[3];
            Assert.AreEqual("Field1", animalGroup1.PaddOccupied.Name);
            Assert.AreEqual("Field3", animalGroup2.PaddOccupied.Name);
            Assert.AreEqual("Field4", animalGroup3.PaddOccupied.Name);
        }

        /// <summary>Ensure a user can call Stock.Add method creating a cohort of animals.</summary>
        [Test]
        public void AddAnimalCohort()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone()
                    {
                        Name = "Field1",
                        Area = 100
                    },
                    new Soil()
                    {
                        Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } }
                    },
                }
            };
            Utilities.ResolveLinks(stock);
            var clock = stock.Children[0] as Clock;

            // Invoke start of simulation event. This should add the animal group to stock.
            Utilities.CallEvent(stock, "StartOfSimulation");
            Utilities.CallEvent(clock, "StartOfSimulation");

            // Add the animal group
            stock.Add(
                new StockAdd()
                {
                    Genotype = "Small Merino",
                    Number = 10,
                    MinYears = 1,
                    MaxYears = 3,
                    CondScore = 0,   // default condition score.
                    BirthDay = 200,  // doy
                    MatedTo = "Small Merino",
                    MeanWeight = 300,
                    Offspring = 2,
                    Sex = ReproductiveType.Female,
                    Pregnant = 20,
                    Foetuses = 2,
                    MeanFleeceWt = 10,
                    ShearDay = 200,
                    YoungCondScore = 1.98,
                    YoungWt = 40
                });
            Assert.AreEqual(7, stock.StockModel.Animals.Count);  // stock.AnimalList.Animals[0] is a temporary animal group.

            var json = ReflectionUtilities.JsonSerialise(stock.StockModel.Animals, false);
            var expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Stock.AddAnimalCohort.json");

            Assert.AreEqual(expectedJson, json);
        }

        /// <summary>Ensure a user can sell animals by tag.</summary>
        [Test]
        public void SellTaggedAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Zone",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Zone",
                        MaxPrevWt = 700,
                        Number = 150,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Sell animals that are tagged 2
            var tag2Animals = stock.AnimalGroups.Last();
            stock.Sell(50, tag2Animals);

            var groups = stock.AnimalGroups;
            Assert.AreEqual(50, groups.First().NoAnimals);
            Assert.AreEqual(100, groups.Last().NoAnimals);

            // Make sure summary file was written to.
            Assert.AreEqual("Sold 50 animals", MockSummary.messages[0]);
        }

        /// <summary>Ensure a user can sell animals by weight.</summary>
        [Test]
        public void SellHeavyAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Zone",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Male,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Zone",
                        MaxPrevWt = 700,
                        Number = 150,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Sell males
            var males = stock.AnimalGroups.First(group => group.ReproState == GrazType.ReproType.Male);
            stock.Sell(50, males);

            var groups = stock.AnimalGroups;
            Assert.AreEqual(50, groups.First().NoAnimals);
            Assert.AreEqual(100, groups.Last().NoAnimals);

            // Make sure summary file was written to.
            Assert.AreEqual("Sold 50 animals", MockSummary.messages[0]);
        }

        /// <summary>Ensure a user can shear animals.</summary>
        [Test]
        public void ShearAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Zone",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Male,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Zone",
                        MaxPrevWt = 700,
                        Number = 150,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Shear all animals
            var cfw = stock.Shear(true, true);
            Assert.AreEqual(70, cfw);

            // Make sure summary file was written to.
            Assert.AreEqual("Shearing animals", MockSummary.messages[0]);
        }

        /// <summary>Ensure a user can move animals between paddocks.</summary>
        [Test]
        public void MoveAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Field1",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 100,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Male,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Move the cattle into field 2
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);
            stock.Move("Field2", cattle);
            Assert.AreEqual("Field2", cattle.PaddOccupied.Name);
        }

        /// <summary>Ensure a user can mate animals.</summary>
        [Test]
        public void JoinAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Field1",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 200,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Male,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Mate the ewes.
            var ewes = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Sheep);
            stock.Join("Small Merino", 30, ewes);

            List<AnimalGroup> newGroups = new List<AnimalGroup>();
            for (int i = 0; i < 10; i++)
                ewes.Age(1, ref newGroups);

            // Ewes should now be pregnant.
            Assert.AreEqual(3, newGroups.Count, 3);
            Assert.AreEqual(1, newGroups[2].Pregnancy);
        }

        /// <summary>Ensure a user can castrate animals.</summary>
        [Test]
        public void CastrateAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Field1",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 200,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 1,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        YoungWt = 100,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Castrate the cattle young.
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);

            stock.Castrate(100, cattle);

            // Young should now be castrated.
            Assert.AreEqual(GrazType.ReproType.Castrated, cattle.Young.ReproState);
        }

        /// <summary>Ensure a user can wean animals.</summary>
        [Test]
        public void WeanAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Field1",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 200,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 1,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        YoungWt = 100,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Wean the cattle young.
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);

            stock.Wean(150, weanMales:true, weanFemales:true, group: cattle);

            // Young should now be weaned
            Assert.IsNull(cattle.Young);
            Assert.AreEqual(4, stock.AnimalGroups.Count());
        }

        /// <summary>Ensure a user can dryoff animals.</summary>
        [Test]
        public void DryoffAnimals()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Field1",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 200,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 1
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 0,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Dryoff the cattle young.
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);
            stock.DryOff(150, cattle);

            // Young should now be weaned
            Assert.AreEqual(0, cattle.Lactation);
        }

        /// <summary>Ensure a user can split an animal group by age.</summary>
        [Test]
        public void SplitByAge()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 0,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Split the cattle group.
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);
            var newGroups = stock.SplitByAge(100, cattle);

            // One new group should have been created.
            Assert.AreEqual(1, newGroups.Count());

            // There should now be a second group with all 150 females in it.
            Assert.AreEqual(2, stock.AnimalGroups.Count());
            Assert.AreEqual(0, stock.AnimalGroups.First().FemaleNo);
            Assert.AreEqual(150, stock.AnimalGroups.Last().FemaleNo);
        }

        /// <summary>Ensure a user can split an animal group by weight.</summary>
        [Test]
        public void SplitByWeight()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 0,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Split the cattle group.
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);
            var newGroups = stock.SplitByWeight(500, cattle);

            // One new group should have been created.
            Assert.AreEqual(1, newGroups.Count());

            // There should now be a second group with 7 of the females in it.
            Assert.AreEqual(2, stock.AnimalGroups.Count());
            Assert.AreEqual(143, stock.AnimalGroups.First().FemaleNo);
            Assert.AreEqual(7, stock.AnimalGroups.Last().FemaleNo);
        }

        /// <summary>Ensure a user can split an animal group by young animals.</summary>
        [Test]
        public void SplitByYoung()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Friesian",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 1,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 2
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Split the cattle group.
            var cattle = stock.AnimalGroups.First(group => group.Genotype.Animal == GrazType.AnimalType.Cattle);
            var newGroups = stock.SplitByYoung(cattle);

            // One new group should have been created.
            Assert.AreEqual(1, newGroups.Count());

            // There should now be a second group with 7 of the females in it.
            Assert.AreEqual(2, stock.AnimalGroups.Count());
            Assert.AreEqual(75, stock.AnimalGroups.First().FemaleNo);
            Assert.AreEqual(75, stock.AnimalGroups.Last().FemaleNo);
        }

        /// <summary>Ensure a user can sort animals by tag.</summary>
        [Test]
        public void SortAnimalsByTag()
        {
            var stock = new Stock
            {
                Children = new List<IModel>()
                {
                    new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 2) },
                    new Weather(),
                    new MockSummary(),
                    new Zone() { Name = "Field1", Area = 100 },
                    new Zone() { Name = "Field2", Area = 100 },
                    new Soil() { Children = new List<IModel>() { new Physical() { Thickness = new double[] { 100, 100, 100 } } } },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Small Merino",
                        MatedTo = "Small Merino",
                        Paddock = "Field1",
                        MaxPrevWt = 300,
                        Number = 50,
                        AgeDays = 200,
                        Weight = 290,
                        FleeceWt = 100,
                        Tag = 2
                    },
                    new Animals()
                    {
                        Name = "MyGroup",
                        Sex = GrazType.ReproType.Empty,
                        Genotype = "Jersey",
                        MatedTo = "Friesian",
                        Paddock = "Field1",
                        MaxPrevWt = 700,
                        Number = 150,
                        NumSuckling = 0,
                        Lactating = 160,
                        AgeDays = 500,
                        Weight = 600,
                        Tag = 1
                    },
                },
            };
            Utilities.ResolveLinks(stock);
            Utilities.CallEventAll(stock, "StartOfSimulation");

            // Sort the animals.
            stock.Sort();

            // Young should now be weaned
            Assert.AreEqual(1, stock.AnimalGroups.First().Tag);
            Assert.AreEqual(2, stock.AnimalGroups.Last().Tag);
        }
    }
}