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
        /// <summary>
        /// Provides access to the properties grid.
        /// </summary>
        IGridView GridView { get; }

        /// <summary>
        /// Provides access to the editor.
        /// </summary>
        Utility.IEditor Editor { get; }
    }

    public partial class ManagerView : UserControl, IManagerView
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ManagerView()
        {
            InitializeComponent();
        }

        public IGridView GridView { get { return Grid; } }
        public Utility.IEditor Editor { get { return ScriptEditor; } }
       
    }
}
