using Gtk;
using System;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    public class ViewBase
    {
        /// <summary>
        /// A reference to the main view.
        /// </summary>
        public static IMainView MasterView = null;

        /// <summary>
        /// The parent view.
        /// </summary>
        protected ViewBase _owner = null;

        /// <summary>
        /// The main widget in this view.
        /// </summary>
        protected Widget _mainWidget = null;

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="err"></param>
        protected void ShowError(Exception err)
        {
            MasterView.ShowMessage(err.ToString(), Models.Core.Simulation.ErrorLevel.Error);
        }

        /// <summary>
        /// The parent view.
        /// </summary>
        public ViewBase Owner
        {
            get
            {
                return _owner;
            }
        }

        /// <summary>
        /// The main widget in this view.
        /// </summary>
        public Widget MainWidget
        {
            get
            {
                return _mainWidget;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The parent view.</param>
        public ViewBase(ViewBase owner)
        {
            _owner = owner;
        }
    }
}