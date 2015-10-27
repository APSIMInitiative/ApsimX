// -----------------------------------------------------------------------
// <copyright file="InsertView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>An interface for a list/button box</summary>
    public interface IListButtonView
    {
        /// <summary>The list.</summary>
        IListBoxView List { get; }

        /// <summary>The button.</summary>
        IButtonView Button { get; }
    }


    /// <summary>An view for a list with a button</summary>
    public partial class ListButtonView : UserControl, IListButtonView
    {
        /// <summary>Constructor</summary>
        public ListButtonView()
        {
            InitializeComponent();
        }

        /// <summary>The list.</summary>
        public IListBoxView List { get { return listBoxView1; } }

        /// <summary>The button.</summary>
        public IButtonView Button { get { return buttonView1; } }
    }
}
