using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Interfaces;
using Models.Storage;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models
{

    /// <summary>
    /// Encapsulates a factorial ANOVA parameter sensitivity analysis.
    /// </summary>
    [Serializable]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Folder))]
    public class FactorialAnova : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>
        /// List of analysis outputs
        /// </summary>
        [Description("Analysis outputs")]
        [Display(Type = DisplayType.MultiLineText)]
        public List<string> Outputs { get; set; }

        /// <summary>
        /// List of analysis inputs
        /// </summary>
        [Description("Analysis inputs")]
        [Display(Type = DisplayType.MultiLineText)]
        public List<string> Inputs { get; set; }

        /// <summary>
        /// This ID is used to identify temp files used by this Factorial ANOVA method.
        /// </summary>
        /// <remarks>
        /// Without this, analyses run in paralel could overwrite each other's
        /// temp files, as the temp files would have the same name.
        /// </remarks>
        [JsonIgnore]
        private readonly string id = Guid.NewGuid().ToString();

        /// <summary>Constructor</summary>
        public FactorialAnova()
        {
            Outputs = new List<string>();
            Inputs = new List<string>();
        }

        /// <summary>Main run method for performing our post simulation calculations</summary>
        public void Run()
        {
            // Note - we seem to be assuming that the predicted data table is called Report.
            // If the predicted table has not been modified during the most recent simulations run, don't do anything.
            if (dataStore?.Writer != null && !dataStore.Writer.TablesModified.Contains("Report"))
                return;

            string sql = "SELECT * FROM [Report]";
            DataTable predictedData = dataStore.Reader.GetDataUsingSql(sql);
            if (predictedData != null)
            {
                IndexedDataTable predictedDataIndexed = new IndexedDataTable(predictedData, null);

                string outputNames = StringUtilities.Build(Outputs, ",", "\"", "\"");
                string inputNames = StringUtilities.Build(Inputs, ",", "\"", "\"");
                string anovaVariableValuesFileName = GetTempFileName("anovaVariableValues", ".csv");

                // Write variables file
                using (var writer = new StreamWriter(anovaVariableValuesFileName))
                    DataTableUtilities.DataTableToText(predictedDataIndexed.ToTable(), 0, ",", true, writer, excelFriendly: true);

                string script = string.Format(
                     "inputs <- c({0})" + Environment.NewLine +
                     "inputs <- inputs[inputs != \"\"]" + Environment.NewLine +
                     "outputs <- c({1})" + Environment.NewLine +
                     "outputs <- outputs[outputs != \"\"]" + Environment.NewLine +
                     "factorial_data <- read.csv(\"{2}\")" + Environment.NewLine +
                     "indices <- data.frame(matrix(ncol = 4, nrow = 0))" + Environment.NewLine +
                     "colnames(indices) <- c(\"Input\", \"Output\", \"FirstOrder\", \"TotalOrder\")" + Environment.NewLine +
                     "for (output in outputs){{" + Environment.NewLine +
                     "  data <- factorial_data[, names(factorial_data) %in% inputs | names(factorial_data) == output]" + Environment.NewLine +
                     "  data[, names(data) %in% inputs] <- lapply(data[, names(data) %in% inputs], factor)" + Environment.NewLine +
                     "  output_mean <- mean(data[[output]])" + Environment.NewLine +
                     "  TSS <- sum((data[[output]] - output_mean)^2)" + Environment.NewLine +
                     "  anova_model <- aov(data[[output]] ~ (.)^1000, data = data[, names(data) %in% inputs])" + Environment.NewLine +
                     "  SSi <- summary(anova_model)[[1]][2]" + Environment.NewLine +
                     "  variance_contributions <- SSi / TSS" + Environment.NewLine +
                     "  parameter_names <- trimws(rownames(SSi), which = \"both\")" + Environment.NewLine +
                     "  all_results <- data.frame(parameter_names, variance_contributions, row.names = NULL)" + Environment.NewLine +
                     "  names(all_results) <- list(\"input\", \"% of variance\")  " + Environment.NewLine +
                     "  for (input in inputs){{" + Environment.NewLine +
                     "    first <- all_results[all_results$input == input, colnames(all_results) == \"% of variance\"]" + Environment.NewLine +
                     "    total <- sum(all_results[grepl(input, all_results$input), colnames(all_results) == \"% of variance\"])" + Environment.NewLine +
                     "    result <- data.frame(Input=c(input), Output=c(output), FirstOrder=c(first), TotalOrder=c(total))" + Environment.NewLine +
                     "    indices <- rbind(indices, result)" + Environment.NewLine +
                     "  }}" + Environment.NewLine +
                     "}}" + Environment.NewLine +
                     "write.table(indices, sep=\",\", row.names=FALSE)" + Environment.NewLine
                    ,
                    inputNames, outputNames, anovaVariableValuesFileName.Replace("\\", "/"));

                DataTable results = RunR(script);
                results.TableName = Name + "Statistics";
                dataStore.Writer.WriteTable(results);
            }
        }

        /// <summary>
        /// Runs the R script.
        /// </summary>
        private DataTable RunR(string script)
        {
            string rFileName = GetTempFileName("ANOVAscript", ".r");
            File.WriteAllText(rFileName, script);
            R r = new R();
            return r.RunToTable(rFileName);
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
    }
}