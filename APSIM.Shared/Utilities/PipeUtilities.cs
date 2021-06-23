namespace APSIM.Shared.Utilities
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// A collection of pipe utility methods.
    /// </summary>
    public class PipeUtilities
    {
        /// <summary>
        /// Get an object from the specified anonymous in pipe.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The object or null if no object read.</returns>
        public static object GetObjectFromPipe(AnonymousPipeClientStream pipeReader)
        {
            // Read number of bytes.
            var intBuffer = new byte[4];
            pipeReader.Read(intBuffer, 0, 4);
            var numBytes = BitConverter.ToInt32(intBuffer, 0);

            if (numBytes > 0)
            {
                // Read bytes for object.
                var buffer = new byte[numBytes];
                pipeReader.Read(buffer, 0, numBytes);

                // Convert bytes to object.
                return ReflectionUtilities.BinaryDeserialise(new MemoryStream(buffer));
            }
            return null;
        }

        /// <summary>
        /// Send an object to the specified anonymous out pipe.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="obj">The object to send.</param>
        public static void SendObjectToPipe(AnonymousPipeClientStream pipeWriter, object obj)
        {
            var objStream = ReflectionUtilities.BinarySerialise(obj) as MemoryStream;

            // Write the number of bytes
            var numBytes = Convert.ToInt32(objStream.Length);
            var intBuffer = BitConverter.GetBytes(numBytes);
            pipeWriter.Write(intBuffer, 0, 4);

            // Write the objStream.
            pipeWriter.Write(objStream.ToArray(), 0, numBytes);
        }

        /// <summary>
        /// Send an object to the specified anonymous out pipe.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="obj">The object to send.</param>
        public static void SendObjectToPipe(NamedPipeClientStream pipeWriter, object obj)
        {
            var objStream = ReflectionUtilities.BinarySerialise(obj) as MemoryStream;

            // Write the number of bytes
            var numBytes = Convert.ToInt32(objStream.Length);
            var intBuffer = BitConverter.GetBytes(numBytes);
            pipeWriter.Write(intBuffer, 0, 4);

            // Write the objStream.
            pipeWriter.Write(objStream.ToArray(), 0, numBytes);
        }

        /// <summary>
        /// Get an object from the specified anonymous in pipe.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The object or null if no object read.</returns>
        public static object GetObjectFromPipe(AnonymousPipeServerStream pipeReader)
        {
            // Read number of bytes.
            var intBuffer = new byte[4];
            pipeReader.Read(intBuffer, 0, 4);
            var numBytes = BitConverter.ToInt32(intBuffer, 0);

            if (numBytes > 0)
            {
                // Read bytes for object.
                var buffer = new byte[numBytes];
                pipeReader.Read(buffer, 0, numBytes);

                // Convert bytes to object.
                return ReflectionUtilities.BinaryDeserialise(new MemoryStream(buffer));
            }
            return null;
        }

        /// <summary>
        /// Send an object to the specified anonymous out pipe.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="obj">The object to send.</param>
        public static void SendObjectToPipe(AnonymousPipeServerStream pipeWriter, object obj)
        {
            var objStream = ReflectionUtilities.BinarySerialise(obj) as MemoryStream;

            // Write the number of bytes
            var numBytes = Convert.ToInt32(objStream.Length);
            var intBuffer = BitConverter.GetBytes(numBytes);
            pipeWriter.Write(intBuffer, 0, 4);

            // Write the objStream.
            pipeWriter.Write(objStream.ToArray(), 0, numBytes);
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
                var buffer = new byte[numBytes];
                pipeReader.Read(buffer, 0, numBytes);

                // Convert bytes to object.
                return ReflectionUtilities.BinaryDeserialise(new MemoryStream(buffer));
            }
            return null;
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
            {
                // Read bytes for object.
                var buffer = new byte[numBytes];
                pipeReader.Read(buffer, 0, numBytes);
                return buffer;
            }
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
            pipeWriter.Write(buffer, 0, numBytes);
        }
    }
}
