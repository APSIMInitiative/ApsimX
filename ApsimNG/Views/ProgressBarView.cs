using System;
using Gtk;


namespace UserInterface.Views
{
    /// <summary>A progress bar view.</summary>
    public class ProgressBarView : ViewBase
    {
        /// <summary>
        /// The button object
        /// </summary>
        private ProgressBar progressBar;

        /// <summary>Constructor.</summary>
        public ProgressBarView()
        {
        }

        /// <summary>The objects constructor</summary>
        /// <param name="owner">The owning view</param>
        public ProgressBarView(ViewBase owner) : base(owner)
        {
            progressBar = new ProgressBar();
            mainWidget = progressBar;
            mainWidget.Destroyed += OnMainWidgetDestroyed;
        }

        /// <summary>The position of the progress bar (0-100).</summary>
        public double Position
        {
            get { return progressBar.Fraction * 100.0; }
            set { progressBar.Fraction = value / 100.0; }
        }

        /// <summary>Sets the visibility of the progress bar.</summary>
        public bool Visible { get => progressBar.Visible; set => progressBar.Visible = value; }

        /// <summary>
        /// Cleanup objects
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The argument parameters</param>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            mainWidget.Destroyed -= OnMainWidgetDestroyed;
            owner = null;
        }


        /// <summary>
        /// A method used when a view is wrapping a gtk control.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="gtkControl">The gtk control being wrapped.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            owner = ownerView;
            progressBar = (ProgressBar)gtkControl;
            mainWidget = progressBar;
            mainWidget.Destroyed += OnMainWidgetDestroyed;
        }

    }
}
