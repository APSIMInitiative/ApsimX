using APSIM.Shared.Utilities;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Presenters;
using UserInterface.Views;
using Utility;

namespace UserInterface
{
    static class UserInterface
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                LoadTheme();

                Task.Run(() => Intellisense.CodeCompletionService.Init());
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                Gtk.Application.Init();

                Gtk.Settings.Default.SetProperty("gtk-overlay-scrolling", new GLib.Value(0));

                IntellisensePresenter.Init();
                MainView mainForm = new MainView();
                MainPresenter mainPresenter = new MainPresenter();

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

        }
    }
}
