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
using ClosedXML.Excel;
using Models;

namespace APSIM.Workflow
{
    public class SplittingGroup
    {
        public string? Name { get; set; }
        public List<string>? Experiments { get; set; }
        public List<string>? Simulations { get; set; }
    }
    public class SplittingRules
    {
        public List<SplittingGroup>? Groups { get; set; }
    }

    public class FileSplitter
    {
        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static int Run()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string jsonFilepath = null;//"C:/git/APSIM.FileSplitter/chickpea.json";
            string apsimDirectrory = "C:/git/ApsimX/";
            string apsimFilepath = "C:/git/ApsimX/Tests/Validation/Wheat/Wheat.apsimx";
            string outFilepath = "C:/git/APSIM.FileSplitter/Export/";

            //load in apsim file
            Simulations? simulations = FileFormat.ReadFromFile<Simulations>(apsimFilepath, e => throw e, false, false).NewModel as Simulations;
            string directory = Path.GetDirectoryName(apsimFilepath);

            Simulations template = MakeTemplateSims(simulations);

            List<Experiment> experiments = simulations.FindAllDescendants<Experiment>().ToList();
            if (string.IsNullOrEmpty(jsonFilepath))
            {
                foreach(Experiment exp in experiments)
                {
                    Folder folder = GetFolderWithExperiments(new List<Experiment>(){exp});
                    WriteSimulationsToFile(template, folder, directory, exp.Name, apsimDirectrory, outFilepath);
                }
            }
            else 
            {
                //load in splitting json
                string json = File.ReadAllText(jsonFilepath);
                SplittingRules? groups = JsonSerializer.Deserialize<SplittingRules>(json);

                //for each group, make a file
                foreach(SplittingGroup group in groups.Groups)
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

            return 0;
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

        private static void FixPaths(Model model, string directory, string originalDirectory) 
        {
            foreach(Weather weather in model.FindAllDescendants<Weather>())
            {
                if (!weather.FileName.Contains("%root%"))
                    weather.FileName = PathUtilities.GetAbsolutePath(weather.FileName, originalDirectory);
                else
                    weather.FileName = weather.FileName.Replace("%root%", originalDirectory);
            }

            foreach(ExcelInput excelInput in model.FindAllDescendants<ExcelInput>())
            {
                List<string> newFilepaths = new List<string>();
                foreach(string file in excelInput.FileNames)
                {
                    if (!file.Contains("%root%"))

                        newFilepaths.Add(PathUtilities.GetAbsolutePath(file, directory));
                    else
                        newFilepaths.Add(file.Replace("%root%", originalDirectory));
                }
                    
                excelInput.FileNames = newFilepaths.ToArray();
            }
            return;
        }

        private static Folder GetFolderWithExperiments(List<Experiment> experiments)
        {
            Folder folder = new Folder();
            folder.Name = "Experiments";

            foreach(Experiment exp in experiments)
                folder.Children.Add(exp);

            return folder;
        }

        private static void WriteSimulationsToFile(Simulations template, Folder folder, string directory, string filename, string originalDirectory, string exportDirectory)
        {
            if (folder.Children.Count > 0)
            {
                Simulations newFile = template.DeepClone();
                newFile.Children.Add(folder);
                newFile.ParentAllDescendants();

                FixPaths(newFile, directory, originalDirectory);
                
                DataStore datastore = newFile.FindChild<DataStore>();
                List<string> simNames = new List<string>();
                foreach(Experiment exp in newFile.FindAllDescendants<Experiment>())
                    foreach(SimulationDescription sim in exp.GetSimulationDescriptions())
                        simNames.Add(sim.Name.ToLower().Trim());

                string newDirectory = exportDirectory + filename + "/";
                Directory.CreateDirectory(newDirectory);
                GetExcelRows(newFile, folder, simNames, originalDirectory, newDirectory);

                string output = FileFormat.WriteToString(newFile);
                File.WriteAllText(newDirectory + filename + ".apsimx", output);
            }
        }

        private static void GetExcelRows(Model sims, Model folder, List<string> simulationNames, string directory, string exportDirectory) 
        {
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
                    string filepath = PathUtilities.GetAbsolutePath(path, directory);
                    string outPath = exportDirectory + filename;

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
                        wb.SaveAs(exportDirectory + filename);

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