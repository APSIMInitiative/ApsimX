using APSIM.Shared.JobRunning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Models.Core.Run
{
    [Serializable]
    class EmptyJob : IRunnable
    {
        /// <summary>Called to start the job. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(System.Threading.CancellationTokenSource cancelToken)
        {
            //do nothing
        }

    }
}
