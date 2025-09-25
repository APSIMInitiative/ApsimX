using System;
using System.Collections.Generic;
using APSIM.Core;
using Newtonsoft.Json;

namespace Models.Core;

/// <summary>
/// A model that contains commands that can be run.
/// </summary>
[Serializable]
[ViewName("UserInterface.Views.EditorView")]
[PresenterName("UserInterface.Presenters.EditorPresenter")]
[ValidParent(ParentType = typeof(Simulations))]
[ValidParent(ParentType = typeof(Simulation))]
[ValidParent(ParentType = typeof(Folder))]
public class ModelCommands : Model//, ILineEditor
{
    //private CommandProcessor commands;

    /*/// <summary>The lines to return to the editor.</summary>
    [JsonIgnore]
    public IEnumerable<string> Lines
    {
        get { return commands?.ToStrings(); }
        set { commands = CommandProcessor.Create(value); }
    }
*/
    /// <summary>
    /// Run all commands.
    /// </summary>
    public void Run()
    {
        //commands?.Run(this);
    }

}
