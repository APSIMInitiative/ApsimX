using System.IO;
using System.Text;

namespace APSIM.Shared.Extensions
{
    /// <summary>
    /// Extension methods for streams.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Get the length of the byte-order mark at the start of the stream.
        /// Return 0 if the stream doesn't contain a byte-order mark. The stream
        /// position will not be modified by this method.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public static uint GetBomLength(this Stream stream)
        {
            long position = stream.Position;

            // Get the stream's encoding.
            Encoding encoding = GetEncoding(stream);

            // Get the byte order mark associated with this encoding.
            byte[] bom = encoding.GetPreamble();

            // Read the first N bytes of the stream, where N is the length of
            // this encoding's byte order mark.
            byte[] buf = new byte[bom.Length];
            stream.Read(buf, 0, bom.Length);

            // Reset stream position after the read.
            stream.Seek(position, SeekOrigin.Begin);

            // Check if the first bytes match the byte order mark. If they do,
            // return the length of the byte order mark. Otherwise, return 0.
            if (Equal(bom, buf))
                return (uint)bom.Length;
            return 0;
        }

        /// <summary>
        /// Use a StreamReader to detect the stream' encoding. After calling
        /// this method, the stream will still be open and in the same position
        /// as before the method call.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public static Encoding GetEncoding(this Stream stream)
        {
            long position = stream.Position;
            using (StreamReader reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            {
                reader.Read(new char[4], 0, 4);
                Encoding encoding = reader.CurrentEncoding;
                stream.Seek(position, SeekOrigin.Begin);
                return encoding;
            }
        }

        /// <summary>
        /// Check if the contents of two byte arrays are equal.
        /// </summary>
        /// <param name="arr0">The first arry.</param>
        /// <param name="arr1">The second array.</param>
        public static bool Equal(this byte[] arr0, byte[] arr1)
        {
            // If both arrays are null, return true.
            if (arr0 == null && arr1 == null)
                return true;

            // If one array is null, return false.
            if (arr0 == null && arr1 != null)
                return false;
            if (arr0 != null && arr1 == null)
                return false;

            // If lengths differ, return false.
            if (arr0.Length != arr1.Length)
                return false;

            // Finally, a byte-wise comparison.
            for (int i = 0; i < arr0.Length; i++)
                if (arr0[i] != arr1[i])
                    return false;
            return true;
        }
    }
}