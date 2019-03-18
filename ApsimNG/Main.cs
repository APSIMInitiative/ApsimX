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
            Gtk.Settings.Default.SetLongProperty("gtk-menu-images", 1, "");
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
    }
}
