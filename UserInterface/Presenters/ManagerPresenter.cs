// -----------------------------------------------------------------------
// <copyright file="ManagerPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using EventArguments;
    using ICSharpCode.NRefactory;
    using ICSharpCode.NRefactory.CSharp;
    using Models;
    using Models.Core;
    using Views;
    using APSIM.Shared.Utilities;

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

            this.propertyPresenter.Attach(this.manager.Script, this.managerView.GridView, this.explorerPresenter);

            this.managerView.Editor.Text = this.manager.Code;
            this.managerView.Editor.ContextItemsNeeded += this.OnNeedVariableNames;
            this.managerView.Editor.LeaveEditor += this.OnEditorLeave;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.BuildScript();  // compiles and saves the script

            this.managerView.Editor.ContextItemsNeeded -= this.OnNeedVariableNames;
            this.managerView.Editor.LeaveEditor -= this.OnEditorLeave;
        }

        /// <summary>
        /// The view is asking for variable names for its intellisense.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Context item arguments</param>
        public void OnNeedVariableNames(object sender, NeedContextItemsArgs e)
        {
            CSharpParser parser = new CSharpParser();
            SyntaxTree syntaxTree = parser.Parse(this.managerView.Editor.Text); // Manager.Code);
            syntaxTree.Freeze();

            IEnumerable<FieldDeclaration> fields = syntaxTree.Descendants.OfType<FieldDeclaration>();

            e.ObjectName = e.ObjectName.Trim(" \t".ToCharArray());
            string typeName = string.Empty;

            // Determine the field name to find. User may have typed 
            // Soil.SoilWater. In this case the field to look for is Soil
            string fieldName = e.ObjectName;
            int posPeriod = e.ObjectName.IndexOf('.');
            if (posPeriod != -1)
                fieldName = e.ObjectName.Substring(0, posPeriod);
                
            // Look for the field name.
            foreach (FieldDeclaration field in fields)
            {
                foreach (VariableInitializer var in field.Variables)
                {
                    if (fieldName == var.Name)
                    {
                        typeName = field.ReturnType.ToString();
                    }
                }
            }
            
            // find the properties and methods
            if (typeName != string.Empty)
            {
                Type atype = ReflectionUtilities.GetTypeFromUnqualifiedName(typeName);
                if (posPeriod != -1)
                {
                    string childName = e.ObjectName.Substring(posPeriod + 1);
                    atype = this.FindType(atype, childName);
                }
                    
                e.AllItems.AddRange(NeedContextItemsArgs.ExamineTypeForContextItems(atype, true, true, false));
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
                    return null;
                t = property.PropertyType;
            }
            return t;
        }

        /// <summary>
        /// Build the script
        /// </summary>
        private void BuildScript()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= new CommandHistory.ModelChangedDelegate(this.CommandHistory_ModelChanged);
            try
            {
                // set the code property manually first so that compile error can be trapped via
                // an exception.
                this.manager.Code = this.managerView.Editor.Text;

                // If it gets this far then compiles ok.
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.manager, "Code", this.managerView.Editor.Text));
            }
            catch (Models.Core.ApsimXException err)
            {
                string msg = err.Message;
                if (err.InnerException != null)
                    this.explorerPresenter.ShowMessage(string.Format("[{0}]: {1}", err.model.Name, err.InnerException.Message), DataStore.ErrorLevel.Error);
                else
                    this.explorerPresenter.ShowMessage(string.Format("[{0}]: {1}", err.model.Name, err.Message), DataStore.ErrorLevel.Error);
            }
            this.explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(this.CommandHistory_ModelChanged);
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The arguments</param>
        public void OnEditorLeave(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged += new CommandHistory.ModelChangedDelegate(this.CommandHistory_ModelChanged);
            if (this.manager.Script != null)
                this.propertyPresenter.PopulateGrid(this.manager.Script);
        }

        /// <summary>
        /// The model has changed so update our view.
        /// </summary>
        /// <param name="changedModel">The changed manager model</param>
        public void CommandHistory_ModelChanged(object changedModel)
        {
            if (changedModel == this.manager)
                this.managerView.Editor.Text = this.manager.Code;
        }
    }
}
