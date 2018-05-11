// -----------------------------------------------------------------------
// <copyright file="ManagerPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
using UserInterface.Intellisense;

namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using EventArguments;
    using ICSharpCode.NRefactory;
    using ICSharpCode.NRefactory.Editor;
    using ICSharpCode.NRefactory.Completion;
    using ICSharpCode.NRefactory.TypeSystem;
    using ICSharpCode.NRefactory.Semantics;
    using ICSharpCode.NRefactory.CSharp;
    using ICSharpCode.NRefactory.CSharp.Completion;     // Why are these two 
    using ICSharpCode.NRefactory.CSharp.CodeCompletion; // namespaces separate?
    using ICSharpCode.NRefactory.CSharp.Resolver;
    // from lukebuehler sample project
    //using ICSharpCode.CodeCompletion;
    
    
    using Models;
    using Models.Core;
    using Views;
    using System.IO;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Forms;

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
        /// List of assemblies frequently used in manager scripts. 
        /// These assemblies get used by the CSharpCompletion object to look for intellisense options.
        /// Would be better to dynamically generate this list based on the user's script. The disadvantage of doing it that way
        /// is that loading these assemblies into the CSharpCompletion object is quite slow. 
        /// </summary>
        private static Assembly[] assemblies = {
            typeof(object).Assembly, // mscorlib
		    typeof(Uri).Assembly, // System.dll
		    typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
            typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
            typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
		    typeof(ICSharpCode.NRefactory.TypeSystem.IProjectContent).Assembly,
            typeof(IModel).Assembly
        };

        private CSharpCompletion completion = new CSharpCompletion(assemblies.ToList().AsReadOnly());

        Lazy<IList<IUnresolvedAssembly>> builtInLibs = new Lazy<IList<IUnresolvedAssembly>>(
        delegate 
        {
            IUnresolvedAssembly[] projectContents = new IUnresolvedAssembly[assemblies.Length];
            Stopwatch total = Stopwatch.StartNew();
            Parallel.For(
                0, assemblies.Length,
                delegate (int i) {
                    Stopwatch w = Stopwatch.StartNew();
                    AssemblyLoader loader = AssemblyLoader.Create();
                    projectContents[i] = loader.LoadAssemblyFile(assemblies[i].Location);
                    Debug.WriteLine(Path.GetFileName(assemblies[i].Location) + ": " + w.Elapsed);
                });
            Debug.WriteLine("Total: " + total.Elapsed);
            return projectContents;
        });

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
        }

        public IEnumerable<AstNode> DescendantsRecursively(IEnumerable<AstNode> nodes)
        {
            if (nodes == null)
                return null;
            List<AstNode> allNodes = new List<AstNode>();
            foreach (AstNode node in nodes)
            {
                if (node == null || node.Descendants == null)
                    continue;
                if (node.Descendants.Count() < 1)
                    allNodes.Add(node);
                else
                    allNodes.AddRange(DescendantsRecursively(node.Descendants));
            }
            return allNodes;
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
            var fileName = Path.GetTempFileName();
            if (!File.Exists(fileName))
                File.Create(fileName).Close();
            File.WriteAllText(fileName, managerView.Editor.Text);
            syntaxTree.FileName = fileName;
            syntaxTree.Freeze();

            try
            {
                // Should probably take into account which namespaces the user is using and load the needed assemblies into the CSharpCompletion object
                // string usings = syntaxTree.Descendants.OfType<UsingDeclaration>().Select(x => x.ToString()).Aggregate((x, y) => x + y);

                IDocument document = new ReadOnlyDocument(new StringTextSource(managerView.Editor.Text), syntaxTree.FileName);
                if (!UriParser.IsKnownScheme("pack"))
                {
                    new System.Windows.Application(); // Removing this line of code will break things.
                }
                
                CodeCompletionResult results = completion.GetCompletions(document, managerView.Editor.Offset, false);
                e.CompletionData = results.CompletionData.Select(x => x as ICompletionData).ToList();
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        public int GetOffsetFromLocation(string[] lines, TextLocation location)
        {
            int offset = location.Column;
            for (int i = 0; i < location.Line; i++)
            {
                offset += lines[i].Length + Environment.NewLine.Length;
            }
            return offset;
        }

        /// <summary>
        /// The user has changed the code script.
        /// </summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">The arguments</param>
        public void OnEditorLeave(object sender, EventArgs e)
        {
            // this.explorerPresenter.CommandHistory.ModelChanged += this.CommandHistory_ModelChanged;
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

        /// <summary>Get a screen shot of the manager grid.</summary>
        /// <returns>An Image object</returns>
        public Image GetScreenshot()
        {
            return this.managerView.GridView.GetScreenshot();
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
                string msg = err.Message;
                if (err.InnerException != null)
                {
                    msg += " ---> " + err.InnerException.Message;
                }

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
