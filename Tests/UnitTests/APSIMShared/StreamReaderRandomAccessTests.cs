using APSIM.Shared.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
                    Assert.That(reader.ReadLine(), Is.EqualTo(expectedOutputs[i]));
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
                Assert.That(actualOutput, Is.EqualTo(expectedOutput));
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
                Assert.That(reader.Position, Is.EqualTo(expectedPosition));
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
                Assert.That(reader.Position, Is.EqualTo(expectedPosition));
            }
        }

        /// <summary>
        /// Each of these input files are identical but have a different
        /// encoding and byte-order mark. Ensure that the stream reader parses
        /// them all identically, and is able to resolve the encoding from the
        /// BOM (using UTF8 when no BOM is present), and is able to seek
        /// correctly within the file, accounting for the presence of the BOM.
        /// </summary>
        /// <param name="inputFile">Input file (stored as embedded resource in this assembly).</param>
        [TestCase("UnitTests.APSIMShared.Resources.test-utf8.csv")]
        [TestCase("UnitTests.APSIMShared.Resources.test-utf8-bom.csv")]
        [TestCase("UnitTests.APSIMShared.Resources.test-utf16-le.csv")]
        [TestCase("UnitTests.APSIMShared.Resources.test-utf16-be.csv")]
        public void TestBomParsing(string inputFile)
        {
            string[] expected = new string[4]
            {
                "x,y",
                "0,1",
                "1,2",
                "2,4"
            };
            long[] endOfLinePositions = new long[expected.Length];

            using (Stream stream = GetResourceStream(inputFile))
            {
                StreamReaderRandomAccess reader = new StreamReaderRandomAccess(stream);
                for (int i = 0; i < expected.Length; i++)
                {
                    string line = reader.ReadLine();
                    Assert.That(line, Is.EqualTo(expected[i]));
                    endOfLinePositions[i] = reader.Position;
                }

                // Seek to the end of each line, and read a line, and ensure
                // that we get the expected result. This should probably be a
                // separate test.
                for (int i = 1; i < expected.Length - 1; i++)
                {
                    reader.Seek(endOfLinePositions[i], SeekOrigin.Begin);
                    string input = reader.ReadLine();
                    Assert.That(input, Is.EqualTo(expected[i + 1]));
                }
            }
        }

        /// <summary>
        /// Return a stream which reads from an embedded resource in the current
        /// assembly with the specified name. Throw if no resource is found.
        /// </summary>
        /// <param name="resourceName">Resource name.</param>
        private Stream GetResourceStream(string resourceName)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Resource does not exist: '{resourceName}'");
            return stream;
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