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

            };
            notebook.AppendPage(propertyEditor.MainWidget, new Label("Parameters"));
            notebook.AppendPage(((ViewBase)scriptEditor).MainWidget, new Label("Script"));
            mainWidget.Destroyed += _mainWidget_Destroyed;
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

        public IPropertyView PropertyEditor { get { return propertyEditor; } }
        public IEditorView Editor { get { return scriptEditor; } }
    }

    public interface IManagerView
    {
        /// <summary>
        /// Provides access to the properties grid.
        /// </summary>
        /// <remarks>
        /// Change type to IProeprtyView when ready to release new property view.
        /// </remarks>
        IPropertyView PropertyEditor { get; }

        /// <summary>
        /// Provides access to the editor.
        /// </summary>
        IEditorView Editor { get; }

        /// <summary>
        /// Indicates the index of the currently active tab
        /// </summary>
        int TabIndex { get; set; }
    }
}
