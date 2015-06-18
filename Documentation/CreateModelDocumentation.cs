using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UserInterface;
using UserInterface.Commands;
using UserInterface.Presenters;
using Models;
using Models.Core;

/// <summary>
/// This script creates model documentation for a single model.
/// </summary>
public class Script
{
    public void Execute(TabbedExplorerPresenter tabbedExplorerPresenter)
    {
        // Set the current working directory to the bin folder.
        string binFolder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        Directory.SetCurrentDirectory(binFolder);

        // Get the environment variable 'ModelName'
        string modelName = System.Environment.GetEnvironmentVariable("ModelName");
        
        // Open wheat validation in a tab
        string fileName = Path.Combine(binFolder, @"..\Tests\" + modelName + @"\" + modelName + "Validation.apsimx");
        tabbedExplorerPresenter.OpenApsimXFileInTab(fileName);
    
        // Get the presenter for this tab.
        ExplorerPresenter presenter = tabbedExplorerPresenter.Presenters[0];
        presenter.SelectNode(".Simulations");

        // Export the model to HTML
        string folderName = Path.Combine(binFolder, @"..\Documentation\html\" + modelName);
        Directory.CreateDirectory(folderName);
       
        ExportNodeCommand command = new ExportNodeCommand(presenter, presenter.CurrentNodePath, folderName);
        command.Do(null);
                
        // Close the user interface.
        tabbedExplorerPresenter.Close(false);
    }
}