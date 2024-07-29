using APSIM.Shared.Extensions;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text;

namespace UnitTests.APSIMShared
{
    /// <summary>
    /// Unit tests for <see cref="APSIM.Shared.Extensions.StreamExtensions"/>.
    /// </summary>
    [TestFixture]
    public class StreamExtensionTests
    {
        /// <summary>
        /// Ensure that UTF8 encoding is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("a short message")]
        public void TestGetEncodingUtf8(string message)
        {
            TestGetEncoding(Encoding.UTF8, message);
        }

        /// <summary>
        /// Ensure that UTF16 (little endian) encoding is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("a short message")]
        public void TestGetEncodingUt16LE(string message)
        {
            TestGetEncoding(Encoding.Unicode, message);
        }

        /// <summary>
        /// Ensure that UTF16 (big endian) encoding is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("a short message")]
        public void TestGetEncodingUtf16BE(string message)
        {
            TestGetEncoding(Encoding.BigEndianUnicode, message);
        }

        /// <summary>
        /// Ensure that UTF8 byte-order mark is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("msg")]
        public void TestGetBomLengthUtf8(string message)
        {
            TestGetBomLength(Encoding.UTF8, message);
        }

        /// <summary>
        /// Ensure that lack of a byte-order mark is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("msg")]
        public void TestGetBomLengthUtf8NoBom(string message)
        {
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(message)))
                Assert.That(stream.GetBomLength(), Is.EqualTo(0));
        }

        /// <summary>
        /// Ensure that UTF16 (little endian) byte-order mark is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("msg")]
        public void TestGetBomLengthUtf16LE(string message)
        {
            TestGetBomLength(Encoding.Unicode, message);
        }

        /// <summary>
        /// Ensure that UTF16 (big endian) byte-order mark is correctly detected.
        /// </summary>
        /// <param name="message">A message.</param>
        [TestCase("msg")]
        public void TestGetBomLengthUtf16BE(string message)
        {
            TestGetBomLength(Encoding.BigEndianUnicode, message);
        }

        /// <summary>
        /// Test byte array equality extension method.
        /// </summary>
        [Test]
        public void TestArrayEquals()
        {
            Assert.That(APSIM.Shared.Extensions.StreamExtensions.Equal(null, null), Is.True);
            Assert.That(APSIM.Shared.Extensions.StreamExtensions.Equal(new byte[0], null), Is.False);
            Assert.That(APSIM.Shared.Extensions.StreamExtensions.Equal(null, new byte[0]), Is.False);
            Assert.That(APSIM.Shared.Extensions.StreamExtensions.Equal(new byte[0], new byte[1]), Is.False);
            Assert.That(APSIM.Shared.Extensions.StreamExtensions.Equal(new byte[2] { 0, 1 }, new byte[2] { 0, 2 }), Is.False);
            Assert.That(APSIM.Shared.Extensions.StreamExtensions.Equal(new byte[2] { 64, 128 }, new byte[2] { 64, 128 }), Is.True);
        }

        /// <summary>
        /// Ensure that the returned BOM length is correct for the given
        /// encoding and message.
        /// </summary>
        /// <param name="encoding">An encoding.</param>
        /// <param name="message">Any text.</param>
        private void TestGetBomLength(Encoding encoding, string message)
        {
            using (Stream stream = GetStream(encoding, message))
                TestGetBomLength(encoding, stream);
        }

        /// <summary>
        /// Ensure that the returned BOM length in the stream matches the BOM
        /// length of the specified encoding, and that the stream position is
        /// not mutated.
        /// </summary>
        /// <param name="encoding">An encoding.</param>
        /// <param name="stream">A stream containing text encoded using the above encoding.</param>
        private void TestGetBomLength(Encoding encoding, Stream stream)
        {
            // Ensure the correct byte-order mark is returned.
            Assert.That(stream.GetBomLength(), Is.EqualTo(encoding.GetPreamble().Length));

            // Ensure the call to GetBomLength() didn't move the stream.
            Assert.That(stream.Position, Is.EqualTo(0));
        }

        /// <summary>
        /// Encode text using the given encoding (with a byte-order mark), and
        /// ensure that the GetEncoding() extension method correctly detects the
        /// encoding, and that the stream position is not mutated.
        /// </summary>
        /// <param name="encoding">An encoding.</param>
        /// <param name="message">Any text.</param>
        private void TestGetEncoding(Encoding encoding, string message)
        {
            using (Stream stream = GetStream(encoding, message))
            {
                // Ensure the correct encoding is returned.
                Assert.That(stream.GetEncoding(), Is.EqualTo(encoding));

                // Ensure the call to GetEncoding() didn't move the stream.
                Assert.That(stream.Position, Is.EqualTo(0));
            }
        }

        /// <summary>
        /// Encode text using the given encoding, and return a stream containing
        /// the binary data.
        /// </summary>
        /// <param name="encoding">An encoding.</param>
        /// <param name="message">Any text.</param>
        private Stream GetStream(Encoding encoding, string message)
        {
            byte[] byteOrderMark = encoding.GetPreamble();
            byte[] bytes = encoding.GetBytes(message);
            return new MemoryStream(byteOrderMark.Concat(bytes).ToArray());
        }
    }
}
