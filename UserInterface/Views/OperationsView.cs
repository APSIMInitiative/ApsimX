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
    public interface IOperationsView
    {
        /// <summary>
        /// A property for accessing the editorview.
        /// </summary>
        Utility.Editor EditorView { get; }
    }

    public partial class OperationsView : UserControl
    {
        public event EventHandler<Utility.NeedContextItems> NeedContextItems;

        /// <summary>
        /// Constructor
        /// </summary>
        public OperationsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// A property for accessing the editorview.
        /// </summary>
        public Utility.Editor EditorView { get { return this.Editor; } }

    }
}
