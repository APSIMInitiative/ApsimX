using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UserInterface.Commands;

namespace UserInterface
{
    class StartPagePlugin
    {

        /// <summary>
        /// Setup the GUI
        /// </summary>
        public void Setup(ApplicationCommands ApplicationCommands)
        {
            ApplicationCommands.AddStartTab();
        }



    }
}
