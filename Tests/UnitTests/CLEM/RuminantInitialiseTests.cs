using Models;
using Models.CLEM.Resources;
using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.CLEM
{
    /// <summary>
    /// Test the application of details provided in the RuminantTypeCohort to create individuals in the RuminantHerd
    /// This does not oconsider newborn individuals created from RuminantFemale.Births
    /// </summary>
    [TestFixture]
    public class RuminantInitialiseTests
    {
        private Simulations singleSheepSim;
        private RuminantTypeCohort sheepCohort;
        private RuminantHerd sheepHerd;

        [SetUp]
        public void SetUp()
        {
            singleSheepSim = Utilities.ReadFromResource<Simulations>("UnitTests.CLEM.Resources.SingleSheep.apsimx", e => throw e);
            sheepCohort = singleSheepSim.FindAllAncestors<RuminantTypeCohort>().First();
            sheepHerd = singleSheepSim.FindAllAncestors<RuminantHerd>().First();
        }

        // Setting initial weight with options

        [TestCase(5)]
        [TestCase(50)]
        [TestCase(100)]
        public void InitialWeight_UserProvided(double initialWt)
        {
            sheepCohort.AgeDetails = new int[] { 10 };
            sheepCohort.Weight = initialWt;
            Utilities.RunModels(singleSheepSim, $"/ListSimulations");

            Assert.That(sheepHerd.Herd[0].Weight.Live, Is.EqualTo(initialWt));
            Assert.That(sheepHerd.Herd[0].Weight.Live, Is.Not.EqualTo(0));
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void InitialWeight_ZeroProvided(double initialWt)
        {
            sheepCohort.AgeDetails = new int[] { 10 };
            sheepCohort.Weight = initialWt;
            Utilities.RunModels(singleSheepSim, $"/ListSimulations");

            // should fall back to using normalised weight for age
            Assert.That(sheepHerd.Herd[0].Weight.NormalisedForAge, Is.EqualTo(sheepHerd.Herd[0].Weight.Live));
            Assert.That(sheepHerd.Herd[0].Weight.Live, Is.Not.EqualTo(0));
        }

        [Test]
        public void NewBornInitialWeight_UserProvided()
        {
            sheepCohort.AgeDetails = new int[] { 0 };
            sheepCohort.Weight = 10;
            Utilities.RunModels(singleSheepSim, $"/ListSimulations");

            Assert.That(sheepHerd.Herd[0].Weight.Live, Is.EqualTo(10));
            Assert.That(sheepHerd.Herd[0].Weight.AtBirth, Is.EqualTo(10));
            Assert.That(sheepHerd.Herd[0].Weight.Live, Is.Not.EqualTo(0));
        }

        // Setting weaning status based on natural weaning

        [Test]
        public void Age_BelowNaturalWean()
        {
            var sheepType = sheepHerd.FindAllChildren<RuminantType>().First();
            var natwean = sheepType.Parameters.General.NaturalWeaningAge.InDays - 1;
            sheepCohort.AgeDetails = new int[] { natwean };
            sheepCohort.Weight = 10;
            Utilities.RunModels(singleSheepSim, $"/ListSimulations");

            Assert.That(sheepHerd.Herd[0].IsWeaned, Is.EqualTo(false));
            Assert.That(sheepHerd.Herd[0].AgeInDays, Is.EqualTo(natwean));
        }

        [Test]
        public void Age_AboveNaturalWean()
        {
            var sheepType = sheepHerd.FindAllChildren<RuminantType>().First();
            var natwean = sheepType.Parameters.General.NaturalWeaningAge.InDays;
            sheepCohort.AgeDetails = new int[] { natwean };
            sheepCohort.Weight = 10;
            Utilities.RunModels(singleSheepSim, $"/ListSimulations");

            Assert.That(sheepHerd.Herd[0].IsWeaned, Is.EqualTo(true));
            Assert.That(sheepHerd.Herd[0].AgeInDays, Is.EqualTo(natwean));
        }

        // Setting birth date and age

        [TestCase(1, 2000, 1, 10)] // 1 day
        [TestCase(0, 2000, 1, 10)] // Newborn
        [TestCase(10, 1999, 12, 31)] // Previous month
        public void BirthDate_FromSetAge(int age, int yr, int mth, int day)
        {
            sheepCohort.AgeDetails = new int[] { age };
            sheepCohort.Weight = 10;
            Utilities.RunModels(singleSheepSim, $"/ListSimulations");

            // singleSheep has start date of 2000-01-10
            // individual should be that many days old at start of this date (start of simulation)
            Assert.That(sheepHerd.Herd[0].DateOfBirth, Is.EqualTo(new DateTime(yr, mth, day)));
            Assert.That(sheepHerd.Herd[0].DateEnteredSimulation, Is.EqualTo(new DateTime(2000, 1, 10)));
        }

        // fat and protein allocation

        // not needed

        // from kg

        // from E

        // from rel cond

        // 

    }
}
