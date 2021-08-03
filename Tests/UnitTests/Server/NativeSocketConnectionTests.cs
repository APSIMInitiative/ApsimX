using System;
using NUnit.Framework;
using APSIM.Server.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Text;

namespace UnitTests.Server
{
    [TestFixture]
    public class NativeSocketConnectionTests
    {
        private const string pipePath = "/tmp/CoreFxPipe_";
        private string pipeName;
        private NativeSocketConnection conn;

        [SetUp]
        public void Initialise()
        {
            pipeName = Guid.NewGuid().ToString();
            conn = new NativeSocketConnection(pipeName, false);
        }

        [TearDown]
        public void Cleanup()
        {
            conn.Disconnect();
            conn.Dispose();
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
                conn.WaitForConnection();
                Assert.AreEqual(target, conn.ReadInt());
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
                conn.WaitForConnection();
                Assert.AreEqual(target, conn.ReadDouble());
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
                conn.WaitForConnection();
                Assert.AreEqual(true, conn.ReadBool());
                Assert.AreEqual(false, conn.ReadBool());
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
                conn.WaitForConnection();
                Assert.Throws<NotImplementedException>(() => conn.ReadDate());
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
                conn.WaitForConnection();
                Assert.AreEqual(target, conn.ReadString());
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