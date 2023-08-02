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
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.TablePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Folder))]
    public class FactorialAnova : Model, IModelAsTable, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>
        /// List of analysis outputs
        /// </summary>
        public List<string> Outputs { get; set; }

        /// <summary>
        /// List of analysis inputs
        /// </summary>
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

        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        [JsonIgnore]
        public List<DataTable> Tables
        {
            get
            {
                List<DataTable> tables = new List<DataTable>();

                // Add an inputs table
                DataTable inputTable = new DataTable();
                inputTable.Columns.Add("Inputs", typeof(string));

                foreach (string input in Inputs)
                {
                    DataRow rowIn = inputTable.NewRow();
                    rowIn["Inputs"] = input;
                    inputTable.Rows.Add(rowIn);
                }

                tables.Add(inputTable);

                // Add an outputs table.
                DataTable outputTable = new DataTable();
                outputTable.Columns.Add("Outputs", typeof(string));

                foreach (string output in Outputs)
                {
                    DataRow rowOut = outputTable.NewRow();
                    rowOut["Outputs"] = output;
                    outputTable.Rows.Add(rowOut);
                }

                tables.Add(outputTable);

                return tables;
            }
            set
            {

                Inputs.Clear();
                Outputs.Clear();
                foreach (DataRow row in value[1].Rows)
                {
                    string output = null;
                    if (!Convert.IsDBNull(row["Outputs"]))
                        output = row["Outputs"].ToString();
                    if (output != null)
                        Outputs.Add(output);
                }

                foreach (DataRow row in value[0].Rows)
                {
                    string input = null;
                    if (!Convert.IsDBNull(row["Inputs"]))
                        input = row["Inputs"].ToString();
                    if (input != null)
                        Inputs.Add(input);
                }
            }
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