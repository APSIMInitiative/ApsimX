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
    using System.Windows.Forms;
    using System.Xml;
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using Importer;
    using Interfaces;
    using Models;
    using Models.Core;

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

        /// <summary>Presenter for the component</summary>
        private IPresenter currentRightHandPresenter;

        /// <summary>Using advanced mode</summary>
        private bool advancedMode = false;

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
            get { return this.view.TreeWidth; }
            set { this.view.TreeWidth = value; }
        }

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
                return this.view.SelectedNode;
            }
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

            this.view.ShortcutKeys = new Keys[] { Keys.F5 };
            this.view.SelectedNodeChanged += this.OnNodeSelected;
            this.view.DragStarted += this.OnDragStart;
            this.view.AllowDrop += this.OnAllowDrop;
            this.view.Droped += this.OnDrop;
            this.view.Renamed += this.OnRename;
            this.view.ShortcutKeyPressed += this.OnShortcutKeyPress;

            this.view.Refresh(GetNodeDescription(this.ApsimXFile));
            this.WriteLoadErrors();
            PopulateMainMenu();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.view.SelectedNodeChanged -= this.OnNodeSelected;
            this.view.DragStarted -= this.OnDragStart;
            this.view.AllowDrop -= this.OnAllowDrop;
            this.view.Droped -= this.OnDrop;
            this.view.Renamed -= this.OnRename;
            this.view.ShortcutKeyPressed -= this.OnShortcutKeyPress;
        }

        /// <summary>Toggle advanced mode.</summary>
        public void ToggleAdvancedMode()
        {
            this.advancedMode = !this.advancedMode;
            this.view.Refresh(GetNodeDescription(this.ApsimXFile));
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
                    // need to test is ApsimXFile has changed and only prompt when changes have occured.
                    // serialise ApsimXFile to buffer
                    StringWriter o = new StringWriter();
                    this.ApsimXFile.Write(o);
                    string newSim = o.ToString();

                    StreamReader simStream = new StreamReader(this.ApsimXFile.FileName);
                    string origSim = simStream.ReadToEnd(); // read original file to buffer2
                    simStream.Close();

                    int choice = 1;                           // no save
                    if (string.Compare(newSim, origSim) != 0)   
                    {
                        choice = this.view.AskToSave();
                    }

                    if (choice == -1)
                    {   // cancel
                        result = false;
                    }
                    else if (choice == 0)
                    {
                        // save
                        // Need to hide the right hand panel because some views may not have saved
                        // their contents until they get a 'Detach' call.
                        this.HideRightHandPanel();

                        this.WriteSimulation();
                        result = true;
                    }
                }
            }
            catch (Exception err)
            {
                this.view.ShowMessage("Cannot save the file. Error: " + err.Message, DataStore.ErrorLevel.Error);
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
                    this.SaveAs();

                if (this.ApsimXFile.FileName != null)
                {
                    this.ApsimXFile.Write(this.ApsimXFile.FileName);
                    return true;
                }
            }
            catch (Exception err)
            {
                this.ShowMessage("Cannot save the file. Error: " + err.Message, DataStore.ErrorLevel.Error);
            }

            return false;
        }

        /// <summary>Save the current file under a different name.</summary>
        /// <returns>True if file was saved.</returns>
        public bool SaveAs()
        {
            string newFileName = this.view.SaveAs(this.ApsimXFile.FileName);
            if (newFileName != null)
            {
                try
                {
                    if (this.ApsimXFile.FileName != null)
                        Utility.Configuration.Settings.DelMruFile(this.ApsimXFile.FileName);

                    Utility.Configuration.Settings.AddMruFile(newFileName);
                    this.ApsimXFile.Write(newFileName);
                    this.view.ChangeTabText(Path.GetFileNameWithoutExtension(newFileName));
                    return true;
                }
                catch (Exception err)
                {
                    this.ShowMessage("Cannot save the file. Error: " + err.Message, DataStore.ErrorLevel.Error);
                }
            }

            return false;
        }

        /// <summary>Toggle the second right hand side explorer view on/off</summary>
        public void ToggleSecondExplorerViewVisible()
        {
            this.view.ToggleSecondExplorerViewVisible();
        }

        /// <summary>Do the actual write to the file</summary>
        public void WriteSimulation()
        {
            this.ApsimXFile.ExplorerWidth = this.TreeWidth;
            this.ApsimXFile.Write(this.ApsimXFile.FileName);
        }

        /// <summary>Add a status message to the explorer window</summary>
        /// <param name="message">Status message</param>
        /// <param name="errorLevel">Level for the error message</param>
        public void ShowMessage(string message, Models.DataStore.ErrorLevel errorLevel)
        {
            this.view.ShowMessage(message, errorLevel);
        }

        /// <summary>
        /// Close the APSIMX user interface
        /// </summary>
        public void Close()
        {
            this.view.Close();
        }

        /// <summary>A helper function that asks user for a folder.</summary>
        /// <param name="prompt">Prompt string</param>
        /// <returns>
        /// Returns the selected folder or null if action cancelled by user.
        /// </returns>
        public string AskUserForFolder(string prompt)
        {
            return this.view.AskUserForFolder(prompt);
        }

        /// <summary>A helper function that asks user for a filename.</summary>
        /// <param name="prompt">Prompt string</param>
        /// <returns>
        /// Returns the selected folder or null if action cancelled by user.
        /// </returns>
        public string AskUserForFile(string prompt)
        {
            return this.view.AskUserForFile(prompt);
        }

        /// <summary>Select a node in the view.</summary>
        /// <param name="nodePath">Path to node</param>
        public void SelectNode(string nodePath)
        {
            this.view.SelectedNode = nodePath;
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
            if (this.view.SelectedNode == ".Standard toolbox")
            {
                this.view.SelectedNode = Apsim.FullPath(allModels[0]);
                return true;
            }

            // Find the current node in this list.
            int index = -1;
            for (int i = 0; i < allModels.Count; i++)
            {
                if (Apsim.FullPath(allModels[i]) == this.view.SelectedNode)
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
            this.view.SelectedNode = Apsim.FullPath(allModels[index + 1]);
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
            this.view.BeginRenamingCurrentNode();
        }

        /// <summary>Pastes the contents of the clipboard.</summary>
        public void Add(string xml, string parentPath)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(xml);
                object newModel = XmlUtilities.Deserialise(document.DocumentElement, Assembly.GetExecutingAssembly());

                // See if the presenter is happy with this model being added.
                Model parentModel = Apsim.Get(this.ApsimXFile, parentPath) as Model;
                AllowDropArgs allowDropArgs = new AllowDropArgs();
                allowDropArgs.NodePath = parentPath;
                allowDropArgs.DragObject = new DragObject()
                {
                    NodePath = null,
                    ModelType = newModel.GetType(),
                    Xml = System.Windows.Forms.Clipboard.GetText()
                };
                this.OnAllowDrop(null, allowDropArgs);

                // If it is happy then issue an AddModelCommand.
                if (allowDropArgs.Allow)
                {
                    // If the model xml is a soil object then try and convert from old
                    // APSIM format to new.
                    if (document.DocumentElement.Name == "Soil")
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

                    IModel child = XmlUtilities.Deserialise(document.DocumentElement, Assembly.GetExecutingAssembly()) as IModel;

                    AddModelCommand command = new AddModelCommand(parentModel, document.DocumentElement,
                                                                  GetNodeDescription(child), view);
                    this.CommandHistory.Add(command, true);
                }
            }
            catch (Exception exception)
            {
                this.ShowMessage(exception.Message, DataStore.ErrorLevel.Error);
            }
        }

        /// <summary>Deletes the specified model.</summary>
        /// <param name="model">The model to delete.</param>
        public void Delete(IModel model)
        {
            DeleteModelCommand command = new DeleteModelCommand(model, GetNodeDescription(model), this.view);
            this.CommandHistory.Add(command, true);
        }

        /// <summary>Moves the specified model up.</summary>
        /// <param name="model">The model to move.</param>
        public void MoveUp(IModel model)
        {
            MoveModelUpDownCommand command = new MoveModelUpDownCommand(model, true, this.view);
            this.CommandHistory.Add(command, true);
        }

        /// <summary>Moves the specified model down.</summary>
        /// <param name="model">The model to move.</param>
        public void MoveDown(IModel model)
        {
            MoveModelUpDownCommand command = new MoveModelUpDownCommand(model, false, this.view);
            this.CommandHistory.Add(command, true);
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
                    desc.ResourceNameForImage = "UserInterface.Resources.MenuImages." + desc.Name + ".png";

                    EventHandler handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), this.mainMenu, method);
                    desc.OnClick = handler;

                    descriptions.Add(desc);
                }
            }
            this.view.PopulateMainToolStrip(descriptions);

            string labelText = "Custom Build";
            string labelToolTip = null;

            // Get assembly title.
            Version version = new Version(Application.ProductVersion);
            if (version.Major > 0)
            {
                labelText = "Official Build";
                labelToolTip = "Version: " + version.ToString();
            }

            this.view.PopulateLabel(labelText, labelToolTip);
        }

        /// <summary>
        /// The view wants us to return a list of menu descriptions for the
        /// currently selected Node.
        /// </summary>
        private void PopulateContextMenu(string nodePath)
        {
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
                    if (contextMenuAttr.AppliesTo != null)
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
                        desc.ResourceNameForImage = "UserInterface.Resources.MenuImages." + desc.Name + ".png";
                        desc.ShortcutKey = contextMenuAttr.ShortcutKey;

                        // Check for an enabled method.
                        MethodInfo enabledMethod = typeof(ContextMenu).GetMethod(desc.ResourceNameForImage + "Enabled");
                        if (enabledMethod != null)
                        {
                            desc.Enabled = (bool)enabledMethod.Invoke(this.contextMenu, null);
                        }
                        else
                        {
                            desc.Enabled = true;
                        }

                        EventHandler handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), this.contextMenu, method);
                        desc.OnClick = handler;

                        if (desc.Name == "Advanced mode")
                        {
                            desc.Checked = this.advancedMode;
                        }

                        descriptions.Add(desc);
                    }
                }
            }

            this.view.PopulateContextMenu(descriptions);
        }

        #region Events from view

        /// <summary>A node has been dragged over another node. Allow drop?</summary>
        /// <param name="sender">Sending node</param>
        /// <param name="e">Node arguments</param>
        public void OnAllowDrop(object sender, AllowDropArgs e)
        {
            e.Allow = false;

            Model destinationModel = Apsim.Get(this.ApsimXFile, e.NodePath) as Model;
            if (destinationModel != null)
            {
                DragObject dragObject = e.DragObject as DragObject;
                ValidParentAttribute validParent = ReflectionUtilities.GetAttribute(dragObject.ModelType, typeof(ValidParentAttribute), false) as ValidParentAttribute;
                if (validParent == null || validParent.ParentModels.Length == 0)
                {
                    e.Allow = true;
                }
                else
                {
                    foreach (Type allowedParentType in validParent.ParentModels)
                    {
                        if (allowedParentType == destinationModel.GetType())
                        {
                            e.Allow = true;
                        }
                    }
                }
            }
        }

        /// <summary>A node has been selected (whether by user or undo/redo)</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Node arguments</param>
        private void OnNodeSelected(object sender, NodeSelectedArgs e)
        {
            this.HideRightHandPanel();
            this.ShowRightHandPanel();
            PopulateContextMenu(e.NewNodePath);

            Commands.SelectNodeCommand selectCommand = new SelectNodeCommand(e.OldNodePath, e.NewNodePath, this.view);
            CommandHistory.Add(selectCommand);
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
                Clipboard.SetText(xml);

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
            string toParentPath = e.NodePath;
            Model toParent = Apsim.Get(this.ApsimXFile, toParentPath) as Model;

            DragObject dragObject = e.DragObject as DragObject;
            if (dragObject != null && toParent != null)
            {
                string fromModelXml = dragObject.Xml;
                string fromParentPath = StringUtilities.ParentName(dragObject.NodePath);

                ICommand cmd = null;
                if (e.Copied)
                    Add(fromModelXml, toParentPath);
                else if (e.Moved)
                {
                    if (fromParentPath != toParentPath)
                    {
                        Model fromModel = Apsim.Get(this.ApsimXFile, dragObject.NodePath) as Model;
                        if (fromModel != null)
                        {
                            cmd = new MoveModelCommand(fromModel, toParent);
                        }
                    }
                }

                if (cmd != null)
                {
                    CommandHistory.Add(cmd);
                }
            }
        }

        /// <summary>User has renamed a node.</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Event node arguments</param>
        private void OnRename(object sender, NodeRenameArgs e)
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
                        string parentModelPath = StringUtilities.ParentName(e.NodePath);
                        RenameModelCommand cmd = new RenameModelCommand(model,  e.NewName, view);
                        CommandHistory.Add(cmd);
                        this.ShowRightHandPanel();
                        e.CancelEdit = (model.Name != e.NewName);
                    }
                }
                else
                {
                    this.ShowMessage("Use alpha numeric characters only!", DataStore.ErrorLevel.Error);
                    e.CancelEdit = true;
                }
            }
        }

        /// <summary>User has attempted to move the current node up.</summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        private void OnMoveUp(object sender, EventArgs e)
        {
            Model model = Apsim.Get(this.ApsimXFile, this.view.SelectedNode) as Model;
            
            if (model != null && model.Parent != null)
            {
                IModel firstModel = model.Parent.Children[0];
                if (model != firstModel)
                {
                    CommandHistory.Add(new Commands.MoveModelUpDownCommand(model, true, this.view));
                }
            }
        }

        /// <summary>User has attempted to move the current node down.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The args</param>
        private void OnMoveDown(object sender, EventArgs e)
        {
            Model model = Apsim.Get(this.ApsimXFile, this.view.SelectedNode) as Model;

            if (model != null && model.Parent != null)
            {
                IModel lastModel = model.Parent.Children[model.Parent.Children.Count - 1];
                if (model != lastModel)
                {
                    CommandHistory.Add(new Commands.MoveModelUpDownCommand(model, false, this.view));
                }
            }
        }

        /// <summary>User has pressed one of our shortcut keys.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnShortcutKeyPress(object sender, KeysArgs e)
        {
            if (e.Keys == Keys.F5)
            {
                ContextMenu contextMenu = new ContextMenu(this);
                contextMenu.RunAPSIM(sender, null);
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
                foreach (Exception err in this.ApsimXFile.LoadErrors)
                {
                    string message = string.Empty;
                    if (err is ApsimXException)
                    {
                        message = string.Format("[{0}]: {1}", (err as ApsimXException).model, err.Message);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(err.Source) && err.Source != "mscorlib")
                            message = "[" + err.Source + "]: ";
                        message += string.Format("{0}", err.Message + "\r\n" + err.StackTrace);
                    }
                    if (err.InnerException != null)
                        message += "\r\n" + err.InnerException.Message;

                    this.view.ShowMessage(message, DataStore.ErrorLevel.Error);
                }
            }
        }

        /// <summary>
        /// A helper function for creating a node description object for the specified model.
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>The description</returns>
        private NodeDescriptionArgs GetNodeDescription(IModel model)
        {
            NodeDescriptionArgs description = new NodeDescriptionArgs();
            description.Name = model.Name;

            string imageFileName;
            if (model is ModelCollectionFromResource &&
                (model as ModelCollectionFromResource).ResourceName != null)
                imageFileName = (model as ModelCollectionFromResource).ResourceName;
            else if (model.GetType().Name == "Plant" || model.GetType().Name == "OldPlant")
                imageFileName = model.Name;
            else
                imageFileName = model.GetType().Name;

            if (model.GetType().Namespace.Contains("Models.PMF"))
            {
                description.ToolTip = model.GetType().Name;
            }

            description.ResourceNameForImage = "UserInterface.Resources.TreeViewImages." + imageFileName + ".png";
            description.Children = new List<NodeDescriptionArgs>();
            foreach (Model child in model.Children)
            {
                if (!child.IsHidden)
                    description.Children.Add(GetNodeDescription(child));
            }

            return description;
        }

        /// <summary>Hide the right hand panel.</summary>
        private void HideRightHandPanel()
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
                    this.ShowMessage(err.Message, DataStore.ErrorLevel.Error);
                }
            }

            this.view.AddRightHandView(null);
        }

        /// <summary>Display a view on the right hand panel in view.</summary>
        private void ShowRightHandPanel()
        {
            if (this.view.SelectedNode != string.Empty)
            {
                object model = Apsim.Get(this.ApsimXFile, this.view.SelectedNode);

                if (model != null)
                {
                    ViewNameAttribute viewName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(ViewNameAttribute), false) as ViewNameAttribute;
                    PresenterNameAttribute presenterName = ReflectionUtilities.GetAttribute(model.GetType(), typeof(PresenterNameAttribute), false) as PresenterNameAttribute;

                    if (this.advancedMode)
                    {
                        viewName = new ViewNameAttribute("UserInterface.Views.GridView");
                        presenterName = new PresenterNameAttribute("UserInterface.Presenters.PropertyPresenter");
                    }

                    if (viewName != null && presenterName != null)
                    {
                        UserControl newView = Assembly.GetExecutingAssembly().CreateInstance(viewName.ToString()) as UserControl;
                        this.currentRightHandPresenter = Assembly.GetExecutingAssembly().CreateInstance(presenterName.ToString()) as IPresenter;
                        if (newView != null && this.currentRightHandPresenter != null)
                        {
                            try
                            {
                                this.view.AddRightHandView(newView);
                                this.currentRightHandPresenter.Attach(model, newView, this);
                            }
                            catch (Exception err)
                            {
                                string message = err.Message;
                                message += "\r\n" + err.StackTrace;
                                this.ShowMessage(message, DataStore.ErrorLevel.Error);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// An object that encompasses the data that is dragged during a drag/drop operation.
    /// </summary>
    [Serializable]
    public class DragObject : ISerializable
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
