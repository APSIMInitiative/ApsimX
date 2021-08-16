using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Core.ApsimFile;
using Models.Storage;
using System.Data;
using APSIM.Shared.Utilities;

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
            Summary summary = sims.FindInScope<Summary>();
            summary.CaptureErrors = false;
            summary.CaptureWarnings = false;
            summary.CaptureSummaryText = false;

            var runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw new Exception("Disabling summary output causes simulation to fail.");
        }

        /// <summary>
        /// This test ensures that data is written immediately following calls to 
        /// </summary>
        [Test]
        public void EnsureDataIsNotWrittenTwice()
        {
            Simulations sims = Utilities.GetRunnableSim();
            Simulation sim = sims.FindChild<Simulation>();
            Summary summary = sim.FindChild<Summary>();

            // Write 2 messages to the DB during StartOfSimulation.
            string message1 = "message 1";
            string message2 = "A slightly longer message";
            SummaryWriter writer = new SummaryWriter();
            writer.AddMessage("[Clock].StartOfSimulation", message1);
            writer.AddMessage("[Clock].StartOfSimulation", message2);

            Structure.Add(writer, sim);

            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            IDataStore storage = sims.FindChild<IDataStore>();
            DataTable messages = storage.Reader.GetData("_Messages");

            // Clock will write its own "Simulation terminated normally" message.
            Assert.AreEqual(3, messages.Rows.Count);

            Assert.AreEqual(message1, messages.Rows[0][6]);
            Assert.AreEqual(message2, messages.Rows[1][6]);

            // The third row should not be written by SummaryWriter.
            Assert.AreNotEqual(writer.Name, messages.Rows[2]["ComponentName"]);
        }

        [Serializable]
        private class SummaryWriter : Model
        {
            [Link] private ISummary summary = null;
            [Link] private IEvent events = null;

            private List<(string, string)> messages = new List<(string, string)>();

            /// <summary>
            /// Add a message to be written.
            /// </summary>
            /// <param name="eventName">Name of the event in which the message should be written.</param>
            /// <param name="message">Message to be written.</param>
            public void AddMessage(string eventName, string message)
            {
                messages.Add((eventName, message));
            }

            [EventSubscribe("SubscribeToEvents")]
            private void DoEventSubscriptions(object sender, EventArgs args)
            {
                foreach ((string eventName, string message) in messages)
                    events.Subscribe(eventName, (_, __) => summary.WriteMessage(this, message));
            }
        }
    }
}
