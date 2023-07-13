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
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Leaf",
                                DigestibilityString = "0.7",
                                UseDigestibilityFromModel = false,
                                FractionConsumable = 1,
                                MinimumAmount = 100,  // kg/ha
                                IsLive = true
                            },
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Stem",
                                DigestibilityString = "0.3",
                                UseDigestibilityFromModel = false,
                                FractionConsumable = 0.5,
                                MinimumAmount = 50,   // kg/ha
                                IsLive = false
                            },
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Stolon",
                                DigestibilityString = "0.6",
                                UseDigestibilityFromModel = false,
                                FractionConsumable = 1.0,
                                MinimumAmount = 100,   // kg/ha
                                IsLive = true
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
            Assert.AreEqual(1, forageModels.Count);

            var forageMaterial = forageModels[0].Material.ToList();
            Assert.AreEqual(3, forageMaterial.Count);

            // leaf live
            Assert.AreEqual(200, forageMaterial[0].Total.Wt);       // 200 (StructuralWt)
            Assert.AreEqual(190, forageMaterial[0].Consumable.Wt);  // 200 (StructuralWt) * 1 (FractionConsumable) - 10 (MinimumAmount)
            Assert.IsTrue(forageMaterial[0].IsLive);
            Assert.AreEqual(0.7, forageMaterial[0].Digestibility);
            Assert.AreEqual("Crop1.Leaf", forageMaterial[0].Name);

            // stem dead
            Assert.AreEqual(100, forageMaterial[1].Total.Wt);      // 100 (StructuralWt)
            Assert.AreEqual(45, forageMaterial[1].Consumable.Wt);  // 100 (StructuralWt) * 0.5 (FractionConsumable) - 5 (MinimumAmount)
            Assert.IsFalse(forageMaterial[1].IsLive);
            Assert.AreEqual(0.3, forageMaterial[1].Digestibility);
            Assert.AreEqual("Crop1.Stem", forageMaterial[1].Name);

            // stolon live
            Assert.AreEqual(5, forageMaterial[2].Total.Wt);        // 5 (StructuralWt)
            Assert.AreEqual(0, forageMaterial[2].Consumable.Wt);    // 5 (StructuralWt) * 1 (FractionConsumable) - 10 (MinimumAmount)
            Assert.IsTrue(forageMaterial[2].IsLive);
            Assert.AreEqual(0.6, forageMaterial[2].Digestibility);
            Assert.AreEqual("Crop1.Stolon", forageMaterial[2].Name);
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
                                DigestibilityString = "[Test].A + [Test].B",
                                UseDigestibilityFromModel = false,
                                FractionConsumable = 1,
                                MinimumAmount = 100, // kg/ha
                                IsLive = true
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
            Assert.AreEqual(0.8, digestibileMaterial.Digestibility, 0.00001);

            test.OnStartOfDay(null, null);
            Assert.AreEqual(0.6, digestibileMaterial.Digestibility, 0.00001);

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
                                DigestibilityString = null,
                                UseDigestibilityFromModel = true,
                                FractionConsumable = 1,
                                MinimumAmount = 100, // kg/ha
                                IsLive = true
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
            Assert.AreEqual(0.1, forageMaterial[0].Digestibility);
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
                                DigestibilityString = "0.4",
                                UseDigestibilityFromModel = false,
                                FractionConsumable = 1,
                                MinimumAmount = 100, // kg/ha
                                IsLive = true
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
            Assert.AreEqual(0.4, forageMaterial[0].Digestibility);
        }
    }
}
