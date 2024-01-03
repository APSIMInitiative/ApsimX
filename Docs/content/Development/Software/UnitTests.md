---
title: "Unit tests"
draft: false
---

In theory, all methods in all classes should have unit tests that test they work correctly. In practice, user interface and infrastructure code is better tested than model science code.

APSIM uses NUnit for all unit tests. This is integrated into Visual Studio via NuGet. The UnitTests project in the APSIM solution contains all tests. The folder structure in this project mimics the folder structure in the models and ApsimNG projects.

An example of a good test is in FertiliserTests.cs:

```c#
        /// <summary>Ensure the the apply method works with non zero depth.</summary>
        [Test]
        public void Fertiliser_EnsureApplyWorks()
        {
            // Create a tree with a root node for our models.
            var simulation = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(2015, 1, 1),
                        EndDate = new DateTime(2015, 1, 1)
                    },
                    new MockSummary(),
                    new MockSoil()
                    {
                        Thickness = new double[] { 100, 100, 100 },
                        NO3 = new double[] { 1, 2, 3 },
                        Children = new List<IModel>()
                        {
                            new MockSoilSolute("NO3"),
                            new MockSoilSolute("NH4"),
                            new MockSoilSolute("Urea")
                        }
                    },
                    new Fertiliser() { Name = "Fertilise" },
                    new Operations()
                    {
                        Operation = new List<Operation>()
                        {
                            new Operation()
                            {
                                Date = "1-jan",
                                Action = "[Fertilise].Apply(Amount: 100, Type:Fertiliser.Types.NO3N, Depth:300)"
                            }
                        }
                    }
                }
            };

            simulation.Run();

            var soil = simulation.Children[2] as MockSoil;
            Assert.AreEqual(new double[] { 1, 2, 103 }, soil.NO3);
            Assert.AreEqual("100 kg/ha of NO3N added at depth 300 layer 3", MockSummary.messages[0]);
        }
```
This test creates a simulation using code, runs it and then uses *Assert* to determine the outputs are as expected. The preference is to create the simulation using code (rather than reading from a resource) as the test is self contained with no need to consult other files. A lot of tests need to create complex simulations and so using a resource file is necessary.

The above test also highlights the use of mocks, classes that mimic a real model but have much simplified, controllable behaviour. The example uses *MockSoilSolute* which mimics a solute model. This allows this fertiliser test to be isolated from the other models in the simulation. It is better to test just the function in question and not the rest of the APSIM as well. Mocks allow you to do that.