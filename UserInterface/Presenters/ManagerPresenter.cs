using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserInterface.Views;
using System.Reflection;
using Models;
using UserInterface.EventArguments;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory;
using Models.Core;

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
            buildScript();  // compiles and saves the script

            ManagerView.Editor.ContextItemsNeeded -= OnNeedVariableNames;
            ManagerView.Editor.LeaveEditor -= OnEditorLeave;
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        void OnNeedVariableNames(object Sender, NeedContextItemsArgs e)
        {
            CSharpParser parser = new CSharpParser();
            SyntaxTree syntaxTree = parser.Parse(ManagerView.Editor.Text); //Manager.Code);
            syntaxTree.Freeze();

            IEnumerable<FieldDeclaration> fields = syntaxTree.Descendants.OfType<FieldDeclaration>();

            e.ObjectName = e.ObjectName.Trim(" \t".ToCharArray());
            string typeName = string.Empty;

            foreach (FieldDeclaration field in fields)
            {
                foreach (VariableInitializer var in field.Variables)
                {
                    if (e.ObjectName == var.Name)
                    {
                        typeName = field.ReturnType.ToString();
                    }
                }
            }

            // find the properties and methods
            if (typeName != string.Empty)
            {
                Type atype = Utility.Reflection.GetTypeFromUnqualifiedName(typeName);
                e.AllItems.AddRange(NeedContextItemsArgs.ExamineTypeForContextItems(atype, true, true));
            }
        }

        private void buildScript()
        {
            ExplorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            try
            {
                // set the code property manually first so that compile error can be trapped via
                // an exception.
                Manager.Code = ManagerView.Editor.Text;

                // If it gets this far then compiles ok.
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Manager, "Code", ManagerView.Editor.Text));
            }
            catch (Models.Core.ApsimXException err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    ExplorerPresenter.ShowMessage(string.Format("[{0}]: {1}",  err.model.Name, err.InnerException.Message), DataStore.ErrorLevel.Error);
                else
                    ExplorerPresenter.ShowMessage(string.Format("[{0}]: {1}",  err.model.Name, err.Message), DataStore.ErrorLevel.Error);
            }
            ExplorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
        }


        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        void OnEditorLeave(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(CommandHistory_ModelChanged);
            if (Manager.Script != null)
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
