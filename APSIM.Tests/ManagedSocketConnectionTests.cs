using System;
using NUnit.Framework;
using APSIM.Server.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using APSIM.Shared.Utilities;
using APSIM.Server.Commands;
using Models.Core.Run;
using System.Collections.Generic;
using Models.Core;

namespace APSIM.Tests
{
    [TestFixture]
    public class ManagedSocketConnectionTests
    {
        [Serializable]
        private class MockModel : Model
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
            IEnumerable<IReplacement> replacements = new IReplacement[]
            {
                new PropertyReplacement("path", "value"),
                new ModelReplacement("x", new MockModel())
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
            IEnumerable<IReplacement> replacements = new IReplacement[]
            {
                new ModelReplacement("f", new MockModel()),
                new PropertyReplacement("path to a model", "replacement value")
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

        private void TestRead(ICommand target)
        {
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.AreEqual(target, protocol.WaitForCommand());
            });

            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None))
            {
                client.Connect();
                PipeUtilities.SendObjectToPipe(client, target);
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

            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.In, PipeOptions.None))
            {
                client.Connect();
                Assert.AreEqual(target, PipeUtilities.GetObjectFromPipe(client));
            }

            server.Wait();
        }
    }
}