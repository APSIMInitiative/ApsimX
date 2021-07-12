using APSIM.Services.Documentation;
using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Interop.Documentation.Renderers;
using System.Linq;
using APSIM.Interop.Markdown.Renderers;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using MigraDocCore.Rendering;
using PdfSharpCore.Fonts;
using SixLabors.ImageSharp.PixelFormats;
#else
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
#endif

namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This class will generate a PDF file from a collection of tags.
    /// </summary>
    public class PdfWriter
    {
#if NETCOREAPP
        /// <summary>
        /// Static constructor to initialise PDFSharp ImageSource.
        /// </summary>
        static PdfWriter()
        {
            if (ImageSource.ImageSourceImpl == null)
                ImageSource.ImageSourceImpl = new PdfSharpCore.Utils.ImageSharpImageSource<Rgba32>();
        }
#endif

        /// <summary>
        /// Cache for looking up renderers based on tag type.
        /// </summary>
        /// <typeparam name="Type">Tag type.</typeparam>
        /// <typeparam name="ITagRenderer">Renderer instance capable of rendering the matching type.</typeparam>
        /// <returns></returns>
        private Dictionary<Type, ITagRenderer> renderersLookup = new Dictionary<Type, ITagRenderer>();

        /// <summary>
        /// Renderers which this PDF writer will use to write the tags to the PDF document.
        /// </summary>
        private IEnumerable<ITagRenderer> renderers = DefaultRenderers();

        /// <summary>
        /// PDF generation options.
        /// </summary>
        private PdfOptions options;

        /// <summary>
        /// Construct a <see cref="PdfWriter" /> instance with default settings.
        /// </summary>
        public PdfWriter()
        {
            options = PdfOptions.Default;
        }

        /// <summary>
        /// Construct a <see cref="PdfWriter" /> instance with a custom image search path.
        /// </summary>
        /// <param name="options">PDF generation options.</param>
        public PdfWriter(PdfOptions options)
        {
            this.options = options;
        }

        /// <summary>
        /// Convert a given list of tags into a PDF document and
        /// save the document to the given path.
        /// </summary>
        /// <param name="fileName">File name of the generated pdf.</param>
        /// <param name="tags">Tags to be converted to a PDF.</param>
        public void Write(string fileName, IEnumerable<ITag> tags)
        {
            // This is a bit tricky on non-Windows platforms. 
            // Normally PdfSharp tries to get a Windows DC for associated font information
            // See https://alex-maz.info/pdfsharp_150 for the work-around we can apply here.
            // See also http://stackoverflow.com/questions/32726223/pdfsharp-migradoc-font-resolver-for-embedded-fonts-system-argumentexception
            // The work-around is to register our own fontresolver. We don't need to do this on Windows.
            if (!ProcessUtilities.CurrentOS.IsWindows && !(GlobalFontSettings.FontResolver is FontResolver))
                GlobalFontSettings.FontResolver = new FontResolver();

            Document pdf = CreateStandardDocument();
            PdfBuilder pdfRenderer = new PdfBuilder(pdf, options);

            foreach (ITag tag in tags)
                Write(tag, pdfRenderer);

#if NETCOREAPP
            var paragraphs = new List<MigraDocCore.DocumentObjectModel.Paragraph>();
            foreach (MigraDocCore.DocumentObjectModel.Section section in pdf.Sections)
                foreach (var paragraph in section.Elements.OfType<MigraDocCore.DocumentObjectModel.Paragraph>())
                    paragraphs.Add(paragraph);
            MigraDocCore.DocumentObjectModel.Visitors.PdfFlattenVisitor visitor = new MigraDocCore.DocumentObjectModel.Visitors.PdfFlattenVisitor();
#endif
            PdfDocumentRenderer renderer = new PdfDocumentRenderer(false);
            renderer.Document = pdf;
            renderer.RenderDocument();
            renderer.Save(fileName);
        }

        /// <summary>
        /// Find an appropriate tag renderer, and use it to render the
        /// given tag to the PDF document.
        /// </summary>
        /// <param name="tag">Tag to be rendered.</param>
        /// <param name="pdfRenderer">PDF renderer to be used by the tag renderer.</param>
        private void Write(ITag tag, PdfBuilder pdfRenderer)
        {
            ITagRenderer tagRenderer = GetTagRenderer(tag);
            tagRenderer.Render(tag, pdfRenderer);
        }

        /// <summary>
        /// Get a tag renderer capcable of rendering the given tag.
        /// Throws if no suitable renderer is found.
        /// </summary>
        /// <param name="tag">The tag to be rendered.</param>
        private ITagRenderer GetTagRenderer(ITag tag)
        {
            Type tagType = tag.GetType();
            if (!renderersLookup.TryGetValue(tagType, out ITagRenderer tagRenderer))
            {
                tagRenderer = renderers.FirstOrDefault(r => r.CanRender(tag));
                if (tagRenderer == null)
                    throw new NotImplementedException($"Unknown tag type {tag.GetType()}: no matching renderers found.");
                renderersLookup[tagType] = tagRenderer;
            }
            return tagRenderer;
        }

        private static Document CreateStandardDocument()
        {
            Document document = new Document();

            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(1);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(1);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(1);
            document.Styles.Normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(10);

            document.AddSection().AddParagraph();
            return document;
        }

        /// <summary>
        /// Get the default tag renderers.
        /// </summary>
        private static IEnumerable<ITagRenderer> DefaultRenderers()
        {
            List<ITagRenderer> result = new List<ITagRenderer>(7);
            // result.Add(new HeadingTagRenderer());
            result.Add(new ImageTagRenderer());
            result.Add(new ParagraphTagRenderer());
            return result;
        }
    }
}
