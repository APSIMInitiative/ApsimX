using System;

using Table = MigraDocCore.DocumentObjectModel.Tables.Table;
using Section = MigraDocCore.DocumentObjectModel.Section;


namespace APSIM.Interop.Documentation.Extensions
{
    internal static class SectionExtensions
    {
        /// <summary>
        /// Get the last table in the section. Throws if section contains no tables.
        /// </summary>
        /// <param name="section">A section.</param>
        internal static Table GetLastTable(this Section section)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));
            try
            {
                // MigraDoc seems to throw a NullReferenceException if the section
                // contains no tables. In such a case, we discard the NRE from
                // MigraDoc and throw our own, more useful exception.
                var table = section.LastTable;
                if (table == null)
                    throw new InvalidOperationException("Section contains no tables.");

                return table;
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("Section contains no tables.");
            }
        }
    }
}
