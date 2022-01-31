using APSIM.Shared.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.APSIMShared
{
    /// <summary>
    /// Unit tests for <see cref="StreamReaderRandomAccess"/>.
    /// </summary>
    [TestFixture]
    public class StreamReaderRandomAccessTests
    {
        /// <summary>
        /// Ensure that ReadLine() works as expected.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="expectedOutputs">Expected outputs from serial calls to ReadLine().</param>
        [TestCase("test\n", "test")]
        [TestCase("line 0\nline 1\n", "line 0", "line 1")]
        [TestCase("line with no LF", "line with no LF")]
        public void TestReadLine(string input, params string[] expectedOutputs)
        {
            using (Stream stream = CreateStream(input))
            {
                StreamReaderRandomAccess reader = new StreamReaderRandomAccess(stream);
                for (int i = 0; i < expectedOutputs.Length; i++)
                    Assert.AreEqual(expectedOutputs[i], reader.ReadLine());
            }
        }

        /// <summary>
        /// Ensure that Seek(0 works correctly by performing a seek followed by
        /// a ReadLine().
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="position">Position to seek to.</param>
        /// <param name="origin">Seek origin used for seeking.</param>
        /// <param name="expectedOutput">Expected string to be returned from ReadLine() after a seek.</param>
        [TestCase("asdf", 0, SeekOrigin.Begin, "asdf")]
        [TestCase("asdf", 1, SeekOrigin.Begin, "sdf")]
        [TestCase("asdf", 1, SeekOrigin.End, "")]
        public void TestSeek(string input, int position, SeekOrigin origin, string expectedOutput)
        {
            using (Stream stream = CreateStream(input))
            {
                StreamReaderRandomAccess reader = new StreamReaderRandomAccess(stream);
                reader.Seek(position, origin);
                string actualOutput = reader.ReadLine();
                Assert.AreEqual(expectedOutput, actualOutput);
            }
        }

        /// <summary>
        /// Ensure that the stream reader's position property is correctly
        /// updated after a read.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="expectedPosition">Expected position after reading a line.</param>
        [TestCase("asdf\nasdf", 5)]
        [TestCase("x", 1)]
        public void TestPositionAfterRead(string input, int expectedPosition)
        {
            using (Stream stream = CreateStream(input))
            {
                StreamReaderRandomAccess reader = new StreamReaderRandomAccess(stream);
                reader.ReadLine();
                Assert.AreEqual(expectedPosition, reader.Position);
            }
        }

        /// <summary>
        /// Ensure that the stream reader's position property is correctly
        /// updated after a Seek().
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="seekPosition">Position to seek to.</param>
        /// <param name="origin">Origin reference used when performing the seek.</param>
        /// <param name="expectedPosition">Expected position after the seek.</param>
        [TestCase("asdf\nasdf", 5, SeekOrigin.Begin, 5)]
        [TestCase("asdf\nasdf", 1, SeekOrigin.End, 1)]
        public void TestPositionAfterSeek(string input, int seekPosition, SeekOrigin origin, int expectedPosition)
        {
            using (Stream stream = CreateStream(input))
            {
                StreamReaderRandomAccess reader = new StreamReaderRandomAccess(stream);
                reader.Seek(seekPosition, SeekOrigin.Begin);
                Assert.AreEqual(expectedPosition, reader.Position);
            }
        }

        private static Stream CreateStream(string message)
        {
            MemoryStream stream = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(stream, leaveOpen: true))
                writer.Write(message);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }
}