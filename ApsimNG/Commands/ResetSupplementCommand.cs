using System;
using System.Collections.Generic;

namespace UserInterface.Commands
{
    using Models.Core;
    using Models.GrazPlan;
    using Interfaces;

    class ResetSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model for which the reset is occurring.</summary>
        private Supplement parent;

        /// <summary>
        /// The active list of supplements
        /// </summary>
        private List<SupplementItem> suppList;

        /// <summary>
        /// The list of supplements prior to the reset
        /// </summary>
        private List<SupplementItem> prevList;

        /// <summary>True if model was Deleted</summary>
        private bool supplementsReset = false;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => parent;

        /// <summary>Constructor.</summary>
        /// <param name="parent">The old index.</param>
        /// <param name="supplements">List of supplements to reset</param>
        public ResetSupplementCommand(Supplement parent, List<SupplementItem> supplements)
        {
            if (parent.ReadOnly)
                throw new ApsimXException(parent, string.Format("Unable to reset {0} - it is read-only.", parent.Name));
            this.parent = parent;
            this.suppList = supplements;
        }

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            if (parent.ReadOnly)
                throw new ApsimXException(parent, string.Format("Unable to modify {0} - it is read-only.", parent.Name));
            if (!supplementsReset) // First call; store a copy of the original values
            {
                prevList = new List<SupplementItem>(this.suppList.Count);
                for (int i = 0; i < this.suppList.Count; i++)
                {
                    SupplementItem newItem = new SupplementItem();
                    newItem.Assign(suppList[i]);
                    prevList.Add(newItem);
                }
            }
            foreach (SupplementItem supp in suppList)
            {
                int suppNo = SupplementLibrary.DefaultSuppConsts.IndexOf(supp.Name);
                if (suppNo >= 0)
                {
                    string name = supp.Name;
                    double amount = supp.Amount;
                    supp.Assign(SupplementLibrary.DefaultSuppConsts[suppNo]);
                    supp.Name = name;
                    supp.Amount = amount;
                }
            }
            supplementsReset = true;
            if (this.parent.CurIndex > 0)
                modelChanged(this.parent[this.parent.CurIndex]);
        }

        /// <summary>Undo the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (supplementsReset)
            {
                for (int i = 0; i < this.prevList.Count; i++)
                    suppList[i].Assign(prevList[i]);
            }
            if (this.parent.CurIndex > 0)
                modelChanged(this.parent[this.parent.CurIndex]);
        }
    }
}