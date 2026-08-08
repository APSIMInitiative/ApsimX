using System;
using Models.Agroforestry;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using NUnit.Framework;

namespace UnitTests.Agroforestry
{
    [TestFixture]
    public class FruitOrganTests
    {
        [Test]
        public void FruitOrgan_DoesNotExposeLegacyCompatibilityParameters()
        {
            Assert.That(typeof(FruitOrgan).GetProperty("FruitGrowth" + "Potential"), Is.Null);
        }

        [Test]
        public void PhaseChanged_FloweringStage_InitialisesSeasonAndUsesBudLoadPotential()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(
                out GenericFruitTree tree,
                numberFunctionValue: 4.0,
                budLoad: 6.0,
                reproductiveCycleActive: true);
            fruitOrgan.ReproPotentialPerBud = 3.0;

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.FloweringStage }
            });

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.Nflower, Is.EqualTo(18.0).Within(1e-12));
                Assert.That(fruitOrgan.FruitAgeTT, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(fruitOrgan.InReproSeason, Is.True);
                Assert.That(fruitOrgan.IsHarvestable, Is.False);
                Assert.That(fruitOrgan.Number, Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void PhaseChanged_FloweringStage_FallsBackToNumberFunctionWhenBudLoadMissing()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(
                out GenericFruitTree tree,
                numberFunctionValue: 4.5,
                reproductiveCycleActive: true);
            fruitOrgan.ReproPotentialPerBud = 3.0;

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.FloweringStage }
            });

            Assert.That(fruitOrgan.Nflower, Is.EqualTo(4.5).Within(1e-12));
        }

        [Test]
        public void PhaseChanged_FruitSetFillAndMaturity_UpdatePublicSeasonFlags()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(
                out GenericFruitTree tree,
                numberFunctionValue: 5.0,
                reproductiveCycleActive: true);

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.FruitSetStage }
            });

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.Nflower, Is.EqualTo(5.0).Within(1e-12));
                Assert.That(fruitOrgan.InReproSeason, Is.True);
                Assert.That(fruitOrgan.IsHarvestable, Is.False);
            });

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.FruitFillStage }
            });

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.InReproSeason, Is.True);
                Assert.That(fruitOrgan.IsHarvestable, Is.False);
            });

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.MaturityStage }
            });

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.InReproSeason, Is.True);
                Assert.That(fruitOrgan.IsHarvestable, Is.True);
            });
        }

        [Test]
        public void SyncCohortFromLivePools_ScalesFruitNumberWhenLiveBiomassDeclines()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(out _);
            Utilities.SetProperty(fruitOrgan, "Nfruit", 10.0);
            Utilities.SetProperty(fruitOrgan, "FruitDM_g_m2", 20.0);
            fruitOrgan.Live.StructuralWt = 6.0;
            fruitOrgan.Live.StorageWt = 4.0;

            fruitOrgan.SyncCohortFromLivePools();

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.Nfruit, Is.EqualTo(5.0).Within(1e-12));
                Assert.That(fruitOrgan.FruitDM_g_m2, Is.EqualTo(10.0).Within(1e-12));
                Assert.That(fruitOrgan.Number, Is.EqualTo(5.0).Within(1e-12));
            });
        }

        [Test]
        public void ResetCohortIfNoFruit_ClearsSeasonStateWhenLivePoolsAreEmpty()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(out _);
            SeedActiveCohort(fruitOrgan);

            fruitOrgan.ResetCohortIfNoFruit();

            AssertFruitCohortReset(fruitOrgan);
        }

        [Test]
        public void ResetCohortAfterHarvest_ClearsSeasonStateWhenPoolsAreAlreadyEmpty()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(out _);
            SeedActiveCohort(fruitOrgan);

            fruitOrgan.ResetCohortAfterHarvest();

            AssertFruitCohortReset(fruitOrgan);
        }

        [Test]
        public void PostHarvesting_AutoHarvestPolicy_ResetsCohortState()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(out GenericFruitTree tree);
            tree.FruitFatePolicy = FruitFatePolicy.AutoHarvest;
            SeedActiveCohort(fruitOrgan);

            Utilities.CallMethod(fruitOrgan, "OnPostHarvestingCohort", new object[]
            {
                tree,
                new HarvestingParameters { RemoveBiomass = true }
            });

            AssertFruitCohortReset(fruitOrgan);
        }

        [Test]
        public void DoPotentialPlantGrowth_KeepsNumberSyncedToCurrentFruitCount()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(out _);
            Utilities.SetProperty(fruitOrgan, "Nfruit", 7.25);
            fruitOrgan.Number = 1.0;

            Utilities.CallMethod(fruitOrgan, "OnDoPotentialPlantGrowthSyncNumber", new object[]
            {
                fruitOrgan,
                EventArgs.Empty
            });

            Assert.That(fruitOrgan.Number, Is.EqualTo(7.25).Within(1e-12));
        }

        [Test]
        public void UpdateCohortDaily_AllowsFruitNumberIncreaseDuringFruitSetWindow()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(
                out GenericFruitTree tree,
                numberFunctionValue: 10.0,
                reproductiveCycleActive: true);
            ConfigureSetKineticsWithoutDrop(fruitOrgan);
            Utilities.SetProperty(fruitOrgan, "Nfruit", 2.0);
            Utilities.SetProperty(fruitOrgan, "FruitDM_g_m2", 0.0);

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.FruitSetStage }
            });
            Utilities.CallMethod(fruitOrgan, "UpdateCohortDaily", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.Nfruit, Is.GreaterThan(2.0));
                Assert.That(fruitOrgan.ReproSetRateOutput, Is.GreaterThan(0.0));
            });
        }

        [Test]
        public void UpdateCohortDaily_DoesNotRefillFruitNumberDuringFruitFill()
        {
            FruitOrgan fruitOrgan = CreateFruitOrgan(
                out GenericFruitTree tree,
                numberFunctionValue: 10.0,
                reproductiveCycleActive: true);
            ConfigureSetKineticsWithoutDrop(fruitOrgan);
            Utilities.SetProperty(fruitOrgan, "Nflower", 10.0);
            Utilities.SetProperty(fruitOrgan, "Nfruit", 2.0);
            Utilities.SetProperty(fruitOrgan, "FruitDM_g_m2", 4.0);
            fruitOrgan.Live.StructuralWt = 4.0;

            Utilities.CallMethod(fruitOrgan, "OnPhaseChanged", new object[]
            {
                tree,
                new PhaseChangedType { StageName = tree.FruitFillStage }
            });
            Utilities.CallMethod(fruitOrgan, "UpdateCohortDaily", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.Nfruit, Is.EqualTo(2.0).Within(1e-12));
                Assert.That(fruitOrgan.ReproSetRateOutput, Is.EqualTo(0.0).Within(1e-12));
            });
        }

        private static FruitOrgan CreateFruitOrgan(
            out GenericFruitTree tree,
            double numberFunctionValue = 1.0,
            double budLoad = 0.0,
            bool reproductiveCycleActive = false)
        {
            tree = new GenericFruitTree
            {
                FloweringStage = "Flowering",
                FruitSetStage = "FruitSet",
                FruitFillStage = "FruitFill",
                MaturityStage = "Maturity",
                PlantType = "GenericFruitTree"
            };
            Utilities.InjectLink(tree, "reproductiveCycleActive", reproductiveCycleActive);

            if (budLoad > 0.0)
            {
                BudOrgan budOrgan = new BudOrgan
                {
                    NodeNumber = budLoad
                };
                Utilities.InjectLink(tree, "budOrgan", budOrgan);
            }

            FruitOrgan fruitOrgan = new FruitOrgan
            {
                Name = "Fruit",
                Summary = new TestSummary(),
                DMDemand = new BiomassPoolType(),
                NDemand = new BiomassPoolType(),
                DMSupply = new BiomassSupplyType(),
                NSupply = new BiomassSupplyType(),
                potentialDMAllocation = new BiomassPoolType(),
                Allocated = new Biomass(),
                Senesced = new Biomass(),
                Detached = new Biomass(),
                Removed = new Biomass()
            };

            Utilities.InjectLink(fruitOrgan, "tree", tree);
            Utilities.InjectLink(fruitOrgan, "NumberFunction", new ConstantFunction(numberFunctionValue));
            return fruitOrgan;
        }

        private static void SeedActiveCohort(FruitOrgan fruitOrgan)
        {
            Utilities.SetProperty(fruitOrgan, "Nflower", 10.0);
            Utilities.SetProperty(fruitOrgan, "Nfruit", 4.0);
            Utilities.SetProperty(fruitOrgan, "FruitDM_g_m2", 3.0);
            Utilities.SetProperty(fruitOrgan, "FruitAgeTT", 25.0);
            Utilities.SetProperty(fruitOrgan, "InReproSeason", true);
            Utilities.InjectLink(fruitOrgan, "isHarvestable", true);
            fruitOrgan.Number = 4.0;
        }

        private static void ConfigureSetKineticsWithoutDrop(FruitOrgan fruitOrgan)
        {
            fruitOrgan.FruitSetMaxFrac = 0.8;
            fruitOrgan.SetRateMax = 1.0;
            fruitOrgan.Fw50_Set = 0.0;
            fruitOrgan.FwSlope_Set = 0.0;
            fruitOrgan.DropRateBase = 0.0;
            fruitOrgan.DropStressSensitivity = 0.0;
            fruitOrgan.FruitDropRate = 0.0;
        }

        private static void AssertFruitCohortReset(FruitOrgan fruitOrgan)
        {
            Assert.Multiple(() =>
            {
                Assert.That(fruitOrgan.Nflower, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(fruitOrgan.Nfruit, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(fruitOrgan.FruitDM_g_m2, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(fruitOrgan.FruitAgeTT, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(fruitOrgan.InReproSeason, Is.False);
                Assert.That(fruitOrgan.IsHarvestable, Is.False);
                Assert.That(fruitOrgan.Number, Is.EqualTo(0.0).Within(1e-12));
            });
        }
    }
}
