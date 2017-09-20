using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using APSIM.Shared.Utilities;
using MonoMac.AppKit;
using Gtk;


namespace UserInterface
{
    public class ViewBase
    {
        static string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        protected ViewBase _owner = null;
        protected Widget _mainWidget = null;

        public ViewBase Owner { get { return _owner; } }

        public Widget MainWidget { get { return _mainWidget; } }

        public ViewBase(ViewBase owner) { _owner = owner; }

        protected Gdk.Window mainWindow { get { return MainWidget == null ? null : MainWidget.Toplevel.GdkWindow; } }
        private bool waiting = false;

        protected bool hasResource(string name)
        {
            return resources.Contains(name);
        }
        public bool WaitCursor
        {
            get
            {
                return waiting;
            }

            set
            {
                if (mainWindow != null)
                {
                    if (value == true)
                        mainWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                    else
                        mainWindow.Cursor = null;
                    waiting = value;
                }
            }
        }

        private static Timer timer = new Timer();

        /// <summary>Ask user for a filename to open on Windows.</summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        static public string WindowsFileDialog(string prompt, string fileSpec, FileChooserAction action, string initialPath)
        {
            string fileName = null;
            FileDialog dialog;
            if (action == FileChooserAction.Open)
                dialog = new OpenFileDialog();
            else
                dialog = new SaveFileDialog();
            dialog.Title = prompt;
            if (!String.IsNullOrEmpty(fileSpec))
                dialog.Filter = fileSpec + "|All files (*.*)|*.*";

            if (!String.IsNullOrWhiteSpace(initialPath)  && (File.Exists(initialPath) || action == FileChooserAction.Save))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                dialog.FileName = null;
                // This almost works, but Windows is buggy.
                // If the file name is long, it doesn't display in a sensible way
                // dialog.FileName = Path.GetFileName(initialPath);
            }
            else if (Directory.Exists(initialPath))
                dialog.InitialDirectory = initialPath;
            else
                dialog.InitialDirectory = Utility.Configuration.Settings.PreviousFolder;
            /*
            if (!string.IsNullOrEmpty(initialPath))
            {
                timer.Tick += new EventHandler(WindowsWorkaround);
                timer.Interval = 50;
                timer.Tag = dialog;
                timer.Start();
            }*/
            if (dialog.ShowDialog() == DialogResult.OK)
                fileName = dialog.FileName;
            dialog = null;
            return fileName;
        }

        /*
        /// <summary>
        /// Works around weird Windows bug.
        /// See https://connect.microsoft.com/VisualStudio/feedback/details/525070/openfiledialog-show-part-of-file-name-in-win7
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void WindowsWorkaround(object sender, EventArgs args)
        {
            FileDialog dialog = timer.Tag as FileDialog;
            if (dialog != null)
            {
                SendKeys.SendWait("{HOME}");
                SendKeys.SendWait("^(a)");
                SendKeys.Flush();
                (sender as Timer).Stop();
            }
        }
        */
        /// <summary>Ask user for a filename to open on Windows.</summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        static public string OSXFileDialog(string prompt, string fileSpec, FileChooserAction action, string initialPath)
        {
            string fileName = null;
            int result = 0;
            NSSavePanel panel;
            if (action == FileChooserAction.Open)
                panel = new NSOpenPanel();
            else
                panel = new NSSavePanel();
            panel.Title = prompt;

            if (!String.IsNullOrEmpty(fileSpec))
            {
                string[] specParts = fileSpec.Split(new Char[] { '|' });
                int nExts = 0;
                string[] allowed = new string[specParts.Length / 2];
                for (int i = 0; i < specParts.Length; i += 2)
                {
                    string pattern = Path.GetExtension(specParts[i + 1]);
                    if (!String.IsNullOrEmpty(pattern))
                    {
                        pattern = pattern.Substring(1); // Get rid of leading "."
                        if (!String.IsNullOrEmpty(pattern))
                            allowed[nExts++] = pattern;
                    }
                }
                if (nExts > 0)
                {
                    Array.Resize(ref allowed, nExts);
                    panel.AllowedFileTypes = allowed;
                }
            }
            panel.AllowsOtherFileTypes = true;

            if (File.Exists(initialPath))
            {
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(Path.GetDirectoryName(initialPath));
                panel.NameFieldStringValue = Path.GetFileName(initialPath);
            }
            else if (Directory.Exists(initialPath))
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(initialPath);
            else
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(Utility.Configuration.Settings.PreviousFolder);

            result = panel.RunModal();
            if (result == 1 /*NSFileHandlingPanelOKButton*/)
            {
                fileName = panel.Url.Path;
            }
            return fileName;
        }

        /// <summary>Ask user for a filename to open.</summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        static public string AskUserForFileName(string prompt, string fileSpec, FileChooserAction action = FileChooserAction.Open, string initialPath = "")
        {

            string fileName = null;

            if (ProcessUtilities.CurrentOS.IsWindows)
                return WindowsFileDialog(prompt, fileSpec, action, initialPath);
            else if (ProcessUtilities.CurrentOS.IsMac)
                return OSXFileDialog(prompt, fileSpec, action, initialPath);
            else
            {
                string btnText = "Open";
                if (action == FileChooserAction.Save)
                    btnText = "Save";
                FileChooserDialog fileChooser = new FileChooserDialog(prompt, null, action, "Cancel", ResponseType.Cancel, btnText, ResponseType.Accept);

                if (!String.IsNullOrEmpty(fileSpec))
                {
                    string[] specParts = fileSpec.Split(new Char[] { '|' });
                    for (int i = 0; i < specParts.Length; i += 2)
                    {
                        FileFilter fileFilter = new FileFilter();
                        fileFilter.Name = specParts[i];
                        fileFilter.AddPattern(specParts[i + 1]);
                        fileChooser.AddFilter(fileFilter);
                    }
                }

                FileFilter allFilter = new FileFilter();
                allFilter.AddPattern("*");
                allFilter.Name = "All files";
                fileChooser.AddFilter(allFilter);

                if (File.Exists(initialPath))
                    fileChooser.SetFilename(initialPath);
                else if (Directory.Exists(initialPath))
                    fileChooser.SetCurrentFolder(initialPath);
                else
                    fileChooser.SetCurrentFolder(Utility.Configuration.Settings.PreviousFolder);
                if (fileChooser.Run() == (int)ResponseType.Accept)
                    fileName = fileChooser.Filename;
                fileChooser.Destroy();
            }
            return fileName;
        }

        /// <summary>
        /// "Native" structure for a key press event
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct EventKeyStruct
        {
            public Gdk.EventType type;
            public IntPtr window;
            public sbyte send_event;

            public uint time;
            public uint state;
            public uint keyval;
            public uint length;
            public string str;
            public ushort hardware_keycode;
            public byte group;
            public uint is_modifier;
        }

        /// <summary>
        /// Fires a key press event to a widget. This is not something that should be used very often,
        /// but it provides a way to store a value of a cell being edited in a grid when the user closes the grid
        /// (e.g., by selecting a different view).
        /// I haven't been able to find any better way to do it.
        /// It's placed in this unit because there may be uses in other contexts.
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="key"></param>
        public static void SendKeyEvent(Gtk.Widget widget, Gdk.Key key)
        {
            uint keyval = (uint)key;
            Gdk.Window window = widget.GdkWindow;
            Gdk.KeymapKey[] keymap = Gdk.Keymap.Default.GetEntriesForKeyval(keyval);

            EventKeyStruct native = new EventKeyStruct();
            native.type = Gdk.EventType.KeyPress;
            native.window = window.Handle;
            native.send_event = 1;
            native.state = (uint)Gdk.EventMask.KeyPressMask;
            native.keyval = keyval;
            native.length = 0;
            native.str = null;
            native.hardware_keycode = (ushort)keymap[0].Keycode;
            native.group = (byte)keymap[0].Group;
            
            IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc(native);
            try
            {
                Gdk.EventKey evnt = new Gdk.EventKey(ptr);
                Gdk.EventHelper.Put(evnt);
                // We need to process the event, or we won't be able
                // to safely free the unmanaged pointer
                // Using DoEvent for this fails on the Mac
                while (GLib.MainContext.Iteration())
                    ;
                // Gtk.Main.DoEvent(evnt);
            }
            finally
            {
                GLib.Marshaller.Free(ptr);
            }
        }
    }
}