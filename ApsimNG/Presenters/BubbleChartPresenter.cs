using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using UserInterface.Commands;
using UserInterface.EventArguments;
using Models.Interfaces;
using UserInterface.Views;
using UserInterface.Interfaces;
using APSIM.Shared.Graphing;
using Models.Core;
using ApsimNG.EventArguments.DirectedGraph;
using Shared.Utilities;
using APSIM.Shared.Utilities;

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
        /// Will be Conditions or Actions
        /// </summary>
        private string currentEditor;
        ManagerCursorLocation editorCursor;
        private EditorView conditionsEditor;
        private EditorView actionsEditor;

        /// <summary>
        /// Warpper around the currently selected node in the rotation manager so it can be
        /// editted in the view.
        /// </summary>
        private Model currentObject { get; set; }

        private PropertyPresenter propertiesPresenter;
        private PropertyPresenter objectPropertiesPresenter;

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

            this.propertiesPresenter = new PropertyPresenter();
            this.propertiesPresenter.Attach(this.model, this.view.PropertiesView, presenter);

            this.objectPropertiesPresenter = new PropertyPresenter();
            this.objectPropertiesPresenter.Attach(this.currentObject, this.view.ObjectPropertiesView, this.presenter);
            this.objectPropertiesPresenter.ViewRefreshed += OnPropertyRefresh;

            this.currentObject = null;

            this.intellisense = new IntellisensePresenter(view as ViewBase);
            this.intellisense.ItemSelected += OnIntellisenseItemSelected;

            this.presenter.CommandHistory.ModelChanged += OnModelChanged;

            RefreshView();

            this.view.ClearSelection(); //have nothing selected
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
            propertiesPresenter.Detach();
            objectPropertiesPresenter.Detach();

            if (conditionsEditor != null)
                conditionsEditor.ContextItemsNeeded -= OnNeedVariableNames;
            if (actionsEditor != null)
                actionsEditor.ContextItemsNeeded -= OnNeedEventNames;
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
                    int objectID = args.Objects[0].ID;
                    for (int i = 0; i < model.Nodes.Count; i++)
                    {
                        if (model.Nodes[i].ID == objectID)
                        {
                            currentObject = new NodePropertyWrapper();
                            (currentObject as NodePropertyWrapper).node = model.Nodes[i];
                        }
                    }
                    for (int i = 0; i < model.Arcs.Count; i++)
                    {
                        if (model.Arcs[i].ID == objectID)
                        {
                            currentObject = new ArcPropertyWrapper();
                            (currentObject as ArcPropertyWrapper).arc = model.Arcs[i];
                        }
                    }
                    if (currentObject != null)
                    {                        
                        objectPropertiesPresenter.RefreshView(currentObject);
                    }
                }
            }
            //else do nothing
            return;
        }

        private void OnPropertyRefresh(object sender, EventArgs args)
        {
            if (conditionsEditor != null)
            {
                conditionsEditor.ContextItemsNeeded -= OnNeedVariableNames;
                conditionsEditor = null;
            }
                
            if (actionsEditor != null)
            {
                actionsEditor.ContextItemsNeeded -= OnNeedVariableNames;
                actionsEditor = null;
            }

            List<EditorView> list = this.objectPropertiesPresenter.GetAllEditorViews();
            if (list.Count == 2)
            {
                conditionsEditor = list[0];
                conditionsEditor.ContextItemsNeeded += OnNeedVariableNames;
                actionsEditor = list[1];
                actionsEditor.ContextItemsNeeded += OnNeedEventNames;
            }
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
                string currentLine = StringUtilities.GetLine(e.Code, e.LineNo - 1);
                currentEditor = (sender as EditorView).MainWidget.TooltipText;

                editorCursor = (sender as EditorView).Location;
                editorCursor.Column += 1; //so that it is placed after the .

                if (!e.ControlShiftSpace)
                {
                    bool gridCompletions = false;
                    if (rules)
                        gridCompletions = intellisense.GenerateGridCompletions(currentLine, e.ColNo, model, true, false, false, false, e.ControlSpace);
                    else
                        gridCompletions = intellisense.GenerateGridCompletions(currentLine, e.ColNo, model, false, true, false, true, e.ControlSpace);

                    if (gridCompletions)
                        intellisense.Show(e.Coordinates.X, e.Coordinates.Y);
                }               
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
                EditorView view = null;
                if (currentEditor.CompareTo(conditionsEditor.MainWidget.TooltipText) == 0)
                    view = conditionsEditor;
                else if (currentEditor.CompareTo(actionsEditor.MainWidget.TooltipText) == 0)
                    view = actionsEditor;

                if (string.IsNullOrEmpty(args.ItemSelected))
                {
                    return;
                }
                else if (string.IsNullOrEmpty(args.TriggerWord))
                {
                    view.Location = editorCursor;
                    view.InsertAtCaret(args.ItemSelected);
                }
                else
                {
                    view.Location = editorCursor;
                    view.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
                }
                    
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
            [Description("Colour")]
            [Display(Type = DisplayType.ColourPicker)]
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

        /// <summary>
        /// A wrapper for the currently selected node in rotation manager so that
        /// it can be used with a PropertyPresenter view.
        /// 
        /// Although this uses the model class, it should not be added as a model
        /// within a simulation file, as by itself it does nothing.
        /// </summary>
        [Serializable]
        public class ArcPropertyWrapper : Model
        {
            /// <summary>Currently selected node in the Rotation Manager</summary>
            public Arc arc = null;

            /// <summary>Property wrapper for name</summary>
            [Description("Name")]
            [Display(Type = DisplayType.None)]
            public string ArcName
            {
                get
                {
                    if (arc != null)
                        return arc.Name;
                    else
                        return string.Empty;
                }
                set
                {
                    if (arc != null)
                        arc.Name = value;
                }
            }

            /// <summary>Property wrapper for description</summary>
            [Description("Conditions")]
            [Display(Type = DisplayType.Code)]
            public string Conditions
            {
                get
                {
                    return String.Join('\n', arc.Conditions.ToArray());
                }
                set
                {
                    arc.Conditions = value.Split('\n').ToList<string>();
                }
            }

            /// <summary>Property wrapper for name</summary>
            [Description("Actions")]
            [Display(Type = DisplayType.Code)]
            public string Actions
            {
                get
                {
                    return String.Join('\n', arc.Actions.ToArray());
                }
                set
                {
                    arc.Actions = value.Split('\n').ToList<string>();
                }
            }
        }
    }
}