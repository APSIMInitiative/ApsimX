using System;
using APSIM.Shared.Utilities;
using UserInterface.EventArguments;
using Models;
using Models.Core;
using UserInterface.Views;
using UserInterface.Interfaces;
using Shared.Utilities;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter for the Manager component
    /// </summary>
    public class ManagerPresenter : IPresenter
    {
        /// <summary>
        /// The presenter used for properties
        /// </summary>
        private PropertyPresenter propertyPresenter;

        /// <summary>
        /// The manager object
        /// </summary>
        private Manager manager;

        /// <summary>
        /// The view for the manager
        /// </summary>
        private IManagerView managerView;

        /// <summary>
        /// The explorer presenter used
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Handles generation of completion options for the view.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="presenter">The explorer presenter being used</param>
        public void Attach(object model, object view, ExplorerPresenter presenter)
        {
            manager = model as Manager;
            managerView = view as IManagerView;

            explorerPresenter = presenter;
            intellisense = new IntellisensePresenter(managerView as ViewBase);
            intellisense.ItemSelected += OnIntellisenseItemSelected;

            if (manager.Children.Count == 0)
            {
                try
                {
                    manager.RebuildScriptModel();
                }
                catch(Exception err) {
                    explorerPresenter.MainPresenter.ShowError(err);
                }
            }

            // See if manager script has a description attribute on it's class.
            if (manager.Script != null)
            {
                DescriptionAttribute descriptionName = ReflectionUtilities.GetAttribute(manager.Script.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descriptionName != null)
                    explorerPresenter.ShowDescriptionInRightHandPanel(descriptionName.ToString());
            }

            propertyPresenter = new PropertyPresenter();
            try
            {
                propertyPresenter.Attach(manager.Script, managerView.PropertyEditor, presenter);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
            managerView.Editor.Mode = EditorType.ManagerScript;
            managerView.Editor.Text = manager.Code;

            managerView.Editor.LeaveEditor += OnEditorLeave;
            managerView.Editor.AddContextSeparator();
            managerView.Editor.AddContextActionWithAccel("Test compile", OnDoCompile, "Ctrl+T");
            managerView.Editor.AddContextActionWithAccel("Reformat", OnDoReformat, "Ctrl+R");
            managerView.CursorLocation = manager.Cursor;

            presenter.CommandHistory.ModelChanged += CommandHistory_ModelChanged;

            if (manager.Errors != null)
                explorerPresenter.MainPresenter.ShowError($"Errors found in manager model {manager.Name}{Environment.NewLine}{manager.Errors}");
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            manager.Cursor.TabIndex = managerView.TabIndex;
            manager.Cursor = managerView.CursorLocation;

            BuildScript();  // compiles and saves the script
            propertyPresenter.Detach();

            explorerPresenter.CommandHistory.ModelChanged -= CommandHistory_ModelChanged;
            managerView.Editor.LeaveEditor -= OnEditorLeave;
            intellisense.ItemSelected -= OnIntellisenseItemSelected;
            intellisense.Cleanup();
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The arguments</param>
        public void OnEditorLeave(object sender, EventArgs e)
        {
            if (!intellisense.Visible)
                BuildScript();
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The changed manager model</param>
        public void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == manager && managerView.Editor != null)
            {
                managerView.Editor.Text = manager.Code;
            }
        }

        /// <summary>
        /// Build the script
        /// </summary>
        private void BuildScript()
        {
            explorerPresenter.CommandHistory.ModelChanged -= CommandHistory_ModelChanged;

            try
            {
                string code = managerView.Editor.Text;

                // set the code property manually first so that compile error can be trapped via
                // an exception.
                bool codeChanged = manager.Code != code;
                manager.Code = code;

                // If it gets this far then compiles ok.
                if (codeChanged)
                {
                    explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(manager, "Code", code));
                }

                explorerPresenter.MainPresenter.ShowMessage("\"" + manager.Name + "\" compiled successfully", Simulation.MessageType.Information);

                // User could have added more inputs to manager script - therefore we update the property presenter.
                if (manager.Script != null && propertyPresenter != null)
                    propertyPresenter.RefreshView(manager.Script);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }

            explorerPresenter.CommandHistory.ModelChanged += CommandHistory_ModelChanged;
        }

        /// <summary>
        /// Perform a compile
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnDoCompile(object sender, EventArgs e)
        {
            BuildScript();
        }

        /// <summary>
        /// Perform a reformat of the text
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnDoReformat(object sender, EventArgs e)
        {
            try
            {
                manager.Reformat();
                if (managerView.Editor != null)
                {
                    managerView.Editor.Text = manager.Code;
                }
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
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
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// A rectangle defining the position of the cursor within the editor text
        /// </summary>
        public ManagerCursorLocation CursorLocation
        {
            get { return managerView.CursorLocation; }
            set { managerView.CursorLocation = value; }
        }
    }
}
