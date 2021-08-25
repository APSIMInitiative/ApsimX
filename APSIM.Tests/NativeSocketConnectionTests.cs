using System;
using NUnit.Framework;
using APSIM.Server.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.IO.Pipes;

namespace APSIM.Tests
{
    [TestFixture]
    public class NativeSocketConnectionTests
    {
        private const string pipePath = "/tmp/CoreFxPipe_";
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

        private Socket CreateClient()
        {
            Socket sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            sock.Connect(new UnixDomainSocketEndPoint($"{pipePath}{pipeName}"));
            return sock;
        }

        [Test]
        public void TestReadInt()
        {
            int target = 1234;
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.AreEqual(target, protocol.ReadInt());
            });
            Socket client = CreateClient();
            byte[] buf = BitConverter.GetBytes(target);
            client.Send(BitConverter.GetBytes(buf.Length).Take(4).ToArray());
            // sock.Send(new byte[4] { 0xD2, 4, 0, 0 });
            client.Send(buf);
            client.Disconnect(false);

            server.Wait();
        }

        [Test]
        public void TestReadDouble()
        {
            double target = -17.5;
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.AreEqual(target, protocol.ReadDouble());
            });
            Socket client = CreateClient();
            byte[] buf = BitConverter.GetBytes(target);
            client.Send(BitConverter.GetBytes(buf.Length).Take(4).ToArray());
            // sock.Send(new byte[8] { 0, 0, 0, 0, 0, 0x80, 0x31, 0xC0 });
            client.Send(buf);
            client.Disconnect(false);

            server.Wait();
        }

        [Test]
        public void TestReadBool()
        {
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.AreEqual(true, protocol.ReadBool());
                Assert.AreEqual(false, protocol.ReadBool());
            });
            Socket client = CreateClient();
            foreach (bool target in new[] { true, false })
            {
                byte[] buf = BitConverter.GetBytes(target);
                client.Send(BitConverter.GetBytes(buf.Length).Take(4).ToArray());
                // sock.Send(new byte[1] { 1 }); // = true
                // sock.Send(new byte[1] { 0 }); // = false
                client.Send(buf);
            }
            client.Disconnect(false);

            server.Wait();
        }

        [Test]
        public void TestReadDate()
        {
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.Throws<NotImplementedException>(() => protocol.ReadDate());
            });
            Socket client = CreateClient();
            server.Wait();
            client.Disconnect(false);
        }

        [Test]
        public void TestReadString()
        {
            string target = "This is a short message";
            Task server = Task.Run(() =>
            {
                pipe.WaitForConnection();
                Assert.AreEqual(target, protocol.ReadString());
            });
            Socket client = CreateClient();
            byte[] buf = Encoding.Default.GetBytes(target);
            client.Send(BitConverter.GetBytes(buf.Length).Take(4).ToArray());
            // sock.Send(new byte[23] { 0x54, 0x68, 0x69, 0x73, 0x20, 0x69, 0x73, 0x20, 0x61, 0x20, 0x73, 0x68, 0x6F, 0x72, 0x74, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65 });
            client.Send(buf);
            client.Disconnect(false);

            server.Wait();
        }
    }
}