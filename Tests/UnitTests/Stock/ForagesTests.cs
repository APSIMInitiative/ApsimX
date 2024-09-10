using Models.Core;
using Models.ForageDigestibility;
using Models.PMF;
using Models.PMF.Interfaces;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
namespace UnitTests.Stock
{
    [TestFixture]
    public class ForagesTests
    {
        /// <summary>Make sure damageable biomasses are correctly matched to forage parameters.</summary>
        [Test]
        public void MatchBiomassToParameters()
        {
            var simulation = new Simulation
            {
                Children = new List<IModel>()
                {
                    new MockSummary(),
                    new Forages()
                    {
                        Parameters = new List<ForageMaterialParameters>
                        {
                            new()
                            {
                                Name = "Crop1.Leaf",
                                LiveDigestibility = "0.7",
                                LiveFractionConsumable = 1,
                                LiveMinimumBiomass = 100,  // kg/ha
                            },
                            new()
                            {
                                Name = "Crop1.Stem",
                                DeadDigestibility = "0.3",
                                DeadFractionConsumable = 0.5,
                                DeadMinimumBiomass = 50,   // kg/ha
                            },
                            new()
                            {
                                Name = "Crop1.Stolon",
                                LiveDigestibility = "0.6",
                                LiveFractionConsumable = 1.0,
                                LiveMinimumBiomass = 100,   // kg/ha
                            }
                        }
                    },
                    new Zone()
                    {
                        Children = new List<IModel>()
                        {
                            new MockForage()
                            {
                                Name = "Crop1",
                                Material = new List<DamageableBiomass>()
                                {
                                    new DamageableBiomass("Crop1.Leaf", new Biomass() {StructuralWt = 200 }, isLive: true),   // g/m2
                                    new DamageableBiomass("Crop1.Stem", new Biomass() {StructuralWt = 100 }, isLive: false),  // g/m2
                                    new DamageableBiomass("Crop1.Stolon", new Biomass() {StructuralWt = 5 }, isLive: true)    // g/m2
                                }
                            }
                        }
                    }
                }
            };

            Utilities.ResolveLinks(simulation);

            var forages = simulation.FindChild<Forages>();

            var forageModels = forages.ModelsWithDigestibleBiomass.ToList();
            Assert.That(forageModels.Count, Is.EqualTo(1));

            var forageMaterial = forageModels[0].Material.ToList();
            Assert.That(forageMaterial.Count, Is.EqualTo(3));

            // leaf live
            Assert.That(forageMaterial[0].Total.Wt, Is.EqualTo(200));       // 200 (StructuralWt)
            Assert.That(forageMaterial[0].Consumable.Wt, Is.EqualTo(190));  // 200 (StructuralWt) * 1 (FractionConsumable) - 10 (MinimumAmount)
            Assert.That(forageMaterial[0].IsLive, Is.True);
            Assert.That(forages.GetDigestibility(forageMaterial[0]), Is.EqualTo(0.7));
            Assert.That(forageMaterial[0].Name, Is.EqualTo("Crop1.Leaf"));

            // stem dead
            Assert.That(forageMaterial[1].Total.Wt, Is.EqualTo(100));      // 100 (StructuralWt)
            Assert.That(forageMaterial[1].Consumable.Wt, Is.EqualTo(45));  // 100 (StructuralWt) * 0.5 (FractionConsumable) - 5 (MinimumAmount)
            Assert.That(forageMaterial[1].IsLive, Is.False);
            Assert.That(forages.GetDigestibility(forageMaterial[1]), Is.EqualTo(0.3));
            Assert.That(forageMaterial[1].Name, Is.EqualTo("Crop1.Stem"));

            // stolon live
            Assert.That(forageMaterial[2].Total.Wt, Is.EqualTo(5));        // 5 (StructuralWt)
            Assert.That(forageMaterial[2].Consumable.Wt, Is.EqualTo(0));    // 5 (StructuralWt) * 1 (FractionConsumable) - 10 (MinimumAmount)
            Assert.That(forageMaterial[2].IsLive, Is.True);
            Assert.That(forages.GetDigestibility(forageMaterial[2]), Is.EqualTo(0.6));
            Assert.That(forageMaterial[2].Name, Is.EqualTo("Crop1.Stolon"));
        }

        /// <summary>Make sure digestibility can be expressed as a function.</summary>
        [Test]
        public void DigestibilityAsExpression()
        {
            var simulation = new Simulation
            {
                Children = new List<IModel>()
                {
                    new MockSummary(),
                    new Forages()
                    {
                        Parameters = new List<ForageMaterialParameters>
                        {
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Leaf",
                                LiveDigestibility = "[Test].A + [Test].B",
                                LiveFractionConsumable = 1,
                                LiveMinimumBiomass = 100, // kg/ha
                            }
                        }
                    },
                    new MockModelValuesChangeDaily(new double[] { 0.7, 0.6 }, new double[] {0.1, 0.0})
                    {
                        Name = "Test"
                    },
                    new Zone()
                    {
                        Children = new List<IModel>()
                        {
                            new MockForage()
                            {
                                Name = "Crop1",
                                Material = new List<DamageableBiomass>()
                                {
                                    new DamageableBiomass("Crop1.Leaf", new Biomass() {StructuralWt = 200 }, isLive: true),  // g/m2
                                }
                            }
                        }
                    }
                }
            };

            Utilities.ResolveLinks(simulation);

            var forages = simulation.FindChild<Forages>();
            var test = simulation.FindChild<MockModelValuesChangeDaily>("Test");

            var digestibileMaterial = forages.ModelsWithDigestibleBiomass.First().Material.First();

            test.OnStartOfDay(null, null);
            Assert.That(forages.GetDigestibility(digestibileMaterial), Is.EqualTo(0.8).Within(0.00001));

            test.OnStartOfDay(null, null);
            Assert.That(forages.GetDigestibility(digestibileMaterial), Is.EqualTo(0.6).Within(0.00001));

        }

        /// <summary>Make sure ForageParameters can use digestibility supplied by a a model.</summary>
        [Test]
        public void EnsureForageParametersCanUseInternalDigestibility()
        {
            var simulation = new Simulation
            {
                Children = new List<IModel>()
                {
                    new MockSummary(),
                    new Forages()
                    {
                        Parameters = new List<ForageMaterialParameters>
                        {
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Leaf",
                                LiveDigestibility = "FromModel",
                                LiveFractionConsumable = 1,
                                LiveMinimumBiomass = 100, // kg/ha
                            }
                        }
                    },
                    new Zone()
                    {
                        Children = new List<IModel>()
                        {
                            new MockForage()
                            {
                                Name = "Crop1",
                                Material = new List<DamageableBiomass>()
                                {
                                    new DamageableBiomass("Crop1.Leaf", new Biomass() {StructuralWt = 200 }, isLive: true, digestibility:0.1),   // g/m2
                                }
                            }
                        }
                    }
                }
            };

            Utilities.ResolveLinks(simulation);

            var forages = simulation.FindChild<Forages>();
            var forageModels = forages.ModelsWithDigestibleBiomass.ToList();
            var forageMaterial = forageModels[0].Material.ToList();
            Assert.That(forages.GetDigestibility(forageMaterial[0]), Is.EqualTo(0.1));
        }

        /// <summary>Make sure the ForageMaterialParameters can override a model that supplys digestibility numbers.</summary>
        [Test]
        public void EnsureForageParametersCanOverrideModelDigestibility()
        {
            var simulation = new Simulation
            {
                Children = new List<IModel>()
                {
                    new MockSummary(),
                    new Forages()
                    {
                        Parameters = new List<ForageMaterialParameters>
                        {
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Leaf",
                                LiveDigestibility = "0.4",
                                LiveFractionConsumable = 1,
                                LiveMinimumBiomass = 100, // kg/ha
                            }
                        }
                    },
                    new Zone()
                    {
                        Children = new List<IModel>()
                        {
                            new MockForage()
                            {
                                Name = "Crop1",
                                Material = new List<DamageableBiomass>()
                                {
                                    new DamageableBiomass("Crop1.Leaf", new Biomass() {StructuralWt = 200 }, isLive: true, digestibility:0.1),   // g/m2
                                }
                            }
                        }
                    }
                }
            };

            Utilities.ResolveLinks(simulation);

            var forages = simulation.FindChild<Forages>();
            var forageModels = forages.ModelsWithDigestibleBiomass.ToList();
            var forageMaterial = forageModels[0].Material.ToList();
            Assert.That(forages.GetDigestibility(forageMaterial[0]), Is.EqualTo(0.4));
        }
    }
}
