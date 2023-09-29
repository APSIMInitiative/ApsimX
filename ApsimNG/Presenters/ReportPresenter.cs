﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ApsimNG.Classes;
using Gtk;
using Models;
using Models.Core;
using Models.Factorial;
using Models.PMF;
using Models.Storage;
using Newtonsoft.Json;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;
using Utility;

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

        /// <summary>
        /// The report object
        /// </summary>
        private Report report;

        /// <summary>
        /// The report view
        /// </summary>
        private IReportView view;

        /// <summary>
        /// The explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// The data storage
        /// </summary>
        private IDataStore dataStore;

        /// <summary>
        /// The data store presenter object
        /// </summary>
        private DataStorePresenter dataStorePresenter;

        /// <summary>
        /// The intellisense object.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>Stores a DataTable of common report frequency variables.</summary>
        private DataTable commonReportFrequencyVariables;

        /// <summary>Stores a DataTable of common report variables.</summary>
        private DataTable commonReportVariables;

        /// <summary>
        /// Stores a list of  common ReportVariables.
        /// </summary>
        private List<ReportVariable> commonReportVariablesList;

        /// <summary>
        /// Stores a lst of common frequency ReportVariables.
        /// </summary>
        private List<ReportVariable> commonFrequencyVariableList;

        private List<string> simulationPlantModelNames;

        /// <summary>Currently selected ReportVariable. </summary>
        private ReportVariable CurrentlySelectedVariable { get; set; }

        /// <summary> File name for reporting variables.</summary>
        private readonly string commonReportVariablesFileName = "CommonReportingVariables.json";

        /// <summary> File name for report frequency variables.</summary>
        private readonly string commonReportFrequencyVariablesFileName = "CommonFrequencyVariables.json";

        /// <summary> Common directory path. </summary>
        private readonly string reportVariablesDirectoryPath = "ApsimNG\\Resources\\CommonReportVariables\\";

        // Returns all model names that are of type Plant.
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


        /// <summary>Stores variable name and variable code while being dragged.</summary>

        // Stores ReportDragObject to coping into EditorView.
        public ReportDragObject StoredDragObject { get; set; }

        /// <summary> Stores any dragged variables index.</summary>
        public int DraggedVariableIndex { get; set; }

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
            SimulationPlantModelNames = explorerPresenter.ApsimXFile.FindAllInScope<Plant>().Select(m => m.Name).ToList<string>();
            CommonReportVariablesList = GetCommonVariables(commonReportVariablesFileName, reportVariablesDirectoryPath);
            CommonFrequencyVariablesList = GetCommonVariables(commonReportFrequencyVariablesFileName, reportVariablesDirectoryPath);
            CommonReportVariables = GetReportVariables(CommonReportVariablesList, GetModelScopeNames(), true); // was async
            this.view.CommonReportVariablesList.DataSource = CommonReportVariables;
            this.view.CommonReportVariablesList.DragStart += OnCommonReportVariableListDragStart;
            CommonReportFrequencyVariables = GetReportVariables(CommonFrequencyVariablesList, GetModelScopeNames(), true); // was async
            this.view.CommonReportFrequencyVariablesList.DataSource = CommonReportFrequencyVariables;
            this.view.CommonReportFrequencyVariablesList.DragStart += OnCommonReportFrequencyVariableListDragStart;
            (this.view as ReportView).VariableList.VariableDragDataReceived += VariableListVariableDrop;
            (this.view as ReportView).EventList.VariableDragDataReceived += EventListVariableDrop;
            this.view.GroupByEdit.Text = report.GroupByVariableName;
            this.view.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded += OnNeedVariableNames;
            this.view.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.VariableList.TextHasChangedByUser += OnVariableListTextChanged;
            this.view.EventList.TextHasChangedByUser += OnEventNamesChanged;
            this.view.EventList.TextHasChangedByUser += OnEventListTextChanged;
            this.view.GroupByEdit.Changed += OnGroupByChanged;
            this.view.SplitterChanged += OnSplitterChanged;
            this.view.SplitterPosition = Configuration.Settings.ReportSplitterPosition;
            this.view.VerticalSplitterChanged += OnVerticalSplitterChanged;
            // Below check required to prevent commonReportVariables/Events
            // windows from occuping whole screen.
            if (Configuration.Settings.ReportSplitterVerticalPosition != 0)
                this.view.VerticalSplitterPosition = Configuration.Settings.ReportSplitterVerticalPosition;
            else
                this.view.VerticalSplitterPosition = 800;
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
        private void OnEventListTextChanged(object sender, EventArgs e) // was using an async keyword
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
                    DataTable potentialCommonFrequencyVariables = GetReportVariables(CommonFrequencyVariablesList, lineStrings, false);
                    if (potentialCommonFrequencyVariables.Rows.Count != 0)
                        CommonReportFrequencyVariables = potentialCommonFrequencyVariables;
                    else
                    {
                        DataRow emptyRow = potentialCommonFrequencyVariables.NewRow();
                        emptyRow["Description"] = "";
                        emptyRow["Code"] = "";
                        emptyRow["Type"] = "";
                        emptyRow["Units"] = "";
                        potentialCommonFrequencyVariables.Rows.Add(emptyRow);
                        CommonReportFrequencyVariables = potentialCommonFrequencyVariables;
                    }
                    view.CommonReportFrequencyVariablesList.DataSource = CommonReportFrequencyVariables;
                }
                else
                {
                    CommonReportFrequencyVariables = GetReportVariables(CommonFrequencyVariablesList, GetModelScopeNames(), true);
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

                    potentialCommonReportVariables = GetReportVariables(CommonReportVariablesList, lineStrings, false);
                    if (potentialCommonReportVariables.Rows.Count != 0)
                        CommonReportVariables = potentialCommonReportVariables;
                    else
                    {
                        DataRow emptyRow = potentialCommonReportVariables.NewRow();
                        emptyRow["Description"] = "";
                        emptyRow["Code"] = "";
                        emptyRow["Type"] = "";
                        emptyRow["Units"] = "";
                        potentialCommonReportVariables.Rows.Add(emptyRow);
                        CommonReportVariables = potentialCommonReportVariables;
                    }
                    view.CommonReportVariablesList.DataSource = CommonReportVariables;

                }
                else
                {
                    CommonReportVariables = GetReportVariables(CommonReportVariablesList, GetModelScopeNames(), true);
                    view.CommonReportVariablesList.DataSource = CommonReportVariables;
                }
            }
        }

        /// <summary>
        /// Adds the code from the StoredDragObject to the VariableList in the NewReportView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VariableListVariableDrop(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(StoredDragObject.Code))
            {
                List<string> textLinesList = view.VariableList.Lines.ToList();
                if (view.VariableList.CurrentLineNumber > textLinesList.Count)
                    textLinesList.Add(StoredDragObject.Code);
                else
                    textLinesList.Insert(view.VariableList.CurrentLineNumber, StoredDragObject.Code);
                string modifiedText = string.Join(Environment.NewLine, textLinesList);
                view.VariableList.Text = modifiedText;
                StoredDragObject = null;
            }
        }

        /// <summary>
        /// Adds the code from the StoredDragObject to the EventList in the NewReportView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventListVariableDrop(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(StoredDragObject.Code))
            {
                List<string> textLinesList = view.EventList.Lines.ToList();
                if (view.EventList.CurrentLineNumber > textLinesList.Count)
                    textLinesList.Add(StoredDragObject.Code);
                else
                    textLinesList.Insert(view.EventList.CurrentLineNumber, StoredDragObject.Code);
                string modifiedText = string.Join(Environment.NewLine, textLinesList);
                view.EventList.Text = modifiedText;
                StoredDragObject = null;
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

        private void OnSplitterChanged(object sender, EventArgs e)
        {
            Configuration.Settings.ReportSplitterPosition = this.view.SplitterPosition;
            Configuration.Settings.Save();
        }

        private void OnVerticalSplitterChanged(object sender, EventArgs e)
        {
            Configuration.Settings.ReportSplitterVerticalPosition = this.view.VerticalSplitterPosition;
            Configuration.Settings.Save();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.report.ActiveTabIndex = this.view.TabIndex;
            this.view.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded -= OnNeedVariableNames;
            this.view.SplitterChanged -= OnSplitterChanged;
            this.view.VerticalSplitterChanged -= OnVerticalSplitterChanged;
            this.view.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            this.view.GroupByEdit.Changed -= OnGroupByChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter?.Detach();
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
        }

        /// <summary>
        /// Takes a full DataTable with all columns and removes the Code column
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns> a DataTable</returns>
        public DataTable GetCommonReportVariablesWithoutCodeColumn(DataTable dataTable)
        {
            //DataTable modifiedDataTable = dataTable.Copy();
            //modifiedDataTable.Columns.Remove("Code");
            //return modifiedDataTable;
            return dataTable;
        }

        /// <summary>
        /// Creates a DataTable from the reportVariables inside a resource file name. 
        /// </summary>
        /// <param name="variableList"> List of ReportVariables</param>
        /// <param name="inputStrings">String List containing model names or properties to use as filters.</param>
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

                    if (inputStrings.Contains(reportVariable.ModelName))
                    {
                        DataRow row = variableDataTable.NewRow();
                        row["Description"] = reportVariable.Description;
                        row["Code"] = reportVariable.Code;
                        row["Type"] = reportVariable.Type;
                        row["Units"] = reportVariable.Units;
                        variableDataTable.Rows.Add(row);
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
            DataColumn codeColumn = new DataColumn("Code");
            DataColumn typeColumn = new DataColumn("Type");
            DataColumn unitsColumn = new DataColumn("Units");
            potentialCommonReportVariables.Columns.Add(descriptionColumn);
            potentialCommonReportVariables.Columns.Add(codeColumn);
            potentialCommonReportVariables.Columns.Add(typeColumn);
            potentialCommonReportVariables.Columns.Add(unitsColumn);
            return potentialCommonReportVariables;
        }

        /// <summary>
        /// Asynchronous version of GetReportVariables().
        /// </summary>
        /// <param name="variablesList"></param>
        /// <param name="filterStrings"></param>
        /// <param name="isModelScope"></param>
        /// <returns></returns>
        private async Task<DataTable> GetReportVariablesAsync(List<ReportVariable> variablesList, List<string> filterStrings, bool isModelScope)
        {
            return await Task.Run(() => GetReportVariables(variablesList, filterStrings, isModelScope));

        }


        private List<string> GetModelScopeNames()
        {
            List<string> modelNamesInScope = new();
            Simulations simulations = explorerPresenter.ApsimXFile;
            List<IModel> modelInScope = simulations.FindAllInScope<IModel>().ToList();
            modelNamesInScope = modelInScope.Select(x => x.Name).Distinct<string>().ToList();
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
                string currentLine = GetLine(e.Code, e.LineNo - 1);
                currentEditor = sender;
                if (!e.ControlShiftSpace && intellisense.GenerateGridCompletions(currentLine, e.ColNo, report, properties, methods, events, false, e.ControlSpace))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Gets a specific line of text, preserving empty lines.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="lineNo">0-indexed line number.</param>
        /// <returns>String containing a specific line of text.</returns>
        private string GetLine(string text, int lineNo)
        {
            // string.Split(Environment.NewLine.ToCharArray()) doesn't work well for us on Windows - Mono.TextEditor seems 
            // to use unix-style line endings, so every second element from the returned array is an empty string.
            // If we remove all empty strings from the result then we also remove any lines which were deliberately empty.

            // TODO : move this to APSIM.Shared.Utilities.StringUtilities?
            string currentLine;
            using (System.IO.StringReader reader = new System.IO.StringReader(text))
            {
                int i = 0;
                while ((currentLine = reader.ReadLine()) != null && i < lineNo)
                {
                    i++;
                }
            }
            return currentLine;
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
            string variableCode = commonReportVariables.Rows[reportVariableIndex][1].ToString();
            List<string> lines = view.VariableList.Lines.ToList();
            string modifiedText = string.Join(Environment.NewLine, lines);
            modifiedText += Environment.NewLine + variableCode;
            view.VariableList.Text = modifiedText;
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
            string variableCode = commonReportFrequencyVariables.Rows[reportVariableIndex][1].ToString();
            List<string> lines = view.EventList.Lines.ToList();
            string modifiedText = string.Join(Environment.NewLine, lines);
            modifiedText += Environment.NewLine + variableCode;
            view.EventList.Text = modifiedText;
        }

    }
}
