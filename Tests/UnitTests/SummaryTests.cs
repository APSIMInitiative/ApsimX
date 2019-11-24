using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Core;
using Models.Core.Run;

namespace UnitTests
{
    [TestFixture]
    public class SummaryTests
    {
        /// <summary>
        /// This reproduces a bug where disabling summary output would
        /// cause a simulation to fail.
        /// </summary>
        [Test]
        public void TestDisabledSummary()
        {
            Simulations sims = Utilities.GetRunnableSim();
            Summary summary = Apsim.Find(sims, typeof(Summary)) as Summary;
            summary.CaptureErrors = false;
            summary.CaptureWarnings = false;
            summary.CaptureSummaryText = false;

            var runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new Exception("Disabling summary output causes simulation to fail.");
        }
    }
}
