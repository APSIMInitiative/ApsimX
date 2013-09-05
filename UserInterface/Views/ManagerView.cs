using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{
    interface IManagerView
    {
        IGridView GridView { get; }
        string Code { get; set; }

        /// <summary>
        /// This event will be invoked when the view needs a list of variable names.
        /// </summary>
        event EventHandler<Utility.Editor.NeedContextItems> NeedVariableNames;

        /// <summary>
        /// The view's code has changed
        /// </summary>
        event EventHandler CodeChanged;

    }

    public partial class ManagerView : UserControl, IManagerView
    {
        public event EventHandler<Utility.Editor.NeedContextItems> NeedVariableNames;
        public event EventHandler CodeChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public ManagerView()
        {
            InitializeComponent();
        }

        public IGridView GridView { get { return Grid; } }
        public string Code
        {
            get
            {
                return ScriptEditor.Text;
            }
            set
            {
                ScriptEditor.Text = value;
            }
        }

        /// <summary>
        /// The variable list editor is asking for names of variables for the specified object name.
        /// </summary>
        private void OnVariableListNeedItems(object Sender, Utility.Editor.NeedContextItems e)
        {
            if (NeedVariableNames != null)
                NeedVariableNames(Sender, e);
        }

        /// <summary>
        /// User has changed the code, invoke our event.
        /// </summary>
        private void ScriptEditor_TextHasChangedByUser(object sender, EventArgs e)
        {
            if (CodeChanged != null)
                CodeChanged(sender, e);
        }


    }
}
