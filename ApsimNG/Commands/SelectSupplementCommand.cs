using System;

namespace UserInterface.Commands
{
    using Models.Core;
    using Models.GrazPlan;
    using Interfaces;

    /// <summary>This command records changes in the 'suppIdx' in the Supplement view and presenter.</summary>
    class SelectSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model for which the selection is changing.</summary>
        private Supplement parent;

        /// <summary>The old index</summary>
        private int prevSuppIdx;

        /// <summary>The new index</summary>
        private int newSuppIdx;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => parent;

        /// <summary>Constructor.</summary>
        /// <param name="parent">The Supplement model.</param>
        /// <param name="oldIdx">The old index.</param>
        /// <param name="newIdx">The new index.</param>
        public SelectSupplementCommand(Supplement parent, int oldIdx, int newIdx)
        {
            if (parent.ReadOnly)
                throw new ApsimXException(parent, string.Format("Unable to select supplement in {0} - it is read-only.", parent.Name));
            this.parent = parent;
            this.prevSuppIdx = oldIdx;
            this.newSuppIdx = newIdx;
        }

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            this.parent.CurIndex = newSuppIdx;
            modelChanged(this.parent);
        }

        /// <summary>Undo the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            this.parent.CurIndex = prevSuppIdx;
            modelChanged(this.parent);
        }
    }
}