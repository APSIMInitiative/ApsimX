using System;
using System.IO;
using UserInterface;
using UserInterface.Classes;
using UserInterface.Presenters;

/// <summary>
/// This script attempts to resolve the path to an image which is an embedded
/// resource in the ApsimNG assembly. The image name is passed in with
/// incorrect case.
/// </summary>
public class Script
{
    public void Execute(MainPresenter mainPresenter)
    {
		// The image is actually called SoltaniChickpeaPhenology.png. 
        HtmlToMigraDoc.GetImagePath("SoltaniChickpeaPhenology.PNG", Path.GetTempPath());
		
        // Close the user interface.
        mainPresenter.Close(askToSave:false);
    }
}