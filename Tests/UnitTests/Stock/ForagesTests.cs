namespace UnitTests.Stock
{
    using Models.Core;
    using Models.ForageDigestibility;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;

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
                                FractionConsumable = 1,
                                MinimumAmount = 100,
                                IsLive = true
                            },
                            new ForageMaterialParameters()
                            {
                                Name = "Crop1.Leaf",
                                DigestibilityString = "0.3",
                                FractionConsumable = 0.5,
                                MinimumAmount = 50,
                                IsLive = false
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
                                    new DamageableBiomass("Leaf", new Biomass() {StructuralWt = 200 }, isLive: true),  // g/m2
                                    new DamageableBiomass("Leaf", new Biomass() {StructuralWt = 100 }, isLive: false)  // g/m2
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
            Assert.AreEqual(2, forageMaterial.Count);

            // live
            Assert.AreEqual(1900, forageMaterial[0].Biomass.Wt);  // 2000 (StructuralWt) * 0 (FractionConsumable) - 100 (MinimumAmount)
            Assert.IsTrue(forageMaterial[0].IsLive);
            Assert.AreEqual(0.7, forageMaterial[0].Digestibility);
            Assert.AreEqual("Leaf", forageMaterial[0].Name);

            // dead
            Assert.AreEqual(450, forageMaterial[1].Biomass.Wt);  // 1000 (StructuralWt) * 0.5 (FractionConsumable) - 50 (MinimumAmount)
            Assert.IsFalse(forageMaterial[1].IsLive);
            Assert.AreEqual(0.3, forageMaterial[1].Digestibility);
            Assert.AreEqual("Leaf", forageMaterial[1].Name);
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
                                FractionConsumable = 1,
                                MinimumAmount = 100,
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
                                    new DamageableBiomass("Leaf", new Biomass() {StructuralWt = 200 }, isLive: true),  // g/m2
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
    }
}
