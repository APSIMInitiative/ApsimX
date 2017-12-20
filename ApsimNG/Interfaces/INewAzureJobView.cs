using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApsimNG.Cloud;
using System.ComponentModel;

namespace UserInterface.Interfaces
{
    public interface INewAzureJobView
    {
        void SetDefaultJobName(string st);
        JobParameters jobParams { get; }
        BackgroundWorker SubmitJob { get; }
        void ShowError(string msg);
        /// <summary>
        /// Displays a warning message and asks the user if they want to continue.
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <returns>true if the user wants to continue, false otherwise</returns>
        bool ShowWarning(string msg);
        void DisplayStatus(string status);
    }
}
