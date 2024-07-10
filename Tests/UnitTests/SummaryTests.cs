using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Models;
using Models.Core;
using Models.Core.Run;
using Models.Storage;
using Models.Core.ApsimFile;
using System.Data;

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
            summary.Verbosity = MessageType.Error;

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
            string message3 = "Written in OnCompleted";
            SummaryWriter writer = new SummaryWriter();
            writer.AddMessage("[Clock].StartOfSimulation", message1);
            writer.AddMessage("[Clock].StartOfSimulation", message2);
            writer.AddMessage("[Simulation].Completed", message3);

            Structure.Add(writer, sim);

            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];

            IDataStore storage = sims.FindChild<IDataStore>();
            DataTable messages = storage.Reader.GetData("_Messages");

            // Clock will write its own "Simulation terminated normally" message.
            Assert.That(messages.Rows.Count, Is.EqualTo(5));

            // The first row will be a warning caused by the lack of a
            // microclimate model.

            Assert.That(messages.Rows[1][6], Is.EqualTo(message1));
            Assert.That(messages.Rows[2][6], Is.EqualTo(message2));

            // The fourth row should not be written by SummaryWriter.
            Assert.That(messages.Rows[3]["ComponentName"], Is.Not.EqualTo(writer.Name));

            // The fifth will be the "Simulation terminated normally" message.
            Assert.That(messages.Rows[4][6], Is.EqualTo(message3));
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
                    events.Subscribe(eventName, (_, __) => summary.WriteMessage(this, message, MessageType.Information));
            }
        }
    }
}
