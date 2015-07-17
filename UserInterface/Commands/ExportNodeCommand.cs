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
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel;
using System.Xml;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.DocumentObjectModel.Shapes;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    public class ExportNodeCommand : ICommand
    {
        private ExplorerPresenter ExplorerPresenter;
        private string NodePath;
        private string[] indents = new string[] { string.Empty, " class=\"tab1\"", " class=\"tab2\"", " class=\"tab3\"",
                                                  " class=\"tab4\"", " class=\"tab5\"", " class=\"tab6\"",
                                                  " class=\"tab7\"", " class=\"tab8\"", " class=\"tab9\""};

        // Setup a list of model types that we will recurse down through.
        private static Type[] modelTypesToRecurseDown = new Type[] {typeof(Folder),
                                                                    typeof(Simulations),
                                                                    typeof(Simulation),
                                                                    typeof(Experiment)};

        /// <summary>A .bib file instance.</summary>
        private BibTeX bibTeX;

        /// <summary>A list of all citations found.</summary>
        private List<BibTeX.Citation> citations;

        /// <summary>Gets the name of the file .</summary>
        public string FileNameWritten { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportNodeCommand"/> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        /// <param name="nodePath">The node path.</param>
        public ExportNodeCommand(ExplorerPresenter explorerPresenter, string nodePath)
        {
            this.ExplorerPresenter = explorerPresenter;
            this.NodePath = nodePath;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            string bibFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "APSIM.bib");
            bibTeX = new BibTeX(bibFile);
            citations = new List<BibTeX.Citation>();

            // Get the model we are to export.
            string modelName = Path.GetFileNameWithoutExtension(ExplorerPresenter.ApsimXFile.FileName.Replace("Validation", ""));
            DoExportPDF(modelName);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
        }
        
        /// <summary>Creates a table of contents.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="tags">The autodoc tags.</param>
        private void NumberHeadings(List<AutoDocumentation.ITag> tags)
        {
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
                    }
                }
            }
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
                    else if (child.Name != "TitlePage" && (child is Memo || child is Graph))
                        child.Document(tags, headingLevel, 0);
                }
            }
        }

        #region HTML

        /// <summary>
        /// Export to HTML.
        /// </summary>
        public void DoExportHTML(string modelNameToExport)
        {
            // Create a temporary working directory.
            string workingDirectory = Path.Combine(Path.GetDirectoryName(ExplorerPresenter.ApsimXFile.FileName), "Doc");
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, true);
            Directory.CreateDirectory(workingDirectory);

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
            FileNameWritten = Path.Combine(workingDirectory, modelNameToExport + ".html");
            StreamWriter index = new StreamWriter(FileNameWritten);
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

            NumberHeadings(tags);
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

            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tag as AutoDocumentation.Heading;
                    if (heading.headingLevel > 0 && (heading.headingLevel < 3 || heading.text.StartsWith("2")))
                        writer.WriteLine("<dt{0}><a href=\"#{1}\">{1}</a><br/></dt>", indents[heading.headingLevel], heading.text);
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
                    graphView.BackColor = System.Drawing.Color.White;
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
                                     System.Drawing.Color.Blue, Models.Graph.LineType.Solid, Models.Graph.MarkerType.None, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, graphAndTable.xName, false, double.NaN, double.NaN, double.NaN);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, graphAndTable.yName, false, double.NaN, double.NaN, double.NaN);
            graph.BackColor = System.Drawing.Color.White;
            graph.Refresh();
            graph.FormatTitle(graphAndTable.title);

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(480, 480);
            graph.Export(image, false);
            image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);
        }
        
        #endregion

        #region PDF

        /// <summary>
        /// Export to PDF
        /// </summary>
        public void DoExportPDF(string modelNameToExport)
        {
            // Create a temporary working directory.
            string workingDirectory = Path.Combine(Path.GetTempPath(), "autodoc");
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, true);
            Directory.CreateDirectory(workingDirectory);

            Document document = new Document();
            CreatePDFSyles(document);
            Section section = document.AddSection();

            // write image files
            string png1 = Path.Combine(workingDirectory, "apsim_logo.png");
            using (FileStream file = new FileStream(png1, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.apsim_logo.png").CopyTo(file);
            }

            string png2 = Path.Combine(workingDirectory, "hd_bg.png");
            using (FileStream file = new FileStream(png2, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.hd_bg.png").CopyTo(file);
            }
            section.AddImage(png1);

            List<AutoDocumentation.ITag> tags = new List<AutoDocumentation.ITag>();

            // See if there is a title page.
            IModel titlePage = Apsim.Find(ExplorerPresenter.ApsimXFile, "TitlePage");
            if (titlePage != null)
            {
                titlePage.Document(tags, 1, 0);
                ScanForCitations(tags);
                WritePDF(section, tags, workingDirectory);
                tags.Clear();
            }

            // Document model description.
            tags.Add(new AutoDocumentation.Heading("Model description", 1));
            ExplorerPresenter.ApsimXFile.DocumentModel(modelNameToExport, tags, 2);

            // Document model validation.
            tags.Add(new AutoDocumentation.Heading("Validation", 1));
            AddValidationTags(tags, ExplorerPresenter.ApsimXFile, 1, workingDirectory);

            // Scan for citations.
            ScanForCitations(tags);

            // Create a bibliography.
            CreateBibliography(tags);

            // numebr all headings.
            NumberHeadings(tags);

            // Populate the PDF section.
            WritePDF(section, tags, workingDirectory);

            // Write the PDF file.
            FileNameWritten = Path.Combine(Path.GetDirectoryName(ExplorerPresenter.ApsimXFile.FileName), modelNameToExport + ".pdf");
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false, PdfSharp.Pdf.PdfFontEmbedding.Always);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(FileNameWritten);

            // Remove temporary working directory.
            Directory.Delete(workingDirectory, true);
        }
    
        /// <summary>Creates the PDF syles.</summary>
        /// <param name="document">The document to create the styles in.</param>
        public void CreatePDFSyles(Document document)
        {
            Style normalStyle = document.Styles["Normal"];
            normalStyle.Font.Size = 10;
            normalStyle.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0.5);

            //normalStyle.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(1);

            // Create a new style called Table based on style Normal
            Style style = document.Styles.AddStyle("H1", "Normal");
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.5);
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.25);
            style.Font.Size = 18;

            style = document.Styles.AddStyle("H2", "Normal");
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.5);
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.25);
            style.Font.Size = 16;

            style = document.Styles.AddStyle("H3", "Normal");
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.5);
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.25);
            style.Font.Size = 14;

            style = document.Styles.AddStyle("H4", "Normal");
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.5);
            style.ParagraphFormat.SpaceBefore = Unit.FromCentimeter(0.25);
            style.Font.Size = 12;

            style = document.Styles.AddStyle("GraphAndTable", "Normal");
            style.Font.Size = 8;
            style.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0);
        }

        /// <summary>Creates the graph.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="graphAndTable">The graph and table to convert to html.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void CreateGraphPDF(Section section, AutoDocumentation.GraphAndTable graphAndTable, string workingDirectory)
        {
            // Ensure graphs directory exists.
            string graphDirectory = Path.Combine(workingDirectory, "Graphs");
            Directory.CreateDirectory(graphDirectory);

            // Determine the name of the .png file to write.
            string PNGFileName = Path.Combine(graphDirectory,
                                              graphAndTable.xyPairs.Parent.Parent.Name + graphAndTable.xyPairs.Parent.Name + ".png");

            // Setup graph.
            GraphView graph = new GraphView();
            graph.Clear();

            // Create a line series.
            graph.DrawLineAndMarkers("", graphAndTable.xyPairs.X, graphAndTable.xyPairs.Y,
                                     Models.Graph.Axis.AxisType.Bottom, Models.Graph.Axis.AxisType.Left,
                                     System.Drawing.Color.Blue, Models.Graph.LineType.Solid, Models.Graph.MarkerType.None, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, graphAndTable.xName, false, double.NaN, double.NaN, double.NaN);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, graphAndTable.yName, false, double.NaN, double.NaN, double.NaN);
            graph.BackColor = System.Drawing.Color.White;
            graph.Refresh();
            graph.FormatTitle(graphAndTable.title);

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(440, 440);
            graph.Export(image, false);
            image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);
            MigraDoc.DocumentObjectModel.Shapes.Image image1 = section.AddImage(PNGFileName);
            image1.Height = "8cm";
            image1.Width = "8cm";
            image1.LockAspectRatio = true;
            image1.Left = graphAndTable.indent + "cm";

            // Add a table.
            Table table = section.AddTable();
            table.Style = "GraphAndTable";
            table.Rows.LeftIndent = graphAndTable.indent + "cm";
            Column column1 = table.AddColumn();
            column1.Format.Alignment = ParagraphAlignment.Right;
            Column column2 = table.AddColumn();
            column2.Format.Alignment = ParagraphAlignment.Right;
            Row row = table.AddRow();
            row.HeadingFormat = true;
            row.Cells[0].AddParagraph("X");
            row.Cells[1].AddParagraph("Y");
            for (int i = 0; i < graphAndTable.xyPairs.X.Length; i++)
            {
                row = table.AddRow();
                row.Cells[0].AddParagraph(graphAndTable.xyPairs.X[i].ToString());
                row.Cells[1].AddParagraph(graphAndTable.xyPairs.Y[i].ToString());
            }

            // Add an empty paragraph for spacing.
            section.AddParagraph();

        }

        /// <summary>Writes PDF for specified auto-doc commands.</summary>
        /// <param name="section">The writer to write to.</param>
        /// <param name="tags">The autodoc tags.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void WritePDF(Section section, List<AutoDocumentation.ITag> tags, string workingDirectory)
        {

            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tag as AutoDocumentation.Heading;
                    if (heading.headingLevel > 0 && heading.headingLevel < 4)
                    {
                        Paragraph para = section.AddParagraph(heading.text, "H" + heading.headingLevel);
                        if (heading.headingLevel == 1)
                            para.Format.OutlineLevel = OutlineLevel.Level1;
                        else if (heading.headingLevel == 2)
                            para.Format.OutlineLevel = OutlineLevel.Level2;
                        else if (heading.headingLevel == 3)
                            para.Format.OutlineLevel = OutlineLevel.Level3;
                        else if (heading.headingLevel == 4)
                            para.Format.OutlineLevel = OutlineLevel.Level4;
                    }
                }
                else if (tag is AutoDocumentation.Paragraph)
                {
                    AddFormattedParagraphToSection(section, tag as AutoDocumentation.Paragraph);
                }
                else if (tag is AutoDocumentation.GraphAndTable)
                {
                    CreateGraphPDF(section, tag as AutoDocumentation.GraphAndTable, workingDirectory);
                }
                else if (tag is Graph)
                {
                    GraphPresenter graphPresenter = new GraphPresenter();
                    GraphView graphView = new GraphView();
                    graphView.BackColor = System.Drawing.Color.White;
                    graphPresenter.Attach(tag, graphView, ExplorerPresenter);
                    string PNGFileName = graphPresenter.ExportToPDF(workingDirectory);
                    section.AddImage(PNGFileName);
                    string caption = (tag as Graph).Caption;
                    if (caption != null)
                        section.AddParagraph(caption);
                    graphPresenter.Detach();
                }
            }
        }

        /// <summary>Adds a formatted paragraph to section.</summary>
        /// <param name="section">The section.</param>
        /// <param name="paragraph">The paragraph.</param>
        private void AddFormattedParagraphToSection(Section section, AutoDocumentation.Paragraph paragraph)
        {
            Paragraph para = section.AddParagraph();
            para.Format.LeftIndent = Unit.FromCentimeter(paragraph.indent);
            if (paragraph.bookmarkName != null)
                para.AddBookmark(paragraph.bookmarkName);

            if (paragraph.handingIndent)
            {
                para.Format.LeftIndent = "1cm";
                para.Format.FirstLineIndent = "-1cm";
            }

            string text = paragraph.text;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<p>" + text.Replace(@"\&", "and") + "</p>");

            foreach (XmlNode element in doc.DocumentElement.ChildNodes)
            {
                if (element.Name == "i")
                    para.AddFormattedText(element.InnerText, TextFormat.Italic);
                else if (element.Name == "b")
                    para.AddFormattedText(element.InnerText, TextFormat.Bold);
                else if (element.Name == "u")
                    para.AddFormattedText(element.InnerText, TextFormat.Underline);
                else if (element.Name == "a")
                {
                    string href = XmlUtilities.Attribute(element, "href").Replace("EQUALS", "=");
                    href = href.Replace("AND", "&");
                    Hyperlink link;
                    if (href.StartsWith("#"))
                    {
                        link = para.AddHyperlink(href.Substring(1), HyperlinkType.Bookmark);
                    }
                    else
                        link = para.AddHyperlink(href, HyperlinkType.Web);

                    link.Font.Color = new MigraDoc.DocumentObjectModel.Color(33, 151, 210);
                    string linkText = element.InnerText;
                    link.AddText(linkText);
                }
                else if (element.Name == "sub")
                {
                    FormattedText subText = para.AddFormattedText(element.InnerText);
                    subText.Subscript = true;
                }
                else if (element.Name == "sup")
                {
                    FormattedText supText = para.AddFormattedText(element.InnerText);
                    supText.Superscript = true;
                }
                else
                    para.AddText(element.InnerText);
            }
        }

        /// <summary>Scans for citations.</summary>
        /// <param name="t">The tags to go through looking for citations.</param>
        private void ScanForCitations(List<AutoDocumentation.ITag> tags)
        {
            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Paragraph)
                {
                    AutoDocumentation.Paragraph paragraph = tag as AutoDocumentation.Paragraph;
                    string text = paragraph.text;
                    int posBracket = text.IndexOf('[');
                    while (posBracket != -1)
                    {
                        int posEndBracket = text.IndexOf(']', posBracket);
                        if (posEndBracket != -1)
                        {
                            // found a possible citation.
                            string citationName = text.Substring(posBracket + 1, posEndBracket - posBracket - 1);

                            string[] inTextCitations = citationName.Split("; ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            string replacementText = string.Empty;

                            foreach (string inTextCitation in inTextCitations)
                            {
                                // see if we have already encountered the citation.
                                BibTeX.Citation citation = citations.Find(c => c.Name == inTextCitation);

                                // If we haven't encountered it, look it up in the .bib file.
                                if (citation == null)
                                    citation = bibTeX.Lookup(inTextCitation);

                                if (citation != null)
                                {
                                    // Add it to our list of citations.
                                    citations.Add(citation);

                                    // Replace the in-text citation with (author et al., year)
                                    if (replacementText != string.Empty)
                                        replacementText += "; ";
                                    replacementText += string.Format("<a href=\"#{0}\">{1}</a>", citation.Name, citation.InTextCite);
                                }
                            }

                            replacementText = "(" + replacementText + ")";
                            text = text.Remove(posBracket, posEndBracket - posBracket + 1);
                            text = text.Insert(posBracket, replacementText);
                        }

                        // Find the next bracketed potential citation.
                        posBracket = text.IndexOf('[', posBracket + 1);
                    }

                    paragraph.text = text;
                }
            }
        }

        /// <summary>Creates the bibliography.</summary>
        /// <param name="tags">The tags to add to.</param>
        private void CreateBibliography(List<AutoDocumentation.ITag> tags)
        {
            // Create the heading.
            tags.Add(new AutoDocumentation.Heading("References", 1));

            citations.Sort(new BibTeX.CitationComparer());
            foreach (BibTeX.Citation citation in citations)
            {
                string url = citation.URL.Replace("=", "EQUALS");
                url = url.Replace("&", "AND");
                string text;
                if (url != string.Empty)
                    text = string.Format("<a href=\"{0}\">{1}</a>", url, citation.BibliographyText);
                else
                    text = citation.BibliographyText;

                AutoDocumentation.Paragraph paragraph = new AutoDocumentation.Paragraph(text, 0);
                paragraph.bookmarkName = citation.Name;
                paragraph.handingIndent = true;
                tags.Add(paragraph);
            }
        }


        #endregion
    }
}

