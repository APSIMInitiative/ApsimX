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
using Models.Graph;
using System.Drawing;
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using UserInterface.Classes;
using Models.Agroforestry;
using Models.Zones;
using Models.PostSimulationTools;
using PdfSharp.Fonts;
using System.Data;
using PdfSharp.Drawing;
using Models.Interfaces;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command exports the specified node and all child nodes as HTML.
    /// </summary>
    public class ExportNodeCommand : ICommand
    {
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>A .bib file instance.</summary>
        private BibTeX bibTeX;

        /// <summary>A list of all citations found.</summary>
        private List<BibTeX.Citation> citations;

        /// <summary>Temporary working directory.</summary>
        private string workingDirectory;

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
            string modelName = Path.GetFileNameWithoutExtension(ExplorerPresenter.ApsimXFile.FileName.Replace("Validation", string.Empty));
            modelName = modelName.Replace("validation", string.Empty);
            DoExportPDF(modelName);
            citations.Clear();
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
            int[] levels = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
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

        #region PDF

        /// <summary>
        /// Export to PDF
        /// </summary>
        public void DoExportPDF(string modelNameToExport)
        {
            /// This is a bit tricky on non-Windows platforms. 
            /// Normally PdfSharp tries to get a Windows DC for associated font information
            /// See https://alex-maz.info/pdfsharp_150 for the work-around we can apply here.
            /// See also http://stackoverflow.com/questions/32726223/pdfsharp-migradoc-font-resolver-for-embedded-fonts-system-argumentexception
            /// The work-around is to register our own fontresolver. We don't need to do this on Windows.
            if (Environment.OSVersion.Platform != PlatformID.Win32NT &&
                Environment.OSVersion.Platform != PlatformID.Win32Windows &&
                GlobalFontSettings.FontResolver == null)
               GlobalFontSettings.FontResolver = new MyFontResolver();

            // Create a temporary working directory.
            workingDirectory = Path.Combine(Path.GetTempPath(), "autodoc");
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, true);
            Directory.CreateDirectory(workingDirectory);

            Document document = new Document();
            CreatePDFSyles(document);
            document.DefaultPageSetup.LeftMargin = MigraDoc.DocumentObjectModel.Unit.FromCentimeter(1);
            document.DefaultPageSetup.TopMargin = MigraDoc.DocumentObjectModel.Unit.FromCentimeter(1);
            document.DefaultPageSetup.BottomMargin = MigraDoc.DocumentObjectModel.Unit.FromCentimeter(1);
            Section section = document.AddSection();

            // write image files
            string png1 = Path.Combine(workingDirectory, "AIBanner.png");
            using (FileStream file = new FileStream(png1, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.AIBanner.png").CopyTo(file);
            }
            section.AddImage(png1);

            Paragraph version = new Paragraph();
            version.AddText(ExplorerPresenter.ApsimXFile.ApsimVersion);
            section.Add(version);
            // Convert all models in file to tags.
            List<AutoDocumentation.ITag> tags = new List<AutoDocumentation.ITag>();
            foreach (IModel child in ExplorerPresenter.ApsimXFile.Children)
            {
                AutoDocumentation.DocumentModel(child, tags, headingLevel:1, indent:0);
                if (child.Name == "TitlePage")
                {
                    AddBackground(tags);
                    AddUserDocumentation(tags, modelNameToExport);

                    // Document model description.
                    int modelDescriptionIndex = tags.Count;
                    tags.Add(new AutoDocumentation.Heading("Model description", 1));
                    ExplorerPresenter.ApsimXFile.DocumentModel(modelNameToExport, tags, 1);

                    // If no model was documented then remove the 'Model description' tag.
                    if (modelDescriptionIndex == tags.Count - 1)
                        tags.RemoveAt(modelDescriptionIndex);
                }
                else if (child.Name == "Validation")
                    AddStatistics(tags);
            }

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
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);
            pdfRenderer.Document = document;
            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(FileNameWritten);

            // Remove temporary working directory.
            Directory.Delete(workingDirectory, true);
        }

        /// <summary>Add statistics</summary>
        /// <param name="tags">Document tags to add to.</param>
        private void AddStatistics(List<AutoDocumentation.ITag> tags)
        {
            IModel dataStore = Apsim.Child(ExplorerPresenter.ApsimXFile, "DataStore");
            if (dataStore != null)
            {
                List<IModel> tests = Apsim.FindAll(dataStore, typeof(Tests));
                tests.RemoveAll(m => !m.IncludeInDocumentation);
                if (tests.Count > 0)
                    tags.Add(new AutoDocumentation.Heading("Statistics", 2));

                foreach (Tests test in tests)
                    test.Document(tags, 3, 0);
            }
        }

        /// <summary>Add user documentation, based on the example.</summary>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="modelName">Name of model to document.</param>
        private void AddUserDocumentation(List<AutoDocumentation.ITag> tags, string modelName)
        {
            // Look for some instructions on which models in the example file we should write.
            // Instructions will be in a memo in the validation .apsimx file 

            IModel userDocumentation = Apsim.Get(ExplorerPresenter.ApsimXFile, ".Simulations.UserDocumentation") as IModel;
            string exampleFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "Examples", modelName + ".apsimx");

            if (userDocumentation != null && userDocumentation.Children.Count > 0 && File.Exists(exampleFileName))
            {
                // Write heading.
                tags.Add(new AutoDocumentation.Heading("User documentation", 1));

                // Open the related example .apsimx file and get its presenter.
                ExplorerPresenter examplePresenter = ExplorerPresenter.MainPresenter.OpenApsimXFileInTab(exampleFileName, onLeftTabControl: true);

                Memo instructionsMemo = userDocumentation.Children[0] as Memo;
                string[] instructions = instructionsMemo.Text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string instruction in instructions)
                {
                    IModel model = Apsim.Find(examplePresenter.ApsimXFile, instruction);
                    if (model != null)
                    {
                        examplePresenter.SelectNode(Apsim.FullPath(model));
                        while (Gtk.Application.EventsPending())
                            Gtk.Application.RunIteration();
                        if (model is Memo)
                            AutoDocumentation.DocumentModel(model, tags, 1, 0);
                        else
                        {
                            Image image = examplePresenter.GetScreenhotOfRightHandPanel();
                            if (image != null)
                            {
                                string name = "Example" + instruction;
                                tags.Add(new AutoDocumentation.Image() { name = name, image = image });
                            }
                        }
                    }
                }

                // Close the tab
                examplePresenter.MainPresenter.CloseTabContaining(examplePresenter.GetView().MainWidget);
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
            }
        }

        /// <summary>Adds a software availability section</summary>
        /// <param name="tags">The tags to add to.</param>
        private void AddBackground(List<AutoDocumentation.ITag> tags)
        {
            string text = "The Agricultural Production Systems sIMulator (APSIM) is a farming systems modelling framework " +
                          "that is being actively developed by the APSIM Initiative. " + Environment.NewLine + Environment.NewLine +
                          " It is comprised of " + Environment.NewLine + Environment.NewLine +
                          " 1. a set of biophysical models that capture the science and management of the system being modelled, " + Environment.NewLine +
                          " 2. a software framework that allows these models to be coupled together to facilitate data exchange between the models, " + Environment.NewLine +
                          " 3. a set of input models that capture soil characteristics, climate variables, genotype information, field management etc, " + Environment.NewLine +
                          " 4. a community of developers and users who work together, to share ideas, data and source code, " + Environment.NewLine +
                          " 5. a data platform to enable this sharing and " + Environment.NewLine +
                          " 6. a user interface to make it accessible to a broad range of users." + Environment.NewLine + Environment.NewLine +
                          " The literature contains numerous papers outlining the many uses of APSIM applied to diverse problem domains. " +
                          " In particular, [holzworth_apsim_2014;keating_overview_2003;mccown_apsim:_1996;mccown_apsim:_1995] " +
                          " have described earlier versions of APSIM in detail, outlining the key APSIM crop and soil process models and presented some examples " +
                          " of the capabilities of APSIM." + Environment.NewLine + Environment.NewLine +

                          "![Alt Text](Jigsaw.jpg)" + Environment.NewLine + Environment.NewLine +
                          "**Figure [FigureNumber]:**  This conceptual representation of an APSIM simulation shows a “top level” farm (with climate, farm management and livestock) " +
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
                          "are to be made publicly available. For more information visit <a href=\"https://www.apsim.info/Products/Licensing.aspx\">the licensing page on the APSIM web site</a>";

            tags.Add(new AutoDocumentation.Heading("APSIM Description", 1));
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
                    if (thisTag != null && nextTag != null && (thisTag.headingLevel >= nextTag.headingLevel && nextTag.headingLevel !=-1))
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
            //tableStyle.Font.Size = 8;
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
            int count = 0;
            // If there are multiple graphs with the same name, they may overwrite each other.
            // Therefore, we attempt to generate a unique name. After 20 attempts, we give up.
            while (File.Exists(PNGFileName) && count < 20)
            {
                count++;
                PNGFileName = Path.Combine(graphDirectory, graphAndTable.xyPairs.Parent.Parent.Name + graphAndTable.xyPairs.Parent.Name + Guid.NewGuid() + ".png");
            }
            // Setup graph.
            GraphView graph = new GraphView();
            graph.Clear();
            graph.Width = 400;
            graph.Height = 250;

            // Create a line series.
            graph.DrawLineAndMarkers("", graphAndTable.xyPairs.X, graphAndTable.xyPairs.Y, null,
                                     Models.Graph.Axis.AxisType.Bottom, Models.Graph.Axis.AxisType.Left,
                                     System.Drawing.Color.Blue, Models.Graph.LineType.Solid, Models.Graph.MarkerType.None,
                                     Models.Graph.LineThicknessType.Normal, Models.Graph.MarkerSizeType.Normal, true);

            // Format the axes.
            graph.FormatAxis(Models.Graph.Axis.AxisType.Bottom, graphAndTable.xName, false, double.NaN, double.NaN, double.NaN, false);
            graph.FormatAxis(Models.Graph.Axis.AxisType.Left, graphAndTable.yName, false, double.NaN, double.NaN, double.NaN, false);
            graph.BackColor = OxyPlot.OxyColors.White;
            graph.FontSize = 10;
            graph.Refresh();

            // Export graph to bitmap file.
            Bitmap image = new Bitmap(graph.Width, graph.Height);
            using (Graphics gfx = Graphics.FromImage(image))
            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.White))
            {
                gfx.FillRectangle(brush, 0, 0, image.Width, image.Height);
            }
            graph.Export(ref image, new Rectangle(0, 0, image.Width, image.Height), false);
            graph.MainWidget.Destroy();
            image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);
            MigraDoc.DocumentObjectModel.Shapes.Image sectionImage = row.Cells[0].AddImage(PNGFileName);
            sectionImage.LockAspectRatio = true;
            sectionImage.Width = "8cm";

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
            section.AddParagraph();
        }
		
        /// <summary>Creates the graph page.</summary>
        /// <param name="section">The section to write to.</param>
        /// <param name="graphPage">The graph and table to convert to html.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private void CreateGraphPage(Section section, GraphPage graphPage, string workingDirectory)
        {
            int numGraphsToPrint = graphPage.graphs.FindAll(g => g.IncludeInDocumentation).Count;
            if (numGraphsToPrint > 0)
            {
                int numColumns = 2;
                int numRows = (numGraphsToPrint + 1) / numColumns;

                // Export graph to bitmap file.
                Bitmap image = new Bitmap(1800, numRows * 600);
                using (Graphics gfx = Graphics.FromImage(image))
                using (SolidBrush brush = new SolidBrush(System.Drawing.Color.White))
                {
                    gfx.FillRectangle(brush, 0, 0, image.Width, image.Height);
                }
                GraphPresenter graphPresenter = new GraphPresenter();
                ExplorerPresenter.ApsimXFile.Links.Resolve(graphPresenter);
                GraphView graphView = new GraphView();
                graphView.BackColor = OxyPlot.OxyColors.White;
                graphView.FontSize = 22;
                graphView.MarkerSize = 8;
                graphView.Width = image.Width / numColumns;
                graphView.Height = image.Height / numRows;
                graphView.LeftRightPadding = 0;

                int col = 0;
                int row = 0;
                for (int i = 0; i < graphPage.graphs.Count; i++)
                {
                    if (graphPage.graphs[i].IncludeInDocumentation)
                    {
                        graphPresenter.Attach(graphPage.graphs[i], graphView, ExplorerPresenter);
                        Rectangle r = new Rectangle(col * graphView.Width, row * graphView.Height,
                                                    graphView.Width, graphView.Height);
                        graphView.Export(ref image, r, false);
                        graphPresenter.Detach();
                        col++;
                        if (col >= numColumns)
                        {
                            col = 0;
                            row++;
                        }
                    }
                }

                string PNGFileName = Path.Combine(workingDirectory,
                                                  graphPage.graphs[0].Parent.Parent.Name +
                                                  graphPage.graphs[0].Parent.Name + 
                                                  graphPage.name + ".png");
                image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);

                MigraDoc.DocumentObjectModel.Shapes.Image sectionImage = section.AddImage(PNGFileName);
                sectionImage.LockAspectRatio = true;
                sectionImage.Width = "19cm";
            }
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
            table.Borders.Color = Colors.Blue;
            table.Borders.Width = 0.25;           
            table.Borders.Left.Width = 0.5;
            table.Borders.Right.Width = 0.5;
            table.Rows.LeftIndent = 0;

            foreach (DataColumn column in tableObj.data.Columns)
            {
                Column column1 = table.AddColumn();
                column1.Format.Alignment = ParagraphAlignment.Right;
            }

            Row row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Font.Bold = true;
            row.Shading.Color = Colors.LightBlue;

            XFont gdiFont = new XFont("Arial", 10);
            XGraphics graphics = XGraphics.CreateMeasureContext(new XSize(2000, 2000), XGraphicsUnit.Point, XPageDirection.Downwards);

            for (int columnIndex = 0; columnIndex < tableObj.data.Columns.Count; columnIndex++)
            {
                string heading = tableObj.data.Columns[columnIndex].ColumnName;

                // Get the width of the column
                double maxSize = graphics.MeasureString(heading, gdiFont).Width;
                for (int rowIndex = 0; rowIndex < tableObj.data.Rows.Count; rowIndex++)
                {
                    string cellText = tableObj.data.Rows[rowIndex][columnIndex].ToString();
                    XSize size = graphics.MeasureString(cellText, gdiFont);
                    maxSize = Math.Max(maxSize, size.Width);
                }

                table.Columns[columnIndex].Width = Unit.FromPoint(maxSize + 10);
                row.Cells[columnIndex].AddParagraph(heading);
            }
            for (int rowIndex = 0; rowIndex < tableObj.data.Rows.Count; rowIndex++)
            {
                row = table.AddRow();
                for (int columnIndex = 0; columnIndex < tableObj.data.Columns.Count; columnIndex++)
                {
                    string cellText = tableObj.data.Rows[rowIndex][columnIndex].ToString();
                    row.Cells[columnIndex].AddParagraph(cellText);
                }
                
            }
            section.AddParagraph();
        }


        /// <summary>
        /// Adds fixed-width text to a MigraDoc paragraph
        /// </summary>
        /// <param name="paragraph">The paragaraph to add text to</param>
        /// <param name="text">The text</param>
        private static void AddFixedWidthText(Paragraph paragraph, string text, int width)
        {
            // For some reason, a parapraph converts all sequences of white
            // space to a single space.  Thus we need to split the text and add
            // the spaces using the AddSpace function.

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
            int figureNumber = 0;
            foreach (AutoDocumentation.ITag tag in tags)
            {
                if (tag is AutoDocumentation.Heading)
                {
                    AutoDocumentation.Heading heading = tag as AutoDocumentation.Heading;
                    if (heading.headingLevel > 0 && heading.headingLevel <= 6)
                    {
                        Paragraph para = section.AddParagraph(heading.text, "Heading" + heading.headingLevel);
                        para.Format.KeepWithNext = true;
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
                    AutoDocumentation.Paragraph paragraph = tag as AutoDocumentation.Paragraph;
                    if (paragraph.text.Contains("![Alt Text]"))
                        figureNumber++;
                    paragraph.text = paragraph.text.Replace("[FigureNumber]", figureNumber.ToString());
                    AddFormattedParagraphToSection(section, paragraph);
                }
                else if (tag is AutoDocumentation.GraphAndTable)
                {
                    CreateGraphPDF(section, tag as AutoDocumentation.GraphAndTable, workingDirectory);
                }
                else if (tag is GraphPage)
                {
                    CreateGraphPage(section, tag as GraphPage, workingDirectory);
                }
                else if (tag is AutoDocumentation.NewPage)
                {
                    section.AddPageBreak();
                }
                else if (tag is AutoDocumentation.Table)
                {
                    CreateTable(section, tag as AutoDocumentation.Table, workingDirectory);
                }
                else if (tag is Graph)
                {
                    GraphPresenter graphPresenter = new GraphPresenter();
                    ExplorerPresenter.ApsimXFile.Links.Resolve(graphPresenter);
                    GraphView graphView = new GraphView();
                    graphView.BackColor = OxyPlot.OxyColors.White;
                    graphView.FontSize = 12;
                    graphView.Width = 500;
                    graphView.Height = 500;
                    graphPresenter.Attach(tag, graphView, ExplorerPresenter);
                    string PNGFileName = graphPresenter.ExportToPNG(workingDirectory);
                    section.AddImage(PNGFileName);
                    string caption = (tag as Graph).Caption;
                    if (caption != null)
                        section.AddParagraph(caption);
                    graphPresenter.Detach();
                    graphView.MainWidget.Destroy();
                }
                else if (tag is Map && (tag as Map).GetCoordinates().Count > 0)
                {
                    MapPresenter mapPresenter = new MapPresenter();
                    MapView mapView = new MapView(null);
                    mapPresenter.Attach(tag, mapView, ExplorerPresenter);
                    string PNGFileName = mapPresenter.ExportToPNG(workingDirectory);
                    if (!String.IsNullOrEmpty(PNGFileName))
                        section.AddImage(PNGFileName);
                    mapPresenter.Detach();
                    mapView.MainWidget.Destroy();
                }
                else if (tag is AutoDocumentation.Image)
                {
                    AutoDocumentation.Image imageTag = tag as AutoDocumentation.Image;
                    if (imageTag.image.Width > 700)
                        imageTag.image = ImageUtilities.ResizeImage(imageTag.image, 700, 500);
                    string PNGFileName = Path.Combine(workingDirectory, imageTag.name);
                    imageTag.image.Save(PNGFileName, System.Drawing.Imaging.ImageFormat.Png);
                    section.AddImage(PNGFileName);
                    figureNumber++;
                }
                else if (tag is AutoDocumentation.ModelView)
                {
                    AutoDocumentation.ModelView modelView = tag as AutoDocumentation.ModelView;
                    ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(modelView.model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                    PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(modelView.model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;
                    if (viewName != null && presenterName != null)
                    {
                        ViewBase view = Assembly.GetExecutingAssembly().CreateInstance(viewName.ToString(), false, BindingFlags.Default, null, new object[] { null }, null, null) as ViewBase;
                        IPresenter presenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as IPresenter;

                        if (view != null && presenter != null)
                        {
                            ExplorerPresenter.ApsimXFile.Links.Resolve(presenter);
                            presenter.Attach(modelView.model, view, ExplorerPresenter);

                            Gtk.Window popupWin;
                            if (view is MapView)
                            {
                                popupWin = (view as MapView).GetPopupWin();
                                popupWin.SetSizeRequest(515, 500);
                            }
                            else
                            {
                                popupWin = new Gtk.Window(Gtk.WindowType.Popup);
                                popupWin.SetSizeRequest(800, 800);
                                popupWin.Add(view.MainWidget);
                            }
                            popupWin.ShowAll();
                            while (Gtk.Application.EventsPending())
                                Gtk.Application.RunIteration();

                            string PNGFileName = (presenter as IExportable).ExportToPNG(workingDirectory);
                            section.AddImage(PNGFileName);
                            presenter.Detach();
                            view.MainWidget.Destroy();
                            popupWin.Destroy();
                        }
                    }
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

            HtmlToMigraDoc.Convert(html, section, workingDirectory);

            Paragraph para = section.LastParagraph;
            para.Format.LeftIndent += Unit.FromCentimeter(paragraph.indent);
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
            if (citations.Count > 0)
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
        }

        #endregion
    }

    public class MyFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Ignore case of font names.
            var name = familyName.ToLower();

            // Deal with the fonts we know.
            if (name.StartsWith("courier"))
                return new FontResolverInfo("Courier#");
            else
            {
                if (isBold)
                {
                    if (isItalic)
                        return new FontResolverInfo("Arial#bi");
                    return new FontResolverInfo("Arial#b");
                }
                if (isItalic)
                    return new FontResolverInfo("Arial#i");
                return new FontResolverInfo("Arial#");
            }
        }

        /// <summary>
        /// Return the font data for the fonts.
        /// </summary>
        public byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "Courier#":
                    return MyFontHelper.Courier;

                case "Arial#":
                    return MyFontHelper.Arial;

                case "Arial#b":
                    return MyFontHelper.ArialBold;

                case "Arial#i":
                    return MyFontHelper.ArialItalic;

                case "Arial#bi":
                    return MyFontHelper.ArialBoldItalic;
            }
            return null;
        }
    }

    /// <summary>
    /// Helper class that reads font data from embedded resources.
    /// </summary>
    public static class MyFontHelper
    {
        public static byte[] Courier
        {
            get { return LoadFontData("ApsimNG.Resources.Fonts.cour.ttf"); }
        }

        // Make sure the fonts have compile type "Embedded Resource". Names are case-sensitive.
        public static byte[] Arial
        {
            get { return LoadFontData("ApsimNG.Resources.Fonts.arial.ttf"); }
        }

        public static byte[] ArialBold
        {
            get { return LoadFontData("ApsimNG.Resources.Fonts.arialbd.ttf"); }
        }

        public static byte[] ArialItalic
        {
            get { return LoadFontData("ApsimNG.Resources.Fonts.ariali.ttf"); }
        }

        public static byte[] ArialBoldItalic
        {
            get { return LoadFontData("ApsimNG.Resources.Fonts.arialbi.ttf"); }
        }

        /// <summary>
        /// Returns the specified font from an embedded resource.
        /// </summary>
        static byte[] LoadFontData(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + name);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }


    /// <summary>A simple container for holding a directed graph - used in auto-doc</summary>
    public class DirectedGraphContainer : IVisualiseAsDirectedGraph
    {
        /// <summary>A property for holding the graph</summary>
        public DirectedGraph DirectedGraphInfo { get; set; }

        public DirectedGraphContainer(DirectedGraph graph)
        {
            DirectedGraphInfo = graph;
        }
    }
}

