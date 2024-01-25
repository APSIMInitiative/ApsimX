using System;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;

namespace APSIM.Interop.Documentation.Renderers
{
    // /// <summary>
    // /// A class which can use a <see cref="PdfBuilder" /> to render an
    // /// <see cref="ITag" /> to a PDF document.
    // /// </summary>
    // /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    // internal class HeadingTagRenderer : TagRendererBase<Heading>
    // {
    //     /// <summary>
    //     /// Render the given heading tag to the PDF document.
    //     /// </summary>
    //     /// <param name="heading">Tag to be rendered.</param>
    //     /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
    //     protected override void Render(Heading heading, PdfBuilder renderer)
    //     {
    //         // Start a new paragraph before and after this tag.
    //         renderer.StartNewParagraph();

    //         renderer.SetHeadingLevel(heading.HeadingLevel);
    //         renderer.AppendText(heading.Text, Markdown.TextStyle.Normal);
    //         renderer.ClearHeadingLevel();

    //         renderer.StartNewParagraph();
    //     }
    // }
}
