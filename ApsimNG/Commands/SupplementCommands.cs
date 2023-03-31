using System;
using UserInterface.Interfaces;



namespace UserInterface.Commands
{
    using Models.Core;
    using Models.GrazPlan;

    class AddSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model to add the supplement to.</summary>
        private Supplement parent;

        /// <summary>The supplement we're to add</summary>
        private SupplementItem supplementToAdd = null;

        /// <summary>True if model was added</summary>
        private bool supplementAdded;

        private string supplementName;

        private int prevSuppIdx;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => parent;

        public AddSupplementCommand(Supplement parent, string suppName)
        {
            if (parent.ReadOnly)
                throw new ApsimXException(parent, string.Format("Unable to add supplement to {0} - it is read-only.", parent.Name));
            this.parent = parent;
            this.supplementName = suppName;
        }

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            int suppNo = SupplementLibrary.DefaultSuppConsts.IndexOf(this.supplementName);
            if (suppNo >= 0)
            {
                this.supplementToAdd = SupplementLibrary.DefaultSuppConsts[suppNo];
                this.prevSuppIdx = this.parent.CurIndex;
                this.parent.CurIndex = this.parent.Add(this.supplementName);
                this.supplementAdded = true;
                modelChanged(parent);
            }
        }

        /// <summary>Undoes the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (this.supplementAdded && this.supplementToAdd != null)
            {
                int suppIdx = this.parent.IndexOf(this.supplementToAdd.Name);
                if (suppIdx >= 0)
                    this.parent.Delete(suppIdx);
                this.parent.CurIndex = Math.Min(this.prevSuppIdx, parent.NoStores - 1);
                modelChanged(parent);
            }
        }

    }
}