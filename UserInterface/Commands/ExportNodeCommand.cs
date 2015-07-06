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
using System.Drawing;

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
        string[] indents = new string[] { string.Empty, " class=\"tab1\"", " class=\"tab2\"", " class=\"tab3\"",
                                                        " class=\"tab4\"", " class=\"tab5\"", " class=\"tab6\"",
                                                        " class=\"tab7\"", " class=\"tab8\"", " class=\"tab9\""};

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
            string modelName = Path.GetFileNameWithoutExtension(ExplorerPresenter.ApsimXFile.FileName.Replace("Validation", ""));
            DoExport(modelName, FolderPath);
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
        public void DoExport(string modelNameToExport, string workingDirectory)
        {
            // Make sure the specified folderPath exists because we're going to 
            // write to it.
            Directory.CreateDirectory(workingDirectory);

            //Load CSS resource
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Write the css file.
            using (FileStream file = new FileStream(Path.Combine(workingDirectory, "AutoDocumentation.css"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.AutoDocumentation.css").CopyTo(file);
            }

            //write image files
            using (FileStream file = new FileStream(Path.Combine(workingDirectory, "apsim_logo.png"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.apsim_logo.png").CopyTo(file);
            }

            using (FileStream file = new FileStream(Path.Combine(workingDirectory, "hd_bg.png"), FileMode.Create, FileAccess.Write))
            {
                assembly.GetManifestResourceStream("UserInterface.Resources.hd_bg.png").CopyTo(file);
            }

            // Write file.
            StreamWriter index = new StreamWriter(Path.Combine(workingDirectory, "Index.html"));
            index.WriteLine("<!DOCTYPE html><html lang=\"en-AU\">");
            index.WriteLine("<head>");
            index.WriteLine("   <link rel=\"stylesheet\" type=\"text/css\" href=\"AutoDocumentation.css\">");
            index.WriteLine("   <title>" + modelNameToExport + " documentation</title>");
            index.WriteLine("</head>");
            index.WriteLine("<body>");
            index.WriteLine("<img src=\"apsim_logo.png\"/><div id=\"right\"><img src=\"hd_bg.png\" /></div>");

            List<AutoDocumentation.ITag> tags = new List<AutoDocumentation.ITag>();

            // See if there is a title page.
            IModel titlePage = Apsim.Find(ExplorerPresenter.ApsimXFile, "TitlePage");
            if (titlePage != null)
            {
                titlePage.Document(tags, 1, 0);
                WriteHTML(index, tags, workingDirectory);
                tags.Clear();
            }

            // Document model description.
            tags.Add(new AutoDocumentation.Heading("Model description", 1));
            ExplorerPresenter.ApsimXFile.DocumentModel(modelNameToExport, tags, 2);

            // Document model validation.
            tags.Add(new AutoDocumentation.Heading("Validation", 1));
            AddValidationTags(tags, ExplorerPresenter.ApsimXFile, 1, workingDirectory);

            CreateTableOfContents(index, tags);

            WriteHTML(index, tags, workingDirectory);

            index.WriteLine("</body>");
            index.WriteLine("</html>");
            index.Close();

            ExplorerPresenter.ShowMessage("Finished creating documentation", DataStore.ErrorLevel.Information);
        }

        /// <summary>Creates a table of contents.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="tags">The autodoc tags.</param>
        private void CreateTableOfContents(TextWriter writer, List<AutoDocumentation.ITag> tags)
        {
            writer.WriteLine("<dl>");

            int[] levels = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tag as AutoDocumentation.Heading;
                    if (heading.headingLevel > 0)
                    {
                        // Update levels based on heading number.
                        levels[heading.headingLevel - 1]++;
                        for (int i = heading.headingLevel; i < levels.Length; i++)
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
                        heading.text = levelString + " " + heading.text;
                        if (heading.headingLevel < 3 || levelString.StartsWith("2"))
                            writer.WriteLine("<dt{0}><a href=\"#{1}\">{1}</a><br/></dt>", indents[heading.headingLevel], heading.text);
                    }
                }
            }
            writer.WriteLine("</dl>");
        }

        /// <summary>Writes HTML for specified auto-doc commands.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="tags">The autodoc tags.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void WriteHTML(TextWriter writer, List<AutoDocumentation.ITag> tags, string workingDirectory)
        {

            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tag as AutoDocumentation.Heading;
                    if (heading.headingLevel > 0 && heading.headingLevel < 4)
                    {
                        writer.WriteLine("<a name=\"{0}\"></a>", heading.text);
                        writer.WriteLine("<h{0}>{1}</h{0}>", heading.headingLevel, heading.text);
                    }
                }
                else if (tag is AutoDocumentation.Paragraph)
                {
                    AutoDocumentation.Paragraph paragraph = tag as AutoDocumentation.Paragraph;
                    writer.WriteLine("<p" + indents[paragraph.indent] + ">" + paragraph.text + "</p>");
                }
                else if (tag is AutoDocumentation.GraphAndTable)
                {
                    CreateGraph(writer, tag as AutoDocumentation.GraphAndTable, workingDirectory);
                }
                else if (tag is Graph)
                {
                    GraphPresenter graphPresenter = new GraphPresenter();
                    GraphView graphView = new GraphView();
                    graphView.BackColor = Color.White;
                    //graphView.Show();
                    graphPresenter.Attach(tag, graphView, ExplorerPresenter);
                    writer.WriteLine(graphPresenter.ConvertToHtml(workingDirectory));
                    graphPresenter.Detach();
                }
            }
        }

        /// <summary>Creates the graph.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="graphAndTable">The graph and table to convert to html.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void CreateGraph(TextWriter writer, AutoDocumentation.GraphAndTable graphAndTable, string workingDirectory)
        {
            // Ensure graphs directory exists.
            string graphDirectory = Path.Combine(workingDirectory, "Graphs");
            Directory.CreateDirectory(graphDirectory);

            // Determine the name of the .png file to write.
            string PNGFileName = Path.Combine(graphDirectory,
                                              graphAndTable.xyPairs.Parent.Parent.Name + graphAndTable.xyPairs.Parent.Name + ".png");

            // Start the outside table.
            writer.WriteLine("<table" + indents[graphAndTable.indent] + ">");

            // output chart as a column to the outer table.
            writer.WriteLine("<tr><td><img src=\"" + PNGFileName + "\"></td>");

            // output xy table as a table.
            writer.WriteLine("<td><table width=\"300\">");
            writer.WriteLine("<td>" + graphAndTable.xName + "</td><td>" + graphAndTable.yName + "</td>");
            for (int i = 0; i < graphAndTable.xyPairs.X.Length; i++)
                writer.WriteLine("<tr><td>" + graphAndTable.xyPairs.X[i] + "</td><td>" + graphAndTable.xyPairs.Y[i] + "</td></tr>");
            writer.WriteLine("</table></td>");
            writer.WriteLine("</tr></table>");

            // Setup graph.
            GraphView graph = new GraphView();
            graph.Clear();

            // Create a line series.
            graph.DrawLineAndMarkers("", graphAndTable.xyPairs.X, graphAndTable.xyPairs.Y, 
                                     Models.Graph.Axis.AxisType.Bottom, Models.Graph.Axis.AxisType.Left,
                                     Color.Blue, Models.Graph.Series.LineType.Solid, Models.Graph.Series.MarkerType.None, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, graphAndTable.xName, false, double.NaN, double.NaN, double.NaN);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, graphAndTable.yName, false, double.NaN, double.NaN, double.NaN);
            graph.BackColor = Color.White;
            graph.Refresh();
            graph.FormatTitle(graphAndTable.title);

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(480, 480);
            graph.Export(image);
            image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);
        }

        /// <summary>
        /// Internal export method.
        /// </summary>
        /// <param name="tags">The autodoc tags.</param>
        /// <param name="modelToExport">The model to export.</param>
        /// <param name="workingDirectory">The folder path where bitmaps can be written.</param>
        /// <param name="url">The URL.</param>
        /// <returns>True if something was written to index.</returns>
        private void AddValidationTags(List<AutoDocumentation.ITag> tags, IModel modelToExport, int headingLevel, string workingDirectory)
        {
            if (modelToExport.Name != "Simulations")
                tags.Add(new AutoDocumentation.Heading(modelToExport.Name, headingLevel));

            // Look for child models that are a folder or simulation etc
            // that we need to recurse down through.
            foreach (Model child in modelToExport.Children)
            {
                bool ignoreChild = (child is Simulation && child.Parent is Experiment);

                if (!ignoreChild)
                {
                    if (Array.IndexOf(modelTypesToRecurseDown, child.GetType()) != -1)
                    {
                        string childFolderPath = Path.Combine(workingDirectory, child.Name);
                        AddValidationTags(tags, child, headingLevel + 1, workingDirectory);
                    }
                    else if (child is Memo || child is Graph)
                        child.Document(tags, headingLevel, 0);
                }
            }
        }
    }
}
