using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using APSIM.Interop.Documentation;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Documentation.Styles;
using APSIM.Interop.Markdown.Tags;
using APSIM.Shared.Utilities;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace APSIM.Interop.Markdown
{
    /// <summary>
    /// Class which renders markdown to a PDF document.
    /// </summary>
    public class MarkdownParser
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
        public MarkdownParser(string imagePath)
        {
            relativePath = imagePath;
        }

        /// <summary>
        /// Renders the given markdown string to a section
        /// of a PDF document.
        /// </summary>
        /// <param name="markdown">Markdown-formatted text.</param>
        public IEnumerable<IMarkdownTag> Parse(string markdown)
        {
            MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().Build();
            MarkdownDocument document = Markdig.Markdown.Parse(markdown, pipeline);
            return Parse(document, new TextStyle());
        }

        private IEnumerable<IMarkdownTag> Parse(IEnumerable<Block> blocks, ITextStyle style)
        {
            foreach (var block in blocks)
            {
                if (block is HeadingBlock header)
                {
                    // hmm
                    yield return new HeadingTag(Parse(header.Inline, style).OfType<TextTag>(), header.Level);
                }
                else if (block is ParagraphBlock paragraph)
                {
                    // hmm
                    yield return new ParagraphTag(Parse(paragraph.Inline, style).OfType<TextTag>());
                }
                else if (block is QuoteBlock quote)
                {
                    foreach (IMarkdownTag tag in Parse(quote, style.MakeQuote()))
                        yield return tag;
                }
                else if (block is ListBlock list)
                {
                    yield return ParseList(list, style);
                }
                else if (block is ListItemBlock listItem)
                {
                    foreach (IMarkdownTag tag in Parse(listItem, style))
                        yield return tag;
                }
                else if (block is CodeBlock code)
                {
                    string text = code.Lines.ToString().TrimEnd(Environment.NewLine.ToCharArray());
                    yield return new ParagraphTag(new TextTag(text, style.MakeCode()));
                }
                else if (block is Table table)
                {
                    yield return CreateTableTag(table, style);
                }
                else if (block is ThematicBreakBlock)
                {
                    // Horizontal rule - tbi.
                    throw new NotImplementedException();
                }
                else
                {
                }
            }
        }

        private IEnumerable<IMarkdownTag> Parse(IEnumerable<Inline> inlines, ITextStyle style)
        {
            foreach (var inline in inlines)
            {
                if (inline is LiteralInline textInline)
                {
                    yield return new TextTag(textInline.Content.ToString(), style);
                }
                else if (inline is EmphasisInline italicInline)
                {
                    foreach (TextTag tag in Parse(italicInline, style.MakeEmphasis(italicInline.DelimiterChar, italicInline.DelimiterCount)))
                        yield return tag;
                }
                else if (inline is LinkInline link)
                {
                    // todo: difference between link.Label and link.Title?
                    if (link.IsImage)
                        // todo: Are there any style components which should affect the way
                        // the image is rendered?
                        yield return new ImageTag(GetImage(link.Url));
                    else
                        yield return new LinkTag(Parse(link, style).OfType<TextTag>(), link.Url);
                }
                //else if (inline is MarkdownLinkInline markdownLinkInline)
                //    textView.Buffer.InsertWithTags(ref insertPos, markdownLinkInline.Inlines[0].ToString(), GetTags("Link", indent, markdownLinkInline.Url));
                else if (inline is CodeInline codeInline)
                {
                    yield return new ParagraphTag(new TextTag(codeInline.Content, style.MakeInlineCode()));
                }
                else if (inline is LineBreakInline br)
                {
                    yield return new TextTag(br.IsHard ? Environment.NewLine : " ", style);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Render an image to a section of a PDF document.
        /// </summary>
        /// <param name="uri">URI of the image. Could be a filename or resource name.</param>
        /// <exception cref="FileNotFoundException" />
        private Image GetImage(string uri)
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
                string imagePath = $"APSIM.Interop.Resources.{uri}";
                using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(imagePath))
                {
                    if (stream == null)
                        throw new FileNotFoundException($"Unable to resolve image path {uri}");
                    image = new Bitmap(stream);
                }
            }

            return image;
        }

        private IMarkdownTag ParseList(ListBlock list, ITextStyle style)
        {
            int itemNumber = 1;
            // listParagraph.Style = style.Indent(1); // fixme
            List<TextTag> listContents = new List<TextTag>();
            foreach (Block listBlock in list)
            {
                if (list.IsOrdered)
                    listContents.Add(new TextTag($"{itemNumber}. ", style));
                else
                    listContents.Add(new TextTag("• ", style));

                listContents.AddRange(Parse(new[] { listBlock }, style).OfType<TextTag>());
                if (itemNumber != list.Count)
                    listContents.Add(new TextTag("\n", style));
                itemNumber++;
            }

            return new ParagraphTag(listContents);
        }

        /// <summary>
        /// Render a table to a section of a PDF document.
        /// </summary>
        /// <param name="table">Table to be rendered.</param>
        /// <param name="style">Style to be applied to table contents.</param>
        private IMarkdownTag CreateTableTag(Table table, ITextStyle style)
        {
            List<Tags.TableRow> rows = new List<Tags.TableRow>();
            for (int i = 0; i < table.Count; i++)
            {
                var row = (Markdig.Extensions.Tables.TableRow)table[i];
                List<Tags.TableCell> rowContents = new List<Tags.TableCell>();
                for (int j = 0; j < Math.Min(table.ColumnDefinitions.Count(), row.Count); j++)
                {
                    if (row[j] is Markdig.Extensions.Tables.TableCell cell)
                    {
                        TableColumnAlign? alignment = table.ColumnDefinitions[j].Alignment;

                        // Recursively process all markdown blocks inside this cell. In
                        // theory, this supports both blocks and inline content. In practice
                        // I wouldn't recommend using blocks inside a table cell.
                        // We also make the cell contents bold/strong if it's the header row.
                        rowContents.Add(new Tags.TableCell(Parse(cell, row.IsHeader ? style.MakeStrong() : style)));
                    }
                    else
                        // This shouldn't happen.
                        throw new Exception($"Unknown cell type {row[(int)j].GetType()}.");
                }
                rows.Add(new Tags.TableRow(rowContents));
            }
            return new TableTag(rows);
        }
    }
}
