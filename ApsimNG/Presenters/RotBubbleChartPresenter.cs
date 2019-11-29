// -----------------------------------------------------------------------
// <copyright file="RotBubbleChartPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Models.Graph;
    using Views;
    using System.IO;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ICSharpCode.NRefactory.CSharp;

    /// <summary>
    /// Presenter for the rotation bubble chart component
    /// </summary>
    public class RotBubbleChartPresenter : IPresenter 
    {
        /// <summary>
        /// The view for the manager
        /// </summary>
        private RotBubbleChartView view;

        /// <summary>The explorer presenter used</summary>
        private ExplorerPresenter presenter;

        /// <summary>The model used</summary>
        private RotBubbleChart model;

        /// <summary>
        /// Handles generation of completion options for the view.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Used by the intellisense to keep track of which editor the user is currently using.
        /// Without this, it's difficult to know which editor (variables or events) to
        /// insert an intellisense item into.
        /// </summary>
        private IEditorView currentEditor;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="_model">The model</param>
        /// <param name="_view">The view to attach</param>
        /// <param name="_presenter">The explorer presenter being used</param>
        public void Attach(object _model, object _view, ExplorerPresenter _presenter)
        {
            view = _view as RotBubbleChartView;
            presenter = _presenter;
            model = _model as RotBubbleChart;

            view.OnGraphChanged += OnModelChanged;
            view.OnInitialStateChanged += OnInitialStateChanged;
            view.AddNode += OnAddNode;
            view.DelNode += OnDelNode;
            view.AddArc += OnAddArc;
            view.DelArc += OnDelArc;

            view.RuleList.ContextItemsNeeded += OnNeedVariableNames;
            //view.RuleList.TextHasChangedByUser += OnVariableNamesChanged;
            view.ActionList.ContextItemsNeeded += OnNeedEventNames;
            //view.ActionList.TextHasChangedByUser += OnEventNamesChanged;

            // Tell the view to populate the axis.
            view.Graph = model.getGraph();
            view.InitialState = model.InitialState;

            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            //presenter.CommandHistory.ModelChanged += CommandHistory_ModelChanged;
        }
        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.OnGraphChanged -= OnModelChanged;
            view.OnInitialStateChanged -= OnInitialStateChanged;
            view.AddNode -= OnAddNode;
            view.DelNode -= OnDelNode;
            view.AddArc -= OnAddArc;
            view.DelArc -= OnDelArc;
            //presenter.CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            view.RuleList.ContextItemsNeeded -= OnNeedVariableNames;
            view.ActionList.ContextItemsNeeded -= OnNeedEventNames;

            model.setGraph(view.Graph);
            model.InitialState = view.InitialState;
        }
        /// <summary>
        /// The view has changed the model (associated rules/actions)
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnModelChanged(object sender, GraphChangedEventArgs e)
        {
            model.setGraph(e.model);
            view.Graph = model.getGraph();
        }

        /// <summary>
        /// A new node has been added
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnAddNode(object sender, AddNodeEventArgs e)
        {
            try
            {
//                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
//                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            model.AddNode(e.Node);
            view.Graph = model.getGraph();
        }

        private void OnDelNode(object sender, DelNodeEventArgs e)
        {
            try
            {
//                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
//                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            model.DelNode( e.nodeNameToDelete );
            view.Graph = model.getGraph();
        }

        private void OnAddArc(object sender, AddArcEventArgs e)
        {
            try
            {
//                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
//                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            Models.RotBubbleChart.RuleAction v = new RotBubbleChart.RuleAction(e.Arc);
            model.AddRuleAction(v);
            view.Graph = model.getGraph();
        }

        private void OnDelArc(object sender, DelArcEventArgs e)
        {
            try
            {
//                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
//                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
            model.DelRuleAction(e.arcNameToDelete);
            view.Graph = model.getGraph();
        }
        /// <summary>
        /// The view has changed the initial state
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnInitialStateChanged(object sender, InitialStateEventArgs e)
        {
            model.InitialState = e.initialState;
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The changed manager model</param>
        public void CommandHistory_ModelChanged(object changedModel)
        {
#if false
            if (changedModel == manager)
            {
                managerView.Editor.Text = manager.Code;
            }
            else if (changedModel == scriptModel)
            {
                propertyPresenter.UpdateModel(scriptModel);
            }
#endif
        }
        /// <summary>
        /// The view is asking for variable names.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            GetCompletionOptions(sender, e, true);
        }

        /// <summary>The view is asking for event names.</summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument values</param>
        private void OnNeedEventNames(object sender, NeedContextItemsArgs e)
        {
            GetCompletionOptions(sender, e, false);
        }

        /// <summary>
        /// The view is asking for items for its intellisense.
        /// </summary>
        /// <param name="sender">Editor that the user is typing in.</param>
        /// <param name="e">Event Arguments.</param>
        /// <param name="properties">Whether or not property suggestions should be generated.</param>
        /// <param name="methods">Whether or not method suggestions should be generated.</param>
        /// <param name="events">Whether or not event suggestions should be generated.</param>
        private void GetCompletionOptions(object sender, NeedContextItemsArgs e, bool rules)
        {
            try
            {
                string currentLine = GetLine(e.Code, e.LineNo - 1);
                currentEditor = sender as IEditorView;
                if (!e.ControlShiftSpace && 
                     (rules ? 
                        intellisense.GenerateGridCompletions(currentLine, e.ColNo, model , true, false, false, false, e.ControlSpace) :
                        intellisense.GenerateGridCompletions(currentLine, e.ColNo, model, false, true, false, true, e.ControlSpace)))
                    intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
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

        /// <summary>
        /// The view is asking for items for the intellisense.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseNeeded(object sender, NeedContextItemsArgs args)
        {
            try
            {
                //fixme if (intellisense.GenerateSeriesCompletions(args.Code, args.Offset, view.TableList.SelectedValue, dataStore.Reader))
                    intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.ItemSelected))
                    return;
                else if (string.IsNullOrEmpty(args.TriggerWord))
                    currentEditor.InsertAtCaret(args.ItemSelected);
                else
                    currentEditor.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }
    }
}