using UserInterface.Views;
using Models.Core;
using UserInterface.Presenters;
using System.IO;
using System.Reflection;
using System;
using Models.Factorial;
using APSIM.Shared.Utilities;
using Models;
using System.Collections.Generic;
using System.Text;
using Models.Graph;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    public class ExportNodeCommand : ICommand
    {
        private ExplorerPresenter ExplorerPresenter;
        private string NodePath;
        private string FolderPath;

        // Setup a list of model types that we will recurse down through.
        private static Type[] modelTypesToRecurseDown = new Type[] {typeof(Folder),
                                                                    typeof(Simulations),
                                                                    typeof(Simulation),
                                                                    typeof(Experiment)};

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExportNodeCommand(ExplorerPresenter explorerPresenter,
                                 string nodePath,
                                 string folderPath)
        {
            this.ExplorerPresenter = explorerPresenter;
            this.NodePath = nodePath;
            this.FolderPath = folderPath;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            
            // Get the model we are to export.
            Model modelToExport = Apsim.Get(ExplorerPresenter.ApsimXFile, NodePath) as Model;
            if (modelToExport != null)
                DoExport(modelToExport, FolderPath);
        }


        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }

        /// <summary>
        /// Main export code.
        /// </summary>
        public void DoExport(Model modelToExport, string folderPath)
        {
            // Make sure the specified folderPath exists because we're going to 
            // write to it.
            Directory.CreateDirectory(folderPath);

            string savedWorkingDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(folderPath);

            //Load CSS resource
            Assembly assembly = Assembly.GetExecutingAssembly();
            StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("UserInterface.Resources.Export.css"));
            string css = reader.ReadToEnd();

            // Write the css file.
            using (FileStream file = new FileStream(Path.Combine(folderPath, "export.css"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.Export.css").CopyTo(file);
            }

            //write image files
            using (FileStream file = new FileStream(Path.Combine(folderPath, "apsim_logo.png"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.apsim_logo.png").CopyTo(file);
            }

            using (FileStream file = new FileStream(Path.Combine(folderPath, "hd_bg.png"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.hd_bg.png").CopyTo(file);
            }

            // Create HTML file.
            StringWriter index = new StringWriter();

            // Look for a crop and export it.
            string titlePageHTML = string.Empty;
            List<IModel> crop = Apsim.ChildrenRecursively(ExplorerPresenter.ApsimXFile, typeof(ICrop));
            if (crop.Count > 0)
            {
                index.WriteLine("<H1>Model Description</H1>");
                Classes.PMFDocumentation doc = new Classes.PMFDocumentation();
                doc.Go(index, crop[0] as Model);
                titlePageHTML = doc.TitlePageHTML(crop[0] as Model);
            }

            // Export the validation.
            index.WriteLine("<H1>Model testing</H1>");
            DoExportInternal(index, modelToExport, "", string.Empty, 2);

            // Update html for table of contents.
            string newHTML = CreateTOC(index.ToString());
  
            // Write file.
            StreamWriter index2 = new StreamWriter(Path.Combine(folderPath, "Index.html"));
            index2.WriteLine("<!DOCTYPE html><html lang=\"en-AU\">");
            index2.WriteLine("<head>");
            index2.WriteLine("   <link rel=\"stylesheet\" type=\"text/css\" href=\"export.css\">");
            index2.WriteLine("</head>");
            index2.WriteLine("<body>");
            index2.WriteLine("<div id=\"content\"><div id=\"left\"><img src=\"apsim_logo.png\" /></div>");
            index2.WriteLine("<div id=\"right\"><img src=\"hd_bg.png\" /></div>");

            index2.WriteLine(titlePageHTML);
            index2.WriteLine(newHTML);

            index2.WriteLine("</body>");
            index2.WriteLine("</html>");
            index2.Close();

            Directory.SetCurrentDirectory(savedWorkingDirectory);
        }

        /// <summary>
        /// Internal export method.
        /// </summary>
        /// <param name="modelToExport">The model to export.</param>
        /// <param name="folderPath">The folder path.</param>
        /// <param name="url">The URL.</param>
        /// <returns>True if something was written to index.</returns>
        private bool DoExportInternal(TextWriter index, Model modelToExport, string folderPath, string url, int level)
        {
            // Make sure the specified folderPath exists because we're going to 
            // write to it.
            if (folderPath != string.Empty)
                Directory.CreateDirectory(folderPath);

            if (modelToExport.Name != "Simulations")
                index.WriteLine("<H" + level.ToString() + ">" + modelToExport.Name + "</H" + level.ToString() + ">");

            // Look for child models that are a folder or simulation etc
            // that we need to recurse down through.
            bool somethingWritten = false;
            foreach (Model child in modelToExport.Children)
            {
                bool ignoreChild = (child is Simulation && child.Parent is Experiment);

                if (!ignoreChild)
                {
                    if (Array.IndexOf(modelTypesToRecurseDown, child.GetType()) != -1)
                    {
                        string childFolderPath = Path.Combine(folderPath, child.Name);
                        DoExportInternal(index, child, childFolderPath, url + "../", level+1);
                    }
                    else
                    {
                        string html = ModelToHTML(child, folderPath);
                        if (html != null)
                        {
                            somethingWritten = true;
                            index.WriteLine(html);
                        }
                    }
                }
            }

            return somethingWritten;
        }

        /// <summary>
        /// Export the specified model to HTML. Can return null if nothing to export.
        /// </summary>
        /// <param name="modelToExport">The model to export</param>
        /// <param name="folderPath">The folder path where images can be stored.</param>
        private string ModelToHTML(Model modelToExport, string folderPath)
        {
            // If this is a graph then only include it if it has been flagged as 'to be included'
            if (modelToExport is Graph)
            {
                Graph graph = modelToExport as Graph;
                if (!graph.IncludeInDocumentation)
                    return null;
            }

            // Select the node in the tree.
            ExplorerPresenter.SelectNode(Apsim.FullPath(modelToExport));

            // If the presenter is exportable then simply export this child.
            // Otherwise, if it is one of a folder, simulation, experiment or zone then
            // recurse down.
            if (ExplorerPresenter.CurrentPresenter is IExportable)
            {
                string html = (ExplorerPresenter.CurrentPresenter as IExportable).ConvertToHtml(folderPath);
                return "<p>" + html + "</p>";
            }

            return null;
        }

        /// <summary>Creates the table of contents, returning HTML.</summary>
        /// <param name="html">The HTML to parse for heading tags and change for TOC</param>
        /// <returns>The modified HTML.</returns>
        private string CreateTOC(string html)
        {
            int[] levels = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int posLastHeading = 0;
            int posHeading = html.IndexOf("<H", StringComparison.CurrentCultureIgnoreCase);

            StringBuilder toc = new StringBuilder();
            StringBuilder htmlModified = new StringBuilder();

            toc.Append("<dl>");

            while (posHeading != -1)
            {
                // write everything up to the old heading;
                htmlModified.Append(html.Substring(posLastHeading, posHeading - posLastHeading));

                int posEndHeading = html.IndexOf("</H", posHeading, StringComparison.CurrentCultureIgnoreCase);

                // extract the heading and heading number (1 to 10)
                string heading = html.Substring(posHeading + 4, posEndHeading - posHeading - 4);
                string htmlFollowingHeading = string.Empty;
                if (posEndHeading + 7 < html.Length)
                    htmlFollowingHeading = html.Substring(posEndHeading + 7, 2);
                if (htmlFollowingHeading != "<H")
                {
                    int headingNumber = Convert.ToInt32(Char.GetNumericValue(html[posHeading + 2]));

                    if (headingNumber > 0 && headingNumber < 4)
                    {
                        // Update levels based on heading number.
                        levels[headingNumber - 1]++;
                        for (int i = headingNumber; i < levels.Length; i++)
                            if (levels[i] > 0)
                                levels[i] = 0;

                        // Insert the levels into the heading e.g. "1.3.2"
                        string levelString = string.Empty;
                        for (int i = 0; i < levels.Length; i++)
                            if (levels[i] > 0)
                            {
                                if (levelString != string.Empty)
                                    levelString += ".";
                                levelString += levels[i].ToString();
                            }
                        heading = levelString + " " + heading;

                        // Write the heading back to the html.

                        htmlModified.Append("<a name=\"" + heading + "\"></a>");
                        htmlModified.Append("<H" + headingNumber + ">" + heading + "</H" + headingNumber + ">");

                        // Add heading to our list of headings.
                        toc.Append("<dt>");
                        for (int i = 0; i < headingNumber; i++)
                            toc.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
                        toc.Append("<a href=\"#" + heading + "\">" + heading + "</a><br></dt>\r\n");
                    }
                }

                // Find next heading.
                posLastHeading = posEndHeading + 5;
                posHeading = html.IndexOf("<H", posLastHeading, StringComparison.CurrentCultureIgnoreCase);
            }

            // write remainder of file.
            htmlModified.Append(html.Substring(posLastHeading));

            toc.Append("</dl>");

            return toc.ToString() + htmlModified.ToString();
        }
    }
}
