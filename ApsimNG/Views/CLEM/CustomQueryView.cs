using System;
using System.IO;
using Gtk;
using UserInterface.Interfaces;
using UserInterface.Views;
using Utility;

namespace ApsimNG.Views.CLEM
{
    /// <summary>
    /// Displays the result of a Custom SQL Query on a DataTable
    /// </summary>
    class CustomQueryView : ViewBase
    {
        // Components of the interface that are interactable
        // Taken from CustomQueryView.glade
        private Notebook notebook1 = null;
        private Entry entry1 = null;
        private Button loadbtn = null;
        private Button savebtn = null;
        private Button saveasbtn = null;
        private Button runbtn = null;
        private TextView textview1 = null;

        // Custom gridview added after reading the glade file
        public GridView gridview1 { get; set; } = null;

        // Raw text containing the SQL query
        public string Sql
        {
            get
            {
                // Read SQL in from the text view box
                return textview1.Buffer.Text;
            }
            set
            {
                // Overwrite the text view box with SQL
                textview1.Buffer.Text = value;
            }
        }
        
        // File containing SQL in raw text
        public string Filename
        {
            get
            {
                // Take the filename from the entry box
                return entry1.Text;
            }
            set
            {
                // Assign the filename to the entry box text
                entry1.Text = value;
            }
        }

        /// <summary>
        /// Instantiate the View
        /// </summary>
        /// <param name="owner"></param>
        public CustomQueryView(ViewBase owner) : base(owner)
        {
            // Read in the glade file
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.CustomQueryView.glade");

            // Assign the interactable objects from glade
            notebook1 = (Notebook)builder.GetObject("notebook1");
            entry1 = (Entry)builder.GetObject("entry1");
            loadbtn = (Button)builder.GetObject("loadbtn");
            savebtn = (Button)builder.GetObject("savebtn");
            saveasbtn = (Button)builder.GetObject("saveasbtn");
            runbtn = (Button)builder.GetObject("runbtn");
            textview1 = (TextView)builder.GetObject("textview1");

            // Add the custom gridview (external to glade)
            gridview1 = new GridView(owner);
            gridview1.ReadOnly = true;
            Label data = new Label("Data");
            notebook1.AppendPage(gridview1.MainWidget, data);

            // Assign methods to the event handlers for the buttons
            loadbtn.Clicked += OnLoadClicked;
            savebtn.Clicked += OnSaveClicked;
            saveasbtn.Clicked += OnSaveAsClicked;
            runbtn.Clicked += OnRunClicked;

            // Let the viewbase know which widget is the main widget
            _mainWidget = notebook1;
        }

        // Handler for executing the SQL query
        public event EventHandler OnRunQuery;

        // Handler for loading SQL from a file
        public event EventHandler OnLoadFile;

        /// <summary>
        /// Select an SQL query file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoadClicked(object sender, EventArgs e)
        {
            try
            {
                IFileDialog fileDialog = new FileDialog()
                {
                    Prompt = "Select query file.",
                    Action = FileDialog.FileActionType.Open,
                    FileType = "SQL files (*.sql)|*.sql"
                };

                string filename = fileDialog.GetFile();
                entry1.Text = filename;

                string sql = File.ReadAllText(filename);

                textview1.Buffer.Text = sql;
                
            }
            catch (Exception error)
            {
                ShowError(error);
            }

            if (OnLoadFile != null)
            {
                OnLoadFile.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Select an SQL query file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSaveAsClicked(object sender, EventArgs e)
        {
            try
            {
                IFileDialog fileDialog = new FileDialog()
                {
                    Prompt = "Select query file.",
                    Action = FileDialog.FileActionType.Save,
                    FileType = "SQL files (*.sql)|*.sql|Text files (*.txt)|*.txt"
                };

                string filename = fileDialog.GetFile();
                entry1.Text = filename;

                File.WriteAllText(filename, Sql);                
            }
            catch (Exception error)
            {
                ShowError(error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {    
                // Save the SQL to file described in entry box
                if (entry1.Text != null)
                {
                    File.WriteAllText(entry1.Text, textview1.Buffer.Text);
                }              
            }
            catch (Exception error)
            {
                ShowError(error);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRunClicked(object sender, EventArgs e)
        {
            if (OnRunQuery != null)
            {
                OnRunQuery.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Detach()
        {
            loadbtn.Clicked -= OnLoadClicked;
            savebtn.Clicked -= OnSaveClicked;
            saveasbtn.Clicked -= OnSaveAsClicked;
            runbtn.Clicked -= OnRunClicked;
        }
    }
}
