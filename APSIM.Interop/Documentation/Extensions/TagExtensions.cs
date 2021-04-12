using System;
using APSIM.Services.Documentation;
using Paragraph = APSIM.Services.Documentation.Paragraph;
using APSIM.Interop.Documentation.Helpers;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
#else
using MigraDoc.DocumentObjectModel;
#endif

namespace APSIM.Interop.Documentation.Extensions
{
    internal static class TagExtensions
    {
        /// <summary>
        /// Add an ITag instance to a section of a PDF document.
        /// </summary>
        /// <param name="tag">Tag to be added.</param>
        /// <param name="section">Section to which the tag will be added.</param>
        public static void AddTo(this ITag tag, Section section)
        {
            throw new NotImplementedException($"Unknown tag type {tag.GetType()}");
        }

        /// <summary>
        /// Add a heading to a section of a PDF document.
        /// </summary>
        /// <param name="heading">Heading to be added.</param>
        /// <param name="section">Section to which the heading will be added.</param>
        public static void AddTo(this Heading heading, Section section)
        {
            var paragraph = section.AddParagraph(heading.Text, $"heading{heading.HeadingLevel}");
            paragraph.Format.KeepWithNext = true;
        }

        /// <summary>
        /// Add a paragraph to a section of a PDF document.
        /// </summary>
        /// <param name="paragraph">Paragraph to be added.</param>
        /// <param name="section">Section to which the paragraph will be added.</param>
        public static void AddTo(this Paragraph paragraph, Section section)
        {
            var renderer = new MarkdownPdfRenderer("");
            renderer.Render(paragraph.Text, section);
        }
    }
}