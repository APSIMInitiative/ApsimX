namespace UserInterface.Commands
{
    using ApsimNG.Classes;
    using Models;
    using Models.Core;
    using Presenters;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// This command creates documentation for a model a.k.a. auto-doc
    /// </summary>
    public class CreateDocCommand : ICommand
    {
        private ExplorerPresenter explorerPresenter;

        /// <summary>A .bib file instance.</summary>
        private BibTeX bibTeX;

        /// <summary>A list of all citations found.</summary>
        private List<BibTeX.Citation> citations;

        /// <summary>The name of the model to document.</summary>
        private string modelNameToDocument;

        /// <summary>Gets the name of the file .</summary>
        public string FileNameWritten { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocCommand"/> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public CreateDocCommand(ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
            modelNameToDocument = Path.GetFileNameWithoutExtension(explorerPresenter.ApsimXFile.FileName.Replace("Validation", string.Empty));
            modelNameToDocument = modelNameToDocument.Replace("validation", string.Empty);
            FileNameWritten = Path.Combine(Path.GetDirectoryName(explorerPresenter.ApsimXFile.FileName), modelNameToDocument + ".pdf");
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory commandHistory)
        {
            string bibFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "APSIM.bib");
            bibTeX = new BibTeX(bibFile);
            citations = new List<BibTeX.Citation>();

            CreatePDF(modelNameToDocument);
            citations.Clear();
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory commandHistory)
        {
        }
        
        /// <summary>
        /// Export to PDF
        /// </summary>
        public void CreatePDF(string modelNameToExport)
        {
            var pdfWriter = new PDFWriter(explorerPresenter, portraitOrientation: true);

            // write image files
            Image banner;
            banner = new Bitmap(Assembly.GetExecutingAssembly().GetManifestResourceStream("ApsimNG.Resources.AIBanner.png"));

            // Convert all models in file to tags.
            var tags = new List<AutoDocumentation.ITag>();

            // Add banner and version.
            tags.Add(new AutoDocumentation.Image() { image = banner, name = "AIBanner" });
            tags.Add(new AutoDocumentation.Paragraph(explorerPresenter.ApsimXFile.ApsimVersion, 0));

            foreach (IModel child in explorerPresenter.ApsimXFile.Children)
            {
                DocumentModel(tags, child);
                if (child.Name == "TitlePage")
                {
                    AddBackground(tags);
                    AddUserDocumentation(tags, modelNameToExport);

                    // Document model description.
                    int modelDescriptionIndex = tags.Count;
                    tags.Add(new AutoDocumentation.Heading("Model description", 1));
                    explorerPresenter.ApsimXFile.DocumentModel(modelNameToExport, tags, 1);

                    // If no model was documented then remove the 'Model description' tag.
                    if (modelDescriptionIndex == tags.Count - 1)
                        tags.RemoveAt(modelDescriptionIndex);
                }
                else if (child.Name == "Validation")
                    AddStatistics(tags);
            }
            // Scan for citations.
            ScanForCitations(tags);

            // Create a bibliography.
            CreateBibliography(tags);

            // Write the PDF.
            pdfWriter.CreatePDF(tags, FileNameWritten);
        }

        /// <summary>
        /// Document the specified model.
        /// </summary>
        /// <param name="tags">Document tags to add to.</param>
        /// <param name="modelToDocument">The model to document.</param>
        private void DocumentModel(List<AutoDocumentation.ITag> tags, IModel modelToDocument)
        {
            var childParent = Apsim.Parent(modelToDocument, typeof(Simulation));
            if (childParent == null || childParent is Simulations)
                AutoDocumentation.DocumentModel(modelToDocument, tags, headingLevel: 1, indent: 0);
            else
            {
                var clonedModel = Apsim.Clone(modelToDocument);
                try
                {
                    explorerPresenter.ApsimXFile.Links.Resolve(clonedModel, true);
                    AutoDocumentation.DocumentModel(clonedModel, tags, headingLevel: 1, indent: 0);
                }
                finally
                {
                    explorerPresenter.ApsimXFile.Links.Unresolve(clonedModel, true);
                }
            }
        }

        /// <summary>Add statistics</summary>
        /// <param name="tags">Document tags to add to.</param>
        private void AddStatistics(List<AutoDocumentation.ITag> tags)
        {
            IModel dataStore = Apsim.Child(explorerPresenter.ApsimXFile, "DataStore");
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

            IModel userDocumentation = Apsim.Get(explorerPresenter.ApsimXFile, ".Simulations.UserDocumentation") as IModel;
            string exampleFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "Examples", modelName + ".apsimx");

            if (userDocumentation != null && userDocumentation.Children.Count > 0 && File.Exists(exampleFileName))
            {
                // Write heading.
                tags.Add(new AutoDocumentation.Heading("User documentation", 1));

                // Open the related example .apsimx file and get its presenter.
                ExplorerPresenter examplePresenter = explorerPresenter.MainPresenter.OpenApsimXFileInTab(exampleFileName, onLeftTabControl: true);

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
                          "are to be made publicly available. For more information visit <a href=\"https://apsimdev.apsim.info/Products/Licensing.aspx\">the licensing page on the APSIM web site</a>";

            tags.Add(new AutoDocumentation.Heading("APSIM Description", 1));
            tags.Add(new AutoDocumentation.Paragraph(text, 0));
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

    }
}