namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Models.Factorial;
    using Models.Interfaces;
    using Models.Sensitivity;
    using Models.Storage;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Utilities;

    /// <summary>
    /// # [Name]
    /// Encapsulates a Morris analysis.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.PropertyAndTablePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Folder))]
    public class Morris : Model, ISimulationDescriptionGenerator, ICustomDocumentation, IModelAsTable, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        private int _numPaths = 200;
        private int _numIntervals = 20;
        private int _jump = 10;
        private string _tableName = "Report";
        private string _aggregationVariableName = "Clock.Today.Year";

        /// <summary>A list of factors that we are to run</summary>
        private List<List<CompositeFactor>> allCombinations = new List<List<CompositeFactor>>();

        /// <summary>Parameter values coming back from R</summary>
        public DataTable ParameterValues { get; set; }

        /// <summary>The number of paths to run</summary>
        [Description("Number of paths:")]
        public int NumPaths
        {
            get { return _numPaths; }
            set { _numPaths = value; ParametersHaveChanged = true; }
        }

        /// <summary>The number of intervals</summary>
        [Description("Number of intervals:")]
        public int NumIntervals
        {
            get { return _numIntervals; }
            set { _numIntervals = value; ParametersHaveChanged = true; }
        }

        /// <summary>The jump parameter</summary>
        [Description("Jump:")]
        public int Jump
        {
            get { return _jump; }
            set { _jump = value; ParametersHaveChanged = true; }
        }

        /// <summary>Name of table in DataStore to read from.</summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        [Description("Name of table to read from:")]
        [Display(Type = DisplayType.TableName)]
        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; ParametersHaveChanged = true; }
        }
        /// <summary>The name of the variable to use to aggregiate each Morris analysis.</summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        [Description("Name of variable in table for aggregation:")]
        [Display(Type = DisplayType.FieldName)]
        public string AggregationVariableName
        {
            get { return _aggregationVariableName; }
            set { _aggregationVariableName = value; ParametersHaveChanged = true; }
        }

        /// <summary>
        /// List of parameters
        /// </summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        /// List of aggregation values
        /// </summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        public string[] AggregationValues { get; set; }

        /// <summary>
        /// This ID is used to identify temp files used by this Morris method.
        /// </summary>
        /// <remarks>
        /// Without this, Morri run in paralel could overwrite each other's
        /// temp files, as the temp files would have the same name.
        /// </remarks>
        [JsonIgnore]
        private readonly string id = Guid.NewGuid().ToString();

        /// <summary>Constructor</summary>
        public Morris()
        {
            Parameters = new List<Parameter>();
            allCombinations = new List<List<CompositeFactor>>();
        }

        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        [XmlIgnore]
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
                ParametersHaveChanged = true;
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

        /// <summary>Have the values of the parameters changed?</summary>
        public bool ParametersHaveChanged { get; set; }  = false;

        /// <summary>Gets a list of simulation descriptions.</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions()
        {
            var baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;

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

                // Add some descriptors
                int path = (simulationNumber - 1) / (Parameters.Count + 1) + 1;
                simDescription.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", simulationName));
                simDescription.Descriptors.Add(new SimulationDescription.Descriptor("Path", path.ToString()));

                // Apply each composite factor of this combination to our simulation description.
                combination.ForEach(c => c.ApplyToSimulation(simDescription));

                // Add simulation description to the return list of descriptions
                simulationDescriptions.Add(simDescription);

                simulationNumber++;
            }

            Console.WriteLine($"Simulation names generated by morris:\n{string.Join("\n", simulationDescriptions.Select(s => s.Name))}");
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
            r.InstallPackage("sensitivity");
            if (ParametersHaveChanged)
            {
                allCombinations.Clear();
                ParameterValues.Clear();
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
                    ParameterValues = RunRToGetParameterValues();
                if (ParameterValues == null || ParameterValues.Rows.Count == 0)
                    throw new Exception("The morris function in R returned null");

                int n = NumPaths * (Parameters.Count + 1);
                // Sometimes R will return an incorrect number of parameter values, usually
                // this happens when jump is too high. In this situation, we retry up to 10 times.
                for (int numTries = 1; numTries < 10 && ParameterValues.Rows.Count != n; numTries++)
                {
                    StringBuilder msg = new StringBuilder();
                    msg.AppendLine("Morris error: Number of parameter values from R is not equal to num paths * (N + 1).");
                    msg.AppendLine($"Number of parameters from R = {ParameterValues.Rows.Count}");
                    msg.AppendLine($"NumPaths={NumPaths}");
                    msg.AppendLine($"Parameters.Count={Parameters.Count}");
                    msg.AppendLine($"Trying again...");
                    Console.WriteLine(msg.ToString());

                    ParameterValues = RunRToGetParameterValues();
                }

                if (ParameterValues.Rows.Count != n)
                {
                    // We've tried and failed 10 times to generate the right number of parameter values.
                    // Time to give up and throw a fatal.
                    StringBuilder msg = new StringBuilder();
                    msg.AppendLine("Morris error: Number of parameter values from R is not equal to num paths * (N + 1).");
                    msg.AppendLine($"Number of parameters from R = {ParameterValues.Rows.Count}");
                    msg.AppendLine($"NumPaths={NumPaths}");
                    msg.AppendLine($"Parameters.Count={Parameters.Count}");
                    msg.AppendLine($"ParameterValues as returned from R:");
                    using (StringWriter writer = new StringWriter(msg))
                        DataTableUtilities.DataTableToText(ParameterValues, 0, ",", true, writer);
                    throw new Exception(msg.ToString());
                }

                int simulationNumber = 1;
                foreach (DataRow parameterRow in ParameterValues.Rows)
                {
                    var factors = new List<CompositeFactor>();
                    foreach (Parameter param in Parameters)
                    {
                        object value = Convert.ToDouble(parameterRow[param.Name], CultureInfo.InvariantCulture);
                        CompositeFactor f = new CompositeFactor(param.Name, param.Path, value);
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
                return Apsim.Child(this, typeof(Simulation)) as Simulation;
            }
        }

        /// <summary>Main run method for performing our post simulation calculations</summary>
        public void Run()
        {
            DataTable predictedData = dataStore.Reader.GetData(TableName, filter: "SimulationName LIKE '" + Name + "%'", orderBy: "SimulationID");
            if (predictedData != null)
            {

                // Determine how many aggregation values we have per simulation
                DataView view = new DataView(predictedData);
                view.RowFilter = "SimulationName='" + Name + "Simulation1'";
                AggregationValues = DataTableUtilities.GetColumnAsStrings(view, AggregationVariableName);

                // Create a table of all predicted values
                DataTable predictedValues = new DataTable();

                List<string> descriptiveColumnNames = new List<string>();
                List<string> variableNames = new List<string>();
                foreach (string aggregationValue in AggregationValues)
                {
                    view.RowFilter = AggregationVariableName + "=" + aggregationValue;

                    foreach (DataColumn predictedColumn in view.Table.Columns)
                    {
                        if (predictedColumn.DataType == typeof(double))
                        {
                            double[] values = DataTableUtilities.GetColumnAsDoubles(view, predictedColumn.ColumnName);
                            if (values.Distinct().Count() == 1)
                            {
                                if (!descriptiveColumnNames.Contains(predictedColumn.ColumnName))
                                    descriptiveColumnNames.Add(predictedColumn.ColumnName);
                            }
                            else
                            {
                                DataTableUtilities.AddColumn(predictedValues, predictedColumn.ColumnName + "_" + aggregationValue, values);
                                if (!variableNames.Contains(predictedColumn.ColumnName))
                                    variableNames.Add(predictedColumn.ColumnName);
                            }
                        }
                    }
                }

                // Run R
                DataTable eeDataRaw;
                DataTable statsDataRaw;
                RunRPostSimulation(predictedValues, out eeDataRaw, out statsDataRaw);

                // Get ee data from R and store in ee table.
                // EE data from R looks like:
                // "ResidueWt", "FASW", "CN2", "Cona", "variable","path"
                // - 22.971008269563,0.00950570342209862,-0.00379987333757356,56.7587080430652,"FallowEvaporation1996",1
                // - 25.790599484188, 0.0170777988614538, -0.0265991133629069,58.0240658644712,"FallowEvaporation1996",2
                // - 26.113599477728, 0.0113851992409871, 0.0113996200126667,57.9689677010766,"FallowEvaporation1996",3
                // - 33.284199334316, 0.0323193916349732, -0.334388853704853,60.5376820772641,"FallowEvaporation1996",4
                DataView eeView = new DataView(eeDataRaw);
                IndexedDataTable eeTableKey = new IndexedDataTable(new string[] { "Parameter", AggregationVariableName });

                // Create a path variable. 
                var pathValues = Enumerable.Range(1, NumPaths).ToArray();

                foreach (var parameter in Parameters)
                {
                    foreach (DataColumn column in predictedValues.Columns)
                    {
                        eeView.RowFilter = "variable = '" + column.ColumnName + "'";
                        if (eeView.Count != NumPaths)
                            throw new Exception("Found only " + eeView.Count + " paths for variable " + column.ColumnName + " in ee table");
                        string aggregationValue = StringUtilities.GetAfter(column.ColumnName, "_");
                        string variableName = StringUtilities.RemoveAfter(column.ColumnName, '_');

                        eeTableKey.SetIndex(new object[] { parameter.Name, aggregationValue });

                        List<double> values = DataTableUtilities.GetColumnAsDoubles(eeView, parameter.Name).ToList();
                        for (int i = 0; i < values.Count; i++)
                            values[i] = Math.Abs(values[i]);
                        var runningMean = MathUtilities.RunningAverage(values);

                        eeTableKey.SetValues("Path", pathValues);
                        eeTableKey.SetValues(variableName + ".MuStar", runningMean);
                    }
                }
                DataTable eeTable = eeTableKey.ToTable();
                eeTable.TableName = Name + "PathAnalysis";

                // Get stats data from R and store in MuStar table.
                // Stats data coming back from R looks like:
                // "mu", "mustar", "sigma", "param","variable"
                // -30.7331368183818, 30.7331368183818, 5.42917964248002,"ResidueWt","FallowEvaporation1996"
                // -0.0731299918470997,0.105740687296631,0.450848277601353, "FASW","FallowEvaporation1996"
                // -0.83061431285624,0.839772007599748, 1.75541097254145, "CN2","FallowEvaporation1996"
                // 62.6942591520838, 62.6942591520838, 5.22778043503867, "Cona","FallowEvaporation1996"
                // -17.286285468283, 19.4018404625051, 24.1361388348929,"ResidueWt","FallowRunoff1996"
                // 8.09850688306722, 8.09852589447407, 15.1988107373113, "FASW","FallowRunoff1996"
                // 18.6196168461051, 18.6196168461051, 15.1496277765849, "CN2","FallowRunoff1996"
                // -7.12794888887507, 7.12794888887507, 5.54014788597839, "Cona","FallowRunoff1996"
                IndexedDataTable tableKey = new IndexedDataTable(new string[2] { "Parameter", AggregationVariableName });

                foreach (DataRow row in statsDataRaw.Rows)
                {
                    string variable = row["variable"].ToString();
                    string aggregationValue = StringUtilities.GetAfter(variable, "_");
                    variable = StringUtilities.RemoveAfter(variable, '_');
                    tableKey.SetIndex(new object[] { row["param"], aggregationValue });

                    tableKey.Set(variable + ".Mu", row["mu"]);
                    tableKey.Set(variable + ".MuStar", row["mustar"]);
                    tableKey.Set(variable + ".Sigma", row["sigma"]);

                    // Need to bring in the descriptive values.
                    view.RowFilter = AggregationVariableName + "=" + aggregationValue;
                    foreach (var descriptiveColumnName in descriptiveColumnNames)
                    {
                        var values = DataTableUtilities.GetColumnAsStrings(view, descriptiveColumnName);
                        if (values.Distinct().Count() == 1)
                            tableKey.Set(descriptiveColumnName, view[0][descriptiveColumnName]);
                    }
                }
                DataTable muStarTable = tableKey.ToTable();
                muStarTable.TableName = Name + "Statistics";

                dataStore.Writer.WriteTable(eeTable);
                dataStore.Writer.WriteTable(muStarTable);
            }
        }

        /// <summary>
        /// Get a list of parameter values that we are to run. Call R to do this.
        /// </summary>
        private DataTable RunRToGetParameterValues()
        {
            string rFileName = Path.Combine(Path.GetTempPath(), "morrisscript" + id + ".r");
            string script = GetMorrisRScript();
            script += "write.table(apsimMorris$X, row.names = F, col.names = T, sep = \",\")" + Environment.NewLine;
            File.WriteAllText(rFileName, script);
            R r = new R();
            return r.RunToTable(rFileName);
        }

        /// <summary>
        /// Get a list of parameter values that we are to run. Call R to do this.
        /// </summary>
        private void RunRPostSimulation(DataTable predictedValues, out DataTable eeDataRaw, out DataTable statsDataRaw)
        {
            string morrisParametersFileName = GetTempFileName("parameters", ".csv");
            string apsimVariableFileName = GetTempFileName("apsimvariable", ".csv");
            string rFileName = GetTempFileName("morrisscript", ".r");
            string eeFileName = GetTempFileName("ee", ".csv");
            string statsFileName = GetTempFileName("stats", ".csv");

            // write predicted values file
            using (StreamWriter writer = new StreamWriter(apsimVariableFileName))
                DataTableUtilities.DataTableToText(predictedValues, 0, ",", true, writer);

            // Write parameters
            using (StreamWriter writer = new StreamWriter(morrisParametersFileName))
                DataTableUtilities.DataTableToText(ParameterValues, 0, ",", true, writer);

            string paramNames = StringUtilities.Build(Parameters.Select(p => p.Name), ",", "\"", "\"");
            string lowerBounds = StringUtilities.Build(Parameters.Select(p => p.LowerBound), ",");
            string upperBounds = StringUtilities.Build(Parameters.Select(p => p.UpperBound), ",");
            string script = GetMorrisRScript();
            script += string.Format
            ("apsimMorris$X <- read.csv(\"{0}\")" + Environment.NewLine +
            "values = read.csv(\"{1}\")" + Environment.NewLine +
            "allEE <- data.frame()" + Environment.NewLine +
            "allStats <- data.frame()" + Environment.NewLine +
            "for (columnName in colnames(values))" + Environment.NewLine +
            "{{" + Environment.NewLine +
            " apsimMorris$y <- values[[columnName]]" + Environment.NewLine +
            " tell(apsimMorris)" + Environment.NewLine +

            " ee <- data.frame(apsimMorris$ee)" + Environment.NewLine +
            " ee$variable <- columnName" + Environment.NewLine +
            " ee$path <- c(1:{2})" + Environment.NewLine +
            " allEE <- rbind(allEE, ee)" + Environment.NewLine +

            " mu <- apply(apsimMorris$ee, 2, mean)" + Environment.NewLine +
            " mustar <- apply(apsimMorris$ee, 2, function(x) mean(abs(x)))" + Environment.NewLine +
            " sigma <- apply(apsimMorris$ee, 2, sd)" + Environment.NewLine +
            " stats <- data.frame(mu, mustar, sigma)" + Environment.NewLine +
            " stats$param <- params" + Environment.NewLine +
            " stats$variable <- columnName" + Environment.NewLine +
            " allStats <- rbind(allStats, stats)" + Environment.NewLine +

            "}}" + Environment.NewLine +
            "write.csv(allEE,\"{3}\", row.names=FALSE)" + Environment.NewLine +
            "write.csv(allStats, \"{4}\", row.names=FALSE)" + Environment.NewLine,
            morrisParametersFileName.Replace("\\", "/"),
            apsimVariableFileName.Replace("\\", "/"),
            NumPaths,
            eeFileName.Replace("\\", "/"),
            statsFileName.Replace("\\", "/"));
            File.WriteAllText(rFileName, script);

            // Run R
            R r = new R();
            r.RunToTable(rFileName);

            eeDataRaw = ApsimTextFile.ToTable(eeFileName);
            statsDataRaw = ApsimTextFile.ToTable(statsFileName);
        }

        /// <summary>
        /// Return the base R script for running morris.
        /// </summary>
        private string GetMorrisRScript()
        {
            string paramNames = StringUtilities.Build(Parameters.Select(p => p.Name), ",", "\"", "\"");
            string lowerBounds = StringUtilities.Build(Parameters.Select(p => p.LowerBound), ",");
            string upperBounds = StringUtilities.Build(Parameters.Select(p => p.UpperBound), ",");
            string script = string.Format
            ($".libPaths(c('{R.PackagesDirectory}', .libPaths()))" + Environment.NewLine +
            $"library('sensitivity', lib.loc = '{R.PackagesDirectory}')" + Environment.NewLine +
            "params <- c({0})" + Environment.NewLine +
            "apsimMorris<-morris(model=NULL" + Environment.NewLine +
            " ,params #string vector of parameter names" + Environment.NewLine +
            " ,{1} #no of paths within the total parameter space" + Environment.NewLine +
            " ,design=list(type=\"oat\",levels={2},grid.jump={3})" + Environment.NewLine +
            " ,binf=c({4}) #min for each parameter" + Environment.NewLine +
            " ,bsup=c({5}) #max for each parameter" + Environment.NewLine +
            " ,scale=T" + Environment.NewLine +
            " )" + Environment.NewLine,
            paramNames, NumPaths, NumIntervals + 1, Jump, lowerBounds, upperBounds);
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
                {
                    if (!(child is Simulation) && !(child is Factors))
                        AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
                }
            }
        }
    }
}