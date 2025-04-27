using System;

namespace UserInterface.Interfaces
{
    /// <summary>A class for holding info about a collection of menu items.</summary>
    public class MenuDescriptionArgs
    {
        /// <summary>Text on the item</summary>
        public string Name;

        /// <summary>Item tooltip</summary>
        public string ToolTip;

        /// <summary>The resource name for the item image</summary>
        public string ResourceNameForImage;

        /// <summary>The on click handler</summary>
        public EventHandler OnClick;

        /// <summary>Does the item show a checkbox?</summary>
        public bool ShowCheckbox;

        /// <summary>Is the item checked (is ticked)?</summary>
        public bool Checked;

        /// <summary>The shortcut key</summary>
        public string ShortcutKey;

        /// <summary>Is the item enabled</summary>
        public bool Enabled;

        /// <summary>For toolstrips, is this menu item aligned to right side of bar?</summary>
        public bool RightAligned;

        /// <summary>Has a separator preceding it. </summary>
        public bool FollowsSeparator;

        /// <summary>MenuDescriptionArgs Constructor</summary>
        public MenuDescriptionArgs() {
            Name = "";
            ToolTip = "";
            ResourceNameForImage = "empty";
            OnClick = null;
            ShowCheckbox = false;
            Checked = false;
            ShortcutKey = "";
            Enabled = true;
            RightAligned = false;
            FollowsSeparator = false;
        }
    }
}
