using System;
using System.Collections.Generic;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using System.Diagnostics;
using Models;
using Models.PMF.Phen;

namespace APSIM.Documentation
{
    class Program
    {
        static int Main(string[] args)
        {
            try
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
                    names.Add(directory.Substring(pos+1));
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

                List<IModel> models = new List<IModel>() {new Clock(), new ZadokPMFWheat()};
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
            catch (Exception err)
            {
                Console.Error.WriteLine(err);
                return 1;
            }
        }
    }
}
