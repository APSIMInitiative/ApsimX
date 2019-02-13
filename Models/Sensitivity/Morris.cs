namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Factorial;
    using Models.Interfaces;
    using Models.Sensitivity;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;
    using Utilities;

    /// <summary>
    /// # [Name]
    /// Encapsulates a Morris analysis.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.DualGridView")]
    [PresenterName("UserInterface.Presenters.TablePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Folder))]
    public class Morris : Model, ISimulationGenerator, ICustomDocumentation, IModelAsTable, IPostSimulationTool
    {
        /// <summary>A list of factors that we are to run</summary>
        private List<List<FactorValue>> allCombinations = new List<List<FactorValue>>();

        /// <summary>A number of the currently running sim</summary>
        private int simulationNumber;

        /// <summary>Used to track whether this particular Morris has been run.</summary>
        private bool hasRun = false;

        /// <summary>Parameter values coming back from R</summary>
        public DataTable ParameterValues { get; set; }

        /// <summary>The number of paths to run</summary>
        public int NumPaths { get; set; } = 200;

        /// <summary>The number of intervals</summary>
        public int NumIntervals { get; set; } = 20;

        /// <summary>The jump parameter</summary>
        public int Jump { get; set; } = 10;

        /// <summary>
        /// List of parameters
        /// </summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        /// List of years
        /// </summary>
        /// <remarks>
        /// Needs to be public so that it gets written to .apsimx file
        /// </remarks>
        public int[] Years { get; set; }

        /// <summary>List of simulation names from last run</summary>
        [XmlIgnore]
        public List<string> simulationNames { get; set; }

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
            allCombinations = new List<List<FactorValue>>();
            simulationNames = new List<string>();
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

                // Add a constant table.
                DataTable constant = new DataTable();
                constant.Columns.Add("Property", typeof(string));
                constant.Columns.Add("Value", typeof(int));
                DataRow constantRow = constant.NewRow();
                constantRow["Property"] = "Number of paths:";
                constantRow["Value"] = NumPaths;
                constant.Rows.Add(constantRow);

                constantRow = constant.NewRow();
                constantRow["Property"] = "Number of intervals:";
                constantRow["Value"] = NumIntervals;
                constant.Rows.Add(constantRow);

                constantRow = constant.NewRow();
                constantRow["Property"] = "Jump:";
                constantRow["Value"] = Jump;
                constant.Rows.Add(constantRow);

                tables.Add(constant);

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
                NumPaths = Convert.ToInt32(value[0].Rows[0][1]);
                NumIntervals = Convert.ToInt32(value[0].Rows[1][1]);
                Jump = Convert.ToInt32(value[0].Rows[2][1]);

                Parameters.Clear();
                foreach (DataRow row in value[1].Rows)
                {
                    Parameter param = new Parameter();
                    if (!Convert.IsDBNull(row["Name"]))
                        param.Name = row["Name"].ToString();
                    if (!Convert.IsDBNull(row["Path"]))
                        param.Path = row["Path"].ToString();
                    if (!Convert.IsDBNull(row["LowerBound"]))
                        param.LowerBound = Convert.ToDouble(row["LowerBound"]);
                    if (!Convert.IsDBNull(row["UpperBound"]))
                        param.UpperBound = Convert.ToDouble(row["UpperBound"]);
                    if (param.Name != null || param.Path != null)
                        Parameters.Add(param);
                }
            }
        }

        private Stream serialisedBase;
        private Simulations parentSimulations;

        /// <summary>Simulation runs are about to begin.</summary>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun()
        {
            if (Enabled)
            {
                Initialise();
                simulationNumber = 1;
            }
        }

        /// <summary>Gets the next job to run</summary>
        public Simulation NextSimulationToRun(bool fullFactorial = true)
        {
            hasRun = true;
            if (allCombinations.Count == 0)
                return null;

            var combination = allCombinations[0];
            allCombinations.RemoveAt(0);

            Simulation newSimulation = Apsim.DeserialiseFromStream(serialisedBase) as Simulation;
            newSimulation.Name = Name + "Simulation" + simulationNumber;
            newSimulation.Parent = null;
            newSimulation.FileName = parentSimulations.FileName;
            Apsim.ParentAllChildren(newSimulation);

            // Make substitutions.
            parentSimulations.MakeSubsAndLoad(newSimulation);

            foreach (FactorValue value in combination)
                value.ApplyToSimulation(newSimulation);

            PushFactorsToReportModels(newSimulation, combination);

            simulationNumber++;
            return newSimulation;
        }

        /// <summary>Find all report models and give them the factor values.</summary>
        /// <param name="factorValues">The factor values to send to each report model.</param>
        /// <param name="simulation">The simulation to search for report models.</param>
        private void PushFactorsToReportModels(Simulation simulation, List<FactorValue> factorValues)
        {
            List<string> names = new List<string>();
            List<string> values = new List<string>();
            names.Add("SimulationName");
            values.Add(simulation.Name);

            // Add path to report files
            int path = (simulationNumber - 1) / (Parameters.Count + 1) + 1;
            names.Add("Path");
            values.Add(path.ToString());

            foreach (FactorValue factor in factorValues)
            {
                names.Add(factor.Name);
                values.Add(factor.Values[0].ToString());
            }

            foreach (Report.Report report in Apsim.ChildrenRecursively(simulation, typeof(Report.Report)))
            {
                report.ExperimentFactorNames = names;
                report.ExperimentFactorValues = values;
            }
        }

        /// <summary>
        /// Generates an .apsimx file for each simulation in the experiment and returns an error message (if it fails).
        /// </summary>
        /// <param name="path">Full path including filename and extension.</param>
        /// <returns>Empty string if successful, error message if it fails.</returns>
        public void GenerateApsimXFile(string path)
        {
            Simulation sim = NextSimulationToRun();
            while (sim != null)
            {
                Simulations sims = Simulations.Create(new List<IModel> { sim, new Models.Storage.DataStore() });

                string st = FileFormat.WriteToString(sims);
                File.WriteAllText(Path.Combine(path, sim.Name + ".apsimx"), st);
                sim = NextSimulationToRun();
            }
        }

        /// <summary>Gets a list of simulation names</summary>
        public IEnumerable<string> GetSimulationNames(bool fullFactorial = true)
        {
            return simulationNames;
        }

        /// <summary>Gets a list of factors</summary>
        public List<ISimulationGeneratorFactors> GetFactors()
        {
            string[] columnNames = new string[] { "Parameter", "Year" };
            string[] columnValues = new string[2];

            var factors = new List<ISimulationGeneratorFactors>();
            foreach (Parameter param in Parameters)
            {
                foreach (var year in Years)
                {
                    factors.Add(new SimulationGeneratorFactors(columnNames, new string[] { param.Name, year.ToString() },
                    "ParameterxYear", param.Name + year));
                    factors.Add(new SimulationGeneratorFactors(new string[] { "Year" }, new string[] { year.ToString() },
                    "Year", year.ToString()));
                }
                factors.Add(new SimulationGeneratorFactors(new string[] { "Parameter" }, new string[] { param.Name },
                "Parameter", param.Name));
            }
            return factors;
        }

        /// <summary>
        /// Initialise the experiment ready for creating simulations.
        /// </summary>
        private void Initialise()
        {
            parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
            Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
            serialisedBase = Apsim.SerialiseToStream(baseSimulation) as Stream;
            allCombinations.Clear();
            CalculateFactors();
        }

        /// <summary>
        /// Calculate factors that we need to run. Put combinations into allCombinations
        /// </summary>
        private void CalculateFactors()
        {
            if (allCombinations.Count == 0)
            {
                ParameterValues = RunRToGetParameterValues();
                if (ParameterValues == null || ParameterValues.Rows.Count == 0)
                    throw new Exception("The morris function in R returned null");

                int simulationNumber = 1;
                simulationNames.Clear();
                foreach (DataRow parameterRow in ParameterValues.Rows)
                {
                    List<FactorValue> factors = new List<FactorValue>();
                    foreach (Parameter param in Parameters)
                    {
                        object value = Convert.ToDouble(parameterRow[param.Name]);
                        FactorValue f = new FactorValue(null, param.Name, param.Path, value);
                        factors.Add(f);
                    }

                    string newSimulationName = Name + "Simulation" + simulationNumber;
                    simulationNames.Add(newSimulationName);
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
        /// <param name="dataStore">The data store.</param>
        public void Run(IStorageReader dataStore)
        {
            if (!hasRun)
                return;
            string sql = "SELECT * FROM REPORT WHERE SimulationName LIKE '" + Name + "%' ORDER BY SimulationID";
            DataTable predictedData = dataStore.RunQuery(sql);
            if (predictedData != null)
            {

                // Determine how many years we have per simulation
                DataView view = new DataView(predictedData);
                view.RowFilter = "SimulationName='" + Name + "Simulation1'";
                Years = DataTableUtilities.GetColumnAsIntegers(view, "Clock.Today.Year");

                // Create a table of all predicted values
                DataTable predictedValues = new DataTable();

                List<string> descriptiveColumnNames = new List<string>();
                List<string> variableNames = new List<string>();
                foreach (double year in Years)
                {
                    view.RowFilter = "Clock.Today.Year=" + year;

                    foreach (DataColumn predictedColumn in view.Table.Columns)
                    {
                        if (predictedColumn.DataType == typeof(double))
                        {
                            double[] valuesForYear = DataTableUtilities.GetColumnAsDoubles(view, predictedColumn.ColumnName);
                            if (valuesForYear.Distinct().Count() == 1)
                            {
                                if (!descriptiveColumnNames.Contains(predictedColumn.ColumnName))
                                    descriptiveColumnNames.Add(predictedColumn.ColumnName);
                            }
                            else
                            {
                                DataTableUtilities.AddColumn(predictedValues, predictedColumn.ColumnName + year, valuesForYear);
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
                IndexedDataTable eeTableKey = new IndexedDataTable(new string[] { "Parameter", "Year" });

                // Create a path variable. 
                var pathValues = Enumerable.Range(1, NumPaths).ToArray();

                foreach (var parameter in Parameters)
                {
                    foreach (DataColumn column in predictedValues.Columns)
                    {
                        eeView.RowFilter = "variable = '" + column.ColumnName + "'";
                        if (eeView.Count != NumPaths)
                            throw new Exception("Found only " + eeView.Count + " paths for variable " + column.ColumnName + " in ee table");
                        int year = Convert.ToInt32(column.ColumnName.Substring(column.ColumnName.Length - 4));
                        string variableName = column.ColumnName.Substring(0, column.ColumnName.Length - 4);

                        eeTableKey.SetIndex(new object[] { parameter.Name, year });

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
                IndexedDataTable tableKey = new IndexedDataTable(new string[2] { "Parameter", "Year" });

                foreach (DataRow row in statsDataRaw.Rows)
                {
                    string variable = row["variable"].ToString();
                    int year = Convert.ToInt32(variable.Substring(variable.Length - 4));
                    variable = variable.Substring(0, variable.Length - 4);
                    tableKey.SetIndex(new object[] { row["param"], year });

                    tableKey.Set(variable + ".Mu", row["mu"]);
                    tableKey.Set(variable + ".MuStar", row["mustar"]);
                    tableKey.Set(variable + ".Sigma", row["sigma"]);

                    // Need to bring in the descriptive values.
                    view.RowFilter = "Clock.Today.Year=" + year;
                    foreach (var descriptiveColumnName in descriptiveColumnNames)
                    {
                        var values = DataTableUtilities.GetColumnAsStrings(view, descriptiveColumnName);
                        if (values.Distinct().Count() == 1)
                            tableKey.Set(descriptiveColumnName, view[0][descriptiveColumnName]);
                    }
                }
                DataTable muStarTable = tableKey.ToTable();
                muStarTable.TableName = Name + "Statistics";

                dataStore.DeleteDataInTable(eeTable.TableName);
                dataStore.WriteTable(eeTable);
                dataStore.DeleteDataInTable(muStarTable.TableName);
                dataStore.WriteTable(muStarTable);
            }
            hasRun = false;
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
            Console.WriteLine(r.GetPackage("sensitivity"));
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
            Console.WriteLine(r.GetPackage("sensitivity"));
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
            ("library('sensitivity')" + Environment.NewLine +
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