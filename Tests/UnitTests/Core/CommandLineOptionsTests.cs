using APSIM.Shared.Utilities;
using Models.Core;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Core
{
    [TestFixture]
    public class CommandLineOptionsTests
    {
        /// <summary>
        /// Test the /SimulationNameRegexPattern option (and the /Verbose option as well,
        /// technically. This isn't really ideal but it makes things simpler...).
        /// </summary>
        [Test]
        public void TestSimNameRegex()
        {
            string models = typeof(IModel).Assembly.Location;
            IModel sim1 = Utilities.GetRunnableSim().Children[1];
            sim1.Name = "sim1";

            IModel sim2 = Utilities.GetRunnableSim().Children[1];
            sim2.Name = "sim2";

            IModel sim3 = Utilities.GetRunnableSim().Children[1];
            sim3.Name = "simulation3";

            IModel sim4 = Utilities.GetRunnableSim().Children[1];
            sim4.Name = "Base";

            Simulations sims = Simulations.Create(new[] { sim1, sim2, sim3, sim4, new DataStore() });
            Apsim.ParentAllChildren(sims);

            string apsimxFileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            sims.Write(apsimxFileName);

            string args = $@"{apsimxFileName} /Verbose /SimulationNameRegexPattern:sim\d";
            ProcessUtilities.ProcessWithRedirectedOutput proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, args, Directory.GetCurrentDirectory(), true);
            proc.WaitForExit();

            Assert.Null(proc.StdErr);
            Assert.True(proc.StdOut.Contains("sim1"));
            Assert.True(proc.StdOut.Contains("sim2"));
            Assert.False(proc.StdOut.Contains("simulation3"));
            Assert.False(proc.StdOut.Contains("Base"));

            args = $@"{apsimxFileName} /Verbose /SimulationNameRegexPattern:sim1";
            proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, args, Directory.GetCurrentDirectory(), true);
            proc.WaitForExit();

            Assert.Null(proc.StdErr);
            Assert.True(proc.StdOut.Contains("sim1"));
            Assert.False(proc.StdOut.Contains("sim2"));
            Assert.False(proc.StdOut.Contains("simulation3"));
            Assert.False(proc.StdOut.Contains("Base"));

            args = $@"{apsimxFileName} /Verbose /SimulationNameRegexPattern:(simulation3)|(Base)";
            proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, args, Directory.GetCurrentDirectory(), true);
            proc.WaitForExit();

            Assert.Null(proc.StdErr);
            Assert.False(proc.StdOut.Contains("sim1"));
            Assert.False(proc.StdOut.Contains("sim2"));
            Assert.True(proc.StdOut.Contains("simulation3"));
            Assert.True(proc.StdOut.Contains("Base"));
        }
    }
}
