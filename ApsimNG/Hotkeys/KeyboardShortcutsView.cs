using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gtk;

namespace UserInterface.Hotkeys
{
    /// <summary>
    /// Represents a window which displays information to the user about
    /// keyboard shortcuts for the application.
    /// </summary>
    internal class KeyboardShortcutsView
    {
        /// <summary>
        /// Default section name.
        /// </summary>
        private const string sectionName = "section";

        /// <summary>
        /// The internal gtk shortcuts window object. Note: this is unmanaged
        /// memory and /must/ be disposed.
        /// </summary>
        private ShortcutsWindow window;

        /// <summary>
        /// Initialises a new <see cref="KeyboardShortcutsView"/> instance.
        /// </summary>
        public KeyboardShortcutsView()
        {
        }

        /// <summary>
        /// Populate the window with keyboard shortcut metadata.
        /// </summary>
        /// <remarks>
        /// The internal gtk structure supports multiple pages, and multiple
        /// groups of shortcuts within a single page. This method could be
        /// extended quite easily in the future if need be.
        /// </remarks>
        /// <param name="hotkeys">Keyboard shortcuts metadata.</param>
        public void Populate(IEnumerable<IHotkey> hotkeys)
        {
            ShortcutsGroup group = (ShortcutsGroup)Activator.CreateInstance(typeof(ShortcutsGroup), true);
            group.Title = "Global Keyboard Shortcuts";
            foreach (IHotkey hotkey in hotkeys)
            {
                ShortcutsShortcut shortcut = (ShortcutsShortcut)Activator.CreateInstance(typeof(ShortcutsShortcut), true);
                shortcut.Accelerator = hotkey.Shortcut;
                shortcut.ShortcutType = ShortcutType.Accelerator;
                shortcut.Title = hotkey.Description;
                group.Add(shortcut);
            }

            ShortcutsSection section = (ShortcutsSection)Activator.CreateInstance(typeof(ShortcutsSection), true);
            section.SectionName = "shortcuts";
            section.Title = "Global Shortcuts";
            section.Add(group);

            window = (ShortcutsWindow)Activator.CreateInstance(typeof(ShortcutsWindow), true);
            window.Child = section;
            window.DeleteEvent += OnWindowClosing;
        }

        /// <summary>
        /// Display the window.
        /// </summary>
        public void Show()
        {
            if (window == null)
                throw new InvalidOperationException("Shortcuts window has not been populated");
            window.ShowAll();
            window.SectionName = "shortcuts";
        }

        /// <summary>
        /// Called when the window is closed by the user. Disposes of unmanaged
        /// resources.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnWindowClosing(object sender, DeleteEventArgs args)
        {
            try
            {
                Dispose();
            }
            catch (Exception error)
            {
                Debug.WriteLine(error);
            }
        }

        /// <summary>
        /// Dispose of unmanaged resources.
        /// </summary>
        private void Dispose()
        {
            foreach (ShortcutsSection section in window)
            {
                foreach (ShortcutsGroup group in section)
                {
                    foreach (ShortcutsShortcut shortcut in group)
                        shortcut.Dispose();
                    group.Dispose();
                }
                section.Dispose();
            }
            window.Dispose();
        }
    }
}
