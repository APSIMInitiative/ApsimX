namespace UnitTests.Stock
{
    using Models.Core.ApsimFile;
    using Models.GrazPlan;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class StockTests
    {

        [Test]
        public void TestPrmConversion()
        {
            var jsonSt = ConvertPRMToJson.Go();
            var json = JObject.Parse(jsonSt);

            var children = json["Children"] as JArray;
            var animalParamSet = children[0];

            Assert.AreEqual(animalParamSet["sEditDate"].ToString(), "30 Jan 2013");
            Assert.AreEqual(animalParamSet["MaxYoung"].Value<int>(), 1);

            File.WriteAllText(@"C:\Users\holzworthdp\Work\Repos\ApsimX\Models\Resources\Ruminant.json", jsonSt);
        }

    }
}
