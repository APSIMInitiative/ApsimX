using System;
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
using Models.Core.ApsimFile;
using Models.Factorial;
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

        /// <summary> File name for reporting variables.</summary>
        private readonly string commonReportVariablesFilePath = "CommonReportingVariables.json";

        /// <summary> File name for report frequency variables.</summary>
        private readonly string commonReportFrequencyVariablesFilePath = "CommonFrequencyVariables.json";

        /// <summary>Stores variable name and variable code while being dragged.</summary>
        private ReportVariable CurrentlySelectedVariable { get; set; }

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
        public async void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.report = model as Report;
            this.explorerPresenter = explorerPresenter;
            CommonReportVariables = await GetReportVariablesAsync(commonReportVariablesFilePath, GetModelScopeNames());
            CommonReportFrequencyVariables = await GetReportVariablesAsync(commonReportFrequencyVariablesFilePath, GetModelScopeNames());
            this.view = view as IReportView;
            this.intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;
            this.view.VariableList.Mode = EditorType.Report;
            this.view.EventList.Mode = EditorType.Report;
            this.view.VariableList.Lines = report.VariableNames;
            this.view.EventList.Lines = report.EventNames;
            this.view.CommonReportVariablesList.DataSource = CommonReportVariables;
            this.view.CommonReportVariablesList.DragStart += OnCommonReportVariableListDragStart;
            this.view.CommonReportFrequencyVariablesList.DataSource = CommonReportFrequencyVariables;
            this.view.CommonReportFrequencyVariablesList.DragStart += OnCommonReportFrequencyVariableListDragStart;
            (this.view as NewReportView).VariableList.VariableDragDataReceived += VariableListVariableDrop;
            (this.view as NewReportView).EventList.VariableDragDataReceived += EventListVariableDrop;
            this.view.GroupByEdit.Text = report.GroupByVariableName;
            this.view.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded += OnNeedVariableNames;
            this.view.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.VariableList.TextHasChangedByUser += OnVariableListTextChanged;
            //(this.view.VariableList as EditorView).GetSourceView().DragDrop += OnVariableListDrop;
            this.view.EventList.TextHasChangedByUser += OnEventNamesChanged;
            this.view.EventList.TextHasChangedByUser += OnEventListTextChanged;
            this.view.GroupByEdit.Changed += OnGroupByChanged;
            this.view.SplitterChanged += OnSplitterChanged;
            this.view.SplitterPosition = Configuration.Settings.ReportSplitterPosition;
            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            this.view.CommonReportVariablesList.DoubleClicked += OnCommonReportVariableListDoubleClicked;
            this.view.CommonReportFrequencyVariablesList.DoubleClicked += OnCommonReportFrequencyVariablesListDoubleClicked;

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


        /// <summary>
        /// Intended to handle what happens to common report frequency variables when text is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnEventListTextChanged(object sender, EventArgs e)
        {
            int frequencyVariableListLineNumber = view.EventList.CurrentLineNumber;
            int lineIndex = frequencyVariableListLineNumber - 1;
            if (lineIndex < view.EventList.Lines.Count())
            {
                string lineContent = view.EventList.Lines[lineIndex];
                List<string> lineStrings = new();
                if (!lineContent.Equals(".") && !string.IsNullOrWhiteSpace(lineContent))
                    lineStrings = lineContent.Split(". ").ToList();
                // Used async wrapper method to avoid sluggish GUI.
                if (lineStrings.Count != 0)
                {
                    commonReportFrequencyVariables = await GetReportVariablesAsync(commonReportFrequencyVariablesFilePath, lineStrings);
                    view.CommonReportFrequencyVariablesList.DataSource = commonReportFrequencyVariables;
                }
                else
                {
                    commonReportFrequencyVariables = await GetReportVariablesAsync(commonReportFrequencyVariablesFilePath, GetModelScopeNames());
                    view.CommonReportFrequencyVariablesList.DataSource = commonReportFrequencyVariables;
                }

            }
        }

        /// <summary>
        /// Intended to handle what happens to common report variables when text is changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnVariableListTextChanged(object sender, EventArgs e)
        {
            int variableListLineNumber = view.VariableList.CurrentLineNumber;
            int lineIndex = variableListLineNumber - 1;
            int linesCount = view.VariableList.Lines.Length;
            if (lineIndex < linesCount)
            {
                string lineContent = this.view.VariableList.Lines[lineIndex];
                List<string> lineStrings = new();
                if (!lineContent.Equals(".") && !string.IsNullOrWhiteSpace(lineContent))
                    lineStrings = lineContent.Split(". ").ToList();
                // Used async wrapper method to avoid sluggish GUI.
                if (lineStrings.Count != 0)
                {
                    commonReportVariables = await GetReportVariablesAsync(commonReportVariablesFilePath, lineStrings);
                    view.CommonReportVariablesList.DataSource = commonReportVariables;
                }
                else
                {
                    commonReportVariables = await GetReportVariablesAsync(commonReportVariablesFilePath, GetModelScopeNames());
                    view.CommonReportVariablesList.DataSource = commonReportVariables;
                }

            }
        }

        /// <summary>
        /// Adds the code from the StoredDragObject to the EventList in the NewReportView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VariableListVariableDrop(object sender, EventArgs e)
        {
            if (StoredDragObject != null)
            {
                List<string> textLinesList = view.VariableList.Lines.ToList();
                textLinesList.Insert(view.VariableList.CurrentLineNumber, StoredDragObject.Code);
                string modifiedText = string.Join(Environment.NewLine, textLinesList);
                view.VariableList.Text = modifiedText;
                //view.VariableList.Text = view.VariableList.Text + Environment.NewLine + StoredDragObject.Code;
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
                textLinesList.Insert(view.EventList.CurrentLineNumber, StoredDragObject.Code);
                string modifiedText = string.Join(Environment.NewLine, textLinesList);
                view.EventList.Text = modifiedText;
                //view.VariableList.Text = view.VariableList.Text + Environment.NewLine + StoredDragObject.Code;
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

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.report.ActiveTabIndex = this.view.TabIndex;
            this.view.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded -= OnNeedVariableNames;
            this.view.SplitterChanged -= OnSplitterChanged;
            this.view.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            this.view.GroupByEdit.Changed -= OnGroupByChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter?.Detach();
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
        }

        /// <summary>
        /// Creates a DataTable with the reportVariables inside a resource file name. 
        /// </summary>
        /// <param name="fileName"> The name including the json extension.</param>
        /// <param name="filterStrings">String List containing model names or properties to use as filters.</param>
        /// <returns>A <see cref="DataTable"/> containing commonReportVariables or commonReportFrequencyVariables.</returns>
        public DataTable GetReportVariables(string fileName, List<string> filterStrings)
        {
            try
            {
                // reportFrequencyVariables name = "CommonFrequencyVariables.json"
                // reportVariables name = "CommonReportingVariables.json"
                string reportingVariablesJSON = null;
                string currentAssemblyDirectory = Assembly.GetExecutingAssembly().Location.Split("bin")[0];
                string commonReportingVariablesFilePath = Path.Combine(currentAssemblyDirectory, "ApsimNG\\Resources\\CommonReportVariables\\", fileName);

                // Build a DataTable to replace the private variable
                DataTable variableDataTable = new DataTable();

                // Get the resource file contents into a JSON Object.
                reportingVariablesJSON = File.ReadAllText(commonReportingVariablesFilePath);
                List<ReportVariable> reportVariableList = JsonConvert.DeserializeObject<List<ReportVariable>>(reportingVariablesJSON);
                DataColumn reportingVariableNameColumn = new DataColumn("Description");
                DataColumn reportingVariableCodeColumn = new DataColumn("Code");
                variableDataTable.Columns.Add(reportingVariableNameColumn);
                variableDataTable.Columns.Add(reportingVariableCodeColumn);

                foreach (ReportVariable reportVariable in reportVariableList)
                {
                    bool isReportVariableRelevant = false;
                    // Find only relevant variables based on models in simulation.
                    List<string> relevantNames = filterStrings;
                    //Check the description
                    foreach (string modelName in relevantNames)
                    {
                        if (reportVariable.Description.Contains(modelName) || reportVariable.Code.Contains(modelName))
                            isReportVariableRelevant = true;
                        if (isReportVariableRelevant)
                        {
                            // Check that DataTable does not already 
                            bool isReportVariableAlreadyAdded = false;
                            foreach (DataRow tableRow in variableDataTable.Rows)
                            {
                                if (tableRow[0].ToString().Contains(reportVariable.Description))
                                    isReportVariableAlreadyAdded = true;
                            }

                            if (isReportVariableAlreadyAdded == false)
                            {
                                // Add to row if relevant.
                                DataRow row = variableDataTable.NewRow();
                                row["Description"] = reportVariable.Description;
                                row["Code"] = reportVariable.Code;
                                variableDataTable.Rows.Add(row);
                            }
                        }
                    }
                }
                return variableDataTable;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        /// <summary>
        /// Asynchronous version of GetReportVariables().
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filterStrings"></param>
        /// <returns></returns>
        private async Task<DataTable> GetReportVariablesAsync(string fileName, List<string> filterStrings)
        {
            return await Task.Run(() => GetReportVariables(fileName, filterStrings));
        }


        private List<string> GetModelScopeNames()
        {
            List<string> modelNamesInScope = new();
            Simulations simulations = FileFormat.ReadFromFile<Simulations>(explorerPresenter.ApsimXFile.FileName, e => throw e, false).NewModel as Simulations;
            List<IModel> modelInScope = simulations.FindAllInScope<IModel>().ToList();
            modelNamesInScope = modelInScope.Select(x => x.Name).ToList();
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
            // get the index of the common report variable selected from the args.
            int reportVariableIndex = rowActivatedArgs.Path.Indices[0];
            // Get the line number for the report variable editorView.
            var currentReportVariablesLineNumber = this.view.VariableList.CurrentLineNumber;
            // Find the coressponding variable code to be inserted into the report variables view.
            string variableCode = commonReportVariables.Rows[reportVariableIndex][1].ToString();
            // Get the contents of the currently selected line.
            string currentlineContent = "";
            if (currentReportVariablesLineNumber <= this.view.VariableList.Lines.Length)
                currentlineContent = this.view.VariableList.Lines[currentReportVariablesLineNumber - 1];

            // Insert the code below the currenLine if line is not null.
            if (string.IsNullOrWhiteSpace(currentlineContent))
                this.view.VariableList.Text = this.view.VariableList.Text + variableCode;
            else
                this.view.VariableList.Text = this.view.VariableList.Text + "\n" + variableCode;
        }

        /// <summary>
        /// Handles the adding of Report Frequency variables to the Report Frequency Variable Editor. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnCommonReportFrequencyVariablesListDoubleClicked(object sender, EventArgs args)
        {
            RowActivatedArgs rowActivatedArgs = args as RowActivatedArgs;
            // get the index of the common report variable selected from the args.
            int reportVariableIndex = rowActivatedArgs.Path.Indices[0];
            // Get the line number for the report variable editorView.
            var currentReportFrequencyVariablesLineNumber = this.view.EventList.CurrentLineNumber;
            // Find the coressponding variable code to be inserted into the report variables view.
            string variableCode = commonReportFrequencyVariables.Rows[reportVariableIndex][1].ToString();
            // Get the contents of the currently selected line.
            string currentlineContent = "";
            if (currentReportFrequencyVariablesLineNumber <= this.view.EventList.Lines.Length)
                currentlineContent = this.view.EventList.Lines[currentReportFrequencyVariablesLineNumber - 1];

            // Insert the code below the currenLine if line is not null.
            if (string.IsNullOrWhiteSpace(currentlineContent))
                this.view.EventList.Text = this.view.EventList.Text + variableCode;
            else
                this.view.EventList.Text = this.view.EventList.Text + "\n" + variableCode;

        }
    }
}
