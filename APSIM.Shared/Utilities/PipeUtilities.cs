namespace APSIM.Shared.Utilities
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// A collection of pipe utility methods.
    /// </summary>
    public class PipeUtilities
    {
        /// <summary>
        /// Serialize an object and encode the result as a base64 string.
        /// </summary>
        /// <param name="obj">Object to be serialized.</param>
        private static MemoryStream SerialiseTo(object obj)
        {
            // Serialise the object, then encode the result in base64.
            // This ensures that we can send the result over a network connection.
            MemoryStream result = new MemoryStream();
            using (Stream cryptoStream = new CryptoStream(result, new ToBase64Transform(), CryptoStreamMode.Write, leaveOpen: true))
            {
                using (Stream serialised = ReflectionUtilities.BinarySerialise(obj))
                {
                    serialised.Seek(0, SeekOrigin.Begin);
                    serialised.CopyTo(cryptoStream);
                }
            }
            return result;
        }

        /// <summary>
        /// Read from the stream, decode from base64, then deserialize
        /// the result.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        private static object DeserialiseFrom(Stream stream)
        {
            // Decode from base64, then deserialise the result.
            using (Stream cryptoStream = new CryptoStream(stream, new FromBase64Transform(), CryptoStreamMode.Read, leaveOpen: true))
                return ReflectionUtilities.BinaryDeserialise(cryptoStream);
        }

        /// <summary>
        /// Send an object to the specified anonymous out pipe.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="obj">The object to send.</param>
        public static void SendObjectToPipe(Stream pipeWriter, object obj)
        {
            using (MemoryStream stream = SerialiseTo(obj))
            {
                // Write the number of bytes
                var numBytes = Convert.ToInt32(stream.Length);
                var intBuffer = BitConverter.GetBytes(numBytes);
                pipeWriter.Write(intBuffer, 0, 4);

                // Write the objStream.
                byte[] buf = stream.ToArray();

                // todo: should probably chunk this.
                pipeWriter.Write(buf, 0, numBytes);
            }
        }

        /// <summary>
        /// Get an object from the specified anonymous in pipe.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The object or null if no object read.</returns>
        public static object GetObjectFromPipe(Stream pipeReader)
        {
            // Read number of bytes.
            var intBuffer = new byte[4];
            pipeReader.Read(intBuffer, 0, 4);
            var numBytes = BitConverter.ToInt32(intBuffer, 0);

            if (numBytes > 0)
            {
                // Read bytes for object.
                byte[] buffer = Read(pipeReader, numBytes);

                // Convert bytes to object.
                using (MemoryStream stream = new MemoryStream(buffer))
                    return DeserialiseFrom(stream);
            }
            return null;
        }

        /// <summary>
        /// Read N bytes from the stream.
        /// </summary>
        /// <remarks>
        /// This method accounts for the possibility of message being split across
        /// multiple datagrams (e.g. as in a network socket connection).
        /// </remarks>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="numBytes">Number of bytes to read.</param>
        private static byte[] Read(Stream stream, int numBytes)
        {
            var buffer = new byte[numBytes];
            int totalRead = 0;
            int read = 0;
            do
            {
                read = stream.Read(buffer, totalRead, numBytes - totalRead);
                totalRead += read;
            }
            while (totalRead < numBytes && read > 0);

            return buffer;
        }

        /// <summary>
        /// Get an object from the specified anonymous in pipe.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The object or null if no object read.</returns>
        public static byte[] GetBytesFromPipe(Stream pipeReader)
        {
            // Read number of bytes.
            var intBuffer = new byte[4];
            pipeReader.Read(intBuffer, 0, 4);
            var numBytes = BitConverter.ToInt32(intBuffer, 0);

            if (numBytes > 0)
                return Read(pipeReader, numBytes);
            return null;
        }

        /// <summary>
        /// Send an object to the specified anonymous out pipe.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="buffer">The data to send.</param>
        public static void SendToPipe(Stream pipeWriter, byte[] buffer)
        {
            // Write the number of bytes
            var numBytes = Convert.ToInt32(buffer.Length);
            var intBuffer = BitConverter.GetBytes(numBytes);
            pipeWriter.Write(intBuffer, 0, 4);

            // Write the objStream.
            // todo: really ought to chunk this.
            pipeWriter.Write(buffer, 0, numBytes);
        }
    }
}
