namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Commands;
    using Extensions;
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Utility;
    using Views;

    /// <summary>
    /// This presenter class is responsible for populating the view
    /// passed into the constructor and handling all user interaction of
    /// the view. Humble Dialog pattern.
    /// </summary>
    public class ExplorerPresenter : IPresenter
    {
        /// <summary>The visual instance</summary>
        private IExplorerView view;

        /// <summary>The main menu</summary>
        private MainMenu mainMenu;

        /// <summary>The context menu</summary>
        public ContextMenu ContextMenu { get; private set; }

        /// <summary>Show tick on tree nodes where models will be included in auto-doc?</summary>
        private bool showDocumentationStatus;

        /// <summary>Presenter for the component</summary>
        private IPresenter currentRightHandPresenter;

        /// <summary>Initializes a new instance of the <see cref="ExplorerPresenter" /> class</summary>
        /// <param name="mainPresenter">The presenter for the main window</param>
        public ExplorerPresenter(MainPresenter mainPresenter)
        {
            this.MainPresenter = mainPresenter;
        }

        /// <summary>
        /// Gets or sets whether graphical ticks should be displayed next to nodes that
        /// are to be included in auto documentation.
        /// </summary>
        public bool ShowIncludeInDocumentation
        {
            get
            {
                return showDocumentationStatus;
            }
            set
            {
                showDocumentationStatus = value;
                Refresh();
            }
        }

        /// <summary>Gets or sets the command history for this presenter</summary>
        /// <value>The command history.</value>
        public CommandHistory CommandHistory { get; set; }

        /// <summary>Gets or sets the APSIMX simulations object</summary>
        /// <value>The apsim x file.</value>
        public Simulations ApsimXFile { get; set; }

        /// <summary>Gets or sets the width of the explorer tree panel</summary>
        /// <value>The width of the tree.</value>
        public int TreeWidth
        {
            get { return view.Tree.TreeWidth; }
            set { this.view.Tree.TreeWidth = value; }
        }

        /// <summary>Gets the presenter for the main window</summary>
        /// To be revised if we want to replicate the Windows.Forms version
        public MainPresenter MainPresenter { get; private set; }

        /// <summary>Gets the current right hand presenter.</summary>
        /// <value>The current presenter.</value>
        public IPresenter CurrentPresenter
        {
            get
            {
                return this.currentRightHandPresenter;
            }
        }

        /// <summary>
        /// Gets the current right hand view.
        /// </summary>
        public ViewBase CurrentRightHandView { get; private set; }

        /// <summary>Gets the path of the current selected node in the tree.</summary>
        /// <value>The current node path.</value>
        public string CurrentNodePath
        {
            get
            {
                return this.view.Tree.SelectedNode;
            }
        }

        private string GetPathToNode(IModel model)
        {
            if (model is Simulations)
            {
                return model.Name;
            }
            return GetPathToNode(model.Parent) + "." + model.Name;
        }
        
        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        /// <param name="model">The simulation model</param>
        /// <param name="view">The view used for display</param>
        /// <param name="explorerPresenter">The presenter for this object</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.CommandHistory = new CommandHistory();
            // When the user undoes/redoes something we want to select the affected
            // model. Therefore we can use the same callback for both events.
            this.CommandHistory.OnUndo += OnUndoRedo;
            this.CommandHistory.OnRedo += OnUndoRedo;
            this.ApsimXFile = model as Simulations;
            this.view = view as IExplorerView;
            this.mainMenu = new MainMenu(this);
            this.ContextMenu = new ContextMenu(this);
            ApsimXFile.Links.Resolve(ContextMenu);

            this.view.Tree.SelectedNodeChanged += this.OnNodeSelected;
            this.view.Tree.DragStarted += this.OnDragStart;
            this.view.Tree.AllowDrop += this.OnAllowDrop;
            this.view.Tree.Droped += this.OnDrop;
            this.view.Tree.Renamed += this.OnRename;

            Refresh();

            ApsimFileMetadata file = Configuration.Settings.GetMruFile(ApsimXFile.FileName);
            if (file != null && file.ExpandedNodes != null)
                this.view.Tree.ExpandNodes(file.ExpandedNodes);

            this.PopulateMainMenu();
        }

        /// <summary>
        /// Called after undoing/redoing a command.
        /// Selects the model which was affected by the command.
        /// </summary>
        /// <param name="model">The model which was affected by the command.</param>
        /// <remarks>
        /// When the user undoes/redoes something we want to select the affected
        /// model. Therefore this callback is used for both undo and redo operations.
        /// </remarks>
        public void OnUndoRedo(IModel model)
        {
            Refresh();
            if (model != null)
            {
                if (ApsimXFile.FindAllDescendants().Contains(model))
                    SelectNode(model);
                else if (model.Parent != null && ApsimXFile.FindAllDescendants().Contains(model.Parent))
                    SelectNode(model.Parent);
            }
        }

        /// <summary>
        /// Refresh the view.
        /// </summary>
        public void Refresh()
        {
            view.Tree.Populate(GetNodeDescription(this.ApsimXFile));
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            try
            {
                if (File.Exists(ApsimXFile.FileName))
                    Configuration.Settings.SetExpandedNodes(ApsimXFile.FileName, view.Tree.GetExpandedNodes());
                CommandHistory.OnRedo -= OnUndoRedo;
                CommandHistory.OnUndo -= OnUndoRedo;
            }
            catch
            {
                // Don't rethrow - this is not a critical operation.
            }

            this.view.Tree.SelectedNodeChanged -= this.OnNodeSelected;
            this.view.Tree.DragStarted -= this.OnDragStart;
            this.view.Tree.AllowDrop -= this.OnAllowDrop;
            this.view.Tree.Droped -= this.OnDrop;
            this.view.Tree.Renamed -= this.OnRename;
            this.view.Tree.ContextMenu.Destroy();
            this.HideRightHandPanel();
            if (this.view is Views.ExplorerView)
            {
                (this.view as Views.ExplorerView).MainWidget.Cleanup();
            }

            this.ContextMenu = null;
            this.mainMenu = null;
            this.CommandHistory.Clear();
            this.ApsimXFile.ClearSimulationReferences();
            this.ApsimXFile = null;
        }

        /// <summary>
        /// Called by TabbedExplorerPresenter to do a save. Return true if all ok.
        /// </summary>
        /// <returns>True if saved</returns>
        public bool SaveIfChanged()
        {
            bool result = true;
            try
            {
                if (this.ApsimXFile != null && this.ApsimXFile.FileName != null)
                {
                    QuestionResponseEnum choice = QuestionResponseEnum.No;

                    if (!File.Exists(this.ApsimXFile.FileName))
                    {
                        choice = MainPresenter.AskQuestion("The original file '" + StringUtilities.PangoString(this.ApsimXFile.FileName) + 
                            "' no longer exists.\n \nClick \"Yes\" to save to this location or \"No\" to discard your work.");
                    }
                    else
                    {
                        // Need to hide the right hand panel because some views may not save
                        // their contents until they get a 'Detach' call.
                        this.HideRightHandPanel();

                        // need to test is ApsimXFile has changed and only prompt when changes have occured.
                        // serialise ApsimXFile to buffer
                        string newSim = FileFormat.WriteToString(ApsimXFile);

                        StreamReader simStream = new StreamReader(this.ApsimXFile.FileName);
                        string origSim = simStream.ReadToEnd(); // read original file to buffer2
                        simStream.Close();

                        if (string.Compare(newSim, origSim) != 0)
                        {
                            choice = MainPresenter.AskQuestion("Do you want to save changes in file " + StringUtilities.PangoString(this.ApsimXFile.FileName) + " ?");
                        }
                    }

                    if (choice == QuestionResponseEnum.Cancel)
                    {   // cancel
                        this.ShowRightHandPanel();
                        result = false;
                    }
                    else if (choice == QuestionResponseEnum.Yes)
                    {
                        // save
                        this.WriteSimulation();
                        result = true;
                    }
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(new Exception("Cannot save the file. Error: ", err));
                this.ShowRightHandPanel();
                result = false;
            }

            return result;
        }

        /// <summary>Save all changes.</summary>
        /// <returns>True if file was saved.</returns>
        public bool Save()
        {
            // Need to hide the right hand panel because some views may not have saved
            // their contents until they get a 'Detach' call.
            try
            {
                HideRightHandPanel();
                if (string.IsNullOrEmpty(ApsimXFile.FileName))
                    SaveAs();

                if (!string.IsNullOrEmpty(ApsimXFile.FileName))
                {
                    ApsimXFile.Write(ApsimXFile.FileName);
                    MainPresenter.ShowMessage(string.Format("Successfully saved to {0}", StringUtilities.PangoString(ApsimXFile.FileName)), Simulation.MessageType.Information);
                    return true;
                }
            }
            finally
            {
                ShowRightHandPanel();
            }

            return false;
        }

        /// <summary>Save the current file under a different name.</summary>
        /// <returns>True if file was saved.</returns>
        public bool SaveAs()
        {
            string newFileName = MainPresenter.AskUserForSaveFileName("ApsimX files|*.apsimx", this.ApsimXFile.FileName);
            if (newFileName != null)
            {
                try
                {
                    /*if (this.ApsimXFile.FileName != null)
                        Utility.Configuration.Settings.DelMruFile(this.ApsimXFile.FileName); */

                    this.ApsimXFile.Write(newFileName);
                    MainPresenter.ChangeTabText(this.view, Path.GetFileNameWithoutExtension(newFileName), newFileName);
                    Configuration.Settings.AddMruFile(new ApsimFileMetadata(newFileName, view.Tree.GetExpandedNodes()));
                    MainPresenter.UpdateMRUDisplay();
                    MainPresenter.ShowMessage(string.Format("Successfully saved to {0}", newFileName), Simulation.MessageType.Information);
                    return true;
                }
                catch (Exception err)
                {
                    this.MainPresenter.ShowError(new Exception("Cannot save the file. Error: ", err));
                }
            }

            return false;
        }

        /// <summary>Do the actual write to the file</summary>
        public void WriteSimulation()
        {
            this.ApsimXFile.ExplorerWidth = this.TreeWidth;
            this.ApsimXFile.Write(this.ApsimXFile.FileName);
        }

        /// <summary>Select a node in the view.</summary>
        /// <param name="node">Node to be selected.</param>
        /// <param name="refreshRightHandPanel">Iff true, the right-hand panel will be redrawn.</param>
        public void SelectNode(IModel node, bool refreshRightHandPanel = true)
        {
            SelectNode(node.FullPath, refreshRightHandPanel);
            if (refreshRightHandPanel)
            {
                this.HideRightHandPanel();
                this.ShowRightHandPanel();
            }
        }

        /// <summary>Select a node in the view.</summary>
        /// <param name="nodePath">Path to node</param>
        /// <param name="refreshRightHandPanel">Iff true, the right-hand panel will be redrawn.</param>
        public void SelectNode(string nodePath, bool refreshRightHandPanel = true)
        {
            this.view.Tree.SelectedNode = nodePath;
            while (GLib.MainContext.Iteration());
            if (refreshRightHandPanel)
            {
                this.HideRightHandPanel();
                this.ShowRightHandPanel();
            }
        }

        internal void CollapseChildren(string path)
        {
            view.Tree.CollapseChildren(path);
        }

        internal void ExpandChildren(string path, bool recursive = true)
        {
            view.Tree.ExpandChildren(path, recursive);
        }

        /// <summary>
        /// Select the next node in the view. The next node is defined as the next one
        /// down in the tree view. It will go through child nodes if they exist.
        /// Will return true if next node was successfully selected. Will return
        /// false if no more nodes to select.
        /// </summary>
        /// <returns>True when node is selected</returns>
        /// <exception cref="System.Exception">Cannot find the current selected model in the .apsimx file</exception>
        public bool SelectNextNode()
        {
            this.HideRightHandPanel();

            // Get a complete list of all models in this file.
            List<IModel> allModels = this.ApsimXFile.FindAllDescendants().Where(m => !m.IsHidden).ToList();
            allModels.Insert(0, ApsimXFile);

            /* If the current node path is '.Simulations' (the root node) then
               select the first item in the 'allModels' list. */
            if (string.IsNullOrEmpty(view.Tree.SelectedNode))
            {
                view.Tree.SelectedNode = allModels[0].FullPath;
                return true;
            }

            // Find the current node in this list.
            int index = -1;
            for (int i = 0; i < allModels.Count; i++)
            {
                if (allModels[i].FullPath == this.view.Tree.SelectedNode)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                throw new Exception("Cannot find the current selected model in the .apsimx file");
            }

            // If the current model is the last one in the list then return false.
            if (index >= allModels.Count - 1)
            {
                return false;
            }

            // Select the next node.
            this.view.Tree.SelectedNode = (allModels[index + 1]).FullPath;
            return true;
        }

        /// <summary>String must have all alpha numeric or '_' characters</summary>
        /// <param name="str">Name to be checked</param>
        /// <returns>
        /// True if all chars are alphanumerics and <code>str</code> is not null
        /// </returns>
        public bool IsValidName(string str)
        {
            bool valid = true;

            // test for invalid characters
            if (!string.IsNullOrEmpty(str))
            {
                int i = 0;
                while (valid && (i < str.Length))
                {
                    if (!char.IsLetter(str[i]) && !char.IsNumber(str[i]) && (str[i] != '_') && (str[i] != ' '))
                    {
                        valid = false;
                    }

                    i++;
                }
            }
            else
            {
                valid = false;
            }

            return valid;
        }

        /// <summary>Rename the current node.</summary>
        public void Rename()
        {
            this.view.Tree.BeginRenamingCurrentNode();
        }

        /// <summary>
        /// Adds a model to a parent model.
        /// </summary>
        /// <param name="parentPath">Path to the parent.</param>
        /// <param name="modelToAdd">The model to add to the tree.</param>
        public void AddChildToTree(string parentPath, IModel modelToAdd)
        {
            var nodeDescription = GetNodeDescription(modelToAdd);
            view.Tree.AddChild(parentPath, nodeDescription);
        }

        /// <summary>
        /// Delete a model from the tree.
        /// </summary>
        /// <param name="pathToNodeToDelete">Path to the node to be deleted.</param>
        public void DeleteFromTree(string pathToNodeToDelete)
        {
            view.Tree.Delete(pathToNodeToDelete);
        }

        /// <summary>Deletes the specified model.</summary>
        /// <param name="model">The model to delete.</param>
        public void Delete(IModel model)
        {
            try
            {
                DeleteModelCommand command = new DeleteModelCommand(model, this.GetNodeDescription(model), this.view);
                CommandHistory.Add(command, true);
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>Moves the specified model up.</summary>
        /// <param name="model">The model to move.</param>
        public void MoveUp(IModel model)
        {
            try
            {
                MoveModelUpDownCommand command = new MoveModelUpDownCommand(model, true, this.view);
                CommandHistory.Add(command, true);
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>Moves the specified model down.</summary>
        /// <param name="model">The model to move.</param>
        public void MoveDown(IModel model)
        {
            try
            {
                MoveModelUpDownCommand command = new MoveModelUpDownCommand(model, false, this.view);
                CommandHistory.Add(command, true);
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Move a node to a new parent node.
        /// </summary>
        public void Move(string originalPath, IModel toParent, TreeViewNode nodeDescription)
        {
            view.Tree.Delete(originalPath);
            view.Tree.AddChild((toParent).FullPath, nodeDescription);
        }

        /// <summary>
        /// Get whatever text is currently on the clipboard
        /// </summary>
        /// <param name="clipboardName">Name of the clipboard to be used.</param>
        public string GetClipboardText(string clipboardName = "_APSIM_MODEL")
        {
            return ViewBase.MasterView.GetClipboardText(clipboardName);
        }

        /// <summary>
        /// Place text on the clipboard
        /// </summary>
        /// <param name="text">The text to be stored in the clipboard</param>
        /// <param name="clipboardName">Name of the clipboard to be used.</param>
        public void SetClipboardText(string text, string clipboardName = "_APSIM_MODEL")
        {
            ViewBase.MasterView.SetClipboardText(text, clipboardName);
        }

        /// <summary>
        /// The view wants us to return a list of menu descriptions for the
        /// currently selected Node.
        /// </summary>
        /// <param name="nodePath">The path to the node</param>
        public void PopulateContextMenu(string nodePath)
        {
            if (view.Tree.ContextMenu == null)
                view.Tree.ContextMenu = new MenuView();

            List<MenuDescriptionArgs> descriptions = new List<MenuDescriptionArgs>();
            
            // Get the selected model.
            object selectedModel = this.ApsimXFile.FindByPath(nodePath)?.Value;

            // Go look for all [UserInterfaceAction]
            foreach (MethodInfo method in typeof(ContextMenu).GetMethods())
            {
                ContextMenuAttribute contextMenuAttr = ReflectionUtilities.GetAttribute(method, typeof(ContextMenuAttribute), false) as ContextMenuAttribute;
                if (contextMenuAttr != null)
                {
                    bool ok = true;
                    if (contextMenuAttr.AppliesTo != null && selectedModel != null)
                    {
                        ok = false;
                        foreach (Type t in contextMenuAttr.AppliesTo)
                        {
                            if (t.IsAssignableFrom(selectedModel.GetType()))
                            {
                                ok = true;
                            }
                        }
                    }

                    if (ok && contextMenuAttr.Excluding != null && selectedModel != null)
                    {
                        ok = true;
                        foreach (Type t in contextMenuAttr.Excluding)
                        {
                            if (t.IsAssignableFrom(selectedModel.GetType()))
                            {
                                ok = false;
                            }
                        }
                    }

                    if (ok)
                    {
                        MenuDescriptionArgs desc = new MenuDescriptionArgs();
                        desc.Name = contextMenuAttr.MenuName;
                        desc.ResourceNameForImage = "ApsimNG.Resources.MenuImages." + desc.Name + ".png";
                        desc.ShortcutKey = contextMenuAttr.ShortcutKey;
                        desc.ShowCheckbox = contextMenuAttr.IsToggle;
                        desc.FollowsSeparator = contextMenuAttr.FollowsSeparator;

                        // Check for an enable method
                        MethodInfo enableMethod = typeof(ContextMenu).GetMethod(method.Name + "Enabled");
                        if (enableMethod != null)
                        {
                            desc.Enabled = (bool)enableMethod.Invoke(this.ContextMenu, null);
                        }
                        else
                        {
                            desc.Enabled = true;
                        }

                        // Check for an checked method
                        MethodInfo checkMethod = typeof(ContextMenu).GetMethod(method.Name + "Checked");
                        if (checkMethod != null)
                        {
                                desc.Checked = (bool)checkMethod.Invoke(this.ContextMenu, null);
                        }
                        else
                        {
                            desc.Checked = false;
                        }

                        EventHandler handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), this.ContextMenu, method);
                        desc.OnClick = handler;

                        descriptions.Add(desc);
                    }
                }
            }

            view.Tree.ContextMenu.Populate(descriptions);
        }

        /// <summary>
        /// Generates .apsimx files for each child model under a given model.
        /// Returns false if errors were encountered, or true otherwise.
        /// </summary>
        /// <param name="model">Model to generate .apsimx files for.</param>
        /// <param name="path">
        /// Path which the files will be saved to. 
        /// If null, the user will be prompted to choose a directory.
        /// </param>
        public bool GenerateApsimXFiles(IModel model, string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                IFileDialog fileChooser = new FileDialog()
                {
                    Prompt = "Select a directory to save model files to.",
                    Action = FileDialog.FileActionType.SelectFolder
                };
                path = fileChooser.GetFile();
            }

            if (!string.IsNullOrEmpty(path))
            {
                MainPresenter.ShowMessage("Generating simulation files: ", Simulation.MessageType.Information);

                var runner = new Runner(model);
                var errors = Models.Core.Run.GenerateApsimXFiles.Generate(runner, path, (int percent) =>
                {
                    MainPresenter.ShowProgress(percent, false);
                });

                if (errors == null || errors.Count == 0)
                {
                    MainPresenter.ShowMessage("Successfully generated .apsimx files under " + path + ".", Simulation.MessageType.Information);
                    return true;
                }
                else
                {
                    MainPresenter.ShowError(errors);
                    return false;
                }
            }
            return true;
        }

        /// <summary>Hide the right hand panel.</summary>
        public void HideRightHandPanel()
        {
            if (this.currentRightHandPresenter != null)
            {
                try
                {
                    this.currentRightHandPresenter.Detach();
                    this.currentRightHandPresenter = null;
                }
                catch (Exception err)
                {
                    MainPresenter.ShowError(err);
                }
            }

            this.view.AddRightHandView(null);
        }

        /// <summary>Display a view on the right hand panel in view.</summary>
        public void ShowRightHandPanel()
        {
            if (this.view.Tree.SelectedNode != string.Empty)
            {
                object model = this.ApsimXFile.FindByPath(this.view.Tree.SelectedNode)?.Value;

                if (model != null)
                {
                    ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                    PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;
                    DescriptionAttribute descriptionName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;

                    if (descriptionName != null && model.GetType().Namespace.Contains("CLEM"))
                    {
                        viewName = new ViewNameAttribute("UserInterface.Views.ModelDetailsWrapperView");
                        presenterName = new PresenterNameAttribute("UserInterface.Presenters.ModelDetailsWrapperPresenter");
                    }

                    if (Configuration.Settings.UseNewPropertyPresenter && presenterName != null)
                    {
                        if (presenterName.ToString().Contains(".PropertyPresenter"))
                        {
                            presenterName = new PresenterNameAttribute("UserInterface.Presenters.SimplePropertyPresenter");
                            viewName = new ViewNameAttribute("UserInterface.Views.PropertyView");
                        }
                        else if (presenterName.ToString().Contains(".BiomassRemovalPresenter"))
                        {
                            presenterName = new PresenterNameAttribute("UserInterface.Presenters.CompositePropertyPresenter");
                            viewName = new ViewNameAttribute("UserInterface.Views.PropertyView");
                        }
                    }

                    // if a clem model ignore the newly added description box that is handled by CLEM wrapper
                    if (!model.GetType().Namespace.Contains("CLEM"))
                    {
                        ShowDescriptionInRightHandPanel(descriptionName?.ToString());
                    }
                    if (viewName != null && viewName.ToString().Contains(".glade"))
                        ShowInRightHandPanel(model,
                                             newView: new ViewBase(view as ViewBase, viewName.ToString()),
                                             presenter: Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as IPresenter);

                    else if (viewName != null && presenterName != null)
                        ShowInRightHandPanel(model, viewName.ToString(), presenterName.ToString());
                    else
                    {
                        var view = new MarkdownView(this.view as ViewBase);
                        var presenter = new DocumentationPresenter();
                        ShowInRightHandPanel(model, view, presenter);
                    }
                }
            }
        }

        /// <summary>Show a view in the right hand panel.</summary>
        /// <param name="model">The model.</param>
        /// <param name="viewName">The view name.</param>
        /// <param name="presenterName">The presenter name.</param>
        public void ShowInRightHandPanel(object model, string viewName, string presenterName)
        {
            ShowInRightHandPanel(model,
                                 newView: (ViewBase) Assembly.GetExecutingAssembly().CreateInstance(viewName, false, BindingFlags.Default, null, new object[] { this.view }, null, null),
                                 presenter: Assembly.GetExecutingAssembly().CreateInstance(presenterName) as IPresenter);
        }

        /// <summary>Show a view in the right hand panel.</summary>
        /// <param name="model">The model.</param>
        /// <param name="gladeResourceName">Name of the glade resource file.</param>
        /// <param name="presenter">The presenter to be used.</param>
        public void ShowInRightHandPanel(object model, string gladeResourceName, IPresenter presenter)
        {
            ShowInRightHandPanel(model,
                                 newView: new ViewBase(view as ViewBase, gladeResourceName),
                                 presenter: presenter);
        }

        /// <summary>
        /// Show a description in the right hand view.
        /// </summary>
        /// <param name="description">The description to show (Markdown).</param>
        public void ShowDescriptionInRightHandPanel(string description)
        {
            view.AddDescriptionToRightHandView(description);
        }

        /// <summary>Show a view in the right hand panel.</summary>
        /// <param name="model">The model.</param>
        /// <param name="newView">The view.</param>
        /// <param name="presenter">The presenter.</param>
        public void ShowInRightHandPanel(object model, ViewBase newView, IPresenter presenter)
        {
            currentRightHandPresenter = presenter;
            if (newView != null && currentRightHandPresenter != null)
            {
                // Resolve links in presenter.
                ApsimXFile.Links.Resolve(currentRightHandPresenter);
                view.AddRightHandView(newView);
                currentRightHandPresenter.Attach(model, newView, this);
                CurrentRightHandView = newView as ViewBase;
            }
        }

        /// <summary>Get a screen shot of the right hand panel.</summary>
        /// <returns>The image</returns>
        public System.Drawing.Image GetScreenhotOfRightHandPanel()
        {
            return this.view.GetScreenshotOfRightHandPanel();
        }

        /// <summary>
        /// Get the View object
        /// </summary>
        /// <returns>Returns the View object</returns>
        public ViewBase GetView()
        {
            return this.view as ExplorerView;
        }

        #region Events from view

        /// <summary>A node has been dragged over another node. Allow drop?</summary>
        /// <param name="sender">Sending node</param>
        /// <param name="e">Node arguments</param>
        public void OnAllowDrop(object sender, AllowDropArgs e)
        {
            e.Allow = false;

            Model parentModel = this.ApsimXFile.FindByPath(e.NodePath)?.Value as Model;
            if (parentModel != null)
            {
                DragObject dragObject = e.DragObject as DragObject;
                e.Allow = Apsim.IsChildAllowable(parentModel, dragObject.ModelType);
            }
        }

        /// <summary>
        /// Open a dialog for downloading a new weather file
        /// </summary>
        public void DownloadWeather()
        {
            Model model = this.ApsimXFile.FindByPath(this.CurrentNodePath)?.Value as Model;
            if (model != null)
            {
                Utility.WeatherDownloadDialog dlg = new Utility.WeatherDownloadDialog();
                IModel currentNode = ApsimXFile.FindByPath(CurrentNodePath)?.Value as IModel;
                dlg.ShowFor(model, (view as ExplorerView), currentNode, this);
            }
        }

        /// <summary>
        /// The view wants us to return a list of menu descriptions for the
        /// main menu.
        /// </summary>
        private void PopulateMainMenu()
        {
            List<MenuDescriptionArgs> descriptions = new List<MenuDescriptionArgs>();

            // Go look for all [UserInterfaceAction]
            foreach (MethodInfo method in typeof(MainMenu).GetMethods())
            {
                MainMenuAttribute mainMenuName = ReflectionUtilities.GetAttribute(method, typeof(MainMenuAttribute), false) as MainMenuAttribute;
                if (mainMenuName != null)
                {
                    MenuDescriptionArgs desc = new MenuDescriptionArgs();
                    desc.Name = mainMenuName.MenuName;
                    desc.ResourceNameForImage = "ApsimNG.Resources.MenuImages." + desc.Name + ".png";

                    EventHandler handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), this.mainMenu, method);
                    desc.OnClick = handler;

                    descriptions.Add(desc);
                }
            }

            // Show version label at right side of toolstrip
            MenuDescriptionArgs versionItem = new MenuDescriptionArgs();
            versionItem.Name = "Custom Build";
            versionItem.RightAligned = true;
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Major > 0)
            {
                versionItem.Name = "Official Build";
                versionItem.ToolTip = "Version: " + version.ToString();
            }
            view.ToolStrip.Populate(descriptions);
        }

        /// <summary>A node has been selected (whether by user or undo/redo)</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Node arguments</param>
        private void OnNodeSelected(object sender, NodeSelectedArgs e)
        {
            try
            {
                this.HideRightHandPanel();
                this.ShowRightHandPanel();
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }

            // If an exception is thrown while loding the view, this
            // shouldn't interfere with the context menu.
            this.PopulateContextMenu(e.NewNodePath);
        }

        /// <summary>A node has begun to be dragged.</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Drag arguments</param>
        private void OnDragStart(object sender, DragStartArgs e)
        {
            Model obj = this.ApsimXFile.FindByPath(e.NodePath)?.Value as Model;
            if (obj != null)
            {
                string st = FileFormat.WriteToString(obj);
                this.SetClipboardText(st);

                DragObject dragObject = new DragObject();
                dragObject.NodePath = e.NodePath;
                dragObject.ModelType = obj.GetType();
                dragObject.ModelString = st;
                e.DragObject = dragObject;
            }
        }

        /// <summary>A node has been dropped.</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Drop arguments</param>
        private void OnDrop(object sender, DropArgs e)
        {
            try
            {
                string toParentPath = e.NodePath;
                Model toParent = this.ApsimXFile.FindByPath(toParentPath)?.Value as Model;

                DragObject dragObject = e.DragObject as DragObject;
                if (dragObject != null && toParent != null)
                {
                    string modelString = dragObject.ModelString;
                    string fromParentPath = StringUtilities.ParentName(dragObject.NodePath);

                    ICommand cmd = null;
                    if (e.Copied)
                    {
                        var command = new AddModelCommand(toParent, modelString);
                        CommandHistory.Add(command, true);
                        Refresh();
                    }
                    else if (e.Moved)
                    {
                        if (fromParentPath != toParentPath)
                        {
                            Model fromModel = this.ApsimXFile.FindByPath(dragObject.NodePath)?.Value as Model;
                            if (fromModel != null)
                            {
                                cmd = new MoveModelCommand(fromModel, toParent, this.GetNodeDescription(fromModel), this);
                                CommandHistory.Add(cmd);
                                Refresh();
                            }
                        }
                    }
                    view.Tree.ExpandChildren(toParent.FullPath, false);
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>User has renamed a node.</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event node arguments</param>
        private void OnRename(object sender, NodeRenameArgs e)
        {
            try
            {
                e.CancelEdit = false;
                if (e.NewName != null)
                {
                    if (this.IsValidName(e.NewName))
                    {
                        Model model = this.ApsimXFile.FindByPath(e.NodePath)?.Value as Model;
                        if (model != null && model.GetType().Name != "Simulations" && e.NewName != string.Empty)
                        {
                            this.HideRightHandPanel();
                            RenameModelCommand cmd = new RenameModelCommand(model, e.NewName, this.view);
                            CommandHistory.Add(cmd);
                            this.ShowRightHandPanel();
                            e.CancelEdit = model.Name != e.NewName;
                        }
                    }
                    else
                    {
                        MainPresenter.ShowError("Use alpha numeric characters only!");
                        e.CancelEdit = true;
                    }
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>User has attempted to move the current node up.</summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnMoveUp(object sender, EventArgs e)
        {
            try
            {
                Model model = ApsimXFile.FindByPath(view.Tree.SelectedNode)?.Value as Model;

                if (model != null && model.Parent != null)
                {
                    IModel firstModel = model.Parent.Children[0];
                    if (model != firstModel)
                    {
                        CommandHistory.Add(new MoveModelUpDownCommand(model, true, view));
                    }
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>User has attempted to move the current node down.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The args</param>
        private void OnMoveDown(object sender, EventArgs e)
        {
            try
            {
                Model model = this.ApsimXFile.FindByPath(this.view.Tree.SelectedNode)?.Value as Model;

                if (model != null && model.Parent != null)
                {
                    IModel lastModel = model.Parent.Children[model.Parent.Children.Count - 1];
                    if (model != lastModel)
                    {
                        CommandHistory.Add(new MoveModelUpDownCommand(model, false, this.view));
                    }
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        #endregion

        #region Privates        

        /// <summary>
        /// A helper function for creating a node description object for the specified model.
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>The description</returns>
        public TreeViewNode GetNodeDescription(IModel model)
        {
            TreeViewNode description = new TreeViewNode();
            description.Name = model.Name;
            string resourceName = (model as ModelCollectionFromResource)?.ResourceName;
            description.ResourceNameForImage = GetIconResourceName(model.GetType(), model.Name, resourceName);

            description.ToolTip = model.GetType().Name;

            description.Children = new List<TreeViewNode>();
            foreach (Model child in model.Children)
            {
                if (!child.IsHidden)
                {
                    description.Children.Add(this.GetNodeDescription(child));
                }
            }
            description.Strikethrough = !model.Enabled;
            description.Checked = model.IncludeInDocumentation && showDocumentationStatus;
            description.Colour = System.Drawing.Color.Empty;
            /*
            // Set the colour here
            System.Drawing.Color colour = model.Enabled ? System.Drawing.Color.Black : System.Drawing.Color.Red;
            description.Colour = colour;
            */
            return description;
        }

        /// <summary>
        /// Find a resource name of an icon for the specified model.
        /// </summary>
        /// <param name="modelType">The model type.</param>
        /// <param name="modelName">The model name.</param>
        /// <param name="resourceName">Name of the model's resource file if one exists.null</param>
        public static string GetIconResourceName(Type modelType, string modelName, string resourceName)
        {
            // We need to find an icon for this model. If the model is a ModelCollectionFromResource, we attempt to find 
            // an image with the same resource name as the model (e.g. Wheat). If this fails, try the model type name.
            // Otherwise, we attempt to find an icon with the same name as the model's type.
            // e.g. A Graph called Biomass should use an icon called Graph.png
            // e.g. A Plant called Wheat should use an icon called Wheat.png
            // e.g. A plant called Wheat with a resource name of Maize (don't do this) should use an icon called Maize.png.

            string resourceNameForImage;
            ManifestResourceInfo info = null;
            if (typeof(ModelCollectionFromResource).IsAssignableFrom(modelType) && modelName != null)
            {
                resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + resourceName + ".png";
                info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(resourceNameForImage);

                // If there's no image for resource name (e.g. Wheat.png), try the model name (e.g. Plant.png)
                if (info == null)
                    resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + modelType.Name + ".png";
            }
            else
            {
                string modelNamespace = modelType.FullName.Split('.')[1] + ".";
                resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + modelNamespace + modelType.Name + ".png";

                if (MainView.MasterView != null && !MainView.MasterView.HasResource(resourceNameForImage))
                    resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + modelType.Name + ".png";
            }

            // Check to see if you can find the image in the resource for this project.
            info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(resourceNameForImage);
            if (info == null)
                resourceNameForImage = "ApsimNG.Resources.TreeViewImages." + modelName + ".png";

            return resourceNameForImage;
        }

        #endregion
    }

    /// <summary>
    /// An object that encompasses the data that is dragged during a drag/drop operation.
    /// </summary>
    [Serializable]
    public sealed class DragObject : ISerializable
    {
        /// <summary>Gets or sets the path to the node</summary>
        public string NodePath { get; set; }

        /// <summary>Gets or sets the string representation of a model.</summary>
        public string ModelString { get; set; }

        /// <summary>Gets or sets the type of model</summary>
        public Type ModelType { get; set; }

        /// <summary>Get data for the specified object in the xml</summary>
        /// <param name="info">Serialized object</param>
        /// <param name="context">The context</param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodePath", this.NodePath);
            info.AddValue("Xml", this.ModelString);
        }
    }
}
