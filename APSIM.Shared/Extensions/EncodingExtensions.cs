using System.Text;

namespace APSIM.Shared.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Encoding"/> class.
    /// </summary>
    public static class EncodingExtensions
    {
        /// <summary>
        /// Get the number of bytes occupied by a character in the given encoding.
        /// </summary>
        /// <param name="encoding">An encoding.</param>
        /// <param name="character">A character.</param>
        public static int GetByteCount(this Encoding encoding, char character)
        {
            return encoding.GetByteCount(new string(character, 1));
        }
    }
}