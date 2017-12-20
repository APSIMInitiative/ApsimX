using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Interfaces
{
    public interface IAzureJobDisplayView
    {
        void AddJobsToTableIfNecessary(List<ApsimNG.Cloud.JobDetails> jobs);
        void ShowError(string msg);
        void UpdateJobLoadStatus(double progress);
    }
}
