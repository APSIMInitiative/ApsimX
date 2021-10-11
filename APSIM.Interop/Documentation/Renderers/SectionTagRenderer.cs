using System.Linq;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render a
    /// <see cref="Section" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class SectionTagRenderer : TagRendererBase<Section>
    {
        /// <summary>
        /// Render the given Paragraph tag to the PDF document.
        /// </summary>
        /// <param name="paragraph">Paragraph tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(Section section, PdfBuilder renderer)
        {
            // If the section contains no content (child tags), then don't
            // bother writing the heading.
            if (!section.IsEmpty())
            {
                // Add a heading at the current heading level.
                if (!string.IsNullOrEmpty(section.Title))
                    renderer.AppendHeading(section.Title);

                // Increment the heading level, so that any child tags' headings
                // are written as subheadings.
                renderer.PushSubHeading();

                // Add child tags.
                foreach (ITag child in section.Children)
                    renderer.Write(child);

                renderer.PopSubHeading();
            }
        }
    }
}
