using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Models.Core;

namespace Utility
{

    /// <summary>Stores user settings and other information which is persistent between restarts of the GUI.</summary>
    public class Configuration
    {
        /// <summary>The instance</summary>
        private static Configuration instance = null;

        /// <summary>The configuration file</summary>
        private string configurationFile = null;

        /// <summary>The location for the form</summary>
        public Point MainFormLocation { get; set; }

        /// <summary>The size of the main form</summary>
        public Size MainFormSize { get; set; }

        /// <summary>The state (max, min, norm) of the form</summary>
        public bool MainFormMaximized { get; set; }

        /// <summary>List of the most recently opened files</summary>
        public List<ApsimFileMetadata> MruList { get; set; }

        /// <summary>The maximum number of files allowed in the mru list</summary>
        [Input("Number of files in history")]
        public int FilesInHistory { get; set; }

        /// <summary>Position of split screen divider.</summary>
        /// <remarks>Percentage 0-100</remarks>
        public int SplitScreenPosition { get; set; }

        /// <summary>Position of split screen divider.</summary>
        /// <remarks>Percentage 0-100</remarks>
        public int TreeSplitScreenPosition { get; set; }

        /// <summary>The previous folder where a file was opened or saved</summary>
        public string PreviousFolder { get; set; }

        /// <summary>The previous height of the status panel</summary>
        /// <remarks>Percentage 0-100</remarks>
        public double StatusPanelHeight { get; set; }

        /// <summary>
        /// The position of the splitter between the variables
        /// and frequency text editors in the report UI.
        /// </summary>
        public double ReportSplitterPosition { get; set; }

        /// <summary>
        /// The position of the splitter between the variables/event text editors
        /// and the common report/event ListViews.
        /// </summary>
        public double ReportSplitterVerticalPosition { get; set; }

        /// <summary>Keeps track of whether the dark theme is enabled.</summary>
        [Input("Dark theme enabled", OnChanged = nameof(OnDarkThemeToggled))]
        public bool DarkTheme { get; set; }

        /// <summary>Should the file be automatically saved to disk before running simulations?</summary>
        [Input("Autosave on run")]
        [Tooltip("Should the file be automatically saved to disk before running simulations?")]
        public bool AutoSave { get; set; } = true;

        /// <summary>If true, the GUI will not play a sound when simulations finish running.</summary>
        [Input("Mute all sound effects")]
        public bool Muted { get; set; } = true;

        /// <summary>
        /// In theory, if there are any commands in the command history,
        /// then the file has been modified. In practice, there may be
        /// some faulty presenters which make changes to the model without
        /// using the command history.
        /// </summary>
        [Input("Use faster file closing algorithm")]
        [Tooltip("This will mostly eliminate the pause when closing a file, but it may cause apsim to fail to prompt to save the file in some cases.")]
        public bool UseFastFileClose { get; set; }

        [Input("Enable graph debugging output")]
        [Tooltip("Outputs messages in the status bar if data is missing, is outside axis bounds or is NaN. Useful for debugging Observed/Predicted graphs.")]
        public bool EnableGraphDebuggingMessages { get; set; } = false;

        /// <summary>Return the name of the summary file JPG.</summary>
        public string SummaryPngFileName
        {
            get
            {
                // Make sure the summary JPG exists in the configuration folder.
                string summaryJpg = Path.Combine(ConfigurationFolder, "ApsimSummary.png");
                if (!File.Exists(summaryJpg))
                {
                    try
                    {
                        Gdk.Pixbuf b = Gdk.Pixbuf.LoadFromResource("Apsim1.png");
                        b.Save(summaryJpg, "jpeg");
                    }
                    catch
                    {

                    }

                }
                return summaryJpg;
            }
        }

        /// <summary>
        /// Version number of the configuration settings.
        /// </summary>
        public int Version { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Organisation { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Postcode { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }

        /// <summary>The maximum number of rows to show on a report grid</summary>
        [Input("Default max number of rows to show in datastore")]
        public int MaximumRowsOnReportGrid { get; set; }

        /// <summary>
        /// Store the style name used in the editor
        /// </summary>
        /// <remarks>
        /// This should probably be user controllable, but we would need a way of
        /// providing a list of valid values for the drop-down box.
        /// </remarks>
        public string EditorStyleName { get; set; } = "Visual Studio";

        /// <summary>
        /// Store the zoom level for editors
        /// </summary>
        [Input("Default zoom level for text editors")]
        public double EditorZoom { get; set; } = 1.0;

        /// <summary>
        /// Store the user's preferred font size
        /// </summary>
        public double BaseFontSize { get; set; } = 12.5;

        /// <summary>
        /// Simulation complete wav file.
        /// </summary>
        [FileInput(".wav file to play when simulation finishes", ".wav")]
        [Tooltip("Leave empty for default sound effect")]
        public string SimulationCompleteWavFileName { get; set; }

        /// <summary>
        /// Simulation complete with error wav file.
        /// </summary>
        [FileInput(".wav file to play when simulation finishes with error")]
        [Tooltip("Leave empty for default sound effect")]
        public string SimulationCompleteWithErrorWavFileName { get; set; }

        /// <summary>
        /// Stores the user's preferred font.
        /// </summary>
        [FontInput("Font")]
        public string FontName { get; set; } = "Segoe UI 11";

        /// <summary>
        /// Stores the user's preferred font for the manager script text editor.
        /// </summary>
        [FontInput("Font used in manager script editor")]
        public string EditorFontName { get; set; } = "monospace 10";

        /// <summary>
        /// Country name used in the download soil view.
        /// </summary>
        public string DownloadFromDataSourceCountry { get; set; }

        public ApsimFileMetadata GetMruFile(string fileName)
        {
            return MruList.Find(f => f.FileName == fileName);
        }

        /// <summary>Add a filename to the list.</summary>
        /// <param name="file">File metadata.</param>
        public void AddMruFile(ApsimFileMetadata file)
        {
            if (file.FileName.Length > 0) // Do we really need this check?
            {
                if (MruList.Count > 0)
                {
                    int index = MruList.FindIndex(f => f.FileName == file.FileName);
                    if (index < 0)
                    {
                        // First time that filename has been added 
                        if (MruList.Count >= FilesInHistory)
                            MruList.RemoveAt(MruList.Count - 1);  // Delete the last item
                    }
                    else
                    {
                        // Item is in the history list => move to top
                        file = MruList[index];
                        MruList.RemoveAt(index);
                    }
                    MruList.Insert(0, file);
                }
                else
                    MruList.Add(file);
            }
        }

        /// <summary>Remove a specified file from the list</summary>
        /// <param name="fileName">The file name to delete</param>
        public void DelMruFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || MruList == null || MruList.Count < 1)
                return;

            int index = MruList.FindIndex(f => f.FileName == fileName);
            if (index >= 0)
                MruList.RemoveAt(index);
        }

        public void SetExpandedNodes(string fileName, TreeNode[] nodes)
        {
            ApsimFileMetadata file = GetMruFile(fileName);
            if (file == null)
            {
                file = new ApsimFileMetadata(fileName, nodes);
                AddMruFile(file);
            }
            else
                file.ExpandedNodes = nodes;
        }

        /// <summary>Rename a specified file in the list</summary>
        /// <param name="fileName">The file name to rename</param>
        /// <param name="newName">The new file name</param>
        public void RenameMruFile(string fileName, string newName)
        {
            if (string.IsNullOrEmpty(fileName) || MruList == null || MruList.Count < 1)
                return;

            int index = MruList.FindIndex(f => f.FileName == fileName);
            if (index >= 0)
            {
                MruList.RemoveAt(index);
                MruList.Insert(index, new ApsimFileMetadata(newName));
            }
        }

        /// <summary>Clean the list by removing missing files</summary>
        public void CleanMruList()
        {
            if (MruList == null || MruList.Count < 1)
                return;

            for (int i = MruList.Count - 1; i >= 0; i--)
            {
                string filename = MruList[i].FileName;
                if (!File.Exists(filename))
                    DelMruFile(filename);
            }
        }

        /// <summary>Return the configuration folder.</summary>
        /// <value>The configuration folder.</value>
        private static string ConfigurationFolder
        {
            get
            {
                // On Linux and Mac the path will be .config/
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "ApsimInitiative",
                                    "ApsimX");
            }
        }

        /// <summary>Private constructor</summary>
        private Configuration() { }

        /// <summary>Finalizes an instance of the <see cref="Configuration"/> class.</summary>
        ~Configuration()
        {
            try
            {
                Save();
            }
            catch
            {
                // An uncaught exception could crash APSIM from the GC thread.
            }
        }

        /// <summary>Gets the configuration settings.</summary>
        public static Configuration Settings
        {
            get
            {
                if (instance != null)
                    return instance;

                string configurationFile = Path.Combine(ConfigurationFolder, "ApsimX.xml");
                // deserialise the file
                if (File.Exists(configurationFile))
                {
                    try
                    {
                        instance = ConfigurationConverter.DoConvert(configurationFile);
                    }
                    catch
                    {
                        // Fallback to old method.
                        System.Xml.Serialization.XmlSerializer xmlreader = new System.Xml.Serialization.XmlSerializer(typeof(Configuration));
                        StreamReader filereader = null;

                        // Dean (Oct 2014): I changed the class that is serialized from Settings to Configuration.
                        // This will cause the code below to throw. When this happens just delete the old
                        // configuration file.
                        try
                        {
                            filereader = new StreamReader(configurationFile);
                            instance = (Configuration)xmlreader.Deserialize(filereader);
                            filereader.Close();
                        }
                        catch (Exception)
                        {
                            filereader.Close();
                            //File.Delete(configurationFile);
                        }
                    }
                }

                if (instance == null)
                {
                    instance = new Configuration();
                    instance.MainFormSize = new Size(640, 480);
                    instance.MainFormMaximized = true;
                    instance.MruList = new List<ApsimFileMetadata>();
                    instance.PreviousFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    instance.FilesInHistory = 20;
                }

                if (instance.FilesInHistory == 0)
                    instance.FilesInHistory = 20;

                instance.configurationFile = configurationFile;
                return instance;
            }
        }

        /// <summary>Store the configuration settings to file</summary>
        public void Save()
        {
            string configPath = Path.GetDirectoryName(configurationFile);
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
            StreamWriter filewriter = new StreamWriter(configurationFile);
            System.Xml.Serialization.XmlSerializer xmlwriter = new System.Xml.Serialization.XmlSerializer(typeof(Configuration), typeof(Configuration).GetNestedTypes());
            xmlwriter.Serialize(filewriter, Settings);
            filewriter.Close();
        }

        /// <summary>
        /// This will be called whenever the 'dark mode' option is toggled.
        /// It will change the default editor style to something.
        /// </summary>
        private void OnDarkThemeToggled()
        {

            EditorStyleName = DarkTheme ? "Adwaita-dark" : "Adwaita";

        }
    }
}
