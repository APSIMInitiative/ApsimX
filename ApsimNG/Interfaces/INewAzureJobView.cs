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
        Presenters.NewAzureJobPresenter Presenter { get; set; }
        
        void DisplayStatus(string status);

        string GetFile(List<string> extensions, string extName = "");        

        string GetZipFile();
    }
}
