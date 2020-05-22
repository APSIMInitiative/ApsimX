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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Models.Sensitivity
{
    /// <summary>
    /// # [Name]
    /// Encapsulates CroptimizR: An R package for parameter estimation, uncertainty analysis and sensitivity analysis for Crop Models
    /// </summary>
    /// <remarks>
    /// https://github.com/SticsRPacks/CroptimizR
    /// https://github.com/hol430/ApsimOnR
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndTablePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Stics : Model, ICustomDocumentation, IModelAsTable
    {
        //[Link]
        //private IDataStore storage = null;
        //
        //[Link]
        //private Simulations sims = null;

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
        [Description("Name of the predicted data table")]
        [Display(Type = DisplayType.TableName)]
        public string PredictedTableName { get; set; }

        /// <summary>
        /// Name of the observed data table.
        /// </summary>
        [Description("Name of the observed data table")]
        [Display(Type = DisplayType.TableName)]
        public string ObservedTableName { get; set; }

        /// <summary>
        /// Variable to optimise.
        /// </summary>
        [Description("Variable to optimise")]
        [Display(Type = DisplayType.FieldName)]
        public string VariableName { get; set; }

        /// <summary>
        /// Number of times we run the minimisation with different parameters.
        /// </summary>
        [Description("Number of repetitions")]
        [Tooltip("Number of times the we run the minimsation with different parameters.")]
        public int NoRepetitions { get; set; } = 3;

        /// <summary>
        /// Tolerance criterion between two iterations.
        /// </summary>
        [Description("Tolerance criterion between two iterations")]
        [Tooltip("Iterations will cease if the objective variable is changing by less than this amount.")]
        public double Tolerance { get; set; } = 1e-5;

        /// <summary>
        /// Maximum number of iterations executed by the optimisation algorithm.
        /// </summary>
        [Description("Max number of iterations")]
        [Tooltip("Maximum number of iterations executed by the optimisation algorithm.")]
        public int MaxEval { get; set; } = 2;

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

        /// <summary>Gets a list of simulation descriptions.</summary>
        public void Run()
        {
            string fileName = GetTempFileName($"parameter_estimation_{id}", ".r");
            GenerateRScript(fileName);
            R r = new R();
            string stdout = r.Run(fileName);
            WriteMessage(stdout);
        }

        private void GenerateRScript(string fileName)
        {
            Simulations sims = Apsim.Find(this, typeof(Simulations)) as Simulations;

            // tbi: package installation. Need to test on a clean VM.
            R r = new R();
            r.InstallPackages("devtools");
            r.InstallPackages("ApsimOnR", "CroptimizR", "dplyr", "nloptr", "DiceDesign", "RSQLite", "DBI");
            StringBuilder contents = new StringBuilder();

            contents.AppendLine($"variable_names <- c('{VariableName}')");
            contents.AppendLine($"apsimx_path <- '{typeof(IModel).Assembly.Location.Replace(@"\", @"\\")}'");
            contents.AppendLine($"apsimx_file <- '{sims.FileName.Replace(@"\", @"\\")}'");//GetTempFileName($"parameter_estimation_{id}", ".apsimx");
            contents.AppendLine($"simulation_names <- {GetSimulationNames()}");
            contents.AppendLine($"predicted_table_name <- '{PredictedTableName}'");
            contents.AppendLine($"observed_table_name <- '{ObservedTableName}'");
            contents.AppendLine($"nb_rep <- {NoRepetitions}");
            contents.AppendLine($"xtol_rel <- {Tolerance}");
            contents.AppendLine($"maxeval <- {MaxEval}");
            contents.AppendLine($"param_info <- {GetParamInfo()}");
            contents.AppendLine();
            contents.Append(ReflectionUtilities.GetResourceAsString("Models.Resources.RScripts.OptimizR.r"));

            File.WriteAllText(fileName, contents.ToString());
        }

        private void WriteMessage(string message)
        {
            IDataStore storage = Apsim.Find(this, typeof(IDataStore)) as IDataStore;
            if (storage == null)
                throw new ApsimXException(this, "No datastore is available!");

            string modelPath = Apsim.FullPath(this);
            string relativeModelPath = modelPath.Replace(Apsim.FullPath(this) + ".", string.Empty);

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

        private string GetParamInfo()
        {
            //param_info=list(lb=c(.Simulations.Replacements.Wheat.Leaf.ExtinctionCoeff.VegetativePhase.FixedValue=0.4,
            //                           .Simulations.Replacements.Wheat.Leaf.Photosynthesis.RUE.FixedValue=1.4),
            //ub=c(.Simulations.Replacements.Wheat.Leaf.ExtinctionCoeff.VegetativePhase.FixedValue=0.6,
            //                           .Simulations.Replacements.Wheat.Leaf.Photosynthesis.RUE.FixedValue=1.6))

            string[] lower = Parameters.Select(p => $"{p.Path}={p.LowerBound}").ToArray();
            string[] upper = Parameters.Select(p => $"{p.Path}={p.UpperBound}").ToArray();
            string lowerBounds = string.Join(", ", lower);
            string upperBounds = string.Join(", ", upper);
            return $"list(lb=c({lowerBounds}), ub=c({upperBounds}))";
        }

        private string GetSimulationNames()
        {
            Simulations sims = Apsim.Find(this, typeof(Simulations)) as Simulations;
            List<string> simulationNames = new List<string>();
            foreach (ISimulationDescriptionGenerator generator in Apsim.ChildrenRecursively(sims, typeof(ISimulationDescriptionGenerator)))
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
    }
}