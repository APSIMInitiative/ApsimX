using System;
using NUnit.Framework;
using APSIM.Server.IO;
using APSIM.Shared.Utilities;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.IO.Pipes;
using System.IO;

namespace UnitTests.Server
{
    [TestFixture]
    [CancelAfter(5 * 1000)]
    public class NativeSocketConnectionTests
    {
        private string pipeName;
        private NamedPipeServerStream pipe;
        private NativeCommunicationProtocol protocol;

        [SetUp]
        public void Initialise()
        {
            pipeName = Guid.NewGuid().ToString();
            pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
            protocol = new NativeCommunicationProtocol(pipe);
        }

        [TearDown]
        public void Cleanup()
        {
            protocol = null;
            pipe.Disconnect();
            pipe.Dispose();
        }

        private NamedPipeClientStream CreateClient()
        {
            NamedPipeClientStream client = new NamedPipeClientStream(pipeName);
            client.Connect();
            return client;
        }

        [Test]
        public void TestReadInt()
        {
            int target = 1234;
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.That(protocol.ReadInt(), Is.EqualTo(target));
            });
            using (NamedPipeClientStream client = CreateClient())
            {
                PipeUtilities.SendIntToPipe(client, target);
            }

            server.Wait();
        }

        [Test]
        public void TestReadDouble()
        {
            double target = -17.5;
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.That(protocol.ReadDouble(), Is.EqualTo(target));
            });
            using (Stream client = CreateClient())
            {
                PipeUtilities.SendDoubleToPipe(client, target);
            }

            server.Wait();
        }

        [Test]
        public void TestReadBool()
        {
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.That(protocol.ReadBool(), Is.EqualTo(true));
                Assert.That(protocol.ReadBool(), Is.EqualTo(false));
            });
            using (Stream client = CreateClient())
            {
                foreach (bool target in new[] { true, false })
                {
                    PipeUtilities.SendBoolToPipe(client, target);
                }
            }

            server.Wait();
        }

        [Test]
        public void TestReadDate()
        {
            DateTime target = new DateTime(2022, 3, 17);
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.That(protocol.ReadDate(), Is.EqualTo(target));
            });
            using (Stream client = CreateClient())
            {
                PipeUtilities.SendDateToPipe(client, target);
            }

            server.Wait();
        }

        [Test]
        public void TestReadString()
        {
            string target = "This is a short message";
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.That(protocol.ReadString(), Is.EqualTo(target));
            });
            using (Stream client = CreateClient())
            {
                PipeUtilities.SendStringToPipe(client, target);
            }

            server.Wait();
        }

        /// <summary>
        /// Test reading a double array over the native socket connection.
        /// </summary>
        [Test]
        public void TestReadDoubleArray()
        {
            double[] array = new double[8] { 2, 1, 0.5, 0.25, -0.25, -0.5, -1, -2 };
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.That(protocol.ReadDoubleArray(), Is.EqualTo(array));
                Assert.That(protocol.ReadDoubleArray(), Is.EqualTo(array));
            });

            using (Stream client = CreateClient())
            {
                // Convert array to binary to send over the socket.
                PipeUtilities.SendArrayToPipe(client, array);

                // Let's also check with a pre-calculated binary representation of
                // the above array, just for fun. Note: does this depend on the
                // host system's endian-ness?
                byte[] buf = new byte[64] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0X40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XF0, 0X3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XE0, 0X3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XD0, 0X3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XD0, 0XBF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XE0, 0XBF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XF0, 0XBF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0XC0 };
                PipeUtilities.SendBytesToPipe(client, buf);
            }

            server.Wait();
        }
    }
}