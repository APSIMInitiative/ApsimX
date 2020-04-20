namespace UnitTests.Stock
{
    using Models.Core.ApsimFile;
    using Models.GrazPlan;
    using Models.PMF;
    using NUnit.Framework;
    using System.IO;
    using System.Linq;

    [TestFixture]
    public class StockTests
    {
        /// <summary>Make sure parameters with all values and some values missing work.</summary>
        [Test]
        public void TestReadingPRM()
        {
            var xml = "<parameters name=\"standard\" version=\"2.0\">" +
                      "  <par name=\"editor\">Andrew Moore</par>" +
                      "  <par name=\"edited\">30 Jan 2013</par>" +
                      "  <par name=\"dairy\">false</par>" +
                      "  <par name=\"c-srs-\">1.2,1.4</par>" +
                      "  <par name=\"c-i-\">,1.7,,,,25.0,22.0,,,,,0.15,,0.002,0.5,1.0,0.01,20.0,3.0,1.5</par>" +
                      "  <par name=\"c-w-\">1.1,,</par>" +
                      "  <set name=\"small ruminants\">" +
                      "     <par name=\"c-w-\">,0.004,</par>" +
                      "     <set name=\"sheep\">" +
                      "        <par name=\"c-w-0\">0.999</par>" +
                      "     </set>" +
                      "  </set>" +
                      "</parameters>";
            var genotypes = new Genotypes();
            genotypes.LoadPRMXml(xml);
            var animalParamSet = genotypes.GetGenotype("sheep");

            Assert.AreEqual(animalParamSet.sEditor, "Andrew Moore");
            Assert.AreEqual(animalParamSet.sEditDate, "30 Jan 2013");
            Assert.AreEqual(animalParamSet.bDairyBreed, false);
            Assert.AreEqual(animalParamSet.SRWScalars, new double[] { 1.2, 1.4 });
            Assert.AreEqual(animalParamSet.IntakeC, new double[] { 0, 0, 1.7, 0, 0, 0, 25.0, 22.0, 0, 0, 0, 0, 0.15, 0, 0.002, 0.5, 1.0, 0.01, 20, 3, 1.5 });
            Assert.AreEqual(animalParamSet.WoolC, new double[] { 0.999, 1.1, 0.004, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        }

        /// <summary>Test that a genotype can be extracted from the stock resource file.</summary>
        [Test]
        public void GetStandardGenotype()
        {
            var genotypes = new Genotypes();
            var friesian = genotypes.GetGenotype("Friesian");
            Assert.AreEqual(friesian.BreedSRW, 550);
            Assert.AreEqual(friesian.bDairyBreed, true);
            Assert.AreEqual(friesian.IntakeLactC, new double[] { 0.0, 0.577, 0.9, 0.0 });
            Assert.AreEqual(friesian.GrowthC, new double[] { 0.0, 0.0115, 0.27, 0.4, 1.1 });
            Assert.AreEqual(friesian.SelfWeanPropn, 0.05);
        }

        /// <summary>Ensure that a user supplied genotype overrides a standard one.</summary>
        [Test]
        public void EnsureUserGenotypeOverridesStandardGenotype()
        {
            // Get a friesian genotype.
            var genotypes = new Genotypes();
            var friesian = genotypes.GetGenotype("Friesian");

            // Change it.
            friesian.BreedSRW = 1;

            // Give it to the genotypes instance as a user genotype.
            genotypes.SetUserGenotypes(new AnimalParamSet[] { friesian });

            // Now ask for friesian again. This time it should return the user genotype, not the standard one.
            friesian = genotypes.GetGenotype("Friesian");

            Assert.AreEqual(friesian.BreedSRW, 1);
        }

        /// <summary>Ensure there are no dot characters in genotype names.</summary>
        [Test]
        public void EnsureNoDotsInGenotypeNames()
        {
            var genotypes = new Genotypes();
            var allGenotypes = genotypes.GetGenotypes();
            foreach (var genotypeName in allGenotypes.Select(genotype => genotype.Name))
                Assert.IsFalse(genotypeName.Contains("."));
        }

        ///// <summary>Make sure nested, hierarchical parameter sets are read.</summary>
        //[Test]
        //public void GenerateRuminantJSON()
        //{
        //    var xml = File.ReadAllText(@"C:\Users\holzworthdp\Work\Repos\ApsimX\Models\Resources\ruminant.prm");

        //    var genotypes = new Genotypes();
        //    var topLevelGenotype = genotypes.LoadPRMXml(xml);

        //    File.WriteAllText(@"C:\Users\holzworthdp\Work\Repos\ApsimX\Models\Resources\Ruminant.json",
        //                      FileFormat.WriteToString(topLevelGenotype));
        //}

    }
}
