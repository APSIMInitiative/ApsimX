using System;
using System.Collections.Generic;
using UserInterface;
using UserInterface.Commands;
using UserInterface.Presenters;
using Models;
using Models.Core;

/// <summary>
/// This script selects all nodes in the standard toolbox, testing to 
/// see that no error is thrown.
/// </summary>
public class Script
{
    public void Execute(TabbedExplorerPresenter tabbedExplorerPresenter)
    {
        // Open test.apsimx in a tab
        tabbedExplorerPresenter.OpenApsimXFileInTab(@"..\Tests\Test.apsimx");
    
        // Get the presenter for this tab.
        ExplorerPresenter presenter = tabbedExplorerPresenter.Presenters[0];

        // Select the field model.
        presenter.SelectNode(".Simulations.Test");

        // Copy the simulation model.
        ContextMenu menu = new ContextMenu(presenter);
        menu.OnCopyClick(null, null);
        
        // Select the top model
        presenter.SelectNode(".Simulations");

        // Paste the model.
        menu.OnPasteClick(null, null);
        
        // Make sure the paste has worked by clicking on a child. 
        presenter.SelectNode(".Simulations.Test1.Clock");
        
        // Make sure the parenting of children has worked correctly.
        Clock clock = Apsim.Get(presenter.ApsimXFile, ".Simulations.Test1.Clock") as Clock;
        if (clock.Parent == null)
            throw new Exception("Parenting of models after copy/paste hasn't worked");
        
        // Close the user interface.
        tabbedExplorerPresenter.Close(false);
    }


}