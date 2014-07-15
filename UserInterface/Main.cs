using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

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
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm f = new MainForm(args);
                Application.Run(f);

                // ErrorMessage can be set when a startup script fails.
                if (f.ErrorMessage != null)
                {
                    File.WriteAllText("errors.txt", f.ErrorMessage);
                    return 1;
                }
            }
            catch (Exception err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message;
                Msg += "\r\n" + err.StackTrace;
                MessageBox.Show(Msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return 0;
        }
    }
}
