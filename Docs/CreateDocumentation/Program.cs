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
    using System.Net;
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
            bool errorsFound = false;

            try
            {
                Gtk.Application.Init();

                var apsimDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ".."));

                // Set the current directory to the bin directory so that APSIM can find sqlite3.dll
                var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Directory.SetCurrentDirectory(binDirectory);

                // Determine where we are going to put the documentation.
                var destinationFolder = Path.Combine(Path.GetTempPath(), "ApsimX", "AutoDoc");
                Directory.CreateDirectory(destinationFolder);

                // Create a string builder for writing the html index file.
                var htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<body>");

                // Was a title for the generated html file given as argument 1?
                var version = GetVersion();
                var serverFtpFolder = "ftp://apsimdev.apsim.info/ApsimX/Releases/" + version;
                var serverHttpFolder = "https://apsimdev.apsim.info/ApsimX/Releases/" + version;

                htmlBuilder.AppendLine("<h1>Documentation for version " + version + "</h1>");

                // Read the documentation instructions file.
                var documentationFileName = Path.Combine(apsimDirectory, "Docs", "CreateDocumentation", "Documentation.json");
                var instructions = JObject.Parse(File.ReadAllText(documentationFileName));

                // Keep track of all exceptions so that we can show them at the end.
                var exceptions = new List<Exception>();

                // Loop through all "Tables" element in the input json file.
                foreach (JObject tableInstruction in instructions["Tables"] as JArray)
                {
                    // Write html heading for table.
                    htmlBuilder.AppendLine("<h2>" + tableInstruction["Title"].ToString() + "</h2>");

                    // Create a data table.
                    var documentationTable = CreateTable(tableInstruction, apsimDirectory, destinationFolder, serverHttpFolder);
                    if (documentationTable == null)
                        errorsFound = true;
                    else
                    {
                        // Write table to html.
                        htmlBuilder.AppendLine(DataTableUtilities.ToHTML(documentationTable, writeHeaders: true));
                    }
                }

                // Close the open html tags.
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                // Write the html to file.
                var htmlFileName = Path.Combine(destinationFolder, "index.html");
                File.WriteAllText(htmlFileName, htmlBuilder.ToString());

                // Upload to server
                Upload(destinationFolder, serverFtpFolder);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                errorsFound = true;
            }
            if (errorsFound)
            {
                Console.WriteLine("Errors were found creating documentation");
                return 1;
            }
            else
                return 0;
        }

        /// <summary>Get the APSIM version.</summary>
        private static string GetVersion()
        {
            var pullRequestID = Environment.GetEnvironmentVariable("ghprbPullId");
            if (string.IsNullOrEmpty(pullRequestID))
                pullRequestID = Environment.GetEnvironmentVariable("PULL_ID");

            var url = string.Format("https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetPullRequestDetails?pullRequestID={0}",
                                    pullRequestID);
            var versionString = WebUtilities.CallRESTService<string>(url);

            var temp = StringUtilities.SplitOffAfterDelimiter(ref versionString, "-");
            var issueNumber = StringUtilities.SplitOffAfterDelimiter(ref temp, ",");

            Console.WriteLine(versionString + "." + issueNumber);

            return versionString + "." + issueNumber;
        }

        /// <summary>
        /// Create a documentation table.
        /// </summary>
        /// <param name="instructions">The json instructions for creating the table.</param>
        /// <param name="apsimDirectory">The root apsim directory.</param>
        /// <param name="destinationFolder">The destination directory where the auto-generated files are to be created.</param>
        /// <param name="destinationUrl">The server destination URL where all files will end up.</param>
        /// <returns></returns>
        private static DataTable CreateTable(JObject instructions, string apsimDirectory, string destinationFolder, string destinationUrl)
        {
            // Create a data table that will later be turned into a table on a html page.
            var documentationTable = new DataTable();
            var columns = instructions["Columns"] as JArray;
            foreach (var column in columns)
                documentationTable.Columns.Add(column.ToString());

            // Loop through all models and document.
            bool errorsFound = false;
            foreach (var model in instructions["Rows"] as JArray)
            {
                try
                {
                    var documentationRow = documentationTable.NewRow();
                    documentationRow[0] = model["Name"].ToString();
                    int columnIndex = 1;
                    foreach (var documentDescription in model["Documents"] as JArray)
                    {
                        documentationRow[columnIndex] = CreateModelDocumentation(documentDescription as JObject, apsimDirectory, destinationFolder, destinationUrl);
                        columnIndex++;
                    }
                    documentationTable.Rows.Add(documentationRow);
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.ToString());
                    errorsFound = true;
                }
            }

            if (errorsFound)
                return null;
            else
                return documentationTable;
        }

        /// <summary>
        /// Create documentation based on the specified file.
        /// </summary>
        /// <param name="documentObject">The documentObject node that describes what to document.</param>
        /// <param name="apsimDirectory">The APSIM root directory.</param>
        /// <param name="destinationFolder">The folder where the PDF should be created.</param>
        /// <param name="destinationUrl">The server destination URL where all files will end up.</param>
        /// <returns>HTML snippet for a single model document.</returns>
        private static string CreateModelDocumentation(JObject documentObject, string apsimDirectory, string destinationFolder, string destinationUrl)
        {
            string href;
            string hrefName = documentObject["Name"].ToString();
            if (documentObject["URL"] != null)
            {
                href = documentObject["URL"].ToString();
                return string.Format("<p><a href=\"{0}\" target=\"_blank\">{1}</a></p>", href, hrefName);
            }
            else
            {
                var fileName = Path.Combine(apsimDirectory, documentObject["FileName"].ToString());
                if (File.Exists(fileName))
                {
                    // Open the file.
                    var simulations = FileFormat.ReadFromFile<Simulations>(fileName, out List<Exception> creationExceptions);
                    if (creationExceptions?.Count > 0)
                        throw creationExceptions[0];

                    // Create some necessary presenters and views.
                    var mainPresenter = new MainPresenter();
                    var explorerPresenter = new ExplorerPresenter(mainPresenter);

                    var explorerView = new ExplorerView(null);
                    explorerPresenter.Attach(simulations, explorerView, explorerPresenter);

                    // Document model.
                    if (documentObject["ModelNameToDocument"] == null)
                    {
                        Console.WriteLine("Creating documentation from " + fileName);

                        // Whole of simulation document.
                        var createDoc = new CreateFileDocumentationCommand(explorerPresenter, destinationFolder);
                        createDoc.Do(null);
                        href = Path.GetFileName(createDoc.FileNameWritten);
                    }
                    else
                    {
                        Console.WriteLine("Creating model description documentation from " + fileName);

                        // Specific model description documentation.
                        var modelNameToDocument = documentObject["ModelNameToDocument"].ToString();
                        var model = Apsim.Find(simulations, modelNameToDocument) as IModel;
                        if (model == null)
                            return null;
                        var outputFileName = documentObject["OutputFileName"]?.ToString();
                        var createDoc = new CreateParamsInputsOutputsDocCommand(explorerPresenter, model, destinationFolder, outputFileName);
                        createDoc.Do(null);
                        href = Path.GetFileName(createDoc.FileNameWritten);
                    }

                    return string.Format("<p><a href=\"{0}/{1}\" target=\"_blank\">{2}</a></p>", destinationUrl, href, hrefName);
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Upload files to server.
        /// </summary>
        /// <param name="destinationFolder">The folder to upload.</param>
        /// <param name="serverFolder">Server folder to send files to.</param>
        private static void Upload(string destinationFolder, string serverFolder)
        {
            var userName = Environment.GetEnvironmentVariable("APSIM_SITE_CREDS_USR");
            var password = Environment.GetEnvironmentVariable("APSIM_SITE_CREDS_PSW");

            Console.WriteLine("Uploading documentation to " + serverFolder);

            try
            {
                var request = WebRequest.Create(serverFolder) as FtpWebRequest;
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.Credentials = new NetworkCredential(userName, password);
                var ftpResponse = (FtpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                // Can fail if the directory already exists on server.
            }

            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(userName, password);

                foreach (var fileName in Directory.GetFiles(destinationFolder))
                {
                    var destFileName = serverFolder + "/" + Path.GetFileName(fileName);
                    client.UploadFile(destFileName, WebRequestMethods.Ftp.UploadFile, fileName);

                }
            }
        }
    }
}