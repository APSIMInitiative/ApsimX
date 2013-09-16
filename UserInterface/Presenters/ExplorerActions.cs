using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;
using UserInterface.Commands;
using UserInterface.Views;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace UserInterface.Presenters
{
    /// <summary>
    /// This class contains methods for all 'actions' that the ExplorerView exposes to the user.
    /// </summary>
    /// <remarks>
    /// Two types of actions: 
    /// Main Tool Bar
    ///     [MainmenuName] - decorate methods with this attribute.
    /// Context (popup) menu:
    ///     [ContextMenuName] - methods with this will show the specified menu name.
    ///     [ContextModelType] - optional. If present the menu will only show for the specified
    ///                          model types. Can have multiple of these attributes.
    ///</remarks>
    class ExplorerActions
    {
        private ExplorerPresenter ExplorerPresenter;
        private IExplorerView ExplorerView;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExplorerActions(ExplorerPresenter ExplorerPresenter, IExplorerView ExplorerView)
        {
            this.ExplorerPresenter = ExplorerPresenter;
            this.ExplorerView = ExplorerView;
        }

        #region Main menu

        /// <summary>
        /// User has clicked on Save
        /// </summary>
        [MainMenuName("Save")]
        public void OnSaveClick(object sender, EventArgs e)
        {
            ExplorerPresenter.ApsimXFile.Write(ExplorerPresenter.ApsimXFile.FileName);
        }

        /// <summary>
        /// User has clicked on SaveAs
        /// </summary>
        [MainMenuName("Save As")]
        public void OnSaveAsClick(object sender, EventArgs e)
        {
            string NewFileName = ExplorerView.SaveAs(ExplorerPresenter.ApsimXFile.FileName);
            if (NewFileName != null)
            {
                ExplorerPresenter.ApsimXFile.Write(NewFileName);
                ExplorerView.ChangeTabText(Path.GetFileNameWithoutExtension(NewFileName));
            }
        }

        /// <summary>
        /// User has clicked on Undo
        /// </summary>
        [MainMenuName("Undo")]
        public void OnUndoClick(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.Undo();
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        [MainMenuName("Redo")]
        public void OnRedoClick(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.Redo();
        }

        /// <summary>
        /// User has clicked on Redo
        /// </summary>
        [MainMenuName("Split screen")]
        public void ToggleSecondExplorerViewVisible(object sender, EventArgs e)
        {
            ExplorerView.ToggleSecondExplorerViewVisible();
        }

        #endregion

        #region Context menu

        /// <summary>
        /// User has clicked Copy
        /// </summary>
        [ContextMenuName("Copy")]
        public void OnCopyClick(object Sender, EventArgs e)
        {
            object Model = ExplorerPresenter.ApsimXFile.Get(ExplorerView.CurrentNodePath);
            if (Model != null)
            {
                string St = Utility.Xml.Serialise(Model, false);
                Clipboard.SetText(St);
            }
        }

        /// <summary>
        /// User has clicked Paste
        /// </summary>
        [ContextMenuName("Paste")]
        public void OnPasteClick(object Sender, EventArgs e)
        {
            try
            {
                XmlDocument Doc = new XmlDocument();
                Doc.LoadXml(Clipboard.GetText());
                object NewModel = Utility.Xml.Deserialise(Doc.DocumentElement);

                // See if the presenter is happy with this model being added.
                IZone ParentModel = ExplorerPresenter.ApsimXFile.Get(ExplorerView.CurrentNodePath) as IZone;
                AllowDropArgs AllowDropArgs = new Views.AllowDropArgs();
                AllowDropArgs.NodePath = ExplorerView.CurrentNodePath;
                AllowDropArgs.DragObject = new DragObject()
                {
                    NodePath = null,
                    ModelType = NewModel.GetType(),
                    Xml = Clipboard.GetText()
                };
                ExplorerPresenter.OnAllowDrop(null, AllowDropArgs);

                // If it is happy then issue an AddModelCommand.
                if (AllowDropArgs.Allow)
                {
                    AddModelCommand Cmd = new AddModelCommand(Clipboard.GetText(), ExplorerView.CurrentNodePath, ParentModel);
                    ExplorerPresenter.CommandHistory.Add(Cmd, true);
                }
            }
            catch (Exception)
            {
                // invalid xml from clipboard.
            }
        }

        /// <summary>
        /// User has clicked Delete
        /// </summary>
        [ContextMenuName("Delete")]
        public void OnDeleteClick(object Sender, EventArgs e)
        {
            object Model = ExplorerPresenter.ApsimXFile.Get(ExplorerView.CurrentNodePath);
            string ParentPath = Utility.String.ParentName(ExplorerView.CurrentNodePath);
            object ParentModel = ExplorerPresenter.ApsimXFile.Get(ParentPath);
            if (Model.GetType().Name != "Simulations" && Model != null && ParentModel != null && ParentModel is IZone)
            {
                DeleteModelCommand Cmd = new DeleteModelCommand(ParentModel as IZone, ParentPath, Model);
                ExplorerPresenter.CommandHistory.Add(Cmd, true);
            }
        }


        ///// <summary>
        ///// User has clicked rename
        ///// </summary>
        //[ContextMenuName("Rename")]
        //public void OnRenameClick(object Sender, EventArgs e)
        //{
        //    (ExplorerView as ExplorerView).DoRename();
        //}

        /// <summary>
        /// Event handler for a User interface "Run APSIM" action
        /// </summary>
        [ContextModelType(typeof(Model.Core.Simulation))]
        [ContextMenuName("Run APSIM")]
        public void RunAPSIM(object Sender, EventArgs e)
        {
            ExplorerView.AddStatusMessage("Simulation running...");

            ISimulation Simulation = ExplorerPresenter.ApsimXFile.Get(ExplorerView.CurrentNodePath) as ISimulation;
            RunCommand C = new Commands.RunCommand(ExplorerPresenter.ApsimXFile, Simulation);
            C.Do(null);
            if (C.ok)
                ExplorerView.AddStatusMessage("Simulation complete");
            else
                ExplorerView.AddStatusMessage("Simulation complete with errors");

        }

        #endregion



    }
}
