using Models.Agroforestry;
using Models.PMF;
using Models.PMF.Phen;
using NUnit.Framework;

namespace UnitTests.Agroforestry
{
    [TestFixture]
    public class BudOrganTests
    {
        [Test]
        public void PlantSowing_SetsNodeNumberFromBudNumberAndPopulation()
        {
            BudOrgan budOrgan = CreateBudOrgan(out _);

            Utilities.CallMethod(budOrgan, "OnPlantSowing", new object[]
            {
                budOrgan,
                new SowingParameters { Population = 2.5, BudNumber = 3.0 }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(7.5).Within(1e-12));
        }

        [Test]
        public void PlantSowing_ClampsNegativeBudNumberToZero()
        {
            BudOrgan budOrgan = CreateBudOrgan(out _);

            Utilities.CallMethod(budOrgan, "OnPlantSowing", new object[]
            {
                budOrgan,
                new SowingParameters { Population = 2.0, BudNumber = -3.0 }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void PlantSowing_AllowsZeroPopulationWithoutNegativeState()
        {
            BudOrgan budOrgan = CreateBudOrgan(out _);

            Utilities.CallMethod(budOrgan, "OnPlantSowing", new object[]
            {
                budOrgan,
                new SowingParameters { Population = 0.0, BudNumber = 3.0 }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void PlantEnding_ResetsNodeNumberToZero()
        {
            BudOrgan budOrgan = CreateBudOrgan(out _);
            budOrgan.NodeNumber = 6.0;

            Utilities.CallMethod(budOrgan, "OnPlantEnding", new object[] { budOrgan, new System.EventArgs() });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void PhaseChanged_DormancyStage_ResetsNodeNumber()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            budOrgan.NodeNumber = 9.0;
            tree.DormancyStage = "Dormancy";

            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = "Dormancy" }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void PhaseChanged_MatchesConfiguredStagesCaseInsensitivelyAndWithTrimmedEventStage()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            budOrgan.NodeNumber = 9.0;
            tree.DormancyStage = "Dormancy";

            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = " dormancy " }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void PhaseChanged_DoesNothingWhenConfiguredStagesAreBlank()
        {
            string[] blankStages = { null, string.Empty, "   " };

            foreach (string dormancyStage in blankStages)
            {
                foreach (string budBreakStage in blankStages)
                {
                    BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
                    budOrgan.NodeNumber = 9.0;
                    tree.DormancyStage = dormancyStage;
                    tree.BudBreakStage = budBreakStage;

                    Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
                    {
                        tree,
                        new PhaseChangedType { StageName = "Dormancy" }
                    });
                    Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
                    {
                        tree,
                        new PhaseChangedType { StageName = "BudBreak" }
                    });

                    Assert.That(budOrgan.NodeNumber, Is.EqualTo(9.0).Within(1e-12));
                }
            }
        }

        [Test]
        public void PhaseChanged_BudBreakStage_UsesConfiguredFunctionWhenPresent()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            tree.Population = 2.0;
            tree.BudBreakStage = "BudBreak";
            Utilities.InjectLink(budOrgan, "BudsPerTreeAtBudBreak", new ConstantFunction(4.0));

            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = "BudBreak" }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(8.0).Within(1e-12));
        }

        [Test]
        public void BudBreak_ClampsNegativeFunctionValueToZero()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            tree.Population = 2.0;
            tree.BudBreakStage = "BudBreak";
            Utilities.InjectLink(budOrgan, "BudsPerTreeAtBudBreak", new ConstantFunction(-5.0));

            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = "BudBreak" }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void PhaseChanged_BudBreakStage_FallsBackToSownBudsWhenFunctionAbsent()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            tree.DormancyStage = "Dormancy";
            tree.BudBreakStage = "BudBreak";
            tree.Population = 3.0;

            Utilities.CallMethod(budOrgan, "OnPlantSowing", new object[]
            {
                budOrgan,
                new SowingParameters { Population = 3.0, BudNumber = 2.0 }
            });
            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = "Dormancy" }
            });
            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = "BudBreak" }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(6.0).Within(1e-12));
        }

        [Test]
        public void BudBreak_FallbackUsesCurrentTreePopulation()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            tree.BudBreakStage = "BudBreak";

            Utilities.CallMethod(budOrgan, "OnPlantSowing", new object[]
            {
                budOrgan,
                new SowingParameters { Population = 1.0, BudNumber = 3.0 }
            });
            tree.Population = 4.0;
            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = "BudBreak" }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(12.0).Within(1e-12));
        }

        [Test]
        public void PhaseChanged_IgnoresEventsFromDifferentSender()
        {
            BudOrgan budOrgan = CreateBudOrgan(out GenericFruitTree tree);
            tree.DormancyStage = "Dormancy";
            budOrgan.NodeNumber = 5.0;

            Utilities.CallMethod(budOrgan, "OnPhaseChanged", new object[]
            {
                new GenericFruitTree(),
                new PhaseChangedType { StageName = "Dormancy" }
            });

            Assert.That(budOrgan.NodeNumber, Is.EqualTo(5.0).Within(1e-12));
        }

        private static BudOrgan CreateBudOrgan(out GenericFruitTree tree)
        {
            tree = new GenericFruitTree();
            BudOrgan budOrgan = new BudOrgan();
            Utilities.InjectLink(budOrgan, "tree", tree);
            return budOrgan;
        }
    }
}
