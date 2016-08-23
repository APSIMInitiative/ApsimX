namespace UserInterface
{
    using Models;
    using Presenters;
    using System;
    using System.IO;
    using System.Windows.Forms;
    using Views;
    static class UserInterface
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainView mainForm = new MainView();
            MainPresenter mainPresenter = new MainPresenter();

            // Clean up temporary files.
            string tempFolder = Path.Combine(Path.GetTempPath(), "ApsimX");
            if (Directory.Exists(tempFolder))
                // This may fail if another ApsimX instance is running. If so,
                // we just ignore the exception and leave the cleanup for another day.
                try
                {
                    Directory.Delete(tempFolder, true);
                }
                catch (Exception)
                {
                }
            Directory.CreateDirectory(tempFolder);
            Environment.SetEnvironmentVariable("TMP", tempFolder, EnvironmentVariableTarget.Process);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Manager.ResolveManagerAssembliesEventHandler);

            try
            {
                mainPresenter.Attach(mainForm, args);
                if (args.Length == 0 || Path.GetExtension(args[0]) != ".cs")
                    Application.Run(mainForm);
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
