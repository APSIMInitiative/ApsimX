#nullable enable
using APSIM.Shared.Utilities;
using DeepCloner.Core;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Factorial;
using Models.PostSimulationTools;
using Models.Storage;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using ClosedXML.Excel;
using Models;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using APSIM.Core;

namespace APSIM.Workflow
{

    /// <summary> Main FileSplitter class</summary>
    public class FileSplitter
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static List<string> Run(string apsimFilepath, string? jsonFilepath, bool IsForWorkflow, ILogger logger)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            string? directory = Path.GetDirectoryName(apsimFilepath) + "/";
            string? outFilepath = directory;
            List<string> newSplitDirectories = new List<string>();
            List<SplittingGroup>? groups = null;
            if (!string.IsNullOrWhiteSpace(jsonFilepath))
            {
                string json = File.ReadAllText(jsonFilepath);
                SplittingRules? rules = JsonSerializer.Deserialize<SplittingRules>(json);
                if (rules != null) 
                {
                    outFilepath = rules.OutputPath;
                    if (outFilepath != null)
                        if (!outFilepath.EndsWith('/') && !outFilepath.EndsWith('\\'))
                            outFilepath += "/";
                    groups = rules.Groups;
                }
            }
            logger.LogInformation("Output directory: " + outFilepath);
            string apsimDirectrory = PathUtilities.GetApsimXDirectory();

            //load in apsim file
            Simulations? simulations = FileFormat.ReadFromFile<Simulations>(apsimFilepath).Model as Simulations;

            Simulations template;
            if (simulations != null)
                template = MakeTemplateSims(simulations);
            else
                throw new Exception("File Could not be loaded.");

            //make new subfolder for weather files
            string weatherFilesDirectory = outFilepath + "WeatherFiles" + "/";
            Directory.CreateDirectory(weatherFilesDirectory);

            List<Experiment> experiments = simulations.FindAllDescendants<Experiment>().Where(exp => exp.Enabled == true).ToList();
            foreach(Experiment exp in experiments)
            {
                string newDirectory = outFilepath + exp.Name + "/";
                string filename = exp.Name + ".apsimx";
                string filepath = newDirectory + filename;

                Folder folder = GetFolderWithExperiments(new List<Experiment>(){exp});
                WriteSimulationsToFile(template, folder, filepath);

                Simulations? sims = FileFormat.ReadFromFile<Simulations>(filepath).Model as Simulations;
                if (sims != null)
                {
                    if (IsForWorkflow)
                    {
                        newDirectory = newDirectory.Trim('/');
                        if (outFilepath != null)
                            PrepareWeatherFiles(sims, newDirectory, outFilepath);
                        else
                            throw new ArgumentNullException(nameof(outFilepath), "Output path cannot be null.");
                    }
                    else CopyWeatherFiles(sims, directory, weatherFilesDirectory);
                    string? oldInputFileDir = Directory.GetParent(directory)!.FullName;
                    CopyObservedData(sims, folder, oldInputFileDir!, newDirectory + "/", logger);
                    newSplitDirectories.Add("/" + newDirectory); 
                }
                else
                {
                    throw new Exception(filepath + " could not be loaded.");
                }
            }
            return newSplitDirectories;

            /*
            if (groups == null)
            {
                
            }
            else 
            {
                
                //for each group, make a file
                foreach(SplittingGroup group in groups)
                {
                    //get list of experiments that match the names in the group
                    List<Experiment> matchingExperiments = new List<Experiment>();
                    foreach(Experiment exp in experiments)
                        if (group.Experiments.Contains(exp.Name))
                            matchingExperiments.Add(exp);

                    //remove the experiments from the main list
                    foreach(Experiment exp in matchingExperiments)
                        experiments.Remove(exp);

                    //Write group to file
                    Folder folder = GetFolderWithExperiments(matchingExperiments);
                    WriteSimulationsToFile(template, folder, directory, group.Name, apsimDirectrory, outFilepath);
                }

                //any leftover experiments
                if (experiments.Count > 0)
                    throw new Exception("Leftover Experiments");
                    
            }
            */
        }

        private static Simulations MakeTemplateSims(Simulations sims) 
        {
            Simulations template = new Simulations();
            foreach(Model child in sims.Children)
            {
                bool keep = true;
                if ((child is Folder) && child.Name.CompareTo("Replacements") != 0)
                    keep = false;
                if ((child is Simulation) || (child is Experiment))
                    keep = false;
                if (keep)
                    template.Children.Add(child.DeepClone());
            }
            return template;
        }

        private static Folder GetFolderWithExperiments(List<Experiment> experiments)
        {
            Folder folder = new Folder();
            folder.Name = "Experiments";

            foreach(Experiment exp in experiments)
                folder.Children.Add(exp);

            return folder;
        }

        private static void WriteSimulationsToFile(Simulations template, Folder folder, string filepath)
        {
            if (folder.Children.Count > 0)
            {
                Simulations newFile = template.DeepClone();
                newFile.Children.Add(folder);
                newFile.ParentAllDescendants();

                string? newDirectory = Path.GetDirectoryName(filepath);
                if (newDirectory != null && !Directory.Exists(newDirectory))
                    Directory.CreateDirectory(newDirectory);
                
                Node newFileNode = Node.Create(newFile, null, false, filepath);
                string output = FileFormat.WriteToString(newFileNode);
                File.WriteAllText(filepath, output);
            }
        }

        /// <summary>
        /// Copy the weather files to the new directory. This is used for the workflow validation process.
        /// </summary>
        /// <param name="model">An apsim model to be searched for Weather models</param>
        /// <param name="newDirectory">Directory where the apsim met file will be copied</param>
        /// <param name="directory">The directory where the current file is located</param>
        /// <exception cref="Exception"></exception>
        private static void PrepareWeatherFiles(Model model, string newDirectory, string directory) 
        {
            foreach(Weather weather in model.FindAllDescendants<Weather>())
            {
                try
                {
                    // string azureWorkingDirectory = "/wd/";
                    string weatherFileName = Path.GetFileName(weather.FullFileName);
                    string? newDirectoryName = Path.GetDirectoryName(newDirectory)!.Split(Path.DirectorySeparatorChar).LastOrDefault();
                    string originalFilePath = directory + weather.FileName;
                    weather.FileName = weatherFileName;
                    Simulations parentSim = weather.FindAncestor<Simulations>();

                    if (parentSim != null)
                        parentSim.Write(parentSim.FileName);
                    string fullNewDir = "/" + newDirectory + "/" + weatherFileName; // github action version
                    // string fullNewDir = newDirectory + "/" + weatherFileName; // local url
                    if (!File.Exists(fullNewDir))
                    {
                        File.Copy(originalFilePath, fullNewDir);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Error copying weather file: " + weather.FileName + "\n" + e.Message);
                }
            }
        }


        private static void CopyWeatherFiles(Model model, string oldDirectory, string newDirectory) 
        {
            foreach(Weather weather in model.FindAllDescendants<Weather>())
            {
                weather.FileName = newDirectory + Path.GetFileName(weather.FileName);
                if (!weather.FileName.Contains("%root%"))
                    weather.FileName = newDirectory + Path.GetFileName(weather.FileName);
                else
                    weather.FileName = weather.FileName.Replace("%root%", oldDirectory);
            }
        }
        
        private static void FixInputPaths(Model model, string oldDirectory, string newDirectory) 
        {
            foreach(ExcelInput excelInput in model.FindAllDescendants<ExcelInput>())
            {
                List<string> newFilepaths = new List<string>();
                foreach(string file in excelInput.FileNames)
                {
                    if (!file.Contains("%root%"))
                        newFilepaths.Add(PathUtilities.GetAbsolutePath(file, newDirectory));
                    else
                        newFilepaths.Add(file.Replace("%root%", oldDirectory));
                }
                    
                excelInput.FileNames = newFilepaths.ToArray();
            }
            return;
        }

        private static List<string> GetListOfSimulationNames(Model sims)
        {
            List<string> simNames = new List<string>();
            foreach(Experiment exp in sims.FindAllDescendants<Experiment>())
                foreach(SimulationDescription sim in exp.GetSimulationDescriptions())
                    simNames.Add(sim.Name.ToLower().Trim());
            return simNames;
        }

        /// <summary>
        /// Copy the observed data from the original file to the new file.
        /// </summary>
        /// <param name="sims">An apsim model</param>
        /// <param name="folder">An apsim Folder model</param>
        /// <param name="oldDirectory">The old directory to copy from</param>
        /// <param name="newDirectory">New directory to copy to</param>
        /// <param name="logger">Logger for logging output.</param>
        /// <exception cref="Exception"></exception>
        public static void CopyObservedData(Model sims, Model folder, string oldDirectory, string newDirectory, ILogger logger) 
        {
            try
            {
                logger.LogInformation("Copying observed data from " + oldDirectory + " to " + newDirectory);
                List<string> simulationNames = GetListOfSimulationNames(sims);
                DataStore datastore = sims.FindDescendant<DataStore>();
                List<string> allSheetNames = new List<string>();
                List<List<string>> allColumnNames = new List<List<string>>();
                foreach (ExcelInput input in datastore.FindAllDescendants<ExcelInput>())
                {
                    List<string> newSheetNames = new List<string>();
                    List<string> newFilepaths = new List<string>();
                    foreach (string path in input.FileNames.ToList())
                    {
                        string filename = Path.GetFileName(path);
                        string filepath = PathUtilities.GetAbsolutePath(path, oldDirectory);
                        string outPath = newDirectory + filename;

                        XLWorkbook wb;
                        if (File.Exists(outPath))
                            wb = new XLWorkbook(outPath);
                        else
                            wb = new XLWorkbook();

                        bool hasData = false;
                        foreach (string sheet in input.SheetNames.ToList())
                        {
                            DataTable data = ExcelUtilities.ReadExcelFileData(filepath, sheet, true);
                            if (data != null)
                            {
                                DataTable newdata;
                                bool worksheetExists = wb.TryGetWorksheet(sheet, out IXLWorksheet s);
                                if (worksheetExists)
                                    newdata = ExcelUtilities.ReadExcelFileData(outPath, sheet, true);
                                else
                                    newdata = data.Clone();

                                foreach (DataRow row in data.Rows)
                                {
                                    string? simName = row["SimulationName"].ToString();
                                    if (simName != null)
                                        if (simulationNames.Contains(simName.ToLower().Trim()))
                                            newdata.ImportRow(row);
                                }

                                if (newdata.Rows.Count > 0)
                                {
                                    hasData = true;
                                    if (!newSheetNames.Contains(sheet))
                                        newSheetNames.Add(sheet);

                                    int index = 0;
                                    if (!allSheetNames.Contains(sheet))
                                    {
                                        allSheetNames.Add(sheet);
                                        allColumnNames.Add(new List<string>());
                                        index = allSheetNames.Count - 1;
                                    }
                                    else
                                    {
                                        index = allSheetNames.IndexOf(sheet);
                                    }

                                    foreach (string columnName in newdata.GetColumnNames())
                                        if (!allColumnNames[index].Contains(columnName))
                                            allColumnNames[index].Add(columnName);

                                    if (worksheetExists)
                                        wb.Worksheet(sheet).Delete();
                                    wb.AddWorksheet(newdata, sheet);
                                }
                            }
                        }
                        if (wb.Worksheets.Count > 0)
                        {
                            // Replace all Console.WriteLine with logger.LogInformation
                            wb.SaveAs("/" + newDirectory + filename);
                            logger.LogInformation("New input file " + filename + " saved to " + "/" + newDirectory);
                            logger.LogInformation("Files in " + "/" + newDirectory + " after saving new workbook:");
                            foreach (string file in Directory.GetFiles("/" + newDirectory))
                            {
                                logger.LogInformation("  " + Path.GetFileName(file));
                            }
                        }

                        //only add filename in if data was found for this experiment in it
                        if (hasData && !newFilepaths.Contains(filename))
                            newFilepaths.Add(filename);
                    }
                    if (newFilepaths.Count > 0)
                    {
                        input.FileNames = newFilepaths.ToArray();
                        input.SheetNames = newSheetNames.ToArray();
                    }
                    else
                    {
                        foreach (string name in simulationNames)
                            logger.LogInformation(name + " has no observed data");
                    }
                }

                RemoveUnusedPO(sims, folder, allSheetNames, allColumnNames);

                // Write the updated simulations back to the file
                (sims as Simulations)?.Write((sims as Simulations)?.FileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while copying observed data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RemoveUnusedPO(Model sims, Model folder, List<string> allSheetNames, List<List<string>> observedColumns) 
        {

            List<PredictedObserved> poStats = sims.FindAllDescendants<PredictedObserved>().ToList();
            List<Report> reports = folder.FindAllDescendants<Report>().ToList();      

            List<PredictedObserved> removeList = new List<PredictedObserved>();
            foreach (PredictedObserved po in poStats)
            {
                // bool validColumns = true;
                // if (!string.IsNullOrEmpty(po.FieldNameUsedForMatch) && !observedColumns.Contains(po.FieldNameUsedForMatch))
                //     validColumns = false;
                // if (!string.IsNullOrEmpty(po.FieldName2UsedForMatch) && !observedColumns.Contains(po.FieldName2UsedForMatch))
                //     validColumns = false;
                // if (!string.IsNullOrEmpty(po.FieldName3UsedForMatch) && !observedColumns.Contains(po.FieldName3UsedForMatch))
                //     validColumns = false;
                // if (!string.IsNullOrEmpty(po.FieldName4UsedForMatch) && !observedColumns.Contains(po.FieldName4UsedForMatch))
                //     validColumns = false;

                if (!allSheetNames.Contains(po.ObservedTableName))
                {
                    removeList.Add(po);
                }
                else
                {
                    bool hasReport = false;
                    foreach (Report report in reports)
                        if (report.Name.CompareTo(po.PredictedTableName) == 0)
                            hasReport = true;
                    if (!hasReport)
                        removeList.Add(po);
                }
                
            }

            foreach (PredictedObserved po in removeList)
            {
                Structure.Delete(po);
            }

            return;
        }
    }
}