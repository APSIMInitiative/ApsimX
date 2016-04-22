using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
///using System.Windows.Forms;
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
        }

        public IGridView GridView { get { return Grid; } }
        public IEditorView Editor { get { return ScriptEditor; } }
       
    }
}
