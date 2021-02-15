using Gtk;
using System;
using System.Linq;

namespace UserInterface.Classes
{
    /// <summary>
    /// Very similar to MenuToolButton, but this widget synchronises the
    /// arrow drop-down button and the 'main' widget (image) button.
    /// </summary>
    /// <remarks>
    /// TODO : better error handling.
    /// </remarks>
    internal class CustomMenuToolButton : MenuToolButton
    {
        /// <summary>
        /// Constructor. Calls base class constructor and connects events.
        /// </summary>
        /// <param name="image">Image to be displayed on the button.</param>
        /// <param name="text">Text to be displayed on the button.</param>
        internal CustomMenuToolButton(Widget image, string text) : base(image, text)
        {
            GetToggleButton().StateChanged += SyncButtonStates;
            GetMenuButton().StateChanged += SyncButtonStates;
            GetMenuButton().Clicked += OnMenuButtonClicked;
        }

        /// <summary>
        /// Invoked when the main button is clicked. Simulates a click on
        /// the arrow button to force the drop-down to appear.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        protected void OnMenuButtonClicked(object sender, EventArgs args)
        {
            try
            {
                GetToggleButton().Click();
            }
            catch //(Exception err) // fixme
            {
                //ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the arrow button changes states. Synchronises the
        /// arrow button's state with the main button's state.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        protected void StateChange(object sender, StateChangedArgs args)
        {
            try
            {
#if NETFRAMEWORK
                GetMenuButton().State = GetToggleButton().State;
#endif
            }
            catch //(Exception err) // fixme
            {
                //ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when any of the buttons' states changes. Applies this
        /// state change to all child buttons.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        protected void SyncButtonStates(object sender, StateChangedArgs args)
        {
            try
            {
#if NETFRAMEWORK
                if (!(sender is Widget))
                    return;
                foreach (Button button in GetButtons())
                    button.State = (sender as Widget).State;
                GetMenuButton().State = GetToggleButton().State;
#endif
            }
            catch //(Exception err) // fixme
            {
                //ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the button is destroyed. Detaches event handlers.
        /// </summary>
        protected override void OnDestroyed()
        {
            try
            {
                GetToggleButton().StateChanged -= SyncButtonStates;
                GetMenuButton().StateChanged -= SyncButtonStates;
                GetMenuButton().Clicked -= OnMenuButtonClicked;
                base.OnDestroyed();
            }
            catch //(Exception err) // fixme
            {
                //ShowError(err);
            }
        }

        /// <summary>
        /// Gets all child buttons.
        /// </summary>
        /// <returns></returns>
        private Button[] GetButtons()
        {
            return (this.Children[0] as Container)?.AllChildren.Cast<Widget>().Where(c => c is Button).Cast<Button>().ToArray();
        }

        /// <summary>
        /// Gets the arrow button.
        /// </summary>
        /// <returns></returns>
        private ToggleButton GetToggleButton()
        {
            return (this.Children[0] as Container).AllChildren.OfType<ToggleButton>().LastOrDefault();
        }

        /// <summary>
        /// Gets the image button.
        /// </summary>
        /// <returns></returns>
        private Button GetMenuButton()
        {
            return (this.Children[0] as Container).AllChildren.OfType<Button>().FirstOrDefault();
        }
    }
}
