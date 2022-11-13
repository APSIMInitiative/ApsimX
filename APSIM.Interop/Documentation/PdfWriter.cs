using APSIM.Shared.Documentation;
using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Interop.Documentation.Renderers;
using System.Linq;
using APSIM.Interop.Markdown.Renderers;

using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using MigraDocCore.Rendering;
using PdfSharpCore.Fonts;
using SixLabors.ImageSharp.PixelFormats;


namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This class will generate a PDF file from a collection of tags.
    /// </summary>
    public class PdfWriter
    {
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
        public void Write(string fileName, IEnumerable<ITag> tags, bool vertical = true)
        {
            Document pdf = CreateStandardDocument(vertical);
            PdfBuilder pdfRenderer = new PdfBuilder(pdf, options);

            Write(tags, pdfRenderer);
            pdfRenderer.WriteBibliography();

            Save(pdf, fileName);
        }

        public static void Save(Document document, string fileName)
        {
            PdfDocumentRenderer renderer = new PdfDocumentRenderer(false);
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.Save(fileName);
        }

        /// <summary>
        /// Write the tags to the given pdf document.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="document"></param>
        public void Write(IEnumerable<ITag> tags, PdfBuilder document)
        {
            foreach (ITag tag in tags)
                document.Write(tag);
        }

        public static Document CreateStandardDocument(bool vertical = true)
        {
            Document document = new Document();
            var section = document.AddSection();

            PageSetup pageSetup = document.DefaultPageSetup.Clone();
            pageSetup.LeftMargin = Unit.FromCentimeter(1);
            pageSetup.RightMargin = Unit.FromCentimeter(1);
            pageSetup.TopMargin = Unit.FromCentimeter(1);
            pageSetup.BottomMargin = Unit.FromCentimeter(1);
            pageSetup.Orientation = vertical ? Orientation.Portrait : Orientation.Landscape;
            section.PageSetup = pageSetup;

            document.Styles.Normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(10);

            section.AddParagraph();

            PdfBuilder builder = new PdfBuilder(document, PdfOptions.Default);
            builder.AppendImage(Image.LoadFromResource("AIBanner.png"));
            document.LastSection.AddParagraph();

            return document;
        }
    }
}
