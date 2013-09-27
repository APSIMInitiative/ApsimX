using UserInterface.Commands;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using UserInterface.Views;
using Models.Core;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

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
        public CommandHistory CommandHistory { get; set; }
        public Simulations ApsimXFile { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExplorerPresenter() { }

        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
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

            this.CommandHistory.ModelStructureChanged += OnModelStructureChanged;
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
            CommandHistory.ModelStructureChanged -= OnModelStructureChanged;
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
            object SelectedModel = ApsimXFile.Get(View.CurrentNodePath);

            // Go look for all [UserInterfaceAction]
            foreach (MethodInfo Method in typeof(ExplorerActions).GetMethods())
            {
                ContextModelType ContextModelType = Utility.Reflection.GetAttribute(Method, typeof(ContextModelType), false) as ContextModelType;
                ContextMenuName ContextMenuName = Utility.Reflection.GetAttribute(Method, typeof(ContextMenuName), false) as ContextMenuName;
                if (ContextMenuName != null &&
                    (ContextModelType == null || ContextModelType.ModelType == SelectedModel.GetType()))
                {
                    MenuDescriptionArgs.Description Desc = new MenuDescriptionArgs.Description();
                    Desc.Name = ContextMenuName.MenuName;
                    Desc.ResourceNameForImage = Desc.Name.Replace(" ", "");

                    EventHandler Handler = (EventHandler) Delegate.CreateDelegate(typeof(EventHandler), ExplorerActions, Method);
                    Desc.OnClick = Handler;

                    e.Descriptions.Add(Desc);
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
                ModelCollection Model = ApsimXFile.Get(e.NodePath) as ModelCollection;
                if (Model != null)
                {
                    foreach (Model ChildModel in Model.Models)
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
            Model Obj = ApsimXFile.Get(e.NodePath) as Model;
            if (Obj != null)
            {
                DragObject DragObject = new DragObject();
                DragObject.NodePath = e.NodePath;
                DragObject.ModelType = Obj.GetType();
                DragObject.Xml = Utility.Xml.Serialise(Obj, false);
                e.DragObject = DragObject;
            }
        }
        
        /// <summary>
        /// A node has been dragged over another node. Allow drop?
        /// </summary>
        public void OnAllowDrop(object sender, AllowDropArgs e)
        {
            e.Allow = false;

            Model DestinationModel = ApsimXFile.Get(e.NodePath) as Model;
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
            ModelCollection ToParent = ApsimXFile.Get(ToParentPath) as ModelCollection;

            DragObject DragObject = e.DragObject as DragObject;
            if (DragObject != null && ToParent != null)
            {
                string FromModelXml = DragObject.Xml;
                string FromParentPath = Utility.StringUtil.ParentName(DragObject.NodePath);

                ICommand Cmd = null;
                if (e.Copied)
                    Cmd = new AddModelCommand(FromModelXml, ToParent);
                else if (e.Moved)
                {
                    if (FromParentPath != ToParentPath)
                    {
                        Model FromModel = ApsimXFile.Get(DragObject.NodePath) as Model;
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
            Model Model = ApsimXFile.Get(e.NodePath) as Model;
            if (Model != null && Model.GetType().Name != "Simulations" && e.NewName != null && e.NewName != "")
            {
                HideRightHandPanel();
                string ParentModelPath = Utility.StringUtil.ParentName(e.NodePath);
                RenameModelCommand Cmd = new RenameModelCommand(Model, ParentModelPath, e.NewName);
                CommandHistory.Add(Cmd);
                View.CurrentNodePath = ParentModelPath + "." + e.NewName;
                ShowRightHandPanel();
            }            
        }


        #endregion

        #region Privates 

        /// <summary>
        /// A helper function for creating a node description object for the specified model.
        /// </summary>
        private NodeDescriptionArgs.Description GetNodeDescription(Model Model)
        {
            return new NodeDescriptionArgs.Description()
            {
                Name = Model.Name,
                ResourceNameForImage = Model.GetType().Name + "16",
                HasChildren = Model is ModelCollection
            };
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
                object Model = ApsimXFile.Get(View.CurrentNodePath);

                ViewName ViewName = Utility.Reflection.GetAttribute(Model.GetType(), typeof(ViewName), false) as ViewName;
                PresenterName PresenterName = Utility.Reflection.GetAttribute(Model.GetType(), typeof(PresenterName), false) as PresenterName;

                if (ViewName != null && PresenterName != null)
                {
                    UserControl NewView = Assembly.GetExecutingAssembly().CreateInstance(ViewName.ToString()) as UserControl;
                    CurrentRightHandPresenter = Assembly.GetExecutingAssembly().CreateInstance(PresenterName.ToString()) as IPresenter;
                    if (NewView != null && CurrentRightHandPresenter != null)
                    {
                        View.AddRightHandView(NewView);
                        CurrentRightHandPresenter.Attach(Model, NewView, CommandHistory);
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
            Model Model = ApsimXFile.Get(ModelPath) as Model;
            View.InvalidateNode(ModelPath, GetNodeDescription(Model));
        }

        #endregion

    }



}
