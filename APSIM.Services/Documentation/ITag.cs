namespace APSIM.Services.Documentation
{
    public interface ITag
    {
        /// <summary>
        /// Indentation level of the tag.
        /// </summary>
        int Indentation { get; }

        /// <summary>
        /// Indent the tag by N levels (relative to current indentation).
        /// </summary>
        /// <param name="n">Number of levels by which the tag will be further indented.</param>
        void Indent(int n);
    }
}
