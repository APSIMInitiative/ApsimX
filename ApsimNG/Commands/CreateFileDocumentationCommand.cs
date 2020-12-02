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
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// This command creates documentation for a file. e.g. wheat validation or tutorial
    /// </summary>
    public class CreateFileDocumentationCommand
    {
        private ExplorerPresenter explorerPresenter;

        /// <summary>The name of the model to document.</summary>
        private string modelNameToDocument;

        /// <summary>Gets the name of the file .</summary>
        public string FileNameWritten { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFileDocumentationCommand"/> class.
        /// </summary>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        /// <param name="destinationFolder">Destination directory.</param>
        public CreateFileDocumentationCommand(ExplorerPresenter explorerPresenter, string destinationFolder)
        {
            this.explorerPresenter = explorerPresenter;
            modelNameToDocument = Path.GetFileNameWithoutExtension(explorerPresenter.ApsimXFile.FileName.Replace("Validation", string.Empty));
            modelNameToDocument = modelNameToDocument.Replace("validation", string.Empty);
            FileNameWritten = Path.Combine(destinationFolder, modelNameToDocument + ".pdf");
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do()
        {
            CreatePDF(modelNameToDocument);
        }

        /// <summary>
        /// Export to PDF
        /// </summary>
        private void CreatePDF(string modelNameToExport)
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
            var childParent = modelToDocument.FindAncestor<Simulation>();
            if (childParent == null)
                AutoDocumentation.DocumentModel(modelToDocument, tags, headingLevel: 1, indent: 0);
            else
            {
                IModel clonedModel;
                if (modelToDocument is Simulation sim)
                    clonedModel = new Models.Core.Run.SimulationDescription(sim).ToSimulation();
                else
                    clonedModel = Apsim.Clone(modelToDocument);
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
            IModel dataStore = explorerPresenter.ApsimXFile.FindChild("DataStore");
            if (dataStore != null)
            {
                IEnumerable<Tests> tests = dataStore.FindAllInScope<Tests>().Where(m => m.IncludeInDocumentation);
                if (tests.Count() > 0)
                    tags.Add(new AutoDocumentation.Heading("Statistics", 2));

                foreach (Tests test in tests)
                    test.Document(tags, 3, 0);
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
    }
}