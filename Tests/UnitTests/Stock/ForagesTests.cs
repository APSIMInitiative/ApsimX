namespace UnitTests.Stock
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Climate;
    using Models.Core;
    using Models.GrazPlan;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.StockManagement;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class ForagesTests
    {
        /// <summary>Make sure parameters with all values and some values missing work.</summary>
        [Test]
        public void ForagesUseDefaults()
        {
            var simulation = new Simulation
            {
                Children = new List<IModel>()
                {
                    new MockSummary(),
                    new Forages()
                    {
                        Parameters = new List<ForageParameters>
                        {
                            new ForageParameters()
                            {
                                Name = "Crop1",
                                Material = new List<ForageParameters.ForageMaterialParameter>()
                                {
                                    new ForageParameters.ForageMaterialParameter()
                                    {
                                        Name = "Leaf",
                                        DigestibilityLiveString = "0.7",
                                        DigestibilityDeadString = "0.3",
                                        FractionConsumableLive = 100,
                                        FractionConsumableDead = 100
                                    }
                                }
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
                                Organs = new List<IOrganDamage>() 
                                { 
                                    new MockOrgan("Leaf", 1000)
                                }
                            }
                        }
                    },
                    new Zone()
                    {
                        Children = new List<IModel>()
                        {
                            new MockForage()
                            {
                                Name = "Crop2",
                                Organs = new List<IOrganDamage>()
                                {
                                    new MockOrgan("Leaf", 2000)
                                }
                            }
                        }
                    }
                }
            };

            Utilities.ResolveLinks(simulation);

            var forages = simulation.FindChild<Forages>();
            var zones = simulation.FindAllChildren<Zone>().ToList();

            var foragesInZone1 = forages.GetForages(zones[0]).ToList();
            Assert.AreEqual(1, foragesInZone1.Count);
            Assert.AreEqual(1, foragesInZone1[0].Material.Count());
        }
    }
}