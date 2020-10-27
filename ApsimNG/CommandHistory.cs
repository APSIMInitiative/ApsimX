using System;
using System.Collections.Generic;
using Models.Core;
using UserInterface.Commands;
using UserInterface.Presenters;

namespace UserInterface
{
    public delegate void Changed(bool haveUnsavedChanges);
    public delegate void ModelChangedDelegate(object changedModel);
    public delegate void ModelStructureChangedDelegate(IModel model);

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

        public event Changed OnChanged = (h) => { };
        public event ModelChangedDelegate ModelChanged;
        public event ModelStructureChangedDelegate OnUndo;
        public event ModelStructureChangedDelegate OnRedo;
        public event ModelStructureChangedDelegate ModelStructureChanged;

        public void Clear()
        {
            commands.Clear();
            lastExecuted = -1;
            lastSaved = -1;

            OnChanged(false);
        }

        public void Save()
        {
            lastSaved = lastExecuted;

            OnChanged(false);
        }

        public bool Modified
        {
            get { return lastSaved != lastExecuted; }
        }

        public int Size
        {
            get { return commands.Count; }
        }

        public int LastExecuted
        {
            get { return lastExecuted; }
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
            {
                command.Do(this);
            }

            OnChanged(true);
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
                        commands[lastExecuted].Undo(this);
                        lastExecuted--;
                        OnChanged(lastExecuted != lastSaved);
                        OnUndo(commands[lastExecuted + 1].AffectedModel);
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
                    commands[lastExecuted + 1].Do(this);
                    lastExecuted++;
                    OnChanged(lastExecuted != lastSaved);
                    OnRedo(commands[lastExecuted].AffectedModel);
                }
                finally
                {
                    inUndoRedo = false;
                }
            }
        }

        public void InvokeModelChanged(object model)
        {
            if (ModelChanged != null && model != null)
                ModelChanged(model);
        }

        public void InvokeModelStructureChanged(IModel model)
        {
            if (ModelStructureChanged != null && model != null)
                ModelStructureChanged(model);
        }
    }
}
