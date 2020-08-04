using Gtk;
using System;
using UserInterface.Extensions;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public interface IManagerView
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
        private IEditorView scriptEditor;
        private Notebook notebook;


        /// <summary>
        /// Constructor
        /// </summary>
        public ManagerView(ViewBase owner) : base(owner)
        {
            notebook = new Notebook();
            mainWidget = notebook;
            grid = new GridView(this);
            scriptEditor = new EditorView(this)
            {
#if NETCOREAPP
                ShowLineNumbers = true,
                Language = "c-sharp",
#endif
            };
            notebook.AppendPage(grid.MainWidget, new Label("Parameters"));
            notebook.AppendPage((scriptEditor as ViewBase).MainWidget, new Label("Script"));
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, System.EventArgs e)
        {
            try
            {
                grid.MainWidget.Cleanup();
                grid = null;
                (scriptEditor as ViewBase)?.MainWidget?.Cleanup();
                scriptEditor = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
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

        public IGridView GridView { get { return grid; } }
        public IEditorView Editor { get { return scriptEditor; } }
       
    }
}
