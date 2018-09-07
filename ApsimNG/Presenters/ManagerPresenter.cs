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
    using EventArguments;
    using Models;
    using Models.Core;
    using Views;
    using System.IO;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ICSharpCode.NRefactory.CSharp;

    /// <summary>
    /// Presenter for the Manager component
    /// </summary>
    public class ManagerPresenter : IPresenter
    {
        /// <summary>
        /// The presenter used for properties
        /// </summary>
        private PropertyPresenter propertyPresenter = new PropertyPresenter();

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
        /// <param name="explorerPresenter">The explorer presenter being used</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.manager = model as Manager;
            this.managerView = view as IManagerView;
            this.explorerPresenter = explorerPresenter;
            this.intellisense = new IntellisensePresenter(managerView as ViewBase);
            intellisense.ItemSelected += (sender, e) => managerView.Editor.InsertCompletionOption(e.ItemSelected, e.TriggerWord);

            this.propertyPresenter.Attach(this.manager.Script, this.managerView.GridView, this.explorerPresenter);
            this.managerView.Editor.ScriptMode = true;
            this.managerView.Editor.Text = this.manager.Code;
            this.managerView.Editor.ContextItemsNeeded += this.OnNeedVariableNames;
            this.managerView.Editor.LeaveEditor += this.OnEditorLeave;
            this.managerView.Editor.AddContextSeparator();
            this.managerView.Editor.AddContextActionWithAccel("Test compile", this.OnDoCompile, "Ctrl+T");
            this.managerView.Editor.AddContextActionWithAccel("Reformat", this.OnDoReformat, "Ctrl+R");
            this.managerView.Editor.Location = manager.Location;
            this.managerView.TabIndex = manager.ActiveTabIndex;
            this.explorerPresenter.CommandHistory.ModelChanged += this.CommandHistory_ModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.BuildScript();  // compiles and saves the script
            this.propertyPresenter.Detach();

            this.explorerPresenter.CommandHistory.ModelChanged -= this.CommandHistory_ModelChanged;
            this.managerView.Editor.ContextItemsNeeded -= this.OnNeedVariableNames;
            this.managerView.Editor.LeaveEditor -= this.OnEditorLeave;
            intellisense.Cleanup();
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Context item arguments</param>
        public void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            try
            {
                if (intellisense.GenerateScriptCompletions(e.Code, e.Offset, e.ControlSpace))
                    intellisense.Show(e.Coordinates.Item1, e.Coordinates.Item2);
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The arguments</param>
        public void OnEditorLeave(object sender, EventArgs e)
        {
            // this.explorerPresenter.CommandHistory.ModelChanged += this.CommandHistory_ModelChanged;
            if (!intellisense.Visible)
                this.BuildScript();
            if (this.manager.Script != null)
            {
                this.propertyPresenter.FindAllProperties(this.manager.Script);
                this.propertyPresenter.PopulateGrid(this.manager.Script);
            }
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The changed manager model</param>
        public void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == this.manager)
            {
                this.managerView.Editor.Text = this.manager.Code;
            }
            else if (changedModel == this.manager.Script)
            {
                this.propertyPresenter.UpdateModel(this.manager.Script);
            }
        }

        /// <summary>
        /// Find the type in the name
        /// </summary>
        /// <param name="t">Type to find</param>
        /// <param name="childTypeName">The text string to search</param>
        /// <returns>The type found</returns>
        private Type FindType(Type t, string childTypeName)
        {
            string[] words = childTypeName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                PropertyInfo property = t.GetProperty(childTypeName);
                if (property == null)
                {
                    return null;
                }

                t = property.PropertyType;
            }

            return t;
        }

        /// <summary>
        /// Build the script
        /// </summary>
        private void BuildScript()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.CommandHistory_ModelChanged;

            try
            {
                this.manager.Location = this.managerView.Editor.Location;
                this.manager.ActiveTabIndex = this.managerView.TabIndex;

                string code = this.managerView.Editor.Text;

                // set the code property manually first so that compile error can be trapped via
                // an exception.
                bool codeChanged = this.manager.Code != code;
                this.manager.Code = code;

                // If it gets this far then compiles ok.
                if (codeChanged)
                {
                    this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.manager, "Code", code));
                }

                this.explorerPresenter.MainPresenter.ShowMessage("\"" + this.manager.Name + "\" compiled successfully", Simulation.MessageType.Information);
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowError(err);
            }

            this.explorerPresenter.CommandHistory.ModelChanged += this.CommandHistory_ModelChanged;
        }

        /// <summary>
        /// Perform a compile
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnDoCompile(object sender, EventArgs e)
        {
            this.BuildScript();
        }

        /// <summary>
        /// Perform a reformat of the text
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnDoReformat(object sender, EventArgs e)
        {
            CSharpFormatter formatter = new CSharpFormatter(FormattingOptionsFactory.CreateAllman());
            string newText = formatter.Format(this.managerView.Editor.Text);
            this.managerView.Editor.Text = newText;
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.manager, "Code", newText));
        }
    }
}
