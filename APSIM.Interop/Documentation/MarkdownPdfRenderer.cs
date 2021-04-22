using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using APSIM.Interop.Markdown.Tags;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Color = MigraDocCore.DocumentObjectModel.Color;
#else
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using Paragraph = MigraDoc.DocumentObjectModel.Paragraph;
using Color = MigraDoc.DocumentObjectModel.Color;
#endif

namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This class renderers markdown tags to a PDF document.
    /// </summary>
    internal class MarkdownPdfRenderer : IMarkdownRenderer
    {
        /// <summary>
        /// The PDF Document.
        /// </summary>
        private Document document;

        public MarkdownPdfRenderer(Document document) => this.document = document;

        public virtual void AddHeading(IEnumerable<TextTag> contents, int headingLevel)
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            AddHeading(paragraph, headingLevel, contents);
        }

        public virtual void AddParagraph(IEnumerable<TextTag> contents)
        {
            Paragraph paragraph = document.LastSection.AddParagraph();
            foreach (TextTag tag in contents)
                AddToParagraph(paragraph, tag);
        }

        public virtual void AddImage(Image image)
        {
            document.LastSection.AddResizeImage(image);
        }

        public virtual void AddTable(IEnumerable<TableRow> rows)
        {
            if (rows == null || !rows.Any())
                return;

            Table table = document.LastSection.AddTable();
            foreach (TableCell cell in rows.First().Cells)
                table.AddColumn();
            foreach (TableRow row in rows)
            {
                Row tableRow = table.Rows.AddRow();
                int i = 0;
                foreach (TableCell cell in row.Cells)
                {
                    if (table.Columns.Count <= i)
                        table.Columns.AddColumn();
                    CellRenderer renderer = new CellRenderer(tableRow.Cells[i]);
                    foreach (IMarkdownTag tag in cell.Contents)
                        tag.Render(renderer);
                    i++;
                }
            }
        }

        public virtual void AddLink(IEnumerable<TextTag> contents, string uri)
        {
            AddLinkToParagraph(document.LastSection.LastParagraph, contents, uri);
        }

        protected void AddHeading(Paragraph paragraph, int headingLevel, IEnumerable<TextTag> contents)
        {
            if (headingLevel <= 0)
                throw new ArgumentException("Heading level must be positive");

            string headingStyleName = $"heading{headingLevel}";
            document.Styles.AddStyle(headingStyleName, document.Styles.Normal.Name);
            document.Styles[headingStyleName].Font.Size = GetFontSizeForHeading(headingLevel);
            if (headingLevel == 1)
                document.Styles[headingStyleName].Font.Bold = true;

            foreach (TextTag tag in contents)
                AddToParagraph(paragraph, tag, headingStyleName);
        }
    
        protected void AddToParagraph(Paragraph paragraph, TextTag tag, string baseStyle = null)
        {
            string style = CreateStyle(tag.Style, baseStyle);
            paragraph.AddFormattedText(tag.Content, style);
        }

        protected void AddLinkToParagraph(Paragraph paragraph, IEnumerable<TextTag> contents, string uri)
        {
            Hyperlink link = paragraph.AddHyperlink(uri);
            foreach (TextTag tag in contents)
                link.AddFormattedText(tag.Content, CreateStyle(tag.Style));
        }

        /// <summary>
        /// Get a font size for a given heading level.
        /// I'm not sure what units this is using - I've just copied
        /// it from existing code in ApsimNG.
        /// </summary>
        /// <param name="headingLevel">The heading level.</param>
        /// <returns></returns>
        protected Unit GetFontSizeForHeading(int headingLevel)
        {
            if (headingLevel <= 0)
                throw new ArgumentException("Heading level must be positive");
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
        /// Create a style in the document corresponding to the given
        /// text style and return the name of the style.
        /// </summary>
        /// <param name="style">The style to be created.</param>
        protected string CreateStyle(ITextStyle style, string baseName = null)
        {
            string name = GetStyleName(style, baseName);
            if (string.IsNullOrEmpty(name))
                return document.Styles.Normal.Name;
            Style documentStyle;
            if (string.IsNullOrEmpty(baseName))
                documentStyle = document.Styles.AddStyle(name, document.Styles.Normal.Name);
            else
            {
                documentStyle = document.Styles[name];
                if (documentStyle == null)
                    documentStyle = document.Styles.AddStyle(name, baseName);
            }

            if (style.Italic)
                documentStyle.Font.Italic = true;
            if (style.Strong)
                documentStyle.Font.Bold = true;
            if (style.Underline)
                // MigraDoc actually supports different line styles.
                documentStyle.Font.Underline = Underline.Single;
            if (style.Strikethrough)
                throw new NotImplementedException();
            if (style.Superscript)
                documentStyle.Font.Superscript = true;
            if (style.Subscript)
                documentStyle.Font.Subscript = true;
            if (style.Quote)
            {
                // Shading shading = new Shading();
                // shading.Color = new MigraDocCore.DocumentObjectModel.Color(122, 130, 139);
                // documentStyle.ParagraphFormat.Shading = shading;
                documentStyle.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1);
                documentStyle.Font.Color = new Color(122, 130, 139);
            }
            if (style.Code)
            {
                // TBI - shading, syntax highlighting?
                documentStyle.Font.Name = "monospace";
            }

            return name;
        }

        protected string GetStyleName(ITextStyle style, string baseName = null)
        {
            StringBuilder styleName = new StringBuilder();
            if (!string.IsNullOrEmpty(baseName))
                styleName.Append(baseName);
            if (style.Italic)
                styleName.Append("italic");
            if (style.Strong)
                styleName.Append("strong");
            if (style.Underline)
                styleName.Append("underline");
            if (style.Strikethrough)
                styleName.Append("strikethrough");
            if (style.Superscript)
                styleName.Append("superscript");
            if (style.Subscript)
                styleName.Append("subscript");
            if (style.Quote)
                styleName.Append("quote");
            if (style.Code)
                styleName.Append("code");
            return styleName.ToString();
        }
    }
}
