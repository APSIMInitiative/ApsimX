namespace UserInterface
{
    using APSIM.Shared.Utilities;
    using Models;
    using Presenters;
    using System;
    using System.IO;
    using Utility;
    using Views;

    static class UserInterface
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            LoadTheme();

            Gtk.Application.Init();
#if NETFRAMEWORK
            Gtk.Settings.Default.SetLongProperty("gtk-menu-images", 1, "");
#endif
            IntellisensePresenter.Init();
            MainView mainForm = new MainView();
            MainPresenter mainPresenter = new MainPresenter();

            try
            {
                mainPresenter.Attach(mainForm, args);
                mainForm.MainWidget.ShowAll();
                if (args.Length == 0 || Path.GetExtension(args[0]) != ".cs")
                    Gtk.Application.Run();
            }
            catch (Exception err)
            {
                File.WriteAllText("errors.txt", err.ToString());
                return 1;
            }
            return 0;
        }

        private static void LoadTheme()
        {
#if NETCOREAPP
            if (!ProcessUtilities.CurrentOS.IsLinux && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GTK_THEME")))
            {
                string themeName;
                if (Configuration.Settings.DarkTheme)
                    themeName = "Adwaita:dark";
                else
                    themeName = "Adwaita";

                //themeName = "Windows10";
                //themeName = "Windows10Dark";
                Environment.SetEnvironmentVariable("GTK_THEME", themeName);
            }
#endif
        }
    }
}
