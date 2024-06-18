using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Extensions;
using ApsimNG.Classes;
using Shared.Utilities;
using Gtk;
using Markdig.Helpers;
using Models;
using Models.Core;
using Models.Factorial;
using Models.PMF;
using Models.Storage;
using Newtonsoft.Json;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;
using APSIM.Shared.Utilities;

namespace UserInterface.Presenters
{

    /// <summary>
    /// The Report presenter class
    /// </summary>
    public class ReportPresenter : IPresenter
    {
        /// <summary>
        /// Used by the intellisense to keep track of which editor the user is currently using.
        /// Without this, it's difficult to know which editor (variables or events) to
        /// insert an intellisense item into.
        /// </summary>
        private object currentEditor;

        /// <summary> The report object</summary>
        private Report report;

        /// <summary> The report view</summary>
        private IReportView view;

        /// <summary> The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary> The data storage</summary>
        private IDataStore dataStore;

        /// <summary> The data store presenter object</summary>
        private DataStorePresenter dataStorePresenter;

        /// <summary> The intellisense object.</summary>
        private IntellisensePresenter intellisense;

        /// <summary> Stores a DataTable of common report frequency variables.</summary>
        private DataTable commonReportFrequencyVariables;

        /// <summary> Stores a DataTable of common report variables.</summary>
        private DataTable commonReportVariables;

        /// <summary> Stores a list of  common ReportVariables.</summary>
        private List<ReportVariable> commonReportVariablesList;

        /// <summary> Stores a lst of common frequency ReportVariables.</summary>
        private List<ReportVariable> commonFrequencyVariableList;

        /// <summary> Stores all names of nodes that are of type Plant. </summary>
        private List<string> simulationPlantModelNames;

        /// <summary> Stores all names of nodes that are of type ISoilWater.</summary>
        private Dictionary<string, List<string>> modelsImplementingSpecificInterfaceDictionary = new();

        /// <summary> File name for reporting variables.</summary>
        private readonly string commonReportVariablesFileName = "CommonReportingVariables.json";

        /// <summary> File name for report frequency variables.</summary>
        private readonly string commonReportFrequencyVariablesFileName = "CommonFrequencyVariables.json";

        /// <summary> Common directory path. </summary>
        private readonly string reportVariablesDirectoryPath = Path.Combine(new string[] { "ApsimNG", "Resources", "CommonReportVariables" });

        /// <summary> All in scope model names of the current apsimx file.</summary>
        public List<string> InScopeModelNames { get; set; }

        /// <summary> Returns all model names that are of type Plant. </summary>
        public List<string> SimulationPlantModelNames
        {
            set { simulationPlantModelNames = value; }
            get { return simulationPlantModelNames; }
        }

        private List<ReportVariable> GetCommonVariables(string fileName, string fileDirectoryPath)
        {
            string currentAssemblyDirectory = Assembly.GetExecutingAssembly().Location.Split("bin")[0];
            string commonReportingVariablesFilePath = Path.Combine(currentAssemblyDirectory, fileDirectoryPath, fileName);
            string reportingVariablesJSON = File.ReadAllText(commonReportingVariablesFilePath);
            return JsonConvert.DeserializeObject<List<ReportVariable>>(reportingVariablesJSON);
        }

        public List<ReportVariable> CommonReportVariablesList
        {
            get { return commonReportVariablesList; }
            set { commonReportVariablesList = value; }
        }

        public List<ReportVariable> CommonFrequencyVariablesList
        {
            get { return commonFrequencyVariableList; }
            set { commonFrequencyVariableList = value; }
        }

        /// <summary> Stores ReportDragObject to coping into EditorView.</summary>
        public ReportDragObject StoredDragObject { get; set; }

        /// <summary> DataTable for storing common report variables. </summary>
        public DataTable CommonReportVariables
        {
            get { return commonReportVariables; }
            set { commonReportVariables = value; }
        }

        /// <summary> DataTable for storing common report frequency variables. </summary>
        public DataTable CommonReportFrequencyVariables
        {
            get { return commonReportFrequencyVariables; }

            set { commonReportFrequencyVariables = value; }
        }

        /// <summary>
        /// Attach the model (report) and the view (IReportView)
        /// </summary>
        /// <param name="model">The report model object</param>
        /// <param name="view">The view object</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter) // was using an async keyword
        {
            this.report = model as Report;
            this.explorerPresenter = explorerPresenter;
            this.view = view as IReportView;
            this.intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;
            this.view.VariableList.Mode = EditorType.Report;
            this.view.EventList.Mode = EditorType.Report;
            this.view.VariableList.Lines = report.VariableNames;
            this.view.EventList.Lines = report.EventNames;
            InScopeModelNames = GetModelScopeNames();//explorerPresenter.ApsimXFile.FindAllInScope<IModel>().Select(m => m.Name).ToList<string>();
            SimulationPlantModelNames = explorerPresenter.ApsimXFile.FindAllInScope<Plant>().Select(m => m.Name).ToList<string>();
            CommonReportVariablesList = GetCommonVariables(commonReportVariablesFileName, reportVariablesDirectoryPath);
            CommonFrequencyVariablesList = GetCommonVariables(commonReportFrequencyVariablesFileName, reportVariablesDirectoryPath);
            FillModelsImplementingSpecificInterfaceDictionary();
            AddInterfaceImplementingTypesToModelScopeNames();
            CommonReportVariables = GetReportVariables(CommonReportVariablesList, InScopeModelNames, true);
            this.view.CommonReportVariablesList.DataSource = CommonReportVariables;
            this.view.CommonReportVariablesList.DragStart += OnCommonReportVariableListDragStart;
            CommonReportFrequencyVariables = GetReportVariables(CommonFrequencyVariablesList, InScopeModelNames, true);
            this.view.CommonReportFrequencyVariablesList.DataSource = CommonReportFrequencyVariables;
            this.view.CommonReportFrequencyVariablesList.DragStart += OnCommonReportFrequencyVariableListDragStart;
            (this.view as ReportView).VariableList.VariableDragDataReceived += OnVariableListVariableDrop;
            (this.view as ReportView).EventList.VariableDragDataReceived += OnEventListVariableDrop;
            this.view.GroupByEdit.Text = report.GroupByVariableName;
            this.view.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded += OnNeedVariableNames;
            this.view.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.VariableList.TextHasChangedByUser += OnVariableListTextChanged;
            this.view.EventList.TextHasChangedByUser += OnEventNamesChanged;
            this.view.EventList.TextHasChangedByUser += OnEventListTextChanged;
            this.view.GroupByEdit.Changed += OnGroupByChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            this.view.CommonReportVariablesList.DoubleClicked += OnCommonReportVariableListDoubleClicked;
            this.view.CommonReportFrequencyVariablesList.DoubleClicked += OnCommonReportFrequencyVariablesListDoubleClicked;
            this.view.CommonReportVariablesList.TreeHover += OnCommonVariableListTreeHover;
            this.view.CommonReportFrequencyVariablesList.TreeHover += OnCommonFrequencyListTreeHover;

            Simulations simulations = report.FindAncestor<Simulations>();
            if (simulations != null)
            {
                dataStore = simulations.FindChild<IDataStore>();
            }

            //// TBI this.view.VariableList.SetSyntaxHighlighter("Report");

            dataStorePresenter = new DataStorePresenter(new string[] { report.Name });
            Simulation simulation = report.FindAncestor<Simulation>();
            Experiment experiment = report.FindAncestor<Experiment>();
            Zone paddock = report.FindAncestor<Zone>();

            // Only show data which is in scope of this report.
            // E.g. data from this zone and either experiment (if applicable) or simulation.
            if (paddock != null)
                dataStorePresenter.ZoneFilter = paddock;
            if (experiment != null)
                dataStorePresenter.ExperimentFilter = experiment;
            else if (simulation != null)
                dataStorePresenter.SimulationFilter = simulation;

            dataStorePresenter.Attach(dataStore, this.view.DataStoreView, explorerPresenter);
            this.view.TabIndex = this.report.ActiveTabIndex;
        }

        private void OnCommonVariableListTreeHover(object o, ListViewArgs args)
        {
            if (args.ListViewRowIndex >= 0 && CommonReportVariables.Rows.Count >= args.ListViewRowIndex)
            {
                //string reportVariableCode = CommonReportVariablesList[args.ListViewRowIndex].Code;
                string reportVariableCode = CommonReportVariables.Rows[args.ListViewRowIndex]["Code"].ToString();
                (view.CommonReportVariablesList as ListView).ShowTooltip(reportVariableCode, args);
            }
        }

        private void OnCommonFrequencyListTreeHover(object o, ListViewArgs args)
        {
            if (args.ListViewRowIndex >= 0 && CommonReportFrequencyVariables.Rows.Count >= args.ListViewRowIndex)
            {
                string reportVariableCode = CommonReportFrequencyVariables.Rows[args.ListViewRowIndex]["Code"].ToString();
                (view.CommonReportFrequencyVariablesList as ListView).ShowTooltip(reportVariableCode, args);
            }
        }


        /// <summary>
        /// Intended to handle what happens to common report frequency variables when text is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEventListTextChanged(object sender, EventArgs e)
        {
            int frequencyVariableListLineNumber = view.EventList.CurrentLineNumber;
            int lineIndex = frequencyVariableListLineNumber - 1;
            int linesCount = view.EventList.Lines.Length;
            if (lineIndex <= linesCount)
            {
                string lineContent = "";
                if (lineIndex < linesCount)
                    lineContent = this.view.EventList.Lines[lineIndex];

                List<string> lineStrings = new();

                if (!lineContent.Equals(".") && !string.IsNullOrWhiteSpace(lineContent))
                {
                    lineStrings = lineContent.Split(new char[3] { '[', ']', '.' }).ToList<string>();
                    lineStrings.RemoveAll(s => s == "");
                }

                if (lineStrings.Count != 0)
                {
                    DataTable potentialCommonReportEvents = CreateDataTableWithColumns();
                    DataTable scopeFilteredCommonEventVariables = CreateDataTableWithColumns();

                    potentialCommonReportEvents = GetReportVariables(CommonFrequencyVariablesList, lineStrings, false);
                    scopeFilteredCommonEventVariables = filterReportVariableByScope(potentialCommonReportEvents);

                    if (scopeFilteredCommonEventVariables.Rows.Count != 0)
                        CommonReportFrequencyVariables = scopeFilteredCommonEventVariables;
                    else
                    {
                        DataRow emptyRow = scopeFilteredCommonEventVariables.NewRow();
                        emptyRow["Description"] = "";
                        emptyRow["Code"] = "";
                        emptyRow["Type"] = "";
                        emptyRow["Units"] = "";
                        scopeFilteredCommonEventVariables.Rows.Add(emptyRow);
                        CommonReportFrequencyVariables = scopeFilteredCommonEventVariables;
                    }
                    view.CommonReportFrequencyVariablesList.DataSource = CommonReportFrequencyVariables;
                }
                else
                {
                    CommonReportFrequencyVariables = GetReportVariables(CommonFrequencyVariablesList, InScopeModelNames, true);
                    view.CommonReportFrequencyVariablesList.DataSource = CommonReportFrequencyVariables;
                }
            }
        }

        /// <summary>
        /// Intended to handle what happens to common report variables when text is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVariableListTextChanged(object sender, EventArgs e)
        {
            int variableListLineNumber = view.VariableList.CurrentLineNumber;
            int lineIndex = variableListLineNumber - 1;
            int linesCount = view.VariableList.Lines.Length;
            if (lineIndex <= linesCount)
            {
                string lineContent = "";
                if (lineIndex < linesCount)
                    lineContent = this.view.VariableList.Lines[lineIndex];

                List<string> lineStrings = new();

                if (!lineContent.Equals(".") && !string.IsNullOrWhiteSpace(lineContent))
                {
                    lineStrings = lineContent.Split(new char[3] { '[', ']', '.' }).ToList<string>();
                    lineStrings.RemoveAll(s => s == "");
                }

                if (lineStrings.Count != 0)
                {
                    DataTable potentialCommonReportVariables = CreateDataTableWithColumns();
                    DataTable scopeFilteredCommonReportVariables = CreateDataTableWithColumns();

                    potentialCommonReportVariables = GetReportVariables(CommonReportVariablesList, lineStrings, false);
                    scopeFilteredCommonReportVariables = filterReportVariableByScope(potentialCommonReportVariables);

                    if (scopeFilteredCommonReportVariables.Rows.Count != 0)
                        CommonReportVariables = scopeFilteredCommonReportVariables;
                    else
                    {
                        DataRow emptyRow = scopeFilteredCommonReportVariables.NewRow();
                        emptyRow["Description"] = "";
                        emptyRow["Code"] = "";
                        emptyRow["Type"] = "";
                        emptyRow["Units"] = "";
                        scopeFilteredCommonReportVariables.Rows.Add(emptyRow);
                        CommonReportVariables = scopeFilteredCommonReportVariables;
                    }
                    view.CommonReportVariablesList.DataSource = CommonReportVariables;

                }
                else
                {
                    CommonReportVariables = GetReportVariables(CommonReportVariablesList, InScopeModelNames, true);
                    view.CommonReportVariablesList.DataSource = CommonReportVariables;
                }
            }
        }

        /// <summary>
        /// Takes <see cref="modelsImplementingSpecificInterfaceDictionary"/> and makes sure any ReportVariables that have a node as a key of this Dictionary
        /// get included.
        /// </summary>
        /// <param name="scopeFilteredCommonReportVariables"></param>
        /// <returns> A DataTable of filtered <see cref="ReportVariable"/></returns>
        private void AddInterfaceImplementingTypesToModelScopeNames()
        {
            foreach (KeyValuePair<string, List<string>> keyValuePair in modelsImplementingSpecificInterfaceDictionary)
                InScopeModelNames.Add(keyValuePair.Key);
        }

        /// <summary>
        /// Returns a DataTable with the ReportVariables filtered to only contain ones that match a node in scope.
        /// </summary>
        /// <param name="potentialCommonReportVariables"></param>
        /// <returns></returns>
        private DataTable filterReportVariableByScope(DataTable potentialCommonReportVariables)
        {
            DataTable filteredDataTable = CreateDataTableWithColumns();
            foreach (DataRow row in potentialCommonReportVariables.Rows)
            {
                // Allows the inclusion of ReportVariables with Node columns containing Plant.
                if (SimulationPlantModelNames.Count > 0)
                {
                    if (row["Node"].ToString().Contains(","))
                    {
                        List<string> nodeRowStrings = row["Node"].ToString().Split(',').ToList();
                        foreach (string nodeString in nodeRowStrings)
                            if (InScopeModelNames.Contains(nodeString) || nodeString.Contains("Plant"))
                                filteredDataTable.ImportRow(row);
                    }
                    else
                        if (InScopeModelNames.Contains(row["Node"]) || row["Node"].ToString().Contains("Plant"))
                        filteredDataTable.ImportRow(row);
                }
                else
                {
                    if (row["Node"].ToString().Contains(","))
                    {
                        List<string> nodeRowStrings = row["Node"].ToString().Split(',').ToList();
                        foreach (string nodeString in nodeRowStrings)
                            if (InScopeModelNames.Contains(nodeString))
                                filteredDataTable.ImportRow(row);
                    }
                    else
                    {
                        if (InScopeModelNames.Contains(row["Node"]))
                            filteredDataTable.ImportRow(row);
                    }
                }
            }
            return filteredDataTable;
        }


        /// <summary>
        /// Adds the code from the StoredDragObject to the VariableList in the NewReportView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVariableListVariableDrop(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(StoredDragObject.Code))
            {
                List<String> plantVariableLines = new();
                if (StoredDragObject.Code.Contains("[Plant]"))
                    plantVariableLines = CreatePlantVariableLines(StoredDragObject.Code);
                List<string> textLinesList = view.VariableList.Lines.ToList();
                if (view.VariableList.CurrentLineNumber > textLinesList.Count)
                    if (plantVariableLines.Count > 0)
                        foreach (String line in plantVariableLines)
                            textLinesList.Add(line);
                    else textLinesList.Add(StoredDragObject.Code);
                else
                    if (plantVariableLines.Count > 0)
                    foreach (String line in plantVariableLines)
                        textLinesList.Insert(view.VariableList.CurrentLineNumber, line);
                else textLinesList.Insert(view.VariableList.CurrentLineNumber, StoredDragObject.Code);
                string modifiedText = string.Join(Environment.NewLine, textLinesList);
                view.VariableList.Text = modifiedText;
                StoredDragObject = null;
                // TODO: Highlighted line needs to be correctly set.
            }
        }

        /// <summary>
        /// Adds the code from the StoredDragObject to the EventList in the NewReportView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEventListVariableDrop(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(StoredDragObject.Code))
            {
                List<String> plantVariableLines = new();
                if (StoredDragObject.Code.Contains("[Plant]"))
                    plantVariableLines = CreatePlantVariableLines(StoredDragObject.Code);
                List<string> textLinesList = view.EventList.Lines.ToList();
                if (view.EventList.CurrentLineNumber > textLinesList.Count)
                    if (plantVariableLines.Count > 0)
                        foreach (String line in plantVariableLines)
                            textLinesList.Add(line);
                    else textLinesList.Add(StoredDragObject.Code);
                else
                    if (plantVariableLines.Count > 0)
                    foreach (String line in plantVariableLines)
                        textLinesList.Insert(view.EventList.CurrentLineNumber, line);
                else textLinesList.Insert(view.EventList.CurrentLineNumber, StoredDragObject.Code);
                string modifiedText = string.Join(Environment.NewLine, textLinesList);
                view.EventList.Text = modifiedText;
                if (plantVariableLines.Count > 0)
                {
                    // Makes the last line the one that is selected when multiples are added, such as when plant variable lines are added.
                    view.EventList.Location = new ManagerCursorLocation(StoredDragObject.Code.Length, view.EventList.CurrentLineNumber + (plantVariableLines.Count - 1)); // TODO: Highlighted line needs to be correctly set.
                }
                else
                {
                    view.EventList.Location = new ManagerCursorLocation(StoredDragObject.Code.Length, view.EventList.CurrentLineNumber);
                    StoredDragObject = null;
                }
            }
        }

        /// <summary>
        /// Stores the ReportDragObject on DragBegin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommonReportVariableListDragStart(object sender, EventArgs e) // Make this store the ReportDragObject.
        {
            if (e != null)
                StoredDragObject = (ReportDragObject)e;
        }

        /// <summary>
        /// Stores the ReportDragObject on DragBegin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommonReportFrequencyVariableListDragStart(object sender, EventArgs e)
        {
            if (e != null)
                StoredDragObject = (ReportDragObject)e;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.report.ActiveTabIndex = this.view.TabIndex;
            this.view.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded -= OnNeedVariableNames;
            this.view.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            this.view.GroupByEdit.Changed -= OnGroupByChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter?.Detach();
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
        }


        /// <summary>
        /// Creates a DataTable from the reportVariables inside a resource file name. 
        /// </summary>
        /// <param name="variableList"> List of ReportVariables</param>
        /// <param name="inputStrings"> String List containing model names or properties to use as filters.</param>
        /// <param name="isModelScope"> A flag to determine if GetReportVariables should perform a substring check on the ReportVariable.Description field. 
        /// If inputStrings are model names and isModelScope is false, many duplicates will appear in the common report variables/events lists.
        /// </param>
        /// <returns>A <see cref="DataTable"/> containing commonReportVariables or commonReportFrequencyVariables.</returns>
        public DataTable GetReportVariables(List<ReportVariable> variableList, List<string> inputStrings, bool isModelScope)
        {
            try
            {
                // Build a DataTable to replace the private variable
                DataTable variableDataTable = CreateDataTableWithColumns();

                foreach (ReportVariable reportVariable in variableList)
                {
                    // Find only relevant variables based on models in simulation.
                    bool plantTypeInFilters = false;
                    if (!plantTypeInFilters)
                    {
                        foreach (string input in inputStrings)
                        {
                            //plantTypeInFilters = IsModelTypePlant(filter);
                            foreach (string filter in SimulationPlantModelNames)
                            {
                                if (filter.Contains(input))
                                    plantTypeInFilters = true;
                                if (plantTypeInFilters)
                                    break;
                            }
                            if (plantTypeInFilters)
                                break;
                        }
                    }

                    List<string> nodeStrings = reportVariable.Node.ToString().Split(",").ToList();
                    foreach (string nodeString in nodeStrings)
                    {
                        foreach (string inputString in inputStrings)
                        {
                            if (nodeString.Contains(inputString))
                            {
                                DataRow row = variableDataTable.NewRow();
                                row["Description"] = reportVariable.Description;
                                row["Node"] = reportVariable.Node;
                                row["Code"] = reportVariable.Code;
                                row["Type"] = reportVariable.Type;
                                row["Units"] = reportVariable.Units;
                                variableDataTable.Rows.Add(row);
                            }

                            if (modelsImplementingSpecificInterfaceDictionary.ContainsKey(reportVariable.Node))
                            {
                                foreach (KeyValuePair<string, List<string>> record in modelsImplementingSpecificInterfaceDictionary)
                                {
                                    if (record.Key == reportVariable.Node.ToString())
                                    {
                                        foreach (string implementingModelName in record.Value)
                                        {
                                            if (implementingModelName == inputString)
                                            {
                                                DataRow row = variableDataTable.NewRow();
                                                row["Description"] = reportVariable.Description;
                                                row["Node"] = reportVariable.Node;
                                                row["Code"] = reportVariable.Code;
                                                row["Type"] = reportVariable.Type;
                                                row["Units"] = reportVariable.Units;
                                                variableDataTable.Rows.Add(row);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!isModelScope)
                    {
                        List<string> inputStringsLowercase = inputStrings.Select(s => s.ToLower()).ToList();
                        string reportVariableDescriptionLowercase = reportVariable.Description.ToLower();
                        foreach (string input in inputStringsLowercase)
                        {
                            if (reportVariableDescriptionLowercase.Contains(input))
                            {
                                DataRow row = variableDataTable.NewRow();
                                row["Description"] = reportVariable.Description;
                                row["Node"] = reportVariable.Node;
                                row["Code"] = reportVariable.Code;
                                row["Type"] = reportVariable.Type;
                                row["Units"] = reportVariable.Units;
                                variableDataTable.Rows.Add(row);
                            }
                        }
                    }

                    // Adds if a plant type name is typed.
                    if (plantTypeInFilters == true && reportVariable.Code.Contains("[Plant]"))
                    {
                        DataRow row = variableDataTable.NewRow();
                        row["Description"] = reportVariable.Description;
                        row["Node"] = reportVariable.Node;
                        row["Code"] = reportVariable.Code;
                        row["Type"] = reportVariable.Type;
                        row["Units"] = reportVariable.Units;
                        variableDataTable.Rows.Add(row);
                    }
                }

                // Perform a check to see if rows are unique, then remove any that are duplicates.
                DataTable allUniqueTable = CreateDataTableWithColumns();
                if (variableDataTable.Rows.Count > 0)
                {
                    allUniqueTable = variableDataTable.AsEnumerable()
                        .GroupBy(row => new
                        {
                            Code = row.Field<string>("Code"),
                        })
                        .Select(group => group.First())
                        .CopyToDataTable();
                }

                return allUniqueTable;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        public DataTable CreateDataTableWithColumns()
        {
            DataTable potentialCommonReportVariables = new DataTable();
            DataColumn descriptionColumn = new DataColumn("Description");
            DataColumn nodeColumn = new DataColumn("Node");
            DataColumn codeColumn = new DataColumn("Code");
            DataColumn typeColumn = new DataColumn("Type");
            DataColumn unitsColumn = new DataColumn("Units");
            potentialCommonReportVariables.Columns.Add(descriptionColumn);
            potentialCommonReportVariables.Columns.Add(nodeColumn);
            potentialCommonReportVariables.Columns.Add(codeColumn);
            potentialCommonReportVariables.Columns.Add(typeColumn);
            potentialCommonReportVariables.Columns.Add(unitsColumn);
            return potentialCommonReportVariables;
        }

        /// <summary> Returns a list of model names of any in scope models that implement a specific interface.</summary>
        /// <param name="interfaceName"> The name of an Interface.</param>
        /// <returns>A List of model name strings that implement the interface string.</returns>
        private List<string> GetInScopeModelImplementingInterface(string interfaceName)
        {
            if (!string.IsNullOrWhiteSpace(interfaceName))
            {
                List<IModel> implementingModels = explorerPresenter.ApsimXFile.FindAllInScope()
                    .Where(model => model.GetType().GetInterfaces().ToList().Any(type => type.Name == interfaceName)).ToList();
                List<string> implementingModelNames = implementingModels.Select(model => model.Name).ToList();
                return implementingModelNames;
            }
            else return new List<string>();
        }

        /// <summary>
        /// Returns a List of strings with all the model's names that are in scope.
        /// </summary>
        /// <returns> A list of model name strings.</returns>
        private List<string> GetModelScopeNames()
        {
            List<string> modelNamesInScope = new();
            Simulations simulations = explorerPresenter.ApsimXFile;
            List<IModel> modelInScope = simulations.FindAllInScope<IModel>().ToList();
            modelNamesInScope = modelInScope.Select(x => x.GetType().GetFriendlyName()).Distinct<string>().ToList();
            return modelNamesInScope;
        }

        /// <summary>
        /// The view is asking for variable names.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            GetCompletionOptions(sender, e, true, false, true);
        }

        /// <summary>The view is asking for event names.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnNeedEventNames(object sender, NeedContextItemsArgs e)
        {
            GetCompletionOptions(sender, e, false, false, true);
        }

        /// <summary>
        /// The view is asking for items for its intellisense.
        /// </summary>
        /// <param name="sender">Editor that the user is typing in.</param>
        /// <param name="e">Event Arguments.</param>
        /// <param name="properties">Whether or not property suggestions should be generated.</param>
        /// <param name="methods">Whether or not method suggestions should be generated.</param>
        /// <param name="events">Whether or not event suggestions should be generated.</param>
        private void GetCompletionOptions(object sender, NeedContextItemsArgs e, bool properties, bool methods, bool events)
        {
            try
            {
                string currentLine = StringUtilities.GetLine(e.Code, e.LineNo - 1);
                currentEditor = sender;
                if (!e.ControlShiftSpace && intellisense.GenerateGridCompletions(currentLine, e.ColNo, report, properties, methods, events, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>The variable names have changed in the view.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnVariableNamesChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.ModelChanged -= new ModelChangedDelegate(OnModelChanged);
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "VariableNames", view.VariableList.Lines));
                explorerPresenter.CommandHistory.ModelChanged += new ModelChangedDelegate(OnModelChanged);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>The event names have changed in the view.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnEventNamesChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.ModelChanged -= new ModelChangedDelegate(OnModelChanged);
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "EventNames", view.EventList.Lines));
                explorerPresenter.CommandHistory.ModelChanged += new ModelChangedDelegate(OnModelChanged);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>The event names have changed in the view.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnGroupByChanged(object sender, EventArgs e)
        {
            try
            {
                explorerPresenter.CommandHistory.ModelChanged -= new ModelChangedDelegate(OnModelChanged);
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "GroupByVariableName", view.GroupByEdit.Text));
                explorerPresenter.CommandHistory.ModelChanged += new ModelChangedDelegate(OnModelChanged);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>The model has changed so update our view.</summary>
        /// <param name="changedModel">The model</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == report)
            {
                view.VariableList.Lines = report.VariableNames;
                view.EventList.Lines = report.EventNames;
            }
        }

        /// <summary>
        /// Invoked when the user selects an item in the intellisense.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            if (string.IsNullOrEmpty(args.ItemSelected))
                return;
            else if (string.IsNullOrEmpty(args.TriggerWord))
            {
                if (currentEditor is IEditorView)
                    (currentEditor as IEditorView).InsertAtCaret(args.ItemSelected);
                else
                    (currentEditor as IEditView).InsertAtCursor(args.ItemSelected);
            }
            else
            {
                if (currentEditor is IEditorView)
                    (currentEditor as IEditorView).InsertCompletionOption(args.ItemSelected, args.TriggerWord);
                else
                    (currentEditor as IEditView).InsertCompletionOption(args.ItemSelected, args.TriggerWord);
            }
        }

        /// <summary>
        /// Handles the adding of Report variables to the Report Variable Editor. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnCommonReportVariableListDoubleClicked(object sender, EventArgs args)
        {
            RowActivatedArgs rowActivatedArgs = args as RowActivatedArgs;
            int reportVariableIndex = rowActivatedArgs.Path.Indices[0];
            var currentReportVariablesLineNumber = this.view.VariableList.CurrentLineNumber;
            string variableCode = commonReportVariables.Rows[reportVariableIndex][2].ToString();
            List<String> plantVariableLines = new();
            // If variableCode contains '[Plant]' a line is added for each plant that is in the simulation.
            if (variableCode.Contains("[Plant"))
                plantVariableLines = CreatePlantVariableLines(variableCode);
            List<string> lines = view.VariableList.Lines.ToList();
            if (currentReportVariablesLineNumber > lines.Count)
                if (plantVariableLines.Count > 0)
                    foreach (string line in plantVariableLines)
                        lines.Add(line);
                else lines.Add(variableCode);
            else
                if (plantVariableLines.Count > 0)
                foreach (string line in plantVariableLines)
                    lines.Insert(currentReportVariablesLineNumber, line);
            else lines.Insert(currentReportVariablesLineNumber, variableCode);
            string modifiedText = string.Join(Environment.NewLine, lines);
            view.VariableList.Text = modifiedText;
            // Makes the selected line the newly added variable's line.
            if (plantVariableLines.Count > 0)
                // Makes the last line the one that is selected when multiples are added, such as when plant variable lines are added.
                view.VariableList.Location = new ManagerCursorLocation( variableCode.Length, currentReportVariablesLineNumber + (plantVariableLines.Count - 1));
            else 
                view.VariableList.Location = new ManagerCursorLocation ( variableCode.Length, currentReportVariablesLineNumber);
        }


        /// <summary>
        /// Handles the adding of Report Frequency variables to the Report Frequency Variable Editor. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnCommonReportFrequencyVariablesListDoubleClicked(object sender, EventArgs args)
        {
            RowActivatedArgs rowActivatedArgs = args as RowActivatedArgs;
            int reportVariableIndex = rowActivatedArgs.Path.Indices[0];
            var currentReportFrequencyVariablesLineNumber = this.view.EventList.CurrentLineNumber;
            string variableCode = commonReportFrequencyVariables.Rows[reportVariableIndex][2].ToString();
            List<string> lines = view.EventList.Lines.ToList();
            List<String> plantVariableLines = new();
            // If variableCode contains '[Plant]' a line is added for each plant that is in the simulation.
            if (variableCode.Contains("[Plant"))
                plantVariableLines = CreatePlantVariableLines(variableCode);
            if (currentReportFrequencyVariablesLineNumber > lines.Count)
                if (plantVariableLines.Count > 0)
                    foreach (string line in plantVariableLines)
                        lines.Add(line);
                else lines.Add(variableCode);
            else
                if (plantVariableLines.Count > 0)
                foreach (string line in plantVariableLines)
                    lines.Insert(currentReportFrequencyVariablesLineNumber, line);
            else lines.Insert(currentReportFrequencyVariablesLineNumber, variableCode);
            string modifiedText = string.Join(Environment.NewLine, lines);
            view.EventList.Text = modifiedText;
            if (plantVariableLines.Count > 0)
                // Makes the last line the one that is selected when multiples are added, such as when plant variable lines are added.
                view.EventList.Location = new ManagerCursorLocation (variableCode.Length, currentReportFrequencyVariablesLineNumber + (plantVariableLines.Count - 1) );
            else 
                view.EventList.Location = new ManagerCursorLocation (variableCode.Length, currentReportFrequencyVariablesLineNumber);
        }

        /// <summary>
        /// Creates a list of variable lines for each plant in the simulation.
        /// </summary>
        /// <param name="variableCode"></param>
        /// <returns></returns>
        private List<string> CreatePlantVariableLines(string variableCode)
        {
            List<string> plantCodeLines = new();
            List<IPlant> areaPlants = new();
            if (report.FindAncestor<Folder>() != null)
                areaPlants = explorerPresenter.ApsimXFile.FindAllInScope<IPlant>().ToList();
            else areaPlants = report.FindAncestor<Zone>().Plants;
            // Make sure plantsNames only has unique names. If under replacements you'll may have many many plants of the same name.
            areaPlants = areaPlants.GroupBy(plant => plant.Name).Select(plant => plant.First()).ToList();
            foreach (IPlant plant in areaPlants)
                if (variableCode.Contains(" as "))
                {
                    string newVariableCode = variableCode.Replace("[Plant]", $"[{plant.Name}]");
                    List<string> variableCodeSplits = newVariableCode.Split(' ').ToList();
                    string updatedAliasString = plant.Name + variableCodeSplits.Last();
                    variableCodeSplits[variableCodeSplits.Count - 1] = updatedAliasString;
                    plantCodeLines.Add(string.Join(' ', variableCodeSplits));
                }
                else plantCodeLines.Add(variableCode.Replace("[Plant]", $"[{plant.Name}]"));
            return plantCodeLines;
        }

        /// <summary>
        /// Returns a string list of all interface type names of ReportVariables in both
        /// CommonReportingVariables.json and CommonFrequencyVariables.json.
        /// </summary>
        /// <returns> A list of strings</returns>
        private List<string> GetAllInterfaceTypesFromCommonReportVariableLists()
        {
            List<string> allInterfaceNames = new();
            List<ReportVariable> combinedReportVariableLists = commonReportVariablesList.Concat(commonFrequencyVariableList).ToList();
            foreach (ReportVariable reportVariable in combinedReportVariableLists)
            {
                List<string> nodeStrings = reportVariable.Node.Split(",").ToList();
                // Some ReportVariables have multiple names under the node property. 
                foreach (string nodeString in nodeStrings)
                    // Tests if the node value matches the signature of an Interface name.
                    if (nodeString.StartsWith("I") && nodeString[1].IsAlphaUpper())
                        allInterfaceNames.Add(nodeString);
            }
            List<string> uniqueInterfaceName = allInterfaceNames.Distinct().ToList();
            return uniqueInterfaceName;

        }

        /// <summary>
        /// Fills the modelsImplementingSpecificInterfaceDictionary property with relevant data.
        /// </summary>
        /// <param name="uniqueInterfaceNames"></param>
        private void FillModelsImplementingSpecificInterfaceDictionary()
        {
            List<string> uniqueInterfaceNames = GetAllInterfaceTypesFromCommonReportVariableLists();
            foreach (string interfaceName in uniqueInterfaceNames)
            {
                modelsImplementingSpecificInterfaceDictionary.Add(interfaceName, GetInScopeModelImplementingInterface(interfaceName));
            }
        }

    }
}
