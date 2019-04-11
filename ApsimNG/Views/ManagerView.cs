using Gtk;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    interface IManagerView
    {
        /// <summary>
        /// Provides access to the properties grid.
        /// </summary>
        IGridView GridView { get; }

        /// <summary>
        /// Provides access to the editor.
        /// </summary>
        IEditorView Editor { get; }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get;  set; }
    }

    public class ManagerView : ViewBase,  IManagerView
    {

        private GridView grid;
        private EditorView scriptEditor;
        private Notebook notebook;


        /// <summary>
        /// Constructor
        /// </summary>
        public ManagerView(ViewBase owner) : base(owner)
        {
            notebook = new Notebook();
            _mainWidget = notebook;
            grid = new GridView(this);
            scriptEditor = new EditorView(this);
            notebook.AppendPage(grid.MainWidget, new Label("Properties"));
            notebook.AppendPage(scriptEditor.MainWidget, new Label("Script"));
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            grid.MainWidget.Destroy();
            grid = null;
            scriptEditor.MainWidget.Destroy();
            scriptEditor = null;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
        }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        public int TabIndex
        {
            get { return notebook.CurrentPage; }
            set { notebook.CurrentPage = value; }
        }

        public IGridView GridView { get { return grid; } }
        public IEditorView Editor { get { return scriptEditor; } }
       
    }
}
