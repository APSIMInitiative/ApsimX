namespace UserInterface
{
    using Models;
    using Presenters;
    using System;
    using System.IO;
    using Views;

    static class UserInterface
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {

            Gtk.Application.Init();
            MainView mainForm = new MainView();
            MainPresenter mainPresenter = new MainPresenter();

            // Clean up temporary files.
            string tempFolder = Path.Combine(Path.GetTempPath(), "ApsimX");
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);
            Environment.SetEnvironmentVariable("TMP", tempFolder, EnvironmentVariableTarget.Process);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Manager.ResolveManagerAssembliesEventHandler);

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
    }
}
