using UserInterface.Commands;
using System;
using System.Reflection;
using System.Windows.Forms;
using UserInterface.Views;
using Models.Core;
using System.Runtime.Serialization;
using System.IO;
using Models;
using System.Collections.Generic;
using System.Data;

namespace UserInterface.Presenters
{
    /// <summary>
    /// An object that encompases the data that is dragged during a drag/drop operation.
    /// </summary>
    [Serializable]
    public class DragObject : ISerializable
    {
        public string NodePath;
        public string Xml;
        public Type ModelType;

        void ISerializable.GetObjectData(SerializationInfo oInfo, StreamingContext oContext)
        {
            oInfo.AddValue("NodePath", NodePath);
            oInfo.AddValue("Xml", Xml);
        }
    }



    /// <summary>
    /// This presenter class is responsible for populating the view
    /// passed into the constructor and handling all user interaction of 
    /// the view. Humble Dialog pattern.
    /// </summary>
    public class ExplorerPresenter : IPresenter
    {
        private IExplorerView View;
        private ExplorerActions ExplorerActions;
        private IPresenter CurrentRightHandPresenter;
        private bool AdvancedMode = false;
        //private DataStore store = null;

        public CommandHistory CommandHistory { get; set; }
        public Simulations ApsimXFile { get; set; }

        public Int32 TreeWidth 
        {
            get { return this.View.TreeWidth; }
            set { this.View.TreeWidth = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExplorerPresenter() { }

        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            this.CommandHistory = new CommandHistory();
            this.ApsimXFile = Model as Simulations;
            this.View = View as IExplorerView;
            this.ExplorerActions = new ExplorerActions(this, this.View);

            this.View.PopulateChildNodes += OnPopulateNodes;
            this.View.PopulateContextMenu += OnPopulateContextMenu;
            this.View.PopulateMainMenu += OnPopulateMainMenu;
            this.View.NodeSelectedByUser += OnNodeSelectedByUser;
            this.View.NodeSelected += OnNodeSelected;
            this.View.DragStart += OnDragStart;
            this.View.AllowDrop += OnAllowDrop;
            this.View.Drop += OnDrop;
            this.View.Rename += OnRename;
            this.View.OnMoveDown += OnMoveDown;
            this.View.OnMoveUp += OnMoveUp;

            this.CommandHistory.ModelStructureChanged += OnModelStructureChanged;


            WriteLoadErrors();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            View.PopulateChildNodes -= OnPopulateNodes;
            View.PopulateContextMenu -= OnPopulateContextMenu;
            View.PopulateMainMenu -= OnPopulateMainMenu;
            View.NodeSelectedByUser -= OnNodeSelectedByUser;
            View.NodeSelected -= OnNodeSelected;
            View.DragStart -= OnDragStart;
            View.AllowDrop -= OnAllowDrop;
            View.Drop -= OnDrop;
            View.Rename -= OnRename;
            View.OnMoveDown -= OnMoveDown;
            View.OnMoveUp -= OnMoveUp;

            CommandHistory.ModelStructureChanged -= OnModelStructureChanged;
        }

        /// <summary>
        /// Toggle advanced mode.
        /// </summary>
        public void ToggleAdvancedMode()
        {
            AdvancedMode = !AdvancedMode;
            View.InvalidateNode(".Simulations", GetNodeDescription(ApsimXFile));
        }

        /// <summary>
        /// Called by TabbedExplorerPresenter to do a save. Return true if all ok.
        /// </summary>
        public bool Save()
        {
            bool result = true;
            try
            {
                if (ApsimXFile != null && ApsimXFile.FileName != null)
                {
                    // need to test is ApsimXFile has changed and only prompt when changes have occured.
                    // serialise ApsimXFile to buffer
                    StringWriter o = new StringWriter();
                    ApsimXFile.Write(o);
                    string newSim = o.ToString();

                    StreamReader simStream = new StreamReader(ApsimXFile.FileName);
                    string origSim = simStream.ReadToEnd(); // read original file to buffer2
                    simStream.Close();

                    Int32 choice = 1;                           // no save
                    if (String.Compare(newSim, origSim) != 0)   // do comparison
                    {
                        choice = View.AskToSave();
                    }
                    if (choice == -1)                           // cancel
                        result = false;
                    else if (choice == 0)                       // save
                    {
                        WriteSimulation();
                        result = true;
                    }
                }
            }
            catch (Exception err)
            {
                View.ShowMessage("Cannot save the file. Error: " + err.Message, DataStore.ErrorLevel.Error);
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Do the actual write to the file
        /// </summary>
        public void WriteSimulation()
        {
            ApsimXFile.ExplorerWidth = TreeWidth;
            ApsimXFile.Write(ApsimXFile.FileName);
        }

        /// <summary>
        /// Write all errors thrown during the loading of the .apsimx file.
        /// </summary>
        private void WriteLoadErrors()
        {
            if (ApsimXFile.LoadErrors != null)
                foreach (ApsimXException err in ApsimXFile.LoadErrors)
                {
                    string message = String.Format("{0}:\n{1}", new object[] {
                                                    err.ModelFullPath,
                                                    err.Message});
                    View.ShowMessage(message, DataStore.ErrorLevel.Error);
                }
        }

        /// <summary>
        /// Add a status message to the explorer window
        /// </summary>
        public void ShowMessage(string Message, Models.DataStore.ErrorLevel errorLevel)
        {
            View.ShowMessage(Message, errorLevel);
        }

        /// <summary>
        /// Return the current right hand presenter.
        /// </summary>
        public IPresenter CurrentPresenter { get { return CurrentRightHandPresenter; } }

        /// <summary>
        /// Select a node in the view.
        /// </summary>
        public void SelectNode(string nodePath)
        {
            View.CurrentNodePath = nodePath;
        }

        /// <summary>
        /// Select the next node in the view. The next node is defined as the next one
        /// down in the tree view. It will go through child nodes if they exist.
        /// Will return true if next node was successfully selected. Will return
        /// false if no more nodes to select.
        /// </summary>
        public bool SelectNextNode()
        {
            // Get a complete list of all models in this file.
            List<Model> allModels = ApsimXFile.Children.AllRecursively;

            // If the current node path is '.Simulations' (the root node) then
            // select the first item in the 'allModels' list.
            if (View.CurrentNodePath == ".Simulations")
            {
                View.CurrentNodePath = allModels[0].FullPath;
                return true;
            }

            // Find the current node in this list.
            int index = -1;
            for (int i = 0; i < allModels.Count; i++)
            {
                if (allModels[i].FullPath == View.CurrentNodePath)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
                throw new Exception("Cannot find the current selected model in the .apsimx file");

            // If the current model is the last one in the list then return false.
            if (index == allModels.Count - 1)
                return false;

            // Select the next node.
            View.CurrentNodePath = allModels[index + 1].FullPath;
            return true;
        }

        #region Events from view

        /// <summary>
        /// The view wants us to return a list of menu descriptions for the 
        /// main menu.
        /// </summary>
        private void OnPopulateMainMenu(object sender, MenuDescriptionArgs e)
        {
            // Go look for all [UserInterfaceAction]
            foreach (MethodInfo Method in typeof(ExplorerActions).GetMethods())
            {
                MainMenuName MainMenuName = Utility.Reflection.GetAttribute(Method, typeof(MainMenuName), false) as MainMenuName;
                if (MainMenuName != null)
                {
                    MenuDescriptionArgs.Description Desc = new MenuDescriptionArgs.Description();
                    Desc.Name = MainMenuName.MenuName;
                    Desc.ResourceNameForImage = Desc.Name.Replace(" ", "");

                    EventHandler Handler = (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), ExplorerActions, Method);
                    Desc.OnClick = Handler;

                    e.Descriptions.Add(Desc);
                }
            }
        }

        /// <summary>
        /// The view wants us to return a list of menu descriptions for the 
        /// currently selected Node.
        /// </summary>
        private void OnPopulateContextMenu(object sender, MenuDescriptionArgs e)
        {
            // Get the selected model.
            object SelectedModel = ApsimXFile.Variables.Get(View.CurrentNodePath);

            // Go look for all [UserInterfaceAction]
            foreach (MethodInfo Method in typeof(ExplorerActions).GetMethods())
            {
                ContextMenuName ContextMenuName = Utility.Reflection.GetAttribute(Method, typeof(ContextMenuName), false) as ContextMenuName;
                if (ContextMenuName != null)
                {
                    // Get a list of allowed types.
                    List<Type> allowedMenuTypes = new List<Type>();
                    foreach (ContextModelType contextModelType in Utility.Reflection.GetAttributes(Method, typeof(ContextModelType), false))
                        allowedMenuTypes.Add(contextModelType.ModelType);


                    if (allowedMenuTypes.Count == 0 || allowedMenuTypes.Contains(SelectedModel.GetType()))
                    {
                        MenuDescriptionArgs.Description Desc = new MenuDescriptionArgs.Description();
                        Desc.Name = ContextMenuName.MenuName;
                        Desc.ResourceNameForImage = Desc.Name.Replace(" ", "");

                        EventHandler Handler = (EventHandler) Delegate.CreateDelegate(typeof(EventHandler), ExplorerActions, Method);
                        Desc.OnClick = Handler;

                        if (Desc.Name == "Advanced mode")
                            Desc.Checked = AdvancedMode;

                        e.Descriptions.Add(Desc);
                    }
                }
            }
        }
        
        /// <summary>
        /// The view wants us to populate the view with child nodes of the specified NodePath.
        /// </summary>
        private void OnPopulateNodes(object Sender, NodeDescriptionArgs e)
        {
            if (e.NodePath == null)
            {
                // Add in a root node.
                e.Descriptions.Add(GetNodeDescription(ApsimXFile));
            }
            else
            {
                Model Model = ApsimXFile.Variables.Get(e.NodePath) as Model;
                if (Model != null)
                {
                    foreach (Model ChildModel in Model.Children.All)
                        if (!(ChildModel.Name == "Script"))
                            e.Descriptions.Add(GetNodeDescription(ChildModel));
                }
            }
        }

        /// <summary>
        /// User has selected a node - store and execute a SelectNodeCommand
        /// </summary>
        private void OnNodeSelectedByUser(object Sender, NodeSelectedArgs e)
        {
            SelectNodeCommand Cmd = new SelectNodeCommand(View, e.OldNodePath, e.NewNodePath);
            CommandHistory.Add(Cmd, true);
            OnNodeSelected(Sender, e);
        }

        /// <summary>
        /// A node has been selected (whether by user or undo/redo)
        /// </summary>
        private void OnNodeSelected(object Sender, NodeSelectedArgs e)
        {
            HideRightHandPanel();
            ShowRightHandPanel();
        }

        /// <summary>
        /// A node has begun to be dragged.
        /// </summary>
        private void OnDragStart(object sender, DragStartArgs e)
        {
            Model Obj = ApsimXFile.Variables.Get(e.NodePath) as Model;
            if (Obj != null)
            {
                string xml = Obj.Serialise();
                Clipboard.SetText(xml);

                DragObject DragObject = new DragObject();
                DragObject.NodePath = e.NodePath;
                DragObject.ModelType = Obj.GetType();
                DragObject.Xml = xml;
                e.DragObject = DragObject;
            }
        }
        
        /// <summary>
        /// A node has been dragged over another node. Allow drop?
        /// </summary>
        public void OnAllowDrop(object sender, AllowDropArgs e)
        {
            e.Allow = false;

            Model DestinationModel = ApsimXFile.Variables.Get(e.NodePath) as Model;
            if (DestinationModel != null)
            {
                DragObject DragObject = e.DragObject as DragObject;
                Attribute[] DropAttributes = Utility.Reflection.GetAttributes(DragObject.ModelType, typeof(AllowDropOn), false);
                if (DropAttributes.Length == 0)
                    e.Allow = true;
                else
                {
                    foreach (AllowDropOn AllowDropOn in DropAttributes)
                    {
                        if (AllowDropOn.ModelTypeName == DestinationModel.GetType().Name)
                            e.Allow = true;
                    }
                }
            }
        }

        /// <summary>
        /// A node has been dropped.
        /// </summary>
        private void OnDrop(object sender, DropArgs e)
        {
            string ToParentPath = e.NodePath;
            Model ToParent = ApsimXFile.Variables.Get(ToParentPath) as Model;

            DragObject DragObject = e.DragObject as DragObject;
            if (DragObject != null && ToParent != null)
            {
                string FromModelXml = DragObject.Xml;
                string FromParentPath = Utility.String.ParentName(DragObject.NodePath);

                ICommand Cmd = null;
                if (e.Copied)
                    Cmd = new AddModelCommand(FromModelXml, ToParent);
                else if (e.Moved)
                {
                    if (FromParentPath != ToParentPath)
                    {
                        Model FromModel = ApsimXFile.Variables.Get(DragObject.NodePath) as Model;
                        if (FromModel != null)
                        {
                            Cmd = new MoveModelCommand(FromModel, ToParent);
                        }
                    }
                }

                if (Cmd != null)
                    CommandHistory.Add(Cmd);
            }
        }

        /// <summary>
        /// User has renamed a node.
        /// </summary>
        private void OnRename(object sender, NodeRenameArgs e)
        {
            Model Model = ApsimXFile.Variables.Get(e.NodePath) as Model;
            if (Model != null && Model.GetType().Name != "Simulations" && e.NewName != null && e.NewName != "")
            {
                HideRightHandPanel();
                string ParentModelPath = Utility.String.ParentName(e.NodePath);
                RenameModelCommand Cmd = new RenameModelCommand(Model, ParentModelPath, e.NewName);
                CommandHistory.Add(Cmd);
                View.CurrentNodePath = ParentModelPath + "." + e.NewName;
                ShowRightHandPanel();
            }            
        }

        /// <summary>
        /// User has attempted to move the current node up.
        /// </summary>
        private void OnMoveUp(object sender, EventArgs e)
        {
            Model model = ApsimXFile.Variables.Get(View.CurrentNodePath) as Model;
            
            if (model != null && model.Parent != null)
            {
                Model firstModel = model.Parent.Models[0];
                if (model != firstModel)
                    CommandHistory.Add(new Commands.MoveModelUpDownCommand(View, model, up: true));
            }
        }

        /// <summary>
        /// User has attempted to move the current node down.
        /// </summary>
        private void OnMoveDown(object sender, EventArgs e)
        {
            Model model = ApsimXFile.Variables.Get(View.CurrentNodePath) as Model;

            if (model != null && model.Parent != null)
            {
                Model lastModel = model.Parent.Models[model.Parent.Models.Count-1];
                if (model != lastModel)
                    CommandHistory.Add(new Commands.MoveModelUpDownCommand(View, model, up: false));
            }
        }


        #endregion

        #region Privates 

        /// <summary>
        /// A helper function for creating a node description object for the specified model.
        /// </summary>
        private NodeDescriptionArgs.Description GetNodeDescription(Model Model)
        {
            NodeDescriptionArgs.Description description = new NodeDescriptionArgs.Description();
            description.Name = Model.Name;
            description.ResourceNameForImage = Model.GetType().Name + "16";
            description.HasChildren = Model.Children.All.Count > 0;
            return description;
        }

        /// <summary>
        /// Hide the right hand panel.
        /// </summary>
        private void HideRightHandPanel()
        {
            if (CurrentRightHandPresenter != null)
            {
                CurrentRightHandPresenter.Detach();
                CurrentRightHandPresenter = null;
            }
            this.View.AddRightHandView(null);
        }

        /// <summary>
        /// Display a view on the right hand panel in view.
        /// </summary>
        private void ShowRightHandPanel()
        {
            if (View.CurrentNodePath != "")
            {
                object Model = ApsimXFile.Variables.Get(View.CurrentNodePath);

                if (Model != null)
                {
                    ViewName ViewName = Utility.Reflection.GetAttribute(Model.GetType(), typeof(ViewName), false) as ViewName;
                    PresenterName PresenterName = Utility.Reflection.GetAttribute(Model.GetType(), typeof(PresenterName), false) as PresenterName;

                    if (AdvancedMode)
                    {
                        ViewName = new ViewName("UserInterface.Views.GridView");
                        PresenterName = new PresenterName("UserInterface.Presenters.PropertyPresenter");
                    }

                    if (ViewName != null && PresenterName != null)
                    {
                        UserControl NewView = Assembly.GetExecutingAssembly().CreateInstance(ViewName.ToString()) as UserControl;
                        CurrentRightHandPresenter = Assembly.GetExecutingAssembly().CreateInstance(PresenterName.ToString()) as IPresenter;
                        if (NewView != null && CurrentRightHandPresenter != null)
                        {
                            try
                            {
                                View.AddRightHandView(NewView);
                                CurrentRightHandPresenter.Attach(Model, NewView, this);
                            }
                            catch (Exception err)
                            {
                                string message = err.Message;
                                message += "\r\n" + err.StackTrace;
                                ShowMessage(message, DataStore.ErrorLevel.Error);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Events from model

        /// <summary>
        /// The model structure has changed. Tell the view about it.
        /// </summary>
        void OnModelStructureChanged(string ModelPath)
        {
            Model Model = ApsimXFile.Variables.Get(ModelPath) as Model;
            View.InvalidateNode(ModelPath, GetNodeDescription(Model));
        }

        #endregion

    }



}
