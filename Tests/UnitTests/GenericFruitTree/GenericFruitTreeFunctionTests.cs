using System;
using System.Reflection;
using Models;
using Models.Agroforestry;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using NUnit.Framework;

namespace UnitTests.Agroforestry
{
    [TestFixture]
    public class VPDCalculatorTests
    {
        [Test]
        public void Value_ReturnsWeightedSpecificVpdFromWeatherInputs()
        {
            TestWeather weather = new TestWeather
            {
                MinT = 10.0,
                MaxT = 30.0,
                VP = 12.0
            };
            VPDCalculator calculator = new VPDCalculator();
            Utilities.InjectLink(calculator, "weather", weather);

            double expected = WeightedSpecificVpd(weather.MinT, weather.MaxT, weather.VP);

            Assert.That(calculator.Value(), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void Value_ClampsNegativeVpdComponentsToZero()
        {
            TestWeather weather = new TestWeather
            {
                MinT = 5.0,
                MaxT = 10.0,
                VP = 50.0
            };
            VPDCalculator calculator = new VPDCalculator();
            Utilities.InjectLink(calculator, "weather", weather);

            Assert.That(calculator.Value(), Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void Value_IsFiniteAndNonNegativeForColdAndHotTemperatures()
        {
            TestWeather weather = new TestWeather
            {
                MinT = -10.0,
                MaxT = 45.0,
                VP = 10.0
            };
            VPDCalculator calculator = new VPDCalculator();
            Utilities.InjectLink(calculator, "weather", weather);

            double value = calculator.Value();

            Assert.That(double.IsFinite(value), Is.True);
            Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
        }

        [Test]
        public void Value_IgnoresArrayIndex()
        {
            TestWeather weather = new TestWeather
            {
                MinT = 10.0,
                MaxT = 30.0,
                VP = 12.0
            };
            VPDCalculator calculator = new VPDCalculator();
            Utilities.InjectLink(calculator, "weather", weather);

            Assert.That(calculator.Value(3), Is.EqualTo(calculator.Value()).Within(1e-12));
        }

        private static double WeightedSpecificVpd(double minT, double maxT, double vp)
        {
            const double SvpA = 6.106;
            const double SvpB = 17.27;
            const double SvpC = 237.3;
            const double Weight = 0.66;

            double Svp(double temperature) => SvpA * Math.Exp(SvpB * temperature / (temperature + SvpC));

            double vpdMin = Math.Max(0.0, Svp(minT) - vp);
            double vpdMax = Math.Max(0.0, Svp(maxT) - vp);
            return Weight * vpdMax + (1.0 - Weight) * vpdMin;
        }
    }

    [TestFixture]
    public class GenericFruitTreeFunctionTests
    {
        [Test]
        public void ReserveMobilisationFactorFunction_ReturnsZeroWhenTreeMissingOrDead()
        {
            ReserveMobilisationFactorFunction missingTreeFunction = new ReserveMobilisationFactorFunction();
            ReserveMobilisationFactorFunction deadTreeFunction = new ReserveMobilisationFactorFunction
            {
                Tree = new GenericFruitTree()
            };

            Assert.That(missingTreeFunction.Value(), Is.EqualTo(0.0).Within(1e-12));
            Assert.That(deadTreeFunction.Value(), Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void ReserveMobilisationFactorFunction_UsesTreeReserveSignalForLiveTree()
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

            ReserveMobilisationFactorFunction function = new ReserveMobilisationFactorFunction
            {
                Tree = tree
            };

            double expected = 2.0 / (7.0 + 1e-9);

            Assert.That(function.Value(), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void ReserveMobilisationFactorFunction_Value_IgnoresArrayIndexAndReturnsTreeSignal()
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
            ReserveMobilisationFactorFunction function = new ReserveMobilisationFactorFunction
            {
                Tree = tree
            };

            double expected = tree.GetReserveDMRetranslocationFactor();

            Assert.That(function.Value(), Is.EqualTo(expected).Within(1e-12));
            Assert.That(function.Value(5), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void ReserveStorageDemandFunction_ReturnsZeroWhenTreeMissingOrDead()
        {
            ReserveStorageDemandFunction missingTreeFunction = new ReserveStorageDemandFunction();
            ReserveStorageDemandFunction deadTreeFunction = new ReserveStorageDemandFunction
            {
                Tree = new GenericFruitTree()
            };

            Assert.That(missingTreeFunction.Value(), Is.EqualTo(0.0).Within(1e-12));
            Assert.That(deadTreeFunction.Value(), Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void ReserveStorageDemandFunction_UsesTreeReserveSignalForLiveTree()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 8.0,
                leafDemand: 3.0,
                woodDemand: 1.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2);

            ReserveStorageDemandFunction function = new ReserveStorageDemandFunction
            {
                Tree = tree
            };

            Assert.That(function.Value(), Is.EqualTo(4.0).Within(1e-12));
        }

        [Test]
        public void ReserveStorageDemandFunction_Value_IgnoresArrayIndexAndReturnsTreeSignal()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 8.0,
                leafDemand: 3.0,
                woodDemand: 1.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2);
            ReserveStorageDemandFunction function = new ReserveStorageDemandFunction
            {
                Tree = tree
            };

            double expected = tree.GetReserveStorageDemandDM();

            Assert.That(function.Value(), Is.EqualTo(expected).Within(1e-12));
            Assert.That(function.Value(5), Is.EqualTo(expected).Within(1e-12));
        }

        [Test]
        public void ReserveStorageNDemandFunction_ReturnsClampedBaseDemandWhenDependenciesAreMissing()
        {
            ReserveStorageNDemandFunction function = new ReserveStorageNDemandFunction
            {
                BaseDemand = new ConstantFunction(-2.0)
            };

            Assert.That(function.Value(), Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void ReserveStorageNDemandFunction_ReturnsBaseDemandWhenWoodMinNConcIsZero()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 8.0,
                leafDemand: 3.0,
                woodDemand: 1.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2);
            GenericOrgan wood = CreateWoodOrgan(minimumNConc: 0.0);

            ReserveStorageNDemandFunction function = new ReserveStorageNDemandFunction
            {
                BaseDemand = new ConstantFunction(0.3),
                Tree = tree,
                Wood = wood
            };

            Assert.That(function.Value(), Is.EqualTo(0.3).Within(1e-12));
        }

        [Test]
        public void ReserveStorageNDemandFunction_ReturnsGreaterOfBaseAndDerivedDemand()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 8.0,
                leafDemand: 3.0,
                woodDemand: 1.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2);
            GenericOrgan wood = CreateWoodOrgan(minimumNConc: 0.2);

            ReserveStorageNDemandFunction function = new ReserveStorageNDemandFunction
            {
                BaseDemand = new ConstantFunction(0.3),
                Tree = tree,
                Wood = wood
            };

            Assert.That(function.Value(), Is.EqualTo(0.8).Within(1e-12));
        }

        [Test]
        public void ReserveStorageNDemandFunction_Value_UsesArrayIndexWhenCallingBaseDemand()
        {
            RecordingFunction baseDemand = new RecordingFunction(0.3);
            ReserveStorageNDemandFunction function = new ReserveStorageNDemandFunction
            {
                BaseDemand = baseDemand
            };

            function.Value(7);

            Assert.That(baseDemand.LastArrayIndex, Is.EqualTo(7));
        }

        [Test]
        public void ReserveStorageNDemandFunction_Value_ReturnsBaseDemandWhenTreeIsDeadEvenIfWoodIsPresent()
        {
            ReserveStorageNDemandFunction function = new ReserveStorageNDemandFunction
            {
                BaseDemand = new ConstantFunction(0.5),
                Tree = new GenericFruitTree(),
                Wood = CreateWoodOrgan(minimumNConc: 0.2)
            };

            Assert.That(function.Value(), Is.EqualTo(0.5).Within(1e-12));
        }

        [Test]
        public void ReserveStorageNDemandFunction_Value_ClampsNegativeWoodMinimumNConcToZero()
        {
            GenericFruitTree tree = CreateLiveReserveTree(
                supply: 8.0,
                leafDemand: 3.0,
                woodDemand: 1.0,
                structuralWood: 100.0,
                storageWood: 10.0,
                reserveCapacityFrac: 0.5,
                reserveStorageRate: 0.0,
                reserveMobilisationRate: 0.2);
            ReserveStorageNDemandFunction function = new ReserveStorageNDemandFunction
            {
                BaseDemand = new ConstantFunction(0.3),
                Tree = tree,
                Wood = CreateWoodOrgan(minimumNConc: -0.2)
            };

            Assert.That(function.Value(), Is.EqualTo(0.3).Within(1e-12));
        }

        [Test]
        public void LeafMinNConcDemandFunction_ReturnsBaseDemandWhenLeafMissing()
        {
            LeafMinNConcDemandFunction function = new LeafMinNConcDemandFunction
            {
                BaseDemand = new ConstantFunction(0.4)
            };

            Assert.That(function.Value(), Is.EqualTo(0.4).Within(1e-12));
        }

        [Test]
        public void LeafMinNConcDemandFunction_Value_ClampsNegativeBaseDemandToZeroWhenLeafMissing()
        {
            LeafMinNConcDemandFunction function = new LeafMinNConcDemandFunction
            {
                BaseDemand = new ConstantFunction(-2.0)
            };

            Assert.That(function.Value(), Is.EqualTo(0.0).Within(1e-12));
        }

        [Test]
        public void LeafMinNConcDemandFunction_ReturnsBaseDemandWhenMinimumNConcIsZero()
        {
            PerennialLeaf leaf = new PerennialLeaf
            {
                DMDemand = new BiomassPoolType { Structural = 5.0 },
                MinimumNConc = new ConstantFunction(0.0)
            };
            LeafMinNConcDemandFunction function = new LeafMinNConcDemandFunction
            {
                BaseDemand = new ConstantFunction(0.4),
                Leaf = leaf
            };

            Assert.That(function.Value(), Is.EqualTo(0.4).Within(1e-12));
        }

        [Test]
        public void LeafMinNConcDemandFunction_ReturnsGreaterOfBaseAndMinimumConcentrationDemand()
        {
            PerennialLeaf leaf = new PerennialLeaf
            {
                DMDemand = new BiomassPoolType { Structural = 5.0 },
                MinimumNConc = new ConstantFunction(0.2)
            };
            LeafMinNConcDemandFunction function = new LeafMinNConcDemandFunction
            {
                BaseDemand = new ConstantFunction(0.4),
                Leaf = leaf
            };

            Assert.That(function.Value(), Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void LeafMinNConcDemandFunction_Value_ClampsNegativeStructuralDMDemandToZero()
        {
            PerennialLeaf leaf = new PerennialLeaf
            {
                DMDemand = new BiomassPoolType { Structural = -5.0 },
                MinimumNConc = new ConstantFunction(0.2)
            };
            LeafMinNConcDemandFunction function = new LeafMinNConcDemandFunction
            {
                BaseDemand = new ConstantFunction(0.0),
                Leaf = leaf
            };

            Assert.That(function.Value(), Is.GreaterThanOrEqualTo(0.0));
        }

        [Test]
        public void LeafMinNConcDemandFunction_Value_UsesArrayIndexWhenCallingBaseDemand()
        {
            RecordingFunction baseDemand = new RecordingFunction(0.4);
            LeafMinNConcDemandFunction function = new LeafMinNConcDemandFunction
            {
                BaseDemand = baseDemand
            };

            function.Value(7);

            Assert.That(baseDemand.LastArrayIndex, Is.EqualTo(7));
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
            PerennialLeaf leaf = new PerennialLeaf
            {
                DMDemand = new BiomassPoolType { Structural = leafDemand }
            };
            GenericOrgan wood = new GenericOrgan
            {
                DMDemand = new BiomassPoolType { Structural = woodDemand }
            };
            wood.Live.StructuralWt = structuralWood;
            wood.Live.StorageWt = storageWood;

            GenericFruitTree tree = new GenericFruitTree
            {
                ReserveCapacityFracOfWoodDM = reserveCapacityFrac,
                ReserveStorageRate = reserveStorageRate,
                ReserveMobilisationRate = reserveMobilisationRate,
                ReserveInitFrac = 0.0
            };

            SetPlantAlive(tree, true);
            Utilities.InjectLink(tree, "clock", new TestClock
            {
                Today = new DateTime(2024, 1, 15),
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31)
            });
            Utilities.InjectLink(tree, "leafOrgan", leaf);
            Utilities.InjectLink(tree, "woodOrgan", wood);
            Utilities.InjectLink(tree, "leafPhotosynthesisFn", new ConstantFunction(supply));
            return tree;
        }

        private static void SetPlantAlive(GenericFruitTree tree, bool isAlive)
        {
            FieldInfo backingField = typeof(Plant).GetField("<IsAlive>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            backingField.SetValue(tree, isAlive);
        }

        private static GenericOrgan CreateWoodOrgan(double minimumNConc)
        {
            GenericOrgan wood = new GenericOrgan();
            Utilities.InjectLink(wood, "minimumNConc", new ConstantFunction(minimumNConc));
            return wood;
        }
    }
}
