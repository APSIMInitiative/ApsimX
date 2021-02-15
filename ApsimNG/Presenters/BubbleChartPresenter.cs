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
    using EventArguments.DirectedGraph;
    using Models;
    using Models.Core;
    using Models.Interfaces;
    using Models.Management;
    using Views;
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

        private SimplePropertyPresenter propertiesPresenter = new SimplePropertyPresenter();

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="presenter">The explorer presenter being used</param>
        public void Attach(object model, object view, ExplorerPresenter presenter)
        {
            this.view = view as IBubbleChartView;
            this.presenter = presenter;
            this.model = model as IBubbleChart;

            this.view.OnGraphChanged += OnViewChanged;
            this.view.AddNode += OnAddNode;
            this.view.DelNode += OnDelNode;
            this.view.AddArc += OnAddArc;
            this.view.DelArc += OnDelArc;

            this.view.RuleList.ContextItemsNeeded += OnNeedVariableNames;
            //view.RuleList.TextHasChangedByUser += OnVariableNamesChanged;
            this.view.ActionList.ContextItemsNeeded += OnNeedEventNames;

            propertiesPresenter.Attach(this.model, this.view.PropertiesView, presenter);
            //view.ActionList.TextHasChangedByUser += OnEventNamesChanged;

            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            presenter.CommandHistory.ModelChanged += OnModelChanged;

            RefreshView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.OnGraphChanged -= OnViewChanged;
            view.AddNode -= OnAddNode;
            view.DelNode -= OnDelNode;
            view.AddArc -= OnAddArc;
            view.DelArc -= OnDelArc;
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            view.RuleList.ContextItemsNeeded -= OnNeedVariableNames;
            view.ActionList.ContextItemsNeeded -= OnNeedEventNames;
            propertiesPresenter.Detach();

            // Shouldn't need to manually update the model at this point.
            // All changes are applied immediately upon user input.
            //OnViewChanged(this, new GraphChangedEventArgs() { Nodes = view.Nodes, Arcs = view.Arcs});
        }

        /// <summary>
        /// The model has been changed. Refresh the view.
        /// </summary>
        /// <param name="changedModel"></param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == model)
                RefreshView();
        }

        /// <summary>
        /// Refresh the view with the model's current state.
        /// </summary>
        private void RefreshView()
        {
            view.SetGraph(model.Nodes, model.Arcs);
            propertiesPresenter.RefreshView(model);
        }

        /// <summary>
        /// The user has made changes in the view.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnViewChanged(object sender, GraphChangedEventArgs e)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            changes.Add(new ChangeProperty.Property(model, nameof(model.Arcs), e.Arcs));
            changes.Add(new ChangeProperty.Property(model, nameof(model.Nodes), e.Nodes));
            // todo - update InitialState as well? This function is currently
            // not used a a callback for initial state being changed, but it
            // might make sense to update it anyway...

            // Check for multiple nodes or arcs with the same name.
            IEnumerable<IGrouping<string, StateNode>> duplicateNodes = e.Nodes.GroupBy(n => n.Name).Where(g => g.Count() > 1);
            if (duplicateNodes.Any())
                throw new Exception($"Unable to apply changes - duplicate node name found: {duplicateNodes.First().Key}");
            IEnumerable<IGrouping<string, RuleAction>> duplicateArcs = e.Arcs.GroupBy(n => n.Name).Where(g => g.Count() > 1);
            if (duplicateArcs.Any())
                throw new Exception($"Unable to apply changes - duplicate arc name found: {duplicateArcs.First().Key}");

            ChangeProperty command = new ChangeProperty(changes);
            presenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// A new node has been added. Propagate this change to the model.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAddNode(object sender, AddNodeEventArgs e)
        {
            List<StateNode> newNodes = new List<StateNode>();
            newNodes.AddRange(model.Nodes);
            newNodes.Add(e.Node);
            ICommand addNode = new ChangeProperty(model, nameof(model.Nodes), newNodes);
            presenter.CommandHistory.Add(addNode);
        }

        /// <summary>
        /// A node has been deleted. Propagate this change to the model.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnDelNode(object sender, DelNodeEventArgs e)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            
            List<StateNode> newNodes = new List<StateNode>();
            newNodes.AddRange(model.Nodes);
            newNodes.RemoveAll(n => n.Name == e.nodeNameToDelete);
            changes.Add(new ChangeProperty.Property(model, nameof(model.Nodes), newNodes));

            // Need to also delete any arcs going to/from this node.
            List<RuleAction> newArcs = new List<RuleAction>();
            newArcs.AddRange(model.Arcs);
            newArcs.RemoveAll(a => a.DestinationName == e.nodeNameToDelete || a.SourceName == e.nodeNameToDelete);
            changes.Add(new ChangeProperty.Property(model, nameof(model.Arcs), newArcs));

            ICommand removeNode = new ChangeProperty(changes);
            presenter.CommandHistory.Add(removeNode);
        }

        /// <summary>
        /// An arc has been added. Propagate this change to the model.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnAddArc(object sender, AddArcEventArgs e)
        {
            // Ensure that existing source/dest nodes exist.
            // todo - does this belong inside RotationManager??
            if (model.Nodes.Find(n => n.Name == e.Arc.SourceName) == null ||
                model.Nodes.Find(n => n.Name == e.Arc.DestinationName) == null)
                throw new Exception("Target empty in arc");

            List<RuleAction> newArcs = new List<RuleAction>();
            newArcs.AddRange(model.Arcs);
            RuleAction existingArc = newArcs.Find(a => a.Name == e.Arc.Name);
            if (existingArc == null)
                newArcs.Add(new RuleAction(e.Arc));
            else
                existingArc.CopyFrom((Arc)new RuleAction(e.Arc));

            ICommand addArc = new ChangeProperty(model, nameof(model.Arcs), newArcs);
            presenter.CommandHistory.Add(addArc);
        }

        /// <summary>
        /// An arc has been deleted. Propagate this change to the model.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnDelArc(object sender, DelArcEventArgs e)
        {
            // fixme - not undoable
            List<RuleAction> newArcs = new List<RuleAction>();
            newArcs.AddRange(model.Arcs);
            newArcs.RemoveAll(delegate (RuleAction a) { return (a.Name == e.arcNameToDelete); });
            ICommand deleteArc = new ChangeProperty(model, nameof(model.Arcs), newArcs);
            presenter.CommandHistory.Add(deleteArc);
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
        /// <param name="rules">Controls whether rules (events) will be shown in intellisense</param>
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