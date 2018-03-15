using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Presenters
{
    public interface INewCloudJobPresenter
    {
        /// <summary>
        /// Validates user input, saves their choices and starts the job submission in a separate thread.
        /// </summary>
        /// <param name="jp">Job Parameters.</param>
        void SubmitJob(ApsimNG.Cloud.JobParameters jp);

        /// <summary>
        /// Cancels submission of a job and hides the right hand panel (which holds the new job view).
        /// </summary>
        void CancelJobSubmission();

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        void ShowErrorMessage(string msg);

        /// <summary>
        /// Displays an error.
        /// </summary>
        /// <param name="err">Error to be displayed.</param>
        void ShowError(Exception err);
    }
}
