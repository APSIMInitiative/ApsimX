using System;
using System.Collections.Generic;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using System.Diagnostics;
using Models;
using Models.PMF.Phen;
using APSIM.Core;

namespace APSIM.Documentation
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                    return GenerateAllDocuments();
                else if (args.Length == 3)
                    return GenerateDocument(args[0], args[1], bool.Parse(args[2]));
                else
                {
                    Console.Error.WriteLine("Invalid number of arguments. Either provide no arguments to generate all documents, or provide two arguments: <input file path> <output folder path>.");
                    return 1;
                }
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err);
                return 1;
            }
        }

        private static int GenerateAllDocuments()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string folderPath = PathUtilities.GetAbsolutePath("%root%/Tests/Validation/", null);
            string apsimPath = PathUtilities.GetAbsolutePath("%root%", null);
            string outputPath = apsimPath + "/_autodocs/";
            string[] directories = Directory.GetDirectories(folderPath);
            List<string> names = new List<string>();
            foreach (string directory in directories)
            {
                int pos = directory.Replace("\\", "/").LastIndexOf("/");
                names.Add(directory.Substring(pos + 1));
            }

            names.Add("SorghumDCaPST");
            names.Add("ClimateController");
            names.Add("Lifecycle");
            names.Add("Manager");
            names.Add("Sensitivity_MorrisMethod");
            names.Add("Sensitivity_SobolMethod");
            names.Add("Sensitivity_FactorialANOVA");
            names.Add("PredictedObserved");
            names.Add("Report");
            names.Add("CLEM_Example_Cropping");
            names.Add("CLEM_Example_Grazing");

            names.Remove("AgPasture");
            names.Remove("CLEM");
            names.Remove("DCaPST");
            names.Remove("NDVI");
            names.Remove("System");
            names.Remove("Clock");

            foreach (string name in names)
            {
                string html = WebDocs.GetPage(apsimPath, name);
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
                File.WriteAllText(outputPath + name + ".html", html);
            }

            List<IModel> models = new List<IModel>() { new Clock(), new ZadokPMFWheat() };
            foreach (IModel model in models)
            {
                string html = WebDocs.GenerateWeb(model);
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
                File.WriteAllText(outputPath + model.Name + ".html", html);
            }

            Console.WriteLine($"Successfully generated files at {outputPath}. Elapsed time: {stopwatch.Elapsed.TotalSeconds} seconds.");

            return 0;
        }

        /// <summary>Generate documentation for a single file.</summary>
        /// <param name="path">The absolute path to the file to document.</param>
        /// <param name="outputPath">The output path where the generated file should be placed.</param>
        /// <param name="generateGraphs">Set whether graphs should be generated.</param>
        static int GenerateDocument(string path, string outputPath, bool generateGraphs = false)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                if (generateGraphs)
                    DocumentationSettings.GenerateGraphs = true;
                Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
                Node.Create(sims, fileName: path);
                if (sims == null)
                    throw new Exception("The file " + path + " does not contain a Simulations model at the root.");
                string html = WebDocs.Generate(sims);
                if (html != null)
                {
                    if (!Directory.Exists(outputPath))
                        Directory.CreateDirectory(outputPath);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    File.WriteAllText(Path.Combine(outputPath, fileName + ".html"), html);
                }
                Console.WriteLine($"Successfully generated file at {outputPath}. Elapsed time: {stopwatch.Elapsed.TotalSeconds} seconds.");
                return 0;
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err);
                return 1;
            }
        }
    }
}
