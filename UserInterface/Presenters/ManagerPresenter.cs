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
        private CommandHistory CommandHistory;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            Manager = Model as Manager;
            ManagerView = View as IManagerView;
            this.CommandHistory = CommandHistory;

            PropertyPresenter.Attach(Manager.Model, ManagerView.GridView, CommandHistory);

            ManagerView.Editor.Text = Manager.Code;
            ManagerView.Editor.ContextItemsNeeded += OnNeedVariableNames;
            ManagerView.Editor.Leave += OnEditorLeave;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            ManagerView.Editor.ContextItemsNeeded -= OnNeedVariableNames;
            ManagerView.Editor.Leave -= OnEditorLeave;
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        void OnNeedVariableNames(object Sender, Utility.NeedContextItems e)
        {
            object o = null;
            if (Manager.Model != null)
            {

                // If no dot was specified then the object name may be refering to a [Link] in the script.
                if (!e.ObjectName.Contains("."))
                {
                    o = Utility.Reflection.GetValueOfFieldOrProperty(e.ObjectName.Trim(" \t".ToCharArray()), Manager.Model);
                    if (o == null)
                    {
                        // Not a [Link] so look for the object within scope
                        o = Manager.ParentZone.Find(e.ObjectName);
                    }
                }
                // If still not found then try a specific get for the object.
                if (o == null)
                    o = Manager.ParentZone.Get(e.ObjectName);

                // If found then loop through all properties and add to the items list.
                if (o != null)
                {
                    foreach (PropertyInfo Property in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        e.Items.Add(Property.Name);
                    foreach (MethodInfo Method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
                        e.Items.Add(Method.Name);
                }
            }
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        void OnEditorLeave(object sender, EventArgs e)
        {
            CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            CommandHistory.Add(new Commands.ChangePropertyCommand(Manager, "Code", ManagerView.Editor.Text));
            CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            PropertyPresenter.PopulateGrid(Manager.Model);
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
