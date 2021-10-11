using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// Create a new <see cref="DocumentationTable"/> instance.
        /// </summary>
        /// <param name="cols">Table column names.</param>
        /// <param name="rows">Table rows.</param>
        public DocumentationTable(IEnumerable<string> cols, IEnumerable<IDocumentationRow> rows)
        {
            this.columns = cols;
            this.rows = rows;
        }

        /// <summary>
        /// Build all documents referenced by the table.
        /// </summary>
        /// <param name="path">Output path for the generated documents.</param>
        public void BuildDocuments(string path)
        {
            // todo - parallelise this
            CancellationTokenSource cts = new CancellationTokenSource();
            DocumentRows(rows, path, cts.Token).Wait();
            // foreach (IDocumentationRow row in rows)
            //     foreach (IDocumentationCell cell in row.Cells)
            //         foreach (IDocumentationFile file in cell.Files)
            //             file.Generate(path);
        }

        private static async Task DocumentRows(IEnumerable<IDocumentationRow> rows, string path, CancellationToken cancelToken)
        {
            List<Task> tasks = new List<Task>();
            foreach (IDocumentationRow row in rows)
            {
                Task task = DocumentRow(row, path, cancelToken);
                tasks.Add(task);
                // Comment this out to enable parallel documentation of rows in the table.
                await task.ConfigureAwait(false);
                GC.Collect();
            }
            foreach (Task task in tasks)
                await task.ConfigureAwait(false);
        }

        private static async Task DocumentRow(IDocumentationRow row, string path, CancellationToken cancelToken)
        {
            List<Task> tasks = new List<Task>();
            foreach (IDocumentationCell cell in row.Cells)
                tasks.Add(DocumentCell(cell, path, cancelToken));
            foreach (Task task in tasks)
                await task.ConfigureAwait(false);
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
            return Task.Run(() => 
            {
                try
                {
                    file.Generate(path);
                }
                catch (Exception err)
                {
                    Console.Error.WriteLine($"Failed to generate documentation for file {file.OutputFileName}");
                    Console.Error.WriteLine(err);
                }
            }, cancelToken);
        }

        /// <summary>
        /// Build a HTML document representing this table.
        /// </summary>
        public string BuildHtmlDocument()
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<table>");

            // Write column headers.
            html.AppendLine("<tr>");
            foreach (string column in columns)
                html.AppendLine($"<th>{column}</th>");
            html.AppendLine("</tr>");

            // Write rows.
            foreach (IDocumentationRow row in rows)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{row.Name}</td>");
                foreach (IDocumentationCell cell in row.Cells)
                {
                    html.AppendLine("<td>");
                    IEnumerable<string> files = cell.Files.Select(f => $"<a href=\"{f.OutputFileName}\">{f.Name}</a>");
                    // fixme - insert actual links with remote path.
                    string links = string.Join(" ", files);
                    html.AppendLine(links);
                    html.AppendLine("</td>");   
                }
                html.AppendLine("</tr>");
            }
            html.AppendLine("</table>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }
    }
}
