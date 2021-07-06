using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;
using Markdig.Renderers;
using Markdig.Syntax;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using Color = MigraDocCore.DocumentObjectModel.Color;
#else
using MigraDoc.DocumentObjectModel;
using Color = MigraDoc.DocumentObjectModel.Color;
using System.Drawing.Imaging;
#endif

namespace APSIM.Interop.Markdown.Renderers
{
    /// <summary>
    /// This class exposes an API for building a PDF document.
    /// It's used by tag renderers and our custom markdown object
    /// renderers.
    /// </summary>
    public class PdfRenderer : RendererBase
    {
        private struct Link
        {
            /// <summary>
            /// Link URI.
            /// </summary>
            public string Uri { get; set; }

            /// <summary>
            /// The MigraDoc hyperlink object.
            /// </summary>
            public Hyperlink LinkObject { get; set; }
        }

        /// <summary>
        /// Conversion factor from points to pixels.
        /// </summary>
        private const double pointsToPixels = 96.0 / 72;

        /// <summary>
        /// The PDF Document.
        /// </summary>
        private Document document;

        /// <summary>
        /// Style stack. This is used to manage styles for nested inline/block elements.
        /// </summary>
        /// <typeparam name="TextStyle">Text styles.</typeparam>
        private Stack<TextStyle> styleStack = new Stack<TextStyle>();

        /// <summary>
        /// Link state. This allows for link style/state to be applied to nested
        /// children of a link container.
        /// </summary>
        /// <remarks>
        /// Technically we could use a stack here, as we do for text style. However,
        /// I'm not really sure what nested links look like, or how they should be
        /// rendered. So for now, nested links will throw a not implemented exception.
        /// </remarks>
        private Link? linkState = null;

        /// <summary>
        /// Current heading state. If not null, this is set to the heading level
        /// of the current text. This should really be incorporated somehow into TextStyle.
        /// </summary>
        private uint? headingLevel = null;

        /// <summary>
        /// Create a <see cref="PdfRenderer" /> instance.
        /// </summary>
        /// <param name="doc"></param>
        public PdfRenderer(Document doc)
        {
            document = doc;
        }

        /// <summary>
        /// Render the markdown object to the PDF document.
        /// </summary>
        /// <param name="markdownObject">The markdown object to be rendered.</param>
        public override object Render(MarkdownObject markdownObject)
        {
            Write(markdownObject);
            return document;
        }

        /// <summary>
        /// Push a style onto the style stack. This style will be applied to
        /// all additions to the PDF until it is removed, via <see cref="PopStyle"/>.
        /// </summary>
        /// <param name="style">The style to be added to the style stack.</param>
        public void PushStyle(TextStyle style)
        {
            styleStack.Push(style);
        }

        /// <summary>
        /// Pop a style from the style stack.
        /// </summary>
        public void PopStyle()
        {
            try
            {
                styleStack.Pop();
            }
            catch (Exception err)
            {
                throw new InvalidOperationException("Markdown renderer is missing a call to ApplyNestedStyle()", err);
            }
        }

        /// <summary>
        /// Set the current link state. It is an error to call this if the
        /// link state is already set (ie in the case of nested links).
        /// Every call to <see cref="SetLinkState"/> *must* have a matching call to
        /// <see cref="ClearLinkState"/>.
        /// </summary>
        /// <param name="linkUri">Link URI.</param>
        /// <param name="newParagraph">Should the link be added to a new paragraph (true) or the last paragraph (false)?</param>
        public void SetLinkState(string linkUri, bool newParagraph)
        {
            if (linkState != null)
                throw new NotImplementedException("Nested links are not supported.");
            linkState = new Link()
            {
                Uri = linkUri,
                LinkObject = GetParagraph(newParagraph).AddHyperlink("")
            };
        }

        /// <summary>
        /// Clear the current link state. This should be called after rendering
        /// all children of a link.
        /// </summary>
        public void ClearLinkState()
        {
            if (linkState == null)
                // Should this be a Debug.WriteLine()?
                throw new InvalidOperationException("Renderer is missing a call to SetLinkState()");
            linkState = null;
        }

        /// <summary>
        /// Set the heading level. All calls to this function should be accompanied
        /// by a call to <see cref="ClearHeadingLevel" />. It is an error to set the
        /// heading level if the heading level is already set (ie nested headings).
        /// </summary>
        /// <param name="headingLevel">Heading level.</param>
        public void SetHeadingLevel(uint headingLevel)
        {
            if (this.headingLevel != null)
                throw new NotImplementedException("Nested headings not supported.");
            this.headingLevel = headingLevel;
        }

        /// <summary>
        /// Clear the currenet heading level. This should be called after rendering
        /// the contents of a heading.
        /// </summary>
        public void ClearHeadingLevel()
        {
            if (headingLevel == null)
                throw new InvalidOperationException("Heading level is already null. Renderer is missing a call to SetHeadingLevel().");
            headingLevel = null;
        }

        /// <summary>
        /// Append text to the PDF document.
        /// </summary>
        /// <param name="text">Text to be appended.</param>
        /// <param name="textStyle">Style to be applied to the text.</param>
        /// <param name="newParagraph">Should the text be added to a new paragraph (true) or the last paragraph (false)?</param>
        public void AppendText(string text, TextStyle textStyle, bool newParagraph)
        {
            if (linkState != null && newParagraph)
                throw new InvalidOperationException("Unable to append text to new paragraph when linkState is set. Renderer is missing a call to ClearLinkState().");
            AppendText(text, textStyle, GetParagraph(newParagraph));
        }

        /// <summary>
        /// Append an image to the PDF document.
        /// </summary>
        /// <param name="image">Image to be appended.</param>
        /// <param name="newParagraph">Should the image be added to a new paragraph (true) or the last paragraph (false)?</param>
        public void AppendImage(Image image, bool newParagraph)
        {
            if (linkState != null && newParagraph)
                throw new InvalidOperationException("Unable to append text to new paragraph when linkState is set. Renderer is missing a call to ClearLinkState().");
            AppendImage(image, GetParagraph(newParagraph));
        }

        /// <summary>
        /// Append a horizontal rule after the last paragraph.
        /// </summary>
        public void AppendHorizontalRule()
        {
            Style hrStyle = GetHRStyle();
            document.LastSection.LastParagraph.Format = hrStyle.ParagraphFormat;
        }

        /// <summary>
        /// Append text to the given paragraph.
        /// </summary>
        /// <param name="text">Text to be appended.</param>
        /// <param name="textStyle">Style to be applied to the text.</param>
        /// <param name="paragraph">Paragraph to which the text should be appended.</param>
        private void AppendText(string text, TextStyle textStyle, Paragraph paragraph)
        {
            if (linkState == null)
                paragraph.AddFormattedText(text, CreateStyle(textStyle));
            else
                ((Link)linkState).LinkObject.AddFormattedText(text, CreateStyle(textStyle));
        }

        /// <summary>
        /// Append an image to the PDF document.
        /// </summary>
        /// <param name="image">Image to be appended.</param>
        /// <param name="paragraph">The paragraph to which the image should be appended.</param>
        private void AppendImage(Image image, Paragraph paragraph)
        {
            // The image could potentially be too large. Therfore we read it,
            // adjust the size to fit the page better (if necessary), and add
            // the modified image to the paragraph.

            GetPageSize(paragraph.Section, out double pageWidth, out double pageHeight);
#if NETFRAMEWORK
            string path = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            ReadAndResizeImage(image, pageWidth, pageHeight).Save(path, ImageFormat.Png);
            if (linkState == null)
                paragraph./*Section.*/AddImage(path);
            else
                //((Link)linkState).LinkObject.AddImage(path);
                throw new NotImplementedException("tbi: image in hyperlink in .net framework builds");
#else
            // Note: the first argument passed to the FromStream() function
            // is the name of the image. Thist must be unique throughout the document,
            // otherwise you will run into problems with duplicate imgages.
            IImageSource imageSource = ImageSource.FromStream(Guid.NewGuid().ToString().Replace("-", ""), () =>
            {
                image = ReadAndResizeImage(image, pageWidth, pageHeight);
                Stream stream = new MemoryStream();
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });
            if (linkState == null)
                paragraph.AddImage(imageSource);
            else
                ((Link)linkState).LinkObject.AddImage(imageSource);
#endif
        }

        /// <summary>
        /// Get the page size in pixels.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="pageWidth"></param>
        /// <param name="pageHeight"></param>
        private static void GetPageSize(Section section, out double pageWidth, out double pageHeight)
        {
            PageSetup pageSetup = section.PageSetup;
            if (pageSetup.PageWidth.Point == 0 || pageSetup.PageHeight.Point == 0)
                pageSetup = section.Document.DefaultPageSetup;
            pageWidth = (pageSetup.PageWidth.Point - pageSetup.LeftMargin.Point - pageSetup.RightMargin.Point) * pointsToPixels;
            pageHeight = (pageSetup.PageHeight.Point - pageSetup.TopMargin.Point - pageSetup.BottomMargin.Point) * pointsToPixels;
        }

        /// <summary>
        /// Ensures that an image's dimensions are less than the specified target
        /// width and height, resizing the image as necessary without changing the
        /// aspect ratio.
        /// </summary>
        /// <param name="image">The image to be resized.</param>
        /// <param name="targetWidth">Max allowed width of the image in pixels.</param>
        /// <param name="targetHeight">Max allowed height of the image in pixels.</param>
        private static Image ReadAndResizeImage(Image image, double targetWidth, double targetHeight)
        {
            if ( (targetWidth > 0 && image.Width > targetWidth) || (targetHeight > 0 && image.Height > targetHeight) )
                image = ImageUtilities.ResizeImage(image, targetWidth, targetHeight);
            return image;
        }

        /// <summary>
        /// Get the paragraph specified by the newParagraph argument.
        /// </summary>
        /// <param name="newParagraph">Create a new paragraph (true) or the most recent paragraph (false)?</param>
        private Paragraph GetParagraph(bool newParagraph)
        {
            return newParagraph ? document.LastSection.LastParagraph : document.LastSection.AddParagraph();
        }

        /// <summary>
        /// Get a font size for a given heading level.
        /// I'm not sure what units this is using - I've just copied
        /// it from existing code in ApsimNG.
        /// </summary>
        /// <param name="headingLevel">The heading level.</param>
        /// <returns></returns>
        protected Unit GetFontSizeForHeading(uint headingLevel)
        {
            switch (headingLevel)
            {
                case 1:
                    return 14;
                case 2:
                    return 12;
                case 3:
                    return 11;
                case 4:
                    return 10;
                case 5:
                    return 9;
                default:
                    return 8;
            }
        }

        /// <summary>
        /// Get the aggregated style from the style stack.
        /// </summary>
        private TextStyle GetNestedStyle() => styleStack.Aggregate((x, y) => x | y);

        /// <summary>
        /// Create a style in the document corresponding to the given
        /// text style and return the name of the style.
        /// </summary>
        /// <param name="style">The style to be created.</param>
        protected string CreateStyle(TextStyle style)
        {
            style |= GetNestedStyle();
            string name = GetStyleName(style);
            if (string.IsNullOrEmpty(name))
                return document.Styles.Normal.Name;
            Style documentStyle = document.Styles.AddStyle(name, document.Styles.Normal.Name);
            //else /* if (!string.IsNullOrEmpty(baseName)) */
            //{
            //    documentStyle = document.Styles[name];
            //    if (documentStyle == null)
            //        documentStyle = document.Styles.AddStyle(name, baseName);
            //}

            if ( (style & TextStyle.Italic) == TextStyle.Italic)
                documentStyle.Font.Italic = true;
            if ( (style & TextStyle.Strong) == TextStyle.Strong)
                documentStyle.Font.Bold = true;
            if ( (style & TextStyle.Underline) == TextStyle.Underline)
                // MigraDoc actually supports different line styles.
                documentStyle.Font.Underline = Underline.Single;
            if ( (style & TextStyle.Strikethrough) == TextStyle.Strikethrough)
                throw new NotImplementedException();
            if ( (style & TextStyle.Superscript) == TextStyle.Superscript)
                documentStyle.Font.Superscript = true;
            if ( (style & TextStyle.Subscript) == TextStyle.Subscript)
                documentStyle.Font.Subscript = true;
            if ( (style & TextStyle.Quote) == TextStyle.Quote)
            {
                // Shading shading = new Shading();
                // shading.Color = new MigraDocCore.DocumentObjectModel.Color(122, 130, 139);
                // documentStyle.ParagraphFormat.Shading = shading;
                documentStyle.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1);
                documentStyle.Font.Color = new Color(122, 130, 139);
            }
            if ( (style & TextStyle.Code) == TextStyle.Code)
            {
                // TBI - shading, syntax highlighting?
                documentStyle.Font.Name = "monospace";
            }
            // todo: do we need to set link style here?
            if (linkState != null)
            {
                // Links can be blue and underlined.
                documentStyle.Font.Color = new Color(0x08, 0x08, 0xef);
                documentStyle.Font.Underline = Underline.Single;
            }

            return name;
        }

        /// <summary>
        /// Generate a name for the given style object. The style name generated by
        /// this function is deterministic in that if all style objects with the same
        /// property values will generate the same style names.
        /// 
        /// This means that the PDF document will have 1 style for bold text, 1 style
        /// for bold + italic text, etc.
        /// </summary>
        /// <param name="style">Text style.</param>
        protected string GetStyleName(TextStyle style)
        {
            string result = style.ToString().Replace(", ", "");
            if (linkState != null)
                result += "Link";
            return result;
        }
    
        /// <summary>
        /// Get a horizontal rule style.
        /// </summary>
        /// <returns></returns>
        private Style GetHRStyle()
        {
            string styleName = "HorizontalRule";
            if (document.Styles[styleName] != null)
                return document.Styles[styleName];
            Style style = document.Styles.AddStyle(styleName, document.Styles.Normal.Name);
            Border hr = new Border();
            hr.Width = Unit.FromPoint(1);
            hr.Color = Colors.DarkGray;
            style.ParagraphFormat.Borders.Bottom = hr;
            style.ParagraphFormat.LineSpacing = 0;
            style.ParagraphFormat.SpaceBefore = 15;
            return style;
        }
    }
}
