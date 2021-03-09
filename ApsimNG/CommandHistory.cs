using System;
using System.Collections.Generic;
using Models.Core;
using UserInterface.Commands;
using UserInterface.Interfaces;

namespace UserInterface
{
    public delegate void Changed(bool haveUnsavedChanges);
    public delegate void ModelChangedDelegate(object changedModel);
    public delegate void ModelStructureChangedDelegate(ICommand command);

    /// <summary>
    /// Simple Command history class.
    /// </summary>
    /// <remarks>
    /// Based on http://www.catnapgames.com/blog/2009/03/19/simple-undo-redo-system-for-csharp.html
    /// </remarks>
    public class CommandHistory
    {
        private List<ICommand> commands = new List<ICommand>();
        private int lastExecuted = -1;
        private int lastSaved = -1;
        private bool inUndoRedo = false;
        private ITreeView tree;

        public event ModelChangedDelegate ModelChanged;
        public event ModelStructureChangedDelegate OnUndo;
        public event ModelStructureChangedDelegate OnRedo;
        public event ModelStructureChangedDelegate OnDo;

        public CommandHistory(ITreeView tree) => this.tree = tree;

        public void Clear()
        {
            commands.Clear();
            lastExecuted = -1;
            lastSaved = -1;
        }

        public void Save()
        {
            lastSaved = lastExecuted;
        }

        public void Limit(int numCommands)
        {
            while (commands.Count > numCommands)
            {
                commands.RemoveAt(0);
                if (lastExecuted >= 0)
                {
                    lastExecuted--;
                }
                if (lastSaved >= 0)
                {
                    lastSaved--;
                }
            }
        }

        public void Add(ICommand command, bool execute = true)
        {
            if (inUndoRedo)
                return;
            if (lastExecuted + 1 < commands.Count)
            {
                int numCommandsToRemove = commands.Count - (lastExecuted + 1);
                for (int i = 0; i < numCommandsToRemove; i++)
                {
                    commands.RemoveAt(lastExecuted + 1);
                }
                lastSaved = -1;
            }
            commands.Add(command);
            lastExecuted = commands.Count - 1; 

            if (execute)
                command.Do(tree, InvokeModelChanged);

            if (OnDo != null)
                OnDo(command);
        }

        public void Undo()
        {
            if (lastExecuted >= 0)
            {
                if (commands.Count > 0)
                {
                    inUndoRedo = true;
                    try
                    {
                        commands[lastExecuted].Undo(tree, InvokeModelChanged);
                        lastExecuted--;
                        if (OnUndo != null)
                            OnUndo(commands[lastExecuted + 1]);
                    }
                    finally
                    {
                        inUndoRedo = false;
                    }
                }
            }
        }

        public void Redo()
        {
            if (lastExecuted + 1 < commands.Count)
            {
                inUndoRedo = true;
                try
                {
                    commands[lastExecuted + 1].Do(tree, InvokeModelChanged);
                    lastExecuted++;
                    if (OnRedo != null)
                        OnRedo(commands[lastExecuted]);
                }
                finally
                {
                    inUndoRedo = false;
                }
            }
        }

        private void InvokeModelChanged(object model)
        {
            if (ModelChanged != null && model != null)
                ModelChanged(model);
        }
    }
}
