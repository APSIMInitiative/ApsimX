// -----------------------------------------------------------------------
// <copyright file="Cultivar.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Windows.Forms;
    using EventArguments;
    using Interfaces;

    /// <summary>
    /// A cultivar view class
    /// </summary>
    public partial class CultivarView : UserControl, ICultivarView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CultivarView" /> class.
        /// </summary>
        public CultivarView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the aliases have changed.
        /// </summary>
        public event EventHandler AliasesChanged;

        /// <summary>
        /// Invoked when the commands have changed.
        /// </summary>
        public event EventHandler CommandsChanged;

        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        public event EventHandler<NeedContextItems> ContextItemsNeeded; 

        /// <summary>
        /// Gets or sets a list of all aliases.
        /// </summary>
        public string[] Aliases
        {
            get
            {
                return this.textBox1.Lines;
            }

            set
            {
                this.textBox1.Lines = value;
            }
        }

        /// <summary>
        /// Gets or sets a list of commands.
        /// </summary>
        public string[] Commands
        {
            get
            {
                return this.editor1.Lines;
            }

            set
            {
                this.editor1.Lines = value;
            }
        }

        /// <summary>
        /// User has pressed a '.'.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnContextItemsNeeded(object sender, NeedContextItems e)
        {
            if (this.ContextItemsNeeded != null)
            {
                this.ContextItemsNeeded.Invoke(this, e);
            }
        }

        /// <summary>
        /// The aliases have been changed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnTextChanged(object sender, EventArgs e)
        {
            if (this.AliasesChanged != null)
            {
                this.AliasesChanged.Invoke(this, e);
            }
        }

        /// <summary>
        /// The commands have changed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnTextHasChangedByUser(object sender, EventArgs e)
        {
            if (this.CommandsChanged != null)
            {
                this.CommandsChanged.Invoke(this, e);
            }
        }

        private void editor1_TextHasChangedByUser(object sender, EventArgs e)
        {

        }

    }
}
