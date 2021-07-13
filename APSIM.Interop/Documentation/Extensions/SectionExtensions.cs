using System;
using APSIM.Interop.Documentation.Helpers;
using APSIM.Interop.Markdown;
using APSIM.Services.Documentation;
using System.Collections.Generic;
using System.Linq;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;
using Section = MigraDocCore.DocumentObjectModel.Section;
using Paragraph = MigraDocCore.DocumentObjectModel.Paragraph;
using Image = MigraDocCore.DocumentObjectModel.Shapes.Image;
#else
using MigraDoc.DocumentObjectModel;
using Table = MigraDoc.DocumentObjectModel.Tables.Table;
using Section = MigraDoc.DocumentObjectModel.Section;
using Paragraph = MigraDoc.DocumentObjectModel.Paragraph;
using Image = MigraDoc.DocumentObjectModel.Shapes.Image;
#endif

namespace APSIM.Interop.Documentation.Extensions
{
    internal static class SectionExtensions
    {
        // /// <summary>
        // /// Add a paragraph to the section and apply the specified style.
        // /// </summary>
        // /// <param name="section">Section to which the tag will be added.</param>
        // /// <param name="text">Text to be added to the paragraph.</param>
        // /// <param name="style">Style to be applied to the paragraph.</param>
        // public static void AddParagraph(this Section section, string text, Style style)
        // {
        //     style = style.Clone();
        //     // style.Name = $"CustomStyle{Guid.NewGuid().ToString().Replace("-", "")}";
        //     section.Document.Styles.Add(style);
        //     Paragraph paragraph = section.AddParagraph(style.Name);
        //     // paragraph.AddFormattedText(text, style.GetTextFormat());
        // }

        // /// <summary>
        // /// Add an ITag instance to a section of a PDF document.
        // /// </summary>
        // /// <param name="section">Section to which the tag will be added.</param>
        // /// <param name="tag">Tag to be added.</param>
        // /// <param name="options">PDF Generation options.</param>
        // public static void Add(this Section section, ITag tag, PdfOptions options)
        // {
        //     if (tag is Heading heading)
        //         section.Add(heading, options);
        //     else if (tag is APSIM.Services.Documentation.Paragraph paragraph)
        //         section.Add(paragraph, options);
        // }

        // /// <summary>
        // /// Add a heading to a section of a PDF document.
        // /// </summary>
        // /// <param name="section">Section to which the heading will be added.</param>
        // /// <param name="heading">Heading to be added.</param>
        // /// <param name="options">PDF Generation options.</param>
        // public static void Add(this Section section, Heading heading, PdfOptions options)
        // {
        //     var paragraph = section.AddParagraph(heading.Text, $"heading{heading.HeadingLevel}");
        //     paragraph.Format.KeepWithNext = true;
        // }

        // /// <summary>
        // /// Add a paragraph to a section of a PDF document.
        // /// </summary>
        // /// <param name="section">Section to which the paragraph will be added.</param>
        // /// <param name="paragraph">Paragraph to be added.</param>
        // /// <param name="options">PDF Generation options.</param>
        // public static void Add(this Section section, APSIM.Services.Documentation.Paragraph paragraph, PdfOptions options)
        // {
            
        // }

        /// <summary>
        /// Get the last table in the section. Throws if section contains no tables.
        /// </summary>
        /// <param name="section">A section.</param>
        internal static Table GetLastTable(this Section section)
        {
            var table = section.LastTable;
            if (table == null)
                throw new InvalidOperationException("Section contains no tables.");
            return table;
        }

        internal static bool IsEmpty(this Paragraph paragraph)
        {
            if (paragraph.Elements.OfType<Image>().Any())
                return false;
            IEnumerable<Text> textElements = paragraph.GetTextElements();
            return textElements.All(t => string.IsNullOrEmpty(t.Content));
        }

        internal static IEnumerable<Text> GetTextElements(this Paragraph paragraph)
        {
            IEnumerable<Text> raw = paragraph.Elements.OfType<Text>();
            IEnumerable<Text> formatted = paragraph.Elements.OfType<FormattedText>().SelectMany(f => f.Elements.OfType<Text>());
            return formatted.Union(raw);
        }

        internal static string GetRawText(this Paragraph paragraph)
        {
            return string.Join("", paragraph.GetTextElements().Select(t => t.Content));
        }
    }
}
