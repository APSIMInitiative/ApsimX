using UserInterface.Commands;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using UserInterface.Views;
using Model.Core;
using System.IO;

namespace UserInterface.Presenters
{


    /// <summary>
    /// This presenter class is responsible for populating the view
    /// passed into the constructor and handling all user interaction of 
    /// the view. Humble Dialog pattern.
    /// </summary>
    public class ExplorerPresenter : IPresenter
    {
        private IExplorerView View;
        private ExplorerActions ExplorerActions;
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
                object Obj = ApsimXFile.Get(e.NodePath);
                if (Obj is IZone)
                {
                    IZone Zone = Obj as IZone;
                    foreach (object ChildModel in Zone.Models)
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
            // Need to display the appropriate view.
            // Looks for a view and a presenter.
            this.View.AddRightHandView(null);  // clear current view.

            if (View.CurrentNodePath != "")
            {
                object Model = ApsimXFile.Get(View.CurrentNodePath);

                ViewName ViewName = Utility.Reflection.GetAttribute(Model.GetType(), typeof(ViewName), false) as ViewName;
                PresenterName PresenterName = Utility.Reflection.GetAttribute(Model.GetType(), typeof(PresenterName), false) as PresenterName;

                if (ViewName != null && PresenterName != null)
                {
                    UserControl NewView = Assembly.GetExecutingAssembly().CreateInstance(ViewName.ToString()) as UserControl;
                    IPresenter NewPresenter = Assembly.GetExecutingAssembly().CreateInstance(PresenterName.ToString()) as IPresenter;
                    if (NewView != null && NewPresenter != null)
                    {
                        View.AddRightHandView(NewView);
                        NewPresenter.Attach(Model, NewView, CommandHistory);
                    }
                }
            }
        }

        #endregion

        #region Privates 

        /// <summary>
        /// A helper function for creating a node description object for the specified model.
        /// </summary>
        private NodeDescriptionArgs.Description GetNodeDescription(object Model)
        {
            return new NodeDescriptionArgs.Description()
            {
                Name = Utility.Reflection.Name(Model),
                ResourceNameForImage = Model.GetType().Name + "16",
                HasChildren = Model is IZone
            };
        }


        #endregion

        #region Events from model
        /// <summary>
        /// A node has been deleted.
        /// </summary>
        private void OnModelRemoved(string NodePath)
        {
            string ParentPath = Utility.String.ParentName(NodePath);
            object ParentModel = ApsimXFile.Get(ParentPath);
            View.InvalidateNode(ParentPath, GetNodeDescription(ParentModel));
        }

        #endregion

    }



}
