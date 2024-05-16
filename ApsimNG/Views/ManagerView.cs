using Shared.Utilities;
using Gtk;
using System;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public class ManagerView : ViewBase,  IManagerView
    {
        private PropertyView propertyEditor;
        private IEditorView scriptEditor;
        private Notebook notebook;
        private ManagerCursorLocation cursor;
        private int drawCount; //used to count how many times the screen has been drawn for drawn event handler

        //constants for the tab indicies
        private const int TAB_PROPERTY = 0;
        private const int TAB_SCRIPT = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        public ManagerView(ViewBase owner) : base(owner)
        {
            notebook = new Notebook();
            mainWidget = notebook;
            propertyEditor = new PropertyView(this);
            scriptEditor = new EditorView(this)
            {
                ShowLineNumbers = true,
                Language = "c-sharp",
                ReadOnly = true
            };
            notebook.AppendPage(propertyEditor.MainWidget, new Label("Parameters"));
            notebook.AppendPage(((ViewBase)scriptEditor).MainWidget, new Label("Script"));
            mainWidget.Destroyed += _mainWidget_Destroyed;

            drawCount = 0;

            notebook.SwitchPage += OnPageChanged;
            notebook.Drawn += OnDrawn;
            
        }

        /// <summary>
        /// OnPageChanged event handler.
        /// </summary>
        public void OnPageChanged(object sender, EventArgs e)
        {
            
            if (cursor != null)
            {
                if (this.TabIndex == TAB_PROPERTY) //if we are switching from the script page, save the position
                {
                    cursor = scriptEditor.Location;
                }
                else
                {
                    scriptEditor.Location = cursor;
                }
            }
        }

        /// <summary>
        /// OnDrawn event handler for setting the scrollbar on the script editor
        /// </summary>
        public void OnDrawn(object sender, EventArgs e)
        {
            //Wait either 40 frames or until the scrollbars match in size
            //This is required because the text is loaded in over time from a buffer, so big files
            //can take a while to completely load in. If we set the scrollbar too early, it scrolls
            //to the wrong position as more text is loaded.
            if (cursor == null)
                cursor = CursorLocation;

            if (drawCount < 40 && !(scriptEditor.Location.ScrollV.Upper == cursor.ScrollV.Upper && scriptEditor.Location.ScrollH.Upper == cursor.ScrollH.Upper))
            {
                drawCount += 1;
            } 
            else
            {
                scriptEditor.ReadOnly = false;
                notebook.Drawn -= OnDrawn;
                if (cursor != null && this.TabIndex == TAB_SCRIPT)
                    scriptEditor.Location = cursor;
            }
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            try
            {
                propertyEditor.Dispose();
                propertyEditor = null;
                (scriptEditor as ViewBase)?.Dispose();
                scriptEditor = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                notebook.SwitchPage -= OnPageChanged;
                notebook.Drawn -= OnDrawn;

                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        public int TabIndex
        {
            get { return notebook.CurrentPage; }
            set { notebook.CurrentPage = value; }
        }

        /// <summary>
        /// The values for the cursor and scrollbar position in the script editor
        /// </summary>
        public ManagerCursorLocation CursorLocation
        {
            get {
                ManagerCursorLocation pos = scriptEditor.Location;
                pos.TabIndex = this.TabIndex;
                return pos;
            }
            set {
                cursor = value;
                TabIndex = value.TabIndex;
            }
        }

        public IPropertyView PropertyEditor { get { return propertyEditor; } }
        public IEditorView Editor { get { return scriptEditor; } }
    }
}
