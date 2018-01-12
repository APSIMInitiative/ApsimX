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
        void ShowError(string msg);
        void UpdateJobLoadStatus(double progress);
        string GetFile(List<string> extensions, string extName);
        void UpdateDownloadStatus(string message);
        void RemoveJobFromJobList(Guid jobId);
        void UpdateTreeView(List<ApsimNG.Cloud.JobDetails> jobs);
    }
}
