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
using UserInterface.Classes;
using MigraDoc.DocumentObjectModel.Fields;
using Models.Agroforestry;
using Models.Zones;
using System.Windows.Forms;
using System.Threading;
using Models.PostSimulationTools;

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
                                                                    typeof(Experiment),
                                                                    typeof(Zone),
                                                                    typeof(AgroforestrySystem),
                                                                    typeof(CircularZone),
                                                                    typeof(RectangularZone),
                                                                    typeof(PredictedObserved)};

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
                    if (heading.headingLevel > 0 && heading.text != "TitlePage")
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
            // Look for child models that are a folder or simulation etc
            // that we need to recurse down through.
            foreach (Model child in modelToExport.Children)
            {
                bool ignoreChild = (child is Simulation && child.Parent is Experiment);

                if (!ignoreChild)
                {
                    if (Array.IndexOf(modelTypesToRecurseDown, child.GetType()) != -1)
                    {
                        tags.Add(new AutoDocumentation.Heading(child.Name, headingLevel));
                        string childFolderPath = Path.Combine(workingDirectory, child.Name);
                        AddValidationTags(tags, child, headingLevel + 1, workingDirectory);

                        if (child.Name == "Validation")
                        {
                            IModel dataStore = Apsim.Child(modelToExport, "DataStore");
                            if (dataStore != null)
                            {
                                tags.Add(new AutoDocumentation.Heading("Statistics", headingLevel + 1));
                                AddValidationTags(tags, dataStore, headingLevel + 2, workingDirectory);
                            }
                        }
                    }
                    else if (child.Name != "TitlePage" && child.Name != "Introduction" && (child is Memo || child is Graph || child is Map || child is Tests))
                        child.Document(tags, headingLevel, 0);
                }
            }
        }

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
            string png1 = Path.Combine(workingDirectory, "AIBanner.png");
            using (FileStream file = new FileStream(png1, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("UserInterface.Resources.AIBanner.png").CopyTo(file);
            }
            section.AddImage(png1);

            List<AutoDocumentation.ITag> tags = new List<AutoDocumentation.ITag>();

            // See if there is a title page. If so do it first.
            IModel titlePage = Apsim.Find(ExplorerPresenter.ApsimXFile, "TitlePage");
            if (titlePage != null)
                titlePage.Document(tags, 1, 0);
            AddBackground(tags);

            // See if there is a title page. If so do it first.
            IModel introductionPage = Apsim.Find(ExplorerPresenter.ApsimXFile, "Introduction");
            if (introductionPage != null)
            {
                tags.Add(new AutoDocumentation.Heading("Introduction", 1));
                introductionPage.Document(tags, 1, 0);
            }

            AddUserDocumentation(tags);

            // Document model description.
            tags.Add(new AutoDocumentation.Heading("Model description", 1));
            ExplorerPresenter.ApsimXFile.DocumentModel(modelNameToExport, tags, 1);

            // Document model validation.
            AddValidationTags(tags, ExplorerPresenter.ApsimXFile, 1, workingDirectory);

            // Move cultivars to end.
            MoveCultivarsToEnd(tags);

            // Strip all blank sections i.e. two headings with nothing between them.
            StripEmptySections(tags);

            // Scan for citations.
            ScanForCitations(tags);

            // Create a bibliography.
            CreateBibliography(tags);

            // numebr all headings.
            NumberHeadings(tags);

            // Populate the PDF section.
            TagsToMigraDoc(section, tags, workingDirectory);

            // Write the PDF file.
            FileNameWritten = Path.Combine(Path.GetDirectoryName(ExplorerPresenter.ApsimXFile.FileName), modelNameToExport + ".pdf");
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false, PdfSharp.Pdf.PdfFontEmbedding.Always);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(FileNameWritten);

            // Remove temporary working directory.
            Directory.Delete(workingDirectory, true);
        }

        /// <summary>Add user documentation, based on the example.</summary>
        /// <param name="tags">The tags to add to.</param>
        private void AddUserDocumentation(List<AutoDocumentation.ITag> tags)
        {
            tags.Add(new AutoDocumentation.Heading("User documentation", 1));
            

        }

        /// <summary>Adds a software availability section</summary>
        /// <param name="tags">The tags to add to.</param>
        private void AddBackground(List<AutoDocumentation.ITag> tags)
        {
            string text = "**Background:** " +
                          "The Agricultural Production Systems sIMulator (APSIM) is a farming systems modelling framework " +
                          "that is being actively developed by the APSIM Initiative. " + Environment.NewLine + Environment.NewLine +
                          " It is comprised of " + Environment.NewLine + Environment.NewLine +
                          " 1. a set of biophysical models that capture the science and management of the system being modelled, " + Environment.NewLine +
                          " 2. a software framework that allows these models to be coupled together to facilitate data exchange between the models, " + Environment.NewLine +
                          " 3. a community of developers and users who work together, to share ideas, data and source code, " + Environment.NewLine +
                          " 4. a data platform to enable this sharing and " + Environment.NewLine +
                          " 5. a user interface to make it accessible to a broad range of users." + Environment.NewLine + Environment.NewLine +
                          " The literature contains numerous papers outlining the many uses of APSIM applied to diverse problem domains. " +
                          " In particular, [holzworth_apsim_2014;keating_overview_2003;mccown_apsim:_1996;mccown_apsim:_1995] " +
                          " have described earlier versions of APSIM in detail, outlining the key APSIM crop and soil process models and presented some examples " +
                          " of the capabilities of APSIM." + Environment.NewLine + Environment.NewLine +

                          "![Alt Text](..\\..\\Documentation\\Images\\Jigsaw.jpg)" + Environment.NewLine + Environment.NewLine +
                          "*Figure: This conceptual representation of an APSIM simulation shows a “top level” farm (with climate, farm management and livestock) " +
                          "and two fields. The farm and each field are built from a combination of models found in the toolbox. The APSIM infrastructure connects all selected model pieces together to form a coherent simulation.*" + Environment.NewLine + Environment.NewLine +

                          "The APSIM Initiative has begun developing a next generation of APSIM (APSIM Next Generation) that is written from scratch and designed " +
                          "to run natively on Windows, LINUX and MAC OSX. The new framework incorporates the best of the APSIM 7.x " +
                          "framework with an improved supporting framework. The Plant Modelling Framework (a generic collection of plant building blocks) was ported " +
                          "from the existing APSIM to bring a rapid development pathway for plant models. The user interface paradigm has been kept the same as the " +
                          "existing APSIM version, but completely rewritten to support new application domains and the newer Plant Modelling Framework. " +
                          "The ability to describe experiments has been added which can also be used for rapidly building factorials of simulations. " +
                          "The ability to write C# scripts to control farm and paddock management has been retained. Finally, all simulation outputs are written to " +
                          "an SQLite database to make it easier and quicker to query, filter and graph outputs." + Environment.NewLine + Environment.NewLine +
                          "The model described in this documentation is for APSIM Next Generation." + Environment.NewLine + Environment.NewLine +

                          "APSIM is freely available for non-commercial purposes. Non-commercial use of APSIM means public-good research & development and educational activities. " +
                          "It includes the support of policy development and/or implementation by, or on behalf of, government bodies and industry-good work where the research outcomes " +
                          "are to be made publicly available. For more information visit <a href=\"http://www.apsim.info/Products/Licensing.aspx\">the licensing page on the APSIM web site</a>";

            tags.Add(new AutoDocumentation.Paragraph(text, 0));
        }

        /// <summary>Strip all blank sections i.e. two headings with nothing between them.</summary>
        /// <param name="tags"></param>
        private void StripEmptySections(List<AutoDocumentation.ITag> tags)
        {
            bool tagsRemoved;
            do
            {
                tagsRemoved = false;
                for (int i = 0; i < tags.Count - 1; i++)
                {
                    AutoDocumentation.Heading thisTag = tags[i] as AutoDocumentation.Heading;
                    AutoDocumentation.Heading nextTag = tags[i + 1] as AutoDocumentation.Heading;
                    if (thisTag != null && nextTag != null && thisTag.headingLevel >= nextTag.headingLevel)
                    {
                        // Need to renumber headings after this tag until we get to the same heading
                        // level that thisTag is on.
                        tags.RemoveAt(i);
                        i--;
                        tagsRemoved = true;
                    }
                }
            }
            while (tagsRemoved);
        }

        /// <summary>Move cultivars to the end of the tags.</summary>
        /// <param name="tags">tags</param>
        private void MoveCultivarsToEnd(List<AutoDocumentation.ITag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tags[i] as AutoDocumentation.Heading;
                    if (heading.text == "Cultivars")
                    {
                        int cultivarHeadingLevel = heading.headingLevel;
                        heading.headingLevel = 1;

                        bool stopMovingTags;
                        do
                        {
                            AutoDocumentation.ITag thisTag = tags[i];
                            tags.Remove(thisTag);
                            tags.Add(thisTag);
                            thisTag = tags[i];
                            if (thisTag is AutoDocumentation.Heading)
                            {
                                AutoDocumentation.Heading thisTagAsHeading = thisTag as AutoDocumentation.Heading;
                                stopMovingTags = thisTagAsHeading.headingLevel <= cultivarHeadingLevel;
                                if (!stopMovingTags)
                                    thisTagAsHeading.headingLevel = 2;
                            }
                            else
                                stopMovingTags = false;
                        }
                        while (!stopMovingTags);
                        return;
                    }
                }
            }
        }

        /// <summary>Creates the PDF syles.</summary>
        /// <param name="document">The document to create the styles in.</param>
        public void CreatePDFSyles(Document document)
        {
            Style style = document.Styles.AddStyle("GraphAndTable", "Normal");
            style.Font.Size = 7;
            style.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0);

            Style xyStyle = document.Styles.AddStyle("GraphAndTable", "Normal");
            xyStyle.Font = new MigraDoc.DocumentObjectModel.Font("Courier New");

            Style tableStyle = document.Styles.AddStyle("Table", "Normal");
            tableStyle.Font.Size = 8;
            tableStyle.ParagraphFormat.SpaceAfter = Unit.FromCentimeter(0);
        }

        /// <summary>Creates the graph.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="graphAndTable">The graph and table to convert to html.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void CreateGraphPDF(Section section, AutoDocumentation.GraphAndTable graphAndTable, string workingDirectory)
        {
            // Create a 2 column, 1 row table. Image in first cell, X/Y data in second cell.
            Table table = section.AddTable();
            table.Style = "GraphAndTable";
            table.Rows.LeftIndent = graphAndTable.indent + "cm";

            Column column1 = table.AddColumn();
            column1.Width = "8cm";
            //column1.Format.Alignment = ParagraphAlignment.Right;
            Column column2 = table.AddColumn();
            column2.Width = "8cm";
            //column2.Format.Alignment = ParagraphAlignment.Right;
            Row row = table.AddRow();

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
                                     System.Drawing.Color.Blue, Models.Graph.LineType.Solid, Models.Graph.MarkerType.None,
                                     Models.Graph.LineThicknessType.Normal, Models.Graph.MarkerSizeType.Normal, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, graphAndTable.xName, false, double.NaN, double.NaN, double.NaN);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, graphAndTable.yName, false, double.NaN, double.NaN, double.NaN);
            graph.BackColor = System.Drawing.Color.White;
            graph.FontSize = 10;
            graph.Refresh();

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(400, 250);
            graph.Export(image, false);
            image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);
            MigraDoc.DocumentObjectModel.Shapes.Image image1 = row.Cells[0].AddImage(PNGFileName);

            // Add x/y data.
            Paragraph xyParagraph = row.Cells[1].AddParagraph();
            xyParagraph.Style = "xyStyle";
            AddFixedWidthText(xyParagraph, "X", 10);
            AddFixedWidthText(xyParagraph, "Y", 10);
            xyParagraph.AddLineBreak();
            for (int i = 0; i < graphAndTable.xyPairs.X.Length; i++)
            {

                AddFixedWidthText(xyParagraph, graphAndTable.xyPairs.X[i].ToString(), 10);
                AddFixedWidthText(xyParagraph, graphAndTable.xyPairs.Y[i].ToString(), 10);
                xyParagraph.AddLineBreak();
            }

            // Add an empty paragraph for spacing.
            Paragraph spacing = section.AddParagraph();
        }

        /// <summary>Creates the table.</summary>
        /// <param name="section">The section.</param>
        /// <param name="tableObj">The table to convert to html.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void CreateTable(Section section, AutoDocumentation.Table tableObj, string workingDirectory)
        {
            // Create a 2 column, 1 row table. Image in first cell, X/Y data in second cell.
            Table table = section.AddTable();
            table.Style = "Table";
            table.Rows.LeftIndent = "1cm";

            foreach (List<string> column in tableObj.data)
            {
                Column column1 = table.AddColumn();
                column1.Width = "1.4cm";
                column1.Format.Alignment = ParagraphAlignment.Right;
            }

            for (int rowIndex = 0; rowIndex < tableObj.data[0].Count; rowIndex++)
            {
                Row row = table.AddRow();
                for (int columnIndex = 0; columnIndex < tableObj.data.Count; columnIndex++)
                {
                    string cellText = tableObj.data[columnIndex][rowIndex];
                    row.Cells[columnIndex].AddParagraph(cellText);
                }
                
            }

            
        }


        /// <summary>
        /// Adds fixed-width text to a MigraDoc paragraph
        /// </summary>
        /// <param name="paragraph">The paragaraph to add text to</param>
        /// <param name="text">The text</param>
        private static void AddFixedWidthText(Paragraph paragraph, string text, int width)
        {
            //For some reason, a parapraph converts all sequences of white
            //space to a single space.  Thus we need to split the text and add
            //the spaces using the AddSpace function.

            int numSpaces = width - text.Length;

            paragraph.AddSpace(numSpaces);
            paragraph.AddText(text);
        }

        /// <summary>Writes PDF for specified auto-doc commands.</summary>
        /// <param name="section">The writer to write to.</param>
        /// <param name="tags">The autodoc tags.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void TagsToMigraDoc(Section section, List<AutoDocumentation.ITag> tags, string workingDirectory)
        {
            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tag as AutoDocumentation.Heading;
                    if (heading.headingLevel > 0 && heading.headingLevel <= 6)
                    {
                        if (heading.headingLevel == 1)
                            section.AddPageBreak();

                        Paragraph para = section.AddParagraph(heading.text, "Heading" + heading.headingLevel);
                        if (heading.headingLevel == 1)
                            para.Format.OutlineLevel = OutlineLevel.Level1;
                        else if (heading.headingLevel == 2)
                            para.Format.OutlineLevel = OutlineLevel.Level2;
                        else if (heading.headingLevel == 3)
                            para.Format.OutlineLevel = OutlineLevel.Level3;
                        else if (heading.headingLevel == 4)
                            para.Format.OutlineLevel = OutlineLevel.Level4;
                        else if (heading.headingLevel == 5)
                            para.Format.OutlineLevel = OutlineLevel.Level5;
                        else if (heading.headingLevel == 6)
                            para.Format.OutlineLevel = OutlineLevel.Level6;
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
                else if (tag is AutoDocumentation.Table)
                {
                    CreateTable(section, tag as AutoDocumentation.Table, workingDirectory);
                }
                else if (tag is Graph)
                {
                    GraphPresenter graphPresenter = new GraphPresenter();
                    GraphView graphView = new GraphView();
                    graphView.BackColor = System.Drawing.Color.White;
                    graphView.FontSize = 12;
                    graphView.Width = 500;
                    graphView.Height = 500;
                    graphPresenter.Attach(tag, graphView, ExplorerPresenter);
                    string PNGFileName = graphPresenter.ExportToPDF(workingDirectory);
                    section.AddImage(PNGFileName);
                    string caption = (tag as Graph).Caption;
                    if (caption != null)
                        section.AddParagraph(caption);
                    graphPresenter.Detach();
                }
                else if (tag is Map)
                {
                    Form f = new Form();
                    f.Width = 700; // 1100;
                    f.Height = 500; // 600;
                    MapPresenter mapPresenter = new MapPresenter();
                    MapView mapView = new MapView();
                    mapView.BackColor = System.Drawing.Color.White;
                    mapView.Parent = f;
                    (mapView as Control).Dock = DockStyle.Fill; 
                    f.Show(); 

                    mapPresenter.Attach(tag, mapView, ExplorerPresenter);

                    Application.DoEvents();
                    Thread.Sleep(2000);
                    Application.DoEvents();
                    string PNGFileName = mapPresenter.ExportToPDF(workingDirectory);
                    section.AddImage(PNGFileName);
                    mapPresenter.Detach();

                    f.Close();
                }
            }
        }

        /// <summary>Adds a formatted paragraph to section.</summary>
        /// <param name="section">The section.</param>
        /// <param name="paragraph">The paragraph.</param>
        private void AddFormattedParagraphToSection(Section section, AutoDocumentation.Paragraph paragraph)
        {
            MarkdownDeep.Markdown markDown = new MarkdownDeep.Markdown();
            markDown.ExtraMode = true;
            string html = markDown.Transform(paragraph.text);

            string imageDirectory = Path.GetDirectoryName(ExplorerPresenter.ApsimXFile.FileName);
            HtmlToMigraDoc.Convert(html, section, imageDirectory);

            Paragraph para = section.LastParagraph;
            para.Format.LeftIndent = Unit.FromCentimeter(paragraph.indent);
            if (paragraph.bookmarkName != null)
                para.AddBookmark(paragraph.bookmarkName);
            if (paragraph.handingIndent)
            {
                para.Format.LeftIndent = "1cm";
                para.Format.FirstLineIndent = "-1cm";
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

                    // citations are of the form [Brown et al. 2014][brown_plant_2014]
                    // where the second bracketed value is the bibliography reference name. i.e.
                    // the bit we're interested in.
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
                                {
                                    citation = bibTeX.Lookup(inTextCitation);
                                    if (citation != null)
                                        citations.Add(citation);
                                }

                                if (citation != null)
                                {
                                    // Replace the in-text citation with (author et al., year)
                                    if (replacementText != string.Empty)
                                        replacementText += "; ";
                                    replacementText += string.Format("<a href=\"#{0}\">{1}</a>", citation.Name, citation.InTextCite);
                                }
                            }

                            if (replacementText != string.Empty)
                            {
                                text = text.Remove(posBracket, posEndBracket - posBracket + 1);
                                text = text.Insert(posBracket, replacementText);
                            }
                        }

                        // Find the next bracketed potential citation.
                        posBracket = text.IndexOf('[', posEndBracket + 1);
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
                string url = citation.URL;
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

