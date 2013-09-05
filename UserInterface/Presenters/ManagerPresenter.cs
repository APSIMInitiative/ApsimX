using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using System.Reflection;
using Model.Components;

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

            ManagerView.Code = Manager.Code;
            ManagerView.NeedVariableNames += OnNeedVariableNames;
            ManagerView.CodeChanged += new EventHandler(OnCodeChanged);
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        void OnNeedVariableNames(object Sender, Utility.Editor.NeedContextItems e)
        {
            object o = null;

            // If no dot was specified then the object name may be refering to a [Link] in the script.
            if (!e.ObjectName.Contains("."))
            {
                o = Utility.Reflection.GetValueOfFieldOrProperty(e.ObjectName, Manager.Model);
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
            }
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        void OnCodeChanged(object sender, EventArgs e)
        {
            CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            CommandHistory.Add(new Commands.ChangePropertyCommand(Manager, "Code", ManagerView.Code));
            CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == Manager)
                ManagerView.Code = Manager.Code;
        }
    }
}
