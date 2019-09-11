using System;
using System.Collections.Generic;
using UserInterface;
using UserInterface.Commands;
using UserInterface.Presenters;
using UserInterface.Views;
using System.IO;
using System.Reflection;

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
        string apsimx = Path.Combine(bin, "..");
		string wheatExample = Path.Combine(apsimx, "Examples", "Wheat.apsimx");
		OpenFile(mainPresenter, wheatExample);
		
		// Cycle through all nodes in the simulations tree.
        CycleThroughNodes(mainPresenter);

		// Close the tab.
		mainPresenter.CloseTab(0, onLeft: true);
		while (GLib.MainContext.Iteration());
			
		// Try and delete the .db file.
		string dbFile = Path.ChangeExtension(wheatExample, ".db");
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
}
