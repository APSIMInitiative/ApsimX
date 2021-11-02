using System.Collections.Generic;

namespace APSIM.Interop.Documentation.Helpers
{
    /// <summary>
    /// This class can compare citations in a bibliography.
    /// </summary>
    public class CitationComparer : IComparer<ICitation>
    {
        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        public int Compare(ICitation x, ICitation y)
        {
            int result = x.FirstAuthor.CompareTo(y.FirstAuthor);
            return result == 0 ? x.Year.CompareTo(y.Year) : result;
        }
    }
}
