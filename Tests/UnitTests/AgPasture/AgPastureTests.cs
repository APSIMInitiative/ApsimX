using APSIM.Core;
using Models.AgPasture;
using Models.Core;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Soils.Nutrients;
using NUnit.Framework;
using Models.WaterModel;

namespace UnitTests
{
    [TestFixture]
    class AgPastureTests
    {

        /// <summary>Ensure no NaNs are produced when bucket is closed ie. DUL = LL.</summary>
        [Test]
        public void EnsureNoNaNWhenDULEqualsLL()
        {
            var zone = new Zone()
            {
                Children =
                [
                    new MockSummary(),
                    new Soil()
                    {
                        Children = [
                            new Physical()
                            {
                                Thickness = [100, 100, 100 ],
                                BD = [ 1.0, 1.0, 1.0 ],
                                LL15 = [ 0.2, 0.2, 0.4 ],
                                DUL = [0.4, 0.4, 0.4],
                                Children = [
                                    new SoilCrop() { Name = "AGPRyegrassSoil", LL = [0.2, 0.2, 0.4 ], KL = [ 0.06, 0.06, 0.06 ], XF = [1, 1, 1] },
                                ]
                            },
                            new WaterBalance(),
                            new Nutrient(),
                            new Solute() { Name = "NO3", InitialValues = [1, 2, 3 ]},
                            new Solute() { Name = "NH4", InitialValues = [4, 5, 6 ]}
                        ]
                    },
                    new PastureSpecies()
                    {
                        Name = "AGPRyegrass",
                        Children = [
                            new PastureBelowGroundOrgan()
                            {
                                Children = [
                                    new RootTissue() { Name = "Live" },
                                    new RootTissue() { Name = "Dead" }
                                ]
                            }
                        ]
                    }
                ]
            };
            // set up the simulation and all models.
            Node.Create(zone);
            var links = new Links();
            links.Resolve(zone, true);

            // get instances.
            var root = zone.Node.FindChild<PastureBelowGroundOrgan>(recurse: true);

            // set some properties in root.
            root.Depth = 300;

            root.GetType().GetProperty("BottomLayer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                          .SetValue(root, 2);

            root.Initialise(zone, 100);
            ZoneWaterAndN zoneWaterAndN = new(zone)
            {
                Water = [30, 30, 30],
                NO3N = [25, 24, 23],
                NH4N = [2, 2, 2]
            };

            double[] waterUptake = [ 0.1, 0.1, 0 ];
            root.EvaluateSoilNitrogenAvailability(zoneWaterAndN, waterUptake);

            Assert.That(root.mySoilNO3Available[0], Is.GreaterThan(0));
            Assert.That(root.mySoilNO3Available[1], Is.GreaterThan(0));
            Assert.That(root.mySoilNO3Available[2], Is.EqualTo(0));    // no uptake because bucket closed.
            Assert.That(root.mySoilNH4Available[0], Is.GreaterThan(0));
            Assert.That(root.mySoilNH4Available[1], Is.GreaterThan(0));
            Assert.That(root.mySoilNH4Available[2], Is.EqualTo(0));    // no uptake because bucket closed.
        }

        /// <summary>Ensure NO3 and NH4 uptake is zero when KL = 0.</summary>
        [Test]
        public void EnsureNO3NH4UptakeZeroWhenKLIsZero()
        {
            var zone = new Zone()
            {
                Children =
                [
                    new MockSummary(),
                    new Soil()
                    {
                        Children = [
                            new Physical()
                            {
                                Thickness = [100, 100, 100 ],
                                BD = [ 1.0, 1.0, 1.0 ],
                                LL15 = [ 0.2, 0.2, 0.2 ],
                                DUL = [0.4, 0.4, 0.4],
                                Children = [
                                    new SoilCrop() { Name = "AGPRyegrassSoil", LL = [0.2, 0.2, 0.2 ], KL = [ 0.06, 0.06, 0 ], XF = [1, 1, 1] },
                                ]
                            },
                            new WaterBalance(),
                            new Nutrient(),
                            new Solute() { Name = "NO3", InitialValues = [1, 2, 3 ]},
                            new Solute() { Name = "NH4", InitialValues = [4, 5, 6 ]}
                        ]
                    },
                    new PastureSpecies()
                    {
                        Name = "AGPRyegrass",
                        Children = [
                            new PastureBelowGroundOrgan()
                            {
                                Children = [
                                    new RootTissue() { Name = "Live" },
                                    new RootTissue() { Name = "Dead" }
                                ]
                            }
                        ]
                    }
                ]
            };
            // set up the simulation and all models.
            Node.Create(zone);
            var links = new Links();
            links.Resolve(zone, true);

            // get instances.
            var root = zone.Node.FindChild<PastureBelowGroundOrgan>(recurse: true);

            // set some properties in root.
            root.Depth = 300;

            root.GetType().GetProperty("BottomLayer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                          .SetValue(root, 2);

            root.Initialise(zone, 100);
            ZoneWaterAndN zoneWaterAndN = new(zone)
            {
                Water = [30, 30, 30],
                NO3N = [25, 24, 23],
                NH4N = [ 2, 2, 2 ]
            };
            double[] waterUptake = [ 0.1, 0.1, 0 ];
            root.EvaluateSoilNitrogenAvailability(zoneWaterAndN, waterUptake);

            Assert.That(root.mySoilNO3Available[0], Is.GreaterThan(0));
            Assert.That(root.mySoilNO3Available[1], Is.GreaterThan(0));
            Assert.That(root.mySoilNO3Available[2], Is.EqualTo(0));    // no uptake because KL = 0
            Assert.That(root.mySoilNH4Available[0], Is.GreaterThan(0));
            Assert.That(root.mySoilNH4Available[1], Is.GreaterThan(0));
            Assert.That(root.mySoilNH4Available[2], Is.EqualTo(0));    // no uptake because KL = 0
        }
    }
}