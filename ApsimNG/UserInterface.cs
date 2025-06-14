using APSIM.Shared.Utilities;
using ApsimNG.Utility;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
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
        private static Mutex appMutex = null;
        private const string appMutexName = "ApsimNGMutex";
        private const string appPipeName = "ApsimNGPipe";

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                bool isOnlyInstance = SingleInstanceCheck(args);
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

                PipeManager pipeManager = null;

                if (isOnlyInstance)
                {
                    pipeManager = new PipeManager(appPipeName);
                    pipeManager.StartServer();
                    pipeManager.ReceiveString += mainPresenter.OnNamedPipe_OpenRequest;
                }

                if (args.Length == 0 || Path.GetExtension(args[0]) != ".cs")
                    Gtk.Application.Run();
                if (isOnlyInstance)
                    pipeManager.StopServer();
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

        private static bool SingleInstanceCheck(string[] args)
        {
            // TO DO (possibly): Add configuration setting so user can control whether
            // or not to open in an existing instance. e.g., 
            // if (Configuration.Settings.UseSingleWindow)
            //     return true;

            bool isOnlyInstance;
            appMutex = new Mutex(true, appMutexName, out isOnlyInstance);
            if (!isOnlyInstance)
            {
                string filesToOpen = " ";
                if (args != null && args.Length > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < args.Length; i++)
                    {
                        sb.AppendLine(args[i]);
                    }
                    filesToOpen = sb.ToString();
                }

                var manager = new PipeManager(appPipeName);
                if (manager.Write(filesToOpen))
                    // this exits the application                    
                    Environment.Exit(0);
            }
            return isOnlyInstance;
        }
    }
}

