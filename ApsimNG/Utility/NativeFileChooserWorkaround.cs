using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace APSIMNG.Utility
{
    /// <summary>
    /// Bypasses a GtkSharp bug (see https://github.com/GtkSharp/GtkSharp/issues/345) where a
    /// mis-marshaled P/Invoke signature corrupts filename&lt;-&gt;UTF-8 conversion on Apple
    /// Silicon (arm64) macOS. Reads a file chooser's selected filenames directly from the
    /// native GTK API, decoding them with .NET's own (correct) UTF-8 handling instead of
    /// GtkSharp's broken marshaller.
    /// </summary>
    internal static class NativeFileChooserWorkaround
    {
        static NativeFileChooserWorkaround()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeFileChooserWorkaround).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            string[] candidates = libraryName switch
            {
                "gtk-3" => new[] { "/opt/homebrew/lib/libgtk-3.dylib", "/opt/homebrew/lib/libgtk-3.0.dylib", "/usr/local/lib/libgtk-3.dylib" },
                "glib-2.0" => new[] { "/opt/homebrew/lib/libglib-2.0.dylib", "/opt/homebrew/lib/libglib-2.0.0.dylib", "/usr/local/lib/libglib-2.0.dylib" },
                _ => Array.Empty<string>()
            };
            foreach (string candidate in candidates)
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out IntPtr handle))
                    return handle;
            return IntPtr.Zero;
        }

        [DllImport("gtk-3")]
        private static extern IntPtr gtk_file_chooser_get_filenames(IntPtr chooser);

        [DllImport("glib-2.0")]
        private static extern void g_slist_free(IntPtr list);

        [DllImport("glib-2.0")]
        private static extern void g_free(IntPtr mem);

        /// <summary>
        /// Reads the currently selected filenames from a native GtkFileChooser, given its
        /// native GObject handle (<see cref="GLib.Object.Handle"/>).
        /// </summary>
        public static string[] GetFilenames(IntPtr chooserHandle)
        {
            IntPtr list = gtk_file_chooser_get_filenames(chooserHandle);
            var result = new List<string>();
            IntPtr node = list;
            while (node != IntPtr.Zero)
            {
                IntPtr dataPtr = Marshal.ReadIntPtr(node, 0);
                IntPtr nextPtr = Marshal.ReadIntPtr(node, IntPtr.Size);
                if (dataPtr != IntPtr.Zero)
                {
                    string filename = Marshal.PtrToStringUTF8(dataPtr);
                    if (filename != null)
                        result.Add(filename);
                    g_free(dataPtr);
                }
                node = nextPtr;
            }
            if (list != IntPtr.Zero)
                g_slist_free(list);
            return result.ToArray();
        }
    }
}
