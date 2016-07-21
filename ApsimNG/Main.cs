using System;
using System.IO;
using UserInterface.Views;
using UserInterface.Presenters;

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
            Gtk.Application.Init();
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
