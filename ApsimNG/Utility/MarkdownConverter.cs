using HtmlAgilityPack;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UserInterface.Classes;

namespace Utility
{
    /// <summary>
    /// Utility class which encapsulates markdown -> HTML conversion process.
    /// </summary>
    internal class MarkdownConverter
    {
        /// <summary>
        /// Converts a markdown string to HTML.
        /// </summary>
        /// <param name="markdown">A markdown string.</param>
        /// <remarks>
        /// Image URIs are assumed to be names of embedded resources. These
        /// images are written to disk (in temp directory).
        /// </remarks>
        public static string ToHtml(string markdown)
        {
            if (markdown == null)
                return null;

            MarkdownDocument doc = MarkdownParser.Parse(markdown);
            var renderer = new HtmlRenderer(new StringWriter());
            renderer.BaseUrl = new Uri(Path.GetTempPath());
            renderer.Render(doc);
            renderer.Writer.Flush();

            string html = renderer.Writer.ToString();

            // Search for all image nodes, and extract matching resource file to temp directory.
            return ParseHtmlImages(html);
        }

        /// <summary>
        /// Checks the src attribute for all images in the HTML, and attempts to
        /// find a resource of the same name. If the resource exists, it is
        /// written to a temporary file and the image's src is changed to point
        /// to the temp file.
        /// </summary>
        /// <param name="html">String containing valid HTML.</param>
        /// <returns>The modified HTML.</returns>
        /// <remarks>
        /// This currently uses an xpath-based lookup but could (should?) be rewritten
        /// to use the syntax tree exposed by the new markdown library.
        /// </remarks>
        private static string ParseHtmlImages(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            // Find images via xpath.
            var images = doc.DocumentNode.SelectNodes(@"//img");
            if (images != null)
            {
                foreach (HtmlNode image in images)
                {
                    string src = image.GetAttributeValue("src", null);
                    Uri uri = new Uri(src);
                    if (!string.IsNullOrEmpty(src) && !File.Exists(uri.AbsolutePath))
                    {
                        string tempFileName = HtmlToMigraDoc.GetImagePath(uri.AbsolutePath, Path.GetTempPath());
                        if (!string.IsNullOrEmpty(tempFileName))
                            image.SetAttributeValue("src", tempFileName);
                    }
                }
            }
            return doc.DocumentNode.OuterHtml;
        }
    }
}
