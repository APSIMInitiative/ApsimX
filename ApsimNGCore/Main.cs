namespace UserInterface
{
    using Presenters;
    using System;
    using System.IO;
    using Views;
    using Gtk;

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
                Application.Init();
                Settings.Default.SetLongProperty("gtk-menu-images", 1, "");
                //IntellisensePresenter.Init();

                MainView mainForm = new MainView();
                MainPresenter mainPresenter = new MainPresenter();

                mainPresenter.Attach(mainForm, args);
                mainForm.MainWidget.Show();
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
