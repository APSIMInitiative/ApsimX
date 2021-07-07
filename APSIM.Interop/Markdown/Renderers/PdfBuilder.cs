using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers.Blocks;
using APSIM.Interop.Markdown.Renderers.Inlines;
using APSIM.Shared.Utilities;
using Markdig.Renderers;
using Markdig.Syntax;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown.Renderers.Extras;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using Color = MigraDocCore.DocumentObjectModel.Color;
#else
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
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
    public class PdfBuilder : RendererBase
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
        /// Are we currently in a list item? If so, calls to <see cref="StartNewParagraph"/>
        /// won't actually create a new paragraph.
        /// </summary>
        private bool inListItem = false;

        /// <summary>
        /// Are we currently writing to a table cell?
        /// </summary>
        private bool inTableCell = false;

        /// <summary>
        /// Index of the current table cell being edited.
        /// </summary>
        /// <remarks>
        /// The way in which table cells are handled by migradoc is quite strange.
        /// </remarks>
        private int tableCellIndex = 0;

        /// <summary>
        /// Create a <see cref="PdfBuilder" /> instance.
        /// </summary>
        /// <param name="doc"></param>
        public PdfBuilder(Document doc, PdfOptions options)
        {
            document = doc;

            ObjectRenderers.Add(new AutolinkInlineRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new HtmlEntityInlineRenderer());
            ObjectRenderers.Add(new HtmlInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer(options.ImagePath));
            ObjectRenderers.Add(new LiteralInlineRenderer());

            ObjectRenderers.Add(new CodeBlockRenderer());
            ObjectRenderers.Add(new HeadingBlockRenderer());
            ObjectRenderers.Add(new HtmlBlockRenderer());
            ObjectRenderers.Add(new ListBlockRenderer());
            ObjectRenderers.Add(new ParagraphBlockRenderer());
            ObjectRenderers.Add(new QuoteBlockRenderer());
            ObjectRenderers.Add(new ThematicBreakBlockRenderer());

            ObjectRenderers.Add(new TableCellRenderer());
            ObjectRenderers.Add(new TableRowRenderer());
            ObjectRenderers.Add(new TableRenderer());
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
        public void SetLinkState(string linkUri)
        {
            if (linkState != null)
                throw new NotImplementedException("Nested links are not supported.");
            linkState = new Link()
            {
                Uri = linkUri,
                LinkObject = GetLastParagraph().AddHyperlink("")
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
        /// Start a new list item. Every call to this function must have a matching
        /// call to <see cref="FinishListItem()" />.
        /// </summary>
        /// <param name="bulletPointSymbol">The bullet point symbol to be used for the list item.</param>
        public void StartListItem(string bulletPointSymbol)
        {
            if (inListItem)
                throw new NotImplementedException("Nested lists not implemented (or programmer is missing a call to FinishListItem())");
            inListItem = true;
            GetLastParagraph().AddText(bulletPointSymbol);
        }

        /// <summary>
        /// Finish a list item.
        /// </summary>
        public void FinishListItem()
        {
            if (!inListItem)
                throw new NotImplementedException("Nested lists not implemented (or programmer is missing a call to StartListItem())");
            inListItem = false;
            GetLastParagraph().AddText(Environment.NewLine);
        }

        /// <summary>
        /// Append text to the PDF document.
        /// </summary>
        /// <param name="text">Text to be appended.</param>
        /// <param name="textStyle">Style to be applied to the text.</param>
        public void AppendText(string text, TextStyle textStyle)
        {
            AppendText(text, textStyle, GetLastParagraph());
        }

        /// <summary>
        /// Append an image to the PDF document.
        /// </summary>
        /// <param name="image">Image to be appended.</param>
        public void AppendImage(Image image)
        {
            AppendImage(image, GetLastParagraph());
        }

        /// <summary>
        /// Create a new table with the specified number of columns.
        /// </summary>
        /// <param name="numColumns">Number of columns in the table.</param>
        public void StartTable(int numColumns)
        {
            if (inTableCell)
                throw new NotImplementedException("Nested tables not implemented.");
            Table table = GetLastSection().AddTable();
            for (int i = 0; i < numColumns; i++)
                table.Columns.AddColumn();
        }

        /// <summary>
        /// Start a new row in the current table.
        /// </summary>
        /// <param name="header"></param>
        public void StartTableRow(bool header)
        {
            try
            {
                if (inTableCell)
                    // The other possibility is that the renderer is missing a call to FinishCell().
                    throw new NotImplementedException("Nested tables not implemented");

                Table table = GetLastSection().GetLastTable();
                if (table.Columns.Count == 0)
                    throw new InvalidOperationException("Table contains no columns");

                Row row = table.AddRow();
                if (header)
                    ApplyRowHeaderStyle(row);

                tableCellIndex = 0;
            }
            catch (Exception err)
            {
                throw new Exception("Unable to create a new table row", err);
            }
        }

        /// <summary>
        /// Start editing a table cell.
        /// </summary>
        public void StartTableCell()
        {
            if (inTableCell)
                // An exception here may be a bit harsh.
                throw new Exception($"Programmer is missing a call to FinishTableCell().");

            // todo: Do we need to add cells to the row?
            inTableCell = true;
        }

        /// <summary>
        /// Finish editing a table cell.
        /// </summary>
        public void FinishTableCell()
        {
            if (!inTableCell)
                throw new Exception($"Programmer is missing a call to StartTableCell().");
            inTableCell = false;
            tableCellIndex++;
        }

        /// <summary>
        /// Append a table to the PDF document.
        /// </summary>
        /// <param name="data">Table to be appended.</param>
        public void AppendTable(DataTable data)
        {
            Table table = GetLastSection().AddTable();

            // Add the columns.
            foreach (DataColumn column in data.Columns)
                table.AddColumn();

            // Add a row containing column headings.
            Row row = table.AddRow();
            ApplyRowHeaderStyle(row);
            for (int i = 0; i < data.Columns.Count; i++)
                row[i].AddParagraph(data.Columns[i].ColumnName);

            // Add the data.
            foreach (DataRow dataRow in data.Rows)
            {
                row = table.AddRow();
                for (int i = 0; i < data.Columns.Count; i++)
                    row[i].AddParagraph(dataRow[i]?.ToString() ?? "");
            }
        }

        /// <summary>
        /// Append a horizontal rule after the last paragraph.
        /// </summary>
        public void AppendHorizontalRule()
        {
            Style hrStyle = GetHRStyle();
            GetLastParagraph().Format = hrStyle.ParagraphFormat;
        }

        /// <summary>
        /// Start a new paragraph. Will do nothing if the last paragraph is empty.
        /// </summary>
        public void StartNewParagraph()
        {
            if (linkState != null)
                throw new InvalidOperationException("Unable to append text to new paragraph when linkState is set (how can a link span multiple paragraphs?). Renderer is missing a call to ClearLinkState().");
            if (!inListItem)
            {
                // Only add a new paragraph if the last paragraph contains any text.
                Paragraph lastParagraph = GetLastParagraph();
                IEnumerable<Text> textElements = lastParagraph.Elements.OfType<FormattedText>().SelectMany(f => f.Elements.OfType<Text>()).Union(lastParagraph.Elements.OfType<Text>());
                if (textElements.Any(t => !string.IsNullOrEmpty(t.Content)))
                {
                    Section section = GetLastSection();
                    if (inTableCell)
                        section.GetLastTable().GetLastRow().Cells[tableCellIndex].AddParagraph();
                    else
                        section.AddParagraph();
                }
            }
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
            {
                string style = CreateStyle(textStyle);
                paragraph.AddFormattedText(text, style);
            }
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
        /// Return the last paragraph in the document. Adds a new paragraph if none
        /// exist in the document.
        /// </summary>
        private Paragraph GetLastParagraph()
        {
            Section section = GetLastSection();
            if (inTableCell)
            {
                Cell cell = section.GetLastTable().GetLastRow().Cells[tableCellIndex];
                Paragraph paragraph = cell.Elements.OfType<Paragraph>().LastOrDefault();
                if (paragraph == null)
                    return cell.AddParagraph();
                else
                    return paragraph;
            }
            else
            {
                if (section.LastParagraph == null)
                    return section.AddParagraph();
                return section.LastParagraph;   
            }
        }

        /// <summary>
        /// Return the last section in the document. Adds a new section if none exist
        /// in the document.
        /// </summary>
        private Section GetLastSection()
        {
            if (document.LastSection == null)
                return document.AddSection();
            return document.LastSection;
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
        private TextStyle GetNestedStyle()
        {
            if (styleStack.Any())
                return styleStack.Aggregate((x, y) => x | y);
            return TextStyle.Normal;
        }

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
            Style documentStyle = document.Styles[name];
            if (documentStyle == null)
                documentStyle = document.Styles.AddStyle(name, document.Styles.Normal.Name);
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
            if (headingLevel != null)
                documentStyle.Font.Size = GetFontSizeForHeading((uint)headingLevel);

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
            if (headingLevel != null)
                result += $"Heading{headingLevel}";
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
    
        /// <summary>
        /// Make a row appear as a table header row.
        /// </summary>
        /// <param name="row">The row to be modified.</param>
        private void ApplyRowHeaderStyle(Row row)
        {
            row.HeadingFormat = true;
            row.Shading.Color = Colors.LightBlue;
            row.Format.Alignment = ParagraphAlignment.Left;
            row.VerticalAlignment = VerticalAlignment.Center;
            // row.Format.Font.Bold = true;
        }
    }
}
