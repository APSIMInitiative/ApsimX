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