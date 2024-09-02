namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
                using (Stream serialised = ReflectionUtilities.JsonSerialiseToStream(obj))
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
                return ReflectionUtilities.JsonDeserialise(cryptoStream);
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
        public static void SendBytesToPipe(Stream pipeWriter, byte[] buffer)
        {
            // Write the number of bytes
            var numBytes = Convert.ToInt32(buffer.Length);
            SendIntToPipe(pipeWriter, numBytes);

            // Write the objStream.
            // todo: really ought to chunk this.
            pipeWriter.Write(buffer, 0, numBytes);
        }

        /// <summary>
        /// Send an array to the specified anonymous out pipe, encoding each element according to its type.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="data">The data to send.</param>
        public static void SendArrayToPipe(Stream pipeWriter, Array data)
        {
            SendBytesToPipe(pipeWriter, Encode(data).ToArray());
        }

        /// <summary>
        /// Encode the array into a sequence of bytes, encoding each element according to its type.
        /// </summary>
        /// <param name="data">The data to encode.</param>
        /// <returns>The encoded array of bytes.</returns>
        public static IEnumerable<byte> Encode(Array data)
        {
            if (data == null || data.Length < 1)
                return new byte[0];
            Type arrayType = data.GetValue(0).GetType();
            if (arrayType == typeof(int))
                return data.Cast<int>().SelectMany(EncodeInt);
            else if (arrayType == typeof(double))
                return data.Cast<double>().SelectMany(EncodeDouble);
            else if (arrayType == typeof(bool))
                return data.Cast<bool>().SelectMany(EncodeBool);
            else if (arrayType == typeof(DateTime)) {
                return data.Cast<DateTime>().SelectMany(EncodeDate);
            } else if (arrayType == typeof(string))
                return data.Cast<string>().SelectMany(EncodeStringWithLength);
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Receive a 32-bit int from the pipe in little-endian order.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The int read from the pipe.</returns>
        public static int GetIntFromPipe(Stream pipeReader)
        {
            byte[] intBuffer = new byte[4];
            pipeReader.Read(intBuffer, 0, 4);
            return DecodeInt(intBuffer);
        }

        /// <summary>
        /// Decode 4 bytes in little-endian order as a 32-bit integer.
        /// </summary>
        /// <param name="intBuffer">4 bytes in little-endian order.</param>
        /// <returns>The parsed int</returns>
        public static int DecodeInt(IEnumerable<byte> intBuffer)
        {
            return BitConverter.ToInt32(FromLittleEndian(intBuffer).ToArray());
        }

        /// <summary>
        /// Send an int to the specified anonymous out pipe as 4 bytes in little-endian order.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="value">The int to send.</param>
        public static void SendIntToPipe(Stream pipeWriter, int value)
        {
            pipeWriter.Write(EncodeInt(value).ToArray(), 0, 4);
        }

        /// <summary>
        /// Receive a 64-bit double from the pipe in little-endian order.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The double read from the pipe.</returns>
        public static double GetDoubleFromPipe(Stream pipeReader)
        {
            byte[] doubleBuffer = new byte[8];
            pipeReader.Read(doubleBuffer, 0, 8);
            return DecodeDouble(doubleBuffer);
        }

        /// <summary>
        /// Decode 8 bytes in little-endian order as a 64-bit double.
        /// </summary>
        /// <param name="doubleBuffer">8 bytes in little-endian order.</param>
        /// <returns>The parsed double</returns>
        public static double DecodeDouble(IEnumerable<byte> doubleBuffer)
        {
            return BitConverter.ToDouble(FromLittleEndian(doubleBuffer).ToArray());
        }

        /// <summary>
        /// Send an double to the specified anonymous out pipe as 8 bytes in little-endian order.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="value">The double to send.</param>
        public static void SendDoubleToPipe(Stream pipeWriter, double value)
        {
            pipeWriter.Write(EncodeDouble(value).ToArray(), 0, 8);
        }

        /// <summary>
        /// Receive an arbitrary-length array of 64-bit doubles from the pipe, each in little-endian order.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The array of doubles read from the pipe.</returns>
        public static double[] GetDoubleArrayFromPipe(Stream pipeReader)
        {
            byte[] buffer = GetBytesFromPipe(pipeReader);
            const int bytesPerNumber = 8; // Doubles have 8 bytes in .net
            int length = buffer.Length / bytesPerNumber;
            double[] result = new double[length];
            for (int i = 0; i < length; i++)
                result[i] = DecodeDouble(new ArraySegment<byte>(buffer, i * bytesPerNumber, bytesPerNumber));
            return result;
        }

        /// <summary>
        /// Send a string to the specified anonymous out pipe, prefixed with the string length as a 4-byte int in little-endian order.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="s">The string to send.</param>
        public static void SendStringToPipe(Stream pipeWriter, string s)
        {
            PipeUtilities.SendBytesToPipe(pipeWriter, EncodeString(s).ToArray());
        }

        /// <summary>
        /// Receive an arbitrary-length utf-8-encoded string from the pipe, prefixed with its length.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The string read from the pipe.</returns>
        public static string GetStringFromPipe(Stream pipeReader)
        {
            byte[] buffer = GetBytesFromPipe(pipeReader);
            if (buffer == null)
                return null;
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Encode an int as 4 bytes in little-endian order.
        /// </summary>
        /// <param name="i">The int to encode.</param>
        /// <returns>The 4-byte sequence of the int encoded in little-endian order.</returns>
        public static IEnumerable<byte> EncodeInt(int i)
        {
            return ToLittleEndian(BitConverter.GetBytes(i));
        }

        /// <summary>
        /// Encode an int as 8 bytes in little-endian order.
        /// </summary>
        /// <param name="d">The double to encode.</param>
        /// <returns>The 8-byte sequence of the double encoded in little-endian order.</returns>
        public static IEnumerable<byte> EncodeDouble(double d)
        {
            return ToLittleEndian(BitConverter.GetBytes(d));
        }

        /// <summary>
        /// Encode a bool as a single byte (0 for false, 1 for true).
        /// </summary>
        /// <param name="b">The bool to encode.</param>
        /// <returns>The 1-byte sequence of the encoded bool.</returns>
        public static IEnumerable<byte> EncodeBool(bool b)
        {
            return BitConverter.GetBytes(b);
        }

        /// <summary>
        /// Send a bool to the specified anonymous out pipe, as a single byte, 1 for true, 0 for false.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="b">The bool to send.</param>
        public static void SendBoolToPipe(Stream pipeWriter, bool b)
        {
            byte[] buffer = EncodeBool(b).ToArray();
            pipeWriter.Write(buffer, 0, 1);
        }

        /// <summary>
        /// Receive a byte from the pipe and decode it as a bool.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The bool read from the pipe.</returns>
        public static bool GetBoolFromPipe(Stream pipeReader)
        {
            byte[] buffer = Read(pipeReader, 1);
            return BitConverter.ToBoolean(buffer);
        }

        /// <summary>
        /// Encode a DateTime as an int representing the date, e.g. 20220315 to represent the 15th of March, 2022.
        /// </summary>
        /// <param name="date">The DateTime to encode.</param>
        /// <returns>The 4-byte sequence of the encoded date.</returns>
        public static IEnumerable<byte> EncodeDate(DateTime date)
        {
            return EncodeInt(DateToInt(date));
        }

        /// <summary>
        /// Send a DateTime to the specified anonymous out pipe.
        /// </summary>
        /// <param name="pipeWriter">The pipe to write to.</param>
        /// <param name="date">The DateTime to send.</param>
        public static void SendDateToPipe(Stream pipeWriter, DateTime date)
        {
            PipeUtilities.SendIntToPipe(pipeWriter, DateToInt(date));
        }

        /// <summary>
        /// Receive an int from the pipe and decode it as a date.
        /// </summary>
        /// <param name="pipeReader">The pipe to read from.</param>
        /// <returns>The date read from the pipe.</returns>
        public static DateTime GetDateFromPipe(Stream pipeReader)
        {
            return IntToDate(GetIntFromPipe(pipeReader));
        }

        /// <summary>
        /// Encode a string as Utf-8.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>The sequence of bytes of the utf-8-encoded string.</returns>
        public static IEnumerable<byte> EncodeString(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        /// <summary>
        /// Encode a string as Utf-8, prefixed by the string length in bytes, encoded as a 4-byte int in little-endian order.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>The sequence of bytes, first the string length and then the utf-8-encoded string.</returns>
        public static IEnumerable<byte> EncodeStringWithLength(string s)
        {
            var data = EncodeString(s);
            return EncodeInt(data.Count()).Concat(data);
        }

        // Over the wire we represent dates as an int.
        // This assumes that we never care about the time of day, and that we won't have issues with timezone conversion changing the date.
        // We simply convert the DateTime to an int, e.g. 20220307 represents the seventh of March, 2022.
        private static int DateToInt(DateTime date)
        {
            return date.Year * 10000 + date.Month * 100 + date.Day;
        }

        private static DateTime IntToDate(int d)
        {
            return new DateTime(d / 10000, (d % 10000) / 100, d % 100);
        }

        /// <summary>
        /// Convert a byte sequence (typically 4 bytes for int and 8 bytes for double) to little-endian order.
        /// </summary>
        /// <param name="data">The byte sequence to convert</param>
        public static IEnumerable<byte> ToLittleEndian(IEnumerable<byte> data)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return data.Reverse();
            }
            return data;
        }

        /// <summary>
        /// Convert a little-endian byte sequence (typically 4 bytes for int and 8 bytes for double) to native-endian order.
        /// </summary>
        /// <param name="data">The byte sequence to convert</param>
        public static IEnumerable<byte> FromLittleEndian(IEnumerable<byte> data)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return data.Reverse();
            }
            return data;
        }
    }
}
