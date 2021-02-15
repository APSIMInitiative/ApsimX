using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Gdk;
using UserInterface.Presenters;
using UserInterface.Views;
using System.Runtime.InteropServices;
using System.Reflection;
using APSIM.Shared.Utilities;

namespace UnitTests.ApsimNG.Utilities
{
    /// <summary>
    /// gtk-related utility functions used by the UI tests.
    /// </summary>
    public static class GtkUtilities
    {
        /// <summary>
        /// A type of button press.
        /// </summary>
        public enum ButtonPressType : uint
        {
            /// <summary>
            /// A left click.
            /// </summary>
            LeftClick = 1,

            /// <summary>
            /// A middle click.
            /// </summary>
            MiddleClick = 2,

            /// <summary>
            /// A Right click.
            /// </summary>
            RightClick = 3,
        };

        /// <summary>
        /// Sends a left-click event to a widget and optionally waits for Gtk to processs the event..
        /// </summary>
        public static void Click(Widget target, bool wait = true)
        {
            if (target is Button btn)
            {
                btn.Click();
                return;
            }

            if (target is Label lbl)
            {
                GLib.Signal.Emit(lbl, "activate-link", new object[0]);
                return;
            }

            GLib.Signal.Emit(target, "button-press-event", new object[0]);
            GLib.Signal.Emit(target, "button-release-event", new object[0]);

            if (wait)
                WaitForGtkEvents();
        }

        public static void ClickOnCheckBox(ICheckBoxView checkBox, bool wait = true)
        {
            CheckButton button = (CheckButton)ReflectionUtilities.GetValueOfFieldOrProperty("checkbutton1", checkBox);
            ClickOnCheckBox(button, wait);
        }

        public static void ClickOnCheckBox(CheckButton checkBox, bool wait = true)
        {
            checkBox.Active = !checkBox.Active;
            if (wait)
                WaitForGtkEvents();
        }

        /// <summary>
        /// Waits for the Gtk event loop to clear.
        /// </summary>
        public static void WaitForGtkEvents()
        {
            while (GLib.MainContext.Iteration()) ;
        }

        /// <summary>
        /// Sends a custom button press (click) event to a particular cell in a GridView.
        /// </summary>
        /// <param name="grid">The GridView which should receive the button press event.</param>
        /// <param name="row">Row index of the cell to be clicked.</param>
        /// <param name="col">Column index of the cell to be clicked.</param>
        /// <param name="eventType">Type of event to be sent.</param>
        /// <param name="state">Modifiers for the click - ie control click, shift click, etc.</param>
        /// <param name="buttonClickType">Type of click - ie left click, middle click or right click.</param>
        /// <param name="wait">Iff true, will wait for gtk to process the event.</param>
        public static void ClickOnGridCell(GridView grid, int row, int col, EventType eventType, ModifierType state, ButtonPressType buttonClickType, bool wait = true)
        {
            // We want to click on a cell, but this requires coordinates.
            GetTreeViewCoordinates(grid.Grid, row, col, out int x, out int y);

            // Double-click on the top-right cell using the coordinates.
            Click(grid.Grid, eventType, state, buttonClickType, x, y, wait);
        }

        /// <summary>
        /// Sends a custom button press (click) event to a widget.
        /// </summary>
        /// <param name="target">Widget which should receive the button press event.</param>
        /// <param name="eventType">Type of event to be sent.</param>
        /// <param name="state">Modifiers for the click - ie control click, shift click, etc.</param>
        /// <param name="button">Type of click - ie left click, middle click or right click.</param>
        /// <param name="x">x-coordinate of the click, relative to the top-left corner of the widget.</param>
        /// <param name="y">y-coordinate of the click, relative to the top-left corner of the widget.</param>
        /// <param name="wait">Iff true, will wait for gtk to process the event.</param>
        public static void Click(Widget target, EventType eventType, ModifierType state, ButtonPressType button, double x = 0, double y = 0, bool wait = true)
        {
            Gdk.Window win = target.GdkWindow;

            int rx, ry;
            win.GetRootOrigin(out rx, out ry);

            var nativeEvent = new NativeEventButtonStruct
            {
                type = eventType,
                send_event = 1,
                window = win.Handle,
                state = (uint)state,
                button = (uint)button,
                x = x,
                y = y,
                axes = IntPtr.Zero,
                device = IntPtr.Zero,
                time = Gtk.Global.CurrentEventTime,
                x_root = x + rx,
                y_root = y + ry
            };

            IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc(nativeEvent);
            try
            {
                EventHelper.Put(new EventButton(ptr));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            if (wait)
                // Clear gtk event loop.
                while (GLib.MainContext.Iteration()) ;
        }

        /// <summary>
        /// Sends a double click event to a widget at the given coordinates, and optionally waits for
        /// Gtk to process the event.
        /// </summary>
        /// <param name="target">Widget which is the target of the double click.</param>
        /// <param name="x">x-coordinate of the click relative to the widget.</param>
        /// <param name="y">y-coordinate of the click relative to the widget.</param>
        /// <param name="wait">Iff true, will wait for gtk to process the click event.</param>
        public static void DoubleClick(Widget target, double x = 0, double y = 0, bool wait = true)
        {
            Click(target, EventType.TwoButtonPress, ModifierType.None, ButtonPressType.LeftClick, x, y, wait);
        }

        /// <summary>
        /// Sends a keypress event to a widget.
        /// </summary>
        /// <param name="target">Widget which is the target of the keypress event.</param>
        /// <param name="key">Key to be sent.</param>
        /// <param name="state">Modifier keys separated by plus signs. e.g. "Control + Alt".</param>
        /// <param name="wait">Iff true, will wait for gtk to process the event.</param>
        public static void SendKeyPress(Widget target, char key, string state = null, bool wait = true)
        {
            ModifierType modifier = ParseModifier(state);
            Gdk.Key realKey = ParseKey(key);
            TypeKey(target, realKey, modifier);

            if (wait)
                // Wait for gtk to process the event.
                while (GLib.MainContext.Iteration()) ;
        }

        public static void GetTreeViewCoordinates(Gtk.TreeView tree, int rowIndex, int colIndex, out int x, out int y)
        {
            TreePath path = new TreePath(new int[1] { rowIndex });
            TreeViewColumn column = tree.Columns[colIndex];
            Rectangle rect = tree.GetCellArea(path, column);
            x = rect.X;
            y = rect.Y;
        }

        public static void TypeKey(Widget target, Gdk.Key key, ModifierType modifier, bool wait = true)
        {
            SendKeyEvent(target, (uint)key, modifier, EventType.KeyPress);
            SendKeyEvent(target, (uint)key, modifier, EventType.KeyRelease);

            if (wait)
                WaitForGtkEvents();
        }

        /// <summary>
        /// Used internally - sends a keypress event to a widget.
        /// </summary>
        /// <param name="target">Target widget.</param>
        /// <param name="keyVal">Key value being sent.</param>
        /// <param name="state">Key press state - e.g. ctrl click.</param>
        /// <param name="eventType">Type of event to be sent.</param>
        private static void SendKeyEvent(Widget target, uint keyVal, ModifierType state, EventType eventType)
        {
            Gdk.KeymapKey[] keyms = Gdk.Keymap.Default.GetEntriesForKeyval(keyVal);
            if (keyms.Length == 0)
                throw new Exception("Keyval not found");

            Gdk.Window win = target.GdkWindow;

            var nativeEvent = new NativeEventKeyStruct
            {
                type = eventType,
                send_event = 1,
                window = win.Handle,
                state = (uint)state,
                keyval = keyVal,
                group = (byte)keyms[0].Group,
                hardware_keycode = (ushort)keyms[0].Keycode,
                length = 0,
                time = Gtk.Global.CurrentEventTime
            };

            IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc(nativeEvent);
            try
            {
                Gdk.EventHelper.Put(new Gdk.EventKey(ptr));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Parses a character to a Gdk.Key.
        /// </summary>
        /// <param name="key">Key value.</param>
        private static Gdk.Key ParseKey(char key)
        {
            if (key == '\n')
                return Gdk.Key.Return;
            else
                return (Gdk.Key)Gdk.Global.UnicodeToKeyval(key);

        }

        /// <summary>
        /// Parses a string of plus sign-delimited modifiers to a gdk ModifierType struct.
        /// </summary>
        /// <param name="state">Plus sign-delimited modifier string. e.g. "Control + alt".</param>
        private static ModifierType ParseModifier(string state)
        {
            if (string.IsNullOrEmpty(state))
                return ModifierType.None;

            string[] modifiers = state.Split('+').Select(m => m.Trim().ToLower()).ToArray();
            ModifierType modifier = ModifierType.None;

            foreach (string mod in modifiers)
            {
                switch (mod)
                {
                    case "shift":
                        modifier |= ModifierType.ShiftMask;
                        break;

                    case "lock":
                        modifier |= Gdk.ModifierType.LockMask;
                        break;

                    case "control":
                        modifier |= Gdk.ModifierType.ControlMask;
                        break;

                    case "mod1":
                        modifier |= Gdk.ModifierType.Mod1Mask;
                        break;

                    case "mod2":
                        modifier |= Gdk.ModifierType.Mod2Mask;
                        break;

                    case "mod3":
                        modifier |= Gdk.ModifierType.Mod3Mask;
                        break;

                    case "mod4":
                        modifier |= Gdk.ModifierType.Mod4Mask;
                        break;

                    case "mod5":
                        modifier |= Gdk.ModifierType.Mod5Mask;
                        break;

                    case "super":
                        modifier |= Gdk.ModifierType.SuperMask;
                        break;

                    case "hyper":
                        modifier |= Gdk.ModifierType.HyperMask;
                        break;

                    case "meta":
                        modifier |= Gdk.ModifierType.MetaMask;
                        break;

                    default:
                        modifier |= Gdk.ModifierType.None;
                        break;
                }
            }

            return modifier;
        }
        
        public static void SelectComboBoxItem(IDropDownView view, string item, bool wait = true)
        {
            ComboBox combo = ReflectionUtilities.GetValueOfFieldOrProperty("combobox1", view) as ComboBox;
            if (combo == null)
                throw new Exception("Unable to get combo box from drop down view - has its property name changed?");

            SelectComboBoxItem(combo, item, wait);
        }

        public static void SelectComboBoxItem(ComboBox combo, string item, bool wait = true)
        {
            //// We want to click on a cell, but this requires coordinates.
            ////GtkUtilities.GetTreeViewCoordinates(combo.Cell, 0, 1, out int x, out int y);
            // fixme
            if (combo.Model.GetIterFirst(out TreeIter iter))
            {
                string entry = (string)combo.Model.GetValue(iter, 0);
                while (entry != null && !entry.Equals(item, StringComparison.InvariantCultureIgnoreCase) && combo.Model.IterNext(ref iter)) // Should the text matchin be case-insensitive?
                    entry = (string)combo.Model.GetValue(iter, 0);
                if (entry == item)
                    combo.SetActiveIter(iter);
                else // Could not find a matching entry
                    combo.Active = -1;
            }

            if (wait)
                WaitForGtkEvents();
        }

        // Analysis disable InconsistentNaming
        [StructLayout(LayoutKind.Sequential)]
        struct NativeEventButtonStruct
        {
            public Gdk.EventType type;
            public IntPtr window;
            public sbyte send_event;
            public uint time;
            public double x;
            public double y;
            public IntPtr axes;
            public uint state;
            public uint button;
            public IntPtr device;
            public double x_root;
            public double y_root;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct NativeEventKeyStruct
        {
            public Gdk.EventType type;
            public IntPtr window;
            public sbyte send_event;
            public uint time;
            public uint state;
            public uint keyval;
            public int length;
            public IntPtr str;
            public ushort hardware_keycode;
            public byte group;
            public uint is_modifier;
        }
        // Analysis restore InconsistentNaming
    }
}
