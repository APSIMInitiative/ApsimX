using System;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace UnitTests.Interop.Documentation
{
    /// <summary>
    /// A mock tag - the rendering action will be used to
    /// render the tag to the document.
    /// </summary>
    /// <remarks>
    /// Before rendering one of these tags, don't forget to call
    /// <see cref="PdfBuilder.UseTagRenderer(ITagRenderer)"/>.
    /// </remarks>
    internal class MockTag : ITag
    {
        /// <summary>
        /// This action will be used to render the tag.
        /// </summary>
        public Action<PdfBuilder> Render { get; private set; }

        /// <summary>
        /// Create a new <see cref="MockTag"/> instance.
        /// </summary>
        /// <param name="renderAction">Action used to render the tag to a pdf document.</param>
        public MockTag(Action<PdfBuilder> renderAction)
        {
            Render = renderAction;
        }
    }
}
