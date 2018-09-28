namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Factorial;
    using Models.Interfaces;
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
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.TablePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class Morris : Model, ISimulationGenerator, ICustomDocumentation, IModelAsTable, IPostSimulationTool
    {
        /// <summary>The numebr of paths to run</summary>
        private int numPaths = 200;

        /// <summary>A list of factors that we are to run</summary>
        private List<List<FactorValue>> allCombinations = new List<List<FactorValue>>();

        /// <summary>A number of the currently running sim</summary>
        private int simulationNumber;

        /// <summary>Parameter values coming back from R</summary>
        public DataTable ParameterValues { get; set; }

        /// <summary>List of parameters</summary>
        public List<Parameter> parameters { get; set; }

        /// <summary>List of simulation names from last run</summary>
        [XmlIgnore]
        public List<string> simulationNames { get; set; }

        /// <summary>Constructor</summary>
        public Morris()
        {
            parameters = new List<Parameter>();
            allCombinations = new List<List<FactorValue>>();
            simulationNames = new List<string>();
        }

        /// <summary>
        /// Gets or sets the table of values.
        /// </summary>
        [XmlIgnore]
        public DataTable Table
        {
            get
            {
                DataTable table = new DataTable();
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Path", typeof(string));
                table.Columns.Add("LowerBound", typeof(double));
                table.Columns.Add("UpperBound", typeof(double));

                foreach (Parameter param in parameters)
                {
                    DataRow row = table.NewRow();
                    row["Name"] = param.Name;
                    row["Path"] = param.Path;
                    row["LowerBound"] = param.LowerBound;
                    row["UpperBound"] = param.UpperBound;
                    table.Rows.Add(row);
                }

                return table;
            }
            set
            {
                parameters.Clear();
                foreach (DataRow row in value.Rows)
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
                        parameters.Add(param);
                }
            }
        }

        private Stream serialisedBase;
        private Simulations parentSimulations;

        /// <summary>Simulation runs are about to begin.</summary>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun()
        {
            Initialise();
            simulationNumber = 1;
        }

        /// <summary>Gets the next job to run</summary>
        public Simulation NextSimulationToRun(bool fullFactorial = true)
        {
            if (allCombinations.Count == 0)
                return null;

            var combination = allCombinations[0];
            allCombinations.RemoveAt(0);

            Simulation newSimulation = Apsim.DeserialiseFromStream(serialisedBase) as Simulation;
            newSimulation.Name = "Simulation" + simulationNumber;
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

                string xml = Apsim.Serialise(sims);
                File.WriteAllText(Path.Combine(path, sim.Name + ".apsimx"), xml);
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
            var factors = new List<ISimulationGeneratorFactors>();
            foreach (Parameter param in parameters)
            {
                var factor = new SimulationGeneratorFactors("Variable", param.Name, "MorrisVariable", param.Name);
                factors.Add(factor);
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
                ParameterValues = CalculateMorrisParameterValues();
                if (ParameterValues == null || ParameterValues.Rows.Count == 0)
                    throw new Exception("The morris function in R returned null");

                int simulationNumber = 1;
                simulationNames.Clear();
                foreach (DataRow parameterRow in ParameterValues.Rows)
                {
                    List<FactorValue> factors = new List<FactorValue>();
                    foreach (Parameter param in parameters)
                    {
                        object value = Convert.ToDouble(parameterRow[param.Name]);
                        FactorValue f = new FactorValue(null, param.Name, param.Path, value);
                        factors.Add(f);
                    }

                    string newSimulationName = "Simulation" + simulationNumber;
                    simulationNames.Add(newSimulationName);
                    allCombinations.Add(factors);
                    simulationNumber++;
                }
            }
        }

        /// <summary>
        /// Get a list of parameter values that we are to run. Call R to do this.
        /// </summary>
        private DataTable CalculateMorrisParameterValues()
        {
            string script;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Models.Resources.Morris.R"))
                using (StreamReader reader = new StreamReader(s))
                    script = reader.ReadToEnd();

            script = script.Replace("%NUMPATHS%", numPaths.ToString());
            script = script.Replace("%PARAMNAMES%", StringUtilities.Build(parameters.Select(p => p.Name), ",", "\"", "\""));
            script = script.Replace("%PARAMLOWERS%", StringUtilities.Build(parameters.Select(p => p.LowerBound), ","));
            script = script.Replace("%PARAMUPPERS%", StringUtilities.Build(parameters.Select(p => p.UpperBound), ","));
            string rFileName = Path.GetTempFileName();
            File.WriteAllText(rFileName, script);
            R r = new R();
            Console.WriteLine(r.GetPackage("sensitivity"));
            return r.RunToTable(rFileName);
        }

        /// <summary>
        /// Get a list of morris values (ee, mustar, sigmastar) from R
        /// </summary>
        private DataTable CalculateMorrisValues()
        {
            string tempFileName = Path.GetTempFileName();

            string script = string.Format
                            ("T <- read.csv(\"{0}\"" + Environment.NewLine +
                             "DF <- as.data.frame(T)" + Environment.NewLine +
                             "APSIMMorris$X <- DF" + Environment.NewLine +
                             "tell(APSIMMorris)" + Environment.NewLine,
                             tempFileName);

            string rFileName = Path.GetTempFileName();
            File.WriteAllText(rFileName, script);
            R r = new R();
            Console.WriteLine(r.GetPackage("sensitivity"));
            return r.RunToTable(rFileName);
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

        /// <summary>
        /// Create a specific simulation.
        /// </summary>
        public Simulation CreateSpecificSimulation(string name)
        {
            //List<List<FactorValue>> allCombinations = AllCombinations();
            //Simulation baseSimulation = Apsim.Child(this, typeof(Simulation)) as Simulation;
            //Simulations parentSimulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;

            //foreach (List<FactorValue> combination in allCombinations)
            //{
            //    string newSimulationName = Name;
            //    foreach (FactorValue value in combination)
            //        newSimulationName += value.Name;

            //    if (newSimulationName == name)
            //    {
            //        Simulation newSimulation = Apsim.Clone(baseSimulation) as Simulation;
            //        newSimulation.Name = newSimulationName;
            //        newSimulation.Parent = null;
            //        newSimulation.FileName = parentSimulations.FileName;
            //        Apsim.ParentAllChildren(newSimulation);

            //        // Make substitutions.
            //        parentSimulations.MakeSubstitutions(newSimulation);

            //        // Connect events and links in our new  simulation.
            //        Events events = new Events(newSimulation);
            //        LoadedEventArgs loadedArgs = new LoadedEventArgs();
            //        events.Publish("Loaded", new object[] { newSimulation, loadedArgs });

            //        foreach (FactorValue value in combination)
            //            value.ApplyToSimulation(newSimulation);

            //        PushFactorsToReportModels(newSimulation, combination);

            //        return newSimulation;
            //    }
            //}

            return null;
        }

        /// <summary>
        /// Generates the name for a combination of FactorValues.
        /// </summary>
        /// <param name="factors"></param>
        /// <returns></returns>
        private string GetName(List<FactorValue> factors)
        {
            string str = Name;
            foreach (FactorValue factor in factors)
            {
                str += factor.Name;
            }
            return str;
        }

        /// <summary>Main run method for performing our post simulation calculations</summary>
        /// <param name="dataStore">The data store.</param>
        public void Run(IStorageReader dataStore)
        {
            DataTable predictedData = dataStore.GetData("Report");
            if (predictedData != null)
            {
                // Add all the necessary rows and columns to our ee data table.
                DataTable eeTable = new DataTable();
                eeTable.TableName = Name + "ElementaryEffects";
                eeTable.Columns.Add("Variable", typeof(string));
                eeTable.Columns.Add("Path", typeof(int));
                foreach (DataColumn column in predictedData.Columns)
                {
                    if (column.DataType == typeof(double))
                        eeTable.Columns.Add(column.ColumnName, typeof(double));
                }
                for (int i = 0; i < parameters.Count * numPaths; i++)
                    eeTable.Rows.Add();

                // Add all the necessary columns to our muStar data table.
                DataTable muStarTable = new DataTable();
                muStarTable.TableName = Name + "MuStar";
                muStarTable.Columns.Add("Variable", typeof(string));
                foreach (DataColumn column in predictedData.Columns)
                {
                    if (column.DataType == typeof(double))
                    {
                        muStarTable.Columns.Add(column.ColumnName + ".Mu", typeof(double));
                        muStarTable.Columns.Add(column.ColumnName + ".MuStar", typeof(double));
                        muStarTable.Columns.Add(column.ColumnName + ".SigmaStar", typeof(double));
                    }
                }
                for (int i = 0; i < parameters.Count; i++)
                    muStarTable.Rows.Add();

                // Setup some file names
                string morrisParametersFileName = Path.Combine(Path.GetTempPath(), "parameters.csv");
                string apsimVariableFileName = Path.Combine(Path.GetTempPath(), "apsimvariable.csv");
                string eeFileName = Path.Combine(Path.GetTempPath(), "ee.csv");
                string muFileName = Path.Combine(Path.GetTempPath(), "mu.csv");
                string muStarFileName = Path.Combine(Path.GetTempPath(), "mustar.csv");
                string sigmaFileName = Path.Combine(Path.GetTempPath(), "sigma.csv");
                string rFileName = Path.Combine(Path.GetTempPath(), "script.r");


                foreach (DataColumn predictedColumn in predictedData.Columns)
                {
                    if (predictedColumn.DataType == typeof(double))
                    {
                        // Write parameters
                        using (StreamWriter writer = new StreamWriter(morrisParametersFileName))
                            DataTableUtilities.DataTableToText(ParameterValues, 0, ",", true, writer);

                        // write apsim variable
                        using (StreamWriter writer = new StreamWriter(apsimVariableFileName))
                        {
                            writer.WriteLine(predictedColumn.ColumnName);
                            foreach (DataRow row in predictedData.Rows)
                                writer.WriteLine(row[predictedColumn]);
                        }

                        // write script
                        string paramNames = StringUtilities.Build(parameters.Select(p => p.Name), ",", "\"", "\"");
                        string lowerBounds = StringUtilities.Build(parameters.Select(p => p.LowerBound), ",");
                        string upperBounds = StringUtilities.Build(parameters.Select(p => p.UpperBound), ",");
                        string script = string.Format
                                        ("library('sensitivity')" + Environment.NewLine +
                                         "Params <- c({0})" + Environment.NewLine +
                                         "APSIMMorris<-morris(model=NULL" + Environment.NewLine +
                                         "    ,Params #string vector of parameter names" + Environment.NewLine +
                                         "    ,{1} #no of paths within the total parameter space" + Environment.NewLine +
                                         "    ,design=list(type=\"oat\",levels=20,grid.jump=10)" + Environment.NewLine +
                                         "    ,binf=c({2}) #min for each parameter" + Environment.NewLine +
                                         "    ,bsup=c({3}) #max for each parameter" + Environment.NewLine +
                                         "    ,scale=T" + Environment.NewLine +
                                         "    )" + Environment.NewLine +
                                         "APSIMMorris$X <- as.data.frame(read.csv(\"{4}\"))" + Environment.NewLine +
                                         "Y <- read.csv(\"{5}\")" + Environment.NewLine +
                                         "APSIMMorris$y <- as.vector(Y${6})" + Environment.NewLine +
                                         "tell(APSIMMorris)" + Environment.NewLine +
                                         "write.csv(APSIMMorris$ee,\"{7}\", row.names=FALSE)" + Environment.NewLine +
                                         "write.csv(apply(APSIMMorris$ee, 2, mean), \"{8}\", row.names=FALSE)" + Environment.NewLine +
                                         "write.csv(apply(APSIMMorris$ee, 2, function(x) mean(abs(x))), \"{9}\", row.names=FALSE)" + Environment.NewLine +
                                         "write.csv(apply(APSIMMorris$ee, 2, sd), \"{10}\", row.names=FALSE)",
                                         paramNames, numPaths, lowerBounds, upperBounds,
                                         morrisParametersFileName.Replace("\\", "/"),
                                         apsimVariableFileName.Replace("\\", "/"),
                                         predictedColumn.ColumnName,
                                         eeFileName.Replace("\\", "/"),
                                         muFileName.Replace("\\", "/"),
                                         muStarFileName.Replace("\\", "/"),
                                         sigmaFileName.Replace("\\", "/"));
                        File.WriteAllText(rFileName, script);

                        // Run R
                        R r = new R();
                        Console.WriteLine(r.GetPackage("sensitivity"));
                        r.RunToTable(rFileName);

                        // Get ee data from R and store in ee table.
                        List<double> values = new List<double>();
                        DataTable eeDataRaw = ApsimTextFile.ToTable(eeFileName);
                        int rowIndex = 0;
                        foreach (DataColumn col in eeDataRaw.Columns)
                        {
                            values.Clear();
                            for (int path = 1; path <= eeDataRaw.Rows.Count; path++)
                            {
                                values.Add(Math.Abs(Convert.ToDouble(eeDataRaw.Rows[path - 1][col])));

                                eeTable.Rows[rowIndex]["Variable"] = col.ColumnName;
                                eeTable.Rows[rowIndex]["Path"] = path;
                                eeTable.Rows[rowIndex][predictedColumn.ColumnName] = MathUtilities.Average(values);
                                rowIndex++;
                            }
                        }

                        // Get mustar data from R and store in MuStar table.
                        DataTable muDataRaw = ApsimTextFile.ToTable(muFileName);
                        rowIndex = 0;
                        foreach (var parameter in parameters)
                        {
                            muStarTable.Rows[rowIndex]["Variable"] = parameter.Name;
                            muStarTable.Rows[rowIndex][predictedColumn + ".Mu"] = muDataRaw.Rows[rowIndex][0];
                            rowIndex++;
                        }

                        // Get mustar data from R and store in MuStar table.
                        DataTable muStarDataRaw = ApsimTextFile.ToTable(muStarFileName);
                        rowIndex = 0;
                        foreach (var parameter in parameters)
                        {
                            muStarTable.Rows[rowIndex]["Variable"] = parameter.Name;
                            muStarTable.Rows[rowIndex][predictedColumn + ".MuStar"] = muStarDataRaw.Rows[rowIndex][0];
                            rowIndex++;
                        }

                        // Get mustar data from R and store in MuStar table.
                        DataTable sigmaStarDataRaw = ApsimTextFile.ToTable(sigmaFileName);
                        rowIndex = 0;
                        foreach (var parameter in parameters)
                        {
                            muStarTable.Rows[rowIndex]["Variable"] = parameter.Name;
                            muStarTable.Rows[rowIndex][predictedColumn + ".SigmaStar"] = sigmaStarDataRaw.Rows[rowIndex][0];
                            rowIndex++;
                        }
                    }
                }
                dataStore.DeleteDataInTable(eeTable.TableName);
                dataStore.WriteTable(eeTable);
                dataStore.DeleteDataInTable(muStarTable.TableName);
                dataStore.WriteTable(muStarTable);
            }
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

        /// <summary>A encapsulation of a parameter to analyse</summary>
        public class Parameter
        {
            /// <summary>Name of parameter</summary>
            public string Name;

            /// <summary>Model path of parameter</summary>
            public string Path;

            /// <summary>Lower bound of parameter</summary>
            public double LowerBound;

            /// <summary>Upper bound of parameter</summary>
            public double UpperBound;
        }


    }
}
