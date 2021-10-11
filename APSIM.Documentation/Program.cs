using APSIM.Services.Documentation.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using APSIM.Documentation.Models;
using APSIM.Interop.Documentation;
using APSIM.Shared.Utilities;
using Models;
using System.Text;

namespace APSIM.Documentation
{
    class Program
    {
        private static readonly string apsimx = PathUtilities.GetAbsolutePath("%root%", null);
        private static readonly string underReview = Path.Combine(apsimx, "Tests", "UnderReview");
        private static readonly string validation = Path.Combine(apsimx, "Tests", "Validation");
        private static readonly string resources = Path.Combine(apsimx, "Models", "Resources");
        private static readonly string examples = Path.Combine(apsimx, "Examples");
        private static PdfOptions options = PdfOptions.Default;

        static int Main(string[] args)
        {
            try
            {
                // Get autodocs config - ie which models to document.
                IEnumerable<IDocumentationTable> tables = new[]
                {
                    GetAutodocsConfig(),
                    GetTutorialsTable(),
                    GetClemTable(),
                };
                StringBuilder html = new StringBuilder();
                html.AppendLine("<html>");
                html.AppendLine("<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head>");
                html.AppendLine("<body>");

                // Create new working directory, into which all docs will be generated.
                string outputPath = Path.Combine("/home/drew/code/ApsimX/autodocs");
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                // Build each table of documents.
                foreach (IDocumentationTable table in tables)
                {
                    // Generate autodocs.
                    table.BuildDocuments(outputPath);

                    // todo - stream
                    string markup = table.BuildHtmlDocument();
                    html.AppendLine(markup);
                }

                // Built index.html file.
                html.AppendLine("</body></html>");
                string index = Path.Combine(outputPath, "index.html");
                File.WriteAllText(index, html.ToString());

                Console.WriteLine($"Successfully generated files at {outputPath}");
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err);
                return 1;
            }
            return 0;
        }

        private static IDocumentationTable GetTutorialsTable()
        {
            IEnumerable<string> cols = new string[2] { "Name", "Tutorial" };
            IEnumerable<IDocumentationRow> rows = new[]
            {
                StandardTutorialRow("Climate Controller", "ClimateController"),
                LifecycleRow(),
                StandardTutorialRow("Manager", "Manager"),
                StandardTutorialRow("Parameter sensitivity (Morris)", "Sensitivity_MorrisMethod"),
                StandardTutorialRow("Parameter sensitivity (SOBOL)", "Sensitivity_SobolMethod"),
                StandardTutorialRow("Parameter sensitivity (Factorial ANOVA)", "Sensitivity_FactorialANOVA"),
                StandardTutorialRow("Report", "Report"),
            };
            return new DocumentationTable(cols, rows);
        }

        private static IDocumentationTable GetClemTable()
        {
            IEnumerable<string> cols = new string[4] { "Name", "Science Documentation", "Example1", "Example2" };
            IEnumerable<IDocumentationRow> rows = new[] { ClemDocsRow() };
            return new DocumentationTable(cols, rows);
        }

        private static IDocumentationTable GetAutodocsConfig()
        {
            IEnumerable<string> cols = new[] { "Name", "Documentation", "Params/Inputs/Outputs", "Detailed" };
            IEnumerable<IDocumentationRow> rows = new[]
            {
                StandardDocsRow("AGPRyegrass (AgPasture)", "AGPRyegrass.json", "AgPasture.apsimx", "AgpRyegrass.pdf"),
                StandardDocsRow("AGPWhiteClover (AgPasture)", "AGPWhiteClover.json", "AgPasture.apsimx", "AgpWhiteClover.pdf"),
                // todo: agroforestry
                StandardPmfPlantRow("Barley"),
                StandardPmfPlantRow("Chicory"),
                StandardPmfPlantRow("Chickpea"),
                StandardPmfPlantRow("Eucalyptus"),
                StandardPmfPlantRow("FodderBeet"),
                StandardPmfPlantRow("Gliricidia"),
                StandardPmfPlantRow("Maize"),
                ModelWithNoResourceRow("MicroClimate"),
                StandardPmfPlantRow("Nutrient"),
                StandardPmfPlantRow("Oats"),
                StandardPmfPlantRow("OilPalm"),
                StandardPmfPlantRow("Peanut"),
                StandardPmfPlantRow("Pinus"),
                StandardPmfPlantRow("Plantain"),
                StandardPmfPlantRow("Potato"),
                StandardPmfPlantRow("RedClover"),
                StandardPmfPlantRow("SCRUM"),
                StandardPmfPlantRow("Slurp"),
                ModelWithNoResourceRow("SoilArbitrator"),
                SoilWaterDocs(),
                CreateUnderReviewPlantRow("Sorghum"),
                StandardPmfPlantRow("Soybean"),
                SugarcaneRow(),
                ModelWithNoResourceRow("Stock"),
                StandardPmfPlantRow("Wheat"),
                StandardPmfPlantRow("WhiteClover"),
            };
            return new DocumentationTable(cols, rows);
        }

        private static IDocumentationRow LifecycleRow()
        {
            string apsimxFile = Path.Combine(examples, "Tutorials", "Lifecycle", "lifecycle.apsimx");
            string outFile = "lifecycle.pdf";
            return CustomDocsRow("Life cycle", new[] { apsimxFile }, outFile);
        }

        private static IDocumentationRow StandardTutorialRow(string rowName, string fileName)
        {
            string inputFile = Path.Combine(examples, "Tutorials", $"{fileName}.apsimx");
            IDocumentationFile file = new DocsFromFile(inputFile, $"{fileName}.pdf", options);
            IDocumentationCell cell = new DocumentationCell(file);
            return new DocumentationRow(rowName, cell.ToEnumerable());
        }

        private static IDocumentationRow ClemDocsRow()
        {
            string croppingFile = Path.Combine(examples, "CLEM_Example_Cropping.apsimx");
            string grazingFile = Path.Combine(examples, "CLEM_Example_Grazing.apsimx");
            IDocumentationFile scienceDocs = new ExternalDocument("Science Documentation", "https://www.apsim.info/clem");
            IDocumentationFile croppingExample = new DocsFromFile(croppingFile, "CLEM_Example_Cropping.pdf", options);
            IDocumentationFile grazingExample = new DocsFromFile(grazingFile, "CLEM_Example_Grazing.pdf", options);
            IEnumerable<IDocumentationCell> cells = new IDocumentationCell[3]
            {
                new DocumentationCell(new[] { scienceDocs }),
                new DocumentationCell(new[] { croppingExample }),
                new DocumentationCell(new[] { grazingExample })
            };
            return new DocumentationRow("CLEM", cells);
        }

        private static IDocumentationRow SugarcaneRow()
        {
            IDocumentationFile cane = new DocsFromModel<Sugarcane>("Sugarcane.pdf", options);
            IDocumentationCell cell = new DocumentationCell(new[] { cane });
            return new DocumentationRow("Sugarcane", new[] { cell });
        }

        private static IDocumentationRow SoilWaterDocs()
        {
            // This is almost like StandardPmfModelRow() except that the resource
            // file is WaterBalance.json rather than SoilWater.json.
            string modelName = "SoilWater";
            string model = Path.Combine(resources, "WaterBalance.json");
            string validationFile = Path.Combine(validation, modelName, $"{modelName}.apsimx");
            IEnumerable<string> inputs = new string[2] { model, validationFile };
            return CustomDocsRow("SoilWater", inputs, $"{modelName}.pdf");
        }

        private static IDocumentationRow ModelWithNoResourceRow(string modelName)
        {
            string validationFile = Path.Combine(validation, modelName, $"{modelName}.apsimx");
            IEnumerable<string> inputs = new string[1] { validationFile };
            return CustomDocsRow(modelName, inputs, $"{modelName}.pdf");
        }

        private static IDocumentationRow CreateUnderReviewPlantRow(string modelName)
        {
            string validationFile = Path.Combine(underReview, modelName, $"{modelName}.apsimx");
            string modelPath = $"[Replacements].{modelName}";
            IDocumentationFile file = new DocsFromModelPath(validationFile, modelPath, $"{modelName}.pdf", options, true);
            IDocumentationCell cell = new DocumentationCell(new[] { file });
            return new DocumentationRow(modelName, new[] { cell });
        }

        private static IDocumentationRow StandardPmfPlantRow(string modelName)
        {
            return StandardDocsRow($"{modelName}", $"{modelName}.json", $"{modelName}.apsimx", $"{modelName}.pdf");
        }

        private static IDocumentationRow StandardDocsRow(string name, string modelResourceFile, string validationFile, string outFile)
        {
            string model = Path.Combine(resources, modelResourceFile);
            string validation = Path.Combine(Program.validation, Path.GetFileNameWithoutExtension(validationFile), validationFile);
            IEnumerable<string> files = new string[2] { model, validation };

            string paramsFileName = $"{Path.GetFileNameWithoutExtension(outFile)}-parameters.pdf";

            IDocumentationFile autodoc = new DocsFromFile(files, outFile, options);
            IDocumentationFile param = new ParamsDocs(model, paramsFileName, options);

            IEnumerable<IDocumentationCell> cells = new[]
            {
                new DocumentationCell(autodoc),
                new DocumentationCell(param),
            };

            return new DocumentationRow(name, cells);
        }

        private static IDocumentationRow CustomDocsRow(string name, IEnumerable<string> inputs, string output)
        {
            return new DocumentationRow(name, new[]
            {
                new DocumentationCell(new []
                {
                    new DocsFromFile(inputs, output, options)
                })
            });
        }
    }
}
