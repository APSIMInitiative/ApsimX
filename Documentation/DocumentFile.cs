using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UserInterface;
using UserInterface.Commands;
using UserInterface.Presenters;
using Models;
using Models.Core;

public class Script
{
	private MainPresenter masterPresenter;
	private string binFolder;
    public void Execute(MainPresenter mainPresenter)
    {
		masterPresenter = mainPresenter;

        // Set the current working directory to the bin folder.
        binFolder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        Directory.SetCurrentDirectory(binFolder);

        // Get the environment variable 'FileToDocument'
        string fileName = System.Environment.GetEnvironmentVariable("FileToDocument");
        
        // Document the specified file.
        Document(fileName);
    }
	
	private void Document(string fileName)
	{
		try
		{
			if (File.Exists(fileName))
			{
				masterPresenter.OpenApsimXFileInTab(fileName, true);
				// Get the presenter for this tab.
				ExplorerPresenter presenter = masterPresenter.presenters1[0] as ExplorerPresenter;
				if (presenter != null)
				{
					presenter.SelectNode(".Simulations");

					// Export the file to PDF
					string folderName = Path.Combine(binFolder, "..", "Documentation", "PDF");
					Directory.CreateDirectory(folderName);

					ExportNodeCommand command = new ExportNodeCommand(presenter, presenter.CurrentNodePath);
					command.Do(null);

					// Copy the file into the PDF directory.
					string outputFileName = Path.ChangeExtension(Path.Combine(folderName, Path.GetFileNameWithoutExtension(fileName)), ".pdf");
					File.Move(command.FileNameWritten, outputFileName);
				}
			}
			else
				throw new Exception(string.Format("Attempted to document file {0} but this file does not exist.", fileName));
		}
		finally
		{
			// Close the user interface.
			masterPresenter.Close(false);
		}
    }
}
