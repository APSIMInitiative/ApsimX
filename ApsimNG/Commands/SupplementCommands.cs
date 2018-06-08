using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Commands
{
    using Models.Core;
    using Models.GrazPlan;
    using Interfaces;
    class AddSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model to add the supplement to.</summary>
        private Supplement parent;

        /// <summary>The supplement we're to add</summary>
        private TSupplementItem supplementToAdd = null;

        /// <summary>True if model was added</summary>
        private bool supplementAdded;

        private string supplementName;

        private int prevSuppIdx;

        public AddSupplementCommand(Supplement parent, string suppName)
        {
            this.parent = parent;
            this.supplementName = suppName;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            int iSuppNo = TSupplementLibrary.DefaultSuppConsts.IndexOf(this.supplementName);
            if (iSuppNo >= 0)
            {
                this.supplementToAdd = TSupplementLibrary.DefaultSuppConsts[iSuppNo];
                this.prevSuppIdx = this.parent.curIndex;
                this.parent.curIndex = this.parent.Add(this.supplementName);
                this.supplementAdded = true;
                commandHistory.InvokeModelChanged(this.parent);
            }
        }

        /// <summary>Undoes the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (this.supplementAdded && this.supplementToAdd != null)
            {
                int suppIdx = this.parent.IndexOf(this.supplementToAdd.Name);
                if (suppIdx >= 0)
                    this.parent.Delete(suppIdx);
                this.parent.curIndex = Math.Min(this.prevSuppIdx, parent.NoStores - 1);
                commandHistory.InvokeModelChanged(this.parent);
            }
        }

    }

    class DeleteSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model from which to delete the supplement.</summary>
        private Supplement parent;

        /// <summary>The supplement we're to add</summary>
        private TSupplementItem supplementToDelete = null;

        /// <summary>True if model was Deleted</summary>
        private bool supplementDeleted;

        private int prevSuppIdx;

        public DeleteSupplementCommand(Supplement parent, TSupplementItem supplementItem)
        {
            this.parent = parent;
            this.supplementToDelete = supplementItem;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            int iSuppNo = parent.IndexOf(this.supplementToDelete);
            this.prevSuppIdx = this.parent.curIndex;
            this.parent.Delete(iSuppNo);
            this.parent.curIndex = Math.Min(this.prevSuppIdx, parent.NoStores - 1);
            this.supplementDeleted = true;
            commandHistory.InvokeModelChanged(this.parent);
        }

        /// <summary>Undoes the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (this.supplementDeleted && this.supplementToDelete != null)
            {
                this.parent.curIndex = this.parent.Add(supplementToDelete);
                commandHistory.InvokeModelChanged(this.parent);
            }
        }

    }

    /// <summary>This command records changes in the 'suppIdx' in the Supplement view and presenter.</summary>
    class SelectSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model for which the selection is changing.</summary>
        private Supplement parent;

        /// <summary>The old index</summary>
        private int prevSuppIdx;

        /// <summary>The new index</summary>
        private int newSuppIdx;

        /// <summary>Constructor.</summary>
        /// <param name="parent">The Supplement model.</param>
        /// <param name="oldIdx">The old index.</param>
        /// <param name="newIdx">The new index.</param>
        public SelectSupplementCommand(Supplement parent, int oldIdx, int newIdx)
        {
            this.parent = parent;
            this.prevSuppIdx = oldIdx;
            this.newSuppIdx = newIdx;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            this.parent.curIndex = newSuppIdx;
            commandHistory.InvokeModelChanged(this.parent);
        }

        /// <summary>Undo the command</summary>
        /// <param name="CommandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            this.parent.curIndex = prevSuppIdx;
            commandHistory.InvokeModelChanged(this.parent);
        }
    }

    class ResetSupplementCommand : ICommand
    {
        /// <summary>The supplement parent model for which the reset is occurring.</summary>
        private Supplement parent;

        /// <summary>
        /// The active list of supplements
        /// </summary>
        private List<TSupplementItem> suppList;

        /// <summary>
        /// The list of supplements prior to the reset
        /// </summary>
        private List<TSupplementItem> prevList;

        /// <summary>True if model was Deleted</summary>
        private bool supplementsReset = false;

        /// <summary>Constructor.</summary>
        /// <param name="parent">The old index.</param>
        /// <param name="supplements">List of supplements to reset</param>
        public ResetSupplementCommand(Supplement parent, List<TSupplementItem> supplements)
        {
            this.parent = parent;
            this.suppList = supplements;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            if (!supplementsReset) // First call; store a copy of the original values
            {
                prevList = new List<TSupplementItem>(this.suppList.Count);
                for (int i = 0; i < this.suppList.Count; i++)
                {
                    TSupplementItem newItem = new TSupplementItem();
                    newItem.Assign(suppList[i]);
                    prevList.Add(newItem);
                }
            }
            foreach (TSupplementItem supp in suppList)
            {
                int suppNo = TSupplementLibrary.DefaultSuppConsts.IndexOf(supp.Name);
                if (suppNo >= 0)
                {
                    string name = supp.Name;
                    double amount = supp.Amount;
                    supp.Assign(TSupplementLibrary.DefaultSuppConsts[suppNo]);
                    supp.Name = name;
                    supp.Amount = amount;
                }
            }
            supplementsReset = true;
            if (this.parent.curIndex > 0)
                commandHistory.InvokeModelChanged(this.parent[this.parent.curIndex]);
        }

        /// <summary>Undo the command</summary>
        /// <param name="CommandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (supplementsReset)
            {
                for (int i = 0; i < this.prevList.Count; i++)
                    suppList[i].Assign(prevList[i]);
            }
            if (this.parent.curIndex > 0)
                commandHistory.InvokeModelChanged(this.parent[this.parent.curIndex]);
        }
    }
}