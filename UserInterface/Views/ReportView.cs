using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UserInterface.Views
{

    public delegate void StringDelegate(string St);

    interface IReportView
    {
        /// <summary>
        /// A property to get and set the list of variables on the view.
        /// </summary>
        string[] VariableNames { get; set; }
        
        /// <summary>
        /// A property to get and set the list of events on the view.
        /// </summary>
        string[] EventNames { get; set; }

        /// <summary>
        /// This event will be invoked when the view needs a list of variable names.
        /// </summary>
        event EventHandler<Utility.Editor.NeedContextItems> NeedVariableNames;

        /// <summary>
        /// This event will be invoked when the view needs a list of event names.
        /// </summary>
        event EventHandler<Utility.Editor.NeedContextItems> NeedEventNames;

        /// <summary>
        /// The variable names have changed.
        /// </summary>
        event EventHandler VariableNamesChanged;

        /// <summary>
        /// The event names have chaanged.
        /// </summary>
        event EventHandler EventNamesChanged;
    }



    public partial class ReportView : UserControl, IReportView
    {
        public event EventHandler<Utility.Editor.NeedContextItems> NeedVariableNames;
        public event EventHandler<Utility.Editor.NeedContextItems> NeedEventNames;
        public event EventHandler VariableNamesChanged;
        public event EventHandler EventNamesChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReportView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// A property to get and set the list of variables on the view.
        /// </summary>
        public string[] VariableNames
        {
            get
            {
                return VariableEditor.Lines;
            }
            set
            {
                VariableEditor.Lines = value;
            }
        }

        /// <summary>
        /// A property to get and set the list of events on the view.
        /// </summary>
        public string[] EventNames
        {
            get
            {
                return FrequencyEditor.Lines;
            }
            set
            {
                FrequencyEditor.Lines = value;
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
        /// The event list editor is asking for names of events for the specified object name
        /// </summary>
        private void OnEventListNeedItems(object Sender, Utility.Editor.NeedContextItems e)
        {
            if (NeedEventNames != null)
                NeedEventNames(Sender, e);
        }

        /// <summary>
        ///  The variable list has changed - store in model.
        /// </summary>
        private void OnVariableListChanged(object sender, EventArgs e)
        {
            if (VariableNamesChanged != null)
                VariableNamesChanged(sender, e);
        }

        /// <summary>
        /// The event list has changed - store in model.
        /// </summary>
        private void OnEventListChanged(object sender, EventArgs e)
        {
            if (EventNamesChanged != null)
                EventNamesChanged(sender, e);
        }
    }
}
