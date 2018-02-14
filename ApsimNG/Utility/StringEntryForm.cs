using System;
using System.Collections.Generic;
using Gtk;
using Mono.TextEditor;
using Cairo;
using UserInterface;

namespace Utility
{
    public class StringEntryForm
    {
        // Gtk Widgets
        private Window window1 = null;
        private Label lblPrompt = null;
        private Button btnOK = null;
        private Button btnCancel = null;
        private Entry txtEntry = null;

        private bool selectionOnly = false;

        /// <summary>Constructor</summary>
        public StringEntryForm()
        {
            Builder builder = ViewBase.BuilderFromResource("ApsimNG.Resources.Glade.StringEntryForm.glade");
            window1 = (Window)builder.GetObject("window1");
            lblPrompt = (Label)builder.GetObject("lblPrompt");
            btnOK = (Button)builder.GetObject("btnOK");
            btnCancel = (Button)builder.GetObject("btnCancel");
            txtEntry = (Entry)builder.GetObject("txtEntry");

            btnOK.Clicked += btnOK_Click;
            btnCancel.Clicked += btnCancel_Click;
            window1.DeleteEvent += Window1_DeleteEvent;
            window1.Destroyed += Window1_Destroyed;
        }


        /// <summary>Gets or sets the text in the edit field</summary>
        public string EditorContents
        {
            get
            {
                return txtEntry.Text;
            }
            set
            {
                txtEntry.Text = value;
            }
        }

        /// <summary>Set the title bar of the window.</summary>
        /// <param name="title">The title to put into the window caption</param>
        public void SetTitleBar(string title)
        {
            window1.Title = title;
        }


        public void Destroy()
        {
            window1.Destroy();
        }

        /// <summary>Show window</summary>
        public void Show(Window topLevelWindow)
        {
            window1.TransientFor = topLevelWindow;

            window1.Parent = topLevelWindow;
            UpdateTitleBar();
            window1.Show();
            txtEntry.GrabFocus();
        }

        /// <summary>Handler for windows being destroyed</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window1_Destroyed(object sender, EventArgs e)
        {
            btnOK.Clicked -= btnOK_Click;
            btnCancel.Clicked -= btnCancel_Click;
            window1.DeleteEvent -= Window1_DeleteEvent;
            window1.Destroyed -= Window1_Destroyed;
            window1.DeleteEvent -= Window1_DeleteEvent;
            window1.Destroyed -= Window1_Destroyed;
        }

        /// <summary>
        /// Handler for window being deleted
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void Window1_DeleteEvent(object o, DeleteEventArgs args)
        {
            window1.Hide();
            args.RetVal = true;
        }

        /// <summary>
        /// Handler for OK button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            window1.Hide();
        }

        /// <summary>
        /// Handler for Cancel button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            window1.Hide();
        }
    }

}

    
