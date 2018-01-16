using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// Interface defining standard functionality which all cloud job presenters much implement.
    /// </summary>
    interface ICloudJobPresenter
    {
        void DownloadResults(List<string> jobIds, bool saveToCsv, bool includeDebugFiles, bool keepOutputFiles);
    }
}
