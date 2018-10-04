// -----------------------------------------------------------------------
// <copyright file="ReportPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Data;
    using APSIM.Shared.Utilities;
    using EventArguments;
    using Interfaces;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Models.Report;
    using Models.Storage;
    using Views;
    using System.Linq;

    /// <summary>
    /// The Report presenter class
    /// </summary>
    public class ReportPresenter : IPresenter
    {
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
        private IStorageReader dataStore;

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
            this.view.VariableList.ScriptMode = false;
            this.view.EventList.ScriptMode = false;
            this.view.VariableList.Lines = report.VariableNames;
            this.view.EventList.Lines = report.EventNames;
            this.view.VariableList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded += OnNeedEventNames;
            this.view.VariableList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser += OnEventNamesChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            Simulations simulations = Apsim.Parent(report, typeof(Simulations)) as Simulations;
            if (simulations != null)
            {
                dataStore = Apsim.Child(simulations, typeof(IStorageReader)) as IStorageReader;
            }
            
            //// TBI this.view.VariableList.SetSyntaxHighlighter("Report");

            dataStorePresenter = new DataStorePresenter();
            Simulation simulation = Apsim.Parent(report, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.Parent is Experiment)
                {
                    dataStorePresenter.ExperimentFilter = simulation.Parent as Experiment;
                }
                else
                {
                    dataStorePresenter.SimulationFilter = simulation;
                }
            }

            dataStorePresenter.Attach(dataStore, this.view.DataStoreView, explorerPresenter);
            this.view.DataStoreView.TableList.SelectedValue = this.report.Name;
            this.view.TabIndex = this.report.ActiveTabIndex;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.report.ActiveTabIndex = this.view.TabIndex;
            this.view.VariableList.ContextItemsNeeded -= OnNeedVariableNames;
            this.view.EventList.ContextItemsNeeded -= OnNeedEventNames;
            this.view.VariableList.TextHasChangedByUser -= OnVariableNamesChanged;
            this.view.EventList.TextHasChangedByUser -= OnEventNamesChanged;
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            dataStorePresenter.Detach();
            intellisense.Cleanup();
        }

        /// <summary>
        /// The view is asking for variable names.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            GetCompletionOptions(sender, e, true, false, false);
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
                if (intellisense.GenerateGridCompletions(currentLine, e.ColNo, report, properties, methods, events, e.ControlSpace))
                    intellisense.Show(e.Coordinates.Item1, e.Coordinates.Item2);
                intellisense.ItemSelected += (o, args) => 
                {
                    if (args.ItemSelected == string.Empty)
                        (sender as IEditorView).InsertAtCaret(args.ItemSelected);
                    else
                        (sender as IEditorView).InsertCompletionOption(args.ItemSelected, args.TriggerWord);
                };
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
                explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(OnModelChanged);
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "VariableNames", view.VariableList.Lines));
                explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(OnModelChanged);
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
                explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(OnModelChanged);
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(report, "EventNames", view.EventList.Lines));
                explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(OnModelChanged);
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
    }
}
