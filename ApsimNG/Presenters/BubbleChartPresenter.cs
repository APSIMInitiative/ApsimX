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
    using Models.Management;
    using Views;
    using System.IO;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ICSharpCode.NRefactory.CSharp;
    using Interfaces;

    /// <summary>
    /// Presenter for the rotation bubble chart component
    /// </summary>
    public class BubbleChartPresenter : IPresenter 
    {
        /// <summary>
        /// The view for the manager
        /// </summary>
        private IBubbleChartView view;

        /// <summary>The explorer presenter used</summary>
        private ExplorerPresenter presenter;

        /// <summary>The model used</summary>
        private IBubbleChart model;

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
        public void Attach(object model, object view, ExplorerPresenter presenter)
        {
            this.view = view as IBubbleChartView;
            this.presenter = presenter;
            this.model = model as IBubbleChart;

            this.view.OnGraphChanged += OnModelChanged;
            this.view.OnInitialStateChanged += OnInitialStateChanged;
            this.view.AddNode += OnAddNode;
            this.view.DelNode += OnDelNode;
            this.view.AddArc += OnAddArc;
            this.view.DelArc += OnDelArc;

            this.view.RuleList.ContextItemsNeeded += OnNeedVariableNames;
            //view.RuleList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.ActionList.ContextItemsNeeded += OnNeedEventNames;
            //view.ActionList.TextHasChangedByUser += OnEventNamesChanged;

            // Tell the view to populate the axis.
            this.view.SetGraph(this.model.Nodes, this.model.Arcs);
            this.view.InitialState = this.model.InitialState;

            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            // fixme - we can replace most/all calls to RefreshViews() by trapping
            // the ModelChanged event of the explorer presenter's command history.
            // Then we just call RefreshView() once, inside the callback to this event.
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

            model.Arcs = view.Arcs;
            model.Nodes = view.Nodes;

            model.InitialState = view.InitialState;
        }
        /// <summary>
        /// The view has changed the model (associated rules/actions)
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnModelChanged(object sender, GraphChangedEventArgs e)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            // fixme - nameof()
            changes.Add(new ChangeProperty.Property(model, "Arcs", view.Arcs));
            changes.Add(new ChangeProperty.Property(model, "Nodes", view.Nodes));
            ChangeProperty command = new ChangeProperty(changes);
            this.presenter.CommandHistory.Add(command);
            RefreshView();
        }

        private void RefreshView()
        {
            view.SetGraph(model.Nodes, model.Arcs);
        }

        /// <summary>
        /// A new node has been added
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnAddNode(object sender, AddNodeEventArgs e)
        {
            // fixme - need to use a ChangeProperty command, otherwise this won't be undoable.
            model.Nodes.Add(e.Node);
            RefreshView();
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

            model.Nodes.RemoveAll(n => n.Name == e.nodeNameToDelete);
            RefreshView();
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
            RuleAction v = new RuleAction(e.Arc);
            model.AddRuleAction(v);
            RefreshView();
        }

        private void OnDelArc(object sender, DelArcEventArgs e)
        {
            // fixme - not undoable
            model.Arcs.RemoveAll(delegate (RuleAction a) { return (a.Name == e.arcNameToDelete); });
            RefreshView();
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