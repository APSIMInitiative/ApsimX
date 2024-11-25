using APSIM.Server.Commands;
using APSIM.Server.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipes;
using System.Threading.Tasks;
using static Models.Core.Overrides;

namespace UnitTests
{
    /// <summary>
    /// Unit tests for managed comms protocol.
    /// </summary>
    /// <remarks>
    /// This is currently quick n dirty to verify that things basically work.
    /// todo:
    /// - mock out socket layer
    /// - 
    /// </remarks>
    [TestFixture]
    public class ManagedSocketConnectionTests
    {
        [Serializable]
        public class MockModel : Model
        {
            private bool a = true;
            private string b = "sdf";
            private uint c = 34;
            private double d = 1;
            private char e = 'e';
            public override bool Equals(object obj)
            {
                if (obj is MockModel model)
                {
                    return  a == model.a &&
                            b == model.b &&
                            c == model.c &&
                            d == model.d &&
                            e == model.e;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return (a, b, c, d, e).GetHashCode();
            }
        }

        private const string pipePath = "/tmp/CoreFxPipe_";
        private string pipeName;
        private NamedPipeServerStream pipe;
        private ManagedCommunicationProtocol protocol;

        [SetUp]
        public void Initialise()
        {
            pipeName = Guid.NewGuid().ToString();
            pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
            protocol = new ManagedCommunicationProtocol(pipe);
        }

        [TearDown]
        public void Cleanup()
        {
            protocol = null;
            pipe.Disconnect();
            pipe.Dispose();
        }

        [Test]
        public void TestReadRunCommand()
        {
            IEnumerable<Override> replacements = new Override[]
            {
                new Override("path", "value", Override.MatchTypeEnum.NameAndType),
                new Override("x", new MockModel(), Override.MatchTypeEnum.NameAndType)
            };
            ICommand target = new RunCommand(true, true, 32, replacements, new[] { "sim1, sim2" });
            TestRead(target);
        }

        [Test]
        public void TestReadReadCommand()
        {
            ICommand target = new ReadCommand("table", new[] { "param1", "param2", "param3" });
            TestRead(target);
        }

        [Test]
        public void TestWriteRunCommand()
        {
            IEnumerable<Override> replacements = new Override[]
            {
                new Override("f", new MockModel(), Override.MatchTypeEnum.NameAndType),
                new Override("path to a model", "replacement value", Override.MatchTypeEnum.NameAndType)
            };
            IEnumerable<string> sims = new[] { "one simulation" };
            ICommand command = new RunCommand(false, true, 65536, replacements, sims);
            TestWrite(command);
        }

        [Test]
        public void TestWriteReadCommand()
        {
            ICommand command = new ReadCommand(" the table to be read ", new[] { "a single parameter"});
            TestWrite(command);
        }

        /// <summary>
        /// Test transmission of a datatable using the managed communication protocol.
        /// </summary>
        [Test]
        public void TestSendDataTable()
        {
            DataTable expected = new DataTable("table name");
            expected.Columns.Add("t", typeof(double));
            expected.Columns.Add("x", typeof(double));
            expected.Rows.Add(0d, 1d);
            expected.Rows.Add(1d, 2d);
            expected.Rows.Add(2d, 4d);

            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                DataTable table = (DataTable)protocol.Read();
                // Assert.AreEqual(expected.TableName, table.TableName);
                Assert.That(table.Columns.Count, Is.EqualTo(expected.Columns.Count));
                Assert.That(table.Rows.Count, Is.EqualTo(expected.Rows.Count));
            });
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                client.Connect();
                PipeUtilities.SendObjectToPipe(client, expected);
            }
            server.Wait();
        }

        private void TestRead(ICommand target)
        {
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                var actual = protocol.WaitForCommand();
                Assert.That(actual, Is.EqualTo(target));
            });

            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                client.Connect();
                PipeUtilities.SendObjectToPipe(client, target);
                PipeUtilities.GetObjectFromPipe(client);
            }
            
            server.Wait();
        }

        public void TestWrite(ICommand target)
        {
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                protocol.SendCommand(target);
            });

            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None))
            {
                client.Connect();
                object resp = PipeUtilities.GetObjectFromPipe(client);
                PipeUtilities.SendObjectToPipe(client, "ACK_MANAGED");
                Assert.That(resp, Is.EqualTo(target));

                if (target is RunCommand)
                    PipeUtilities.SendObjectToPipe(client, "FIN_MANAGED");
                if (target is ReadCommand readCommand)
                    PipeUtilities.SendObjectToPipe(client, new DataTable(readCommand.TableName));
            }

            server.Wait();
        }
    }
}