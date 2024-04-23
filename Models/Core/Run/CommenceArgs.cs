using System.Threading;

namespace Models.Core.Run
{

    /// <summary>The arguments for a commence event.</summary>
    public class CommenceArgs
    {
        /// <summary>The token to check for a job cancellation</summary>
        public CancellationTokenSource CancelToken;
    }
}
