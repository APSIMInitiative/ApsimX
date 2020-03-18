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
            Application.Init();
            Settings.Default.SetLongProperty("gtk-menu-images", 1, "");
            //IntellisensePresenter.Init();

            var app = new Application("org.test.test", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            MainView mainForm = new MainView();
            app.AddWindow(mainForm.MainWidget as Window);

            MainPresenter mainPresenter = new MainPresenter();

            try
            {
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
