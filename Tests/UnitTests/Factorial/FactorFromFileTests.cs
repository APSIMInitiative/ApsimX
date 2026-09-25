using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Core;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using NUnit.Framework;

namespace UnitTests.Factorial
{
    /// <summary>This is a test class for the FactorFromFile class</summary>
    [TestFixture]
    public class FactorFromFileTest
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
            
            FactorFromFile factorFromFile = new FactorFromFile();
            factorFromFile.Name = "Site";
            factorFromFile.NameColumn = "Site";
            factorFromFile.FileName = filename;

            Factors factors = new Factors();
            factors.AddChild(factorFromFile);
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

            List<SimulationDescription> sims = experiment.GenerateSimulationDescriptions();
            Assert.That(sims[0].Name, Is.EqualTo("ExperimentSiteA"));
            Assert.That(sims[1].Name, Is.EqualTo("ExperimentSiteB"));

            string[] commands = factorFromFile.Code.ToArray();
            Assert.That(commands[0], Is.EqualTo("add new CompositeFactor to [Site] name A"));
            Assert.That(commands[1], Is.EqualTo("[Site].A.Specifications += [Zone].Area=1"));
            Assert.That(commands[2], Is.EqualTo("add new CompositeFactor to [Site] name B"));
            Assert.That(commands[3], Is.EqualTo("[Site].B.Specifications += [Zone].Area=2"));
        }

    }
}
