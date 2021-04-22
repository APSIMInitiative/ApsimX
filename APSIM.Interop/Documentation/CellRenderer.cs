using System;
using System.Collections.Generic;
using System.Drawing;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown;
using APSIM.Interop.Markdown.Tags;
#if NETCOREAPP
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
#else
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
#endif

namespace APSIM.Interop.Documentation
{
    /// <summary>
    /// This class will render markdown tags to a table cell.
    /// This exists to workaround a few quirks in the MigraDoc API.
    /// </summary>
    internal class CellRenderer : MarkdownPdfRenderer
    {
        private Cell cell;
        public CellRenderer(Cell cell) : base(cell.Document) => this.cell = cell;

        public override void AddHeading(IEnumerable<TextTag> contents, int headingLevel)
        {
            base.AddHeading(cell.AddParagraph(), headingLevel, contents);
        }

        public override void AddImage(Image image)
        {
            cell.AddResizeImage(image);
        }

        public override void AddParagraph(IEnumerable<TextTag> contents)
        {
            Paragraph paragraph = cell.AddParagraph();
            foreach (TextTag tag in contents)
                AddToParagraph(paragraph, tag);
        }

        /// <summary>
        /// Nested table - TBI.
        /// </summary>
        /// <param name="rows"></param>
        public override void AddTable(IEnumerable<TableRow> rows)
        {
            throw new NotImplementedException();
        }

        public override void AddLink(IEnumerable<TextTag> contents, string uri)
        {
            // todo: test this
            // AddLinkToParagraph(cell.Section.LastParagraph, contents, uri);
            throw new NotImplementedException("todo: test this");
        }
    }
}
