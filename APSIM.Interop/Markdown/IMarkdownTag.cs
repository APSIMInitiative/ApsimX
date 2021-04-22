namespace APSIM.Interop.Markdown
{
    public interface IMarkdownTag
    {
        /// <summary>
        /// Render the tag.
        /// </summary>
        /// <param name="renderer">The markdown renderer.</param>
        void Render(IMarkdownRenderer renderer);
    }
}
