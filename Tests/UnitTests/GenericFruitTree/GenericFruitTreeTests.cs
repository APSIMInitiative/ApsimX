using System;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Library;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Storage;
using NUnit.Framework;

namespace UnitTests.Agroforestry
{
    [TestFixture]
    public class GenericFruitTreeTests
    {
        [Test]
        public void ReserveSignals_ReturnZeroWhenTreeIsDeadOrOrgansAreMissing()
        {
            GenericFruitTree deadTree = new GenericFruitTree();
            GenericFruitTree missingOrgansTree = new GenericFruitTree();
            SetPlantAlive(missingOrgansTree, true);
            Utilities.InjectLink(deadTree, "clock", new TestClock { Today = new DateTime(2024, 1, 15) });
            Utilities.InjectLink(missingOrgansTree, "clock", new TestClock { Today = new DateTime(2024, 1, 15) });

            Assert.Multiple(() =>
            {
                Assert.That(deadTree.GetReserveDMRetranslocationFactor(), Is.EqualTo(0.0).Within(1e-12));
                Assert.That(deadTree.GetReserveStorageDemandDM(), Is.EqualTo(0.0).Within(1e-12));
                Assert.That(missingOrgansTree.GetReserveDMRetranslocationFactor(), Is.EqualTo(0.0).Within(1e-12));
                Assert.That(missingOrgansTree.GetReserveStorageDemandDM(), Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void ReserveSignals_UseLiveTreeReserveBalance()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 2.0,
                leafDemand: 5.0,
                woodDemand: 2.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2);

            Assert.Multiple(() =>
            {
                Assert.That(tree.GetReserveDMRetranslocationFactor(), Is.EqualTo(2.0 / (7.0 + 1e-9)).Within(1e-12));
                Assert.That(tree.GetReserveStorageDemandDM(), Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void ReserveSignals_AreCachedWithinDayAndRecomputedAfterDateChange()
        {
            MutableFunction supplyFunction = new MutableFunction(8.0);
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: supplyFunction,
                leafDemand: 3.0,
                woodDemand: 1.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2,
                today: new DateTime(2024, 1, 15));

            double first = tree.GetReserveStorageDemandDM();
            supplyFunction.ReturnValue = 2.0;
            double second = tree.GetReserveStorageDemandDM();

            TestClock clock = (TestClock)ReflectionUtilities.GetValueOfFieldOrProperty("clock", tree);
            clock.Today = new DateTime(2024, 1, 16);
            double third = tree.GetReserveStorageDemandDM();

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(4.0).Within(1e-12));
                Assert.That(second, Is.EqualTo(4.0).Within(1e-12));
                Assert.That(third, Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void AllowReproductivePhase_ReturnsFalseForBlankStage()
        {
            GenericFruitTree tree = new GenericFruitTree();

            Assert.Multiple(() =>
            {
                Assert.That(tree.AllowReproductivePhase(null), Is.False);
                Assert.That(tree.AllowReproductivePhase(string.Empty), Is.False);
                Assert.That(tree.AllowReproductivePhase("   "), Is.False);
            });
        }

        [Test]
        public void AllowReproductivePhase_AlwaysAllowsMaturityStage()
        {
            GenericFruitTree tree = new GenericFruitTree { MaturityStage = "Maturity" };
            Utilities.InjectLink(tree, "reproductiveCycleActive", false);

            Assert.That(tree.AllowReproductivePhase("Maturity"), Is.True);
        }

        [Test]
        public void AllowReproductivePhase_ReturnsTrueOnlyWhenCycleActiveForNonMaturityStages()
        {
            GenericFruitTree tree = new GenericFruitTree { MaturityStage = "Maturity" };

            Utilities.InjectLink(tree, "reproductiveCycleActive", false);
            bool inactiveFlowering = tree.AllowReproductivePhase("Flowering");

            Utilities.InjectLink(tree, "reproductiveCycleActive", true);
            bool activeFlowering = tree.AllowReproductivePhase("Flowering");
            bool activeFruitSet = tree.AllowReproductivePhase("FruitSet");

            Assert.Multiple(() =>
            {
                Assert.That(inactiveFlowering, Is.False);
                Assert.That(activeFlowering, Is.True);
                Assert.That(activeFruitSet, Is.True);
            });
        }

        [Test]
        public void PhaseChanged_IgnoresEventsFromDifferentSender()
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 7, 1));
            Utilities.InjectLink(tree, "reproductiveCycleActive", true);
            Utilities.InjectLink(tree, "inLeafFallWindow", false);
            Utilities.InjectLink(tree, "chillUnitsAccumulated", 12.0);
            Utilities.InjectLink(tree, "forcingUnitsAccumulated", 8.0);

            TriggerPhaseChanged(tree, new GenericFruitTree(), "Dormancy");

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<bool>(tree, "reproductiveCycleActive"), Is.True);
                Assert.That(GetPrivate<bool>(tree, "inLeafFallWindow"), Is.False);
                Assert.That(GetPrivate<double>(tree, "chillUnitsAccumulated"), Is.EqualTo(12.0).Within(1e-12));
                Assert.That(GetPrivate<double>(tree, "forcingUnitsAccumulated"), Is.EqualTo(8.0).Within(1e-12));
            });
        }

        [Test]
        public void PhaseChanged_Dormancy_StartsLeafFallAndDisablesReproductiveCycle()
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 7, 1));
            Utilities.InjectLink(tree, "reproductiveCycleActive", true);
            Utilities.InjectLink(tree, "chillUnitsAccumulated", 12.0);
            Utilities.InjectLink(tree, "forcingUnitsAccumulated", 8.0);

            TriggerPhaseChanged(tree, tree, "Dormancy");

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<bool>(tree, "inLeafFallWindow"), Is.True);
                Assert.That(GetPrivate<bool>(tree, "reproductiveCycleActive"), Is.False);
                Assert.That(GetPrivate<double>(tree, "chillUnitsAccumulated"), Is.EqualTo(0.0).Within(1e-12));
                Assert.That(GetPrivate<double>(tree, "forcingUnitsAccumulated"), Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void PhaseChanged_BudBreak_StopsLeafFallAndActivatesReproductiveCycle()
        {
            DateTime today = new DateTime(2024, 8, 15);
            GenericFruitTree tree = CreatePhaseEventTree(today);

            TriggerPhaseChanged(tree, tree, "Dormancy");
            TriggerPhaseChanged(tree, tree, "BudBreak");

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<bool>(tree, "inLeafFallWindow"), Is.False);
                Assert.That(GetPrivate<bool>(tree, "reproductiveCycleActive"), Is.True);
                Assert.That(GetPrivate<DateTime>(tree, "lastBudBreakDate"), Is.EqualTo(today));
            });
        }

        [Test]
        public void PhaseChanged_BudBreak_RespectsMinimumDaysBetweenBudBreak()
        {
            DateTime firstBudBreak = new DateTime(2024, 8, 1);
            GenericFruitTree tree = CreatePhaseEventTree(firstBudBreak);
            tree.MinDaysBetweenBudBreak = 10;

            TriggerPhaseChanged(tree, tree, "BudBreak");
            TestClock clock = (TestClock)ReflectionUtilities.GetValueOfFieldOrProperty("clock", tree);
            clock.Today = firstBudBreak.AddDays(5);

            TriggerPhaseChanged(tree, tree, "BudBreak");

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<bool>(tree, "reproductiveCycleActive"), Is.False);
                Assert.That(GetPrivate<DateTime>(tree, "lastBudBreakDate"), Is.EqualTo(firstBudBreak));
            });
        }

        [Test]
        public void PhaseChanged_Maturity_DisablesReproductiveCycle()
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 10, 1));
            Utilities.InjectLink(tree, "reproductiveCycleActive", true);

            TriggerPhaseChanged(tree, tree, "Maturity");

            Assert.That(GetPrivate<bool>(tree, "reproductiveCycleActive"), Is.False);
        }

        [Test]
        public void DailyInitialisation_ChillAdvancesToBudBreakWhenThresholdReached()
        {
            GenericFruitTree tree = CreateDormancyTreeWithPhenology(out Phenology phenology);
            tree.ChillUnitsRequired = 5.0;
            Utilities.InjectLink(tree, "ChillRate", new ConstantFunction(5.0));
            MovePhenologyToStage(phenology, "Dormancy");

            Utilities.CallMethod(tree, "OnDoDailyInitialisation_Chill", new object[] { tree, EventArgs.Empty });

            Assert.Multiple(() =>
            {
                Assert.That(phenology.InPhase("BudBreak"), Is.True);
                Assert.That(GetPrivate<double>(tree, "chillUnitsAccumulated"), Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void DailyInitialisation_ChillAdvancesToForcingWhenConfigured()
        {
            GenericFruitTree tree = CreateDormancyTreeWithPhenology(out Phenology phenology);
            tree.ChillUnitsRequired = 5.0;
            tree.ForcingStage = "Forcing";
            tree.ForcingUnitsRequired = 10.0;
            Utilities.InjectLink(tree, "ChillRate", new ConstantFunction(5.0));
            MovePhenologyToStage(phenology, "Dormancy");

            Utilities.CallMethod(tree, "OnDoDailyInitialisation_Chill", new object[] { tree, EventArgs.Empty });

            Assert.Multiple(() =>
            {
                Assert.That(phenology.InPhase("Forcing"), Is.True);
                Assert.That(phenology.InPhase("BudBreak"), Is.False);
            });
        }

        [Test]
        public void DailyInitialisation_ForcingAdvancesToBudBreakWhenGddThresholdReached()
        {
            GenericFruitTree tree = CreateDormancyTreeWithPhenology(out Phenology phenology);
            tree.ForcingStage = "Forcing";
            tree.ForcingUnitsRequired = 5.0;
            tree.ForcingBaseTemperature = 10.0;
            Utilities.InjectLink(tree, "weather", new TestWeather { MinT = 14.0, MaxT = 18.0 });
            MovePhenologyToStage(phenology, "Forcing");

            Utilities.CallMethod(tree, "OnDoDailyInitialisation_Forcing", new object[] { tree, EventArgs.Empty });

            Assert.Multiple(() =>
            {
                Assert.That(phenology.InPhase("BudBreak"), Is.True);
                Assert.That(GetPrivate<double>(tree, "forcingUnitsAccumulated"), Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void DailyInitialisation_ForcingDoesNotAccumulateNegativeGdd()
        {
            GenericFruitTree tree = CreateDormancyTreeWithPhenology(out Phenology phenology);
            tree.ForcingStage = "Forcing";
            tree.ForcingUnitsRequired = 10.0;
            tree.ForcingBaseTemperature = 12.0;
            Utilities.InjectLink(tree, "weather", new TestWeather { MinT = 4.0, MaxT = 8.0 });
            Utilities.InjectLink(tree, "forcingUnitsAccumulated", 2.0);
            MovePhenologyToStage(phenology, "Forcing");

            Utilities.CallMethod(tree, "OnDoDailyInitialisation_Forcing", new object[] { tree, EventArgs.Empty });

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<double>(tree, "forcingUnitsAccumulated"), Is.EqualTo(2.0).Within(1e-12));
                Assert.That(phenology.InPhase("Forcing"), Is.True);
            });
        }

        [Test]
        public void ApplyDailyLeafFall_RemovesConfiguredFractionOverWindow()
        {
            GenericFruitTree tree = CreateLeafFallTree(liveLeafWt: 100.0);
            PerennialLeaf leaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree);
            tree.LeafFallTotalFraction = 0.8;
            tree.LeafFallDurationDays = 4.0;
            tree.LeafFallShape = 1.0;

            TriggerPhaseChanged(tree, tree, "Dormancy");
            for (int day = 0; day < 4; day++)
                Utilities.CallMethod(tree, "ApplyDailyLeafFall", Array.Empty<object>());

            double removedFraction = GetPrivate<double>(tree, "leafFallRemovedSoFarFrac");

            Assert.Multiple(() =>
            {
                Assert.That(leaf.Live.Wt, Is.LessThan(100.0));
                Assert.That(removedFraction, Is.EqualTo(0.8).Within(1e-10));
                Assert.That(removedFraction, Is.LessThanOrEqualTo(tree.LeafFallTotalFraction + 1e-12));
                Assert.That(GetPrivate<bool>(tree, "inLeafFallWindow"), Is.False);
            });
        }

        [Test]
        public void ApplyDailyLeafFall_DoesNothingWhenWindowInactive()
        {
            GenericFruitTree tree = CreateLeafFallTree(liveLeafWt: 100.0);
            PerennialLeaf leaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree);

            Utilities.CallMethod(tree, "ApplyDailyLeafFall", Array.Empty<object>());

            Assert.That(leaf.Live.Wt, Is.EqualTo(100.0).Within(1e-12));
        }

        [Test]
        public void ApplyDailyLeafFall_ClampsInvalidLeafFallParameters()
        {
            foreach ((double totalFraction, double duration, double shape) in new[]
            {
                (-0.5, 0.0, 0.0),
                (1.5, -3.0, -2.0)
            })
            {
                GenericFruitTree tree = CreateLeafFallTree(liveLeafWt: 100.0);
                tree.LeafFallTotalFraction = totalFraction;
                tree.LeafFallDurationDays = duration;
                tree.LeafFallShape = shape;

                TriggerPhaseChanged(tree, tree, "Dormancy");

                Assert.DoesNotThrow(() => Utilities.CallMethod(tree, "ApplyDailyLeafFall", Array.Empty<object>()));
                Assert.Multiple(() =>
                {
                    Assert.That(double.IsFinite(GetPrivate<double>(tree, "leafFallProgress")), Is.True);
                    Assert.That(double.IsFinite(GetPrivate<double>(tree, "leafFallRemovedSoFarFrac")), Is.True);
                    Assert.That(GetPrivate<double>(tree, "leafFallRemovedSoFarFrac"), Is.InRange(0.0, 1.0));
                    Assert.That(((PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree)).Live.Wt, Is.GreaterThanOrEqualTo(0.0));
                });
            }
        }

        [Test]
        public void UpdateCanopyStructure_CreatesLightProfileWithAtLeastOneLayer()
        {
            GenericFruitTree tree = CreateCanopyTree(lai: 2.0, heightMm: 1500.0);
            tree.LayerCount = 0;

            Utilities.CallMethod(tree, "UpdateCanopyStructure", Array.Empty<object>());
            PerennialLeaf leaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree);

            Assert.Multiple(() =>
            {
                Assert.That(leaf.LightProfile, Is.Not.Null);
                Assert.That(leaf.LightProfile.Length, Is.GreaterThanOrEqualTo(1));
                Assert.That(leaf.LightProfile.All(layer => layer.thickness > 0.0), Is.True);
            });
        }

        [Test]
        public void GenericFruitTree_DoesNotExposeLegacyCompatibilityParameters()
        {
            Assert.Multiple(() =>
            {
                Assert.That(typeof(GenericFruitTree).GetProperty("Density" + "Distribution", BindingFlags.Instance | BindingFlags.Public), Is.Null);
                Assert.That(typeof(GenericFruitTree).GetProperty("LeafWaterStress" + "ForFruit", BindingFlags.Instance | BindingFlags.Public), Is.Null);
            });
        }

        [Test]
        public void UpdateCanopyStructure_PreservesPhysicalLaiAcrossProfile()
        {
            const double lai = 3.2;
            GenericFruitTree tree = CreateCanopyTree(lai: lai, heightMm: 1500.0);
            tree.LayerCount = 5;
            tree.ClumpingIndex = 1.0;

            Utilities.CallMethod(tree, "UpdateCanopyStructure", Array.Empty<object>());
            PerennialLeaf leaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree);

            Assert.That(leaf.LightProfile.Sum(layer => layer.AmountOnGreen), Is.EqualTo(lai).Within(1e-10));
        }

        [Test]
        public void UpdateCanopyStructure_ClampsClumpingIndexForLightProfile()
        {
            GenericFruitTree lowClumpingTree = CreateCanopyTree(lai: 3.0, heightMm: 1500.0);
            lowClumpingTree.ClumpingIndex = 0.05;
            Utilities.CallMethod(lowClumpingTree, "UpdateCanopyStructure", Array.Empty<object>());
            PerennialLeaf lowLeaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", lowClumpingTree);

            GenericFruitTree highClumpingTree = CreateCanopyTree(lai: 3.0, heightMm: 1500.0);
            highClumpingTree.ClumpingIndex = 3.5;
            Utilities.CallMethod(highClumpingTree, "UpdateCanopyStructure", Array.Empty<object>());
            PerennialLeaf highLeaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", highClumpingTree);

            Assert.Multiple(() =>
            {
                Assert.That(lowLeaf.LightProfile.Sum(layer => layer.AmountOnGreen), Is.EqualTo(0.2 * 3.0).Within(1e-10));
                Assert.That(highLeaf.LightProfile.Sum(layer => layer.AmountOnGreen), Is.EqualTo(2.0 * 3.0).Within(1e-10));
                Assert.That(lowLeaf.LightProfile.Concat(highLeaf.LightProfile).All(layer => double.IsFinite(layer.AmountOnGreen) && layer.AmountOnGreen >= 0.0), Is.True);
            });
        }

        [Test]
        public void UpdateCanopyStructure_ClampsNegativeCrownRatiosToNonNegativeWidthAndDepth()
        {
            GenericFruitTree tree = CreateCanopyTree(lai: 2.0, heightMm: 1500.0);
            tree.CrownWidthHeightRatio = -1.0;
            tree.CrownDepthHeightRatio = -0.5;

            Utilities.CallMethod(tree, "UpdateCanopyStructure", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(tree.Width, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(tree.Depth, Is.GreaterThanOrEqualTo(0.0));
            });
        }

        [Test]
        public void UpdateCanopyStructure_BetaLadTopHeavyAndBottomHeavyProfilesShiftLaiAsExpected()
        {
            GenericFruitTree topHeavyTree = CreateCanopyTree(lai: 4.0, heightMm: 1500.0);
            topHeavyTree.LayerCount = 6;
            topHeavyTree.LAD_Alpha = 5.0;
            topHeavyTree.LAD_Beta = 1.5;
            Utilities.CallMethod(topHeavyTree, "UpdateCanopyStructure", Array.Empty<object>());
            PerennialLeaf topHeavyLeaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", topHeavyTree);

            GenericFruitTree bottomHeavyTree = CreateCanopyTree(lai: 4.0, heightMm: 1500.0);
            bottomHeavyTree.LayerCount = 6;
            bottomHeavyTree.LAD_Alpha = 1.5;
            bottomHeavyTree.LAD_Beta = 5.0;
            Utilities.CallMethod(bottomHeavyTree, "UpdateCanopyStructure", Array.Empty<object>());
            PerennialLeaf bottomHeavyLeaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", bottomHeavyTree);

            double topHeavyLower = topHeavyLeaf.LightProfile.Take(3).Sum(layer => layer.AmountOnGreen);
            double topHeavyUpper = topHeavyLeaf.LightProfile.Skip(3).Sum(layer => layer.AmountOnGreen);
            double bottomHeavyLower = bottomHeavyLeaf.LightProfile.Take(3).Sum(layer => layer.AmountOnGreen);
            double bottomHeavyUpper = bottomHeavyLeaf.LightProfile.Skip(3).Sum(layer => layer.AmountOnGreen);

            Assert.Multiple(() =>
            {
                Assert.That(topHeavyUpper, Is.GreaterThan(topHeavyLower));
                Assert.That(bottomHeavyLower, Is.GreaterThan(bottomHeavyUpper));
            });
        }

        [Test]
        public void UpdateHydraulics_ReturnsZeroWhenRootDepthIsZero()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 0.0,
                thicknessMm: new[] { 100.0, 100.0 },
                kCmPerHour: new[] { 1.0, 2.0 },
                pawMm: new[] { 50.0, 50.0 },
                pawcMm: new[] { 100.0, 100.0 });

            double conductance = (double)Utilities.CallMethod(tree, "UpdateHydraulics", Array.Empty<object>());

            Assert.That(conductance, Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void UpdateHydraulics_ComputesSeriesConductanceFromSoilAndPlant()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 200.0,
                thicknessMm: new[] { 100.0, 100.0 },
                kCmPerHour: new[] { 1.0, 3.0 },
                pawMm: new[] { 50.0, 50.0 },
                pawcMm: new[] { 100.0, 100.0 });
            tree.PlantHydraulicConductance = 1.0;

            double conductance = (double)Utilities.CallMethod(tree, "UpdateHydraulics", Array.Empty<object>());
            double kSoil = ((1.0 * 0.24 * 0.1) + (3.0 * 0.24 * 0.1)) / 0.2;
            double expected = 1.0 / (1.0 / kSoil + 1.0 / 1.0);

            Assert.That(conductance, Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void UpdateHydraulics_UsesPartialRootedLayerDepth()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 150.0,
                thicknessMm: new[] { 100.0, 100.0 },
                kCmPerHour: new[] { 1.0, 3.0 },
                pawMm: new[] { 50.0, 50.0 },
                pawcMm: new[] { 100.0, 100.0 });
            tree.PlantHydraulicConductance = 1.0;

            double conductance = (double)Utilities.CallMethod(tree, "UpdateHydraulics", Array.Empty<object>());
            double kSoil = ((1.0 * 0.24 * 0.1) + (3.0 * 0.24 * 0.05)) / 0.15;
            double expected = 1.0 / (1.0 / kSoil + 1.0 / 1.0);

            Assert.That(conductance, Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void UpdateHydraulics_HandlesMismatchedSoilArraysUsingMinimumLength()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 300.0,
                thicknessMm: new[] { 100.0, 100.0, 100.0 },
                kCmPerHour: new[] { 1.0, 2.0 },
                pawMm: new[] { 50.0, 50.0, 50.0 },
                pawcMm: new[] { 100.0, 100.0, 100.0 });

            double conductance = (double)Utilities.CallMethod(tree, "UpdateHydraulics", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(double.IsFinite(conductance), Is.True);
                Assert.That(conductance, Is.GreaterThan(0.0));
            });
        }

        [Test]
        public void UpdateSupplyState_NoRootsDefaultsToNoStress()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 0.0,
                thicknessMm: new[] { 100.0 },
                kCmPerHour: new[] { 1.0 },
                pawMm: new[] { 20.0 },
                pawcMm: new[] { 100.0 });

            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 0.0 });

            Assert.Multiple(() =>
            {
                Assert.That(tree.RootZonePAWFraction, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(tree.SupplyIndexRaw, Is.EqualTo(1.0).Within(1e-12));
                Assert.That(tree.SupplyIndexEffective, Is.EqualTo(1.0).Within(1e-12));
                Assert.That(tree.GrowthStressModifier, Is.EqualTo(1.0).Within(1e-12));
            });
        }

        [Test]
        public void UpdateSupplyState_ComputesRootZonePawFractionUsingPartialLayers()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 150.0,
                thicknessMm: new[] { 100.0, 100.0 },
                kCmPerHour: new[] { 1.0, 1.0 },
                pawMm: new[] { 50.0, 20.0 },
                pawcMm: new[] { 100.0, 100.0 });

            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 1.0 });

            Assert.That(tree.RootZonePAWFraction, Is.EqualTo(60.0 / 150.0).Within(1e-12));
        }

        [Test]
        public void UpdateSupplyState_IsCachedWithinDay()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 100.0,
                thicknessMm: new[] { 100.0 },
                kCmPerHour: new[] { 1.0 },
                pawMm: new[] { 80.0 },
                pawcMm: new[] { 100.0 });
            SimpleSoilWater water = (SimpleSoilWater)ReflectionUtilities.GetValueOfFieldOrProperty("soilWater", tree);

            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 1.0 });
            double firstPaw = tree.RootZonePAWFraction;
            double firstRaw = tree.SupplyIndexRaw;
            water.PAWmm = new[] { 5.0 };
            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 0.0 });

            Assert.Multiple(() =>
            {
                Assert.That(tree.RootZonePAWFraction, Is.EqualTo(firstPaw).Within(1e-12));
                Assert.That(tree.SupplyIndexRaw, Is.EqualTo(firstRaw).Within(1e-12));
            });
        }

        [Test]
        public void UpdateSupplyState_RecomputesAfterDateChange()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 100.0,
                thicknessMm: new[] { 100.0 },
                kCmPerHour: new[] { 1.0 },
                pawMm: new[] { 80.0 },
                pawcMm: new[] { 100.0 });
            SimpleSoilWater water = (SimpleSoilWater)ReflectionUtilities.GetValueOfFieldOrProperty("soilWater", tree);
            TestClock clock = (TestClock)ReflectionUtilities.GetValueOfFieldOrProperty("clock", tree);

            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 1.0 });
            water.PAWmm = new[] { 20.0 };
            clock.Today = clock.Today.AddDays(1);
            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 1.0 });

            Assert.That(tree.RootZonePAWFraction, Is.EqualTo(0.2).Within(1e-12));
        }

        [Test]
        public void UpdateSupplyState_UsesFastDownAndSlowUpSmoothing()
        {
            GenericFruitTree tree = CreateHydraulicsTree(
                rootDepthMm: 100.0,
                thicknessMm: new[] { 100.0 },
                kCmPerHour: new[] { 1.0 },
                pawMm: new[] { 100.0 },
                pawcMm: new[] { 100.0 });
            tree.SupplyTauDownDays = 1.0;
            tree.SupplyTauUpDays = 10.0;
            TestClock clock = (TestClock)ReflectionUtilities.GetValueOfFieldOrProperty("clock", tree);

            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 10.0 });
            double high = tree.SupplyIndexEffective;

            clock.Today = clock.Today.AddDays(1);
            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 0.0 });
            double low = tree.SupplyIndexEffective;

            clock.Today = clock.Today.AddDays(1);
            Utilities.CallMethod(tree, "UpdateSupplyState", new object[] { 10.0 });
            double recovered = tree.SupplyIndexEffective;

            Assert.Multiple(() =>
            {
                Assert.That(low, Is.LessThan(high));
                Assert.That(recovered, Is.GreaterThan(low));
                Assert.That(high - low, Is.GreaterThan(recovered - low));
            });
        }

        [Test]
        public void SeedInitialReserves_AddsStorageUpToCapacity()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 0.0,
                leafDemand: 0.0,
                woodDemand: 0.0,
                structuralWood: 100.0,
                storageWood: 0.0,
                reserveCapacityFrac: 0.15,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.0);
            tree.ReserveInitFrac = 0.25;
            GenericOrgan wood = (GenericOrgan)ReflectionUtilities.GetValueOfFieldOrProperty("woodOrgan", tree);

            tree.GetReserveStorageDemandDM();

            Assert.That(wood.Live.StorageWt, Is.EqualTo(15.0).Within(1e-12));
        }

        [Test]
        public void SeedInitialReserves_DoesNotExceedExistingStorageOrCapacity()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 0.0,
                leafDemand: 0.0,
                woodDemand: 0.0,
                structuralWood: 100.0,
                storageWood: 30.0,
                reserveCapacityFrac: 0.15,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.0);
            tree.ReserveInitFrac = 0.25;
            GenericOrgan wood = (GenericOrgan)ReflectionUtilities.GetValueOfFieldOrProperty("woodOrgan", tree);

            tree.GetReserveStorageDemandDM();

            Assert.That(wood.Live.StorageWt, Is.EqualTo(30.0).Within(1e-12));
        }

        [Test]
        public void ReserveSignals_StorageDemandIsLimitedByFreeCapacityAndStorageRate()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 20.0,
                leafDemand: 3.0,
                woodDemand: 2.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.25,
                reserveMobilisationRate: 0.0);

            double stored = tree.GetReserveStorageDemandDM();

            Assert.Multiple(() =>
            {
                Assert.That(stored, Is.LessThanOrEqualTo(tree.ReserveSurplusDM + 1e-12));
                Assert.That(stored, Is.LessThanOrEqualTo((tree.ReserveCapacityDM - tree.ReservePoolDM) + 1e-12));
                Assert.That(stored, Is.EqualTo(0.25 * (50.0 - 10.0)).Within(1e-12));
            });
        }

        [Test]
        public void ReserveSignals_MobilisationIsLimitedByReservePoolAndMobilisationRate()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 2.0,
                leafDemand: 6.0,
                woodDemand: 4.0,
                structuralWood: 100.0,
                storageWood: 20.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.1);

            tree.GetReserveDMRetranslocationFactor();

            Assert.Multiple(() =>
            {
                Assert.That(tree.ReserveMobilisedDM, Is.LessThanOrEqualTo(tree.ReserveDeficitDM + 1e-12));
                Assert.That(tree.ReserveMobilisedDM, Is.LessThanOrEqualTo(0.1 * 20.0 + 1e-12));
                Assert.That(tree.ReserveMobilisedDM, Is.EqualTo(2.0).Within(1e-12));
            });
        }

        [Test]
        public void ReserveSignals_ClampNegativeSupplyDemandAndReserveParameters()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: -5.0,
                leafDemand: -3.0,
                woodDemand: -2.0,
                structuralWood: -100.0,
                storageWood: -10.0,
                reserveCapacityFrac: -0.5,
                reserveStorageRate: -0.25,
                reserveMobilisationRate: -0.1);

            tree.GetReserveDMRetranslocationFactor();

            Assert.Multiple(() =>
            {
                Assert.That(new[]
                {
                    tree.ReserveSupplyDM,
                    tree.ReserveCriticalDemandDM,
                    tree.ReserveSurplusDM,
                    tree.ReserveDeficitDM,
                    tree.ReserveCapacityDM,
                    tree.ReservePoolDM,
                    tree.ReserveStoredDM,
                    tree.ReserveMobilisedDM,
                    tree.ReserveDMRetranslocationFactor
                }.All(value => double.IsFinite(value) && value >= 0.0), Is.True);
                Assert.That(tree.ReserveDMRetranslocationFactor, Is.InRange(0.0, 1.0));
            });
        }

        [Test]
        public void ReserveSignals_UseFallbackOrganDemandWhenDemandFunctionsMissing()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 1.0,
                leafDemand: 3.0,
                woodDemand: 4.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.1);

            tree.GetReserveDMRetranslocationFactor();

            Assert.That(tree.ReserveCriticalDemandDM, Is.EqualTo(7.0).Within(1e-12));
        }

        [Test]
        public void IsReadyForHarvesting_ReturnsTrueWhenPhenologyBeyondMaturity()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out Phenology phenology);
            tree.HarvestByPhenology = true;
            tree.MaturityStage = "Maturity";
            MovePhenologyToStage(phenology, "Maturity");

            Assert.That(tree.IsReadyForHarvesting, Is.True);
        }

        [Test]
        public void IsReadyForHarvesting_ReturnsTrueWhenQualityThresholdsMet()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out _);
            tree.HarvestByPhenology = false;
            tree.HarvestBrixThreshold = 12.0;
            tree.HarvestAcidThreshold = 0.8;
            Utilities.InjectLink(tree, "fruitDryMatter_g_m2", 25.0);
            Utilities.SetProperty(tree, "QualityBrix", 13.0);
            Utilities.SetProperty(tree, "QualityAcidPct", 0.6);

            Assert.That(tree.IsReadyForHarvesting, Is.True);
        }

        [Test]
        public void IsReadyForHarvesting_FallsBackToFruitPresenceWhenNoHarvestCriteriaEnabled()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out _);
            tree.HarvestByPhenology = false;
            tree.HarvestBrixThreshold = 0.0;
            tree.HarvestAcidThreshold = 0.0;
            Utilities.InjectLink(tree, "fruitDryMatter_g_m2", 10.0);

            Assert.That(tree.IsReadyForHarvesting, Is.True);
        }

        [Test]
        public void UpdateHarvestStatus_FiresReadyToHarvestOnlyOnce()
        {
            GenericFruitTree tree = CreateHarvestReadyTree();
            int eventCount = 0;
            tree.ReadyToHarvest += (_, _) => eventCount++;

            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());
            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(GetPrivate<bool>(tree, "isHarvested"), Is.True);
            });
        }

        [Test]
        public void UpdateHarvestStatus_DoesNotFireWhenTreeDead()
        {
            GenericFruitTree tree = CreateHarvestReadyTree();
            SetPlantAlive(tree, false);
            int eventCount = 0;
            tree.ReadyToHarvest += (_, _) => eventCount++;

            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(eventCount, Is.EqualTo(0));
                Assert.That(GetPrivate<bool>(tree, "isHarvested"), Is.False);
            });
        }

        [Test]
        public void UpdateHarvestStatus_AutoHarvestPolicyCallsHarvestAndMarksHarvested()
        {
            GenericFruitTree tree = CreateHarvestReadyTree();
            tree.FruitFatePolicy = FruitFatePolicy.AutoHarvest;
            int harvestCount = 0;
            tree.Harvesting += (_, _) => harvestCount++;

            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(harvestCount, Is.EqualTo(1));
                Assert.That(GetPrivate<bool>(tree, "isHarvested"), Is.True);
            });
        }

        [Test]
        public void TryResetSeasonalPhenologyAfterHarvest_UsesConfiguredResetStageWhenPresent()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out Phenology phenology);
            tree.ResetPhenologyAfterHarvest = true;
            tree.SeasonalPhenologyResetStage = "BudBreak";
            tree.BudBreakStage = "BudBreak";
            MovePhenologyToStage(phenology, "Maturity");

            bool reset = (bool)Utilities.CallMethod(tree, "TryResetSeasonalPhenologyAfterHarvest", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(reset, Is.True);
                Assert.That(phenology.InPhase("BudBreak_Maturity"), Is.True);
            });
        }

        [Test]
        public void TryResetSeasonalPhenologyAfterHarvest_FallsBackToDormancyThenBudBreak()
        {
            GenericFruitTree dormancyTree = CreateTreeWithPhenology(out Phenology dormancyPhenology);
            dormancyTree.ResetPhenologyAfterHarvest = true;
            dormancyTree.DormancyStage = "Dormancy";
            dormancyTree.BudBreakStage = "BudBreak";
            MovePhenologyToStage(dormancyPhenology, "Maturity");

            bool dormancyReset = (bool)Utilities.CallMethod(dormancyTree, "TryResetSeasonalPhenologyAfterHarvest", Array.Empty<object>());

            GenericFruitTree budBreakTree = CreateTreeWithBudBreakOnlyPhenology(out Phenology budBreakPhenology);
            budBreakTree.ResetPhenologyAfterHarvest = true;
            budBreakTree.DormancyStage = string.Empty;
            budBreakTree.BudBreakStage = "BudBreak";
            MovePhenologyToStage(budBreakPhenology, "Maturity");

            bool budBreakReset = (bool)Utilities.CallMethod(budBreakTree, "TryResetSeasonalPhenologyAfterHarvest", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(dormancyReset, Is.True);
                Assert.That(dormancyPhenology.InPhase("Dormancy_BudBreak"), Is.True);
                Assert.That(budBreakReset, Is.True);
                Assert.That(budBreakPhenology.InPhase("BudBreak_Maturity"), Is.True);
            });
        }

        [Test]
        public void TryResetSeasonalPhenologyAfterHarvest_ThrowsWhenResetStageMissingFromPhenology()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out _);
            tree.ResetPhenologyAfterHarvest = true;
            tree.SeasonalPhenologyResetStage = "NotAStage";

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                Utilities.CallMethod(tree, "TryResetSeasonalPhenologyAfterHarvest", Array.Empty<object>()));

            Assert.That(exception.InnerException.Message, Does.Contain("NotAStage"));
        }

        [Test]
        public void TryResetSeasonalPhenologyAfterHarvest_IsIdempotentWithinSameDay()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out Phenology phenology);
            tree.ResetPhenologyAfterHarvest = true;
            tree.DormancyStage = "Dormancy";
            MovePhenologyToStage(phenology, "Maturity");

            Utilities.CallMethod(tree, "TryResetSeasonalPhenologyAfterHarvest", Array.Empty<object>());
            Utilities.InjectLink(tree, "isHarvested", true);
            MovePhenologyToStage(phenology, "Maturity");

            bool second = (bool)Utilities.CallMethod(tree, "TryResetSeasonalPhenologyAfterHarvest", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.True);
                Assert.That(phenology.InPhase("Maturity_Harvest"), Is.True);
                Assert.That(GetPrivate<bool>(tree, "isHarvested"), Is.False);
            });
        }

        [Test]
        public void UpdateFruitQuality_IncreasesBrixAndAcidWhenFruitDmIncreases()
        {
            GenericFruitTree tree = CreateQualityTree(out FruitOrgan fruit);
            fruit.Live.StructuralWt = 10.0;
            Utilities.InjectLink(tree, "prevFruitDM_g_m2", 5.0);
            double initialSugar = GetPrivate<double>(tree, "fruitSugarMass_g_m2");
            double initialAcid = GetPrivate<double>(tree, "fruitAcidMass_g_m2");

            Utilities.CallMethod(tree, "UpdateFruitQuality", new object[] { true });

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<double>(tree, "fruitSugarMass_g_m2"), Is.GreaterThan(initialSugar));
                Assert.That(GetPrivate<double>(tree, "fruitAcidMass_g_m2"), Is.GreaterThan(initialAcid));
                Assert.That(double.IsFinite(tree.QualityBrix), Is.True);
                Assert.That(double.IsFinite(tree.QualityAcidPct), Is.True);
                Assert.That(double.IsFinite(tree.QualityDMPct), Is.True);
                Assert.That(tree.QualityBrix, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(tree.QualityAcidPct, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(tree.QualityDMPct, Is.GreaterThanOrEqualTo(0.0));
            });
        }

        [Test]
        public void UpdateFruitQuality_DecaysQualityPoolsWhenFruitDmIsZero()
        {
            GenericFruitTree tree = CreateQualityTree(out FruitOrgan fruit);
            fruit.Live.StructuralWt = 0.0;
            Utilities.InjectLink(tree, "fruitSugarMass_g_m2", 10.0);
            Utilities.InjectLink(tree, "fruitAcidMass_g_m2", 5.0);

            Utilities.CallMethod(tree, "UpdateFruitQuality", new object[] { true });

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<double>(tree, "fruitSugarMass_g_m2"), Is.LessThan(10.0));
                Assert.That(GetPrivate<double>(tree, "fruitAcidMass_g_m2"), Is.LessThan(5.0));
                Assert.That(tree.QualityBrix, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(tree.QualityAcidPct, Is.EqualTo(0.0).Within(1e-12));
            });
        }

        [Test]
        public void UpdateFruitQuality_SoftCapsBrixAndAcidOutputs()
        {
            GenericFruitTree tree = CreateQualityTree(out FruitOrgan fruit);
            fruit.Live.StructuralWt = 10.0;
            tree.BrixMax = 18.0;
            tree.AcidMax = 2.0;
            Utilities.InjectLink(tree, "fruitSugarMass_g_m2", 1000.0);
            Utilities.InjectLink(tree, "fruitAcidMass_g_m2", 500.0);

            Utilities.CallMethod(tree, "UpdateFruitQuality", new object[] { false });

            Assert.Multiple(() =>
            {
                Assert.That(tree.QualityBrix, Is.LessThanOrEqualTo(tree.BrixMax));
                Assert.That(tree.QualityAcidPct, Is.LessThanOrEqualTo(tree.AcidMax));
            });
        }

        [Test]
        public void ComputeFruitWaterFraction_ClampsBetweenConfiguredMinMaxAndHardBounds()
        {
            GenericFruitTree tree = CreateQualityTree(out _);
            tree.WC_Min = 0.95;
            tree.WC_Max = 0.20;
            Utilities.SetProperty(tree, "SupplyIndexEffective", 4.0);

            double lowStage = (double)Utilities.CallMethod(tree, "ComputeFruitWaterFraction", new object[] { -100.0 });
            double highStage = (double)Utilities.CallMethod(tree, "ComputeFruitWaterFraction", new object[] { 100.0 });
            double lowerBound = Math.Max(0.01, Math.Min(tree.WC_Min, tree.WC_Max));
            double upperBound = Math.Min(0.99, Math.Max(tree.WC_Min, tree.WC_Max));

            Assert.Multiple(() =>
            {
                Assert.That(lowStage, Is.InRange(lowerBound, upperBound));
                Assert.That(highStage, Is.InRange(lowerBound, upperBound));
            });
        }

        [Test]
        public void Prune_RemovesConfiguredLiveLeafFractionAndAppliesStructuralState()
        {
            GenericFruitTree tree = CreateLeafFallTree(liveLeafWt: 100.0);
            tree.PruningLeafFraction = 0.25;
            tree.PruningStructuralFraction = 0.4;
            PerennialLeaf leaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree);

            Utilities.CallMethod(tree, "OnPrune", new object[] { tree, EventArgs.Empty });

            Assert.Multiple(() =>
            {
                Assert.That(leaf.Live.Wt, Is.EqualTo(75.0).Within(1e-10));
                Assert.That(tree.ActiveStructuralPruningFractionOutput, Is.EqualTo(0.4).Within(1e-12));
            });
        }

        [Test]
        public void Prune_DoesNothingForZeroOrNegativePruningFractions()
        {
            foreach (double fraction in new[] { 0.0, -0.2 })
            {
                GenericFruitTree tree = CreateLeafFallTree(liveLeafWt: 100.0);
                tree.PruningLeafFraction = fraction;
                tree.PruningStructuralFraction = fraction;
                PerennialLeaf leaf = (PerennialLeaf)ReflectionUtilities.GetValueOfFieldOrProperty("leafOrgan", tree);

                Utilities.CallMethod(tree, "OnPrune", new object[] { tree, EventArgs.Empty });

                Assert.Multiple(() =>
                {
                    Assert.That(leaf.Live.Wt, Is.EqualTo(100.0).Within(1e-12));
                    Assert.That(tree.ActiveStructuralPruningFractionOutput, Is.EqualTo(0.0).Within(1e-12));
                });
            }
        }

        [Test]
        public void Thin_RemovesConfiguredLiveFruitFraction()
        {
            GenericFruitTree tree = CreateFruitManagementTree(liveFruitWt: 100.0);
            FruitOrgan fruit = (FruitOrgan)ReflectionUtilities.GetValueOfFieldOrProperty("fruitOrgan", tree);
            tree.ThinningFraction = 0.3;

            Utilities.CallMethod(tree, "OnThin", new object[] { tree, EventArgs.Empty });

            Assert.That(fruit.Live.Wt, Is.EqualTo(70.0).Within(1e-10));
        }

        [Test]
        public void Thin_DoesNothingForZeroOrNegativeThinningFraction()
        {
            foreach (double fraction in new[] { 0.0, -0.2 })
            {
                GenericFruitTree tree = CreateFruitManagementTree(liveFruitWt: 100.0);
                FruitOrgan fruit = (FruitOrgan)ReflectionUtilities.GetValueOfFieldOrProperty("fruitOrgan", tree);
                tree.ThinningFraction = fraction;

                Utilities.CallMethod(tree, "OnThin", new object[] { tree, EventArgs.Empty });

                Assert.That(fruit.Live.Wt, Is.EqualTo(100.0).Within(1e-12));
            }
        }

        [Test]
        public void WaterUptake_SumsAcrossZonesWithDifferentLayerCounts()
        {
            GenericFruitTree tree = new GenericFruitTree();
            tree.Uptakes.Add(new ZoneWaterAndN(new Zone()) { Water = new[] { 1.0, 2.0 } });
            tree.Uptakes.Add(new ZoneWaterAndN(new Zone()) { Water = new[] { 0.5, 1.5, 2.5 } });

            Assert.That(tree.WaterUptake, Is.EqualTo(new[] { 1.5, 3.5, 2.5 }));
        }

        [Test]
        public void NitrogenUptake_SumsNo3AndNh4AcrossZones()
        {
            GenericFruitTree tree = new GenericFruitTree();
            tree.Uptakes.Add(new ZoneWaterAndN(new Zone())
            {
                NO3N = new[] { 1.0, 2.0 },
                NH4N = new[] { 0.2, 0.3, 0.4 }
            });
            tree.Uptakes.Add(new ZoneWaterAndN(new Zone())
            {
                NO3N = new[] { 0.5, 1.5, 2.5 },
                NH4N = new[] { 0.1 }
            });

            Assert.That(tree.NitrogenUptake, Is.EqualTo(new[] { 1.8, 3.8, 2.9 }));
        }

        [Test]
        public void AboveGround_SumsLeafWoodAndFruitLiveAndDeadPools()
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 1, 15));
            PerennialLeaf leaf = CreateSeededLeaf(tree, liveLeafWt: 20.0, sla: 0.01, heightMm: 1500.0);
            ((Cohorts)ReflectionUtilities.GetValueOfFieldOrProperty("cohort", leaf)).KillLeavesUniformly(0.25);
            GenericOrgan wood = new GenericOrgan();
            wood.Live.StructuralWt = 30.0;
            wood.Live.StorageWt = 4.0;
            wood.Live.StructuralN = 1.0;
            wood.Live.StorageN = 0.2;
            wood.Dead.StructuralWt = 6.0;
            wood.Dead.StorageWt = 1.0;
            wood.Dead.StructuralN = 0.3;
            wood.Dead.StorageN = 0.1;
            FruitOrgan fruit = CreateFruitOrganForBiomass(tree, liveFruitWt: 12.0);
            fruit.Live.StorageWt = 3.0;
            fruit.Live.StructuralN = 0.6;
            fruit.Live.StorageN = 0.2;
            fruit.Dead.StructuralWt = 2.0;
            fruit.Dead.StorageWt = 1.0;
            fruit.Dead.StructuralN = 0.1;
            fruit.Dead.StorageN = 0.05;

            Utilities.InjectLink(tree, "leafOrgan", leaf);
            Utilities.InjectLink(tree, "woodOrgan", wood);
            Utilities.InjectLink(tree, "fruitOrgan", fruit);

            IBiomass aboveGround = tree.AboveGround;

            Assert.Multiple(() =>
            {
                Assert.That(aboveGround.StructuralWt + aboveGround.StorageWt, Is.EqualTo(20.0 + 30.0 + 4.0 + 6.0 + 1.0 + 12.0 + 3.0 + 2.0 + 1.0).Within(1e-12));
                Assert.That(aboveGround.StructuralN + aboveGround.StorageN, Is.EqualTo(0.4 + 1.0 + 0.2 + 0.3 + 0.1 + 0.6 + 0.2 + 0.1 + 0.05).Within(1e-12));
            });
        }

        [Test]
        public void DailyInitialisation_StoresVpdFromCalculator()
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 1, 15));
            Utilities.InjectLink(tree, "VPDCalculator", new ConstantFunction(4.2));

            Utilities.CallMethod(tree, "OnDailyInitialisation", new object[] { tree, EventArgs.Empty });

            Assert.That(tree.VPDOutput, Is.EqualTo(4.2).Within(1e-12));
        }

        [Test]
        public void Commencing_InitialisesLeafHeightConductanceDefaultsAndState()
        {
            GenericFruitTree tree = CreateCommencingTree(out PerennialLeaf leaf);
            tree.InitialHeight = 1.2;
            tree.DefaultGsmax350 = 0.02;
            tree.DefaultR50 = 180.0;
            leaf.Height = 0.0;
            leaf.Gsmax350 = 0.0;
            leaf.R50 = 0.0;
            Utilities.InjectLink(tree, "inLeafFallWindow", true);
            Utilities.InjectLink(tree, "reproductiveCycleActive", true);
            Utilities.InjectLink(tree, "lastReserveBalanceDate", new DateTime(2024, 1, 15));

            Utilities.CallMethod(tree, "OnSimulationCommencing", new object[] { tree, EventArgs.Empty });

            Assert.Multiple(() =>
            {
                Assert.That(leaf.Height, Is.GreaterThanOrEqualTo(1200.0));
                Assert.That(leaf.Gsmax350, Is.EqualTo(0.02).Within(1e-12));
                Assert.That(leaf.R50, Is.EqualTo(180.0).Within(1e-12));
                Assert.That(GetPrivate<bool>(tree, "inLeafFallWindow"), Is.False);
                Assert.That(GetPrivate<bool>(tree, "reproductiveCycleActive"), Is.False);
                Assert.That(GetPrivate<DateTime>(tree, "lastReserveBalanceDate"), Is.EqualTo(DateTime.MinValue));
            });
        }

        [Test]
        public void PlantSowing_ResetsLeafFallAndReserveCache()
        {
            GenericFruitTree tree = CreateCommencingTree(out _);
            Utilities.InjectLink(tree, "inLeafFallWindow", true);
            Utilities.InjectLink(tree, "leafFallProgress", 0.5);
            Utilities.InjectLink(tree, "reservesSeeded", true);
            Utilities.InjectLink(tree, "lastReserveBalanceDate", new DateTime(2024, 1, 15));

            Utilities.CallMethod(tree, "OnPlantSowing", new object[]
            {
                tree,
                new SowingParameters { Plant = tree }
            });

            Assert.Multiple(() =>
            {
                Assert.That(GetPrivate<bool>(tree, "inLeafFallWindow"), Is.False);
                Assert.That(GetPrivate<double>(tree, "leafFallProgress"), Is.EqualTo(0.0).Within(1e-12));
                Assert.That(GetPrivate<bool>(tree, "reservesSeeded"), Is.False);
                Assert.That(GetPrivate<DateTime>(tree, "lastReserveBalanceDate"), Is.EqualTo(DateTime.MinValue));
            });
        }

        [Test]
        public void OutputAliases_ReflectLinkedOrganState()
        {
            GenericFruitTree tree = new GenericFruitTree();
            PerennialLeaf leaf = new PerennialLeaf { Height = 1500.0, LAI = 2.3 };
            BudOrgan bud = new BudOrgan { NodeNumber = 6.5 };
            FruitOrgan fruit = new FruitOrgan();
            Utilities.InjectLink(fruit, "dailySetFlux_fruit_m2", 1.25);
            Utilities.InjectLink(fruit, "dailyDropFlux_fruit_m2", 0.4);

            Utilities.InjectLink(tree, "leafOrgan", leaf);
            Utilities.InjectLink(tree, "budOrgan", bud);
            Utilities.InjectLink(tree, "fruitOrgan", fruit);
            Utilities.SetProperty(tree, "SupplyIndexEffective", 0.42);

            tree.Height = 1800.0;

            Assert.Multiple(() =>
            {
                Assert.That(tree.Height, Is.EqualTo(1800.0).Within(1e-12));
                Assert.That(leaf.Height, Is.EqualTo(1800.0).Within(1e-12));
                Assert.That(tree.BudLoadPerAreaOutput, Is.EqualTo(6.5).Within(1e-12));
                Assert.That(tree.ReproSetRateOutput, Is.EqualTo(1.25).Within(1e-12));
                Assert.That(tree.ReproDropRateOutput, Is.EqualTo(0.4).Within(1e-12));
                Assert.That(tree.StressForReproOutput, Is.EqualTo(0.42).Within(1e-12));
            });
        }

        [Test]
        public void Height_UsesStoredStateWhenLeafOrganIsNotLinked()
        {
            GenericFruitTree tree = new GenericFruitTree();

            tree.Height = 1750.0;

            Assert.That(tree.Height, Is.EqualTo(1750.0).Within(1e-12));
        }

        [Test]
        public void PotentialEpAndWaterDemand_UseStoredStateWhenLeafOrganIsNotLinked()
        {
            GenericFruitTree tree = new GenericFruitTree();

            tree.PotentialEP = 4.2;
            tree.WaterDemand = 3.1;

            Assert.Multiple(() =>
            {
                Assert.That(tree.PotentialEP, Is.EqualTo(4.2).Within(1e-12));
                Assert.That(tree.WaterDemand, Is.EqualTo(3.1).Within(1e-12));
            });
        }

        [Test]
        public void GenericFruitTreeExample_LoadsAndResolvesCoreComponents()
        {
            string path = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..",
                "..",
                "..",
                "Examples",
                "Agroforestry",
                "GenericFruitTreeGenericOrchardExample.apsimx"));

            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();

            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            if (storage != null)
                storage.UseInMemoryDB = true;

            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);
            Utilities.ResolveLinks(sim);

            GenericFruitTree tree = sim.Node.FindChild<GenericFruitTree>(recurse: true);
            BudOrgan bud = sim.Node.FindChild<BudOrgan>(recurse: true);
            FruitOrgan fruit = sim.Node.FindChild<FruitOrgan>(recurse: true);
            PerennialMicroClimate microClimate = sim.Node.FindChild<PerennialMicroClimate>(recurse: true);

            Assert.Multiple(() =>
            {
                Assert.That(tree, Is.Not.Null);
                Assert.That(bud, Is.Not.Null);
                Assert.That(fruit, Is.Not.Null);
                Assert.That(microClimate, Is.Not.Null);
                Assert.That(() => tree.Height, Throws.Nothing);
                Assert.That(double.IsFinite(tree.Height), Is.True);
                Assert.That(double.IsFinite(tree.BudLoadPerAreaOutput), Is.True);
                Assert.That(double.IsFinite(tree.ReproSetRateOutput), Is.True);
                Assert.That(double.IsFinite(tree.ReproDropRateOutput), Is.True);
                Assert.That(double.IsFinite(tree.GetReserveStorageDemandDM()), Is.True);
            });
        }

        [Test]
        public void HarvestReadiness_CanResetPhenologyForRepeatedSeasonalCycles()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out Phenology phenology);
            TestClock clock = (TestClock)ReflectionUtilities.GetValueOfFieldOrProperty("clock", tree);

            tree.ResetPhenologyAfterHarvest = true;
            tree.DormancyStage = "Dormancy";
            tree.MaturityStage = "Maturity";

            MovePhenologyToStage(phenology, "Maturity");
            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(phenology.InPhase("Dormancy_BudBreak"), Is.True);
                Assert.That((bool)ReflectionUtilities.GetValueOfFieldOrProperty("isHarvested", tree), Is.False);
                Assert.That((bool)ReflectionUtilities.GetValueOfFieldOrProperty("inLeafFallWindow", tree), Is.True);
            });

            clock.Today = clock.Today.AddYears(1);
            MovePhenologyToStage(phenology, "Maturity");
            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(phenology.InPhase("Dormancy_BudBreak"), Is.True);
                Assert.That((bool)ReflectionUtilities.GetValueOfFieldOrProperty("isHarvested", tree), Is.False);
                Assert.That((bool)ReflectionUtilities.GetValueOfFieldOrProperty("inLeafFallWindow", tree), Is.True);
            });
        }

        [Test]
        public void HarvestReadiness_DoesNotResetPhenologyUnlessEnabled()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out Phenology phenology);

            tree.ResetPhenologyAfterHarvest = false;
            tree.MaturityStage = "Maturity";

            MovePhenologyToStage(phenology, "Maturity");
            Utilities.CallMethod(tree, "UpdateHarvestStatus", Array.Empty<object>());

            Assert.Multiple(() =>
            {
                Assert.That(phenology.InPhase("Maturity_Harvest"), Is.True);
                Assert.That((bool)ReflectionUtilities.GetValueOfFieldOrProperty("isHarvested", tree), Is.True);
            });
        }

        private static GenericFruitTree CreateLiveReserveTree(
            double supply,
            double leafDemand,
            double woodDemand,
            double structuralWood,
            double storageWood,
            double reserveCapacityFrac,
            double reserveStorageRate,
            double reserveMobilisationRate)
        {
            return CreateLiveReserveTree(
                new MutableFunction(supply),
                leafDemand,
                woodDemand,
                structuralWood,
                storageWood,
                reserveCapacityFrac,
                reserveStorageRate,
                reserveMobilisationRate,
                new DateTime(2024, 1, 15));
        }

        private class MutableFunction : Model, IFunction
        {
            public MutableFunction(double value)
            {
                ReturnValue = value;
            }

            public double ReturnValue { get; set; }

            public double Value(int arrayIndex = -1)
            {
                return ReturnValue;
            }
        }

        private static GenericFruitTree CreateLiveReserveTree(
            MutableFunction supply,
            double leafDemand,
            double woodDemand,
            double structuralWood,
            double storageWood,
            double reserveCapacityFrac,
            double reserveStorageRate,
            double reserveMobilisationRate,
            DateTime today)
        {
            PerennialLeaf leaf = new PerennialLeaf
            {
                DMDemand = new Models.PMF.Interfaces.BiomassPoolType { Structural = leafDemand },
                DMSupply = new Models.PMF.Interfaces.BiomassSupplyType()
            };
            GenericOrgan wood = new GenericOrgan
            {
                DMDemand = new Models.PMF.Interfaces.BiomassPoolType { Structural = woodDemand },
                DMSupply = new Models.PMF.Interfaces.BiomassSupplyType()
            };
            wood.Live.StructuralWt = structuralWood;
            wood.Live.StorageWt = storageWood;

            GenericFruitTree tree = new GenericFruitTree
            {
                ReserveCapacityFracOfWoodDM = reserveCapacityFrac,
                ReserveStorageRate = reserveStorageRate,
                ReserveMobilisationRate = reserveMobilisationRate,
                ReserveInitFrac = 0.0,
                Summary = new TestSummary()
            };

            SetPlantAlive(tree, true);
            Utilities.InjectLink(tree, "clock", new TestClock
            {
                Today = today,
                StartDate = new DateTime(today.Year, 1, 1),
                EndDate = new DateTime(today.Year, 12, 31)
            });
            Utilities.InjectLink(tree, "leafOrgan", leaf);
            Utilities.InjectLink(tree, "woodOrgan", wood);
            Utilities.InjectLink(tree, "leafPhotosynthesisFn", supply);
            return tree;
        }

        private static GenericFruitTree CreatePhaseEventTree(DateTime today)
        {
            GenericFruitTree tree = new GenericFruitTree
            {
                Name = "FruitTree",
                DormancyStage = "Dormancy",
                BudBreakStage = "BudBreak",
                MaturityStage = "Maturity",
                PlantType = "GenericFruitTree",
                Summary = new TestSummary()
            };

            SetPlantAlive(tree, true);
            Utilities.InjectLink(tree, "clock", new TestClock
            {
                Today = today,
                StartDate = new DateTime(today.Year, 1, 1),
                EndDate = new DateTime(today.Year, 12, 31)
            });
            return tree;
        }

        private static GenericFruitTree CreateDormancyTreeWithPhenology(out Phenology phenology)
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 7, 1));
            tree.ForcingStage = string.Empty;

            phenology = new Phenology { Name = "Phenology" };
            phenology.phases.Add(CreatePhase("Sowing", "Sowing", "Dormancy"));
            phenology.phases.Add(CreatePhase("Dormancy", "Dormancy", "Forcing"));
            phenology.phases.Add(CreatePhase("Forcing", "Forcing", "BudBreak"));
            phenology.phases.Add(CreatePhase("BudBreak", "BudBreak", "Maturity"));
            phenology.phases.Add(CreatePhase("Maturity", "Maturity", "Harvest"));

            Utilities.InjectLink(phenology, "plant", tree);
            Utilities.InjectLink(tree, "phenology", phenology);
            return tree;
        }

        private static GenericFruitTree CreateLeafFallTree(double liveLeafWt)
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 7, 1));
            Utilities.InjectLink(tree, "leafOrgan", CreateSeededLeaf(tree, liveLeafWt, sla: 0.01, heightMm: 1500.0));
            return tree;
        }

        private static GenericFruitTree CreateCanopyTree(double lai, double heightMm)
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 7, 1));
            PerennialLeaf leaf = CreateSeededLeaf(tree, liveLeafWt: 100.0, sla: lai / 100.0, heightMm: heightMm);
            Utilities.InjectLink(tree, "leafOrgan", leaf);
            return tree;
        }

        private static PerennialLeaf CreateSeededLeaf(GenericFruitTree tree, double liveLeafWt, double sla, double heightMm)
        {
            PerennialLeaf leaf = new PerennialLeaf
            {
                Name = "Leaf",
                Detached = new Biomass(),
                Removed = new Biomass(),
                Height = heightMm
            };
            leaf.Parent = tree;

            Cohorts cohorts = new Cohorts();
            cohorts.AddLeaf(liveLeafWt, minNConc: 0.01, maxNConc: 0.02, sla: sla);
            Utilities.InjectLink(leaf, "cohort", cohorts);

            BiomassRemoval biomassRemoval = new BiomassRemoval { Name = "BiomassRemoval" };
            biomassRemoval.Parent = leaf;
            Utilities.InjectLink(biomassRemoval, "plant", tree);
            Utilities.InjectLink(biomassRemoval, "surfaceOrganicMatter", new NoopSurfaceOrganicMatter());
            Utilities.InjectLink(biomassRemoval, "summary", new Summary { Verbosity = MessageType.Error });
            leaf.biomassRemovalModel = biomassRemoval;
            return leaf;
        }

        private static GenericFruitTree CreateHydraulicsTree(
            double rootDepthMm,
            double[] thicknessMm,
            double[] kCmPerHour,
            double[] pawMm,
            double[] pawcMm)
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 1, 15));
            SimplePhysical physical = new SimplePhysical(thicknessMm, pawcMm);
            SimpleSoilWater water = new SimpleSoilWater(thicknessMm, kCmPerHour, pawMm);
            ZoneState zoneState = (ZoneState)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ZoneState));
            zoneState.Depth = rootDepthMm;
            zoneState.Physical = physical;
            zoneState.WaterBalance = water;
            zoneState.plant = tree;
            Root root = new Root
            {
                PlantZone = zoneState
            };

            Utilities.InjectLink(tree, "soilPhysical", physical);
            Utilities.InjectLink(tree, "soilWater", water);
            Utilities.InjectLink(tree, "rootOrgan", root);
            return tree;
        }

        private static GenericFruitTree CreateHarvestReadyTree()
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out _);
            tree.HarvestByPhenology = false;
            tree.HarvestBrixThreshold = 0.0;
            tree.HarvestAcidThreshold = 0.0;
            Utilities.InjectLink(tree, "fruitDryMatter_g_m2", 25.0);
            return tree;
        }

        private static GenericFruitTree CreateQualityTree(out FruitOrgan fruit)
        {
            GenericFruitTree tree = CreateTreeWithPhenology(out Phenology phenology);
            tree.Summary = new TestSummary();
            tree.FruitSetStage = "FruitSet";
            tree.FruitFillStage = "FruitFill";
            Utilities.SetProperty(tree, "SupplyIndexEffective", 1.0);
            Utilities.InjectLink(tree, "fruitWaterFractionState", 0.80);

            fruit = new FruitOrgan
            {
                Name = "Fruit",
                Summary = new TestSummary(),
                DMDemand = new Models.PMF.Interfaces.BiomassPoolType(),
                NDemand = new Models.PMF.Interfaces.BiomassPoolType(),
                DMSupply = new Models.PMF.Interfaces.BiomassSupplyType(),
                NSupply = new Models.PMF.Interfaces.BiomassSupplyType(),
                potentialDMAllocation = new Models.PMF.Interfaces.BiomassPoolType(),
                Allocated = new Biomass(),
                Senesced = new Biomass(),
                Detached = new Biomass(),
                Removed = new Biomass()
            };
            fruit.Parent = tree;
            Utilities.InjectLink(fruit, "tree", tree);
            Utilities.InjectLink(tree, "fruitOrgan", fruit);
            Utilities.InjectLink(tree, "phenology", phenology);
            return tree;
        }

        private static GenericFruitTree CreateFruitManagementTree(double liveFruitWt)
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 1, 15));
            Utilities.InjectLink(tree, "fruitOrgan", CreateFruitOrganForBiomass(tree, liveFruitWt));
            return tree;
        }

        private static FruitOrgan CreateFruitOrganForBiomass(GenericFruitTree tree, double liveFruitWt)
        {
            FruitOrgan fruit = new FruitOrgan
            {
                Name = "Fruit",
                Summary = new TestSummary(),
                DMDemand = new Models.PMF.Interfaces.BiomassPoolType(),
                NDemand = new Models.PMF.Interfaces.BiomassPoolType(),
                DMSupply = new Models.PMF.Interfaces.BiomassSupplyType(),
                NSupply = new Models.PMF.Interfaces.BiomassSupplyType(),
                potentialDMAllocation = new Models.PMF.Interfaces.BiomassPoolType(),
                Allocated = new Biomass(),
                Senesced = new Biomass(),
                Detached = new Biomass(),
                Removed = new Biomass()
            };
            fruit.Parent = tree;
            fruit.Live.StructuralWt = liveFruitWt;

            BiomassRemoval biomassRemoval = new BiomassRemoval { Name = "BiomassRemoval" };
            biomassRemoval.Parent = fruit;
            Utilities.InjectLink(biomassRemoval, "plant", tree);
            Utilities.InjectLink(biomassRemoval, "surfaceOrganicMatter", new NoopSurfaceOrganicMatter());
            Utilities.InjectLink(biomassRemoval, "summary", new Summary { Verbosity = MessageType.Error });
            fruit.biomassRemovalModel = biomassRemoval;
            return fruit;
        }

        private static GenericFruitTree CreateCommencingTree(out PerennialLeaf leaf)
        {
            GenericFruitTree tree = CreatePhaseEventTree(new DateTime(2024, 1, 15));
            leaf = CreateSeededLeaf(tree, liveLeafWt: 10.0, sla: 0.01, heightMm: 0.0);
            leaf.Detached = null;
            leaf.Removed = null;
            Utilities.InjectLink(tree, "leafOrgan", leaf);
            return tree;
        }

        private static void TriggerPhaseChanged(GenericFruitTree tree, object sender, string stageName)
        {
            Utilities.CallMethod(tree, "OnPhaseChanged", new object[]
            {
                sender,
                new PhaseChangedType { StageName = stageName }
            });
        }

        private static T GetPrivate<T>(object model, string name)
        {
            return (T)ReflectionUtilities.GetValueOfFieldOrProperty(name, model);
        }

        private static void SetPlantAlive(GenericFruitTree tree, bool isAlive)
        {
            FieldInfo backingField = typeof(Plant).GetField("<IsAlive>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            backingField.SetValue(tree, isAlive);
        }

        private static GenericFruitTree CreateTreeWithPhenology(out Phenology phenology)
        {
            GenericFruitTree tree = new GenericFruitTree { Summary = new TestSummary() };
            SetPlantAlive(tree, true);

            phenology = new Phenology { Name = "Phenology" };
            phenology.phases.Add(CreatePhase("Sowing_Emergence", "Sowing", "Emergence"));
            phenology.phases.Add(CreatePhase("Emergence_Dormancy", "Emergence", "Dormancy"));
            phenology.phases.Add(CreatePhase("Dormancy_BudBreak", "Dormancy", "BudBreak"));
            phenology.phases.Add(CreatePhase("BudBreak_Maturity", "BudBreak", "Maturity"));
            phenology.phases.Add(CreatePhase("Maturity_Harvest", "Maturity", "Harvest"));

            Utilities.InjectLink(phenology, "plant", tree);
            Utilities.InjectLink(tree, "phenology", phenology);
            Utilities.InjectLink(tree, "clock", new TestClock
            {
                Today = new DateTime(2024, 9, 1),
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2025, 12, 31)
            });

            return tree;
        }

        private static GenericFruitTree CreateTreeWithBudBreakOnlyPhenology(out Phenology phenology)
        {
            GenericFruitTree tree = new GenericFruitTree { Summary = new TestSummary() };
            SetPlantAlive(tree, true);

            phenology = new Phenology { Name = "Phenology" };
            phenology.phases.Add(CreatePhase("Sowing_BudBreak", "Sowing", "BudBreak"));
            phenology.phases.Add(CreatePhase("BudBreak_Maturity", "BudBreak", "Maturity"));
            phenology.phases.Add(CreatePhase("Maturity_Harvest", "Maturity", "Harvest"));

            Utilities.InjectLink(phenology, "plant", tree);
            Utilities.InjectLink(tree, "phenology", phenology);
            Utilities.InjectLink(tree, "clock", new TestClock
            {
                Today = new DateTime(2024, 9, 1),
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2025, 12, 31)
            });

            return tree;
        }

        private static GenericPhase CreatePhase(string name, string start, string end)
        {
            GenericPhase phase = new GenericPhase
            {
                Name = name,
                Start = start,
                End = end
            };
            Utilities.InjectLink(phase, "target", new ConstantFunction(100.0));
            return phase;
        }

        private static void MovePhenologyToStage(Phenology phenology, string stageName)
        {
            phenology.SetToStage(stageName);
            phenology.Stage = phenology.StageNames.IndexOf(stageName) + 1;
        }

        private sealed class NoopSurfaceOrganicMatter : ISurfaceOrganicMatter
        {
            public double Cover => 0.0;

            public void Add(double mass, double N, double P, string type, string name, double fractionStanding = 0, double no3 = -1, double nh4 = -1)
            {
            }
        }

        private sealed class SimplePhysical : IPhysical
        {
            private readonly double[] pawcMm;

            public SimplePhysical(double[] thickness, double[] pawcMm)
            {
                Thickness = thickness;
                this.pawcMm = pawcMm;
                AirDry = new double[thickness.Length];
                BD = Enumerable.Repeat(1.2, thickness.Length).ToArray();
                DUL = new double[thickness.Length];
                KS = new double[thickness.Length];
                LL15 = new double[thickness.Length];
                ParticleSizeClay = new double[thickness.Length];
                ParticleSizeSand = new double[thickness.Length];
                ParticleSizeSilt = new double[thickness.Length];
                Rocks = new double[thickness.Length];
                SAT = new double[thickness.Length];
            }

            public double[] AirDry { get; set; }
            public double[] BD { get; set; }
            public double[] DUL { get; set; }
            public double[] DULmm => DUL.Zip(Thickness, (dul, thickness) => dul * thickness).ToArray();
            public double[] KS { get; set; }
            public double[] LL15 { get; set; }
            public double[] LL15mm => LL15.Zip(Thickness, (ll15, thickness) => ll15 * thickness).ToArray();
            public double[] ParticleSizeClay { get; set; }
            public double[] ParticleSizeSand { get; set; }
            public double[] ParticleSizeSilt { get; set; }
            public double[] Rocks { get; set; }
            public double[] SAT { get; set; }
            public double[] SATmm => SAT.Zip(Thickness, (sat, thickness) => sat * thickness).ToArray();
            public string[] Texture => Enumerable.Repeat(string.Empty, Thickness.Length).ToArray();
            public double[] Thickness { get; set; }
            public double[] ThicknessCumulative => Thickness.Select((_, i) => Thickness.Take(i + 1).Sum()).ToArray();
            public double[] DepthMidPoints => Thickness.Select((thickness, i) => Thickness.Take(i).Sum() + thickness / 2.0).ToArray();
            public double[] PAWC => pawcMm.Zip(Thickness, (pawc, thickness) => pawc / thickness).ToArray();
            public double[] PAWCmm => pawcMm;
        }

        private sealed class SimpleSoilWater : ISoilWater
        {
            public SimpleSoilWater(double[] thickness, double[] k, double[] pawMm)
            {
                Thickness = thickness;
                K = k;
                PAWmm = pawMm;
                SW = new double[thickness.Length];
                PoreInteractionIndex = new double[thickness.Length];
            }

            public double[] Thickness { get; }
            public double[] SW { get; set; }
            public double[] SWmm => SW.Zip(Thickness, (sw, thickness) => sw * thickness).ToArray();
            public double[] PSI => new double[Thickness.Length];
            public double[] K { get; set; }
            public double[] PoreInteractionIndex { get; set; }
            public double[] ESW => PAWmm;
            public double Eos => 0.0;
            public double Es => 0.0;
            public double Eo { get; set; }
            public double Runoff => 0.0;
            public double Drainage => 0.0;
            public double SubsurfaceDrain => 0.0;
            public double Pond => 0.0;
            public double Salb => 0.0;
            public double[] LateralOutflow => new double[Thickness.Length];
            public double LeachNO3 => 0.0;
            public double LeachNH4 => 0.0;
            public double LeachUrea => 0.0;
            public double LeachCl => 0.0;
            public double[] Flow => new double[Thickness.Length];
            public double[] Flux => new double[Thickness.Length];
            public double[] PAW => PAWmm.Zip(Thickness, (paw, thickness) => paw / thickness).ToArray();
            public double[] PAWmm { get; set; }
            public double PotentialInfiltration { get; set; }
            public double PrecipitationInterception { get; set; }
            public double WaterTable { get; set; }

            public void RemoveWater(double[] amountToRemove)
            {
            }

            public void SetWaterTable(double InitialDepth)
            {
                WaterTable = InitialDepth;
            }

            public void Reset()
            {
            }

            public void Tillage(TillageType Data)
            {
            }

            public void Tillage(string tillageType)
            {
            }
        }
    }
}
