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
/// This script creates model documentation.
/// </summary>
public class Script
{
    public void Execute(TabbedExplorerPresenter tabbedExplorerPresenter)
    {
        // Set the current working directory to the bin folder.
        string binFolder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        Directory.SetCurrentDirectory(binFolder);

        ///////////////////////////////////////////////////////////////////
        // Wheat
        ///////////////////////////////////////////////////////////////////
        
        // Open wheat validation in a tab
        string fileName = Path.Combine(binFolder, @"..\Tests\Wheat\WheatValidation.apsimx");
        tabbedExplorerPresenter.OpenApsimXFileInTab(fileName);
    
        // Get the presenter for this tab.
        ExplorerPresenter presenter = tabbedExplorerPresenter.Presenters[0];
        ContextMenu menu = new ContextMenu(presenter);

        // Export the model to HTML
        string folderName = Path.Combine(binFolder, @"..\Documentation\html\Wheat");
        Directory.CreateDirectory(folderName);
        presenter.SelectNode(".Simulations.APS26.APS26NRate160WaterWet.paddock.Wheat");
        menu.ExportToHTML(folderName);
        presenter.SelectNode(".Simulations");
        menu.ExportToHTML(folderName);
        
        ///////////////////////////////////////////////////////////////////
        // OilPalm
        ///////////////////////////////////////////////////////////////////
        
        // Open oil palm validation in a tab
        fileName = Path.Combine(binFolder, @"..\Tests\OilPalm\OilPalmValidation.apsimx");
        tabbedExplorerPresenter.OpenApsimXFileInTab(fileName);
    
        // Get the presenter for this tab.
        presenter = tabbedExplorerPresenter.Presenters[1];
        menu = new ContextMenu(presenter);

        // Export the model to HTML
        folderName = Path.Combine(binFolder, @"..\Documentation\html\OilPalm");
        Directory.CreateDirectory(folderName);
        presenter.SelectNode(".Simulations.Sangara.Base324.Field.OilPalm");
        menu.ExportToHTML(folderName);
        presenter.SelectNode(".Simulations");
        menu.ExportToHTML(folderName);
        
        ///////////////////////////////////////////////////////////////////
        // Potato
        ///////////////////////////////////////////////////////////////////
        
        // Open potato validation in a tab
        fileName = Path.Combine(binFolder, @"..\Tests\Potato\PotatoValidation.apsimx");
        tabbedExplorerPresenter.OpenApsimXFileInTab(fileName);
    
        // Get the presenter for this tab.
        presenter = tabbedExplorerPresenter.Presenters[2];
        menu = new ContextMenu(presenter);

        // Export the model to HTML
        folderName = Path.Combine(binFolder, @"..\Documentation\html\Potato");
        Directory.CreateDirectory(folderName);
        presenter.SelectNode(".Simulations.Potato.RussetBurbank.Field.Potato");
        menu.ExportToHTML(folderName);
        presenter.SelectNode(".Simulations");
        menu.ExportToHTML(folderName);        
        
        // Close the user interface.
        tabbedExplorerPresenter.Close(false);
    }
}