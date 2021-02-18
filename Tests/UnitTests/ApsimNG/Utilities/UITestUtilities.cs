using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Models.Core;
using Models.Soils;
using UnitTests.ApsimNG.Utilities;
using UserInterface.Presenters;
using UserInterface.Views;

namespace UnitTests.ApsimNG
{
    /// <summary>
    /// Utility functions used by the UI tests.
    /// </summary>
    public static class UITestUtilities
    {
        /// <summary>
        /// Gets a resource from a given assembly as a string.
        /// </summary>
        /// <param name="assembly">The assembly in which the resource is stored.</param>
        /// <param name="resourceName">Name of the resource.</param>
        public static string GetResource(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
        }
        
        /// <summary>
        /// Opens an .apsimx file stored as an embedded resource in a given assembly in a new tab.
        /// </summary>
        public static ExplorerPresenter OpenResourceFileInTab(Assembly assembly, string resourceName)
        {
            string json = GetResource(assembly, resourceName);
            string fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, json);
            return UITestsMain.MasterPresenter.OpenApsimXFileInTab(fileName, onLeftTabControl: true);
        }

        /// <summary>
        /// Opens a simple .apsimx file in the GUI.
        /// </summary>
        public static ExplorerPresenter OpenBasicFileInGui()
        {
            Simulations sims = UnitTests.Utilities.GetRunnableSim();
            string fileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            sims.Write(fileName);
            return UITestsMain.MasterPresenter.OpenApsimXFileInTab(fileName, onLeftTabControl: true);
        }
    }
}
