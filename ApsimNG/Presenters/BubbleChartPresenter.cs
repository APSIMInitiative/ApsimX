using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using UserInterface.Commands;
using UserInterface.EventArguments;
using Models.Interfaces;
using Models.Management;
using UserInterface.Views;
using UserInterface.Interfaces;
using APSIM.Shared.Graphing;
using Models.Core;
using ApsimNG.EventArguments.DirectedGraph;
using APSIM.Interop.Visualisation;

namespace UserInterface.Presenters
{
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
        /// Warpper around the currently selected node in the rotation manager so it can be
        /// editted in the view.
        /// </summary>
        private NodePropertyWrapper currentNode { get; set; }

        private PropertyPresenter propertiesPresenter = new PropertyPresenter();
        private PropertyPresenter nodePropertiesPresenter = new PropertyPresenter();

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

            this.view.GraphObjectSelected += OnGraphObjectSelected;
            this.view.GraphChanged += OnViewChanged;
            this.view.AddNode += OnAddNode;
            this.view.DelNode += OnDelNode;
            this.view.AddArcEnd += OnAddArc;
            this.view.DelArc += OnDelArc;

            this.view.RuleList.ContextItemsNeeded += OnNeedVariableNames;
            this.view.ActionList.ContextItemsNeeded += OnNeedEventNames;

            propertiesPresenter.Attach(this.model, this.view.PropertiesView, presenter);
            
            currentNode = new NodePropertyWrapper();
            nodePropertiesPresenter.Attach(currentNode, this.view.NodePropertiesView, presenter);

            intellisense = new IntellisensePresenter(view as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            presenter.CommandHistory.ModelChanged += OnModelChanged;

            RefreshView();

            this.view.Select(0); //have nothing selected
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.GraphChanged -= OnViewChanged;
            view.AddNode -= OnAddNode;
            view.DelNode -= OnDelNode;
            view.AddArcEnd -= OnAddArc;
            view.DelArc -= OnDelArc;
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
            view.RuleList.ContextItemsNeeded -= OnNeedVariableNames;
            view.ActionList.ContextItemsNeeded -= OnNeedEventNames;
            propertiesPresenter.Detach();
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
        /// <param name="args">Event arguments</param>
        private void OnGraphObjectSelected(object sender, GraphObjectsArgs args)
        {
            if (args.Objects != null)
            {
                if (args.Objects.Count == 1)
                {
                    string objectName = args.Objects[0].Name;
                    for (int i = 0; i < model.Nodes.Count; i++)
                    {
                        if (model.Nodes[i].Name == objectName)
                        {
                            currentNode = new NodePropertyWrapper();
                            currentNode.node = model.Nodes[i];
                            nodePropertiesPresenter.RefreshView(currentNode);
                            return;
                        }
                    }
                }
            }
            //else do nothing
            return;
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

            // Check for multiple nodes or arcs with the same name.
            IEnumerable<IGrouping<int, Node>> duplicateNodes = e.Nodes.GroupBy(n => n.ID).Where(g => g.Count() > 1);
            if (duplicateNodes.Any())
                throw new Exception($"Unable to apply changes - duplicate node name found: {duplicateNodes.First().Key}");
            IEnumerable<IGrouping<int, Arc>> duplicateArcs = e.Arcs.GroupBy(a => a.ID).Where(g => g.Count() > 1);
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
            List<Node> newNodes = new List<Node>();
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
        private void OnDelNode(object sender, GraphObjectsArgs e)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            List<Arc> newArcs = new List<Arc>(model.Arcs);
            List<Node> newNodes = new List<Node>(model.Nodes);
            foreach (var obj in e.Objects)
            {
                int idToDelete = obj.ID;

                for(int i = newNodes.Count-1; i >= 0; i--)
                    if (newNodes[i].ID == idToDelete)
                        newNodes.Remove(newNodes[i]);

                for (int i = newArcs.Count-1; i >= 0; i--)
                    if (newArcs[i].ID == idToDelete || newArcs[i].SourceID == idToDelete || newArcs[i].DestinationID == idToDelete)
                        newArcs.Remove(newArcs[i]);               
            }
            changes.Add(new ChangeProperty.Property(model, nameof(model.Nodes), newNodes));
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
            if (model.Nodes.Find(n => n.ID == e.Arc.SourceID) == null)
                throw new Exception("BubbleChartPresenter: Source empty in arc new Arc");

            if (model.Nodes.Find(n => n.ID == e.Arc.DestinationID) == null)
                throw new Exception("BubbleChartPresenter: Destination empty in arc new Arc");

            List<Arc> newArcs = new List<Arc>();
            newArcs.AddRange(model.Arcs);
            Arc existingArc = newArcs.Find(a => a.ID == e.Arc.ID);
            if (existingArc == null)
                newArcs.Add(new Arc(e.Arc));
            else
                existingArc.CopyFrom(new Arc(e.Arc));

            ICommand addArc = new ChangeProperty(model, nameof(model.Arcs), newArcs);
            presenter.CommandHistory.Add(addArc);
        }

        /// <summary>
        /// An arc has been deleted. Propagate this change to the model.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event data.</param>
        private void OnDelArc(object sender, GraphObjectsArgs e)
        {
            List<ChangeProperty.Property> changes = new List<ChangeProperty.Property>();
            foreach (var obj in e.Objects)
            {
                int idToDelete = obj.ID;
                List<Arc> newArcs = new List<Arc>();
                newArcs.AddRange(model.Arcs);
                newArcs.RemoveAll(delegate (Arc a) { return (a.ID == idToDelete); });
                changes.Add(new ChangeProperty.Property(model, nameof(model.Arcs), newArcs));
            }
            ICommand removeArcs = new ChangeProperty(changes);
            presenter.CommandHistory.Add(removeArcs);
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
                        intellisense.GenerateGridCompletions(currentLine, e.ColNo, model, true, false, false, false, e.ControlSpace) :
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

        /// <summary>
        /// A wrapper for the currently selected node in rotation manager so that
        /// it can be used with a PropertyPresenter view.
        /// 
        /// Although this uses the model class, it should not be added as a model
        /// within a simulation file, as by itself it does nothing.
        /// </summary>
        [Serializable]
        public class NodePropertyWrapper : Model
        {
            /// <summary>Currently selected node in the Rotation Manager</summary>
            public Node node = null;

            /// <summary>Property wrapper for name</summary>
            [Description("Name")]
            [Display(Type = DisplayType.None)]
            public string NodeName
            {
                get
                {
                    if (node != null)
                        return node.Name;
                    else
                        return string.Empty;
                }
                set
                {
                    if (node != null)
                        node.Name = value;
                }
            }

            /// <summary>Property wrapper for description</summary>
            [Description("Description")]
            [Display(Type = DisplayType.None)]
            public string Description
            {
                get
                {
                    if (node != null)
                        return node.Description;
                    else
                        return string.Empty;
                }
                set
                {
                    if (node != null)
                        node.Description = value;
                }
            }

            /// <summary>Property wrapper for name</summary>
            [Description("Name")]
            [Display(Type = DisplayType.None)]
            public Color Colour
            {
                get
                {
                    if (node != null)
                        return node.Colour;
                    else
                        return Color.FromName("Black");
                }
                set
                {
                    if (node != null)
                        node.Colour = value;
                }
            }

        }
    }
}