namespace Models.Core
{
    using System;

    /// <summary>
    /// Specifies that the related class should use the user interface view
    /// that has the specified name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ContextMenuAttribute : System.Attribute
    {
        /// <summary>
        /// Gets or sets the menu name.
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// Gets or sets the model types that this menu applies to.
        /// </summary>
        public Type[] AppliesTo { get; set; }

        /// <summary>
        /// Gets or sets the model types that this menu DOES NOT apply to.
        /// </summary>
        public Type[] Excluding { get; set; }

        /// <summary>
        /// Key to be treated as a shortcut for the menu item.
        /// </summary>
        public string ShortcutKey { get; set; }

        /// <summary>
        /// Indicates whether this item can toggle between "on" and "off" states
        /// </summary>
        public bool IsToggle { get; set;  }

        /// <summary>
        /// A separator is placed before this item.
        /// </summary>
        public bool FollowsSeparator { get; set; }
    } 
}
