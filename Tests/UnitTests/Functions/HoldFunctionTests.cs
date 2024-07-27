using Models;
using Models.Core;
using Models.Functions;
using Models.PMF;
using Models.PMF.Phen;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
namespace UnitTests.Functions
{
    [TestFixture]
    class HoldFunctionTests
    {
        [Serializable]
        class MockFunctionThatThrows : Model, IFunction
        {
            public bool DoThrow { get; set; }
            public double Value(int arrayIndex = -1)
            {
                if (DoThrow)
                    throw new Exception("Intentional exception");
                else
                    return 1;
            }
        }

        [Serializable]
        class MockPhase : Model, IPhase
        {
            public string Start { get; set; }
            public string End { get; set; }

            public bool IsEmerged { get; } = true;

            public double FractionComplete { get; set; }

            public bool DoTimeStep(ref double PropOfDayToUse)
            {
                throw new NotImplementedException();
            }

            public void ResetPhase()
            {
            }

        }

        /// <summary>Ensure the hold function can deal with exceptions.</summary>
        [Test]
        public void HoldFunctionHandlesExceptions()
        {
            var f = new HoldFunction()
            {
                WhenToHold = "B",

                Children = new List<IModel>()
                {
                    new MockFunctionThatThrows() { Name = "ValueToHold" },
                    new Plant()
                    {
                        Children = new List<IModel>()
                        {
                            new Phenology()
                            {
                                Children = new List<IModel>()
                                {
                                    new Zone(),
                                    new Clock(),
                                    new MockSummary(),
                                    new MockFunctionThatThrows() { Name = "ThermalTime"},
                                    new MockPhase()
                                    {
                                        Name = "Phase1",
                                        Start = "A",
                                        End = "B"
                                    },
                                    new MockPhase()
                                    {
                                        Name = "Phase2",
                                        Start = "B",
                                        End = "C"
                                    }
                                }
                            },
                            new Constant()
                            {
                                Name = "MortalityRate",
                            }
                        }
                    }
                }
            };
            f.ParentAllDescendants();


            var links = new Links();
            links.Resolve(f, true);

            Utilities.CallEvent(f.Children[1].Children[0], "Commencing", new object[] { this, new EventArgs() });

            try
            {
                (f.Children[0] as MockFunctionThatThrows).DoThrow = true;
                Utilities.CallEvent(f, "Commencing", new object[] { this, new EventArgs() });
                Assert.Fail();
            }
            catch
            {
                Assert.That(f.Value(), Is.EqualTo(0));
            }
            

            (f.Children[0] as MockFunctionThatThrows).DoThrow = false;
            Utilities.CallEvent(f, "DoUpdate", new object[] { this, new EventArgs() });
            Assert.That(f.Value(), Is.EqualTo(1));
        }

    }
}
