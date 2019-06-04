// -----------------------------------------------------------------------
// <copyright file="ManagerPresenter.cs"  company="APSIM Initiative">
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
        private Interfaces.IRotBubbleChartView view;

        /// <summary>The explorer presenter used</summary>
        private ExplorerPresenter presenter;

        private RotBubbleChart model;

        /// <summary>
        /// Handles generation of completion options for the view.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="_model">The model</param>
        /// <param name="_view">The view to attach</param>
        /// <param name="_presenter">The explorer presenter being used</param>
        public void Attach(object _model, object _view, ExplorerPresenter _presenter)
        {
            view = _view as Interfaces.IRotBubbleChartView;
            presenter = _presenter;
            model = _model as RotBubbleChart;
            view.AddNode += OnAddNode;
            view.DupNode += OnDupNode;
            view.DelNode += OnDelNode;
            // Tell the view to populate the axis.
            this.PopulateView();

            //intellisense = new IntellisensePresenter(managerView as ViewBase);
            //intellisense.ItemSelected += OnIntellisenseItemSelected;
            //presenter.CommandHistory.ModelChanged += CommandHistory_ModelChanged;

        }
        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.AddNode -= OnAddNode;
            view.DupNode -= OnDupNode;
            view.DelNode -= OnDelNode;
            //presenter.CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
            //intellisense.ItemSelected -= OnIntellisenseItemSelected;
            //intellisense.Cleanup();
        }
        private void PopulateView()
        {
            view.Graph.DirectedGraph = model.DirectedGraphInfo;
        }
        /// <summary>
        /// A new node has been added
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnAddNode(object sender, AddNodeEventArgs e)
        {
#if false
            try
            {
                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
#else
            model.AddNode(sender, e.Name, e.Background, e.Outline);
            view.Graph.DirectedGraph = model.DirectedGraphInfo;
#endif
        }

        private void OnDupNode(object sender, DupNodeEventArgs e)
        {
#if false
            try
            {
                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
#else
            model.DupNode(sender, e.nodeNameToDuplicate);
            view.Graph.DirectedGraph = model.DirectedGraphInfo;
#endif
        }

        private void OnDelNode(object sender, DelNodeEventArgs e)
        {
#if false
            try
            {
                AddNodeCommand command = new AddNodeCommand("AddNode", view, presenter);
                presenter.CommandHistory.Add(command);
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
#else
            model.DelNode(sender, e.nodeNameToDelete);
            view.Graph.DirectedGraph = model.DirectedGraphInfo;
#endif
        }

#if false
        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The changed manager model</param>
        public void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == manager)
            {
                managerView.Editor.Text = manager.Code;
            }
            else if (changedModel == scriptModel)
            {
                propertyPresenter.UpdateModel(scriptModel);
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
            try
            {
                managerView.Editor.InsertCompletionOption(args.ItemSelected, args.TriggerWord);
                if (args.IsMethod)
                    intellisense.ShowScriptMethodCompletion(manager, managerView.Editor.Text, managerView.Editor.Offset, managerView.Editor.GetPositionOfCursor());
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }
#endif
    }
}

