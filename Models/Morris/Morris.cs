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

        /// <summary>List of parameters</summary>
        public List<Parameter> parameters { get; set; }

        /// <summary>List of simulation names from last run</summary>
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
        }

        /// <summary>Gets the next job to run</summary>
        public Simulation NextSimulationToRun(bool fullFactorial = true)
        {
            if (allCombinations.Count == 0)
                return null;

            var combination = allCombinations[0];
            allCombinations.RemoveAt(0);
            string newSimulationName = Name;
            foreach (FactorValue value in combination)
                newSimulationName += value.Name + value.Values[0];

            Simulation newSimulation = Apsim.DeserialiseFromStream(serialisedBase) as Simulation;
            newSimulation.Name = newSimulationName;
            newSimulation.Parent = null;
            newSimulation.FileName = parentSimulations.FileName;
            Apsim.ParentAllChildren(newSimulation);

            // Make substitutions.
            parentSimulations.MakeSubsAndLoad(newSimulation);

            foreach (FactorValue value in combination)
                value.ApplyToSimulation(newSimulation);

            return newSimulation;
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
                DataTable parameterValues = CalculateMorrisParameterValues();
                if (parameterValues == null || parameterValues.Rows.Count == 0)
                    throw new Exception("The morris function in R returned null");

                foreach (DataRow parameterRow in parameterValues.Rows)
                {
                    List<FactorValue> factors = new List<FactorValue>();
                    foreach (Parameter param in parameters)
                    {
                        FactorValue f = new FactorValue(null, param.Name, param.Path, parameterRow[param.Name]);
                        factors.Add(f);
                    }

                    allCombinations.Add(factors);
                }

                // Work out simulation names;
                simulationNames.Clear();
                foreach (List<FactorValue> combination in allCombinations)
                {
                    string newSimulationName = Name;

                    foreach (FactorValue value in combination)
                        newSimulationName += value.Name + value.Values[0];

                    simulationNames.Add(newSimulationName);
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
                DataTable elementalEffects = CreateElementalEffectsTable(predictedData);
                dataStore.DeleteDataInTable(elementalEffects.TableName);
                dataStore.WriteTable(elementalEffects);

                DataTable muStarByPath = CreateMuStarByPath(elementalEffects);
                dataStore.DeleteDataInTable(muStarByPath.TableName);
                dataStore.WriteTable(muStarByPath);

                DataTable muStar = CreateMuStar(muStarByPath);
                dataStore.DeleteDataInTable(muStar.TableName);
                dataStore.WriteTable(muStar);
            }
        }

        /// <summary>
        /// Create an elemental effects table.
        /// </summary>
        /// <param name="predictedData"></param>
        private DataTable CreateElementalEffectsTable(DataTable predictedData)
        {
            // Add all the necessary columns to our data table.
            DataTable eeTable = new DataTable();
            eeTable.TableName = Name + "ElementalEffects";
            eeTable.Columns.Add("Variable", typeof(string));
            eeTable.Columns.Add("Path", typeof(int));
            foreach (DataColumn column in predictedData.Columns)
            {
                if (column.DataType == typeof(double))
                    eeTable.Columns.Add(column.ColumnName, typeof(double));
            }

            int path = 1;
            for (int rowIndex = 1; rowIndex < predictedData.Rows.Count; rowIndex++)
            {
                for (int parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
                {
                    DataRow newRow = eeTable.NewRow();
                    newRow["Variable"] = parameters[parameterIndex].Name;
                    newRow["Path"] = path;
                    foreach (DataColumn column in predictedData.Columns)
                    {
                        if (column.DataType == typeof(double))
                        {
                            double thisValue = Convert.ToDouble(predictedData.Rows[rowIndex][column.ColumnName]);
                            double previousValue = Convert.ToDouble(predictedData.Rows[rowIndex - 1][column.ColumnName]);
                            newRow[column.ColumnName] = Math.Abs(thisValue - previousValue);
                        }
                    }
                    eeTable.Rows.Add(newRow);

                    rowIndex++;
                }

                path++;
            }
            return eeTable;
        }

        /// <summary>
        /// Create a MuStar by path number table - running mean mustar
        /// </summary>
        private DataTable CreateMuStarByPath(DataTable eeTable)
        {
            // Add all the necessary columns to our data table.
            DataTable muStarTable = new DataTable();
            muStarTable.TableName = Name + "MuStarByPath";
            muStarTable.Columns.Add("Variable", typeof(string));
            muStarTable.Columns.Add("Path", typeof(int));
            foreach (DataColumn column in eeTable.Columns)
            {
                if (column.DataType == typeof(double))
                {
                    muStarTable.Columns.Add(column.ColumnName + ".MuStar", typeof(double));
                    muStarTable.Columns.Add(column.ColumnName + ".SigmaStar", typeof(double));
                }
            }

            int numPathsFound = eeTable.Rows.Count / parameters.Count;
            DataView eeView = new DataView(eeTable);
            for (int path = 1; path <= numPathsFound; path++)
            {
                for (int parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
                {
                    DataRow newRow = muStarTable.NewRow();
                    newRow["Variable"] = parameters[parameterIndex].Name;
                    newRow["Path"] = path;

                    eeView.RowFilter = "Path <= " + path + " and Variable = '" + parameters[parameterIndex].Name + "'";
                    foreach (DataColumn column in eeTable.Columns)
                    {
                        if (column.DataType == typeof(double))
                        {
                            double[] values = DataTableUtilities.GetColumnAsDoubles(eeView, column.ColumnName);
                            newRow[column.ColumnName + ".MuStar"] = MathUtilities.Average(values);
                            newRow[column.ColumnName + ".SigmaStar"] = MathUtilities.StandardDeviation(values);
                        }
                    }
                    muStarTable.Rows.Add(newRow);
                }
            }
            return muStarTable;
        }

        /// <summary>
        /// Create a MuStar by path number table - running mean mustar
        /// </summary>
        private DataTable CreateMuStar(DataTable muStarByPathTable)
        {
            // Add all the necessary columns to our data table.
            DataTable muStarTable = new DataTable();
            muStarTable.TableName = Name + "MuStar";
            muStarTable.Columns.Add("Variable", typeof(string));
            foreach (DataColumn column in muStarByPathTable.Columns)
            {
                if (column.DataType == typeof(double))
                    muStarTable.Columns.Add(column.ColumnName, typeof(double));
            }

            muStarTable.ImportRow(muStarByPathTable.Rows[muStarByPathTable.Rows.Count - 2]);
            muStarTable.ImportRow(muStarByPathTable.Rows[muStarByPathTable.Rows.Count - 1]);
            return muStarTable;
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
