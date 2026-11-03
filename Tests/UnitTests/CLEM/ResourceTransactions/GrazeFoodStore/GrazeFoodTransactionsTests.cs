using APSIM.Core;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.InkML;
using Models;
using Models.CLEM;
using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Storage;
using NUnit.Framework;
using System;
using System.IO;
using UnitTests.Properties;

namespace UnitTests.CLEM
{
    /// <summary>
    /// Test the application of details provided in the RuminantTypeCohort to create individuals in the RuminantHerd
    /// This does not Consider newborn individuals created from RuminantFemale.Births
    /// </summary>
    [TestFixture]
    public class GrazeFoodTransactionsTests
    {
        private Simulations simulations;
        private Simulation simulation;
        private GrazeFoodStoreType grazeFoodStore;
        private CropActivityManageProduct cropActivityManageProduct = new();
        private CLEMEvents clemEvents;

        /// <summary>
        /// Start up code for all tests.
        /// </summary>
        [SetUp]
        public void Initialise()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "UnitTests");
            Directory.CreateDirectory(tempFolder);
            Directory.SetCurrentDirectory(tempFolder);

            string str = ReflectionUtilities.GetResourceAsString("UnitTests.CLEM.Resources.SinglePasture.apsimx");
            simulations = FileFormat.ReadFromString<Simulations>(str).Model as Simulations;
            simulation = simulations.Node.FindChild<Simulation>();
            grazeFoodStore = simulation.Node.FindChild<GrazeFoodStoreType>(recurse: true);
            clemEvents = simulations.Node.FindChild<CLEMEvents>(recurse: true);
            Utilities.ResolveLinks(simulations);

            clemEvents.OnStartUnitTest();
        }

        [TearDown]
        public void Cleanup()
        {
            DataStore storage = simulations.Node.FindChild<DataStore>(recurse: true);
            storage?.Dispose();
        }

        [Test]
        public void ApsimXFileIsPresentTest()
        {
            Assert.That(simulations.Name, Is.EqualTo("Simulations"));
        }

        // add kg per ha to pasture as if read from data file in CropActivityManage
        [TestCase(1000)]
        public void AddResource_ByAmount(double kgTotal)
        {
            grazeFoodStore.AddToResource(kgTotal, cropActivityManageProduct, "None", "None");

            Assert.That(grazeFoodStore.Pools.Count, Is.EqualTo(1), actualExpression: "Number of pools");
            Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(kgTotal));
            Assert.That(grazeFoodStore.Pools[0].AgeInDays, Is.EqualTo(0), actualExpression: "New pool age");
        }

        // add kg per ha to pasture as if read from data file in CropActivityManage
        [TestCase(1000)]
        public void AddResourceInAnotherTimestep_ByAmount(double kgTotal)
        {
            grazeFoodStore.AddToResource(5.0, cropActivityManageProduct, "None", "None");
            grazeFoodStore.Pools[0].UpdateAge(clemEvents.TimeStepStart.AddMonths(-1), clemEvents.TimeStepStart);

            grazeFoodStore.AddToResource(kgTotal, cropActivityManageProduct, "None", "None");
            Assert.That(grazeFoodStore.Pools.Count, Is.EqualTo(2), actualExpression: "Number of pools");
            Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(kgTotal));
            Assert.That(grazeFoodStore.Pools[0].AgeInMonths, Is.EqualTo(0), actualExpression: "New pool age");

            Assert.That(grazeFoodStore.Pools[1].Amount, Is.EqualTo(5), actualExpression: "Previous pool amount");
            Assert.That(grazeFoodStore.Pools[1].AgeInMonths, Is.EqualTo(1), actualExpression: "Previous pool age");
        }

        // add kg per ha to pasture as if read from data file in CropActivityManage
        [TestCase(1000)]
        public void AddResource_ByGrazeFoodStorePool(double kgTotal)
        {
            GrazeFoodStorePool pool = new(kgTotal, grazeFoodStore, clemEvents.TimeStepStart.AddMonths(-1), clemEvents.TimeStepStart)
            {
                DryMatterDigestibility = 85,
                NitrogenPercent = 2.5,
            };

            grazeFoodStore.AddToResource(pool, cropActivityManageProduct, "None", "None");

            Assert.That(grazeFoodStore.Pools.Count, Is.EqualTo(1), actualExpression: "Number of pools");
            Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(kgTotal));
            Assert.That(grazeFoodStore.Pools[0].AgeInDays, Is.EqualTo(31), actualExpression: "New pool age");
            Assert.That(grazeFoodStore.Pools[0].DryMatterDigestibility, Is.EqualTo(85), actualExpression: "New pool dry matter digestibility");
            Assert.That(grazeFoodStore.Pools[0].NitrogenPercent, Is.EqualTo(2.5), actualExpression: "New pool nitrogen percent");
        }

        // add kg per ha to pasture as if read from data file in CropActivityManage
        [TestCase(1000)]
        public void AddResource_BySecondGrazeFoodStorePool(double kgTotal)
        {
            GrazeFoodStorePool pool = new(kgTotal, grazeFoodStore, clemEvents.TimeStepStart.AddMonths(-1), clemEvents.TimeStepStart)
            {
                DryMatterDigestibility = 80,
                NitrogenPercent = 2
            };

            grazeFoodStore.AddToResource(pool, cropActivityManageProduct, "None", "None");

            GrazeFoodStorePool pool2 = new(kgTotal * 2, grazeFoodStore, clemEvents.TimeStepStart.AddMonths(-1), clemEvents.TimeStepStart)
            {
                DryMatterDigestibility = 70,
                NitrogenPercent = 3
            };

            grazeFoodStore.AddToResource(pool2, cropActivityManageProduct, "None", "None");

            Assert.That(grazeFoodStore.Pools.Count, Is.EqualTo(2), actualExpression: "Number of pools");
            Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(kgTotal * 2));
            Assert.That(grazeFoodStore.Pools[0].AgeInMonths, Is.EqualTo(1), actualExpression: "Combined pool age");
            Assert.That(grazeFoodStore.Pools[0].DryMatterDigestibility, Is.EqualTo(70), actualExpression: "Combined pool dry matter digestibility");
            Assert.That(grazeFoodStore.Pools[0].NitrogenPercent, Is.EqualTo(3), actualExpression: "Combined pool nitrogen percent");
            Assert.That(grazeFoodStore.AmountAvailable, Is.EqualTo(3000), actualExpression: "Amount available");
        }

        // todo: now needs updating to ensure the additional details is an Ienumerable<Store>

        //// remove an amount from a two pool graze food store
        //[TestCase(300)]
        //public void RemoveResource_ByRequestNoPending(double kgTotal)
        //{
        //    double amountPool1 = 1000;
        //    double amountPool2 = 2000;

        //    GrazeFoodStorePool pool = new(amountPool1);

        //    grazeFoodStore.AddToResource(pool, cropActivityManageProduct, "None", "None");

        //    GrazeFoodStorePool pool2 = new(amountPool2);

        //    grazeFoodStore.AddToResource(pool2, cropActivityManageProduct, "None", "None");

        //    // remove amount
        //    ResourceRequest request = new()
        //    {
        //        Resource = grazeFoodStore,
        //        Required = kgTotal
        //    };

        //    grazeFoodStore.RemoveFromResource(request);
        //    Assert.That(grazeFoodStore.Pools.Count, Is.EqualTo(2), actualExpression: "Number of pools");
        //    Assert.That(grazeFoodStore.Pools[1].Amount, Is.EqualTo(amountPool1 - (kgTotal * (amountPool1 / amountPool2))));
        //    Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(amountPool2 - (kgTotal * (amountPool2 / amountPool1))));
        //    Assert.That(grazeFoodStore.AmountAvailable, Is.EqualTo(2700), actualExpression: "Amount available");
        //}
        //// remove an amount from a two pool graze food store with pending transactions
        //[TestCase(300)]
        //public void RemoveResource_ByRequestWithPending(double kgTotal)
        //{
        //    double amountPool1 = 1000;
        //    double amountPool2 = 2000;

        //    GrazeFoodStorePool pool = new(amountPool1);

        //    grazeFoodStore.AddToResource(pool, cropActivityManageProduct, "None", "None");

        //    GrazeFoodStorePool pool2 = new(amountPool2);

        //    grazeFoodStore.AddToResource(pool2, cropActivityManageProduct, "None", "None");

        //    // remove amount
        //    ResourceRequest request = new()
        //    {
        //        Resource = grazeFoodStore,
        //        Required = kgTotal,
        //        TransactionPending = true,

        //    };

        //    grazeFoodStore.RemoveFromResource(request);
        //    Assert.That(grazeFoodStore.AmountAvailable, Is.EqualTo(2700), actualExpression: "Amount available");
        //    Assert.That(grazeFoodStore.AmountPending, Is.EqualTo(kgTotal), actualExpression: "Amount pending");
        //    Assert.That(grazeFoodStore.AmountTotal, Is.EqualTo(3000), actualExpression: "Amount Total");

        //    Assert.That(grazeFoodStore.Pools[1].Amount, Is.EqualTo(amountPool1 - (kgTotal * (amountPool1 / amountPool2))));
        //    Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(amountPool2 - (kgTotal * (amountPool2 / amountPool1))));
        //}

        // create a three pool food store for testing
        public void CreateThreePoolStore(GrazeFoodStoreType type, double amount1, double amount2, double amount3, int age1, int age2, int age3)
        {
            GrazeFoodStorePool pool1 = new(amount1, type, clemEvents.TimeStepStart.AddDays(age1 * -1), clemEvents.TimeStepStart);
            type.AddToResource(pool1, cropActivityManageProduct, "None", "None");
            GrazeFoodStorePool pool2 = new(amount2, type, clemEvents.TimeStepStart.AddDays(age2 * -1), clemEvents.TimeStepStart);
            type.AddToResource(pool2, cropActivityManageProduct, "None", "None");
            GrazeFoodStorePool pool3 = new(amount3, type, clemEvents.TimeStepStart.AddDays(age3 * -1), clemEvents.TimeStepStart);
            type.AddToResource(pool3, cropActivityManageProduct, "None", "None");
            return;
        }

        /// <summary>
        /// Test detachment of pasture with negative rates
        /// </summary>
        /// <param name="detachRate">Detachment rate to use</param>
        /// <param name="carryoverDetachRate">Carryover detachment rate to use for pools 12 months or older</param>
        [TestCase(-0.02, 0.04)]
        [TestCase(0.02, -0.04)]
        [TestCase(-0.02, -0.04)]
        public void DetatchPasture_NegativeRates(double detachRate, double carryoverDetachRate)
        {
            grazeFoodStore.DetachRate = detachRate;
            grazeFoodStore.CarryoverDetachRate = carryoverDetachRate;
            GrazeFoodStorePool pool1 = new(100.0, grazeFoodStore, clemEvents.TimeStepStart.AddMonths(-1), clemEvents.TimeStepStart);
            grazeFoodStore.AddToResource(pool1, cropActivityManageProduct, "None", "None");

            Assert.That(() => grazeFoodStore.DetachPasture(10, 30.4), Throws.Exception);
        }

        [Test]
        public void DetatchPasture_PoolAgeLessThan12Months()
        {
            grazeFoodStore.DetachRate = 0.304; // 0.01 daily at 30.4 days in month
            grazeFoodStore.CarryoverDetachRate = 0.608; // 0,02 daily at 30.4 days in month
            GrazeFoodStorePool pool1 = new(100.0, grazeFoodStore, clemEvents.TimeStepStart.AddMonths(-6), clemEvents.TimeStepStart);
            grazeFoodStore.AddToResource(pool1, cropActivityManageProduct, "None", "None");

            grazeFoodStore.DetachPasture(10, 30.4);

            Assert.That(grazeFoodStore.AmountAvailable, Is.EqualTo(90.0), actualExpression: "Amount available");
            Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(90.0));
        }

        [Test]
        public void DetatchPasture_PoolAge12MonthsOrOlder()
        {
            grazeFoodStore.DetachRate = 0.304; // 0.01 daily at 30.4 days in month
            grazeFoodStore.CarryoverDetachRate = 0.608; // 0,02 daily at 30.4 days in month
            GrazeFoodStorePool pool1 = new(100.0, grazeFoodStore, clemEvents.TimeStepStart.AddMonths(-14), clemEvents.TimeStepStart);
            grazeFoodStore.AddToResource(pool1, cropActivityManageProduct, "None", "None");

            grazeFoodStore.DetachPasture(10, 30.4);

            Assert.That(grazeFoodStore.AmountAvailable, Is.EqualTo(80.0), actualExpression: "Amount available");
            Assert.That(grazeFoodStore.Pools[0].Amount, Is.EqualTo(80.0));
        }

        [Test]
        public void AgePasture()
        {
            GrazeFoodStorePool pool1 = new(100.0, grazeFoodStore, clemEvents.TimeStepStart, clemEvents.TimeStepStart);
            grazeFoodStore.AddToResource(pool1, cropActivityManageProduct, "None", "None");

            grazeFoodStore.AgePasture(10, 30.4);

            Assert.That(grazeFoodStore.Pools[0].AgeInDays, Is.EqualTo(30));
            Assert.That(grazeFoodStore.Pools[0].AgeInMonths, Is.EqualTo(1));
        }
    }
}
