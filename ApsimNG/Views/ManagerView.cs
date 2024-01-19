﻿using Shared.Utilities;
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
            //if we are switching from the script page, save the position
            if (this.TabIndex == TAB_PROPERTY)
            {
                cursor = scriptEditor.Location;
            }
            else if (cursor != null)
            {
                scriptEditor.Location = cursor;
                scriptEditor.Refresh();
            }
        }

        /// <summary>
        /// OnDrawn event handler for setting the scrollbar on the script editor
        /// </summary>
        public void OnDrawn(object sender, EventArgs e)
        {
            //We can move the scrollbar until everything is displayed on screen,
            //So we skip the first two draw calls and move it on the 3rd.
            if (drawCount < 2)
            {
                drawCount += 1;
            }
            else if (cursor != null && this.TabIndex == TAB_SCRIPT)
            {
                //We then only do this once and disable the event
                notebook.Drawn -= OnDrawn;
                scriptEditor.Location = cursor;
                scriptEditor.Refresh();
            } else if (this.TabIndex == TAB_PROPERTY)
            { //on the other tab, disable the event
                notebook.Drawn -= OnDrawn;
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

        /// <summary>
        /// The values for the cursor and scrollbar position in the script editor
        /// </summary>
        ManagerCursorLocation CursorLocation { get; set; }
    }
}
