namespace UnitTests.Stock
{
    using Models.Core.ApsimFile;
    using Models.GrazPlan;
    using Models.PMF;
    using NUnit.Framework;
    using System.IO;

    [TestFixture]
    public class StockTests
    {
        /// <summary>Make sure parameters with all values and some values missing work.</summary>
        [Test]
        public void TestFullAndPartialParametersAreRead()
        {
            var xml = "<parameters name=\"standard\" version=\"2.0\">" +
                      "  <par name=\"editor\">Andrew Moore</par>" +
                      "  <par name=\"edited\">30 Jan 2013</par>" +
                      "  <par name=\"dairy\">false</par>" +
                      "  <par name=\"c-srs-\">1.2,1.4</par>" +
                      "  <par name=\"c-i-\">,1.7,,,,25.0,22.0,,,,,0.15,,0.002,0.5,1.0,0.01,20.0,3.0,1.5</par>" +
                      "</parameters>";

            var animalParamSet = ConvertPRMToJson.Go(xml);

            Assert.AreEqual(animalParamSet.Command[0], "sEditor = Andrew Moore");
            Assert.AreEqual(animalParamSet.Command[1], "sEditDate = 30 Jan 2013");
            Assert.AreEqual(animalParamSet.Command[2], "bDairyBreed = False");
            Assert.AreEqual(animalParamSet.Command[3], "SRWScalars = 1.2,1.4");
            Assert.AreEqual(animalParamSet.Command[4], "IntakeC[3] = 1.7");
            Assert.AreEqual(animalParamSet.Command[5], "IntakeC[7] = 25.0");
            Assert.AreEqual(animalParamSet.Command[6], "IntakeC[8] = 22.0");
            Assert.AreEqual(animalParamSet.Command[7], "IntakeC[13] = 0.15");
            Assert.AreEqual(animalParamSet.Command[8], "IntakeC[15] = 0.002");
            Assert.AreEqual(animalParamSet.Command[9], "IntakeC[16] = 0.5");
            Assert.AreEqual(animalParamSet.Command[10], "IntakeC[17] = 1.0");
            Assert.AreEqual(animalParamSet.Command[11], "IntakeC[18] = 0.01");
            Assert.AreEqual(animalParamSet.Command[12], "IntakeC[19] = 20.0");
            Assert.AreEqual(animalParamSet.Command[13], "IntakeC[20] = 3.0");
            Assert.AreEqual(animalParamSet.Command[14], "IntakeC[21] = 1.5");
        }

        /// <summary>Make sure nested, hierarchical parameter sets are read.</summary>
        [Test]
        public void TestHierarchicalSetsAreRead()
        {
            var xml = "<parameters name=\"standard\" version=\"2.0\">" +
                      "  <par name=\"c-w-\">1.1,,</par>" +
                      "  <set name=\"small ruminants\">" +
                      "     <par name=\"c-w-\">,0.004,</par>" +
                      "     <set name=\"sheep\">" +
                      "        <par name=\"c-w-0\">0.108</par>" +
                      "     </set>" +
                      "  </set>" +
                      "</parameters>";

            var animalParamSet0 = ConvertPRMToJson.Go(xml);

            Assert.AreEqual(animalParamSet0.Name, "standard");
            Assert.AreEqual(animalParamSet0.Command[0], "WoolC[2] = 1.1");

            var animalParamSet1 = animalParamSet0.Children[0] as Cultivar;
            Assert.AreEqual(animalParamSet1.Name, "small ruminants");
            Assert.AreEqual(animalParamSet1.Command[0], "WoolC[3] = 0.004");

            var animalParamSet2 = animalParamSet1.Children[0] as Cultivar;
            Assert.AreEqual(animalParamSet2.Name, "sheep");
            Assert.AreEqual(animalParamSet2.Command[0], "WoolC[1] = 0.108");
        }

        /// <summary>Test that a genotype can be extracted from the stock resource file.</summary>
        [Test]
        public void GetGenotype()
        {
            var friesian = ConvertPRMToJson.GetGenotype("Friesian");
            Assert.AreEqual(friesian.BreedSRW, 550);
            Assert.AreEqual(friesian.bDairyBreed, true);
            Assert.AreEqual(friesian.IntakeLactC, new double[] { 0.0, 0.577, 0.9, 0.0 });
            Assert.AreEqual(friesian.GrowthC, new double[] { 0.0, 0.0115, 0.27, 0.4, 1.1 });
            Assert.AreEqual(friesian.SelfWeanPropn, 0.05);
        }

        /// <summary>Make sure nested, hierarchical parameter sets are read.</summary>
        [Test]
        public void GenerateRuminantJSON()
        {
            var xml = File.ReadAllText(@"C:\Users\holzworthdp\Work\Repos\ApsimX\Models\Resources\ruminant.prm");

            var genotype = ConvertPRMToJson.Go(xml);

            File.WriteAllText(@"C:\Users\holzworthdp\Work\Repos\ApsimX\Models\Resources\Ruminant.json",
                              FileFormat.WriteToString(genotype));
        }

    }
}
