using System;
using System.Collections.Generic;
using System.IO;
using APSIM.Core;
using APSIM.Documentation;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using NUnit.Framework;

namespace UnitTests.Factorial
{
    /// <summary>This is a test class for the FactorsFromFile class</summary>
    [TestFixture]
    public class FactorsFromFileTests
    {
        /// <summary></summary>
        [Test]
        public void EnsurePropertySetsWork()
        {
            string csv = "";
            csv += "Site,[Zone].Area\n";
            csv += "A,1\n";
            csv += "B,2\n";
            string filename = Path.ChangeExtension(Path.GetTempFileName(), ".csv");
            File.WriteAllText(filename, csv);
            
            FactorFromFile factorsFromFile = new FactorFromFile();
            factorsFromFile.FactorName = "Site";
            factorsFromFile.LabelColumn = "Site";
            factorsFromFile.FileName = filename;

            Factors factors = new Factors();
            factors.AddChild(factorsFromFile);
            Simulations simulations = Utilities.GetRunnableSim(useInMemoryDb: true);
            Experiment experiment = new Experiment();
            experiment.AddChild(factors);
            experiment.AddChild(simulations.Children[1] as INodeModel);
            simulations.RemoveChild(simulations.Children[1] as INodeModel);
            simulations.AddChild(experiment);

            simulations.Node = Node.Create(simulations);

            Runner runner = new Runner(simulations);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new AggregateException("Errors: ", errors);

            Assert.That(simulations.Node.FindChild<FactorFromFile>(recurse:true), Is.Not.Null);
            Factor generatedFactor = simulations.Node.FindChild<Factor>("Site", recurse:true);
            Assert.That(generatedFactor, Is.Not.Null);
            Assert.That(generatedFactor.ReadOnly, Is.True);
            Assert.That(generatedFactor.Children.Count, Is.EqualTo(2));
            Assert.That((generatedFactor.Children[0] as CompositeFactor).Name, Is.EqualTo("A"));
            Assert.That((generatedFactor.Children[0] as CompositeFactor).Specifications[0], Contains.Substring("1"));
            Assert.That((generatedFactor.Children[1] as CompositeFactor).Name, Is.EqualTo("B"));
            Assert.That((generatedFactor.Children[1] as CompositeFactor).Specifications[0], Contains.Substring("2"));
        }

    }
}
