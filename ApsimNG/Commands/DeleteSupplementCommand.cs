using System;

namespace UserInterface.Commands
{
    using Models.Core;
    using Models.GrazPlan;
    using Interfaces;

    class DeleteSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model from which to delete the supplement.</summary>
        private Supplement parent;

        /// <summary>The supplement we're to add</summary>
        private SupplementItem supplementToDelete = null;

        /// <summary>True if model was Deleted</summary>
        private bool supplementDeleted;

        private int prevSuppIdx;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => parent;

        public DeleteSupplementCommand(Supplement parent, SupplementItem supplementItem)
        {
            if (parent.ReadOnly)
                throw new ApsimXException(parent, string.Format("Unable to delete {0} - it is read-only.", parent.Name));
            this.parent = parent;
            this.supplementToDelete = supplementItem;
        }

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            int suppNo = parent.IndexOf(this.supplementToDelete);
            this.prevSuppIdx = this.parent.CurIndex;
            this.parent.Delete(suppNo);
            this.parent.CurIndex = Math.Min(this.prevSuppIdx, parent.NoStores - 1);
            this.supplementDeleted = true;
            modelChanged(this.parent);
        }

        /// <summary>Undoes the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (this.supplementDeleted && this.supplementToDelete != null)
            {
                this.parent.CurIndex = this.parent.Add(supplementToDelete);
                modelChanged(this.parent);
            }
        }
    }
}