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
		// Open the wheat example in a tab
		string bin = Path.GetDirectoryName(typeof(MainPresenter).Assembly.Location);
        string apsimx = Path.Combine(bin, "..");
		string wheatExample = Path.Combine(apsimx, "Examples", "Wheat.apsimx");
        mainPresenter.OpenApsimXFileInTab(wheatExample, true);
		
        // Get the presenter for this tab.
        ExplorerPresenter presenter = mainPresenter.Presenters1[0] as ExplorerPresenter;
		if (presenter == null)
			throw new Exception("Unable to open wheat example.");
		
		// Loop through all nodes in the standard toolbox and select each in turn.
		while (presenter.SelectNextNode());
		
		// Close the tab.
		mainPresenter.CloseTab(0, onLeft: true);
		
		// Try and delete the .db file.
		string dbFile = Path.ChangeExtension(wheatExample, ".db");
		File.Delete(dbFile);
		
        // Close the user interface.
        mainPresenter.Close(askToSave:false);
    }
}
