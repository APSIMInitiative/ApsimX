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

        private GridView Grid;
        private EditorView ScriptEditor;
        private Notebook notebook;


        /// <summary>
        /// Constructor
        /// </summary>
        public ManagerView(ViewBase owner) : base(owner)
        {
            notebook = new Notebook();
            _mainWidget = notebook;
            Grid = new GridView(this);
            ScriptEditor = new EditorView(this);
            notebook.AppendPage(Grid.MainWidget, new Label("Properties"));
            notebook.AppendPage(ScriptEditor.MainWidget, new Label("Script"));
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            Grid.MainWidget.Destroy();
            Grid = null;
            ScriptEditor.MainWidget.Destroy();
            ScriptEditor = null;
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

        public IGridView GridView { get { return Grid; } }
        public IEditorView Editor { get { return ScriptEditor; } }
       
    }
}
