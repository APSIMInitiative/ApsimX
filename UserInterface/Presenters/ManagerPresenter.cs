using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using System.Reflection;
using Models;

namespace UserInterface.Presenters
{
    class ManagerPresenter : IPresenter
    {
        private PropertyPresenter PropertyPresenter = new PropertyPresenter();
        private Manager Manager;
        private IManagerView ManagerView;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            Manager = Model as Manager;
            ManagerView = View as IManagerView;
            ExplorerPresenter = explorerPresenter;

            PropertyPresenter.Attach(Manager.Script, ManagerView.GridView, ExplorerPresenter);

            ManagerView.Editor.Text = Manager.Code;
            ManagerView.Editor.ContextItemsNeeded += OnNeedVariableNames;
            ManagerView.Editor.LeaveEditor += OnEditorLeave;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            ManagerView.Editor.ContextItemsNeeded -= OnNeedVariableNames;
            ManagerView.Editor.LeaveEditor -= OnEditorLeave;

            Manager.RebuildScriptModel();
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        void OnNeedVariableNames(object Sender, Utility.NeedContextItems e)
        {
            object o = null;
            if (Manager.Script != null)
            {

                // If no dot was specified then the object name may be refering to a [Link] in the script.
                if (!e.ObjectName.Contains("."))
                {
                    o = Utility.Reflection.GetValueOfFieldOrProperty(e.ObjectName.Trim(" \t".ToCharArray()), Manager.Script);
                    if (o == null)
                    {
                        // Not a [Link] so look for the object within scope
                        o = Manager.Find(e.ObjectName);
                    }
                }
                // If still not found then try a specific get for the object.
                if (o == null)
                    o = Manager.Get(e.ObjectName);

                // If found then loop through all properties and add to the items list.
                if (o != null)
                {
                    foreach (PropertyInfo Property in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        e.Items.Add(Property.Name);
                    foreach (MethodInfo Method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                        e.Items.Add(Method.Name);
                    e.Items.Sort();
                }
            }
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        void OnEditorLeave(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            try
            {
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangePropertyCommand(Manager, "Code", ManagerView.Editor.Text));
            }
            catch (Exception err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    ExplorerPresenter.ShowMessage(err.InnerException.Message, DataStore.ErrorLevel.Error);
                else
                    ExplorerPresenter.ShowMessage(err.Message, DataStore.ErrorLevel.Error);
            }
            ExplorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            PropertyPresenter.PopulateGrid(Manager.Script);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == Manager)
                ManagerView.Editor.Text = Manager.Code;
        }
    }
}
