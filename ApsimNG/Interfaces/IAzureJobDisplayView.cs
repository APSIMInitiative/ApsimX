using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Interfaces
{
    public interface IAzureJobDisplayView
    {
        Presenters.AzureJobDisplayPresenter Presenter { get; set; }
        void AddJobsToTableIfNecessary(List<ApsimNG.Cloud.JobDetails> jobs);
        void ShowError(string msg);
        void UpdateJobLoadStatus(double progress);
        string GetFile(List<string> extensions, string extName);
        void UpdateDownloadStatus(string path, bool successful);
    }
}
