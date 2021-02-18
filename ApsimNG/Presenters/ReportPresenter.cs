namespace UserInterface.Presenters
{
    using System;
    using EventArguments;
    using Models.Core;
    using Models.Factorial;
    using Models.Storage;
    using Utility;
    using Views;
    using Models;
    using Interfaces;

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

        /// <summary>
        /// Attach the model (report) and the view (IReportView)
        /// </summary>
        /// <param name="model">The report model object</param>
        /// <param name="view">The view object</param>
        /// <param name="explorerPresenter">The explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
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
            this.view.GroupByEdit.Text = report.GroupByVariableName;
            this.view.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.view.GroupByEdit.IntellisenseItemsNeeded += OnNeedVariableNames;
            this.view.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser += OnEventNamesChanged;
            this.view.GroupByEdit.Changed += OnGroupByChanged;
            this.view.SplitterChanged += OnSplitterChanged;
            this.view.SplitterPosition = Configuration.Settings.ReportSplitterPosition;
            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

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

        private void OnSplitterChanged(object sender, EventArgs e)
        {
            Configuration.Settings.ReportSplitterPosition = this.view.SplitterPosition;
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
            dataStorePresenter.Detach();
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
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
    }
}
