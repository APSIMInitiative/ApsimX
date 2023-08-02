using System;
using System.Collections.Generic;
using UserInterface;
using UserInterface.Commands;
using UserInterface.Presenters;
using UserInterface.Views;
using System.IO;
using System.Reflection;
using Models;
using Models.Core;
using Models.Core.Run;

/// <summary>
/// This script selects all nodes in the wheat example, then closes the
/// tab and attempts to delete the .db file without closing the UI.
/// </summary>
public class Script
{
    public void Execute(MainPresenter mainPresenter)
    {
		// Can't have any ExplorerPresenter local variables as they
		// would intefere with garbage collection. To workaround this,
		// some functionality needs to be hidden away in methods.

		// Open the wheat example in a tab.
		string bin = Path.GetDirectoryName(typeof(MainPresenter).Assembly.Location);
        string apsimx = Path.Combine(bin, "..", "..", "..");
		string wheatExample = Path.Combine(apsimx, "Examples", "Wheat.apsimx");
		string tempFile = Path.GetTempFileName();
		File.Copy(wheatExample, tempFile, true);
		OpenFile(mainPresenter, tempFile);

		// Cycle through all nodes in the simulations tree.
        CycleThroughNodes(mainPresenter);
		RunFile(mainPresenter);
		// Close the tab.
		mainPresenter.CloseTab(0, onLeft: true);
		while (GLib.MainContext.Iteration());

		// Try and delete the .db file.
		string dbFile = Path.ChangeExtension(tempFile, ".db");
		GC.Collect();
		GC.WaitForPendingFinalizers();
		File.Delete(dbFile);

        // Close the user interface.
        mainPresenter.Close(askToSave:false);
    }

	private void OpenFile(MainPresenter presenter, string file)
	{
        presenter.OpenApsimXFileInTab(file, true);
	}

    private void CycleThroughNodes(MainPresenter mainPresenter)
    {
        // Get the presenter for this tab.
        ExplorerPresenter presenter = mainPresenter.Presenters1[0] as ExplorerPresenter;
        if (presenter == null)
            throw new Exception("Unable to open wheat example.");

        // Loop through all nodes in the standard toolbox and select each in turn.
        while (presenter.SelectNextNode()) ;

		presenter = null;
    }

	private void RunFile(MainPresenter mainPresenter)
	{
		ExplorerPresenter presenter = mainPresenter.Presenters1[0] as ExplorerPresenter;
        if (presenter == null)
            throw new Exception("Unable to open wheat example.");

		IClock clock = presenter.ApsimXFile.FindInScope<IClock>();
		clock.EndDate = clock.StartDate.AddDays(10);

		Runner runner = new Runner(presenter.ApsimXFile, runType: Runner.RunTypeEnum.MultiThreaded);
		RunCommand command = new RunCommand("Simulations", runner, presenter);
		command.Do();
	}
}
