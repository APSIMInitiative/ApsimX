// -----------------------------------------------------------------------
// <copyright file="FactorPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using EventArguments;
    using Models.Core;
    using Models.Factorial;
    using Views;

    /// <summary>
    /// Connects a Factor model to a FactorView.
    /// </summary>
    public class FactorPresenter : IPresenter
    {
        /// <summary>
        /// The factor object
        /// </summary>
        private Factor factor;

        /// <summary>
        /// The view object
        /// </summary>
        private IEditorView factorView;

        /// <summary>
        /// The presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.factor = model as Factor;
            this.factorView = view as IEditorView;
            this.explorerPresenter = explorerPresenter;

            this.factorView.Lines = this.factor.Specifications.ToArray();

            this.factorView.TextHasChangedByUser += this.OnTextHasChangedByUser;
            this.factorView.ContextItemsNeeded += this.OnContextItemsNeeded;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the objects
        /// </summary>
        public void Detach()
        {
            this.factorView.TextHasChangedByUser -= this.OnTextHasChangedByUser;
            this.factorView.ContextItemsNeeded -= this.OnContextItemsNeeded;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;            
        }

        /// <summary>
        /// Intellisense lookup.
        /// </summary>
        /// <param name="sender">The menu item</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItemsArgs e)
        {
            if (e.ObjectName == string.Empty)
            {
                e.ObjectName = ".";
            }

            try
            {
                Experiment experiment = this.factor.Parent.Parent as Experiment;
                if (experiment != null && experiment.BaseSimulation != null)
                {
                    object o = experiment.BaseSimulation.Get(e.ObjectName);

                    if (o != null)
                    {
                        foreach (IVariable property in Apsim.FieldsAndProperties(o, BindingFlags.Instance | BindingFlags.Public))
                        {
                            e.Items.Add(property.Name);
                        }

                        e.Items.Sort();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// User has changed the paths. Save to model.
        /// </summary>
        /// <param name="sender">The text control</param>
        /// <param name="e">Event arguments</param>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            List<string> newPaths = new List<string>();
            foreach (string line in this.factorView.Lines)
            {
                if (line != string.Empty)
                {
                    newPaths.Add(line);
                }
            }

            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.factor, "Specifications", newPaths));

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// The model has changed probably by an undo.
        /// </summary>
        /// <param name="changedModel">The model</param>
        private void OnModelChanged(object changedModel)
        {
            this.factorView.Lines = this.factor.Specifications.ToArray();
        }
    }
}
