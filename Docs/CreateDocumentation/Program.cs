namespace CreateDocumentation
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using UserInterface.Commands;
    using UserInterface.Presenters;
    using UserInterface.Views;

    class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                Gtk.Application.Init();
                Gtk.Settings.Default.SetLongProperty("gtk-menu-images", 1, "");

                var apsimDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ".."));

                // Set the current directory to the bin directory so that APSIM can find sqlite3.dll
                var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Directory.SetCurrentDirectory(binDirectory);

                // Determine where we are going to put the documentation.
                var destinationFolder = Path.Combine(Path.GetTempPath(), "ApsimX", "AutoDoc");
                Directory.CreateDirectory(destinationFolder);

                // Create a data table that will later be turned into a table on a html page.
                var documentationTable = new DataTable();
                documentationTable.Columns.Add("Model name", typeof(string));
                documentationTable.Columns.Add("Documentation", typeof(string));

                // Read the documentation instructions file.
                var documentationFileName = Path.Combine(apsimDirectory, "Docs", "CreateDocumentation", "Documentation.json");
                var instructions = JObject.Parse(File.ReadAllText(documentationFileName));

                // Loop through all models and document.
                foreach (var model in instructions["Models"] as JArray)
                {
                    var documentationRow = documentationTable.NewRow();
                    documentationRow[0] = model["Name"].ToString();
                    string html = string.Empty;
                    foreach (var documentDescription in model["Documents"] as JArray)
                        html += CreateModelDocumentation(documentDescription as JObject, apsimDirectory, destinationFolder);
                    documentationRow[1] = html;
                    documentationTable.Rows.Add(documentationRow);
                }

                // Create a documentation html page.
                var builder = new StringBuilder();
                builder.AppendLine("<html>");
                builder.AppendLine("<body>");

                builder.AppendLine(DataTableUtilities.ToHTML(documentationTable));

                builder.AppendLine("</body>");
                builder.AppendLine("</html>");

                var htmlFileName = Path.Combine(destinationFolder, "index.html");
                File.WriteAllText(htmlFileName, builder.ToString());
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Create documentation based on the specified file.
        /// </summary>
        /// <param name="documentObject">The documentObject node that describes what to document.</param>
        /// <param name="apsimDirectory">The APSIM root directory.</param>
        /// <param name="destinationFolder">The folder where the PDF should be created.</param>
        /// <returns>HTML snippet for a single model document.</returns>
        private static string CreateModelDocumentation(JObject documentObject, string apsimDirectory, string destinationFolder)
        {
            string href;
            string hrefName = documentObject["Name"].ToString();
            if (documentObject["URL"] != null)
                href = documentObject["URL"].ToString();
            else
            {
                var fileName = Path.Combine(apsimDirectory, documentObject["FileName"].ToString());
                if (File.Exists(fileName))
                {
                    // Get the name of the model
                    string modelName = Path.GetFileNameWithoutExtension(fileName);

                    // Open the file.
                    var simulations = FileFormat.ReadFromFile<Simulations>(fileName, out List<Exception> creationExceptions);
                    if (creationExceptions?.Count > 0)
                        throw creationExceptions[0];

                    // Create some necessary presenters.
                    var mainPresenter = new MainPresenter();
                    var explorerPresenter = new ExplorerPresenter(mainPresenter);
                    var explorerView = new ExplorerView(null);
                    explorerPresenter.Attach(simulations, explorerView, explorerPresenter);
                    explorerView.MainWidget.ShowAll();

                    // Document model.
                    if (documentObject["ModelNameToDocument"] == null)
                    {
                        var createDoc = new CreateDocCommand(explorerPresenter, destinationFolder);
                        createDoc.Do(null);
                        href = Path.GetFileName(createDoc.FileNameWritten);
                    }
                    else
                    {
                        var modelNameToDocument = documentObject["ModelNameToDocument"].ToString();
                        var model = Apsim.Find(simulations, modelNameToDocument) as IModel;
                        if (model == null)
                            return null;
                        //explorerPresenter.SelectNode(Apsim.FullPath(model));
                        var createDoc = new CreateModelDescriptionDocCommand(explorerPresenter, model);
                        createDoc.Do(null);
                        href = Path.GetFileName(createDoc.FileNameWritten);
                    }
                }
                else
                    return null;
            }
            return string.Format("<p><a href=\"{0}\">{1}</a></p>", href, hrefName);
        }
    }
}