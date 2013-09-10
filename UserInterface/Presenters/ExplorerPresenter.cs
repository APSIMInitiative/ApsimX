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
        private CommandHistory CommandHistory = new CommandHistory();
        private IExplorerView View;
        private Simulations ApsimXFile;
        private ExplorerUICommands ExplorerUICommands = new ExplorerUICommands();

        /// <summary>
        /// Constructor
        /// </summary>
        public ExplorerPresenter()
        {
            
        }

        /// <summary>
        /// Attach the view to this presenter and begin populating the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            this.ApsimXFile = Model as Simulations;
            this.View = View as IExplorerView;

            this.View.AddNode(null, GetNodeDescription(ApsimXFile));

            this.View.PopulateChildNodes += OnPopulateNodes;
            this.View.NodeSelectedByUser += OnNodeSelectedByUser;
            this.View.NodeSelected += OnNodeSelected;

            this.View.AddAction("Save", Properties.Resources.Save, OnSaveClick);
            this.View.AddAction("SaveAs", Properties.Resources.SaveAs, OnSaveAsClick);
            this.View.AddAction("Undo", Properties.Resources.Undo, OnUndoClick);
            this.View.AddAction("Redo", Properties.Resources.Redo, OnRedoClick);

            this.View.AddContextAction("Copy",  Properties.Resources.Copy,  OnCopyClick);
            this.View.AddContextAction("Paste", Properties.Resources.Paste, OnPasteClick);
            this.View.AddContextAction("Run APSIM",   Properties.Resources.Run,   OnRunClick);
        }


        #region Events from view

        
        /// <summary>
        /// User has clicked on Save
        /// </summary>
        private void OnSaveClick(object sender, EventArgs e)
        {
            ApsimXFile.Write(ApsimXFile.FileName);
        }

        /// <summary>
        /// User has clicked on SaveAs
        /// </summary>
        private void OnSaveAsClick(object sender, EventArgs e)
        {
            string NewFileName = View.SaveAs(ApsimXFile.FileName);
            if (NewFileName != null)
            {
                ApsimXFile.Write(NewFileName);
                View.ChangeTabText(Path.GetFileNameWithoutExtension(NewFileName));
            }
        }

        /// <summary>
        /// User has clicked on Undo
        /// </summary>
        private void OnUndoClick(object sender, EventArgs e)
        {
            CommandHistory.Undo();
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        private void OnRedoClick(object sender, EventArgs e)
        {
            CommandHistory.Redo();
        }

        /// <summary>
        /// User has clicked on Run
        /// </summary>
        private void OnRunClick(object sender, EventArgs e)
        {
            View.AddStatusMessage("Simulation running...");
            Application.DoEvents();

            ISimulation Simulation = ApsimXFile.Get(View.CurrentNodePath) as ISimulation;
            RunCommand C = new Commands.RunCommand(ApsimXFile, Simulation);
            C.Do();
            if (C.ok)
                View.AddStatusMessage("Simulation complete");
            else
                View.AddStatusMessage("Simulation complete with errors");
        }

        public void OnPopulateNodes(string NodePath)
        {
            if (NodePath == null)
                throw new Exception("Node path is null. Cannot populate nodes");
            View.ClearNodes(NodePath);
            object Obj = ApsimXFile.Get(NodePath);
            if (Obj is IZone)
            {
                IZone Zone = Obj as IZone;
                foreach (object ChildModel in Zone.Models)
                    View.AddNode(NodePath, GetNodeDescription(ChildModel));
            }
        }

        private NodeDescription GetNodeDescription(object Model)
        {
 	        return new NodeDescription()
                {
                    Name = Utility.Reflection.Name(Model),
                    ResourceNameForImage = Model.GetType().Name + "16",
                    HasChildren = Model is IZone
                };
        }

        /// <summary>
        /// User has selected a node - store and execute a SelectNodeCommand
        /// </summary>
        void OnNodeSelectedByUser(string NodePath)
        {
            SelectNodeCommand Cmd = new SelectNodeCommand(View, NodePath);
            CommandHistory.Add(Cmd, true);
        }

        /// <summary>
        /// A node has been selected (whether by user or undo/redo)
        /// </summary>
        void OnNodeSelected(string NodePath)
        {
            // Need to display the appropriate view.
            // Looks for a view and a presenter.
            this.View.AddRightHandView(null);  // clear current view.

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

        private void OnCopyClick(object Sender, EventArgs e)
        {
            object Model = ApsimXFile.Get(View.CurrentNodePath);
            if (Model != null)
            {
                string St = Utility.Xml.Serialise(Model, false);
                Clipboard.SetText(St);
            }
        }

        private void OnPasteClick(object Sender, EventArgs e)
        {
            object ParentModel = ApsimXFile.Get(View.CurrentNodePath);
            AddModelCommand Cmd = new AddModelCommand(ParentModel, Clipboard.GetText());
            CommandHistory.Add(Cmd, true);
        }

        #endregion

        #region Privates 



        #endregion

        #region Events from model
        /// <summary>
        /// A node has been deleted.
        /// </summary>
        private void OnModelRemoved(string NodePath)
        {
            View.RemoveNode(NodePath);
        }

        private void OnModelAdded(string NodePath)
        {
            object Model = ApsimXFile.Get(NodePath);
            if (Model != null)
                View.AddNode(Utility.String.ParentName(NodePath), GetNodeDescription(Model));
        }

        #endregion

    }



}
