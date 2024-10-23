using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Interfaces;
using Models.Sensitivity;
using Models.Storage;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models
{

    /// <summary>
    /// Encapsulates a SOBOL parameter sensitivity analysis.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyAndGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndGridPresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Folder))]
    public class Sobol : Model, ISimulationDescriptionGenerator, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>A list of factors that we are to run</summary>
        private List<List<CompositeFactor>> allCombinations = new List<List<CompositeFactor>>();

        private int _numPaths = 1000;

        /// <summary>Parameter values coming back from R</summary>
        [JsonIgnore]
        public DataTable ParameterValues { get; set; }

        /// <summary>X1 values coming back from R</summary>
        [JsonIgnore]
        public DataTable X1 { get; set; }

        /// <summary>X2 values coming back from R</summary>
        [JsonIgnore]
        public DataTable X2 { get; set; }

        /// <summary>The number of paths to run</summary>
        [Description("Number of paths:")]
        public int NumPaths
        {
            get { return _numPaths; }
            set { _numPaths = value; ParametersHaveChanged = true; }
        }

        /// <summary>Name of the table containing predicted data.</summary>
        [Description("Table name")]
        [Tooltip("Name of the table containing predicted data")]
        [Display(Type = DisplayType.TableName)]
        public string TableName { get; set; }

        /// <summary>The name of the variable to use to aggregiate each Morris analysis.</summary>
        [Description("Name of variable in table for aggregation")]
        [Display(Type = DisplayType.FieldName)]
        public string AggregationVariableName { get; set; }

        /// <summary>
        /// List of parameters
        /// </summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        [Display]
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        /// This ID is used to identify temp files used by this Sobol model.
        /// </summary>
        /// <remarks>
        /// Without this, Sobols run in paralel could overwrite each other's
        /// temp files, as the temp files would have the same name.
        /// </remarks>
        [JsonIgnore]
        private readonly string id = Guid.NewGuid().ToString();

        /// <summary>Constructor</summary>
        public Sobol()
        {
            Parameters = new List<Parameter>();
            allCombinations = new List<List<CompositeFactor>>();
        }

        /// <summary>Have the values of the parameters changed?</summary>
        public bool ParametersHaveChanged { get; set; } = false;

        /// <summary>Gets a list of simulation descriptions.</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions()
        {
            var baseSimulation = this.FindChild<Simulation>();

            // Calculate all combinations.
            CalculateFactors();

            // Loop through all combinations and add a simulation description to the
            // list of simulations descriptions being returned to the caller.
            var simulationDescriptions = new List<SimulationDescription>();
            int simulationNumber = 1;
            foreach (var combination in allCombinations)
            {
                // Create a simulation.
                var simulationName = Name + "Simulation" + simulationNumber;
                var simDescription = new SimulationDescription(baseSimulation, simulationName);
                simDescription.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", simulationName));

                // Apply each composite factor of this combination to our simulation description.
                combination.ForEach(c => c.ApplyToSimulation(simDescription));

                // Add simulation description to the return list of descriptions
                simulationDescriptions.Add(simDescription);

                simulationNumber++;
            }

            return simulationDescriptions;
        }

        /// <summary>
        /// Invoked when a run is beginning.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments.</param>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun(object sender, EventArgs e)
        {
            R r = new R();
            r.InstallPackage("boot");
            r.InstallPackage("sensitivity");
            if (ParametersHaveChanged)
            {
                allCombinations?.Clear();
                ParameterValues?.Clear();
            }
        }

        /// <summary>
        /// Calculate factors that we need to run. Put combinations into allCombinations
        /// </summary>
        private void CalculateFactors()
        {
            if (allCombinations.Count == 0)
            {
                if (ParameterValues == null || ParameterValues.Rows.Count == 0)
                {
                    // Write a script to get random numbers from R.
                    string script = string.Format
                        ($".libPaths(c('{R.PackagesDirectory}', .libPaths()))" + Environment.NewLine +
                        $"library('boot')" + Environment.NewLine +
                         $"library('sensitivity')" + Environment.NewLine +
                         "n <- {0}" + Environment.NewLine +
                         "nparams <- {1}" + Environment.NewLine +
                         "X1 <- data.frame(matrix(nr = n, nc = nparams))" + Environment.NewLine +
                         "X2 <- data.frame(matrix(nr = n, nc = nparams))" + Environment.NewLine
                         ,
                        NumPaths, Parameters.Count);

                    for (int i = 0; i < Parameters.Count; i++)
                    {
                        script += string.Format("X1[, {0}] <- {1}+runif(n)*{2}" + Environment.NewLine +
                                                "X2[, {0}] <- {1}+runif(n)*{2}" + Environment.NewLine
                                                ,
                                                i + 1, Parameters[i].LowerBound,
                                                Parameters[i].UpperBound - Parameters[i].LowerBound);
                    }

                    string sobolx1FileName = GetTempFileName("sobolx1", ".csv");
                    string sobolx2FileName = GetTempFileName("sobolx2", ".csv");

                    script += string.Format("write.table(X1, \"{0}\",sep=\",\",row.names=FALSE)" + Environment.NewLine +
                                            "write.table(X2, \"{1}\",sep=\",\",row.names=FALSE)" + Environment.NewLine +
                                            "sa <- sobolSalt(model = NULL, X1, X2, scheme=\"A\", nboot = 100)" + Environment.NewLine +
                                            "write.csv(sa$X,row.names=FALSE)" + Environment.NewLine,
                                            sobolx1FileName.Replace("\\", "/"),
                                            sobolx2FileName.Replace("\\", "/"));

                    // Run the script
                    ParameterValues = RunR(script);

                    // Read in the 2 data frames (X1, X2) that R wrote.
                    if (!File.Exists(sobolx1FileName))
                    {
                        string rFileName = GetTempFileName("sobolscript", ".r");
                        if (!File.Exists(rFileName))
                            throw new Exception("Cannot find file: " + rFileName);
                        string message = "Cannot find : " + sobolx1FileName + Environment.NewLine +
                                     "Script:" + Environment.NewLine +
                                     File.ReadAllText(rFileName);
                        throw new Exception(message);
                    }
                    X1 = ApsimTextFile.ToTable(sobolx1FileName);
                    X2 = ApsimTextFile.ToTable(sobolx2FileName);
                }

                int simulationNumber = 1;
                foreach (DataRow parameterRow in ParameterValues.Rows)
                {
                    var factors = new List<CompositeFactor>();
                    for (int p = 0; p < Parameters.Count; p++)
                    {
                        object value = Convert.ToDouble(parameterRow[p], CultureInfo.InvariantCulture);
                        var f = new CompositeFactor(Parameters[p].Name, Parameters[p].Path, value);
                        factors.Add(f);
                    }

                    string newSimulationName = Name + "Simulation" + simulationNumber;
                    allCombinations.Add(factors);
                    simulationNumber++;
                }
            }
        }

        /// <summary>
        /// Gets the base simulation
        /// </summary>
        public Simulation BaseSimulation
        {
            get
            {
                return this.FindChild<Simulation>();
            }
        }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (dataStore?.Writer != null && !dataStore.Writer.TablesModified.Contains(TableName))
                return;

            DataTable predictedData = dataStore.Reader.GetData(TableName);
            if (predictedData != null)
            {
                IndexedDataTable variableValues = new IndexedDataTable(null);

                // Determine how many aggregation values we have per simulation
                DataView view = new DataView(predictedData);
                view.RowFilter = "SimulationName='" + Name + "Simulation1'";
                //var aggregationValues = DataTableUtilities.GetColumnAsIntegers(view, AggregationVariableName);
                object[] aggregationValues = view.ToTable()
                                                 .AsEnumerable()
                                                 .Select(r => r[AggregationVariableName])
                                                 .Select(r => r == DBNull.Value ? null : r)
                                                 .ToArray();
                if (aggregationValues.FirstOrDefault()?.GetType() == typeof(DateTime))
                    aggregationValues = aggregationValues.Select(d => ((DateTime)d).ToString("yyyy-MM-dd")).ToArray();

                // Create a results table.
                IndexedDataTable results;
                if (aggregationValues.Count() > 1)
                    results = new IndexedDataTable(new string[] { AggregationVariableName });
                else
                    results = new IndexedDataTable(null);


                // Loop through all aggregation values and perform analysis on each.
                List<string> errorsFromR = new List<string>();
                foreach (object value in aggregationValues)
                {
                    if (value.GetType() == typeof(string))
                        view.RowFilter = $"{AggregationVariableName}='{value}'";
                    else
                        view.RowFilter = $"{AggregationVariableName}={value}";

                    foreach (DataColumn predictedColumn in predictedData.Columns)
                    {
                        if (predictedColumn.DataType == typeof(double))
                        {
                            var values = DataTableUtilities.GetColumnAsDoubles(view, predictedColumn.ColumnName);
                            if (values.Distinct().Count() > 1)
                                variableValues.SetValues(predictedColumn.ColumnName, values);
                        }
                    }

                    string paramNames = StringUtilities.Build(Parameters.Select(p => p.Name), ",", "\"", "\"");
                    string sobolx1FileName = GetTempFileName("sobolx1", ".csv");
                    string sobolx2FileName = GetTempFileName("sobolx2", ".csv");
                    string sobolVariableValuesFileName = GetTempFileName("sobolvariableValues", ".csv");

                    // Write variables file
                    using (var writer = new StreamWriter(sobolVariableValuesFileName))
                        DataTableUtilities.DataTableToText(variableValues.ToTable(), 0, ",", true, writer, excelFriendly: false, decimalFormatString: "F6");

                    // Write X1
                    using (var writer = new StreamWriter(sobolx1FileName))
                        DataTableUtilities.DataTableToText(X1, 0, ",", true, writer, excelFriendly: false, decimalFormatString: "F6");

                    // Write X2
                    using (var writer = new StreamWriter(sobolx2FileName))
                        DataTableUtilities.DataTableToText(X2, 0, ",", true, writer, excelFriendly: false, decimalFormatString: "F6");

                    string script = string.Format(
                         $".libPaths(c('{R.PackagesDirectory}', .libPaths()))" + Environment.NewLine +
                         $"library('boot')" + Environment.NewLine +
                         $"library('sensitivity')" + Environment.NewLine +
                         "params <- c({0})" + Environment.NewLine +
                         "n <- {1}" + Environment.NewLine +
                         "nparams <- {2}" + Environment.NewLine +
                         "X1 <- read.csv(\"{3}\")" + Environment.NewLine +
                         "X2 <- read.csv(\"{4}\")" + Environment.NewLine +
                         "sa <- sobolSalt(model = NULL, X1, X2, scheme=\"A\", nboot = 100)" + Environment.NewLine +
                         "variableValues = read.csv(\"{5}\")" + Environment.NewLine +
                         "for (columnName in colnames(variableValues))" + Environment.NewLine +
                         "{{" + Environment.NewLine +
                         "  sa$y <- variableValues[[columnName]]" + Environment.NewLine +
                         "  tell(sa)" + Environment.NewLine +
                         "  sa$S$Parameter <- params" + Environment.NewLine +
                         "  sa$T$Parameter <- params" + Environment.NewLine +
                         "  sa$S$ColumnName <- columnName" + Environment.NewLine +
                         "  sa$T$ColumnName <- columnName" + Environment.NewLine +
                         "  sa$S$Indices <- \"FirstOrder\"" + Environment.NewLine +
                         "  sa$T$Indices <- \"Total\"" + Environment.NewLine +
                         "  if (!exists(\"allData\"))" + Environment.NewLine +
                         "    allData <- rbind(sa$S, sa$T)" + Environment.NewLine +
                         "  else" + Environment.NewLine +
                         "    allData <- rbind(allData, sa$S, sa$T)" + Environment.NewLine +
                         "}}" + Environment.NewLine +
                         "write.table(allData, sep=\",\", row.names=FALSE)" + Environment.NewLine
                        ,
                        paramNames, NumPaths, Parameters.Count,
                        sobolx1FileName.Replace("\\", "/"),
                        sobolx1FileName.Replace("\\", "/"),
                        sobolVariableValuesFileName.Replace("\\", "/"));

                    DataTable resultsForValue = null;
                    try
                    {
                        resultsForValue = RunR(script);

                        // Put output from R into results table.
                        if (aggregationValues.Count() > 1)
                            results.SetIndex(new object[] { value.ToString() });

                        foreach (DataColumn col in resultsForValue.Columns)
                        {
                            if (col.DataType == typeof(string))
                                results.SetValues(col.ColumnName, DataTableUtilities.GetColumnAsStrings(resultsForValue, col.ColumnName, CultureInfo.InvariantCulture));
                            else
                                // Someone needs to test this on a non-Australian locale.
                                // Does R print out numbers using the system locale, or
                                // an international one? If the system locale, change this
                                // to CultureInfo.CurrentCulture.
                                results.SetValues(col.ColumnName, DataTableUtilities.GetColumnAsDoubles(resultsForValue, col.ColumnName, CultureInfo.InvariantCulture));
                        }
                    }
                    catch (Exception err)
                    {
                        string msg = err.Message;

                        if (aggregationValues.Count() > 1)
                            msg = $"{AggregationVariableName} {value}: {msg}";
                        errorsFromR.Add(msg);
                    }
                }
                var resultsRawTable = results.ToTable();
                resultsRawTable.TableName = Name + "Statistics";
                dataStore.Writer.WriteTable(resultsRawTable);

                if (errorsFromR.Count > 0)
                {
                    string msg = StringUtilities.BuildString(errorsFromR.ToArray(), Environment.NewLine);
                    throw new Exception(msg);
                }
            }
        }

        /// <summary>
        /// Get a list of parameter values that we are to run. Call R to do this.
        /// </summary>
        private DataTable RunR(string script)
        {
            string rFileName = GetTempFileName("sobolscript", ".r");
            File.WriteAllText(rFileName, script);
            R r = new R();

            string result = r.Run(rFileName);
            string tempFile = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), "csv");
            if (!File.Exists(tempFile))
                File.Create(tempFile).Close();

            using (var reader = new StringReader(result))
            using (var writer = new StreamWriter(tempFile))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    if (!line.StartsWith("[")) // detect R error.
                        writer.WriteLine(line);
                    line = reader.ReadLine();
                }
            }

            DataTable table = null;
            try
            {
                table = ApsimTextFile.ToTable(tempFile);
            }
            catch (Exception)
            {
                throw new Exception(File.ReadAllText(tempFile));
            }
            finally
            {
                Thread.Sleep(200);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            return table;
        }

        /// <summary>
        /// Return the base R script for running morris.
        /// </summary>
        private string GetSobolRScript()
        {
            string script = string.Format
                ($".libPaths(c('{R.PackagesDirectory}', .libPaths()))" + Environment.NewLine +
                 $"library('boot')" + Environment.NewLine +
                 $"library('sensitivity')" + Environment.NewLine +
                 "n <- {0}" + Environment.NewLine +
                 "nparams <- {1}" + Environment.NewLine +
                 "X1 <- data.frame(matrix(nr = n, nc = nparams))" + Environment.NewLine +
                 "X2 <- data.frame(matrix(nr = n, nc = nparams))" + Environment.NewLine
                 ,
                NumPaths, Parameters.Count);

            for (int i = 0; i < Parameters.Count; i++)
            {
                script += string.Format("X1[, {0}] <- {1}+runif(n)*{2}" + Environment.NewLine +
                                        "X2[, {0}] <- {1}+runif(n)*{2}" + Environment.NewLine
                                        ,
                                        i + 1, Parameters[i].LowerBound,
                                        Parameters[i].UpperBound - Parameters[i].LowerBound);
            }

            string sobolx1FileName = GetTempFileName("sobolx1", ".csv");
            string sobolx2FileName = GetTempFileName("sobolx2", ".csv");

            script += string.Format("write.csv(X1, \"{0}\")" + Environment.NewLine +
                                    "write.csv(X2, \"{1}\")" + Environment.NewLine
                                    ,
                                    sobolx1FileName.Replace("\\", "/"),
                                    sobolx2FileName.Replace("\\", "/"));

            //script += "sa <- sobolSalt(model = NULL, X1, X2, scheme=\"A\", nboot = 100)" + Environment.NewLine;
            return script;
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