using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace UserInterface
{
    static class UserInterface
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message;
                Msg += "\r\n" + err.StackTrace;
                MessageBox.Show(Msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
