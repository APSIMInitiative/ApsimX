﻿using Gtk;
using System;

namespace UserInterface.Views
{

    public class FactorView : ViewBase, IFactorView
    {
        /// <summary>Constructor</summary>
        /// <param name="owner">The owner widget.</param>
        public FactorView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.FactorView.glade");
            mainWidget = (Widget)builder.GetObject("scrolledwindow1");
            mainWidget.Destroyed += OnMainWidgetDestroyed;

            Specification = new EditView(owner, 
                                         (Entry)builder.GetObject("specificationEditBox"));
        }

        /// <summary>Gets or sets the specification.</summary>
        public IEditView Specification { get; set; }

        /// <summary>Invoked when main widget has been destroyed.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                (Specification as EditView).Dispose();

                mainWidget.Destroyed -= OnMainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    public interface IFactorView
    {
        /// <summary>Gets or sets the specification.</summary>
        IEditView Specification { get; set; }
    }
}