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
using APSIM.Shared.Documentation.Extensions;
using Models.PreSimulationTools;

namespace APSIM.Workflow
{

    /// <summary> Main FileSplitter class</summary>
    public class FileSplitter
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static List<string> Run(string apsimFilepath, string? jsonFilepath, string outputPath, ILogger logger)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string inputPath = Path.GetDirectoryName(apsimFilepath) + "/";

            if (inputPath == null)
                throw new ArgumentNullException(nameof(inputPath), "Current directory path cannot be null.");

            if (outputPath == null)
                throw new ArgumentNullException(nameof(outputPath), "Output path cannot be null.");

            List<SplittingGroup>? groups = null;
            bool copyWeatherFiles = false;
            bool copyObservedData = false;
            if (!string.IsNullOrWhiteSpace(jsonFilepath))
            {
                string json = File.ReadAllText(jsonFilepath);
                SplittingRules? rules = JsonSerializer.Deserialize<SplittingRules>(json);
                if (rules != null && rules.OutputPath != null)
                {
                    outputPath = rules.OutputPath;
                    if (outputPath != null)
                        if (!outputPath.EndsWith('/') && !outputPath.EndsWith('\\'))
                            outputPath += "/";
                    groups = rules.Groups;
                    if (rules.CopyWeatherFiles != null)
                        copyWeatherFiles = (bool)rules.CopyWeatherFiles;
                    if (rules.CopyObservedData != null)
                        copyObservedData = (bool)rules.CopyObservedData;
                }
            }

            logger.LogInformation("Output directory: " + outputPath);
            string apsimDirectrory = PathUtilities.GetApsimXDirectory();

            //load in apsim file
            Simulations? sims = FileFormat.ReadFromFile<Simulations>(apsimFilepath).Model as Simulations;

            if (sims == null)
                throw new Exception("File Could not be loaded.");

            List<Simulation> simulations = sims.Node.FindChildren<Simulation>(recurse: true).Where(exp => exp.Enabled == true && exp.Parent.GetType() != typeof(Experiment)).ToList();
            List<Experiment> experiments = sims.Node.FindChildren<Experiment>(recurse: true).Where(sim => sim.Enabled == true).ToList();
            List<PredictedObserved> predictedObserveds = sims.Node.FindChildren<PredictedObserved>(recurse: true).Where(po => po.Enabled == true).ToList();

            if (experiments.Count() == 0)
                throw new Exception("No Experiments found.");

            List<string> newSplitDirectories = new List<string>();
            if (groups == null)
            {
                foreach (Experiment exp in experiments)
                {
                    string subFolder = inputPath + outputPath + exp.Name + "/";
                    string filename = exp.Name + ".apsimx";
                    string weatherFilesDirectory = subFolder + "WeatherFiles" + "/";
                    string fullFilePath = subFolder + filename;

                    Folder folder = GetFolderWithExperiments(new List<Experiment>() { exp }, new List<Simulation>(), new List<PredictedObserved>());

                    Simulations copiedSims = GetSimulations(sims, folder, subFolder + filename);
                    copiedSims.FileName = sims.FileName;
                    copiedSims.ResetSimulationFileNames();

                    Directory.CreateDirectory(weatherFilesDirectory);
                    CopyWeatherFiles(copiedSims, weatherFilesDirectory);
                    CopyObservedData(copiedSims, folder, inputPath, subFolder, logger);

                    copiedSims.FileName = fullFilePath;
                    copiedSims.ResetSimulationFileNames();

                    copiedSims.Write(fullFilePath);

                    newSplitDirectories.Add(subFolder);
                }
            }
            else
            {
                //for each group, make a file
                foreach (SplittingGroup group in groups)
                {
                    if (group == null)
                        throw new Exception("Group is Null");

                    if (group.Folders == null)
                        group.Folders = new List<string>();

                    if (group.Experiments == null)
                        group.Experiments = new List<string>();

                    if (group.Simulations == null)
                        group.Simulations = new List<string>();

                    //get list of experiments that match the names in the group
                    List<Experiment> matchingExperiments = new List<Experiment>();
                    foreach (Experiment exp in experiments)
                    {
                        bool found = false;
                        if (group.Folders.Count() == 0)
                            found = true;
                        else
                            foreach (string name in group.Folders)
                                if (exp.Node.FindParents<Folder>(name).Count() > 0)
                                    found = true;

                        if (found)
                        {
                            if (group.Experiments.Count() == 0)
                                matchingExperiments.Add(exp);
                            else
                                foreach (string name in group.Experiments)
                                    if (exp.Name.Contains(name))
                                        matchingExperiments.Add(exp);
                        }
                    }

                    //remove the experiments from the main list
                    foreach (Experiment exp in matchingExperiments)
                        experiments.Remove(exp);

                    List<Simulation> matchingSimulations = new List<Simulation>();
                    foreach (Simulation sim in simulations)
                    {
                        bool found = false;
                        if (group.Folders.Count() == 0)
                            found = true;
                        else
                            foreach (string name in group.Folders)
                                if (sim.Node.FindParents<Folder>(name).Count() > 0)
                                    found = true;

                        if (found)
                        {
                            if (group.Simulations.Count() == 0)
                                matchingSimulations.Add(sim);
                            else
                                foreach (string name in group.Simulations)
                                    if (sim.Name.Contains(name))
                                        matchingSimulations.Add(sim);
                        }
                    }

                    foreach (Simulation sim in matchingSimulations)
                        simulations.Remove(sim);

                    //get list of po that are in the folders
                    List<PredictedObserved> matchingPOs = new List<PredictedObserved>();
                    foreach (PredictedObserved po in predictedObserveds)
                    {
                        foreach (string name in group.Folders)
                            if (po.Node.FindParents<Folder>(name).Count() > 0)
                                matchingPOs.Add(po);
                    }

                    //remove the experiments from the main list
                    foreach (PredictedObserved po in matchingPOs)
                        predictedObserveds.Remove(po);

                    string subFolder = inputPath + outputPath + group.Name + "/";
                    string filename = group.Name + ".apsimx";
                    string fullFilePath = subFolder + filename;

                    //Write group to file
                    Folder folder = GetFolderWithExperiments(matchingExperiments, matchingSimulations, matchingPOs);

                    Simulations copiedSims = GetSimulations(sims, folder, subFolder + filename);
                    copiedSims.FileName = sims.FileName;
                    copiedSims.ResetSimulationFileNames();

                    if (copyWeatherFiles)
                    {
                        string weatherFilesDirectory = subFolder + "WeatherFiles" + "/";
                        Directory.CreateDirectory(weatherFilesDirectory);
                        CopyWeatherFiles(copiedSims, weatherFilesDirectory);
                    }

                    if (copyObservedData)
                    {
                        string dataFilesDirectory = subFolder + "Data" + "/";
                        Directory.CreateDirectory(dataFilesDirectory);
                        CopyObservedData(copiedSims, folder, inputPath, dataFilesDirectory, logger);
                    }
                    else
                    {
                        List<string> allSheetNames = new List<string>();
                        foreach (ExcelInput input in copiedSims.Node.FindAll<ExcelInput>())
                            foreach (string sheet in input.SheetNames)
                                if (!allSheetNames.Contains(sheet))
                                    allSheetNames.Add(sheet);

                        foreach (ObservedInput input in copiedSims.Node.FindAll<ObservedInput>())
                            foreach (string sheet in input.SheetNames)
                                if (!allSheetNames.Contains(sheet))
                                    allSheetNames.Add(sheet);
                        RemoveUnusedPO(copiedSims, allSheetNames);


                    }

                    copiedSims.FileName = fullFilePath;
                    copiedSims.ResetSimulationFileNames();

                    copiedSims.Write(fullFilePath);
                    logger.LogInformation("  created:" + fullFilePath);

                    newSplitDirectories.Add(subFolder);
                }

                //any leftover experiments
                if (experiments.Count > 0)
                    throw new Exception("Leftover Experiments");

            }

            return newSplitDirectories;
        }

        private static Simulations MakeTemplateSims(Simulations sims)
        {
            Simulations template = new Simulations();
            foreach (Model child in sims.Children)
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

        private static Folder GetFolderWithExperiments(List<Experiment> experiments, List<Simulation> simulations, List<PredictedObserved> predictedObserveds)
        {
            Folder folder = new Folder();
            folder.Name = "Experiments";

            foreach(Experiment exp in experiments)
                folder.Children.Add(exp);

            foreach(Simulation sim in simulations)
                folder.Children.Add(sim);
            
            foreach(PredictedObserved po in predictedObserveds)
                folder.Children.Add(po);

            return folder;
        }

        private static Simulations GetSimulations(Simulations template, Folder folder, string filepath)
        {
            if (folder.Children.Count > 0)
            {
                Simulations newFile = MakeTemplateSims(template);
                newFile.Children.Add(folder);

                string? newDirectory = Path.GetDirectoryName(filepath);
                if (newDirectory != null && !Directory.Exists(newDirectory))
                    Directory.CreateDirectory(newDirectory);

                Node newFileNode = Node.Create(newFile, null, false, filepath);

                return newFile;
            }
            else
            {
                throw new Exception("Simulation has no children.");
            }
        }

        /// <summary>
        /// Copy the weather files to the new directory. This is used for the workflow validation process.
        /// </summary>
        /// <param name="model">An apsim model to be searched for Weather models</param>
        /// <param name="newDirectory">Directory where the apsim met file will be copied</param>
        /// <exception cref="Exception"></exception>
        private static void CopyWeatherFiles(Model model, string newDirectory)
        {
            foreach(Weather weather in model.Node.FindChildren<Weather>(recurse: true))
            {
                string fullpath = weather.FullFileName;
                try
                {
                    string weatherFileName = Path.GetFileName(fullpath);
                    string newFilepath = newDirectory + weatherFileName;

                    if (!File.Exists(newFilepath))
                        if (File.Exists(fullpath))
                            File.Copy(weather.FullFileName, newFilepath);

                    weather.FileName = newFilepath;
                }
                catch (Exception e)
                {
                    throw new Exception("Error copying weather file: " + fullpath + "\n" + e.Message);
                }
            }
        }

        private static void FixInputPaths(Model model, string oldDirectory, string newDirectory)
        {
            foreach(ExcelInput excelInput in model.Node.FindChildren<ExcelInput>(recurse: true))
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
            foreach(Experiment exp in sims.Node.FindChildren<Experiment>(recurse: true))
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
                DataStore datastore = sims.Node.FindChild<DataStore>(recurse: true);
                List<string> allSheetNames = new List<string>();
                List<List<string>> allColumnNames = new List<List<string>>();
                foreach (ExcelInput input in datastore.Node.FindChildren<ExcelInput>(recurse: true))
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
                            wb.SaveAs(newDirectory + filename);
                            logger.LogInformation("New input file " + filename + " saved to " + newDirectory);
                            logger.LogInformation("Files in " + newDirectory + " after saving new workbook:");
                            foreach (string file in Directory.GetFiles(newDirectory))
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

                RemoveUnusedPO(sims, allSheetNames);

                // Write the updated simulations back to the file
                (sims as Simulations)?.Write((sims as Simulations)?.FileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while copying observed data: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void RemoveUnusedPO(Model sims, List<string> allSheetNames)
        {

            List<PredictedObserved> poStats = sims.Node.FindChildren<PredictedObserved>(recurse: true).ToList();
            List<Report> reports = sims.Node.FindChildren<Report>(recurse: true).ToList();

            List<PredictedObserved> removeList = new List<PredictedObserved>();
            foreach (PredictedObserved po in poStats)
            {
                if (!allSheetNames.Contains(po.ObservedTableName))
                {
                    removeList.Add(po);
                }
                else
                {
                    bool hasReport = false;
                    foreach (Report report in reports)
                        if (report.Name == po.PredictedTableName && Folder.IsUnderReplacementsFolder(report) == null)
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