using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Core;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using NUnit.Framework;
using UnitTests.Weather;

namespace UnitTests.Factorial
{
    [TestFixture]
    public class FactorTests
    {
        /// <summary>
        /// Ensure that an unbounded "step" factor causes an exception. This
        /// reproduces bug #7019.
        /// 
        /// https://github.com/APSIMInitiative/ApsimX/issues/7019
        /// </summary>
        /// <remarks>
        /// This previously resulted in an infinite loop, so I've set ta 1s
        /// timeout on the test method.
        /// </remarks>
        [TestCase("x = 0 to 10 step -1")]
        [TestCase("x = 0 to -10 step 1")]
        [CancelAfter(1_000)]
        public void TestUnboundedStep(string spec)
        {
            Factor factor = new Factor();
            factor.Specification = spec;
            Assert.Throws<InvalidOperationException>(() => factor.GetCompositeFactors());
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public void TestCanGetReferencedFiles()
        {
            Simulations simulations = new Simulations()
            {
                FileName = Path.GetTempFileName(),
                Children = new List<IModel>()
                {
                    new Experiment()
                    {
                        Name = "Experiment",
                        Children = new List<IModel>()
                        {
                            new Factors()
                            {
                                Name = "Factors",
                                Children = new List<IModel>()
                                {
                                    new Factor()
                                    {
                                        Name = "Factor",
                                        Children = new List<IModel>()
                                        {
                                            new CompositeFactor("CompositeFactor", "[Weather].FileName", "File.met")
                                        }
                                    },
                                }
                            },
                            new Simulation()
                            {
                                Name = "Simulation",
                                Children = new List<IModel>()
                                {
                                    new Clock()
                                    {
                                        StartDate = new DateTime(2019, 1, 1),
                                        EndDate = new DateTime(2019, 1, 2)
                                    },
                                    new MockSummary(),
                                    new MockWeather()
                                }
                            }
                        }
                    }
                }
            };

            simulations.Node = Node.Create(simulations);
            Assert.DoesNotThrow(() => simulations.FindAllReferencedFiles());
        }
    }
}
