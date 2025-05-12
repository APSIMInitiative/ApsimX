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
using System.Reflection;

namespace APSIM.Workflow
{

    /// <summary> Main FileSplitter class</summary>
    public class FileSplitter
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static void Run(string apsimFilepath, string? jsonFilepath, bool IsForWorkflow=false)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            string? directory = Path.GetDirectoryName(apsimFilepath) + "/";
            string? outFilepath = directory;
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
            Console.WriteLine("Output directory: " + outFilepath);
            string apsimDirectrory = PathUtilities.GetApsimXDirectory();

            //load in apsim file
            Simulations? simulations = FileFormat.ReadFromFile<Simulations>(apsimFilepath, e => throw e, false, false).NewModel as Simulations;

            Simulations template;
            if (simulations != null)
                template = MakeTemplateSims(simulations);
            else
                throw new Exception("File Could not be loaded.");

            //make new subfolder for weather files
            string weatherFilesDirectory = outFilepath + "WeatherFiles" + "/";
            Directory.CreateDirectory(weatherFilesDirectory);

            List<Experiment> experiments = simulations.FindAllDescendants<Experiment>().ToList();
            foreach(Experiment exp in experiments)
            {
                string newDirectory = outFilepath + exp.Name + "/";
                string filename = exp.Name + ".apsimx";
                string filepath = newDirectory + filename;

                Folder folder = GetFolderWithExperiments(new List<Experiment>(){exp});
                WriteSimulationsToFile(template, folder, filepath);

                Simulations? sims = FileFormat.ReadFromFile<Simulations>(filepath, e => throw e, false, false).NewModel as Simulations;
                if (sims != null)
                {
                    if (IsForWorkflow)
                    {
                        newDirectory = newDirectory.Trim('/');
                        PrepareWeatherFiles(sims, newDirectory);
                    }
                    else CopyWeatherFiles(sims, directory, weatherFilesDirectory);
                    // TODO: Input data needs to be copied to directories as well. They do not currently.
                    CopyObservedData(sims, folder, directory, newDirectory);
                }
                else
                {
                    throw new Exception(filepath + " could not be loaded.");
                }
            }

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

                string output = FileFormat.WriteToString(newFile);
                File.WriteAllText(filepath, output);
            }
        }

        /// <summary>
        /// Copy the weather files to the new directory. This is used for the workflow validation process.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="newDirectory"></param>
        /// <exception cref="Exception"></exception>
        private static void PrepareWeatherFiles(Model model, string newDirectory) 
        {
            foreach(Weather weather in model.FindAllDescendants<Weather>())
            {
                try
                {
                    // string azureWorkingDirectory = "/wd/";
                    Console.WriteLine("New directory: " + newDirectory);
                    string weatherFileName = Path.GetFileName(weather.FullFileName);
                    Console.WriteLine("Weather full file name: " + weatherFileName);
                    Console.WriteLine("Weather file name: " + weather.FileName);
                    string? newDirectoryName = Path.GetDirectoryName(newDirectory)!.Split(Path.DirectorySeparatorChar).LastOrDefault();
                    Console.WriteLine("New directory name: " + newDirectoryName);
                    // string originalFilePath = Directory.GetParent(newDirectory)!.ToString() + "/" + weather.FileName;
                    string originalFilePath = newDirectory + "/" + weather.FileName;
                    Console.WriteLine("Original file path: " + originalFilePath);
                    weather.FileName = weatherFileName;
                    Simulations parentSim = weather.FindAncestor<Simulations>();

                    if (parentSim != null)
                        parentSim.Write(parentSim.FileName);                  
                    string fullNewDir = "/" + newDirectory + "/" + weatherFileName;
                    Console.WriteLine("Full new directory: " + fullNewDir);
                    if (!File.Exists(fullNewDir))
                        File.Copy(originalFilePath, fullNewDir);
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

        private static void CopyObservedData(Model sims, Model folder, string oldDirectory, string newDirectory) 
        {
            List<string> simulationNames = GetListOfSimulationNames(sims);
            DataStore datastore = sims.FindDescendant<DataStore>();
            List<string> allSheetNames = new List<string>();
            List<List<string>> allColumnNames = new List<List<string>>();
            foreach(ExcelInput input in datastore.FindAllDescendants<ExcelInput>())
            {
                List<string> newSheetNames = new List<string>();
                List<string> newFilepaths = new List<string>();
                foreach(string path in input.FileNames.ToList())
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
                    foreach(string sheet in input.SheetNames.ToList())
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
                                
                            foreach(DataRow row in data.Rows)
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
                                    index = allSheetNames.Count-1;
                                }
                                else
                                {
                                    index = allSheetNames.IndexOf(sheet);
                                }

                                foreach(string columnName in newdata.GetColumnNames())
                                    if (!allColumnNames[index].Contains(columnName))
                                        allColumnNames[index].Add(columnName);
                                    
                                if (worksheetExists)
                                    wb.Worksheet(sheet).Delete();
                                wb.AddWorksheet(newdata, sheet);
                            }
                        }
                    }
                    if (wb.Worksheets.Count > 0)
                        wb.SaveAs(newDirectory + filename);

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
                    foreach(string name in simulationNames)
                        Console.WriteLine(name + " has no observed data");
                }
            }

            for(int i = 0; i < allSheetNames.Count; i++)
                RemoveUnusedPO(sims, folder, allSheetNames[i], allColumnNames[i]);
        }

        private static void RemoveUnusedPO(Model sims, Model folder, string observedSheet, List<string> observedColumns) 
        {
            List<PredictedObserved> poStats = sims.FindAllDescendants<PredictedObserved>().ToList();
            List<Report> reports = folder.FindAllDescendants<Report>().ToList();
            
            List<PredictedObserved> removeList = new List<PredictedObserved>();
            foreach (PredictedObserved po in poStats)
            {
                bool validColumns = true;
                if (!string.IsNullOrEmpty(po.FieldNameUsedForMatch) && !observedColumns.Contains(po.FieldNameUsedForMatch))
                    validColumns = false;
                if (!string.IsNullOrEmpty(po.FieldName2UsedForMatch) && !observedColumns.Contains(po.FieldName2UsedForMatch))
                    validColumns = false;
                if (!string.IsNullOrEmpty(po.FieldName3UsedForMatch) && !observedColumns.Contains(po.FieldName3UsedForMatch))
                    validColumns = false;
                if (!string.IsNullOrEmpty(po.FieldName4UsedForMatch) && !observedColumns.Contains(po.FieldName4UsedForMatch))
                    validColumns = false;

                if (observedSheet.CompareTo(po.ObservedTableName) != 0 || !validColumns)
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