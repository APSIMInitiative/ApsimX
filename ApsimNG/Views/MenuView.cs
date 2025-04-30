using UserInterface.Extensions;
using Gtk;
using UserInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using Utility;

namespace UserInterface.Views
{
    /// <summary>
    /// Encapsulates a menu
    /// </summary>
    public class MenuView
    {
        private Menu menu = new Menu();

        /// <summary>Constructor</summary>
        public MenuView()
        {
            Accelerators = new AccelGroup();
        }

        /// <summary>Accelerators for the menu</summary>
        public AccelGroup Accelerators { get; private set; }

        /// <summary>Destroy the menu</summary>
        public void Destroy()
        {
            menu.Clear();
            menu.Dispose();
        }

        /// <summary>Populate the main menu tool strip.</summary>
        /// <param name="menuDescriptions">Descriptions for each item.</param>
        public void Populate(List<MenuDescriptionArgs> menuDescriptions)
        {
            menu.Clear();
            foreach (MenuDescriptionArgs description in menuDescriptions)
            {
                if (description.Enabled)
                {
                    MenuItem item;
                    if (description.ShowCheckbox)
                    {
                        CheckMenuItem checkItem = new CheckMenuItem(description.Name);
                        checkItem.Active = description.Checked;
                        item = checkItem;
                    }
                    else
                    {
                        ManifestResourceInfo info = null;
                        if (!string.IsNullOrEmpty(description.ResourceNameForImage))
                            info = Assembly.GetExecutingAssembly().GetManifestResourceInfo(description.ResourceNameForImage);
                        if (info != null)
                        {
                            //Add a space to the entries with icons so the text isn't up against the icon
                            MenuItem imageItem = WidgetExtensions.CreateImageMenuItem(" " + description.Name, new Gtk.Image(null, description.ResourceNameForImage));
                            item = imageItem;
                        }
                        else
                        {
                            item = new MenuItem(description.Name);
                        }
                    }

                    if (!String.IsNullOrEmpty(description.ShortcutKey))
                    {
                        string keyName = String.Empty;
                        Gdk.ModifierType modifier = Gdk.ModifierType.None;
                        string[] keyNames = description.ShortcutKey.Split(new Char[] { '+' });
                        foreach (string name in keyNames)
                        {
                            if (name == "Ctrl")
                                modifier |= Gdk.ModifierType.ControlMask;
                            else if (name == "Shift")
                                modifier |= Gdk.ModifierType.ShiftMask;
                            else if (name == "Alt")
                                modifier |= Gdk.ModifierType.Mod1Mask;
                            else if (name == "Del")
                                keyName = "Delete";
                            else
                                keyName = name;
                        }
                        try
                        {
                            Gdk.Key accelKey = (Gdk.Key)Enum.Parse(typeof(Gdk.Key), keyName, false);
                            item.AddAccelerator("activate", Accelerators, (uint)accelKey, modifier, AccelFlags.Visible);
                        }
                        catch
                        {
                        }
                    }
                    if (description.OnClick != null)
                        item.Activated += description.OnClick;
                    if (description.FollowsSeparator && (menu.Children.Length > 0))
                    {
                        menu.Append(new SeparatorMenuItem());
                    }
                    menu.Append(item);
                }
            }
            menu.ShowAll();
        }

        /// <summary>Low level method to attach this menu to a widget</summary>
        /// <param name="w">Widget to attach to</param>
        public void AttachToWidget(Widget w)
        {
            if (menu.AttachWidget == null)
                menu.AttachToWidget(w, null);
        }

        /// <summary>Low level method to show the menu</summary>
        public void Show()
        {
            menu.Popup();
        }
    }
}
