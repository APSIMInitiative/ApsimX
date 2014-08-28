using System;
using Models.Factorial;
using UserInterface.Views;
using System.Reflection;
using System.Collections.Generic;
using Models.Core;
using UserInterface.EventArguments;
namespace UserInterface.Presenters
{
    /// <summary>
    /// Connects a Factor model to a FactorView.
    /// </summary>
    public class FactorPresenter : IPresenter
    {
        private Factor Factor;
        private IEditorView FactorView;
        private ExplorerPresenter ExplorerPresenter;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Factor = model as Factor;
            FactorView = view as IEditorView;
            ExplorerPresenter = explorerPresenter;

            FactorView.Lines = Factor.Paths.ToArray();

            FactorView.TextHasChangedByUser += OnTextHasChangedByUser;
            FactorView.ContextItemsNeeded += OnContextItemsNeeded;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }


        public void Detach()
        {
            FactorView.TextHasChangedByUser -= OnTextHasChangedByUser;
            FactorView.ContextItemsNeeded -= OnContextItemsNeeded;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;            
        }

        /// <summary>
        /// Intellisense lookup.
        /// </summary>
        void OnContextItemsNeeded(object sender, NeedContextItems e)
        {
            if (e.ObjectName == "")
                e.ObjectName = ".";
            try
            {
                Experiment experiment = Factor.Parent.Parent as Experiment;
                if (experiment != null && experiment.BaseSimulation != null)
                {
                    object o = experiment.BaseSimulation.Get(e.ObjectName);

                    if (o != null)
                    {
                        foreach (IVariable Property in ModelFunctions.FieldsAndProperties(o, BindingFlags.Instance | BindingFlags.Public))
                            e.Items.Add(Property.Name);
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
        void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;

            List<string> newPaths = new List<string>();
            newPaths.AddRange(FactorView.Lines);
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Factor, "Paths", newPaths));

            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }


        /// <summary>
        /// The model has changed probably by an undo.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            FactorView.Lines = Factor.Paths.ToArray();
        }


    }
}
