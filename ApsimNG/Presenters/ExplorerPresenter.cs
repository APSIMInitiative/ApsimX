// -----------------------------------------------------------------------
// <copyright file="ExplorerPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using Importer;
    using Interfaces;
    using Models;
    using Models.Core;
    using System.Linq;
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
        private ContextMenu contextMenu;

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
            this.ApsimXFile = model as Simulations;
            this.view = view as IExplorerView;
            this.mainMenu = new MainMenu(this);
            this.contextMenu = new ContextMenu(this);
            ApsimXFile.Links.Resolve(contextMenu);

            this.view.Tree.SelectedNodeChanged += this.OnNodeSelected;
            this.view.Tree.DragStarted += this.OnDragStart;
            this.view.Tree.AllowDrop += this.OnAllowDrop;
            this.view.Tree.Droped += this.OnDrop;
            this.view.Tree.Renamed += this.OnRename;
            
            Refresh();
            this.PopulateMainMenu();
        }

        /// <summary>
        /// Refresh the view.
        /// </summary>
        public void Refresh()
        {
            view.Tree.Populate(GetNodeDescription(this.ApsimXFile));
            this.WriteLoadErrors();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.view.Tree.SelectedNodeChanged -= this.OnNodeSelected;
            this.view.Tree.DragStarted -= this.OnDragStart;
            this.view.Tree.AllowDrop -= this.OnAllowDrop;
            this.view.Tree.Droped -= this.OnDrop;
            this.view.Tree.Renamed -= this.OnRename;
            this.HideRightHandPanel();
            if (this.view is Views.ExplorerView)
            {
                (this.view as Views.ExplorerView).MainWidget.Destroy();
            }

            this.contextMenu = null;
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
                        choice = MainPresenter.AskQuestion("The original file '" + this.ApsimXFile.FileName + 
                            "' no longer exists.\n \nClick \"Yes\" to save to this location or \"No\" to discard your work.");
                    }
                    else
                    {
                        // Need to hide the right hand panel because some views may not save
                        // their contents until they get a 'Detach' call.
                        this.HideRightHandPanel();

                        // need to test is ApsimXFile has changed and only prompt when changes have occured.
                        // serialise ApsimXFile to buffer
                        StringWriter o = new StringWriter();
                        this.ApsimXFile.Write(o);
                        string newSim = o.ToString();

                        StreamReader simStream = new StreamReader(this.ApsimXFile.FileName);
                        string origSim = simStream.ReadToEnd(); // read original file to buffer2
                        simStream.Close();

                        if (string.Compare(newSim, origSim) != 0)
                        {
                            choice = MainPresenter.AskQuestion("Do you want to save changes in file " + this.ApsimXFile.FileName + " ?");
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
            try
            {
                // Need to hide the right hand panel because some views may not have saved
                // their contents until they get a 'Detach' call.
                this.HideRightHandPanel();

                if (this.ApsimXFile.FileName == null)
                {
                    this.SaveAs();
                }

                if (this.ApsimXFile.FileName != null)
                {
                    this.ApsimXFile.Write(this.ApsimXFile.FileName);
                    return true;
                }
            }
            catch (Exception err)
            {
                this.MainPresenter.ShowError(new Exception("Cannot save the file. Error: ", err));
            }
            finally
            {
                this.ShowRightHandPanel();
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
                    Utility.Configuration.Settings.AddMruFile(newFileName);
                    MainPresenter.UpdateMRUDisplay();
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
        /// <param name="nodePath">Path to node</param>
        public void SelectNode(string nodePath)
        {
            this.view.Tree.SelectedNode = nodePath;
            this.HideRightHandPanel();
            this.ShowRightHandPanel();
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
            List<IModel> allModels = Apsim.ChildrenRecursivelyVisible(this.ApsimXFile);

            /* If the current node path is '.Simulations' (the root node) then
               select the first item in the 'allModels' list. */
            if (this.view.Tree.SelectedNode == string.Empty)
            {
                this.view.Tree.SelectedNode = Apsim.FullPath(allModels[0]);
                return true;
            }

            // Find the current node in this list.
            int index = -1;
            for (int i = 0; i < allModels.Count; i++)
            {
                if (Apsim.FullPath(allModels[i]) == this.view.Tree.SelectedNode)
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
            this.view.Tree.SelectedNode = Apsim.FullPath(allModels[index + 1]);
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
        /// Pastes the contents of the clipboard.
        /// </summary>
        /// <param name="xml">The XML document text</param>
        /// <param name="parentPath">Path to the parent</param>
        public void Add(string xml, string parentPath)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                try
                {
                    document.LoadXml(xml);
                }
                catch (XmlException err)
                {
                    MainPresenter.ShowError(new Exception("Invalid XML. Are you sure you're trying to paste an APSIM model?", err));
                }

                object newModel = XmlUtilities.Deserialise(document.DocumentElement, ApsimXFile.GetType().Assembly);

                // See if the presenter is happy with this model being added.
                Model parentModel = Apsim.Get(ApsimXFile, parentPath) as Model;
                AllowDropArgs allowDropArgs = new AllowDropArgs();
                allowDropArgs.NodePath = parentPath;
                allowDropArgs.DragObject = new DragObject()
                {
                    NodePath = null,
                    ModelType = newModel.GetType(),
                    Xml = GetClipboardText()
                };

                OnAllowDrop(null, allowDropArgs);

                // If it is happy then issue an AddModelCommand.
                if (allowDropArgs.Allow)
                {
                    // If the model xml is a soil object then try and convert from old
                    // APSIM format to new.
                    if (document.DocumentElement.Name == "Soil" && XmlUtilities.Attribute(document.DocumentElement, "Name") != string.Empty)
                    {
                        XmlDocument newDoc = new XmlDocument();
                        newDoc.AppendChild(newDoc.CreateElement("D"));
                        APSIMImporter importer = new APSIMImporter();
                        importer.ImportSoil(document.DocumentElement, newDoc.DocumentElement, newDoc.DocumentElement);
                        XmlNode soilNode = XmlUtilities.FindByType(newDoc.DocumentElement, "Soil");
                        if (soilNode != null &&
                            XmlUtilities.FindByType(soilNode, "Sample") == null &&
                            XmlUtilities.FindByType(soilNode, "InitialWater") == null)
                        {
                            // Add in an initial water and initial conditions models.
                            XmlNode initialWater = soilNode.AppendChild(soilNode.OwnerDocument.CreateElement("InitialWater"));
                            XmlUtilities.SetValue(initialWater, "Name", "Initial water");
                            XmlUtilities.SetValue(initialWater, "PercentMethod", "FilledFromTop");
                            XmlUtilities.SetValue(initialWater, "FractionFull", "1");
                            XmlUtilities.SetValue(initialWater, "DepthWetSoil", "NaN");
                            XmlNode initialConditions = soilNode.AppendChild(soilNode.OwnerDocument.CreateElement("Sample"));
                            XmlUtilities.SetValue(initialConditions, "Name", "Initial conditions");
                            XmlUtilities.SetValue(initialConditions, "Thickness/double", "1800");
                            XmlUtilities.SetValue(initialConditions, "NO3/double", "10");
                            XmlUtilities.SetValue(initialConditions, "NH4/double", "1");
                            XmlUtilities.SetValue(initialConditions, "NO3Units", "kgha");
                            XmlUtilities.SetValue(initialConditions, "NH4Units", "kgha");
                            XmlUtilities.SetValue(initialConditions, "SWUnits", "Volumetric");
                        }

                        document.LoadXml(newDoc.DocumentElement.InnerXml);
                    }

                    IModel child = XmlUtilities.Deserialise(document.DocumentElement, ApsimXFile.GetType().Assembly) as IModel;

                    AddModelCommand command = new AddModelCommand(parentModel, document.DocumentElement, GetNodeDescription(child), view);
                    CommandHistory.Add(command, true);
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
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
        /// Get whatever text is currently on the clipboard
        /// </summary>
        /// <returns>Clipboard text</returns>
        public string GetClipboardText(string clipboardName = "_APSIM_MODEL")
        {
            return ViewBase.MasterView.GetClipboardText(clipboardName);
        }

        /// <summary>
        /// Place text on the clipboard
        /// </summary>
        /// <param name="text">The text to be stored in the clipboard</param>
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
            object selectedModel = Apsim.Get(this.ApsimXFile, nodePath);

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
                            desc.Enabled = (bool)enableMethod.Invoke(this.contextMenu, null);
                        }
                        else
                        {
                            desc.Enabled = true;
                        }

                        // Check for an checked method
                        MethodInfo checkMethod = typeof(ContextMenu).GetMethod(method.Name + "Checked");
                        if (checkMethod != null)
                        {
                            desc.Checked = (bool)checkMethod.Invoke(this.contextMenu, null);
                        }
                        else
                        {
                            desc.Checked = false;
                        }

                        EventHandler handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), this.contextMenu, method);
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
            List<IModel> children;
            if (model is ISimulationGenerator)
            {
                children = new List<IModel> { model };
            }
            else
            {
                children = Apsim.ChildrenRecursively(model, typeof(ISimulationGenerator));
            }

            if (string.IsNullOrEmpty(path))
            {
                IFileDialog fileChooser = new FileDialog()
                {
                    Prompt = "Select a directory to save model files to.",
                    Action = FileDialog.FileActionType.SelectFolder
                };
                path = fileChooser.GetFile();
                if (string.IsNullOrEmpty(path))
                    return false;
            }
            
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            List<Exception> errors = new List<Exception>();
            int i = 0;
            foreach (IModel sim in children)
            {
                MainPresenter.ShowMessage("Generating simulation files: ", Simulation.MessageType.Information);
                MainPresenter.ShowProgress(100 * i / children.Count, false);
                while (GLib.MainContext.Iteration()) ;
                try
                {
                    (sim as ISimulationGenerator).GenerateApsimXFile(path);
                }
                catch (Exception err)
                {
                    errors.Add(err);
                }

                i++;
            }
            if (errors.Count < 1)
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
            try
            {
                if (this.view.Tree.SelectedNode != string.Empty)
                {
                    object model = Apsim.Get(this.ApsimXFile, this.view.Tree.SelectedNode);

                    if (model != null)
                    {
                        ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                        PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;
                        DescriptionAttribute descriptionName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(DescriptionAttribute), false) as DescriptionAttribute;

                        if (descriptionName != null)
                        {
                            viewName = new ViewNameAttribute("UserInterface.Views.ModelDetailsWrapperView");
                            presenterName = new PresenterNameAttribute("UserInterface.Presenters.ModelDetailsWrapperPresenter");
                        }

                        if (viewName == null && presenterName == null)
                        {
                            viewName = new ViewNameAttribute("UserInterface.Views.HTMLView");
                            presenterName = new PresenterNameAttribute("UserInterface.Presenters.GenericPresenter");
                        }

                        if (viewName != null && presenterName != null)
                        {
                            this.ShowInRightHandPanel(model, viewName.ToString(), presenterName.ToString());
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MainPresenter.ShowError(err);
            }
        }

        /// <summary>Show a view in the right hand panel.</summary>
        /// <param name="model">The model.</param>
        /// <param name="viewName">The view name.</param>
        /// <param name="presenterName">The presenter name.</param>
        public void ShowInRightHandPanel(object model, string viewName, string presenterName)
        {
            try
            {
                object newView = Assembly.GetExecutingAssembly().CreateInstance(viewName, false, BindingFlags.Default, null, new object[] { this.view }, null, null);
                this.currentRightHandPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName) as IPresenter;
                if (newView != null && this.currentRightHandPresenter != null)
                {
                    // Resolve links in presenter.
                    ApsimXFile.Links.Resolve(currentRightHandPresenter);
                    this.view.AddRightHandView(newView);
                    this.currentRightHandPresenter.Attach(model, newView, this);
                }
            }
            catch (Exception err)
            {
                if (err is TargetInvocationException)
                {
                    err = (err as TargetInvocationException).InnerException;
                }

                string message = err.Message;
                message += "\r\n" + err.StackTrace;
                MainPresenter.ShowError(err);
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

            Model parentModel = Apsim.Get(this.ApsimXFile, e.NodePath) as Model;
            if (parentModel != null)
            {
                DragObject dragObject = e.DragObject as DragObject;
                e.Allow = Apsim.IsChildAllowable(parentModel, dragObject.ModelType);
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
            this.HideRightHandPanel();
            this.ShowRightHandPanel();
            this.PopulateContextMenu(e.NewNodePath);

            Commands.SelectNodeCommand selectCommand = new SelectNodeCommand(e.OldNodePath, e.NewNodePath, this.view);
            CommandHistory.Add(selectCommand, false);
        }

        /// <summary>A node has begun to be dragged.</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Drag arguments</param>
        private void OnDragStart(object sender, DragStartArgs e)
        {
            Model obj = Apsim.Get(this.ApsimXFile, e.NodePath) as Model;
            if (obj != null)
            {
                string xml = Apsim.Serialise(obj);
                this.SetClipboardText(xml);

                DragObject dragObject = new DragObject();
                dragObject.NodePath = e.NodePath;
                dragObject.ModelType = obj.GetType();
                dragObject.Xml = xml;
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
                Model toParent = Apsim.Get(this.ApsimXFile, toParentPath) as Model;

                DragObject dragObject = e.DragObject as DragObject;
                if (dragObject != null && toParent != null)
                {
                    string fromModelXml = dragObject.Xml;
                    string fromParentPath = StringUtilities.ParentName(dragObject.NodePath);

                    ICommand cmd = null;
                    if (e.Copied)
                    {
                        this.Add(fromModelXml, toParentPath);
                    }
                    else if (e.Moved)
                    {
                        if (fromParentPath != toParentPath)
                        {
                            Model fromModel = Apsim.Get(this.ApsimXFile, dragObject.NodePath) as Model;
                            if (fromModel != null)
                            {
                                cmd = new MoveModelCommand(fromModel, toParent, this.GetNodeDescription(fromModel), this.view);
                                CommandHistory.Add(cmd);
                            }
                        }
                    }
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
                        Model model = Apsim.Get(this.ApsimXFile, e.NodePath) as Model;
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
                Model model = Apsim.Get(ApsimXFile, view.Tree.SelectedNode) as Model;

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
                Model model = Apsim.Get(this.ApsimXFile, this.view.Tree.SelectedNode) as Model;

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
        /// Write all errors thrown during the loading of the <code>.apsimx</code> file.
        /// </summary>
        private void WriteLoadErrors()
        {
            if (this.ApsimXFile.LoadErrors != null)
            {
                MainPresenter.ShowError(ApsimXFile.LoadErrors);
            }
        }

        /// <summary>
        /// A helper function for creating a node description object for the specified model.
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>The description</returns>
        private TreeViewNode GetNodeDescription(IModel model)
        {
            TreeViewNode description = new TreeViewNode();
            description.Name = model.Name;

            description.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages." + model.Name + ".png";
            ManifestResourceInfo info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(description.ResourceNameForImage);
            if (info == null || (typeof(IModel).Assembly.DefinedTypes.Any(t => string.Equals(t.Name, model.Name, StringComparison.Ordinal)) && model.GetType().Name != model.Name))
                description.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages." + model.GetType().Name + ".png";
            if (typeof(Models.Functions.IFunction).IsAssignableFrom(model.GetType()))
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
            /*
            // Set the colour here
            System.Drawing.Color colour = model.Enabled ? System.Drawing.Color.Black : System.Drawing.Color.Red;
            description.Colour = colour;
            */
            return description;
        }

        #endregion
    }

    /// <summary>
    /// An object that encompasses the data that is dragged during a drag/drop operation.
    /// </summary>
    [Serializable]
    public sealed class DragObject : ISerializable
    {
        /// <summary>Path to the node</summary>
        private string nodePath;

        /// <summary>Xml string</summary>
        private string xml;

        /// <summary>Type of the model</summary>
        private Type modelType;

        /// <summary>Gets or sets the path to the node</summary>
        /// <value>The node path.</value>
        public string NodePath
        {
            get { return this.nodePath; }
            set { this.nodePath = value; }
        }

        /// <summary>Gets or sets the xml string</summary>
        /// <value>The XML.</value>
        public string Xml
        {
            get { return this.xml; }
            set { this.xml = value; }
        }

        /// <summary>Gets or sets the type of model</summary>
        /// <value>The type of the model.</value>
        public Type ModelType
        {
            get { return this.modelType; }
            set { this.modelType = value; }
        }

        /// <summary>Get data for the specified object in the xml</summary>
        /// <param name="info">Serialized object</param>
        /// <param name="context">The context</param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("NodePath", this.NodePath);
            info.AddValue("Xml", this.Xml);
        }
    }
}
