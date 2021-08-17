using System;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Services.Documentation;

namespace APSIM.Tests.Interop.Documentation
{
    /// <summary>
    /// Renderer class for a <see cref="MockTag"/>. This simply calls
    /// the tag's rendering action.
    /// </summary>
    internal class MockTagRenderer : TagRendererBase<MockTag>
    {
        /// <summary>
        /// Render the tag using the tag's render action.
        /// </summary>
        /// <param name="tag">The tag to be rendered.</param>
        /// <param name="renderer">Pdf builder API.</param>
        protected override void Render(MockTag tag, PdfBuilder renderer)
        {
            tag.Render(renderer);
        }
    }
}
