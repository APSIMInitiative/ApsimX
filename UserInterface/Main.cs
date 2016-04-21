using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainView mainForm = new MainView();
            MainPresenter mainPresenter = new MainPresenter();

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
