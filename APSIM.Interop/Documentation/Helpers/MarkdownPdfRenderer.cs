using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Shared.Utilities;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
#else
using MigraDoc.DocumentObjectModel;
#endif

namespace APSIM.Interop.Documentation.Helpers
{
    /// <summary>
    /// Class which renders markdown to a PDF document.
    /// </summary>
    public class MarkdownPdfRenderer
    {
        /// <summary>
        /// Image names in markdown can be specified as relative paths. If so,
        /// they will be assumed to be relative to this directory.
        /// </summary>
        private string relativePath;

        /// <summary>
        /// Create a renderer instance.
        /// </summary>
        /// <param name="imagePath">Path used to resolve relative paths to images in markdown.</param>
        public MarkdownPdfRenderer(string imagePath)
        {
            relativePath = imagePath;
        }

        /// <summary>
        /// Renders the given markdown string to a section
        /// of a PDF document.
        /// </summary>
        /// <param name="markdown">Markdown-formatted text.</param>
        /// <param name="section">Section of a PDF document to which the markdown will be rendered.</param>
        public void Render(string markdown, Section section)
        {
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().Build();
            MarkdownDocument document = Markdown.Parse(markdown, pipeline);
            Render(document, section, section.Document.Styles.Normal);
        }

        private void Render(IEnumerable<Block> blocks, Section section, Style style)
        {
            foreach (var block in blocks)
            {
                if (block is HeadingBlock header)
                {
                    Render(header.Inline, section, style.MakeHeading(header.Level));
                }
                else if (block is ParagraphBlock paragraph)
                {
                    Render(paragraph.Inline, section, style);
                }
                else if (block is QuoteBlock quote)
                {
                    Render(quote, section, style.MakeQuote());
                }
                else if (block is ListBlock list)
                {
                    int itemNumber = 1;
                    Paragraph listParagraph = section.AddParagraph();
                    // listParagraph.Style = style.Indent(1); // fixme
                    TextFormat format = style.GetTextFormat();
                    foreach (Block listBlock in list)
                    {
                        if (list.IsOrdered)
                        {
                            listParagraph.AddFormattedText($"{itemNumber}. ", format);
                        }
                        else
                        {
                            listParagraph.AddFormattedText("• ", format);
                        }
                        // fixme - this will append to a new paragraph.
                        Render(new[] { listBlock }, section, style);
                        if (itemNumber != list.Count)
                            listParagraph.AddText("\n");
                        itemNumber++;
                    }
                }
                else if (block is ListItemBlock listItem)
                {
                    Render(listItem, section, style);
                }
                else if (block is CodeBlock code)
                {
                    string text = code.Lines.ToString().TrimEnd(Environment.NewLine.ToCharArray());
                    Paragraph codeParagraph = section.AddParagraph();
                    codeParagraph.AddFormattedText(text, style.MakeCode().GetTextFormat());
                }
                else if (block is Table table)
                {
                    RenderTable(table, section, style);
                }
                else if (block is ThematicBreakBlock)
                {
                    // Horizontal rule - tbi.
                }
                else
                {
                }
            }
        }

        private void Render(IEnumerable<Inline> inlines, Section section, Style style)
        {
            foreach (var inline in inlines)
            {
                if (inline is LiteralInline textInline)
                {
                    section.AddParagraph(textInline.Content.ToString(), style);
                }
                else if (inline is EmphasisInline italicInline)
                {
                    Render(italicInline, section, style.MakeEmphasis(italicInline.DelimiterChar, italicInline.DelimiterCount));
                }
                else if (inline is LinkInline link)
                {
                    // todo: difference between link.Label and link.Title?
                    if (link.IsImage)
                        // todo: Are there any style components which should affect the way
                        // the image is rendered?
                        RenderImage(link.Url, section);
                    else
                        Render(link, section, style.MakeLink());
                }
                //else if (inline is MarkdownLinkInline markdownLinkInline)
                //    textView.Buffer.InsertWithTags(ref insertPos, markdownLinkInline.Inlines[0].ToString(), GetTags("Link", indent, markdownLinkInline.Url));
                else if (inline is CodeInline codeInline)
                {
                    section.AddParagraph(codeInline.Content, style.MakeInlineCode());
                }
                else if (inline is LineBreakInline br)
                    // todo: account for hard vs soft line breaks?
                    section.AddParagraph();
                else
                {
                }
            }
        }

        /// <summary>
        /// Render an image to a section of a PDF document.
        /// </summary>
        /// <param name="uri">URI of the image. Could be a filename or resource name.</param>
        /// <param name="section">Section of a PDF documented to which the image will be rendered.</param>
        /// <param name="style">Style to be applied to the image.</param>
        private void RenderImage(string uri, Section section)
        {
            string absolutePath = PathUtilities.GetAbsolutePath(uri, relativePath);
            Image image = null;
            if (File.Exists(absolutePath))
            {
                // Image.FromFile() will cause the file to be locked until the image is disposed of. 
                // This workaround allows us to immediately release the lock on the file.
                using (Bitmap bmp = new Bitmap(absolutePath))
                    image = new Bitmap(bmp);
            }
            else
            {
                string imagePath = $"ApsimNG.Resources.{uri}";
                // todo: needs further thought
                throw new NotImplementedException();
            }

            if (image != null)
                section.AddResizeImage(image);
        }

        /// <summary>
        /// Render a table to a section of a PDF document.
        /// </summary>
        /// <param name="table">Table to be rendered.</param>
        /// <param name="section">Section of a PDF documented to which the table will be rendered.</param>
        /// <param name="style">Style to be applied to table contents.</param>
        private void RenderTable(Table table, Section section, Style style)
        {
            // tbi
            throw new NotImplementedException();
        }
    }
}