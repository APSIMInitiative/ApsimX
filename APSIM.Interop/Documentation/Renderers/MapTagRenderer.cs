using System;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using Models.Mapping;
using System.Drawing;
using APSIM.Interop.Mapping;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render an
    /// <see cref="ITag" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class MapTagRenderer : TagRendererBase<MapTag>
    {
        /// <summary>
        /// Render the given Map tag to the PDF document.
        /// </summary>
        /// <param name="map">Map tag to be rendered.</param>
        /// <param name="document">PDF renderer to use for rendering the tag.</param>
        protected override void Render(MapTag map, PdfBuilder document)
        {
            document.GetPageSize(out double width, out double height);
            int resolution = (int)Math.Min(width, height);

            document.StartNewParagraph();
            document.AppendImage(map.ToImage(resolution));
            document.StartNewParagraph();
        }
    }
}
