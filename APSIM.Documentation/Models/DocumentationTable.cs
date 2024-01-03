using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Models.Core;

namespace APSIM.Documentation.Models
{
    internal class DocumentationTable : IDocumentationTable
    {
        /// <summary>
        /// Names of the columns in the table.
        /// </summary>
        private IEnumerable<string> columns;

        /// <summary>
        /// Rows in the table.
        /// </summary>
        private IEnumerable<IDocumentationRow> rows;

        /// <summary>
        /// Name of the table.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Create a new <see cref="DocumentationTable"/> instance.
        /// </summary>
        /// <param name="name">Name of the table.</param>
        /// <param name="cols">Table column names.</param>
        /// <param name="rows">Table rows.</param>
        public DocumentationTable(string name, IEnumerable<string> cols, IEnumerable<IDocumentationRow> rows)
        {
            Name = name;
            columns = cols;
            this.rows = rows;
        }

        /// <summary>
        /// Build all documents referenced by the table.
        /// </summary>
        /// <param name="path">Output path for the generated documents.</param>
        public void BuildDocuments(string path)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            DocumentRows(rows, path, cts.Token).Wait();
        }

        private static async Task DocumentRows(IEnumerable<IDocumentationRow> rows, string path, CancellationToken cancelToken)
        {
            List<Task> tasks = new List<Task>();
            foreach (IDocumentationRow row in rows)
            {
                Task task = DocumentRow(row, path, cancelToken);
                tasks.Add(task);

                // Uncomment this to enable serial documentation of rows in the table.
                // This should massively reduce memory usage.

                // await task.ConfigureAwait(false);
                // GC.Collect();
            }
            foreach (Task task in tasks)
                await task.ConfigureAwait(false);
        }

        private static async Task DocumentRow(IDocumentationRow row, string path, CancellationToken cancelToken)
        {
            Stopwatch docStopWatch = Stopwatch.StartNew();

            List<Task> tasks = new List<Task>();
            foreach (IDocumentationCell cell in row.Cells)
                tasks.Add(DocumentCell(cell, path, cancelToken));
            foreach (Task task in tasks)
                await task.ConfigureAwait(false);

            Console.WriteLine($"Successfully generated doc for {row.Name}. Elapsed time: {docStopWatch.Elapsed.TotalSeconds} seconds.");
        }

        private static async Task DocumentCell(IDocumentationCell cell, string path, CancellationToken cancelToken)
        {
            List<Task> tasks = new List<Task>();
            foreach (IDocumentationFile file in cell.Files)
                tasks.Add(DocumentFile(file, path, cancelToken));
            foreach (Task task in tasks)
                await task.ConfigureAwait(false);
        }

        private static Task DocumentFile(IDocumentationFile file, string path, CancellationToken cancelToken)
        {
            return Task.Run(() => file.Generate(path), cancelToken);
        }

        /// <summary>
        /// Build a HTML document representing this table.
        /// </summary>
        public string BuildHtmlDocument()
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine($"<h1>{Name}</h1>");
            html.AppendLine("<table>");

            // Write column headers.
            html.AppendLine("<tr>");
            uint numColumns = 0;
            foreach (string column in columns)
            {
                html.AppendLine($"<th>{column}</th>");
                numColumns++;
            }
            html.AppendLine("</tr>");

            // Write rows.
            foreach (IDocumentationRow row in rows)
            {
                html.Append($"<tr><td>{row.Name}</td><td>");
                uint numCells = 1;
                foreach (IDocumentationCell cell in row.Cells)
                {
                    if (numCells != 1)
                        html.Append(", ");

                    IEnumerable<string> files = cell.Files.Select(f => $"<a href=\"{f.OutputFileName}\" target=\"blank\">{f.Name}</a>");
                    // fixme - insert actual links with remote path.
                    string links = string.Join(", ", files);
                    html.Append(links);

                    numCells++;
                }
                html.AppendLine("</td></tr>");
            }
            html.AppendLine("</table>");

            return html.ToString();
        }
    }
}
