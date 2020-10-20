using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Interfaces;
using Models.Storage;
using Models.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Models.Sensitivity;

namespace Models.Optimisation
{
    /// <summary>
    /// Enumeration of all suported optimization types.
    /// https://sticsrpacks.github.io/CroptimizR/articles/Available_parameter_estimation_algorithms.html
    /// </summary>
    public enum OptimizationTypeEnum
    {
        /// <summary>
        /// Nelder-Meade simplex method implemented in the nloptr package.
        /// https://sticsrpacks.github.io/CroptimizR/articles/Parameter_estimation_simple_case.html
        /// </summary>
        Simplex,

        /// <summary>
        /// DREAM-zs/Bayesian method implemented in the BayesianTools package.
        /// https://sticsrpacks.github.io/CroptimizR/articles/Parameter_estimation_DREAM.html
        /// </summary>
        Bayesian,
    }

    /// <summary>
    /// # [Name]
    /// Encapsulates CroptimizR: An R package for parameter estimation, uncertainty analysis and sensitivity analysis for Crop Models
    /// </summary>
    /// <remarks>
    /// https://github.com/SticsRPacks/CroptimizR
    /// https://github.com/hol430/ApsimOnR
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndTablePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class CroptimizR : Model, ICustomDocumentation, IModelAsTable, IRunnable, IReportsStatus
    {
        /// <summary>
        /// This ID is used to identify temp files used by this tool.
        /// </summary>
        /// <remarks>
        /// Without this, Morri run in paralel could overwrite each other's
        /// temp files, as the temp files would have the same name.
        /// </remarks>
        [JsonIgnore]
        private readonly string id = Guid.NewGuid().ToString();

        /// <summary>
        /// List of parameters
        /// </summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();

        /// <summary>
        /// Name of the predicted data table.
        /// </summary>
        [Description("Predicted table")]
        [Tooltip("Name of the predicted table in the datastore")]
        [Display(Type = DisplayType.TableName)]
        public string PredictedTableName { get; set; }

        /// <summary>
        /// Name of the observed data table.
        /// </summary>
        [Description("Observed table")]
        [Tooltip("Name of the observed table in the datastore")]
        [Display(Type = DisplayType.TableName)]
        public string ObservedTableName { get; set; }

        /// <summary>
        /// Variable to optimise.
        /// </summary>
        [Description("Variables to optimise")]
        [Tooltip("Can select multiple values, separated by commas")]
        //[Display(Type = DisplayType.FieldName)]
        public string[] VariableNames { get; set; }

        /// <summary>
        /// Directory to which output files (graphs, reports, ...) will be saved.
        /// </summary>
        [Description("Output path")]
        [Tooltip("Path to which output files (graphs, reports, ...) will be saved. If empty, output files will not be saved.")]
        [Display(Type = DisplayType.DirectoryName)]
        public string OutputPath { get; set; }

        /// <summary>
        /// Random seed to be used. Set to null for random results.
        /// </summary>
        [Description("Random seed (optional)")]
        [Tooltip("Optional random seed. Iff set, results will be the same for each execution. Leave empty for randomised results.")]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Optimization algorithm to be used. Changing this will change <see cref="OptimizationMethod"/>.
        /// </summary>
        /// <remarks>
        /// The reason we need both this enum and the IOptimizationMethod property are because
        /// we want to provide a drop-down in the gui of available optimization methods.
        /// </remarks>
        [Description("Optimization algorithm")]
        [JsonIgnore]
        public OptimizationTypeEnum OptimizationType
        {
            get
            {
                return OptimizationMethod.Type;
            }
            set
            {
                switch (value)
                {
                    case OptimizationTypeEnum.Bayesian:
                        OptimizationMethod = new DreamZs();
                        break;
                    case OptimizationTypeEnum.Simplex:
                        OptimizationMethod = new Simplex();
                        break;
                    default:
                        throw new NotImplementedException($"Unsuported optimization type: {value}");
                }
            }
        }

        /// <summary>
        /// Optimization method to be used.
        /// </summary>
        [Description("Optimization method")]
        [Display(Type = DisplayType.SubModel)]
        public IOptimizationMethod OptimizationMethod { get; set; } = new Simplex();

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        [JsonIgnore]
        public double Progress { get; private set; } = 0;

        /// <summary>
        /// Returns the job's status.
        /// </summary>
        [JsonIgnore]
        public string Status { get; private set; }

        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        [JsonIgnore]
        public List<DataTable> Tables
        {
            get
            {
                List<DataTable> tables = new List<DataTable>();

                // Add a parameter table
                DataTable table = new DataTable();
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Path", typeof(string));
                table.Columns.Add("LowerBound", typeof(double));
                table.Columns.Add("UpperBound", typeof(double));

                foreach (Parameter param in Parameters)
                {
                    DataRow row = table.NewRow();
                    row["Name"] = param.Name;
                    row["Path"] = param.Path;
                    row["LowerBound"] = param.LowerBound;
                    row["UpperBound"] = param.UpperBound;
                    table.Rows.Add(row);
                }
                tables.Add(table);

                return tables;
            }
            set
            {
                Parameters.Clear();
                foreach (DataRow row in value[0].Rows)
                {
                    Parameter param = new Parameter();
                    if (!Convert.IsDBNull(row["Name"]))
                        param.Name = row["Name"].ToString();
                    if (!Convert.IsDBNull(row["Path"]))
                        param.Path = row["Path"].ToString();
                    if (!Convert.IsDBNull(row["LowerBound"]))
                        param.LowerBound = Convert.ToDouble(row["LowerBound"], CultureInfo.InvariantCulture);
                    if (!Convert.IsDBNull(row["UpperBound"]))
                        param.UpperBound = Convert.ToDouble(row["UpperBound"], CultureInfo.InvariantCulture);
                    if (param.Name != null || param.Path != null)
                        Parameters.Add(param);
                }
            }
        }

        /// <summary>
        /// Invoked whenever the R process writes to stdout.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOutputReceivedFromR(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Contains("Working:"))
            {
                // Update progress.
                Match match = Regex.Match(e.Data, @"Working: ([^%]+)%");
                string progressString = match.Groups[1].Value;
                if (double.TryParse(progressString, NumberStyles.Float, CultureInfo.CurrentCulture, out double progress))
                    Progress = progress / 100; // The R script reports progress as percent
            }
        }

        /// <summary>
        /// Generates the R script which performs the optimization.
        /// </summary>
        /// <param name="fileName">File path to which the R code will be saved.</param>
        private void GenerateRScript(string fileName)
        {
            // tbi: package installation. Need to test on a clean VM.
            StringBuilder contents = new StringBuilder();
            string apsimxFileName = GenerateApsimXFile();

            contents.AppendLine($"variable_names <- c({string.Join(", ", VariableNames.Select(x => $"'{x.Trim()}'").ToArray())})");

            // If we're reading from the PredictedObserved table, need to fix
            // Predicted./Observed. suffix for the observed variables.
            string[] sanitisedObservedVariables = GetObservedVariableName().Select(x => $"'{x.Trim()}'").ToArray();
            string dateVariable = VariableNames.Any(v => v.StartsWith("Predicted.")) ? "Predicted.Clock.Today" : "Clock.Today";
            contents.AppendLine($"observed_variable_names <- c({string.Join(", ", sanitisedObservedVariables)}, '{dateVariable}')");
            contents.AppendLine($"apsimx_path <- '{typeof(IModel).Assembly.Location.Replace(@"\", @"\\")}'");
            contents.AppendLine($"apsimx_file <- '{apsimxFileName.Replace(@"\", @"\\")}'");
            contents.AppendLine($"simulation_names <- {GetSimulationNames()}");
            contents.AppendLine($"predicted_table_name <- '{PredictedTableName}'");
            contents.AppendLine($"observed_table_name <- '{ObservedTableName}'");
            contents.AppendLine($"param_info <- {GetParamInfo()}");
            contents.AppendLine();
            contents.AppendLine(OptimizationMethod.GenerateOptimizationOptions("optim_options"));
            if (!string.IsNullOrEmpty(OutputPath))
                contents.AppendLine($"optim_options$path_results <- '{OutputPath.Replace(@"\", @"\\")}'");
            if (RandomSeed != null)
                contents.AppendLine($"optim_options$ranseed <- {RandomSeed}");
            contents.AppendLine();
            contents.AppendLine($"crit_function <- {OptimizationMethod.CritFunction}");
            contents.AppendLine($"optim_method <- '{OptimizationMethod.ROptimizerName}'");
            contents.AppendLine();
            contents.Append(ReflectionUtilities.GetResourceAsString("Models.Resources.RScripts.OptimizR.r"));

            File.WriteAllText(fileName, contents.ToString());
        }

        private string[] GetObservedVariableName()
        {
            if (PredictedTableName == ObservedTableName)
                return VariableNames.Select(x => x.Replace("Predicted.", "Observed.")).ToArray();
            return VariableNames;
        }

        /// <summary>
        /// Generates an .apsimx file containing replacements model (if it
        /// exists), a datastore, and all children of this model. Saves the
        /// file to disk and returns the absolute path to the file.
        /// </summary>
        private string GenerateApsimXFile()
        {
            Simulations rootNode = FindAncestor<Simulations>();
            string apsimxFileName = GetTempFileName($"apsimx_file_{id}", ".apsimx");

            Simulations sims = new Simulations();
            sims.Children.AddRange(Children.Select(c => Apsim.Clone(c)));
            sims.Children.RemoveAll(c => c is IDataStore);

            IModel replacements = this.FindInScope<Replacements>();
            if (replacements != null && !sims.Children.Any(c => c is Replacements))
                sims.Children.Add(Apsim.Clone(replacements));

            // Search for IDataStore, not DataStore - to allow for StorageViaSockets.
            IDataStore storage = this.FindInScope<IDataStore>();
            IModel newDataStore = new DataStore();
            if (storage != null && storage is IModel m)
                newDataStore.Children.AddRange(m.Children.Select(c => Apsim.Clone(c)));

            sims.Children.Add(newDataStore);
            sims.ParentAllDescendants();

            sims.Write(apsimxFileName);

            string originalFile = rootNode?.FileName;
            if (string.IsNullOrEmpty(originalFile))
                originalFile = storage?.FileName;

            // Copy files across.
            foreach (IReferenceExternalFiles fileReference in sims.FindAllDescendants<IReferenceExternalFiles>().Cast<IReferenceExternalFiles>())
                foreach (string file in fileReference.GetReferencedFileNames())
                {
                    string absoluteFileName = PathUtilities.GetAbsolutePath(file, originalFile);
                    string fileName = Path.GetFileName(absoluteFileName);
                    string newPath = Path.GetDirectoryName(sims.FileName);
                    File.Copy(absoluteFileName, Path.Combine(newPath, fileName), true);
                }

            return apsimxFileName;
        }

        /// <summary>
        /// Write a message to the summary table.
        /// This is currently used to record console output generated by R.
        /// </summary>
        /// <param name="message">Message to be written.</param>
        private void WriteMessage(string message)
        {
            IDataStore storage = this.FindInScope<IDataStore>();
            if (storage == null)
                throw new ApsimXException(this, "No datastore is available!");

            string modelPath = this.FullPath;
            string relativeModelPath = modelPath.Replace(this.FullPath + ".", string.Empty);

            DataTable messages = new DataTable("_Messages");
            messages.Columns.Add("SimulationName", typeof(string));
            messages.Columns.Add("ComponentName", typeof(string));
            messages.Columns.Add("Date", typeof(DateTime));
            messages.Columns.Add("Message", typeof(string));
            messages.Columns.Add("MessageType", typeof(int));

            var newRow = messages.NewRow();
            newRow[0] = Name;
            newRow[1] = relativeModelPath;
            newRow[2] = DateTime.Now;
            newRow[3] = message;
            newRow[4] = Convert.ToInt32(Simulation.ErrorLevel.Information);
            messages.Rows.Add(newRow);

            storage.Writer.WriteTable(messages);
        }

        /// <summary>
        /// Generate an R named list containing the parameter bounds.
        /// </summary>
        /// <returns></returns>
        private string GetParamInfo()
        {
            string[] lower = Parameters.Select(p => $"{p.Path}={p.LowerBound}").ToArray();
            string[] upper = Parameters.Select(p => $"{p.Path}={p.UpperBound}").ToArray();
            string lowerBounds = string.Join(", ", lower);
            string upperBounds = string.Join(", ", upper);

            return $"list(lb=c({lowerBounds}), ub=c({upperBounds}))";
        }

        /// <summary>
        /// Return all simulation names generated by all descendant models as a
        /// comma-separated string.
        /// </summary>
        private string GetSimulationNames()
        {
            List<string> simulationNames = new List<string>();
            foreach (ISimulationDescriptionGenerator generator in this.FindAllDescendants<ISimulationDescriptionGenerator>())
                if (!(generator is Simulation sim && sim.Parent is ISimulationDescriptionGenerator))
                    simulationNames.AddRange(generator.GenerateSimulationDescriptions().Select(s => $"'{s.Name}'"));

            string csv = string.Join(", ", simulationNames);
            return $"c({csv})";
        }

        /// <summary>
        /// Returns a unique temporary filename.
        /// </summary>
        /// <param name="name">Base name of the file. The returned filename will contain this name.</param>
        /// <param name="extension">File extension to be used.</param>
        /// <returns>Unique temporary filename.</returns>
        private string GetTempFileName(string name, string extension)
        {
            return Path.ChangeExtension(Path.Combine(Path.GetTempPath(), name + id), extension);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                foreach (IModel child in Children)
                    if (!(child is Simulation) && !(child is Factors)) // why do we have this check?
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }

        /// <summary>
        /// Run the optimization (and wait for it to finish).
        /// </summary>
        /// <param name="cancelToken">Cancellation token.</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            Progress = 0;
            Status = "Installing R Packages";

            R r = new R(cancelToken.Token);
            r.InstallPackages("devtools", "dplyr", "nloptr", "DiceDesign", "RSQLite", "DBI", "cli");
            r.InstallFromGithub("hol430/ApsimOnR", "SticsRPacks/CroptimizR");

            Status = "Generating R Script";
            string fileName = GetTempFileName($"parameter_estimation_{id}", ".r");
            GenerateRScript(fileName);

            // todo - capture stderr as well?
            r.OutputReceived += OnOutputReceivedFromR;

            Status = "Running Parameter Optimization";
            string stdout = r.Run(fileName);
            r.OutputReceived -= OnOutputReceivedFromR;
            WriteMessage(stdout);
        }
    }
}