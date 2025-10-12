using System;
using System.Collections.Generic;
using APSIM.Core;
using Models.Core.Run;
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
public class ModelCommands : Model, ILineEditor
{
    /// <summary>The lines to return to the editor.</summary>
    [JsonIgnore]
    public IEnumerable<string> Lines { get; set; }

    /// <summary>
    /// Run all commands.
    /// </summary>
    public void Run()
    {
        var runner = new Runner(this);
        //runner.SimulationCompleted += OnJobCompleted;
        //runner.AllSimulationsCompleted += OnAllJobsCompleted;

        var processor = new CommandProcessor(CommandLanguage.StringToCommands(Lines, this), runner);
        processor.Run(this);
    }

}
